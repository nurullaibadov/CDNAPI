using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CdnApi.Application.DTOs.Admin;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Application.Interfaces.Services;
using CdnApi.Domain.Enums;
using CdnApi.Infrastructure.Data;

namespace CdnApi.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IStorageService _storageService;
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService,
        IStorageService storageService, AppDbContext context, ILogger<AdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _storageService = storageService;
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(AdminUserFilterDto filter)
    {
        var query = _context.Users
            .Include(u => u.CdnFiles.Where(f => !f.IsDeleted))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(u => u.FirstName.Contains(filter.SearchTerm) ||
                                      u.LastName.Contains(filter.SearchTerm) ||
                                      u.Email.Contains(filter.SearchTerm));

        if (filter.Role.HasValue) query = query.Where(u => u.Role == filter.Role);
        if (filter.Status.HasValue) query = query.Where(u => u.Status == filter.Status);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResultDto<AdminUserDto>
        {
            Items = _mapper.Map<IEnumerable<AdminUserDto>>(users),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<AdminUserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.CdnFiles.Where(f => !f.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        return _mapper.Map<AdminUserDto>(user);
    }

    public async Task<AdminUserDto> UpdateUserAsync(Guid userId, AdminUpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (dto.FirstName != null) user.FirstName = dto.FirstName;
        if (dto.LastName != null) user.LastName = dto.LastName;
        if (dto.Role.HasValue) user.Role = dto.Role.Value;
        if (dto.Status.HasValue) user.Status = dto.Status.Value;
        if (dto.StorageQuotaBytes.HasValue) user.StorageQuotaBytes = dto.StorageQuotaBytes.Value;
        if (dto.EmailVerified.HasValue) user.EmailVerified = dto.EmailVerified.Value;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.AuditLogs.AddAsync(new Domain.Entities.AuditLog
        {
            Action = "AdminUpdateUser",
            EntityName = "User",
            EntityId = userId.ToString()
        });
        await _unitOfWork.SaveChangesAsync();
        return await GetUserByIdAsync(userId);
    }

    public async Task BanUserAsync(Guid userId, string reason)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.Status = UserStatus.Banned;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.AuditLogs.AddAsync(new Domain.Entities.AuditLog
        {
            Action = "BanUser",
            EntityName = "User",
            EntityId = userId.ToString(),
            NewValues = $"{{\"reason\":\"{reason}\"}}"
        });
        await _unitOfWork.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try { await _emailService.SendBanNotificationAsync(user.Email, user.FullName, reason); }
            catch { /* ignore */ }
        });
    }

    public async Task UnbanUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.Status = UserStatus.Active;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        await _unitOfWork.Users.SoftDeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
        var bannedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Banned);
        var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= todayStart);
        var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= monthStart);
        var totalFiles = await _context.CdnFiles.LongCountAsync();
        var totalStorage = await _context.CdnFiles.SumAsync(f => (long?)f.FileSizeBytes) ?? 0;
        var totalDownloads = await _context.CdnFiles.SumAsync(f => (long?)f.DownloadCount) ?? 0;
        var filesToday = await _context.CdnFiles.CountAsync(f => f.CreatedAt >= todayStart);

        // Daily stats for last 7 days
        var dailyStats = new List<DailyStatDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = todayStart.AddDays(-i);
            var nextDate = date.AddDays(1);
            dailyStats.Add(new DailyStatDto
            {
                Date = date,
                Uploads = await _context.CdnFiles.CountAsync(f => f.CreatedAt >= date && f.CreatedAt < nextDate),
                Downloads = 0, // Could be computed from access logs
                NewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate)
            });
        }

        // Content type stats
        var contentTypeStats = await _context.CdnFiles
            .GroupBy(f => f.ContentType)
            .Select(g => new ContentTypeStatDto
            {
                ContentType = g.Key,
                Count = g.Count(),
                TotalSize = g.Sum(f => f.FileSizeBytes)
            })
            .OrderByDescending(s => s.Count)
            .Take(10)
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            BannedUsers = bannedUsers,
            NewUsersToday = newUsersToday,
            NewUsersThisMonth = newUsersThisMonth,
            TotalFiles = totalFiles,
            TotalStorageUsed = totalStorage,
            TotalStorageFormatted = FormatBytes(totalStorage),
            TotalDownloads = totalDownloads,
            FilesUploadedToday = filesToday,
            DailyStats = dailyStats,
            ContentTypeStats = contentTypeStats
        };
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter)
    {
        var logs = await _unitOfWork.AuditLogs.GetPagedAsync(filter.PageNumber, filter.PageSize, filter.UserId);
        var total = await _unitOfWork.AuditLogs.GetTotalCountAsync(filter.UserId);
        return new PagedResultDto<AuditLogDto>
        {
            Items = _mapper.Map<IEnumerable<AuditLogDto>>(logs),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResultDto<CdnFileResponseDto>> GetAllFilesAsync(AdminFileFilterDto filter)
    {
        var files = await _unitOfWork.CdnFiles.GetPagedAsync(
            filter.PageNumber, filter.PageSize, filter.SearchTerm, filter.UserId);
        var total = await _unitOfWork.CdnFiles.GetTotalCountAsync(filter.SearchTerm, filter.UserId);
        return new PagedResultDto<CdnFileResponseDto>
        {
            Items = _mapper.Map<IEnumerable<CdnFileResponseDto>>(files),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task DeleteFileAdminAsync(Guid fileId)
    {
        var file = await _unitOfWork.CdnFiles.GetByIdAsync(fileId)
            ?? throw new KeyNotFoundException("File not found.");
        await _storageService.DeleteFileAsync(file.StoragePath);
        await _unitOfWork.CdnFiles.SoftDeleteAsync(file);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<SystemSettingDto>> GetSystemSettingsAsync()
    {
        var settings = await _unitOfWork.SystemSettings.GetAllAsync();
        return _mapper.Map<IEnumerable<SystemSettingDto>>(settings);
    }

    public async Task UpdateSystemSettingAsync(string key, string value)
    {
        await _unitOfWork.SystemSettings.SetValueAsync(key, value);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string FormatBytes(long bytes)
    {
        string[] s = { "B", "KB", "MB", "GB", "TB" };
        int i = 0; double size = bytes;
        while (size >= 1024 && i < s.Length - 1) { size /= 1024; i++; }
        return $"{size:0.##} {s[i]}";
    }
}
