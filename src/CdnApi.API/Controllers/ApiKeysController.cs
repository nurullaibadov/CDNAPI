using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.API.Controllers;

/// <summary>
/// API key management for programmatic access.
/// </summary>
[Tags("API Keys")]
[Authorize]
public class ApiKeysController : BaseApiController
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// Create a new API key. 
    /// IMPORTANT: The full key is only returned once — save it securely.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse { Success = false, Message = "Validation failed.", Errors = ModelState });

        var result = await _apiKeyService.CreateApiKeyAsync(CurrentUserId, dto);
        return Created(result, "API key created. Save the key now — it will not be shown again.");
    }

    /// <summary>List all API keys for the current user (plaintext key is not returned).</summary>
    [HttpGet]
    public async Task<IActionResult> GetApiKeys()
    {
        var keys = await _apiKeyService.GetUserApiKeysAsync(CurrentUserId);
        return Success(keys);
    }

    /// <summary>Revoke (deactivate) an API key by its ID.</summary>
    [HttpDelete("{keyId:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid keyId)
    {
        try
        {
            await _apiKeyService.RevokeApiKeyAsync(CurrentUserId, keyId);
            return Success(null, "API key revoked.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "API key not found." }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
