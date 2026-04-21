using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ManuTrackAPI.Services;
using ManuTrackAPI.Models.DTOs;

namespace ManuTrackAPI.Pages.Products;

public class IndexModel : PageModel
{
    private readonly ProductService _products;

    public IndexModel(ProductService products)
    {
        _products = products;
    }

    public List<ProductResponse> Products { get; set; } = new();
    public string Role { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (HttpContext.Session.GetString("token") == null)
            return RedirectToPage("/Auth/Login");

        Role = HttpContext.Session.GetString("role") ?? string.Empty;

        if (Role != "Admin" && Role != "Planner")
            return RedirectToPage("/Dashboard/Index");

        Products = await _products.GetAllProductsAsync();
        return Page();
    }

    // ── CREATE PRODUCT ─────────────────────────────────────
    public async Task<IActionResult> OnPostCreateProductAsync(
        string Name, string Category, string Version, string Status)
    {
        Role = HttpContext.Session.GetString("role") ?? string.Empty;

        var (product, error) = await _products.CreateProductAsync(
            new CreateProductRequest(Name, Category, Version, Status),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = $"Product '{Name}' created successfully!";

        Products = await _products.GetAllProductsAsync();
        return Page();
    }

    // ── UPDATE PRODUCT ─────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateProductAsync(
        int ProductID, string Name, string Category, string Version, string Status)
    {
        Role = HttpContext.Session.GetString("role") ?? string.Empty;

        var (product, error) = await _products.UpdateProductAsync(
            ProductID,
            new UpdateProductRequest(Name, Category, Version, Status),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = $"Product '{Name}' updated successfully!";

        Products = await _products.GetAllProductsAsync();
        return Page();
    }

    // ── DELETE PRODUCT ─────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteProductAsync(int productId)
    {
        Role = HttpContext.Session.GetString("role") ?? string.Empty;

        var (success, error) = await _products.DeleteProductAsync(productId, GetActorId());

        if (!success)
            ErrorMessage = error ?? "Could not delete product.";
        else
            SuccessMessage = "Product deleted successfully.";

        Products = await _products.GetAllProductsAsync();
        return Page();
    }

    // ── CREATE BOM ─────────────────────────────────────────
    public async Task<IActionResult> OnPostCreateBOMAsync(
        int ProductID, string ComponentName, string Unit,
        decimal Quantity, string Version, string Status)
    {
        Role = HttpContext.Session.GetString("role") ?? string.Empty;

        var (bom, error) = await _products.CreateBOMAsync(
            ProductID,
            new CreateBOMRequest(ComponentName, Unit, Quantity, Version, Status),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = $"BOM entry for '{ComponentName}' added successfully!";

        Products = await _products.GetAllProductsAsync();
        return Page();
    }

    // ── UPDATE BOM ─────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateBOMAsync(
        int BOMID, string ComponentName, string Unit,
        decimal Quantity, string Version, string Status)
    {
        Role = HttpContext.Session.GetString("role") ?? string.Empty;

        var (bom, error) = await _products.UpdateBOMAsync(
            BOMID,
            new CreateBOMRequest(ComponentName, Unit, Quantity, Version, Status),
            GetActorId());

        if (error != null)
            ErrorMessage = error;
        else
            SuccessMessage = "BOM entry updated successfully!";

        Products = await _products.GetAllProductsAsync();
        return Page();
    }

    // ── HELPER ─────────────────────────────────────────────
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
