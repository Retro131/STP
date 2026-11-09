using Lab7.Application.Exceptions;
using Lab7.Application.Interfaces;
using Lab7.Application.Models;
using Lab7.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lab7.Infrastructure.Repositories;

public class AssetRepository(AppDbContext dbContext) : IAssetRepository
{
    public async Task<bool> ExistsByInventoryNumberAsync(string inventoryNumber, CancellationToken ct = default) =>
        await dbContext.Assets.AnyAsync(x => x.InventoryNumber == inventoryNumber, ct);

    public async Task AddAsync(Asset asset, CancellationToken ct = default)
    {
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<AssetRow>> GetAllWithHoldersAsync(CancellationToken ct = default)
    {
        return await dbContext.Assets
            .AsNoTracking()
            .OrderBy(x => x.InventoryNumber)
            .Select(asset => new AssetRow(asset,
                    asset.Assignments
                        .Where(assignment => assignment.ReturnedAt == null)
                        .Select(assignment => assignment.Employee.FullName)
                        .FirstOrDefault() ?? ""
                )
            )
            .ToListAsync(ct);
    }

    public async Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken ct)
    {
        return await dbContext.Assets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == assetId, ct);
    }

    public async Task UpdateAsync(Asset asset, CancellationToken ct)
    {
        dbContext.Assets.Update(asset);
        await dbContext.SaveChangesAsync(ct);
    }
}