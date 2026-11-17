using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using DontLetMeExpire.Models;
using DontLetMeExpire.Resources.Strings;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

[JsonConverter(typeof(JsonStringEnumConverter<Verdict>))]
public enum Verdict
{
    /// <summary>Keine Frage zur Essbarkeit (Lagern, Einfrieren, Kochen).</summary>
    Info,
    LikelyFine,
    CheckFirst,
    DoNotEat,
    NeedPhoto,
}

/// <summary>Strukturierte Antwort zu einem einzelnen Lebensmittel.</summary>
public sealed class ItemAssessment
{
    [Description("Info for questions that are not about whether the food is safe to eat. Otherwise LikelyFine, CheckFirst, DoNotEat or NeedPhoto.")]
    public Verdict Verdict { get; set; }

    [Description("Short, friendly answer in the user's language, at most 4 sentences.")]
    public string Answer { get; set; } = string.Empty;

    [Description("Concrete checks for the user in the user's language (look, smell, taste ...). Empty if not applicable.")]
    public List<string> Checks { get; set; } = [];

    [Description("true if you need a photo of the food for a reliable assessment.")]
    public bool NeedsPhoto { get; set; }
}

/// <summary>Haltbarkeit nach dem Öffnen.</summary>
public sealed class OpenedShelfLife
{
    [Description("Usual shelf life after opening in days, with proper storage.")]
    public int Days { get; set; }

    [Description("Short storage hint in the user's language, e.g. 'Keep in the fridge and use within 3 days'.")]
    public string Hint { get; set; } = string.Empty;
}

/// <summary>
/// Chat mit einem einzelnen Lebensmittel ("Kann ich das noch essen?"). Das Modell kennt Name, Datum
/// und Datumsart und fordert bei verderblichen Lebensmitteln ein Foto an. Das Foto kommt in dasselbe Gespräch.
/// Die wichtigste Sicherheitsregel steht zusätzlich im Code, nicht nur im Prompt.
/// </summary>
public sealed class ItemAdvisorService(AiClientProvider aiClientProvider, ReplayStore replayStore)
{
    private readonly List<ChatMessage> _history = [];
    private Item? _item;

    public void Start(Item item, string locationName)
    {
        _item = item;
        _history.Clear();
        _history.Add(new ChatMessage(ChatRole.System, CreateInstructions(item, locationName, DateTime.Today)));
    }

    public Task<(ItemAssessment Assessment, AiClientChoice Source, bool FromReplay)> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatMessage(ChatRole.User, question));
        return SendAsync(AiTask.Text, cancellationToken);
    }

    public Task<(ItemAssessment Assessment, AiClientChoice Source, bool FromReplay)> AddPhotoAsync(PhotoInput photo, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatMessage(ChatRole.User,
        [
            new TextContent("Here is a current photo of the food. Assess it again and look for mould, discolouration, slime and bloated packaging."),
            new DataContent(photo.Data, photo.MediaType),
        ]));
        return SendAsync(AiTask.Image, cancellationToken);
    }

    private async Task<(ItemAssessment, AiClientChoice, bool)> SendAsync(AiTask task, CancellationToken cancellationToken)
    {
        // Enthält das Gespräch ein Foto, muss das Modell Bilder verstehen (auch für spätere Fragen)
        if (_history.Any(m => m.Contents.OfType<DataContent>().Any()))
        {
            task = AiTask.Image;
        }

        var choice = await aiClientProvider.GetAsync(task, cancellationToken: cancellationToken);
        var replaysBefore = replayStore.ReplayCount;
        try
        {
            var response = await choice.Client.GetResponseAsync<ItemAssessment>(_history, cancellationToken: cancellationToken);
            _history.AddRange(response.Messages);
            return (ApplySafetyRules(response.Result), choice, replayStore.ReplayCount != replaysBefore);
        }
        catch
        {
            // Unbeantwortete Nachricht entfernen, damit ein neuer Versuch sauber startet
            _history.RemoveAt(_history.Count - 1);
            throw;
        }
    }

    /// <summary>
    /// "Geöffnet": Wie lange hält das Lebensmittel nach dem Öffnen? Eigene, kurze Anfrage ohne Verlauf.
    /// Das neue Datum ist nie später als das bisherige (ein MHD verlängert sich durch Öffnen nicht).
    /// </summary>
    public async Task<(DateTime NewDate, OpenedShelfLife ShelfLife, AiClientChoice Source)> GetDateAfterOpeningAsync(
        string name, string locationName, DateTime currentDate, CancellationToken cancellationToken = default)
    {
        var choice = await aiClientProvider.GetAsync(AiTask.Text, cancellationToken: cancellationToken);
        var response = await choice.Client.GetResponseAsync<OpenedShelfLife>(
            $"The food item \"{name}\" was opened today and is kept in the storage location \"{locationName}\". " +
            $"How many days does it usually keep after opening? Be rather cautious. {AiLanguage.ResponseInstruction}",
            cancellationToken: cancellationToken);

        var days = Math.Clamp(response.Result.Days, 0, 365);
        var opened = DateTime.Today.AddDays(days);
        return (opened < currentDate ? opened : currentDate, response.Result, choice);
    }

    /// <summary>
    /// Leitplanke im Code: Ein abgelaufenes Verbrauchsdatum ("zu verbrauchen bis") ist nie "noch gut",
    /// egal was das Modell meint. Auf so etwas verlässt man sich nicht allein auf den Prompt.
    /// </summary>
    private ItemAssessment ApplySafetyRules(ItemAssessment assessment)
    {
        if (_item is { DateKind: ExpirationDateKind.UseBy } item
            && item.ExpirationDate.Date < DateTime.Today
            && assessment.Verdict is Verdict.LikelyFine or Verdict.CheckFirst or Verdict.NeedPhoto)
        {
            assessment.Verdict = Verdict.DoNotEat;
            assessment.NeedsPhoto = false;
            assessment.Checks = [];
            assessment.Answer = AppResources.SafetyUseByExpired;
        }

        return assessment;
    }

    private static string CreateInstructions(Item item, string locationName, DateTime today)
    {
        // Prompt auf Englisch, Antwortsprache über AiLanguage
        var days = (item.ExpirationDate.Date - today).Days;
        var status = days < 0 ? $"expired {-days} day(s) ago" : days == 0 ? "expires today" : $"expires in {days} day(s)";
        var dateKind = item.DateKind switch
        {
            ExpirationDateKind.UseBy => "use-by date",
            ExpirationDateKind.Estimated => "estimated shelf life (no date printed)",
            _ => "best-before date",
        };

        return $"""
            You give advice about exactly one food item from the user's pantry. {AiLanguage.Today}
            Food item: {item.Name}, amount {item.Amount}, storage location {locationName}.
            Date: {item.ExpirationDate:yyyy-MM-dd} ({dateKind}), {status}.

            Rules for the question whether it is still safe to eat:
            - Use-by date passed (minced meat, fish, poultry, raw milk ...): always DoNotEat.
            - Best-before date passed for long-life, industrially packaged food (chocolate, pasta, canned food, biscuits):
              usually LikelyFine, with checks (look, smell, taste a little).
            - Best-before date passed for perishable food (yoghurt, quark, cheese, bread, fruit, vegetables, cold cuts):
              As long as you have no photo yet, ALWAYS answer with Verdict NeedPhoto and NeedsPhoto = true
              and ask for a photo in your answer. Still list the checks.
              Once you have a photo: LikelyFine, CheckFirst or DoNotEat.
            - Mould on bread, yoghurt, soft fruit or soft cheese: DoNotEat.
            - Be cautious. If in doubt, CheckFirst or DoNotEat. Never give a guarantee.
            Answer questions about storing, freezing or cooking with Verdict Info.

            {AiLanguage.ResponseInstruction}
            """;
    }
}
