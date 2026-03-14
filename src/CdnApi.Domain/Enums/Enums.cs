namespace CdnApi.Domain.Enums;

public enum UserRole
{
    User = 1,
    Admin = 2,
    SuperAdmin = 3
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Banned = 3,
    PendingVerification = 4
}

public enum CdnFileStatus
{
    Active = 1,
    Deleted = 2,
    Processing = 3
}

public enum StorageProvider
{
    Local = 1,
    AzureBlob = 2,
    AWSS3 = 3
}
