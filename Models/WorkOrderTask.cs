namespace ManuTrackAPI.Models;

public class WorkOrderTask
{
    public int TaskID { get; set; }
    public int WorkOrderID { get; set; }
    public string Description { get; set; } = string.Empty;
    public int AssignedTo { get; set; } 
    public string Status { get; set; } = "Pending";
    

    public WorkOrder? WorkOrder { get; set; }
    public User? AssignedUser { get; set; }
}