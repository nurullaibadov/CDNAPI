using CdnApi.Domain.Entities;

namespace CdnApi.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByEmailVerificationTokenAsync(string token);
    Task<User?> GetByPasswordResetTokenAsync(string token);
    Task<IEnumerable<User>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null);
    Task<int> GetTotalCountAsync(string? searchTerm = null);
    Task<bool> EmailExistsAsync(string email);
}

public interface ICdnFileRepository : IGenericRepository<CdnFile>
{
    Task<IEnumerable<CdnFile>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize);
    Task<int> GetCountByUserIdAsync(Guid userId);
    Task<IEnumerable<CdnFile>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, Guid? userId = null);
    Task<int> GetTotalCountAsync(string? searchTerm = null, Guid? userId = null);
    Task<long> GetTotalStorageUsedAsync(Guid userId);
    Task<CdnFile?> GetByStoredFileNameAsync(string storedFileName);
}

public interface IUserApiKeyRepository : IGenericRepository<UserApiKey>
{
    Task<UserApiKey?> GetByKeyHashAsync(string keyHash);
    Task<IEnumerable<UserApiKey>> GetByUserIdAsync(Guid userId);
}

public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetPagedAsync(int pageNumber, int pageSize, Guid? userId = null);
    Task<int> GetTotalCountAsync(Guid? userId = null);
}

public interface IEmailLogRepository : IGenericRepository<EmailLog>
{
    Task<IEnumerable<EmailLog>> GetUnsentAsync();
}

public interface ISystemSettingRepository : IGenericRepository<SystemSetting>
{
    Task<SystemSetting?> GetByKeyAsync(string key);
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICdnFileRepository CdnFiles { get; }
    IUserApiKeyRepository ApiKeys { get; }
    IAuditLogRepository AuditLogs { get; }
    IEmailLogRepository EmailLogs { get; }
    ISystemSettingRepository SystemSettings { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
