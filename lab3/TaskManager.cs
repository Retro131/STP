namespace lab3;

public class TaskManager
{
    private Repository<Task> taskRepository = new();
    private Repository<Project> projectRepository = new();
    private int nextTaskId = 1;
    private int nextProjectId = 1;

    public void AddTask(string title, string description, Priority priority, Status status, int projectId)
    {
        var newTask = new Task(nextTaskId, title)
        {
            Description = description,
            Priority = priority,
            Status = status,
            ProjectId = projectId
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

    public bool MoveTaskToProject(int taskIndex, int projectId)
    {
        var tasks = taskRepository.GetAll();

        if (taskIndex < 0 || taskIndex >= tasks.Count)
            return false;

        if (projectId != 0 && projectRepository.GetAll().All(p => p.Id != projectId))
            return false;

        var task = tasks[taskIndex];

        task.ProjectId = projectId;
        taskRepository.Update(taskIndex, task);

        return true;    }
    
    public int CountTasksInProject(int projectId) => 
        taskRepository.
            GetAll().
            Count(t => t.ProjectId == projectId);
    
    public int CountTasksInProjectByStatus(int projectId, Status status) => 
        taskRepository.
            GetAll().
            Where(t => t.ProjectId == projectId).
            Count(t => t.Status == status);

    public double CompletionPercent(int projectId)
    {
        var allTasks = CountTasksInProject(projectId);
        if (allTasks == 0)
        {
            return 0.0;
        }
        var completedTasks = CountTasksInProjectByStatus(projectId, Status.Done);
        return  100.0 * completedTasks / allTasks;
    }
    
    public List<Task> GetTasks() => taskRepository.GetAll();

    public void AddProject(string name)
    {
        var newProject = new Project(nextProjectId, name);
        projectRepository.Add(newProject);
        nextProjectId++;
    }

    public void DeleteProject(int index)
    {
        var projectId = projectRepository.GetAll()[index].Id;
        var tasks = taskRepository.GetAll();

        for (var i = tasks.Count - 1; i >= 0; i--)
        {
            if (tasks[i].ProjectId == projectId)
            {
                taskRepository.Remove(i);
            }
        }

        projectRepository.Remove(index);
    }
    
    public List<Project> GetProjects() => projectRepository.GetAll();
}