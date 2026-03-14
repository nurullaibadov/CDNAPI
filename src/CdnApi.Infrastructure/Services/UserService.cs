using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CdnApi.Application.DTOs.User;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStorageService _storageService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IStorageService storageService, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<string> UploadProfileImageAsync(Guid userId, IFormFile file)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException("Only JPEG, PNG, GIF, and WEBP images are allowed for profile pictures.");

        if (file.Length > 5 * 1024 * 1024) // 5MB
            throw new InvalidOperationException("Profile image must be smaller than 5MB.");

        // Delete old profile image if exists
        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            try
            {
                var oldPath = user.ProfileImageUrl.Replace(_storageService.GetPublicUrl(""), "");
                await _storageService.DeleteFileAsync(oldPath);
            }
            catch { /* ignore */ }
        }

        var storagePath = await _storageService.SaveFileAsync(file, "profile-images");
        var publicUrl = _storageService.GetPublicUrl(storagePath);

        user.ProfileImageUrl = publicUrl;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return publicUrl;
    }

    public async Task<StorageInfoDto> GetStorageInfoAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var totalFiles = await _unitOfWork.CdnFiles.GetCountByUserIdAsync(userId);
        var storageUsed = await _unitOfWork.CdnFiles.GetTotalStorageUsedAsync(userId);

        return new StorageInfoDto
        {
            QuotaBytes = user.StorageQuotaBytes,
            UsedBytes = storageUsed,
            AvailableBytes = user.StorageQuotaBytes - storageUsed,
            UsagePercentage = user.StorageQuotaBytes > 0
                ? Math.Round((double)storageUsed / user.StorageQuotaBytes * 100, 2)
                : 0,
            QuotaFormatted = FormatBytes(user.StorageQuotaBytes),
            UsedFormatted = FormatBytes(storageUsed),
            AvailableFormatted = FormatBytes(user.StorageQuotaBytes - storageUsed),
            TotalFiles = totalFiles
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1) { size /= 1024; i++; }
        return $"{size:0.##} {suffixes[i]}";
    }
}
