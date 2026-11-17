using System.Globalization;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

/// <summary>Ein Foto für die KI.</summary>
public sealed record PhotoInput(byte[] Data, string MediaType);

/// <summary>Ergebnis einer Erkennung: das Formular und wer geantwortet hat.</summary>
public sealed record RecognitionResult(ItemDraft Draft, AiClientChoice Source, bool FromReplay);

/// <summary>
/// Erfasst ein Lebensmittel im Gespräch mit dem Modell:
/// 1. <see cref="RecognizeAsync"/>: ein oder mehrere Fotos (z. B. Vorder- und Rückseite) rein, typisiertes <see cref="ItemDraft"/> raus.
/// 2. <see cref="CorrectAsync"/>: Korrektur in natürlicher Sprache ("Das sind zwei, die kommen in den Gefrierschrank").
///    Der Verlauf bleibt erhalten, das Modell kennt also die Fotos und seine letzte Antwort.
/// Das Modell kann die Lagerorte abfragen und den Barcode in OpenFoodFacts nachschlagen.
/// </summary>
public sealed class ItemRecognitionService(AiClientProvider aiClientProvider, ItemRecognitionTools tools, ToolCallLog toolCallLog, ReplayStore replayStore)
{
    private readonly List<ChatMessage> _history = [];
    private AiClientChoice? _choice;

    /// <summary>Beginnt eine neue Erfassung mit einem oder mehreren Fotos desselben Produkts.</summary>
    public async Task<RecognitionResult> RecognizeAsync(IReadOnlyList<PhotoInput> photos, CancellationToken cancellationToken = default)
    {
        Reset();

        // Cloud oder lokal: nur hier entschieden, der Rest des Codes ist identisch.
        _choice = await aiClientProvider.GetAsync(AiTask.ImageWithTools, photos.Count, cancellationToken);

        List<AIContent> content =
        [
            new TextContent(photos.Count == 1
                ? "Capture the food item in this photo."
                : $"These {photos.Count} photos show the same product from different sides (e.g. front with the name, back with barcode and date). Combine the information and capture it."),
            .. photos.Select(p => new DataContent(p.Data, p.MediaType)),
        ];
        _history.Add(new ChatMessage(ChatRole.User, content));

        return await SendAsync(cancellationToken);
    }

    /// <summary>
    /// Korrigiert die letzte Erfassung per Sprache oder Text. Gibt es noch keine Erfassung,
    /// wird das Lebensmittel nur aus der Beschreibung erfasst ("Ein Liter Milch, hält bis Freitag").
    /// </summary>
    public async Task<RecognitionResult> CorrectAsync(string instruction, ItemDraft? currentForm, CancellationToken cancellationToken = default)
    {
        if (_choice is null)
        {
            Reset();
            _choice = await aiClientProvider.GetAsync(AiTask.TextWithTools, cancellationToken: cancellationToken);
            _history.Add(new ChatMessage(ChatRole.User, $"Capture this food item from the user's description: \"{instruction}\""));
        }
        else
        {
            // Der Benutzer kann das Formular inzwischen von Hand geändert haben: aktuellen Stand mitgeben.
            var form = currentForm is null
                ? string.Empty
                : $" Current form values: name '{currentForm.Name}', date {currentForm.ExpirationDate:yyyy-MM-dd}, date kind {currentForm.DateKind}, storage location id {currentForm.StorageLocationId}, amount {currentForm.Amount}.";
            _history.Add(new ChatMessage(ChatRole.User,
                $"Correction from the user (spoken, may contain recognition errors): \"{instruction}\".{form} Change only what the user corrects and return the complete result."));
        }

        return await SendAsync(cancellationToken);
    }

    /// <summary>Vergisst die laufende Erfassung (neuer Artikel).</summary>
    public void Reset()
    {
        _history.Clear();
        _history.Add(new ChatMessage(ChatRole.System, CreateInstructions()));
        _choice = null;
        toolCallLog.Clear();
    }

    private async Task<RecognitionResult> SendAsync(CancellationToken cancellationToken)
    {
        var options = new ChatOptions
        {
            Tools = [.. tools.Create().Select(toolCallLog.Track)],
        };

        // UseFunctionInvocation() in MauiProgram führt die Tool-Aufrufe aus.
        // GetResponseAsync<T> verlangt am Ende JSON nach dem Schema von ItemDraft.
        var replaysBefore = replayStore.ReplayCount;
        var response = await _choice!.Client.GetResponseAsync<ItemDraft>(_history, options, cancellationToken: cancellationToken);

        // Antwort (inkl. Tool-Aufrufe) in den Verlauf übernehmen, damit Korrekturen darauf aufbauen
        _history.AddRange(response.Messages);

        return new RecognitionResult(response.Result, _choice, replayStore.ReplayCount != replaysBefore);
    }

    // Prompt auf Englisch, Antwortsprache über AiLanguage (siehe dort)
    private static string CreateInstructions() => $"""
        You capture food items for a pantry app. {AiLanguage.Today}

        1. Identify the product in the photo(s).
        2. If a barcode is readable, call lookup_product with its digits. Treat the result as a hint, not as the truth:
           the database is often maintained only in the manufacturer's language and is sometimes wrong.
           The name is always short (2-5 words, brand plus product type) and in {AiLanguage.Name}: translate it if needed and check it against the photo.
           If a name contradicts the photo, the photo wins.
        3. Read the date printed on the package and pay attention to its kind:
           "best before", "BBE", "mindestens haltbar bis", "MHD" → DateKind BestBefore.
           "use by", "zu verbrauchen bis" (typical for minced meat, fish, poultry, raw milk) → DateKind UseBy.
           If only month and year are printed, use the last day of that month.
        4. If no date is readable (e.g. fruit, vegetables, bread), estimate the usual shelf life from today
           with proper storage and set DateKind to Estimated.
        5. Call get_storage_locations and pick the matching storage location. Only use an id from that list.
        6. Amount is the number of identical packages or pieces, at least 1.
        7. If the user corrects something, take over their values. Convert relative dates like "until Friday" starting from today.

        {AiLanguage.ResponseInstruction}
        """;
}
