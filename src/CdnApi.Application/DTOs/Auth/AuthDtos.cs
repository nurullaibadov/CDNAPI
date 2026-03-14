using System.ComponentModel.DataAnnotations;

namespace CdnApi.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required][MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required][MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required][EmailAddress][MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required][MinLength(8)][MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required][Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }
}

public class LoginRequestDto
{
    [Required][EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiry { get; set; }
    public UserTokenDto? User { get; set; }
}

public class UserTokenDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    [Required][EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required][EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required][MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required][Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required][MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required][Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class VerifyEmailRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
