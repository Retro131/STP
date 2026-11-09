namespace Lab7.Domain;

public class Assignment
{
    public Guid Id { get; set; }

    public Guid AssetId { get; set; }
    public Guid EmployeeId { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }

    public Asset Asset { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}