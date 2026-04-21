using ManuTrackAPI.Data;
using ManuTrackAPI.Models;
using ManuTrackAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ManuTrackAPI.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    // ── REGISTER (first time, no actor) ───────────────────────
    public async Task<(UserResponse? user, string? error)> RegisterAsync(CreateUserRequest req)
    {

        if (await db.Users.AnyAsync(u => u.Role == "Admin"))
            return (null, "Registration is closed. Contact your Admin.");



        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return (null, "Email already exists.");

        string[] validRoles = ["Admin", "Planner", "Operator", "Inspector", "InventoryManager", "ComplianceOfficer"];
        if (!validRoles.Contains(req.Role))
            return (null, $"Invalid role. Valid: {string.Join(", ", validRoles)}");

        var user = new User
        {
            Name = req.Name,
            Role = req.Role,
            Email = req.Email,
            Phone = req.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Log using new user's own ID
        await WriteAuditAsync(user.UserID, "UserRegistered", $"Role:{user.Role}");
        return (MapToResponse(user), null);
    }

    // ── LOGIN ──────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            if (user != null)
                await WriteAuditAsync(user.UserID, "LoginFailed");
            return null;
        }

        await WriteAuditAsync(user.UserID, "Login");
        return new LoginResponse(GenerateToken(user), user.Role, user.Name);
    }

    // ── CREATE USER (Admin creates other actors) ───────────────
    public async Task<(UserResponse? user, string? error)> CreateUserAsync(CreateUserRequest req, int actorId)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return (null, "Email already exists.");

        string[] validRoles = ["Admin", "Planner", "Operator", "Inspector", "InventoryManager", "ComplianceOfficer"];
        if (!validRoles.Contains(req.Role))
            return (null, $"Invalid role. Valid: {string.Join(", ", validRoles)}");

        var user = new User
        {
            Name = req.Name,
            Role = req.Role,
            Email = req.Email,
            Phone = req.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Use actorId (Admin's ID) for audit
        await WriteAuditAsync(actorId, "UserCreated", $"NewUserID:{user.UserID} Role:{user.Role}");
        return (MapToResponse(user), null);
    }

    // ── GET ALL USERS ──────────────────────────────────────────
    public async Task<List<UserResponse>> GetAllUsersAsync() =>
        await db.Users.Select(u => MapToResponse(u)).ToListAsync();

    // ── GET USER BY ID ─────────────────────────────────────────
    public async Task<UserResponse?> GetUserByIdAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        return user == null ? null : MapToResponse(user);
    }

    // ── UPDATE USER ────────────────────────────────────────────
    public async Task<(UserResponse? user, string? error)> UpdateUserAsync(int id, UpdateUserRequest req, int actorId)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return (null, "User not found.");

        user.Name = req.Name;
        user.Phone = req.Phone;
        user.Role = req.Role;
        user.IsActive = req.IsActive;

        await db.SaveChangesAsync();
        await WriteAuditAsync(actorId, "UserUpdated", $"UserID:{id}");
        return (MapToResponse(user), null);
    }

    // ── CHANGE PASSWORD ────────────────────────────────────────
    public async Task<(bool success, string? error)> ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(userId);
        if (user == null) return (false, "User not found.");
        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return (false, "Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        await WriteAuditAsync(userId, "PasswordChanged");
        return (true, null);
    }

    // ── DEACTIVATE USER ────────────────────────────────────────
    public async Task<bool> DeactivateUserAsync(int id, int actorId)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        await db.SaveChangesAsync();
        await WriteAuditAsync(actorId, "UserDeactivated", $"UserID:{id}");
        return true;
    }

    // ── AUDIT LOGS ─────────────────────────────────────────────
    public async Task<List<AuditLog>> GetAuditLogsAsync(int? userId = null)
    {
        var query = db.AuditLogs.Include(a => a.User).AsQueryable();
        if (userId.HasValue)
            query = query.Where(a => a.UserID == userId.Value);
        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    // ── GET OPERATORS ──────────────────────────────────────────
    public async Task<List<UserResponse>> GetOperatorsAsync() =>
        await db.Users
            .Where(u => u.Role == "Operator" && u.IsActive)
            .Select(u => MapToResponse(u))
            .ToListAsync();

    // ── WRITE AUDIT (insert only, never update/delete) ─────────
    public async Task WriteAuditAsync(int userId, string action, string? details = null)
    {
        // Skip if no valid user (safety check)
        if (userId == 0) return;

        db.AuditLogs.Add(new AuditLog
        {
            UserID = userId,
            Action = action,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // ── HELPERS ────────────────────────────────────────────────
    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        ];
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserResponse MapToResponse(User u) =>
        new(u.UserID, u.Name, u.Role, u.Email, u.Phone, u.IsActive, u.CreatedAt);
}