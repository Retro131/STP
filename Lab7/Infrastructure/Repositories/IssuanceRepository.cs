using System.Data;
using Lab7.Application.Exceptions;
using Lab7.Application.Interfaces;
using Lab7.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Infrastructure.Repositories;

public class IssuanceRepository : IIssuanceRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IAssetRepository _assetRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAssignmentRepository _assignmentRepository;

    public IssuanceRepository(AppDbContext dbContext,
        IAssetRepository assetRepository,
        IEmployeeRepository employeeRepository,
        IAssignmentRepository assignmentRepository)
    {
        _dbContext = dbContext;
        _assetRepository = assetRepository;
        _employeeRepository = employeeRepository;
        _assignmentRepository = assignmentRepository;
    }

    public async Task IssueAsync(Guid assetId, Guid employeeId, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct)
                       ?? throw new DomainException("Фотограф не найден");

        var asset = await _assetRepository.GetByIdAsync(assetId, ct)
                    ?? throw new DomainException("Техника не найдена");

        if (asset.Assigned)
            throw new DomainException("Техника уже выдана");

        if (!employee.Active)
        {
            throw new DomainException("Нельзя выдать уволенному фотографу");
        }

        asset.Assigned = true;

        await _assetRepository.UpdateAsync(asset, ct);

        var assignment = new Assignment
        {
            AssetId = assetId,
            EmployeeId = employeeId,
            IssuedAt = DateTime.UtcNow,
            ReturnedAt = null
        };

        await _assignmentRepository.AddAsync(assignment, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task GiveBackAsync(Guid assetId, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var asset = await _assetRepository.GetByIdAsync(assetId, ct)
                    ?? throw new DomainException("Техника не найдена");

        var assignment = await _assignmentRepository.GetActiveByAssetIdAsync(assetId, ct)
                         ?? throw new DomainException("Нет активной выдачи для этой техники");

        assignment.ReturnedAt = DateTime.UtcNow;
        asset.Assigned = false;

        await _assignmentRepository.UpdateAsync(assignment, ct);
        await _assetRepository.UpdateAsync(asset, ct);

        await transaction.CommitAsync(ct);
    }
    public async Task IssueMultipleAsync(IReadOnlyCollection<Guid> assetIds, Guid employeeId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetIds);

        if (assetIds.Count == 0)
        {
            throw new DomainException("Не выбрано ни одной единицы техники");
        }
        
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct)
                       ?? throw new DomainException("Фотограф не найден");

        if (!employee.Active)
        {
            throw new DomainException("Нельзя выдать уволенному фотографу");
        }

        var assets = new List<Asset>(assetIds.Count);

        foreach (var assetId in assetIds)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId, ct)
                        ?? throw new DomainException($"Техника с id {assetId} не найдена");

            if (asset.Assigned)
            {
                throw new DomainException(
                    $"Техника {asset.InventoryNumber} уже выдана");
            }

            assets.Add(asset);
        }

        var issuedAt = DateTime.UtcNow;

        foreach (var asset in assets)
        {
            asset.Assigned = true;

            await _assetRepository.UpdateAsync(asset, ct);

            var assignment = new Assignment
            {
                AssetId = asset.Id,
                EmployeeId = employeeId,
                IssuedAt = issuedAt,
                ReturnedAt = null
            };

            await _assignmentRepository.AddAsync(assignment, ct);
        }

        await transaction.CommitAsync(ct);
    }
    public async Task ReassignAssetAsync(Guid assetId, Guid newEmployeeId, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var asset = await _assetRepository.GetByIdAsync(assetId, ct)
                    ?? throw new DomainException("Техника не найдена");

        if (!asset.Assigned)
        {
            throw new DomainException(
                "Техника не выдана — используйте «Выдать»");
        }

        var newEmployee = await _employeeRepository.GetByIdAsync(newEmployeeId, ct)
                          ?? throw new DomainException("Фотограф не найден");

        if (!newEmployee.Active)
        {
            throw new DomainException(
                "Нельзя переназначить уволенному фотографу");
        }

        var currentAssignment = await _assignmentRepository
                                    .GetActiveByAssetIdAsync(assetId, ct)
                                ?? throw new DomainException("Нет активной выдачи");

        if (currentAssignment.EmployeeId == newEmployeeId)
        {
            throw new DomainException("Техника уже у этого фотографа");
        }

        var now = DateTime.UtcNow;

        currentAssignment.ReturnedAt = now;

        await _assignmentRepository.UpdateAsync(currentAssignment, ct);

        var newAssignment = new Assignment
        {
            AssetId = assetId,
            EmployeeId = newEmployeeId,
            IssuedAt = now,
            ReturnedAt = null
        };

        await _assignmentRepository.AddAsync(newAssignment, ct);

        await transaction.CommitAsync(ct);
    }
}