namespace lab3;

public class Project
{
    public int Id { get; }
    public string Name { get; set; }

    public Project(int id = 0, string name = "")
    {
        Id = id;
        Name = name;
    }
}