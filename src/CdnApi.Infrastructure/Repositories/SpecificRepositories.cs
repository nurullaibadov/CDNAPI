using Microsoft.EntityFrameworkCore;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Domain.Entities;
using CdnApi.Infrastructure.Data;

namespace CdnApi.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        => await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

    public async Task<User?> GetByEmailVerificationTokenAsync(string token)
        => await _dbSet.FirstOrDefaultAsync(u => u.EmailVerificationToken == token && u.EmailVerificationTokenExpiry > DateTime.UtcNow);

    public async Task<User?> GetByPasswordResetTokenAsync(string token)
        => await _dbSet.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

    public async Task<IEnumerable<User>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(u => u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        return await query.OrderByDescending(u => u.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm = null)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(u => u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        return await query.CountAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
        => await _dbSet.AnyAsync(u => u.Email == email.ToLower());
}

public class CdnFileRepository : GenericRepository<CdnFile>, ICdnFileRepository
{
    public CdnFileRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<CdnFile>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize)
        => await _dbSet.Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<int> GetCountByUserIdAsync(Guid userId)
        => await _dbSet.CountAsync(f => f.UserId == userId);

    public async Task<IEnumerable<CdnFile>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, Guid? userId = null)
    {
        var query = _dbSet.Include(f => f.User).AsQueryable();
        if (userId.HasValue) query = query.Where(f => f.UserId == userId);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(f => f.OriginalFileName.Contains(searchTerm) || (f.Tags != null && f.Tags.Contains(searchTerm)));
        return await query.OrderByDescending(f => f.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm = null, Guid? userId = null)
    {
        var query = _dbSet.AsQueryable();
        if (userId.HasValue) query = query.Where(f => f.UserId == userId);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(f => f.OriginalFileName.Contains(searchTerm) || (f.Tags != null && f.Tags.Contains(searchTerm)));
        return await query.CountAsync();
    }

    public async Task<long> GetTotalStorageUsedAsync(Guid userId)
        => await _dbSet.Where(f => f.UserId == userId).SumAsync(f => f.FileSizeBytes);

    public async Task<CdnFile?> GetByStoredFileNameAsync(string storedFileName)
        => await _dbSet.Include(f => f.User).FirstOrDefaultAsync(f => f.StoredFileName == storedFileName);
}

public class UserApiKeyRepository : GenericRepository<UserApiKey>, IUserApiKeyRepository
{
    public UserApiKeyRepository(AppDbContext context) : base(context) { }

    public async Task<UserApiKey?> GetByKeyHashAsync(string keyHash)
        => await _dbSet.Include(k => k.User).FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

    public async Task<IEnumerable<UserApiKey>> GetByUserIdAsync(Guid userId)
        => await _dbSet.Where(k => k.UserId == userId).OrderByDescending(k => k.CreatedAt).ToListAsync();
}

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLog>> GetPagedAsync(int pageNumber, int pageSize, Guid? userId = null)
    {
        var query = _dbSet.IgnoreQueryFilters().Include(a => a.User).AsQueryable();
        if (userId.HasValue) query = query.Where(a => a.UserId == userId);
        return await query.OrderByDescending(a => a.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(Guid? userId = null)
    {
        var query = _dbSet.IgnoreQueryFilters().AsQueryable();
        if (userId.HasValue) query = query.Where(a => a.UserId == userId);
        return await query.CountAsync();
    }
}

public class EmailLogRepository : GenericRepository<EmailLog>, IEmailLogRepository
{
    public EmailLogRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<EmailLog>> GetUnsentAsync()
        => await _dbSet.Where(e => !e.IsSent).OrderBy(e => e.CreatedAt).Take(50).ToListAsync();
}

public class SystemSettingRepository : GenericRepository<SystemSetting>, ISystemSettingRepository
{
    public SystemSettingRepository(AppDbContext context) : base(context) { }

    public async Task<SystemSetting?> GetByKeyAsync(string key)
        => await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Key == key);

    public async Task<string?> GetValueAsync(string key)
        => (await GetByKeyAsync(key))?.Value;

    public async Task SetValueAsync(string key, string value)
    {
        var setting = await GetByKeyAsync(key);
        if (setting != null) { setting.Value = value; setting.UpdatedAt = DateTime.UtcNow; }
        else await AddAsync(new SystemSetting { Key = key, Value = value });
    }
}
