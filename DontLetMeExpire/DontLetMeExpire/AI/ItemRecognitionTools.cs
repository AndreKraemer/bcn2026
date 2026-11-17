using System.ComponentModel;
using DontLetMeExpire.OpenFoodFacts;
using DontLetMeExpire.Services;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

/// <summary>
/// Vorhandene Services der App, als Tools für das Modell freigegeben.
/// Das Modell entscheidet selbst, ob und wann es sie aufruft.
/// Beschreibungen und Ergebnisse sind englisch: Sie liest nur das Modell.
/// </summary>
public sealed class ItemRecognitionTools(IStorageLocationService storageLocationService, IOpenFoodFactsApiClient openFoodFactsApiClient)
{
    public IList<AIFunction> Create() =>
    [
        AIFunctionFactory.Create(GetStorageLocationsAsync, "get_storage_locations"),
        AIFunctionFactory.Create(LookupProductAsync, "lookup_product"),
    ];

    [Description("Returns all storage locations of the user (id and name), e.g. fridge or pantry.")]
    private async Task<IEnumerable<StorageLocationInfo>> GetStorageLocationsAsync()
    {
        var locations = await storageLocationService.GetAsync();
        return locations.Select(l => new StorageLocationInfo(l.Id, l.Name)).ToList();
    }

    [Description("Looks up a product in the OpenFoodFacts database by the digits of an EAN-8 or EAN-13 barcode readable in the photo.")]
    private async Task<string> LookupProductAsync(
        [Description("The barcode digits without spaces.")] string barcode)
    {
        barcode = new string(barcode.Where(char.IsDigit).ToArray());

        // Die Prüfziffer verhindert, dass ein falsch abgelesener Barcode ein falsches Produkt liefert.
        if (!IsValidEan(barcode))
        {
            return "Invalid barcode (check digit does not match). Do not guess again; identify the product from the photo only.";
        }

        try
        {
            var response = await openFoodFactsApiClient.GetProductByCodeAsync(barcode);
            if (response is not { Status: 1, Product: { } product } || string.IsNullOrWhiteSpace(product.ProductName))
            {
                return "Product is not known in OpenFoodFacts.";
            }

            // Die Daten sind von der Community gepflegt: oft nur in der Sprache des Herstellers,
            // manchmal auch falsch. Deshalb alles mitgeben und das Modell gegen das Foto prüfen lassen.
            return $"Found. Brand: {product.Brands ?? "unknown"}. " +
                   $"Name in the main language '{product.Language ?? "?"}': {product.ProductName}. " +
                   $"German name: {Or(product.ProductNameDe)}. English name: {Or(product.ProductNameEn)}.";
        }
        catch (Exception ex)
        {
            return $"OpenFoodFacts is not reachable: {ex.Message}";
        }
    }

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "none" : value;

    /// <summary>Prüft Länge und Prüfziffer eines EAN-8- oder EAN-13-Codes.</summary>
    internal static bool IsValidEan(string code)
    {
        if (code.Length is not (8 or 13) || !code.All(char.IsDigit))
        {
            return false;
        }

        // Von rechts gezählt (ohne Prüfziffer) abwechselnd mit 3 und 1 gewichten.
        var sum = 0;
        for (var i = 0; i < code.Length - 1; i++)
        {
            var digit = code[code.Length - 2 - i] - '0';
            sum += i % 2 == 0 ? digit * 3 : digit;
        }

        return (10 - sum % 10) % 10 == code[^1] - '0';
    }

    private sealed record StorageLocationInfo(string Id, string Name);
}
