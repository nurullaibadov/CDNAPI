using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.API.Controllers;

/// <summary>
/// Public CDN file delivery endpoint.
/// </summary>
[Tags("CDN Delivery")]
[AllowAnonymous]
[Route("cdn")]
[ApiController]
public class CdnController : ControllerBase
{
    private readonly ICdnFileService _fileService;

    public CdnController(ICdnFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>Serve a CDN file by its stored path. Supports range requests.</summary>
    [HttpGet("{**path}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> ServeFile(string path)
    {
        try
        {
            var storedFileName = Path.GetFileName(path);
            var fileInfo = await _fileService.GetFileByIdAsync(Guid.Empty); // just to look up

            // Look up by stored filename
            var bytes = await _fileService.ServeFileAsync(storedFileName);

            // Track access asynchronously
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();
            var referer = Request.Headers.Referer.ToString();

            // Determine content type
            var contentType = GetContentType(storedFileName);

            Response.Headers["X-CDN-Served-By"] = "CdnApi";
            Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            Response.Headers["Cache-Control"] = "public, max-age=86400";

            return File(bytes, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { success = false, message = "File not found." });
        }
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".css" => "text/css",
            ".js" => "application/javascript",
            _ => "application/octet-stream"
        };
    }
}
