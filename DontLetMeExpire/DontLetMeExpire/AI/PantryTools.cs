using System.ComponentModel;
using DontLetMeExpire.Models;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

/// <summary>
/// Akt 2 der Demo: die vorhandenen Services der App als Tools für den Vorrats-Chat.
/// Lesen und Hinzufügen geschieht direkt, Austragen nur nach Rückfrage beim Benutzer.
/// </summary>
public sealed class PantryTools(IItemService itemService, IStorageLocationService storageLocationService, IAlertService alertService)
{
    public IList<AIFunction> Create() =>
    [
        AIFunctionFactory.Create(GetStockAsync, "get_stock"),
        AIFunctionFactory.Create(GetStorageLocationsAsync, "get_storage_locations"),
        AIFunctionFactory.Create(AddItemAsync, "add_item"),
        AIFunctionFactory.Create(UpdateAmountAsync, "update_amount"),
        AIFunctionFactory.Create(RemoveItemAsync, "remove_item"),
    ];

    [Description("Returns the whole stock with id, name, storage location, date, date kind, days left (negative = expired) and amount.")]
    private async Task<List<StockEntry>> GetStockAsync()
    {
        var locations = (await storageLocationService.GetAsync()).ToDictionary(l => l.Id, l => l.Name);
        var items = await itemService.GetAsync();
        return items
            .OrderBy(i => i.ExpirationDate)
            .Select(i => new StockEntry(
                i.Id,
                i.Name,
                locations.GetValueOrDefault(i.StorageLocationId, "?"),
                DateOnly.FromDateTime(i.ExpirationDate),
                i.DateKind,
                (i.ExpirationDate.Date - DateTime.Today).Days,
                i.Amount))
            .ToList();
    }

    [Description("Returns all storage locations of the user (id and name).")]
    private async Task<List<LocationEntry>> GetStorageLocationsAsync() =>
        (await storageLocationService.GetAsync()).Select(l => new LocationEntry(l.Id, l.Name)).ToList();

    [Description("Adds an item to the stock. Call get_storage_locations first to get a valid storage location id.")]
    private async Task<string> AddItemAsync(
        [Description("Short name in the user's language")] string name,
        [Description("Best-before or use-by date")] DateOnly expirationDate,
        [Description("Id from get_storage_locations")] string storageLocationId,
        [Description("Amount, at least 1")] decimal amount,
        [Description("Kind of date: BestBefore, UseBy or Estimated")] ExpirationDateKind dateKind = ExpirationDateKind.BestBefore)
    {
        if (await storageLocationService.GetByIdAsync(storageLocationId) is null)
        {
            return "Unknown storage location id. Call get_storage_locations.";
        }

        var item = new Item
        {
            Id = string.Empty,
            Name = name,
            ExpirationDate = expirationDate.ToDateTime(TimeOnly.MinValue),
            StorageLocationId = storageLocationId,
            Amount = Math.Max(1, amount),
            DateKind = dateKind,
            Image = string.Empty,
        };
        await itemService.SaveAsync(item);
        return $"Added: {name} (id {item.Id}).";
    }

    [Description("Changes the amount of an item. If the new amount is 0, the item is removed after asking the user.")]
    private async Task<string> UpdateAmountAsync(
        [Description("Id from get_stock")] string itemId,
        [Description("New amount")] decimal amount)
    {
        if (await itemService.GetByIdAsync(itemId) is not { } item)
        {
            return "Unknown item id. Call get_stock.";
        }

        if (amount <= 0)
        {
            return await RemoveItemAsync(itemId);
        }

        item.Amount = amount;
        await itemService.SaveAsync(item);
        return $"Amount of {item.Name} is now {amount}.";
    }

    [Description("Removes an item from the stock (eaten or thrown away). The user has to confirm.")]
    private async Task<string> RemoveItemAsync([Description("Id from get_stock")] string itemId)
    {
        if (await itemService.GetByIdAsync(itemId) is not { } item)
        {
            return "Unknown item id. Call get_stock.";
        }

        // Der Mensch entscheidet: Löschen nur mit Zustimmung
        var allowed = await alertService.DisplayAlert(
            AppResources.PantryChat,
            string.Format(AppResources.ConfirmDeleteByAi, item.Name),
            AppResources.YesAnswer,
            AppResources.NoAnswer);
        if (!allowed)
        {
            return $"The user declined removing {item.Name}.";
        }

        await itemService.DeleteAsync(item);
        return $"Removed: {item.Name}.";
    }

    private sealed record StockEntry(string Id, string Name, string Location, DateOnly ExpirationDate, ExpirationDateKind DateKind, int DaysLeft, decimal Amount);

    private sealed record LocationEntry(string Id, string Name);
}
