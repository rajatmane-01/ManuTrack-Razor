using ManuTrackAPI.Models.DTOs;
using ManuTrackAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManuTrackAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public class ProductController(ProductService products) : ControllerBase
{
    private int ActorId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── PRODUCT endpoints ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await products.GetAllProductsAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await products.GetProductByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var (product, error) = await products.CreateProductAsync(req, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = product!.ProductID }, product);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest req)
    {
        var (product, error) = await products.UpdateProductAsync(id, req, ActorId);
        if (error != null) return NotFound(new { message = error });
        return Ok(product);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await products.DeleteProductAsync(id, ActorId);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Product deleted." });
    }

    // ── BOM endpoints ──────────────────────────────────────────

    [HttpGet("{id}/bom")]
    public async Task<IActionResult> GetBOMs(int id) =>
        Ok(await products.GetBOMsByProductAsync(id));

    [HttpPost("{id}/bom")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> CreateBOM(int id, [FromBody] CreateBOMRequest req)
    {
        var (bom, error) = await products.CreateBOMAsync(id, req, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetBOMs), new { id }, bom);
    }

    [HttpPut("bom/{bomId}")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> UpdateBOM(int bomId, [FromBody] CreateBOMRequest req)
    {
        var (bom, error) = await products.UpdateBOMAsync(bomId, req, ActorId);
        if (error != null) return NotFound(new { message = error });
        return Ok(bom);
    }

    [HttpPatch("bom/{bomId}/obsolete")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> ObsoleteBOM(int bomId)
    {
        var (success, error) = await products.ObsoleteBOMAsync(bomId, ActorId);
        if (!success) return NotFound(new { message = error });
        return Ok(new { message = "BOM marked as obsolete." });
    }


    [HttpGet("/api/v1/components")]
    public async Task<IActionResult> GetAllComponents() =>
        Ok(await products.GetAllComponentsAsync());

    [HttpPost("/api/v1/components")]
    [Authorize(Roles = "Admin,Planner")]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentRequest req)
    {
        var (component, error) = await products.CreateComponentAsync(req, ActorId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(component);
    }




}