namespace lab3;

public class TaskManager
{
    private Repository<Task> taskRepository = new();
    private Repository<Project> projectRepository = new();
    private int nextTaskId = 1;
    private int nextProjectId = 1;

    public void AddTask(string title, string description, Priority priority, Status status)
    {
        var newTask = new Task(nextTaskId, title)
        {
            Description = description,
            Priority = priority,
            Status = status
        };

        taskRepository.Add(newTask);
        nextTaskId++;
    }

    public void UpdateTask(int index, string title, string description, Priority priority, Status status)
    {
        var task = taskRepository.GetAll()[index];

        task.Title = title;
        task.Description = description;
        task.Priority = priority;
        task.Status = status;

        taskRepository.Update(index, task);
    }

    public void DeleteTask(int index) => taskRepository.Remove(index);
    
    public List<Task> GetTasks() => taskRepository.GetAll();

    public void AddProject(string name)
    {
        var newProject = new Project(nextProjectId, name);
        projectRepository.Add(newProject);
        nextProjectId++;
    }

    public void DeleteProject(int index) => projectRepository.Remove(index);
    
    public List<Project> GetProjects() => projectRepository.GetAll();
}