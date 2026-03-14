using CdnApi.Application.DTOs.Auth;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.DTOs.Admin;
using CdnApi.Application.DTOs.User;
using Microsoft.AspNetCore.Http;

namespace CdnApi.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId);
    Task<bool> VerifyEmailAsync(string token);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto dto);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto);
}

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto dto);
    Task<string> UploadProfileImageAsync(Guid userId, IFormFile file);
    Task<StorageInfoDto> GetStorageInfoAsync(Guid userId);
}

public interface ICdnFileService
{
    Task<CdnFileResponseDto> UploadFileAsync(Guid userId, UploadFileRequestDto dto);
    Task<IEnumerable<CdnFileResponseDto>> UploadMultipleFilesAsync(Guid userId, IEnumerable<IFormFile> files, string? folderPath = null);
    Task<CdnFileResponseDto?> GetFileByIdAsync(Guid fileId, Guid? requestingUserId = null);
    Task<PagedResultDto<CdnFileResponseDto>> GetUserFilesAsync(Guid userId, PaginationDto pagination);
    Task DeleteFileAsync(Guid fileId, Guid userId);
    Task<CdnFileResponseDto> UpdateFileAsync(Guid fileId, Guid userId, UpdateFileRequestDto dto);
    Task<byte[]> ServeFileAsync(string storedFileName);
    Task IncrementDownloadCountAsync(Guid fileId, string? ipAddress, string? userAgent, string? referer);
}

public interface IAdminService
{
    Task<PagedResultDto<AdminUserDto>> GetUsersAsync(AdminUserFilterDto filter);
    Task<AdminUserDto> GetUserByIdAsync(Guid userId);
    Task<AdminUserDto> UpdateUserAsync(Guid userId, AdminUpdateUserDto dto);
    Task BanUserAsync(Guid userId, string reason);
    Task UnbanUserAsync(Guid userId);
    Task DeleteUserAsync(Guid userId);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter);
    Task<PagedResultDto<CdnFileResponseDto>> GetAllFilesAsync(AdminFileFilterDto filter);
    Task DeleteFileAdminAsync(Guid fileId);
    Task<IEnumerable<SystemSettingDto>> GetSystemSettingsAsync();
    Task UpdateSystemSettingAsync(string key, string value);
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    Task SendVerificationEmailAsync(string toEmail, string userName, string token);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string token);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendPasswordChangedNotificationAsync(string toEmail, string userName);
    Task SendBanNotificationAsync(string toEmail, string userName, string reason);
}

public interface ITokenService
{
    string GenerateJwtToken(Domain.Entities.User user);
    string GenerateRefreshToken();
    string GenerateSecureToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folderPath);
    Task<bool> DeleteFileAsync(string storagePath);
    Task<byte[]> GetFileAsync(string storagePath);
    bool FileExists(string storagePath);
    string GetPublicUrl(string storagePath);
}

public interface IApiKeyService
{
    Task<ApiKeyResponseDto> CreateApiKeyAsync(Guid userId, CreateApiKeyRequestDto dto);
    Task<IEnumerable<ApiKeyResponseDto>> GetUserApiKeysAsync(Guid userId);
    Task RevokeApiKeyAsync(Guid userId, Guid keyId);
    Task<Domain.Entities.User?> ValidateApiKeyAsync(string apiKey);
}
