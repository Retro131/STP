using FluentAssertions;
using lab3;

namespace lab3Tests;

[TestFixture]
public class TaskManagerTests
{
    [Test]
    public void TaskManager_AddUpdateDeleteTask()
    {
        var manager = new TaskManager();

        manager.GetTasks().Should().BeEmpty();

        manager.AddTask(
            "Task1", "Description1",
            Priority.High, Status.InProgress, 0);

        manager.AddTask(
            "Task2", "Description2",
            Priority.Low, Status.Done, 0);

        var tasks = manager.GetTasks();

        tasks.Should().HaveCount(2);

        tasks[0].Id.Should().Be(1);
        tasks[0].Title.Should().Be("Task1");
        tasks[0].Description.Should().Be("Description1");
        tasks[0].Priority.Should().Be(Priority.High);
        tasks[0].Status.Should().Be(Status.InProgress);

        tasks[1].Id.Should().Be(2);

        manager.UpdateTask(
            0, "Updated", "NewDescription",
            Priority.Medium, Status.Todo);

        var updated = manager.GetTasks()[0];

        updated.Id.Should().Be(1);
        updated.Title.Should().Be("Updated");
        updated.Description.Should().Be("NewDescription");
        updated.Priority.Should().Be(Priority.Medium);
        updated.Status.Should().Be(Status.Todo);

        manager.DeleteTask(0);

        manager.GetTasks().Should().ContainSingle()
            .Which.Id.Should().Be(2);
    }

    [Test]
    public void TaskManager_AddDeleteProject()
    {
        var manager = new TaskManager();

        manager.GetProjects().Should().BeEmpty();

        manager.AddProject("Project1");
        manager.AddProject("Project2");

        var projects = manager.GetProjects();

        projects.Should().HaveCount(2);
        projects[0].Id.Should().Be(1);
        projects[0].Name.Should().Be("Project1");
        projects[1].Id.Should().Be(2);
        projects[1].Name.Should().Be("Project2");

        manager.DeleteProject(0);

        manager.GetProjects().Should().ContainSingle()
            .Which.Id.Should().Be(2);
    }
    
    
    [Test]
    public void TaskManager_MoveTaskToProject()
    {
        var manager = new TaskManager();

        manager.AddProject("Project1");
        manager.AddProject("Project2");

        manager.AddTask(
            "Task1", "Description1",
            Priority.High, Status.InProgress, 1);

        manager.AddTask(
            "Task2", "Description2",
            Priority.Low, Status.Todo, 0);

        manager.MoveTaskToProject(-1, 2).Should().BeFalse();
        manager.MoveTaskToProject(2, 2).Should().BeFalse();

        manager.MoveTaskToProject(0, 99).Should().BeFalse();
        manager.GetTasks()[0].ProjectId.Should().Be(1);

        manager.MoveTaskToProject(0, 2).Should().BeTrue();

        var task = manager.GetTasks()[0];

        task.ProjectId.Should().Be(2);
        task.Id.Should().Be(1);
        task.Title.Should().Be("Task1");
        task.Description.Should().Be("Description1");
        task.Priority.Should().Be(Priority.High);
        task.Status.Should().Be(Status.InProgress);

        manager.MoveTaskToProject(0, 0).Should().BeTrue();

        manager.GetTasks()[0].ProjectId.Should().Be(0);
        manager.GetTasks()[1].ProjectId.Should().Be(0);
        manager.GetTasks().Should().HaveCount(2);
    }
    
    
    [Test]
    public void TaskManager_ProjectStatisticsAndCascadeDelete()
    {
        var manager = new TaskManager();

        manager.AddProject("Project1");
        manager.AddProject("Project2");
        manager.AddProject("EmptyProject");

        manager.AddTask("Task1", "", Priority.High, Status.Done, 1);
        manager.AddTask("Task2", "", Priority.Low, Status.Done, 2);
        manager.AddTask("Task3", "", Priority.Low, Status.InProgress, 1);
        manager.AddTask("Task4", "", Priority.High, Status.Done, 0);
        manager.AddTask("Task5", "", Priority.Medium, Status.Todo, 1);

        manager.CountTasksInProject(1).Should().Be(3);
        manager.CountTasksInProject(2).Should().Be(1);
        manager.CountTasksInProject(3).Should().Be(0);
        manager.CountTasksInProject(0).Should().Be(1);

        manager.CountTasksInProjectByStatus(1, Status.Done).Should().Be(1);
        manager.CountTasksInProjectByStatus(1, Status.InProgress).Should().Be(1);
        manager.CountTasksInProjectByStatus(1, Status.Todo).Should().Be(1);
        manager.CountTasksInProjectByStatus(2, Status.Todo).Should().Be(0);

        manager.CompletionPercent(1).Should().BeApproximately(100.0 / 3, 0.0001);
        manager.CompletionPercent(2).Should().Be(100.0);
        manager.CompletionPercent(3).Should().Be(0.0);

        manager.DeleteProject(0);

        manager.GetTasks().Select(t => t.Id).Should().Equal(2, 4);
        manager.GetProjects().Select(p => p.Id).Should().Equal(2, 3);

        manager.CountTasksInProject(1).Should().Be(0);
        manager.CountTasksInProject(2).Should().Be(1);
        manager.CountTasksInProject(0).Should().Be(1);
    }

}