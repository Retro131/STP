using Lab7.Application.Interfaces;
using Lab7.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Infrastructure.Repositories;

public class AssignmentRepository(AppDbContext dbContext) : IAssignmentRepository
{
    public async Task<Assignment?> GetActiveByAssetIdAsync(Guid assetId, CancellationToken ct)
    {
        return await dbContext.Assignments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AssetId == assetId && x.ReturnedAt == null, ct);
    }

    public async Task AddAsync(Assignment assignment, CancellationToken ct)
    {
        dbContext.Assignments.Add(assignment);

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Assignment assignment, CancellationToken ct)
    {
        dbContext.Assignments.Update(assignment);

        await dbContext.SaveChangesAsync(ct);
    }
}