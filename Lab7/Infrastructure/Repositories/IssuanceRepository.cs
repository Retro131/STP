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
}