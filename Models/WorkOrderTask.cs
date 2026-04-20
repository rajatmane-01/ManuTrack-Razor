namespace ManuTrackAPI.Models;

public class WorkOrderTask
{
    public int TaskID { get; set; }
    public int WorkOrderID { get; set; }
    public string Description { get; set; } = string.Empty;
    public int AssignedTo { get; set; } // UserID of Operator
    public string Status { get; set; } = "Pending";
    // Pending | InProgress | Done

    public WorkOrder? WorkOrder { get; set; }
    public User? AssignedUser { get; set; }
}