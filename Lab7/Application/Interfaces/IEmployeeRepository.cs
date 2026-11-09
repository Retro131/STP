using Lab7.Domain;

namespace Lab7.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExistsByEmailAsync(string email, Guid excludedEmployeeId, CancellationToken ct);

    Task<bool> HasActiveAssignmentsAsync(Guid employeeId, CancellationToken ct);

    Task UpdateAsync(Employee employee, CancellationToken ct);

    Task AddAsync(Employee employee, CancellationToken ct);

    Task<List<Employee>> GetAllAsync(CancellationToken ct);
}