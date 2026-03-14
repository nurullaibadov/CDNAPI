using System.Security.Cryptography;
using AutoMapper;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Application.Interfaces.Services;
using CdnApi.Domain.Entities;

namespace CdnApi.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ApiKeyService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiKeyResponseDto> CreateApiKeyAsync(Guid userId, CreateApiKeyRequestDto dto)
    {
        // Generate a random API key: prefix_randomhex
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var keyBody = Convert.ToHexString(randomBytes).ToLower();
        var prefix = keyBody[..8];
        var plainTextKey = $"cdnapi_{keyBody}";
        var keyHash = HashApiKey(plainTextKey);

        var apiKey = new UserApiKey
        {
            UserId = userId,
            KeyName = dto.KeyName,
            KeyHash = keyHash,
            KeyPrefix = prefix,
            IsActive = true,
            ExpiresAt = dto.ExpiresAt,
            AllowedIps = dto.AllowedIps
        };

        await _unitOfWork.ApiKeys.AddAsync(apiKey);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<ApiKeyResponseDto>(apiKey);
        result.PlainTextKey = plainTextKey; // Only returned once
        return result;
    }

    public async Task<IEnumerable<ApiKeyResponseDto>> GetUserApiKeysAsync(Guid userId)
    {
        var keys = await _unitOfWork.ApiKeys.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<ApiKeyResponseDto>>(keys);
    }

    public async Task RevokeApiKeyAsync(Guid userId, Guid keyId)
    {
        var key = await _unitOfWork.ApiKeys.GetByIdAsync(keyId)
            ?? throw new KeyNotFoundException("API key not found.");
        if (key.UserId != userId) throw new UnauthorizedAccessException("Access denied.");
        key.IsActive = false;
        await _unitOfWork.ApiKeys.UpdateAsync(key);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Domain.Entities.User?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var keyHash = HashApiKey(apiKey);
        var key = await _unitOfWork.ApiKeys.GetByKeyHashAsync(keyHash);
        if (key == null || !key.IsActive) return null;
        if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow) return null;

        key.LastUsedAt = DateTime.UtcNow;
        key.RequestCount++;
        await _unitOfWork.ApiKeys.UpdateAsync(key);
        await _unitOfWork.SaveChangesAsync();
        return key.User;
    }

    private static string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
