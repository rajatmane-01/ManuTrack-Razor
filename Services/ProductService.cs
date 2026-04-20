using System.ComponentModel;
using ManuTrackAPI.Data;
using ManuTrackAPI.Models;
using ManuTrackAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

using Component = ManuTrackAPI.Models.Component;


namespace ManuTrackAPI.Services;

public class ProductService(AppDbContext db, AuthService auth)
{
    // ── COMPONENT ──────────────────────────────────────────────

    public async Task<List<ComponentResponse>> GetAllComponentsAsync() =>
        await db.Components
            .Select(c => MapComponent(c))
            .ToListAsync();

    public async Task<(ComponentResponse? component, string? error)> CreateComponentAsync(
        CreateComponentRequest req, int actorId)
    {
        // check if component already exists
        if (await db.Components.AnyAsync(c => c.Name.ToLower() == req.Name.ToLower()))
            return (null, "Component with this name already exists.");

        var component = new Component
        {
            Name = req.Name,
            Unit = req.Unit,
            Description = req.Description
        };

        db.Components.Add(component);
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "ComponentCreated",
            $"ComponentID:{component.ComponentID} Name:{component.Name}");

        return (MapComponent(component), null);
    }

    // ── PRODUCT ────────────────────────────────────────────────

    public async Task<List<ProductResponse>> GetAllProductsAsync() =>
        await db.Products
            .Select(p => MapProduct(p))
            .ToListAsync();

    public async Task<ProductResponse?> GetProductByIdAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        return product == null ? null : MapProduct(product);
    }

    public async Task<(ProductResponse? product, string? error)> CreateProductAsync(
        CreateProductRequest req, int actorId)
    {
        string[] validStatuses = ["Draft", "Active", "Discontinued"];
        if (!validStatuses.Contains(req.Status))
            return (null, "Invalid status. Valid: Draft, Active, Discontinued");

        var product = new Product
        {
            Name = req.Name,
            Category = req.Category,
            Version = req.Version,
            Status = req.Status
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "ProductCreated",
            $"ProductID:{product.ProductID} Name:{product.Name}");

        return (MapProduct(product), null);
    }

    public async Task<(ProductResponse? product, string? error)> UpdateProductAsync(
        int id, UpdateProductRequest req, int actorId)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return (null, "Product not found.");

        string[] validStatuses = ["Draft", "Active", "Discontinued"];
        if (!validStatuses.Contains(req.Status))
            return (null, "Invalid status. Valid: Draft, Active, Discontinued");

        product.Name = req.Name;
        product.Category = req.Category;
        product.Version = req.Version;
        product.Status = req.Status;

        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "ProductUpdated", $"ProductID:{id}");

        return (MapProduct(product), null);
    }

    public async Task<(bool success, string? error)> DeleteProductAsync(
        int id, int actorId)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return (false, "Product not found.");

        var hasActiveBOMs = await db.BOMs
            .AnyAsync(b => b.ProductID == id && b.Status == "Active");
        if (hasActiveBOMs)
            return (false, "Cannot delete product with active BOMs.");

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "ProductDeleted", $"ProductID:{id}");

        return (true, null);
    }

    // ── BOM ────────────────────────────────────────────────────

    public async Task<List<BOMResponse>> GetBOMsByProductAsync(int productId) =>
        await db.BOMs
            .Include(b => b.Component)  // ← include component name
            .Where(b => b.ProductID == productId)
            .Select(b => MapBOM(b))
            .ToListAsync();

    public async Task<(BOMResponse? bom, string? error)> CreateBOMAsync(
        int productId, CreateBOMRequest req, int actorId)
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null) return (null, "Product not found.");

        // Find component by name or create it automatically
        var component = await db.Components
            .FirstOrDefaultAsync(c => c.Name.ToLower() == req.ComponentName.ToLower());

        if (component == null)
        {
            component = new Component
            {
                Name = req.ComponentName,
                Unit = req.Unit,
                Description = string.Empty
            };
            db.Components.Add(component);
            await db.SaveChangesAsync();
        }

        string[] validStatuses = ["Active", "Obsolete"];
        if (!validStatuses.Contains(req.Status))
            return (null, "Invalid status. Valid: Active, Obsolete");

        var bom = new BOM
        {
            ProductID = productId,
            ComponentID = component.ComponentID,
            Quantity = req.Quantity,
            Version = req.Version,
            Status = req.Status
        };

        db.BOMs.Add(bom);
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "BOMCreated",
            $"BOMID:{bom.BOMID} Product:{product.Name} Component:{component.Name}");

        return (MapBOM(bom, component), null);
    }

    public async Task<(BOMResponse? bom, string? error)> UpdateBOMAsync(
        int bomId, CreateBOMRequest req, int actorId)
    {
        var bom = await db.BOMs
            .Include(b => b.Component)
            .FirstOrDefaultAsync(b => b.BOMID == bomId);
        if (bom == null) return (null, "BOM not found.");

        // Find or create component by name
        var component = await db.Components
            .FirstOrDefaultAsync(c => c.Name.ToLower() == req.ComponentName.ToLower());

        if (component == null)
        {
            component = new Component
            {
                Name = req.ComponentName,
                Unit = req.Unit,
                Description = string.Empty
            };
            db.Components.Add(component);
            await db.SaveChangesAsync();
        }

        bom.ComponentID = component.ComponentID;
        bom.Quantity = req.Quantity;
        bom.Version = req.Version;
        bom.Status = req.Status;

        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "BOMUpdated", $"BOMID:{bomId}");

        return (MapBOM(bom, component), null);
    }

    public async Task<(bool success, string? error)> ObsoleteBOMAsync(
        int bomId, int actorId)
    {
        var bom = await db.BOMs.FindAsync(bomId);
        if (bom == null) return (false, "BOM not found.");

        bom.Status = "Obsolete";
        await db.SaveChangesAsync();
        await auth.WriteAuditAsync(actorId, "BOMObsoleted", $"BOMID:{bomId}");

        return (true, null);
    }

    // ── HELPERS ────────────────────────────────────────────────

    private static ProductResponse MapProduct(Product p) =>
        new(p.ProductID, p.Name, p.Category, p.Version, p.Status, p.CreatedAt);

    private static ComponentResponse MapComponent(Component c) =>
        new(c.ComponentID, c.Name, c.Unit, c.Description, c.CreatedAt);

    private static BOMResponse MapBOM(BOM b, Component? c = null) =>
        new(b.BOMID, b.ProductID, b.ComponentID,
            c?.Name ?? b.Component?.Name ?? "Unknown",
            c?.Unit ?? b.Component?.Unit ?? "",
            b.Quantity, b.Version, b.Status);
}