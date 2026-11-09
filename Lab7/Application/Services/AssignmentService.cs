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
}