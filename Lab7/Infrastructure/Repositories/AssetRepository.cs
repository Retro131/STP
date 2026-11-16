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

    public async Task UpdateAsync(IReadOnlyCollection<Asset> assets, CancellationToken ct)
    {
        dbContext.Assets.UpdateRange(assets);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken ct)
    {
        return await dbContext.Assets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == assetId, ct);
    }

    public async Task<List<Asset>> GetByIdsAsync(IReadOnlyCollection<Guid> assetIds, CancellationToken ct)
    {
        var distinctIds = assetIds.Distinct().ToList();

        if (distinctIds.Count == 0)
        {
            return [];
        }

        var assets = await dbContext.Assets
            .AsNoTracking()
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(ct);

        if (assets.Count != distinctIds.Count)
        {
            var foundIds = assets.Select(x => x.Id).ToHashSet();
            var missingIds = distinctIds.Where(id => !foundIds.Contains(id));

            throw new KeyNotFoundException(
                $"Не удалось найти {nameof(Asset)} со следующими ID: {string.Join(", ", missingIds)}");
        }

        return assets;
    }

    public async Task UpdateAsync(Asset asset, CancellationToken ct)
    {
        dbContext.Assets.Update(asset);
        await dbContext.SaveChangesAsync(ct);
    }
    
    public async Task DeleteAsync(Asset asset, CancellationToken ct)
    {
        dbContext.Assets.Remove(asset);
        await dbContext.SaveChangesAsync(ct);
    }
}