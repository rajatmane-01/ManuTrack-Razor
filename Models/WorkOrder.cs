namespace ManuTrackAPI.Models;

public class WorkOrder
{
    public int WorkOrderID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Pending , InProgress , Completed , Cancelled
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Product? Product { get; set; }
    public ICollection<WorkOrderTask> Tasks { get; set; } = new List<WorkOrderTask>();
}