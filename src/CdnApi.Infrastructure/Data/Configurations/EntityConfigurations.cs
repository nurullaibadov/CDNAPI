using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CdnApi.Domain.Entities;

namespace CdnApi.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);
        builder.Property(u => u.Role).HasConversion<int>();
        builder.Property(u => u.Status).HasConversion<int>();

        builder.HasMany(u => u.CdnFiles)
               .WithOne(f => f.User)
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ApiKeys)
               .WithOne(k => k.User)
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.AuditLogs)
               .WithOne(a => a.User)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CdnFileConfiguration : IEntityTypeConfiguration<CdnFile>
{
    public void Configure(EntityTypeBuilder<CdnFile> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => f.StoredFileName).IsUnique();
        builder.Property(f => f.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(f => f.StoredFileName).IsRequired().HasMaxLength(500);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(f => f.CdnUrl).IsRequired().HasMaxLength(1000);
        builder.Property(f => f.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(f => f.Status).HasConversion<int>();
        builder.Property(f => f.StorageProvider).HasConversion<int>();
        builder.Property(f => f.Description).HasMaxLength(2000);
        builder.Property(f => f.Tags).HasMaxLength(500);

        builder.HasMany(f => f.AccessLogs)
               .WithOne(a => a.File)
               .HasForeignKey(a => a.FileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.Key).IsUnique();
        builder.Property(s => s.Key).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Value).IsRequired();
        // NOTE: Seed data removed from here — use CreateDatabase.sql instead
    }
}
