using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CdnApi.Application.DTOs.Admin;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.API.Controllers;

/// <summary>
/// Admin panel — full user management, file management, audit logs, system settings.
/// </summary>
[Tags("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin")]
[ApiController]
public class AdminController : BaseApiController
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // ─── Dashboard ──────────────────────────────────────────────────────────────

    /// <summary>Get dashboard statistics — users, files, storage, daily stats.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Success(stats);
    }

    // ─── User Management ────────────────────────────────────────────────────────

    /// <summary>Get paginated list of all users with optional search and filters.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilterDto filter)
    {
        var result = await _adminService.GetUsersAsync(filter);
        return Success(result);
    }

    /// <summary>Get a single user's full details.</summary>
    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        try
        {
            var user = await _adminService.GetUserByIdAsync(userId);
            return Success(user);
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "User not found." }); }
    }

    /// <summary>Update a user's role, status, storage quota, or email verification.</summary>
    [HttpPut("users/{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] AdminUpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });
        try
        {
            var user = await _adminService.UpdateUserAsync(userId, dto);
            return Success(user, "User updated.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "User not found." }); }
    }

    /// <summary>Ban a user account.</summary>
    [HttpPost("users/{userId:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Errors = ModelState });
        try
        {
            await _adminService.BanUserAsync(userId, dto.Reason);
            return Success(null, "User banned.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "User not found." }); }
    }

    /// <summary>Unban a user account.</summary>
    [HttpPost("users/{userId:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid userId)
    {
        try
        {
            await _adminService.UnbanUserAsync(userId);
            return Success(null, "User unbanned.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "User not found." }); }
    }

    /// <summary>Soft-delete a user and all their data.</summary>
    [HttpDelete("users/{userId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            await _adminService.DeleteUserAsync(userId);
            return Success(null, "User deleted.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "User not found." }); }
    }

    // ─── File Management ────────────────────────────────────────────────────────

    /// <summary>Get all files across all users with optional filters.</summary>
    [HttpGet("files")]
    public async Task<IActionResult> GetAllFiles([FromQuery] AdminFileFilterDto filter)
    {
        var result = await _adminService.GetAllFilesAsync(filter);
        return Success(result);
    }

    /// <summary>Delete any file (admin override).</summary>
    [HttpDelete("files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId)
    {
        try
        {
            await _adminService.DeleteFileAdminAsync(fileId);
            return Success(null, "File deleted.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "File not found." }); }
    }

    // ─── Audit Logs ─────────────────────────────────────────────────────────────

    /// <summary>Get audit logs with optional filtering by user.</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        var result = await _adminService.GetAuditLogsAsync(filter);
        return Success(result);
    }

    // ─── System Settings ────────────────────────────────────────────────────────

    /// <summary>Get all system configuration settings.</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _adminService.GetSystemSettingsAsync();
        return Success(settings);
    }

    /// <summary>Update a specific system setting by key.</summary>
    [HttpPut("settings/{key}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto)
    {
        await _adminService.UpdateSystemSettingAsync(key, dto.Value);
        return Success(null, "Setting updated.");
    }
}

public class UpdateSettingDto
{
    public string Value { get; set; } = string.Empty;
}
