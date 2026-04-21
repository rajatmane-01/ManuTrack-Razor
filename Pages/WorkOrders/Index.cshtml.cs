using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ManuTrackAPI.Services;
using ManuTrackAPI.Models.DTOs;

namespace ManuTrackAPI.Pages.WorkOrders;

public class IndexModel : PageModel
{
    private readonly WorkOrderService _workOrders;
    private readonly ProductService _products;
    private readonly AuthService _auth;

    public IndexModel(WorkOrderService workOrders,
        ProductService products, AuthService auth)
    {
        _workOrders = workOrders;
        _products = products;
        _auth = auth;
    }

    public string Role { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public List<WorkOrderResponse> WorkOrders { get; set; } = new();
    public List<ProductResponse> Products { get; set; } = new();
    public List<UserResponse> Operators { get; set; } = new();
    public List<TaskResponse> Tasks { get; set; } = new();
    public WorkOrderResponse? SelectedWO { get; set; }

    public async Task<IActionResult> OnGetAsync(int? woId)
    {
        if (HttpContext.Session.GetString("token") == null)
            return RedirectToPage("/Auth/Login");

        Role = HttpContext.Session.GetString("role") ?? "";

        await LoadDataAsync();

        if (woId.HasValue)
        {
            SelectedWO = WorkOrders.FirstOrDefault(
                w => w.WorkOrderID == woId.Value);
            if (SelectedWO != null)
                Tasks = await _workOrders.GetTasksByWorkOrderAsync(
                    SelectedWO.WorkOrderID);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        int ProductID, int Quantity,
        DateTime StartDate, DateTime EndDate)
    {
        Role = HttpContext.Session.GetString("role") ?? "";

        var (wo, error) = await _workOrders.CreateAsync(
            new CreateWorkOrderRequest(ProductID, Quantity, StartDate, EndDate),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = $"Work Order #{wo!.WorkOrderID} created!";

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(
        int woId, string status)
    {
        Role = HttpContext.Session.GetString("role") ?? "";

        var (wo, error) = await _workOrders.UpdateStatusAsync(
            woId, status, GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
        {
            SuccessMessage = $"Status updated to {status}";
            SelectedWO = wo;
            Tasks = await _workOrders.GetTasksByWorkOrderAsync(woId);
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int woId)
    {
        Role = HttpContext.Session.GetString("role") ?? "";

        var (success, error) = await _workOrders.CancelAsync(
            woId, GetActorId());

        if (!success)
            ErrorMessage = error ?? "Failed to cancel.";
        else
            SuccessMessage = "Work order cancelled.";

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddTaskAsync(
        int woId, string Description, int AssignedTo)
    {
        Role = HttpContext.Session.GetString("role") ?? "";

        var (task, error) = await _workOrders.CreateTaskAsync(
            woId,
            new CreateTaskRequest(Description, AssignedTo),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = "Task added successfully!";

        await LoadDataAsync();
        SelectedWO = WorkOrders.FirstOrDefault(w => w.WorkOrderID == woId);
        if (SelectedWO != null)
            Tasks = await _workOrders.GetTasksByWorkOrderAsync(woId);

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateTaskStatusAsync(
        int taskId, int woId, string status)
    {
        Role = HttpContext.Session.GetString("role") ?? "";

        var (task, error) = await _workOrders.UpdateTaskStatusAsync(
            taskId, status, GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = $"Task marked as {status}";

        await LoadDataAsync();
        SelectedWO = WorkOrders.FirstOrDefault(w => w.WorkOrderID == woId);
        if (SelectedWO != null)
            Tasks = await _workOrders.GetTasksByWorkOrderAsync(woId);

        return Page();
    }

    private async Task LoadDataAsync()
    {
        WorkOrders = await _workOrders.GetAllAsync();
        Products = await _products.GetAllProductsAsync();
        Operators = await _auth.GetOperatorsAsync();
    }

    private int GetActorId()
    {
        var token = HttpContext.Session.GetString("token");
        if (token == null) return 0;

        var handler = new System.IdentityModel.Tokens.Jwt
            .JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var id = jwt.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/" +
            "claims/nameidentifier")?.Value;
        return int.TryParse(id, out var result) ? result : 0;
    }
}