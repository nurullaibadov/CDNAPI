using System.ComponentModel.DataAnnotations;
using CdnApi.Domain.Enums;

namespace CdnApi.Application.DTOs.User;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UpdateProfileRequestDto
{
    [Required][MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required][MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }
}

public class StorageInfoDto
{
    public long QuotaBytes { get; set; }
    public long UsedBytes { get; set; }
    public long AvailableBytes { get; set; }
    public double UsagePercentage { get; set; }
    public string QuotaFormatted { get; set; } = string.Empty;
    public string UsedFormatted { get; set; } = string.Empty;
    public string AvailableFormatted { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
}
