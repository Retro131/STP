using Lab7.Application.Exceptions;
using Lab7.Application.Interfaces;
using Lab7.Domain;

namespace Lab7.Application.Services;

public class EmployeeService(IEmployeeRepository repository)
{
    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync(CancellationToken ct = default)
    {
        return await repository.GetAllAsync(ct);
    }

    public async Task<Employee> GetEmployeeByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await repository.GetByIdAsync(id, ct)
               ?? throw new DomainException("Фотограф не найден");
    }

    public async Task AddEmployeeAsync(string name, string email, CancellationToken ct = default)
    {
        Validate(name, email);

        name = name.Trim();
        email = email.Trim();

        if (await repository.ExistsByEmailAsync(email, Guid.Empty, ct))
        {
            throw new DomainException($"Фотограф с email {email} уже существует");
        }

        var newEmployee = new Employee
        {
            FullName = name,
            Email = email,
            Active = true
        };

        await repository.AddAsync(newEmployee, ct);
    }

    public async Task UpdateEmployeeAsync(Guid id, string name, string email, bool active,
        CancellationToken ct = default)
    {
        Validate(name, email);

        name = name.Trim();
        email = email.Trim();

        var employee = await repository.GetByIdAsync(id, ct);

        if (employee == null)
        {
            throw new DomainException($"Фотограф с id {id} не найден");
        }
        
        if (employee.Email != email && await repository.ExistsByEmailAsync(email, id, ct))
        {
            throw new DomainException($"Фотограф с email {email} уже существует");
        }

        if (!active && await repository.HasActiveAssignmentsAsync(id, ct))
        {
            throw new DomainException($"У фотографа на руках техника. Для начала верните технику");
        }

        employee.FullName = name;
        employee.Email = email;
        employee.Active = active;

        await repository.UpdateAsync(employee, ct);
    }

    private static void Validate(string name, string email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Укажите ФИО");

        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Укажите email");

        if (errors.Count > 0)
        {
            throw new DomainException(string.Join(Environment.NewLine, errors));
        }
    }
}