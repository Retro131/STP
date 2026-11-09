using Lab7.Application.Interfaces;
using Lab7.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Infrastructure.Repositories;

public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid excludedEmployeeId, CancellationToken ct)
    {
        return await context.Employees
            .AnyAsync(x => x.Email == email 
                           && x.Id != excludedEmployeeId, ct);
    }

    public async Task<bool> HasActiveAssignmentsAsync(Guid employeeId, CancellationToken ct)
    {
        return await context.Assignments
            .AnyAsync(x => x.EmployeeId == employeeId 
                           && x.ReturnedAt == null, ct);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken ct)
    {
        context.Employees.Update(employee);

        await context.SaveChangesAsync(ct);
    }

    public async Task AddAsync(Employee employee, CancellationToken ct)
    {
        context.Employees.Add(employee);

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Employee>> GetAllAsync(CancellationToken ct)
    {
        return await context.Employees
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);
    }
}