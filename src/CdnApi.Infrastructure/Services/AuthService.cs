using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using CdnApi.Application.DTOs.Auth;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Application.Interfaces.Services;
using CdnApi.Domain.Entities;
using CdnApi.Domain.Enums;

namespace CdnApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IEmailService emailService, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(dto.Email.ToLower()))
            return new AuthResponseDto { Success = false, Message = "This email address is already registered." };

        var verificationToken = _tokenService.GenerateSecureToken();

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            Role = UserRole.User,
            Status = UserStatus.PendingVerification
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = "Register",
            EntityName = "User",
            EntityId = user.Id.ToString()
        });
        await _unitOfWork.SaveChangesAsync();

        // Send verification email (non-blocking)
        _ = Task.Run(async () =>
        {
            try { await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, verificationToken); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email); }
        });

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful! Please check your email to verify your account."
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, string ipAddress)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLower());
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };

        // Check lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            return new AuthResponseDto { Success = false, Message = $"Account is temporarily locked. Try again after {user.LockoutEnd:HH:mm UTC}." };

        if (!VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("User {Email} locked out after {Attempts} failed attempts", user.Email, user.FailedLoginAttempts);
            }
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }

        if (!user.EmailVerified)
            return new AuthResponseDto { Success = false, Message = "Please verify your email address before logging in." };

        if (user.Status == UserStatus.Banned)
            return new AuthResponseDto { Success = false, Message = "Your account has been suspended. Please contact support." };

        if (user.Status == UserStatus.Inactive)
            return new AuthResponseDto { Success = false, Message = "Your account is inactive." };

        // Successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;

        var refreshTokenExpiry = dto.RememberMe
            ? DateTime.UtcNow.AddDays(30)
            : DateTime.UtcNow.AddDays(7);

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiry = refreshTokenExpiry;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = "Login",
            EntityName = "User",
            EntityId = user.Id.ToString(),
            IpAddress = ipAddress
        });
        await _unitOfWork.SaveChangesAsync();

        var accessToken = _tokenService.GenerateJwtToken(user);
        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = new UserTokenDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfileImageUrl = user.ProfileImageUrl
            }
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _unitOfWork.Users.GetByRefreshTokenAsync(refreshToken);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "Invalid or expired refresh token." };

        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            AccessToken = _tokenService.GenerateJwtToken(user),
            RefreshToken = user.RefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = new UserTokenDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfileImageUrl = user.ProfileImageUrl
            }
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return;

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(token);
        if (user == null) return false;

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.Status = UserStatus.Active;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try { await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send welcome email"); }
        });

        return true;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLower());
        if (user == null) return; // Silent - don't reveal if email exists

        var token = _tokenService.GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try { await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, token); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send password reset email"); }
        });
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(dto.Token);
        if (user == null || user.Email != dto.Email.ToLower()) return false;

        user.PasswordHash = HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RefreshToken = null; // Invalidate all sessions
        user.RefreshTokenExpiry = null;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try { await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.FullName); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send password changed notification"); }
        });

        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash)) return false;

        user.PasswordHash = HashPassword(dto.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try { await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.FullName); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send password changed notification"); }
        });

        return true;
    }

    public static string HashPassword(string password)
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            for (int i = 0; i < 32; i++)
                if (hashBytes[i + 16] != hash[i]) return false;
            return true;
        }
        catch { return false; }
    }
}
