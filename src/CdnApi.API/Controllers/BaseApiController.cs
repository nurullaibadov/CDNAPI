using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CdnApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("uid")
                ?? User.FindFirstValue("sub");
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    protected string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "User";

    protected string CurrentUserIp =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    protected IActionResult Success(object? data = null, string message = "Success")
        => Ok(new ApiResponse { Success = true, Message = message, Data = data });

    protected IActionResult Created(object data, string message = "Created successfully")
        => StatusCode(201, new ApiResponse { Success = true, Message = message, Data = data });

    protected IActionResult Fail(string message, int statusCode = 400)
        => StatusCode(statusCode, new ApiResponse { Success = false, Message = message });
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public object? Errors { get; set; }
}
