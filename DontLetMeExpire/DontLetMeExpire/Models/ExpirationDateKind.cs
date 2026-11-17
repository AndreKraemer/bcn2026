using System.Text.Json.Serialization;

namespace DontLetMeExpire.Models;

/// <summary>
/// Welche Art von Datum auf der Packung steht. Wichtig für "Kann ich das noch essen?":
/// Nach dem MHD ist ein Lebensmittel oft noch gut, nach dem Verbrauchsdatum nicht mehr.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ExpirationDateKind>))]
public enum ExpirationDateKind
{
  /// <summary>"Mindestens haltbar bis" (MHD).</summary>
  BestBefore = 0,

  /// <summary>"Zu verbrauchen bis": leicht verderblich, z. B. Hackfleisch, Fisch, Rohmilch.</summary>
  UseBy = 1,

  /// <summary>Kein Datum aufgedruckt, Haltbarkeit geschätzt (z. B. Obst, Gemüse, Brot).</summary>
  Estimated = 2,
}
