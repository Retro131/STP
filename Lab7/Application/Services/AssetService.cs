using Lab7.Application.Exceptions;
using Lab7.Application.Interfaces;
using Lab7.Application.Models;
using Lab7.Domain;

namespace Lab7.Application.Services;

public class AssetService(IAssetRepository assetRepository)
{
    public async Task AddAssetAsync(string inventoryNumber, string model, string category, CancellationToken ct = default)
    {
        Validate(inventoryNumber, model, category);

        inventoryNumber = inventoryNumber.Trim();
        model = model.Trim();
        category = category.Trim();

        if (await assetRepository.ExistsByInventoryNumberAsync(inventoryNumber, ct))
        {
            throw new DomainException($"Инвентарный номер {inventoryNumber} уже существует");
        }

        var asset = new Asset
        {
            InventoryNumber = inventoryNumber,
            Model = model,
            Category = category,
            Assigned = false
        };

        try
        {
            await assetRepository.AddAsync(asset, ct);
        }
        catch (Exception e)
        {
            throw new DomainException($"Не удалось добавить технику с инвентарным номером {inventoryNumber}: {e.Message}");
        }
    }

    public Task<List<AssetRow>> GetAllWithHoldersAsync(CancellationToken ct = default)
    {
        return assetRepository.GetAllWithHoldersAsync(ct);
    }
    
    public async Task DuplicateAssetAsync(
        Guid sourceId,
        string newInventoryNumber,
        CancellationToken ct = default)
    {
        var source = await assetRepository.GetByIdAsync(sourceId, ct);

        if (source is null)
        {
            throw new DomainException("Исходная техника не найдена");
        }

        await AddAssetAsync(
            newInventoryNumber,
            source.Model,
            source.Category,
            ct);
    }
    
    public async Task DeleteAssetCascadeAsync(
        Guid assetId,
        CancellationToken ct = default)
    {
        var asset = await assetRepository.GetByIdAsync(assetId, ct)
                    ?? throw new DomainException("Техника не найдена");

        if (asset.Assigned)
        {
            throw new DomainException(
                "Нельзя удалить выданную технику — сначала примите возврат");
        }

        await assetRepository.DeleteAsync(asset, ct);
    }
    
    private static void Validate(string inventoryNumber, string model, string category)
    {
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(inventoryNumber))
            messages.Add("Укажите инвентарный номер");

        if (string.IsNullOrWhiteSpace(model))
            messages.Add("Укажите модель");

        if (string.IsNullOrWhiteSpace(category))
            messages.Add("Укажите категорию");

        if (messages.Count > 0)
            throw new DomainException(string.Join(Environment.NewLine, messages));
    }
}