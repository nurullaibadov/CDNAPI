-- ============================================================
-- CDN API - Database Creation Script
-- Run this in SQL Server Management Studio or Package Manager Console:
--   PM> sqlcmd -S localhost -d CdnApiDb_Dev -i CreateDatabase.sql
-- OR paste directly into SSMS Query window
-- ============================================================

USE master;
GO

-- Create DB if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CdnApiDb_Dev')
BEGIN
    CREATE DATABASE CdnApiDb_Dev;
    PRINT 'Database CdnApiDb_Dev created.';
END
GO

USE CdnApiDb_Dev;
GO

-- ============================================================
-- EF Migrations history table
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[__EFMigrationsHistory]') AND type = 'U')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId]    nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT '__EFMigrationsHistory created.';
END
GO

-- ============================================================
-- 1. INDEPENDENT TABLES (no foreign keys)
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[EmailLogs]') AND type = 'U')
BEGIN
    CREATE TABLE [EmailLogs] (
        [Id]           uniqueidentifier NOT NULL DEFAULT NEWID(),
        [ToEmail]      nvarchar(max)    NOT NULL,
        [Subject]      nvarchar(max)    NOT NULL,
        [Body]         nvarchar(max)    NOT NULL,
        [IsSent]       bit              NOT NULL DEFAULT 0,
        [ErrorMessage] nvarchar(max)    NULL,
        [SentAt]       datetime2        NULL,
        [EmailType]    nvarchar(max)    NOT NULL DEFAULT '',
        [CreatedAt]    datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    datetime2        NULL,
        [IsDeleted]    bit              NOT NULL DEFAULT 0,
        [DeletedAt]    datetime2        NULL,
        CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id])
    );
    PRINT 'EmailLogs created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SystemSettings]') AND type = 'U')
BEGIN
    CREATE TABLE [SystemSettings] (
        [Id]          uniqueidentifier NOT NULL DEFAULT NEWID(),
        [Key]         nvarchar(200)    NOT NULL,
        [Value]       nvarchar(max)    NOT NULL,
        [Description] nvarchar(max)    NULL,
        [IsPublic]    bit              NOT NULL DEFAULT 0,
        [CreatedAt]   datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   datetime2        NULL,
        [IsDeleted]   bit              NOT NULL DEFAULT 0,
        [DeletedAt]   datetime2        NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_SystemSettings_Key] ON [SystemSettings] ([Key]);
    PRINT 'SystemSettings created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Users]') AND type = 'U')
BEGIN
    CREATE TABLE [Users] (
        [Id]                            uniqueidentifier NOT NULL DEFAULT NEWID(),
        [FirstName]                     nvarchar(100)    NOT NULL,
        [LastName]                      nvarchar(100)    NOT NULL,
        [Email]                         nvarchar(256)    NOT NULL,
        [PasswordHash]                  nvarchar(max)    NOT NULL,
        [PhoneNumber]                   nvarchar(20)     NULL,
        [Role]                          int              NOT NULL DEFAULT 1,
        [Status]                        int              NOT NULL DEFAULT 4,
        [ProfileImageUrl]               nvarchar(500)    NULL,
        [EmailVerified]                 bit              NOT NULL DEFAULT 0,
        [EmailVerificationToken]        nvarchar(max)    NULL,
        [EmailVerificationTokenExpiry]  datetime2        NULL,
        [PasswordResetToken]            nvarchar(max)    NULL,
        [PasswordResetTokenExpiry]      datetime2        NULL,
        [RefreshToken]                  nvarchar(max)    NULL,
        [RefreshTokenExpiry]            datetime2        NULL,
        [LastLoginAt]                   datetime2        NULL,
        [LastLoginIp]                   nvarchar(max)    NULL,
        [FailedLoginAttempts]           int              NOT NULL DEFAULT 0,
        [LockoutEnd]                    datetime2        NULL,
        [StorageQuotaBytes]             bigint           NOT NULL DEFAULT 1073741824,
        [StorageUsedBytes]              bigint           NOT NULL DEFAULT 0,
        [CreatedAt]                     datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]                     datetime2        NULL,
        [IsDeleted]                     bit              NOT NULL DEFAULT 0,
        [DeletedAt]                     datetime2        NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
    PRINT 'Users created.';
END
GO

-- ============================================================
-- 2. TABLES THAT DEPEND ON Users
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AuditLogs]') AND type = 'U')
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id]         uniqueidentifier NOT NULL DEFAULT NEWID(),
        [UserId]     uniqueidentifier NULL,
        [Action]     nvarchar(max)    NOT NULL,
        [EntityName] nvarchar(max)    NOT NULL,
        [EntityId]   nvarchar(max)    NULL,
        [OldValues]  nvarchar(max)    NULL,
        [NewValues]  nvarchar(max)    NULL,
        [IpAddress]  nvarchar(max)    NULL,
        [UserAgent]  nvarchar(max)    NULL,
        [CreatedAt]  datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]  datetime2        NULL,
        [IsDeleted]  bit              NOT NULL DEFAULT 0,
        [DeletedAt]  datetime2        NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
    PRINT 'AuditLogs created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[CdnFiles]') AND type = 'U')
BEGIN
    CREATE TABLE [CdnFiles] (
        [Id]               uniqueidentifier NOT NULL DEFAULT NEWID(),
        [UserId]           uniqueidentifier NOT NULL,
        [OriginalFileName] nvarchar(500)    NOT NULL,
        [StoredFileName]   nvarchar(500)    NOT NULL,
        [ContentType]      nvarchar(200)    NOT NULL,
        [FileSizeBytes]    bigint           NOT NULL DEFAULT 0,
        [CdnUrl]           nvarchar(1000)   NOT NULL,
        [StoragePath]      nvarchar(1000)   NOT NULL,
        [StorageProvider]  int              NOT NULL DEFAULT 1,
        [Status]           int              NOT NULL DEFAULT 1,
        [Description]      nvarchar(2000)   NULL,
        [Tags]             nvarchar(500)    NULL,
        [IsPublic]         bit              NOT NULL DEFAULT 1,
        [DownloadCount]    bigint           NOT NULL DEFAULT 0,
        [Checksum]         nvarchar(max)    NULL,
        [FolderPath]       nvarchar(max)    NULL,
        [CreatedAt]        datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]        datetime2        NULL,
        [IsDeleted]        bit              NOT NULL DEFAULT 0,
        [DeletedAt]        datetime2        NULL,
        CONSTRAINT [PK_CdnFiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CdnFiles_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CdnFiles_UserId] ON [CdnFiles] ([UserId]);
    CREATE UNIQUE INDEX [IX_CdnFiles_StoredFileName] ON [CdnFiles] ([StoredFileName]);
    PRINT 'CdnFiles created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[UserApiKeys]') AND type = 'U')
BEGIN
    CREATE TABLE [UserApiKeys] (
        [Id]           uniqueidentifier NOT NULL DEFAULT NEWID(),
        [UserId]       uniqueidentifier NOT NULL,
        [KeyName]      nvarchar(max)    NOT NULL,
        [KeyHash]      nvarchar(max)    NOT NULL,
        [KeyPrefix]    nvarchar(max)    NOT NULL,
        [IsActive]     bit              NOT NULL DEFAULT 1,
        [ExpiresAt]    datetime2        NULL,
        [LastUsedAt]   datetime2        NULL,
        [AllowedIps]   nvarchar(max)    NULL,
        [RequestCount] bigint           NOT NULL DEFAULT 0,
        [CreatedAt]    datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]    datetime2        NULL,
        [IsDeleted]    bit              NOT NULL DEFAULT 0,
        [DeletedAt]    datetime2        NULL,
        CONSTRAINT [PK_UserApiKeys] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserApiKeys_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_UserApiKeys_UserId] ON [UserApiKeys] ([UserId]);
    PRINT 'UserApiKeys created.';
END
GO

-- ============================================================
-- 3. TABLES THAT DEPEND ON CdnFiles
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[FileAccessLogs]') AND type = 'U')
BEGIN
    CREATE TABLE [FileAccessLogs] (
        [Id]         uniqueidentifier NOT NULL DEFAULT NEWID(),
        [FileId]     uniqueidentifier NOT NULL,
        [IpAddress]  nvarchar(max)    NULL,
        [UserAgent]  nvarchar(max)    NULL,
        [Referer]    nvarchar(max)    NULL,
        [AccessType] nvarchar(max)    NOT NULL DEFAULT 'download',
        [CreatedAt]  datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]  datetime2        NULL,
        [IsDeleted]  bit              NOT NULL DEFAULT 0,
        [DeletedAt]  datetime2        NULL,
        CONSTRAINT [PK_FileAccessLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FileAccessLogs_CdnFiles_FileId]
            FOREIGN KEY ([FileId]) REFERENCES [CdnFiles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FileAccessLogs_FileId] ON [FileAccessLogs] ([FileId]);
    PRINT 'FileAccessLogs created.';
END
GO

-- ============================================================
-- 4. SEED DATA
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [SystemSettings] WHERE [Key] = 'MaxFileSizeMB')
BEGIN
    INSERT INTO [SystemSettings] ([Id],[Key],[Value],[Description],[IsPublic],[CreatedAt],[IsDeleted])
    VALUES
    ('11111111-1111-1111-1111-111111111111','MaxFileSizeMB','100','Maximum file size in MB',0,GETUTCDATE(),0),
    ('22222222-2222-2222-2222-222222222222','AllowedFileTypes','image/jpeg,image/png,image/gif,image/webp,video/mp4,application/pdf,text/plain,application/zip','Comma-separated allowed MIME types',0,GETUTCDATE(),0),
    ('33333333-3333-3333-3333-333333333333','DefaultStorageQuotaGB','1','Default storage quota for new users (GB)',0,GETUTCDATE(),0),
    ('44444444-4444-4444-4444-444444444444','MaintenanceMode','false','Maintenance mode flag',1,GETUTCDATE(),0);
    PRINT 'SystemSettings seeded.';
END
GO

-- ============================================================
-- 5. Register migration so EF thinks it ran
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20240101000000_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion])
    VALUES ('20240101000000_InitialCreate','8.0.8');
    PRINT 'Migration registered.';
END
GO

PRINT '============================================================';
PRINT 'CDN API database setup complete!';
PRINT '============================================================';
