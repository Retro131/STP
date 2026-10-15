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
            Priority.High, Status.InProgress);

        manager.AddTask(
            "Task2", "Description2",
            Priority.Low, Status.Done);

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
}