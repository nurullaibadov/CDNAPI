using System.ComponentModel.DataAnnotations;
using CdnApi.Domain.Enums;

namespace CdnApi.Application.DTOs.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int TotalFiles { get; set; }
    public long StorageUsedBytes { get; set; }
    public long StorageQuotaBytes { get; set; }
}

public class AdminUpdateUserDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public long? StorageQuotaBytes { get; set; }
    public bool? EmailVerified { get; set; }
}

public class AdminUserFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class AdminFileFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public Guid? UserId { get; set; }
    public string? ContentType { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int BannedUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisMonth { get; set; }
    public long TotalFiles { get; set; }
    public long TotalStorageUsed { get; set; }
    public string TotalStorageFormatted { get; set; } = string.Empty;
    public long TotalDownloads { get; set; }
    public int FilesUploadedToday { get; set; }
    public List<DailyStatDto> DailyStats { get; set; } = new();
    public List<ContentTypeStatDto> ContentTypeStats { get; set; } = new();
}

public class DailyStatDto
{
    public DateTime Date { get; set; }
    public int Uploads { get; set; }
    public int Downloads { get; set; }
    public int NewUsers { get; set; }
}

public class ContentTypeStatDto
{
    public string ContentType { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalSize { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogFilterDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
}

public class BanUserRequestDto
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
