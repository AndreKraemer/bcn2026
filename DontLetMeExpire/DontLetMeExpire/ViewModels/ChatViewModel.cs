using System.Collections.ObjectModel;
using System.Globalization;
using DontLetMeExpire.AI;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;
using Microsoft.Extensions.AI;

namespace DontLetMeExpire.ViewModels;

/// <summary>Eine Nachricht im Vorrats-Chat.</summary>
public sealed class ChatEntry(bool isUser, string text) : ViewModelBase
{
    private string _text = text;

    public bool IsUser { get; } = isUser;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public LayoutOptions Alignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;
}

/// <summary>
/// Akt 2 der Demo: mit dem Vorrat sprechen ("Was muss diese Woche weg?", "Ich hab die Milch aufgebraucht").
/// Die Services der App sind als Tools freigegeben (<see cref="PantryTools"/>).
/// </summary>
public class ChatViewModel : ViewModelBase
{
    private readonly AiClientProvider _aiClientProvider;
    private readonly PantryTools _pantryTools;
    private readonly ToolCallLog _toolCallLog;
    private readonly ISettingsService _settingsService;
    private readonly VoiceInputService _voiceInputService;
    private readonly ReplayStore _replayStore;
    private readonly List<ChatMessage> _history = [];
    private string _input = string.Empty;
    private bool _isBusy;
    private bool _isListening;
    private bool _showToolCalls;

    public ChatViewModel(AiClientProvider aiClientProvider, PantryTools pantryTools, ToolCallLog toolCallLog,
        ISettingsService settingsService, VoiceInputService voiceInputService, ReplayStore replayStore)
    {
        _aiClientProvider = aiClientProvider;
        _pantryTools = pantryTools;
        _toolCallLog = toolCallLog;
        _settingsService = settingsService;
        _voiceInputService = voiceInputService;
        _replayStore = replayStore;
        ToolCalls = toolCallLog.Entries;
        SendCommand = new Command(async () => await SendAsync(Input), () => !IsBusy && !string.IsNullOrWhiteSpace(Input));
        SpeakCommand = new Command(async () => await SpeakAsync(), () => !IsBusy);
        NewChatCommand = new Command(NewChat, () => !IsBusy);
    }

    public ObservableCollection<ChatEntry> Messages { get; } = [];

    public ObservableCollection<ToolCallEntry> ToolCalls { get; }

    public Command SendCommand { get; }

    public Command SpeakCommand { get; }

    public Command NewChatCommand { get; }

    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value, SendCommand.ChangeCanExecute);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value, () =>
        {
            SendCommand.ChangeCanExecute();
            SpeakCommand.ChangeCanExecute();
            NewChatCommand.ChangeCanExecute();
        });
    }

    public bool IsListening
    {
        get => _isListening;
        set => SetProperty(ref _isListening, value);
    }

    public bool ShowToolCalls
    {
        get => _showToolCalls;
        set => SetProperty(ref _showToolCalls, value);
    }

    public void Initialize()
    {
        ShowToolCalls = _settingsService.GetShowToolCalls();
        if (_history.Count == 0)
        {
            NewChat();
        }
    }

    private void NewChat()
    {
        _history.Clear();
        _history.Add(new ChatMessage(ChatRole.System, $"""
            You are the pantry assistant of the app "Don't let me expire". {AiLanguage.Today}
            Answer questions about the user's stock and maintain it with the tools. Call get_stock before answering instead of guessing.
            Help the user not to waste food. Keep answers short and friendly.
            If the user says they used something up, adjust the amount or remove the item.
            Convert relative dates like "until Friday" starting from today.

            {AiLanguage.ResponseInstruction}
            """));
        Messages.Clear();
        _toolCallLog.Clear();
    }

    private async Task SpeakAsync()
    {
        if (IsListening)
        {
            await _voiceInputService.StopAsync();
            return;
        }

        string? spoken;
        try
        {
            IsListening = true;
            Input = AppResources.Listening;
            spoken = await _voiceInputService.ListenAsync(text => Input = text);
        }
        catch (Exception ex)
        {
            Input = string.Empty;
            Messages.Add(new ChatEntry(false, string.Format(AppResources.VoiceError, ex.Message)));
            return;
        }
        finally
        {
            IsListening = false;
        }

        if (string.IsNullOrWhiteSpace(spoken))
        {
            Input = string.Empty;
            return;
        }

        // Gesprochenes direkt abschicken
        await SendAsync(spoken);
    }

    private async Task SendAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Input = string.Empty;
        Messages.Add(new ChatEntry(true, text));
        var answer = new ChatEntry(false, "…");
        Messages.Add(answer);

        try
        {
            IsBusy = true;
            _history.Add(new ChatMessage(ChatRole.User, text));

            // Cloud oder lokal: das lokale Modell muss dafür Tools aufrufen können
            var choice = await _aiClientProvider.GetAsync(AiTask.TextWithTools);
            var options = new ChatOptions { Tools = [.. _pantryTools.Create().Select(_toolCallLog.Track)] };

            var replaysBefore = _replayStore.ReplayCount;
            var response = await choice.Client.GetResponseAsync(_history, options);
            _history.AddRange(response.Messages);

            var source = choice.SourceText;
            if (_replayStore.ReplayCount != replaysBefore)
            {
                source += $" · {AppResources.FromReplay}";
            }
            if (choice.FallbackReason is not null)
            {
                source += $" · {choice.FallbackReason}";
            }

            answer.Text = $"{response.Text}\n\n— {source}";
        }
        catch (Exception ex)
        {
            answer.Text = string.Format(AppResources.AiError, ex.Message);

            // Unbeantwortete Frage aus dem Verlauf nehmen, damit der nächste Versuch sauber startet
            if (_history.Count > 0 && _history[^1].Role == ChatRole.User)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
