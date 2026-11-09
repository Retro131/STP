using Lab7.Domain;

namespace Lab7.Application.Interfaces;

public interface IAssignmentRepository
{
    Task AddAsync(Assignment assignment, CancellationToken ct);
    Task<Assignment?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken ct);
    Task UpdateAsync(Assignment assignment, CancellationToken ct);
}