using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Application.Interfaces.Services;
using CdnApi.Infrastructure.Data;
using CdnApi.Infrastructure.Repositories;
using CdnApi.Infrastructure.Services;

namespace CdnApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                      .EnableRetryOnFailure(3)
            ));

        // Unit of Work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICdnFileService, CdnFileService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }
}
