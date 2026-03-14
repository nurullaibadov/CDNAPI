using CdnApi.Domain.Common;

namespace CdnApi.Domain.Entities;

public class UserApiKey : BaseEntity
{
    public Guid UserId { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? AllowedIps { get; set; }
    public long RequestCount { get; set; } = 0;

    // Navigation
    public User User { get; set; } = null!;
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation
    public User? User { get; set; }
}

public class FileAccessLog : BaseEntity
{
    public Guid FileId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public string AccessType { get; set; } = "download";

    // Navigation
    public CdnFile File { get; set; } = null!;
}

public class EmailLog : BaseEntity
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public string EmailType { get; set; } = string.Empty;
}

public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
}
