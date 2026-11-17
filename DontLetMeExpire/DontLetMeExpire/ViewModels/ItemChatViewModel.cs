using System.Collections.ObjectModel;
using DontLetMeExpire.AI;
using DontLetMeExpire.Models;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;

namespace DontLetMeExpire.ViewModels;

/// <summary>Eine Antwort im Lebensmittel-Chat, mit farbigem Urteil.</summary>
public sealed class AdviceEntry
{
    public bool IsUser { get; init; }

    public string Text { get; init; } = string.Empty;

    public string? VerdictText { get; init; }

    public Color VerdictColor { get; init; } = Colors.Transparent;

    public string Checks { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public bool HasVerdict => VerdictText is not null;

    public bool HasChecks => Checks.Length > 0;

    public LayoutOptions Alignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;
}

/// <summary>"Frag dein Lebensmittel": vordefinierte Fragen, freie Fragen und Foto auf Nachfrage.</summary>
public class ItemChatViewModel : ViewModelBase
{
    private readonly IItemService _itemService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly ItemAdvisorService _advisor;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string? _image;
    private string _input = string.Empty;
    private bool _isBusy;
    private bool _photoRequested;

    public ItemChatViewModel(IItemService itemService, IStorageLocationService storageLocationService, ItemAdvisorService advisor)
    {
        _itemService = itemService;
        _storageLocationService = storageLocationService;
        _advisor = advisor;
        AskCommand = new Command<string>(async q => await AskAsync(q), q => !IsBusy && !string.IsNullOrWhiteSpace(q));
        SendCommand = new Command(async () => await AskAsync(Input), () => !IsBusy && !string.IsNullOrWhiteSpace(Input));
        TakePhotoCommand = new Command(async () => await TakePhotoAsync(), () => !IsBusy);
    }

    public ObservableCollection<AdviceEntry> Messages { get; } = [];

    /// <summary>Vordefinierte Fragen, passend zum Zustand des Lebensmittels.</summary>
    public ObservableCollection<string> SuggestedQuestions { get; } = [];

    public Command<string> AskCommand { get; }

    public Command SendCommand { get; }

    public Command TakePhotoCommand { get; }

    public string Title { get => _title; set => SetProperty(ref _title, value); }

    public string Subtitle { get => _subtitle; set => SetProperty(ref _subtitle, value); }

    public string? Image { get => _image; set => SetProperty(ref _image, value); }

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
            AskCommand.ChangeCanExecute();
            SendCommand.ChangeCanExecute();
            TakePhotoCommand.ChangeCanExecute();
        });
    }

    /// <summary>Hat das Modell um ein Foto gebeten? Dann wird der Foto-Button hervorgehoben.</summary>
    public bool PhotoRequested { get => _photoRequested; set => SetProperty(ref _photoRequested, value); }

    public string Disclaimer => AppResources.AiDisclaimer;

    public async Task InitializeAsync(string itemId)
    {
        if (await _itemService.GetByIdAsync(itemId) is not { } item)
        {
            return;
        }

        var location = await _storageLocationService.GetByIdAsync(item.StorageLocationId);
        _advisor.Start(item, location?.Name ?? "?");

        Title = item.Name;
        Image = string.IsNullOrEmpty(item.Image) ? null : item.Image;
        var kind = item.DateKind switch
        {
            ExpirationDateKind.UseBy => AppResources.DateTypeUseBy,
            ExpirationDateKind.Estimated => AppResources.DateTypeEstimated,
            _ => AppResources.DateTypeBestBefore,
        };
        Subtitle = $"{kind}: {item.ExpirationDate:dd.MM.yyyy}";

        Messages.Clear();
        PhotoRequested = false;
        SuggestedQuestions.Clear();
        var daysLeft = (item.ExpirationDate.Date - DateTime.Today).Days;
        if (daysLeft < 0)
        {
            SuggestedQuestions.Add(AppResources.QuestionCanIEat);
            SuggestedQuestions.Add(AppResources.QuestionHowToCheck);
        }
        else
        {
            SuggestedQuestions.Add(AppResources.QuestionFreeze);
            SuggestedQuestions.Add(AppResources.QuestionCook);
        }
        SuggestedQuestions.Add(AppResources.QuestionStore);
    }

    private async Task AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        Input = string.Empty;
        Messages.Add(new AdviceEntry { IsUser = true, Text = question });
        await RunAsync(() => _advisor.AskAsync(question));
    }

    private async Task TakePhotoAsync()
    {
        var options = new MediaPickerOptions { MaximumWidth = 1280, MaximumHeight = 1280, CompressionQuality = 85 };
        var photo = MediaPicker.Default.IsCaptureSupported
            ? await MediaPicker.Default.CapturePhotoAsync(options)
            : (await MediaPicker.Default.PickPhotosAsync(options)).FirstOrDefault();
        if (photo is null)
        {
            return;
        }

        await using var stream = await photo.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        Messages.Add(new AdviceEntry { IsUser = true, Text = $"📷 {AppResources.TakePhotoForAi}" });
        var mediaType = string.IsNullOrEmpty(photo.ContentType) ? "image/jpeg" : photo.ContentType;
        await RunAsync(() => _advisor.AddPhotoAsync(new PhotoInput(memory.ToArray(), mediaType)));
    }

    private async Task RunAsync(Func<Task<(ItemAssessment Assessment, AiClientChoice Source, bool FromReplay)>> call)
    {
        try
        {
            IsBusy = true;
            var (assessment, source, fromReplay) = await call();

            var sourceText = source.SourceText
                + (fromReplay ? $" · {AppResources.FromReplay}" : string.Empty)
                + (source.FallbackReason is null ? string.Empty : $" · {source.FallbackReason}");

            Messages.Add(new AdviceEntry
            {
                Text = assessment.Answer,
                VerdictText = assessment.Verdict switch
                {
                    Verdict.LikelyFine => $"✅ {AppResources.VerdictLikelyFine}",
                    Verdict.CheckFirst => $"⚠️ {AppResources.VerdictCheckFirst}",
                    Verdict.DoNotEat => $"⛔ {AppResources.VerdictDoNotEat}",
                    Verdict.NeedPhoto => $"📷 {AppResources.VerdictNeedPhoto}",
                    _ => null,
                },
                VerdictColor = assessment.Verdict switch
                {
                    Verdict.LikelyFine => Color.FromArgb("#2E7D32"),
                    Verdict.CheckFirst => Color.FromArgb("#ED6C02"),
                    Verdict.DoNotEat => Color.FromArgb("#C62828"),
                    _ => Color.FromArgb("#6A5AE0"),
                },
                Checks = string.Join("\n", assessment.Checks.Select(c => $"• {c}")),
                Source = sourceText,
            });

            PhotoRequested = assessment.NeedsPhoto || assessment.Verdict == Verdict.NeedPhoto;
        }
        catch (Exception ex)
        {
            Messages.Add(new AdviceEntry { Text = string.Format(AppResources.AiError, ex.Message) });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
