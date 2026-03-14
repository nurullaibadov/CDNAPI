using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CdnApi.Application.DTOs.CDN;
using CdnApi.Application.Interfaces.Services;

namespace CdnApi.API.Controllers;

/// <summary>
/// CDN file upload, management, and delivery.
/// </summary>
[Tags("Files")]
[Authorize]
public class FilesController : BaseApiController
{
    private readonly ICdnFileService _fileService;

    public FilesController(ICdnFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>Upload a single file to the CDN.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(201)]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequestDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return Fail("No file provided.");

        try
        {
            var result = await _fileService.UploadFileAsync(CurrentUserId, dto);
            return Created(result, "File uploaded successfully.");
        }
        catch (InvalidOperationException ex) { return Fail(ex.Message); }
    }

    /// <summary>Upload multiple files at once (max 10 files).</summary>
    [HttpPost("upload/multiple")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMultipleFiles([FromForm] UploadMultipleFilesRequestDto dto)
    {
        if (dto.Files == null || !dto.Files.Any())
            return Fail("No files provided.");
        if (dto.Files.Count > 10)
            return Fail("Maximum 10 files per request.");

        try
        {
            var results = await _fileService.UploadMultipleFilesAsync(CurrentUserId, dto.Files, dto.FolderPath);
            return Created(results, $"{dto.Files.Count} file(s) uploaded successfully.");
        }
        catch (InvalidOperationException ex) { return Fail(ex.Message); }
    }

    /// <summary>Get a paginated list of the current user's files.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyFiles([FromQuery] PaginationDto pagination)
    {
        var result = await _fileService.GetUserFilesAsync(CurrentUserId, pagination);
        return Success(result);
    }

    /// <summary>Get file metadata by ID.</summary>
    [HttpGet("{fileId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFile(Guid fileId)
    {
        var userId = User.Identity?.IsAuthenticated == true ? CurrentUserId : (Guid?)null;
        var file = await _fileService.GetFileByIdAsync(fileId, userId);
        if (file == null) return NotFound(new ApiResponse { Success = false, Message = "File not found or access denied." });
        return Success(file);
    }

    /// <summary>Update file metadata (description, tags, visibility, folder).</summary>
    [HttpPut("{fileId:guid}")]
    public async Task<IActionResult> UpdateFile(Guid fileId, [FromBody] UpdateFileRequestDto dto)
    {
        try
        {
            var result = await _fileService.UpdateFileAsync(fileId, CurrentUserId, dto);
            return Success(result, "File updated successfully.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "File not found." }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>Delete a file.</summary>
    [HttpDelete("{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId)
    {
        try
        {
            await _fileService.DeleteFileAsync(fileId, CurrentUserId);
            return Success(null, "File deleted successfully.");
        }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse { Success = false, Message = "File not found." }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
