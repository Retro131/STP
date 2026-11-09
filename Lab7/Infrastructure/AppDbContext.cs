using Lab7.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}