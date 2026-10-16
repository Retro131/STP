namespace lab3;

public class Task
{
    public int Id { get; }
    public int ProjectId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; } = "";
    public Status Status { get; set; } = Status.Todo;
    public Priority Priority { get; set; } = Priority.Medium;

    public Task(int id, string title, int projectId = 0)
    {
        ProjectId = projectId;
        Id = id;
        Title = title;
    }
}