using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ManuTrackAPI.Services;
using ManuTrackAPI.Models;
using ManuTrackAPI.Models.DTOs;

namespace ManuTrackAPI.Pages.Auth;

public class AuditLogsModel : PageModel
{
    private readonly AuthService _auth;

    public AuditLogsModel(AuthService auth)
    {
        _auth = auth;
    }

    public List<AuditLog> AuditLogs { get; set; } = new();
    public List<UserResponse> Users { get; set; } = new();
    public int? SelectedUserId { get; set; }
    public string SelectedAction { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(
        int? userId, string? action)
    {
        // Only Admin and ComplianceOfficer can view
        if (HttpContext.Session.GetString("token") == null)
            return RedirectToPage("/Auth/Login");

        var role = HttpContext.Session.GetString("role");
        if (role != "Admin" && role != "ComplianceOfficer")
            return RedirectToPage("/Dashboard/Index");

        SelectedUserId = userId;
        SelectedAction = action ?? string.Empty;

        // Load all users for filter dropdown
        Users = await _auth.GetAllUsersAsync();

        // Load audit logs
        var logs = await _auth.GetAuditLogsAsync(userId);

        // Filter by action if selected
        if (!string.IsNullOrEmpty(action))
            logs = logs.Where(l => l.Action == action).ToList();

        AuditLogs = logs;
        return Page();
    }
}