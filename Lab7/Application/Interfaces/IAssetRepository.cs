using Lab7.Application.Models;
using Lab7.Domain;

namespace Lab7.Application.Interfaces;

public interface IAssetRepository
{
    Task<bool> ExistsByInventoryNumberAsync(string inventoryNumber, CancellationToken ct = default);
    Task AddAsync(Asset asset, CancellationToken ct = default);
    Task<List<AssetRow>> GetAllWithHoldersAsync(CancellationToken ct = default);
    Task UpdateAsync(Asset asset, CancellationToken ct);
    Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken ct);
}