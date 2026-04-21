namespace ManuTrackAPI.Models;

public class Component
{
    public int ComponentID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BOM> BOMs { get; set; } = new List<BOM>();
}