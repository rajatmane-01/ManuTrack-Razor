namespace ManuTrackAPI.Models;

public class BOM
{
    public int BOMID { get; set; }
    public int ProductID { get; set; }
    public int ComponentID { get; set; }
    public decimal Quantity { get; set; }
    public string Version { get; set; } = "1.0";
    public string Status { get; set; } = "Active";

    public Product? Product { get; set; }
    public Component? Component { get; set; }  
}