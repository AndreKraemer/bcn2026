using System.Globalization;
using CommunityToolkit.Maui.Media;
using DontLetMeExpire.Resources.Strings;

namespace DontLetMeExpire.Services;

/// <summary>
/// Spracheingabe über die Spracherkennung des Betriebssystems (CommunityToolkit.Maui kapselt
/// Android SpeechRecognizer, iOS SFSpeechRecognizer und Windows SpeechRecognition).
/// Hört zu, bis der Benutzer eine Pause macht, und liefert den erkannten Satz.
/// </summary>
public sealed class VoiceInputService(ISpeechToText speechToText)
{
    public bool IsListening => speechToText.CurrentState == SpeechToTextState.Listening;

    /// <summary>Hört einen Satz lang zu. Liefert null, wenn nichts erkannt oder abgebrochen wurde.</summary>
    /// <param name="onPartialResult">Wird mit dem bisher erkannten Text aufgerufen (Live-Anzeige).</param>
    public async Task<string?> ListenAsync(Action<string> onPartialResult, CancellationToken cancellationToken = default)
    {
        if (!await speechToText.RequestPermissions(cancellationToken))
        {
            throw new PermissionException(AppResources.VoicePermissionDenied);
        }

        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var partialText = string.Empty;

        void OnUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
        {
            partialText += e.RecognitionResult;
            MainThread.BeginInvokeOnMainThread(() => onPartialResult(partialText));
        }

        void OnCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
        {
            if (e.RecognitionResult.IsSuccessful)
            {
                completion.TrySetResult(string.IsNullOrWhiteSpace(e.RecognitionResult.Text) ? null : e.RecognitionResult.Text);
            }
            else
            {
                completion.TrySetException(e.RecognitionResult.Exception ?? new InvalidOperationException(AppResources.VoiceRecognitionFailed));
            }
        }

        speechToText.RecognitionResultUpdated += OnUpdated;
        speechToText.RecognitionResultCompleted += OnCompleted;
        try
        {
            await speechToText.StartListenAsync(new SpeechToTextOptions
            {
                Culture = CultureInfo.CurrentUICulture,
                ShouldReportPartialResults = true,
                // Nach 2 Sekunden Stille ist der Satz zu Ende
                AutoStopSilenceTimeout = TimeSpan.FromSeconds(2),
            }, cancellationToken);

            await using var registration = cancellationToken.Register(() => completion.TrySetResult(null));
            return await completion.Task;
        }
        finally
        {
            speechToText.RecognitionResultUpdated -= OnUpdated;
            speechToText.RecognitionResultCompleted -= OnCompleted;
            if (speechToText.CurrentState == SpeechToTextState.Listening)
            {
                await speechToText.StopListenAsync(CancellationToken.None);
            }
        }
    }

    /// <summary>Beendet das Zuhören vorzeitig. Der bisher erkannte Text wird noch geliefert.</summary>
    public Task StopAsync() => speechToText.StopListenAsync(CancellationToken.None);
}
