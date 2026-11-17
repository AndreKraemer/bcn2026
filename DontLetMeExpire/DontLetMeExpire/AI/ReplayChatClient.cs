using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

/// <summary>Wie die App mit Live-Modell und Aufnahmen umgeht.</summary>
public enum ReplayMode
{
    /// <summary>Immer live, nichts aufnehmen.</summary>
    Off,

    /// <summary>Live und jede Antwort aufnehmen. Schlägt der Aufruf fehl, wird die Aufnahme abgespielt.</summary>
    Auto,

    /// <summary>Nur Aufnahmen abspielen (schlechtes WLAN, Generalprobe ohne Kosten).</summary>
    Only,
}

/// <summary>Speichert Modellantworten als JSON-Dateien im App-Verzeichnis.</summary>
public sealed class ReplayStore(ISettingsService settingsService)
{
    private readonly string _directory = Path.Combine(FileSystem.AppDataDirectory, "ai-recordings");
    private int _replayCount;

    public ReplayMode Mode => settingsService.GetReplayMode();

    /// <summary>Zählt abgespielte Aufnahmen, damit die Oberfläche "aus Aufnahme" anzeigen kann.</summary>
    public int ReplayCount => _replayCount;

    public int RecordingCount => Directory.Exists(_directory) ? Directory.GetFiles(_directory, "*.json").Length : 0;

    public void Clear()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    internal ChatResponse? Load(string key)
    {
        var path = Path.Combine(_directory, key + ".json");
        if (!File.Exists(path))
        {
            return null;
        }

        Interlocked.Increment(ref _replayCount);
        return JsonSerializer.Deserialize<ChatResponse>(File.ReadAllText(path), AIJsonUtilities.DefaultOptions);
    }

    internal void Save(string key, ChatResponse response)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, key + ".json"), JsonSerializer.Serialize(response, AIJsonUtilities.DefaultOptions));
    }
}

/// <summary>
/// Middleware für Bühnen-Demos: nimmt jede Modellantwort auf und spielt sie bei Netzproblemen
/// (oder auf Wunsch immer) wieder ab. Sitzt innen in der Pipeline, also unter UseFunctionInvocation:
/// Die Tools laufen auch bei einer Aufnahme wirklich, nur die Modellantworten kommen aus der Datei.
/// </summary>
public sealed class ReplayChatClient(IChatClient innerClient, ReplayStore store) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var mode = store.Mode;
        if (mode == ReplayMode.Off)
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }

        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        var key = CreateKey(list, options);

        if (mode == ReplayMode.Only)
        {
            return store.Load(key) ?? throw new InvalidOperationException(AppResources.NoRecording);
        }

        try
        {
            var response = await base.GetResponseAsync(list, options, cancellationToken);
            store.Save(key, response);
            return response;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested && store.Load(key) is { } recorded)
        {
            return recorded;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var mode = store.Mode;
        if (mode == ReplayMode.Off)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        var key = CreateKey(list, options);
        var recorded = mode == ReplayMode.Only
            ? store.Load(key) ?? throw new InvalidOperationException(AppResources.NoRecording)
            : null;

        if (recorded is null)
        {
            // Live streamen und mitschneiden. Bricht der Stream ab, bevor etwas ankam, die Aufnahme nehmen.
            var updates = new List<ChatResponseUpdate>();
            await using var enumerator = base.GetStreamingResponseAsync(list, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }
                    update = enumerator.Current;
                }
                catch (Exception) when (updates.Count == 0 && !cancellationToken.IsCancellationRequested && store.Load(key) is { } fallback)
                {
                    recorded = fallback;
                    break;
                }

                updates.Add(update);
                yield return update;
            }

            if (recorded is null)
            {
                store.Save(key, updates.ToChatResponse());
                yield break;
            }
        }

        // Aufnahme in kleinen Stücken abspielen, damit es auch auf der Bühne nach Streaming aussieht
        foreach (var update in recorded.ToChatResponseUpdates())
        {
            if (update.Contents.Count == 1 && update.Contents[0] is TextContent { Text.Length: > 0 } text)
            {
                foreach (var chunk in text.Text.Chunk(12))
                {
                    yield return new ChatResponseUpdate(update.Role, new string(chunk));
                    await Task.Delay(15, cancellationToken);
                }
            }
            else
            {
                yield return update;
            }
        }
    }

    /// <summary>
    /// Schlüssel aus dem Inhalt der Anfrage. Systemnachrichten (mit dem heutigen Datum) und zufällige
    /// Ids von Tool-Aufrufen bleiben außen vor, damit eine Aufnahme aus der Generalprobe auch am Vortragstag passt.
    /// </summary>
    private static string CreateKey(IReadOnlyList<ChatMessage> messages, ChatOptions? options)
    {
        // Die Sprache gehört zum Schlüssel: Systemnachrichten zählen nicht mit, die Antwortsprache steht aber dort
        var builder = new StringBuilder("lang:").Append(AiLanguage.Code).Append('|');
        foreach (var message in messages.Where(m => m.Role != ChatRole.System))
        {
            builder.Append(message.Role.Value).Append(':');
            foreach (var content in message.Contents)
            {
                builder.Append(content switch
                {
                    TextContent t => t.Text,
                    DataContent d => Convert.ToHexString(SHA256.HashData(d.Data.Span)),
                    FunctionCallContent c => $"call {c.Name}({JsonSerializer.Serialize(c.Arguments, AIJsonUtilities.DefaultOptions)})",
                    FunctionResultContent r => $"result {JsonSerializer.Serialize(r.Result, AIJsonUtilities.DefaultOptions)}",
                    _ => content.GetType().Name,
                }).Append('|');
            }
        }

        if (options?.Tools is { Count: > 0 } tools)
        {
            builder.Append("tools:").AppendJoin(',', tools.Select(t => t.Name));
        }

        if (options?.ResponseFormat is ChatResponseFormatJson { Schema: { } schema })
        {
            builder.Append("schema:").Append(schema.GetRawText());
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))[..32];
    }
}
