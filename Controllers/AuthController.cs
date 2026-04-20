using System.Security.Claims;
using ManuTrackAPI.Models.DTOs;
using ManuTrackAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManuTrackAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    private int ActorId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Public — no token needed ───────────────────────────────

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest req)
    {
        var (user, error) = await auth.RegisterAsync(req);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetUser), new { id = user!.UserID }, user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await auth.LoginAsync(req);
        if (result == null)
            return Unauthorized(new { message = "Invalid email or password." });
        return Ok(result);
    }

    // ── Admin only ─────────────────────────────────────────────

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers() =>
        Ok(await auth.GetAllUsersAsync());

    [HttpGet("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await auth.GetUserByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        var (user, error) = await auth.CreateUserAsync(req, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetUser), new { id = user!.UserID }, user);
    }

    [HttpPut("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest req)
    {
        var (user, error) = await auth.UpdateUserAsync(id, req, ActorId);
        if (error != null) return NotFound(new { message = error });
        return Ok(user);
    }

    [HttpPatch("users/{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var success = await auth.DeactivateUserAsync(id, ActorId);
        return success ? Ok(new { message = "User deactivated." }) : NotFound();
    }

    // ── Any logged-in user ─────────────────────────────────────

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var (success, error) = await auth.ChangePasswordAsync(ActorId, req);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Password changed successfully." });
    }

    // ── Admin + Compliance Officer ─────────────────────────────

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin,ComplianceOfficer")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int? userId = null) =>
        Ok(await auth.GetAuditLogsAsync(userId));
}