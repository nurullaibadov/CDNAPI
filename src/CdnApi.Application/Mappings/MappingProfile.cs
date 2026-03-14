using AutoMapper;
using CdnApi.Application.DTOs.Auth;
using CdnApi.Application.DTOs.User;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.DTOs.Admin;
using CdnApi.Domain.Entities;

namespace CdnApi.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserTokenDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<User, UserProfileDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<User, AdminUserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.TotalFiles, o => o.MapFrom(s => s.CdnFiles.Count(f => !f.IsDeleted)));

        // CdnFile mappings
        CreateMap<CdnFile, CdnFileResponseDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UploaderName, o => o.MapFrom(s => s.User != null ? s.User.FullName : null))
            .ForMember(d => d.FileSizeFormatted, o => o.MapFrom(s => FormatFileSize(s.FileSizeBytes)));

        // AuditLog mappings
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.FullName : null))
            .ForMember(d => d.UserEmail, o => o.MapFrom(s => s.User != null ? s.User.Email : null));

        // ApiKey mappings
        CreateMap<UserApiKey, ApiKeyResponseDto>();

        // SystemSetting mappings
        CreateMap<SystemSetting, SystemSettingDto>();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;
        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        return $"{size:0.##} {suffixes[suffixIndex]}";
    }
}
