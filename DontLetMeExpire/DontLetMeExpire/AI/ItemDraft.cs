using System.ComponentModel;
using DontLetMeExpire.Models;

namespace DontLetMeExpire.AI;

/// <summary>
/// Strukturierte Antwort des Modells auf ein Foto. Microsoft.Extensions.AI erzeugt aus dieser Klasse
/// ein JSON-Schema (GetResponseAsync&lt;ItemDraft&gt;). Die [Description]-Attribute landen im Schema
/// und sind damit Teil des Prompts. Sie sind englisch wie alle Prompts; die Werte kommen in der App-Sprache.
/// </summary>
public sealed class ItemDraft
{
    [Description("Short product name (2-5 words) in the user's language, e.g. 'UHT milk 3.5 %' or 'Rawbite date bar cocoa'. Brand only if clearly readable.")]
    public string Name { get; set; } = string.Empty;

    [Description("Best-before or use-by date in the format yyyy-MM-dd.")]
    public DateOnly ExpirationDate { get; set; }

    [Description("BestBefore for 'best before' dates, UseBy for 'use by' dates, Estimated if no date was readable and it was estimated.")]
    public ExpirationDateKind DateKind { get; set; }

    [Description("Id of the matching storage location from get_storage_locations. Never invent an id.")]
    public string StorageLocationId { get; set; } = string.Empty;

    [Description("Number of identical packages or pieces in the photo, at least 1.")]
    public decimal Amount { get; set; } = 1;

    [Description("Barcode digits if a barcode was visible, otherwise null.")]
    public string? Barcode { get; set; }
}
