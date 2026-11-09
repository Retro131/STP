namespace Lab7.Domain;

public class Employee
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Active { get; set; } = true;

    public ICollection<Assignment> Assignments { get; set; } = [];
}