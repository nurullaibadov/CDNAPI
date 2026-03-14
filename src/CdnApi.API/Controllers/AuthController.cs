using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CdnApi.Application.DTOs.Auth;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.API.Controllers;

/// <summary>
/// Authentication endpoints — register, login, refresh token, email verification, password reset.
/// </summary>
[Tags("Authentication")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });

        var result = await _authService.RegisterAsync(dto);
        if (!result.Success) return Fail(result.Message!);
        return Created(result, result.Message!);
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });

        var result = await _authService.LoginAsync(dto, CurrentUserIp);
        if (!result.Success) return Unauthorized(new ApiResponse { Success = false, Message = result.Message });
        return Success(result, "Login successful.");
    }

    /// <summary>Refresh an expired access token using a refresh token.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        if (!result.Success) return Unauthorized(new ApiResponse { Success = false, Message = result.Message });
        return Success(result);
    }

    /// <summary>Logout and invalidate the current refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(CurrentUserId);
        return Success(null, "Logged out successfully.");
    }

    /// <summary>Verify email address with the token sent by email.</summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return Fail("Token is required.");
        var result = await _authService.VerifyEmailAsync(token);
        if (!result) return Fail("Invalid or expired verification token.", 400);
        return Success(null, "Email verified successfully. You can now login.");
    }

    /// <summary>Request a password reset email.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _authService.ForgotPasswordAsync(dto);
        // Always return success to prevent email enumeration
        return Success(null, "If this email is registered, you will receive a password reset link shortly.");
    }

    /// <summary>Reset password using the token from the reset email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });

        var result = await _authService.ResetPasswordAsync(dto);
        if (!result) return Fail("Invalid or expired reset token.", 400);
        return Success(null, "Password reset successfully. You can now login with your new password.");
    }

    /// <summary>Change password for the currently authenticated user.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });

        var result = await _authService.ChangePasswordAsync(CurrentUserId, dto);
        if (!result) return Fail("Current password is incorrect.", 400);
        return Success(null, "Password changed successfully.");
    }
}
