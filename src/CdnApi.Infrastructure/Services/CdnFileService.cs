using System.Security.Cryptography;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Application.Interfaces.Services;
using CdnApi.Domain.Entities;
using CdnApi.Domain.Enums;

namespace CdnApi.Infrastructure.Services;

public class CdnFileService : ICdnFileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CdnFileService> _logger;

    public CdnFileService(IUnitOfWork unitOfWork, IStorageService storageService, IMapper mapper,
        IConfiguration configuration, ILogger<CdnFileService> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CdnFileResponseDto> UploadFileAsync(Guid userId, UploadFileRequestDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        await ValidateFileAsync(dto.File, userId);

        var folderPath = string.IsNullOrWhiteSpace(dto.FolderPath) ? userId.ToString() : $"{userId}/{dto.FolderPath}";
        var storagePath = await _storageService.SaveFileAsync(dto.File, folderPath);
        var cdnUrl = _storageService.GetPublicUrl(storagePath);

        var checksum = await ComputeChecksumAsync(dto.File);

        var cdnFile = new CdnFile
        {
            UserId = userId,
            OriginalFileName = dto.File.FileName,
            StoredFileName = Path.GetFileName(storagePath),
            ContentType = dto.File.ContentType,
            FileSizeBytes = dto.File.Length,
            CdnUrl = cdnUrl,
            StoragePath = storagePath,
            StorageProvider = StorageProvider.Local,
            Status = CdnFileStatus.Active,
            Description = dto.Description,
            Tags = dto.Tags,
            IsPublic = dto.IsPublic,
            FolderPath = dto.FolderPath,
            Checksum = checksum
        };

        await _unitOfWork.CdnFiles.AddAsync(cdnFile);

        user.StorageUsedBytes += dto.File.Length;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<CdnFileResponseDto>(cdnFile);
        result.UploaderName = user.FullName;
        return result;
    }

    public async Task<IEnumerable<CdnFileResponseDto>> UploadMultipleFilesAsync(Guid userId, IEnumerable<IFormFile> files, string? folderPath = null)
    {
        var results = new List<CdnFileResponseDto>();
        foreach (var file in files)
        {
            var dto = new UploadFileRequestDto { File = file, FolderPath = folderPath, IsPublic = true };
            results.Add(await UploadFileAsync(userId, dto));
        }
        return results;
    }

    public async Task<CdnFileResponseDto?> GetFileByIdAsync(Guid fileId, Guid? requestingUserId = null)
    {
        var file = await _unitOfWork.CdnFiles.FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null) return null;
        if (!file.IsPublic && file.UserId != requestingUserId) return null;

        return _mapper.Map<CdnFileResponseDto>(file);
    }

    public async Task<PagedResultDto<CdnFileResponseDto>> GetUserFilesAsync(Guid userId, PaginationDto pagination)
    {
        var files = await _unitOfWork.CdnFiles.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize, pagination.SearchTerm, userId);
        var total = await _unitOfWork.CdnFiles.GetTotalCountAsync(pagination.SearchTerm, userId);

        return new PagedResultDto<CdnFileResponseDto>
        {
            Items = _mapper.Map<IEnumerable<CdnFileResponseDto>>(files),
            TotalCount = total,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await _unitOfWork.CdnFiles.GetByIdAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");

        if (file.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this file.");

        await _storageService.DeleteFileAsync(file.StoragePath);
        await _unitOfWork.CdnFiles.SoftDeleteAsync(file);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.StorageUsedBytes = Math.Max(0, user.StorageUsedBytes - file.FileSizeBytes);
            await _unitOfWork.Users.UpdateAsync(user);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CdnFileResponseDto> UpdateFileAsync(Guid fileId, Guid userId, UpdateFileRequestDto dto)
    {
        var file = await _unitOfWork.CdnFiles.GetByIdAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");

        if (file.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this file.");

        if (dto.Description != null) file.Description = dto.Description;
        if (dto.Tags != null) file.Tags = dto.Tags;
        if (dto.IsPublic.HasValue) file.IsPublic = dto.IsPublic.Value;
        if (dto.FolderPath != null) file.FolderPath = dto.FolderPath;

        await _unitOfWork.CdnFiles.UpdateAsync(file);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CdnFileResponseDto>(file);
    }

    public async Task<byte[]> ServeFileAsync(string storedFileName)
    {
        var file = await _unitOfWork.CdnFiles.GetByStoredFileNameAsync(storedFileName)
            ?? throw new FileNotFoundException("File not found.");

        return await _storageService.GetFileAsync(file.StoragePath);
    }

    public async Task IncrementDownloadCountAsync(Guid fileId, string? ipAddress, string? userAgent, string? referer)
    {
        var file = await _unitOfWork.CdnFiles.GetByIdAsync(fileId);
        if (file == null) return;

        file.DownloadCount++;
        await _unitOfWork.CdnFiles.UpdateAsync(file);

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Action = "FileAccess",
            EntityName = "CdnFile",
            EntityId = fileId.ToString(),
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task ValidateFileAsync(IFormFile file, Guid userId)
    {
        var maxSizeMb = int.Parse(await _unitOfWork.SystemSettings.GetValueAsync("MaxFileSizeMB") ?? "100");
        if (file.Length > maxSizeMb * 1024 * 1024)
            throw new InvalidOperationException($"File size exceeds the maximum allowed size of {maxSizeMb}MB.");

        var allowedTypesStr = await _unitOfWork.SystemSettings.GetValueAsync("AllowedFileTypes")
            ?? "image/jpeg,image/png,image/gif,image/webp,video/mp4,application/pdf";
        var allowedTypes = allowedTypesStr.Split(',');
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException($"File type '{file.ContentType}' is not allowed.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId)!;
        if (user!.StorageUsedBytes + file.Length > user.StorageQuotaBytes)
            throw new InvalidOperationException("Storage quota exceeded.");
    }

    private static async Task<string> ComputeChecksumAsync(IFormFile file)
    {
        using var md5 = MD5.Create();
        using var stream = file.OpenReadStream();
        var hash = await md5.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLower();
    }
}
