namespace lab3;

public class Task
{
    public int Id { get; }
    public string Title { get; set; }
    public string Description { get; set; } = "";
    public Status Status { get; set; } = Status.Todo;
    public Priority Priority { get; set; } = Priority.Medium;

    public Task(int id, string title)
    {
        Id = id;
        Title = title;
    }
}