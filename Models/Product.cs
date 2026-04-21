namespace ManuTrackAPI.Models;

public class Product
{
    public int ProductID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BOM> BOMs { get; set; } = new List<BOM>();
}