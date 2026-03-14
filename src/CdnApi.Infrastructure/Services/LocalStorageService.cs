using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _storageRoot;
    private readonly string _baseUrl;

    public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _storageRoot = configuration["StorageSettings:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";

        if (!Directory.Exists(_storageRoot))
            Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderPath)
    {
        var sanitizedFolder = SanitizePath(folderPath);
        var fullFolderPath = Path.Combine(_storageRoot, sanitizedFolder);

        if (!Directory.Exists(fullFolderPath))
            Directory.CreateDirectory(fullFolderPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(fullFolderPath, storedFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        _logger.LogInformation("File saved: {Path}", fullPath);
        return Path.Combine(sanitizedFolder, storedFileName).Replace("\\", "/");
    }

    public Task<bool> DeleteFileAsync(string storagePath)
    {
        try
        {
            var fullPath = Path.Combine(_storageRoot, storagePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", storagePath);
            return Task.FromResult(false);
        }
    }

    public async Task<byte[]> GetFileAsync(string storagePath)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {storagePath}");
        return await File.ReadAllBytesAsync(fullPath);
    }

    public bool FileExists(string storagePath)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        return File.Exists(fullPath);
    }

    public string GetPublicUrl(string storagePath)
        => $"{_baseUrl}/cdn/{storagePath.Replace("\\", "/")}";

    private static string SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "general";
        var invalid = Path.GetInvalidPathChars();
        return string.Concat(path.Split(invalid)).Trim('/').Replace("..", "");
    }
}
