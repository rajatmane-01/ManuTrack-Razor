using ManuTrackAPI.Data;
using ManuTrackAPI.Models;
using ManuTrackAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ManuTrackAPI.Services;

public class WorkOrderService(AppDbContext db, AuthService auth)
{
    // ── WORK ORDER ─────────────────────────────────────────────

    public async Task<List<WorkOrderResponse>> GetAllAsync() =>
        await db.WorkOrders
            .Include(w => w.Product)
            .Select(w => MapWorkOrder(w))
            .ToListAsync();

    public async Task<WorkOrderResponse?> GetByIdAsync(int id)
    {
        var wo = await db.WorkOrders
            .Include(w => w.Product)
            .FirstOrDefaultAsync(w => w.WorkOrderID == id);
        return wo == null ? null : MapWorkOrder(wo);
    }

    public async Task<(WorkOrderResponse? wo, string? error)> CreateAsync(
        CreateWorkOrderRequest req, int actorId)
    {
        // Verify product exists
        var product = await db.Products.FindAsync(req.ProductID);
        if (product == null)
            return (null, $"Product ID {req.ProductID} not found.");

        if (req.EndDate <= req.StartDate)
            return (null, "End date must be after start date.");

        var wo = new WorkOrder
        {
            ProductID = req.ProductID,
            Quantity = req.Quantity,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Status = "Pending"
        };

        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "WorkOrderCreated",
            $"WorkOrderID:{wo.WorkOrderID} Product:{product.Name} Qty:{req.Quantity}");

        wo.Product = product;
        return (MapWorkOrder(wo), null);
    }

    public async Task<(WorkOrderResponse? wo, string? error)> UpdateStatusAsync(
        int id, string status, int actorId)
    {
        var wo = await db.WorkOrders
            .Include(w => w.Product)
            .FirstOrDefaultAsync(w => w.WorkOrderID == id);
        if (wo == null) return (null, "Work order not found.");

        string[] validStatuses = ["Pending", "InProgress", "Completed", "Cancelled"];
        if (!validStatuses.Contains(status))
            return (null, "Invalid status. Valid: Pending, InProgress, Completed, Cancelled");

        // Cannot reopen a completed or cancelled order
        if (wo.Status == "Completed" || wo.Status == "Cancelled")
            return (null, $"Cannot update a {wo.Status} work order.");

        wo.Status = status;
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "WorkOrderStatusUpdated",
            $"WorkOrderID:{id} Status:{status}");

        return (MapWorkOrder(wo), null);
    }

    public async Task<(bool success, string? error)> CancelAsync(
        int id, int actorId)
    {
        var wo = await db.WorkOrders.FindAsync(id);
        if (wo == null) return (false, "Work order not found.");

        if (wo.Status == "Completed")
            return (false, "Cannot cancel a completed work order.");

        wo.Status = "Cancelled";
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "WorkOrderCancelled",
            $"WorkOrderID:{id}");

        return (true, null);
    }

    // ── TASKS ──────────────────────────────────────────────────

    public async Task<List<TaskResponse>> GetTasksByWorkOrderAsync(int workOrderId) =>
        await db.WorkOrderTasks
            .Include(t => t.AssignedUser)
            .Where(t => t.WorkOrderID == workOrderId)
            .Select(t => MapTask(t))
            .ToListAsync();

    public async Task<(TaskResponse? task, string? error)> CreateTaskAsync(
        int workOrderId, CreateTaskRequest req, int actorId)
    {
        var wo = await db.WorkOrders.FindAsync(workOrderId);
        if (wo == null) return (null, "Work order not found.");

        if (wo.Status == "Completed" || wo.Status == "Cancelled")
            return (null, $"Cannot add tasks to a {wo.Status} work order.");

        // Verify assigned user exists and is an Operator
        var user = await db.Users.FindAsync(req.AssignedTo);
        if (user == null)
            return (null, $"User ID {req.AssignedTo} not found.");
        if (user.Role != "Operator" && user.Role != "Admin")
            return (null, "Tasks can only be assigned to Operators.");

        var task = new WorkOrderTask
        {
            WorkOrderID = workOrderId,
            Description = req.Description,
            AssignedTo = req.AssignedTo,
            Status = "Pending"
        };

        db.WorkOrderTasks.Add(task);
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "TaskCreated",
            $"TaskID:{task.TaskID} WO:{workOrderId} AssignedTo:{user.Name}");

        task.AssignedUser = user;
        return (MapTask(task), null);
    }

    public async Task<(TaskResponse? task, string? error)> UpdateTaskStatusAsync(
        int taskId, string status, int actorId)
    {
        var task = await db.WorkOrderTasks
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.TaskID == taskId);
        if (task == null) return (null, "Task not found.");

        string[] validStatuses = ["Pending", "InProgress", "Done"];
        if (!validStatuses.Contains(status))
            return (null, "Invalid status. Valid: Pending, InProgress, Done");

        task.Status = status;
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "TaskStatusUpdated",
            $"TaskID:{taskId} Status:{status}");

        // Auto complete work order if all tasks are done
        await AutoCompleteWorkOrderAsync(task.WorkOrderID, actorId);

        return (MapTask(task), null);
    }

    // ── AUTO COMPLETE ──────────────────────────────────────────

    private async Task AutoCompleteWorkOrderAsync(int workOrderId, int actorId)
    {
        var allTasks = await db.WorkOrderTasks
            .Where(t => t.WorkOrderID == workOrderId)
            .ToListAsync();

        // Only auto complete if all tasks are done
        if (allTasks.Any() && allTasks.All(t => t.Status == "Done"))
        {
            var wo = await db.WorkOrders.FindAsync(workOrderId);
            if (wo != null && wo.Status != "Completed")
            {
                wo.Status = "Completed";
                await db.SaveChangesAsync();
                await auth.WriteAuditAsync(actorId, "WorkOrderAutoCompleted",
                    $"WorkOrderID:{workOrderId} — all tasks done");
            }
        }
    }

    // ── HELPERS ────────────────────────────────────────────────

    private static WorkOrderResponse MapWorkOrder(WorkOrder w) =>
        new(w.WorkOrderID, w.ProductID,
            w.Product?.Name ?? "Unknown",
            w.Quantity, w.StartDate, w.EndDate,
            w.Status, w.CreatedAt);

    private static TaskResponse MapTask(WorkOrderTask t) =>
        new(t.TaskID, t.WorkOrderID, t.Description,
            t.AssignedTo,
            t.AssignedUser?.Name ?? "Unknown",
            t.Status);
}