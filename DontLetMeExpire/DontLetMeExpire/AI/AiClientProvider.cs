using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
#if IOS || MACCATALYST
using Microsoft.Maui.Essentials.AI;
#endif

namespace DontLetMeExpire.AI;

/// <summary>Wofür die KI gebraucht wird. Davon hängt ab, ob das lokale Modell reicht.</summary>
public enum AiTask
{
    /// <summary>Reiner Text, z. B. das Rezept.</summary>
    Text,

    /// <summary>Text mit Tools, z. B. der Vorrats-Chat.</summary>
    TextWithTools,

    /// <summary>Bild mit Tools und strukturierter Ausgabe, z. B. die Foto-Erfassung.</summary>
    ImageWithTools,

    /// <summary>Bild ohne Tools, z. B. Foto für "Kann ich das noch essen?".</summary>
    Image,
}

/// <summary>Der gewählte Client und eine Zeile für die Anzeige, welches Modell geantwortet hat.</summary>
public sealed record AiClientChoice(IChatClient Client, bool IsLocal, string SourceText, string? FallbackReason = null);

/// <summary>
/// Wählt zwischen Cloud (Azure OpenAI) und lokaler KI (Apple Intelligence).
/// Beide Clients sind <see cref="IChatClient"/>: der aufrufende Code bleibt identisch,
/// nur die Registrierung unterscheidet sich (Keyed Services "cloud" und "local").
/// </summary>
public sealed class AiClientProvider(IServiceProvider services, ISettingsService settingsService)
{
    public const string CloudKey = "cloud";
    public const string LocalKey = "local";

    /// <summary>Gibt es auf dieser Plattform überhaupt ein lokales Modell?</summary>
    [System.Runtime.Versioning.SupportedOSPlatformGuard("ios26.0")]
    [System.Runtime.Versioning.SupportedOSPlatformGuard("maccatalyst26.0")]
    public static bool IsLocalSupportedOnPlatform =>
#if IOS
        OperatingSystem.IsIOSVersionAtLeast(26);
#elif MACCATALYST
        OperatingSystem.IsMacCatalystVersionAtLeast(26);
#else
        false;
#endif

    public async Task<AiClientChoice> GetAsync(AiTask task, int imageCount = 0, CancellationToken cancellationToken = default)
    {
        if (settingsService.GetUseLocalAi() && IsLocalSupportedOnPlatform)
        {
            var reason = await GetLocalUnavailableReasonAsync(task, imageCount, cancellationToken);
            if (reason is null)
            {
                return new(services.GetRequiredKeyedService<IChatClient>(LocalKey), true, AppResources.AiViaLocal);
            }

            return Cloud() with { FallbackReason = reason };
        }

        return Cloud();
    }

    private AiClientChoice Cloud() =>
        new(services.GetRequiredKeyedService<IChatClient>(CloudKey), false, string.Format(AppResources.AiViaCloud, AiSettings.Model));

    /// <summary>null, wenn das lokale Modell die Aufgabe kann, sonst der Grund.</summary>
    private static async Task<string?> GetLocalUnavailableReasonAsync(AiTask task, int imageCount, CancellationToken cancellationToken)
    {
        // Apples lokales Modell nimmt (Stand iOS 27) nur ein Bild pro Anfrage
        if (imageCount > 1)
        {
            return AppResources.LocalAiOneImage;
        }

#if IOS || MACCATALYST
#pragma warning disable MAUIAI0001 // Preview-API
        if (!IsLocalSupportedOnPlatform)
        {
            return null;
        }

        var status = await AppleIntelligenceChatClient.GetModelStatusAsync(cancellationToken);
        if (status.Availability != ChatModelAvailability.Available)
        {
            return string.Format(AppResources.LocalAiUnavailable, status.Availability);
        }

        var capabilities = status.Capabilities ?? [];
        if (task == AiTask.TextWithTools && !capabilities.Contains(ChatModelCapability.ToolCalling))
        {
            return AppResources.LocalAiNoTools;
        }

        if (task == AiTask.Image && !capabilities.Contains(ChatModelCapability.ImageInput))
        {
            return AppResources.LocalAiNoImages;
        }

        if (task == AiTask.ImageWithTools &&
            !(capabilities.Contains(ChatModelCapability.ImageInput) && capabilities.Contains(ChatModelCapability.ToolCalling)))
        {
            return AppResources.LocalAiNoImages;
        }
#pragma warning restore MAUIAI0001

        return null;
#else
        await Task.CompletedTask;
        return string.Format(AppResources.LocalAiUnavailable, DeviceInfo.Platform);
#endif
    }
}
