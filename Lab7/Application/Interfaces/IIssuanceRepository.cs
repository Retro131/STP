namespace Lab7.Application.Interfaces;

public interface IIssuanceRepository
{
    Task IssueAsync(Guid assetId, Guid employeeId, CancellationToken ct = default);
    Task GiveBackAsync(Guid assetId, CancellationToken ct = default);
    Task IssueMultipleAsync(IReadOnlyCollection<Guid> assetIds, Guid employeeId, CancellationToken ct = default);
    Task ReassignAssetAsync(Guid assetId, Guid newEmployeeId, CancellationToken ct = default);
}