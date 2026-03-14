using Microsoft.EntityFrameworkCore.Storage;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Infrastructure.Data;

namespace CdnApi.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public ICdnFileRepository CdnFiles { get; }
    public IUserApiKeyRepository ApiKeys { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IEmailLogRepository EmailLogs { get; }
    public ISystemSettingRepository SystemSettings { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        CdnFiles = new CdnFileRepository(context);
        ApiKeys = new UserApiKeyRepository(context);
        AuditLogs = new AuditLogRepository(context);
        EmailLogs = new EmailLogRepository(context);
        SystemSettings = new SystemSettingRepository(context);
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
        => _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null) await _transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null) await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
