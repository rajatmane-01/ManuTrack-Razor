using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ManuTrackAPI.Services;
using ManuTrackAPI.Models;
using ManuTrackAPI.Models.DTOs;

namespace ManuTrackAPI.Pages.Users;

public class IndexModel : PageModel
{
    private readonly AuthService _auth;

    public IndexModel(AuthService auth)
    {
        _auth = auth;
    }

    public List<UserResponse> Users { get; set; } = new();
    public string SuccessMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (HttpContext.Session.GetString("token") == null)
            return RedirectToPage("/Auth/Login");

        if (HttpContext.Session.GetString("role") != "Admin")
            return RedirectToPage("/Dashboard/Index");

        Users = await _auth.GetAllUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string Name, string Email, string Phone,
        string Role, string Password)
    {
        var (user, error) = await _auth.CreateUserAsync(
            new CreateUserRequest(Name, Role, Email, Phone, Password),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = $"User {Name} created successfully!";

        Users = await _auth.GetAllUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int userId)
    {
        await _auth.DeactivateUserAsync(userId, GetActorId());
        SuccessMessage = "User deactivated.";
        Users = await _auth.GetAllUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostActivateAsync(int userId)
    {
        var user = await _auth.GetUserByIdAsync(userId);
        if (user == null)
        {
            ErrorMessage = "User not found.";
            Users = await _auth.GetAllUsersAsync();
            return Page();
        }

        await _auth.UpdateUserAsync(userId, new UpdateUserRequest(
            user.Name,
            user.Role,
            user.Phone,
            true   // ← IsActive = true
        ), GetActorId());

        SuccessMessage = $"User {user.Name} activated successfully!";
        Users = await _auth.GetAllUsersAsync();
        return Page();
    }

    private int GetActorId()
    {
        var token = HttpContext.Session.GetString("token");
        if (token == null) return 0;

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var id = jwt.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            ?.Value;
        return int.TryParse(id, out var result) ? result : 0;
    }
}