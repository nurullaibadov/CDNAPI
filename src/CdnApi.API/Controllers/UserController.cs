using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CdnApi.Application.DTOs.User;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.API.Controllers;

/// <summary>
/// User profile and storage management.
/// </summary>
[Tags("User")]
[Authorize]
public class UserController : BaseApiController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Get the current user's profile.</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _userService.GetProfileAsync(CurrentUserId);
        return Success(profile);
    }

    /// <summary>Update the current user's profile.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });

        var updated = await _userService.UpdateProfileAsync(CurrentUserId, dto);
        return Success(updated, "Profile updated successfully.");
    }

    /// <summary>Upload a profile picture. Accepts JPEG, PNG, GIF, WEBP (max 5MB).</summary>
    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Fail("No file provided.");

        try
        {
            var url = await _userService.UploadProfileImageAsync(CurrentUserId, file);
            return Success(new { profileImageUrl = url }, "Profile picture updated.");
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>Get storage usage info for the current user.</summary>
    [HttpGet("storage")]
    public async Task<IActionResult> GetStorageInfo()
    {
        var info = await _userService.GetStorageInfoAsync(CurrentUserId);
        return Success(info);
    }
}
