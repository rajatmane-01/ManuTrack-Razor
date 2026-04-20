using ManuTrackAPI.Models.DTOs;
using ManuTrackAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManuTrackAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workorders")]
public class WorkOrderController(WorkOrderService workOrders) : ControllerBase
{
    private int ActorId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── WORK ORDER endpoints ───────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await workOrders.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var wo = await workOrders.GetByIdAsync(id);
        return wo == null ? NotFound() : Ok(wo);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest req)
    {
        var (wo, error) = await workOrders.CreateAsync(req, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = wo!.WorkOrderID }, wo);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Planner,Operator")]
    public async Task<IActionResult> UpdateStatus(int id,
        [FromBody] UpdateWorkOrderStatusRequest req)
    {
        var (wo, error) = await workOrders.UpdateStatusAsync(id, req.Status, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(wo);
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> Cancel(int id)
    {
        var (success, error) = await workOrders.CancelAsync(id, ActorId);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Work order cancelled." });
    }

    // ── TASK endpoints ─────────────────────────────────────────

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> GetTasks(int id) =>
        Ok(await workOrders.GetTasksByWorkOrderAsync(id));

    [HttpPost("{id}/tasks")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> CreateTask(int id,
        [FromBody] CreateTaskRequest req)
    {
        var (task, error) = await workOrders.CreateTaskAsync(id, req, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetTasks), new { id }, task);
    }

    [HttpPatch("tasks/{taskId}/status")]
    [Authorize(Roles = "Admin,Planner,Operator")]
    public async Task<IActionResult> UpdateTaskStatus(int taskId,
        [FromBody] UpdateTaskStatusRequest req)
    {
        var (task, error) = await workOrders.UpdateTaskStatusAsync(taskId, req.Status, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(task);
    }
}