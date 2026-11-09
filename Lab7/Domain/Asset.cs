namespace Lab7.Domain;

public class Asset
{
    public Guid Id { get; set; }

    public string InventoryNumber { get; set; } = "";
    public string Model { get; set; } = "";
    public string Category { get; set; } = "";
    public bool Assigned { get; set; } = false;

    public ICollection<Assignment> Assignments { get; set; } = [];
}