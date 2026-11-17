namespace DontLetMeExpire;

/// <summary>
/// Liefert Endpoint und Key fuer Azure AI Foundry.
/// 1. Startet Aspire die App, kommen die Werte aus den Parametern des AppHost (Umgebungsvariablen zur Laufzeit).
/// 2. Sonst gelten die Werte, die beim Build aus den Umgebungsvariablen in <see cref="AiSecrets"/> geschrieben wurden.
/// </summary>
internal static class AiSettings
{
    public static string Endpoint { get; } = Resolve("AI_Foundry_Endpoint", AiSecrets.Endpoint);

    public static string ApiKey { get; } = Resolve("AI_Foundry_ApiKey", AiSecrets.ApiKey);

    /// <summary>Name des Modell-Deployments in Foundry, Standard: gpt-4o.</summary>
    public static string Model { get; } = Resolve("AI_Foundry_Model", string.IsNullOrWhiteSpace(AiSecrets.Model) ? "gpt-4o" : AiSecrets.Model);

    public static bool IsConfigured =>
        Uri.TryCreate(Endpoint, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(ApiKey);

    static string Resolve(string variable, string buildTimeValue)
    {
        var runtimeValue = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(runtimeValue) ? buildTimeValue : runtimeValue;
    }
}
