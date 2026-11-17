using System.Text.Json.Serialization;

namespace DontLetMeExpire.OpenFoodFacts;

public class Product
{
  [JsonPropertyName("_id")]
  public string Id { get; set; } = string.Empty;

  [JsonPropertyName("image_url")]
  public string? ImageUrl { get; set; }

  // Name in der Hauptsprache des Produkts (Feld "lang"), z. B. Dänisch bei dänischen Marken
  [JsonPropertyName("product_name")]
  public string? ProductName { get; set; }

  [JsonPropertyName("product_name_de")]
  public string? ProductNameDe { get; set; }

  [JsonPropertyName("product_name_en")]
  public string? ProductNameEn { get; set; }

  [JsonPropertyName("brands")]
  public string? Brands { get; set; }

  // Hauptsprache des Produkteintrags, z. B. "de" oder "da"
  [JsonPropertyName("lang")]
  public string? Language { get; set; }

  /// <summary>Name in der Sprache der App (Deutsch oder Englisch), falls vorhanden, sonst der Name in der Hauptsprache.</summary>
  [JsonIgnore]
  public string? PreferredName
  {
    get
    {
      var localized = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
      {
        "de" => ProductNameDe,
        "en" => ProductNameEn,
        _ => null,
      };
      return !string.IsNullOrWhiteSpace(localized) ? localized : ProductName;
    }
  }
}
