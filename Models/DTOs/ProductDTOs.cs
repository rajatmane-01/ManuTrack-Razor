namespace ManuTrackAPI.Models.DTOs;

// ── Product ────────────────────────────────────────────────
public record CreateProductRequest(
    string Name,
    string Category,
    string Version,
    string Status
);

public record UpdateProductRequest(
    string Name,
    string Category,
    string Version,
    string Status
);

public record ProductResponse(
    int ProductID,
    string Name,
    string Category,
    string Version,
    string Status,
    DateTime CreatedAt
);

// ── BOM ────────────────────────────────────────────────────
public record CreateBOMRequest(
    string ComponentName,
    string Unit,
    decimal Quantity,
    string Version,
    string Status
);

public record BOMResponse(
    int BOMID,
    int ProductID,
    int ComponentID,
    string ComponentName,
    string Unit,
    decimal Quantity,
    string Version,
    string Status
);

// ── Component ──────────────────────────────────────────────
public record CreateComponentRequest(
    string Name,
    string Unit,
    string Description
);

public record ComponentResponse(
    int ComponentID,
    string Name,
    string Unit,
    string Description,
    DateTime CreatedAt
);