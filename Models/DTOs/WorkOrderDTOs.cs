namespace ManuTrackAPI.Models.DTOs;

// WorkOrder
public record CreateWorkOrderRequest(
    int ProductID,
    int Quantity,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateWorkOrderStatusRequest(string Status);

public record WorkOrderResponse(
    int WorkOrderID,
    int ProductID,
    string ProductName,
    int Quantity,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    DateTime CreatedAt
);

// WorkOrderTask
public record CreateTaskRequest(
    string Description,
    int AssignedTo
);

public record UpdateTaskStatusRequest(string Status);

public record TaskResponse(
    int TaskID,
    int WorkOrderID,
    string Description,
    int AssignedTo,
    string AssignedUserName,
    string Status
);