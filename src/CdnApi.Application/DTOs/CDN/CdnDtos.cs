using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using CdnApi.Domain.Enums;

namespace CdnApi.Application.DTOs.CDN;

public class UploadFileRequestDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public string? Description { get; set; }
    public string? Tags { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? FolderPath { get; set; }
}

public class UploadMultipleFilesRequestDto
{
    [Required]
    public List<IFormFile> Files { get; set; } = new();
    public string? FolderPath { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class CdnFileResponseDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string CdnUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public bool IsPublic { get; set; }
    public long DownloadCount { get; set; }
    public string? FolderPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UploaderName { get; set; }
}

public class UpdateFileRequestDto
{
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public bool? IsPublic { get; set; }
    public string? FolderPath { get; set; }
}

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class PaginationDto
{
    private int _pageSize = 20;
    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
    }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class ApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? PlainTextKey { get; set; } // Only returned on creation
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public long RequestCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateApiKeyRequestDto
{
    [Required][MaxLength(100)]
    public string KeyName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? AllowedIps { get; set; }
}
