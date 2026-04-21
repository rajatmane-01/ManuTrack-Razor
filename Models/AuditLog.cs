namespace ManuTrackAPI.Models;

public class AuditLog
{
    public int AuditLogID { get; set; }
    public int UserID { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public User? User { get; set; }
}