namespace Lab7.Application.Interfaces;

public interface IIssuanceRepository
{
    Task IssueAsync(Guid assetId, Guid employeeId, CancellationToken ct = default);
    Task GiveBackAsync(Guid assetId, CancellationToken ct = default);
}