using Lab7.Application.Interfaces;

namespace Lab7.Application.Services;

public class AssignmentService(IIssuanceRepository issuanceRepository)
{
    public Task IssueAsync(Guid assetId, Guid employeeId, CancellationToken ct = default)
    {
        return issuanceRepository.IssueAsync(assetId, employeeId, ct);
    }
    
    public Task GiveBackAsync(Guid assetId, CancellationToken ct = default)
    {
        return issuanceRepository.GiveBackAsync(assetId, ct);
    }
    
    public Task IssueMultipleAsync(IReadOnlyCollection<Guid> assetIds, Guid employeeId, CancellationToken ct = default)
    {
        return issuanceRepository.IssueMultipleAsync(assetIds, employeeId, ct);
    }
    
    public Task ReassignAssetAsync(Guid assetId, Guid newEmployeeId, CancellationToken ct = default)
    {
        return issuanceRepository.ReassignAssetAsync(assetId, newEmployeeId, ct);
    }
}