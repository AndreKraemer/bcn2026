using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.AI;

/// <summary>
/// Sammelt die Tool-Aufrufe der letzten KI-Anfrage, damit die App sie live anzeigen kann.
/// </summary>
public sealed class ToolCallLog
{
    public ObservableCollection<ToolCallEntry> Entries { get; } = [];

    public void Clear() => MainThread.BeginInvokeOnMainThread(Entries.Clear);

    /// <summary>Hüllt eine Funktion so ein, dass jeder Aufruf hier protokolliert wird.</summary>
    public AIFunction Track(AIFunction function) => new TrackedFunction(function, this);

    private sealed class TrackedFunction(AIFunction inner, ToolCallLog log) : DelegatingAIFunction(inner)
    {
        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            var entry = new ToolCallEntry(Name, string.Join(", ", arguments.Select(a => $"{a.Key}: {a.Value}")));
            MainThread.BeginInvokeOnMainThread(() => log.Entries.Add(entry));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await base.InvokeCoreAsync(arguments, cancellationToken);
                entry.Complete(Describe(result), stopwatch.Elapsed, failed: false);
                return result;
            }
            catch (Exception ex)
            {
                entry.Complete(ex.Message, stopwatch.Elapsed, failed: true);
                throw;
            }
        }

        // Einzeilig, damit das Ergebnis in die Leiste passt. AIFunctionFactory liefert das Ergebnis
        // als eingerücktes JSON, das sonst nur aus "[" und "{" bestehen würde.
        private static readonly JsonSerializerOptions CompactJson = new(AIJsonUtilities.DefaultOptions) { WriteIndented = false };

        private static string Describe(object? result) => result switch
        {
            null => "–",
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() ?? "–",
            _ => JsonSerializer.Serialize(result, CompactJson),
        };
    }
}

/// <summary>Ein Tool-Aufruf, wie er in der Leiste auf der ItemPage erscheint.</summary>
public sealed class ToolCallEntry(string name, string arguments) : INotifyPropertyChanged
{
    public string Name { get; } = name;

    public string Arguments { get; } = arguments;

    public string Result { get; private set; } = "…";

    public string Status { get; private set; } = "⏳";

    public string Duration { get; private set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Complete(string result, TimeSpan duration, bool failed)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Result = result.Length > 200 ? result[..200] + " …" : result;
            Status = failed ? "❌" : "✅";
            Duration = $"{duration.TotalMilliseconds:0} ms";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        });
    }
}
