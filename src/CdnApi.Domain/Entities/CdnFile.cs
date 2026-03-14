using CdnApi.Domain.Common;
using CdnApi.Domain.Enums;

namespace CdnApi.Domain.Entities;

public class CdnFile : BaseEntity
{
    public Guid UserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string CdnUrl { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public StorageProvider StorageProvider { get; set; } = StorageProvider.Local;
    public CdnFileStatus Status { get; set; } = CdnFileStatus.Active;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public bool IsPublic { get; set; } = true;
    public long DownloadCount { get; set; } = 0;
    public string? Checksum { get; set; }
    public string? FolderPath { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<FileAccessLog> AccessLogs { get; set; } = new List<FileAccessLog>();
}
