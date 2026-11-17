using System.ComponentModel;
using System.Text;
using DontLetMeExpire.Models;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

/// <summary>Welche Artikel das Rezept vollständig verbraucht (Nummern aus der Liste im Prompt).</summary>
public sealed class UsedItems
{
    [Description("Numbers of the listed food items that the recipe uses up completely.")]
    public List<int> FullyUsed { get; set; } = [];
}

/// <summary>Ein erzeugtes Rezept samt Gesprächsverlauf, um danach die verbrauchten Artikel zu ermitteln.</summary>
public sealed class RecipeSession(AiClientChoice source, List<ChatMessage> history, IReadOnlyList<Item> items)
{
    public AiClientChoice Source { get; } = source;

    internal List<ChatMessage> History { get; } = history;

    internal IReadOnlyList<Item> Items { get; } = items;
}

/// <summary>
/// Akt 3 der Demo: Rezept gestreamt erzeugen (der Text baut sich live auf) und danach in derselben
/// Unterhaltung strukturiert fragen, welche Artikel verbraucht werden. Daraus wird der Button "Gekocht".
/// </summary>
public sealed class RecipeService(AiClientProvider aiClientProvider)
{
    public async Task<RecipeSession> StreamRecipeAsync(IReadOnlyList<Item> items, Action<string> onText, CancellationToken cancellationToken = default)
    {
        var choice = await aiClientProvider.GetAsync(AiTask.Text, cancellationToken: cancellationToken);
        List<ChatMessage> history = [new(ChatRole.User, CreatePrompt(items))];

        var recipe = new StringBuilder();
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in choice.Client.GetStreamingResponseAsync(history, cancellationToken: cancellationToken))
        {
            updates.Add(update);
            recipe.Append(update.Text);
            onText(StripCodeFence(recipe.ToString()));
        }

        history.AddRange(updates.ToChatResponse().Messages);
        return new RecipeSession(choice, history, items);
    }

    /// <summary>Fragt nach, welche Artikel das Rezept vollständig verbraucht (strukturierte Ausgabe).</summary>
    public async Task<IReadOnlyList<Item>> GetFullyUsedItemsAsync(RecipeSession session, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages =
        [
            .. session.History,
            new(ChatRole.User, "Which of the numbered food items are used up completely by this recipe? Answer only with the numbers."),
        ];

        var response = await session.Source.Client.GetResponseAsync<UsedItems>(messages, cancellationToken: cancellationToken);
        return response.Result.FullyUsed
            .Where(n => n >= 1 && n <= session.Items.Count)
            .Distinct()
            .Select(n => session.Items[n - 1])
            .ToList();
    }

    // Modelle verpacken Markdown gern in einen Codeblock (```markdown ... ```). Der MarkdownView zeigt das sonst als Code.
    private static string StripCodeFence(string text)
    {
        var trimmed = text.TrimStart();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            return string.Empty; // Erste Zeile noch unvollständig
        }

        var body = trimmed[(firstLineEnd + 1)..].TrimEnd();
        return body.EndsWith("```", StringComparison.Ordinal) ? body[..^3].TrimEnd() : body;
    }

    private static string CreatePrompt(IReadOnlyList<Item> items)
    {
        // Nummern statt Ids: kurz, eindeutig und für das Modell leicht wiederzugeben
        // Prompt auf Englisch, Antwortsprache über AiLanguage
        var prompt = new StringBuilder("I have these food items:");
        for (var i = 0; i < items.Count; i++)
        {
            prompt.Append($"\n{i + 1}. {items[i].Name}, amount {items[i].Amount} (expires on {items[i].ExpirationDate:yyyy-MM-dd})");
        }

        prompt.Append($"""


            The list contains all food items that expire soon, including their dates.
            Goals:
            1. Use up the items that expire first completely, if possible.
            2. Avoid food waste.
            3. You may optionally use the other listed items if it makes sense.
            4. The recipe is shown to the user in an app, so feel free to use emojis.

            Requirements:
            - Create exactly ONE recipe.
            - Praise the user for not wanting to waste food and be encouraging without overdoing it.
            - You may assume basic kitchen staples such as salt, pepper, oil, butter, flour, sugar and water.
            - List every other additional ingredient explicitly under a heading "Missing ingredients".
            - If no additional ingredients are needed, write "Missing ingredients: none".

            Answer format: plain Markdown, not wrapped in a code block (```).
            1. Recipe title (heading 1)
            2. Short description (1–2 sentences, italic)
            3. Ingredients with amounts (heading 2)
            4. Step-by-step instructions (heading 2)
            5. Missing ingredients (list or "none", heading 2)
            6. A note which of the listed items are used up completely and which may be left over.
            7. A nice closing line
            Translate all headings into the response language.

            {AiLanguage.ResponseInstruction}
            """);
        return prompt.ToString();
    }
}
