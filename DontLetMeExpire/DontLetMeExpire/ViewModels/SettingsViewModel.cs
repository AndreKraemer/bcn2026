using System.Collections.ObjectModel;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;

namespace DontLetMeExpire.ViewModels;

public class SettingsViewModel : ViewModelBase
{
  private readonly IAlertService _alertService;
  private readonly IItemService _itemService;
  private readonly IStorageLocationService _storageLocationService;
  private readonly ISettingsService _settingsService;
  private KeyValuePair<string, string> _selectedLanguage;
  private KeyValuePair<string, string> _selectedTheme;

  private readonly AI.ReplayStore _replayStore;

  public SettingsViewModel(ISettingsService settingsService, IAlertService alertService,
      IItemService itemService, IStorageLocationService storageLocationService, AI.ReplayStore replayStore)
  {
    _settingsService = settingsService;
    _alertService = alertService;
    _itemService = itemService;
    _storageLocationService = storageLocationService;
    _replayStore = replayStore;
    SaveCommand = new Command(async () => await Save(), CanSave);
    ResetAppCommand = new Command(async () => await ResetApp());
    ClearRecordingsCommand = new Command(() =>
    {
      _replayStore.Clear();
      OnPropertyChanged(nameof(ClearRecordingsText));
    });
    ReplayModes =
    [
      new(AI.ReplayMode.Off, AppResources.ReplayOff),
      new(AI.ReplayMode.Auto, AppResources.ReplayAuto),
      new(AI.ReplayMode.Only, AppResources.ReplayOnly),
    ];
  }

  // Live-Modell oder Aufnahmen (für schlechtes WLAN auf der Bühne)
  public IReadOnlyList<KeyValuePair<AI.ReplayMode, string>> ReplayModes { get; }

  public KeyValuePair<AI.ReplayMode, string> SelectedReplayMode
  {
    get => ReplayModes.First(m => m.Key == _settingsService.GetReplayMode());
    set
    {
      if (value.Key != _settingsService.GetReplayMode())
      {
        _settingsService.SetReplayMode(value.Key);
        OnPropertyChanged(nameof(SelectedReplayMode));
      }
    }
  }

  public Command ClearRecordingsCommand { get; }

  public string ClearRecordingsText => string.Format(AppResources.ClearRecordings, _replayStore.RecordingCount);

  public Command ResetAppCommand { get; set; }

  public Command SaveCommand { get; }
  public ObservableCollection<KeyValuePair<string, string>> Languages { get; } = [];

  public ObservableCollection<KeyValuePair<string, string>> Themes { get; } = [];

  // Ausgewählte Sprache
  public KeyValuePair<string, string> SelectedLanguage
  {
    get => _selectedLanguage;
    set
    {
      SetProperty(ref _selectedLanguage, value);
      SaveCommand.ChangeCanExecute();
    }
  }

  // KI-Tool-Aufrufe auf der ItemPage anzeigen. Wird sofort gespeichert, ohne "Speichern".
  public bool ShowToolCalls
  {
    get => _settingsService.GetShowToolCalls();
    set
    {
      if (value != _settingsService.GetShowToolCalls())
      {
        _settingsService.SetShowToolCalls(value);
        OnPropertyChanged(nameof(ShowToolCalls));
      }
    }
  }

  // Lokale KI nur dort anbieten, wo es sie gibt (iOS/macOS 26+)
  public bool IsLocalAiSupported => AI.AiClientProvider.IsLocalSupportedOnPlatform;

  // Lokale KI (Apple Intelligence) statt Cloud. Wird sofort gespeichert, ohne "Speichern".
  public bool UseLocalAi
  {
    get => _settingsService.GetUseLocalAi();
    set
    {
      if (value != _settingsService.GetUseLocalAi())
      {
        _settingsService.SetUseLocalAi(value);
        OnPropertyChanged(nameof(UseLocalAi));
      }
    }
  }

  // Ausgewähltes Theme
  public KeyValuePair<string, string> SelectedTheme
  {
    get => _selectedTheme;
    set
    {
      SetProperty(ref _selectedTheme, value);
      SaveCommand.ChangeCanExecute();
    }
  }


  public Task InitializeAsync()
  {
    var languages = _settingsService.GetLanguages();
    Languages.Clear();
    SelectedLanguage = new KeyValuePair<string, string>();
    foreach (var language in languages) Languages.Add(language);

    SelectedLanguage = Languages.FirstOrDefault(x => x.Key == _settingsService.GetLanguage());

    var themes = _settingsService.GetThemes();
    Themes.Clear();
    SelectedTheme = new KeyValuePair<string, string>();
    foreach (var theme in themes) Themes.Add(theme);

    SelectedTheme = Themes.FirstOrDefault(x => x.Key == _settingsService.GetTheme());

    OnPropertyChanged(nameof(ClearRecordingsText));

    return Task.CompletedTask;
  }

  // Speichern der Einstellungen
  private async Task Save()
  {
    // prüfen, ob die Sprache geändert wurde
    var languageChanged = _settingsService.GetLanguage() != SelectedLanguage.Key;

    // Sprache und Theme speichern
    _settingsService.SetLanguage(SelectedLanguage.Key);
    _settingsService.SetTheme(SelectedTheme.Key);

    // Hinweis auf Neustart anzeigen, falls die Sprache geändert wurde
    if (languageChanged)
    {
      await _alertService.DisplayAlert(AppResources.RestartApp, AppResources.RestartAppLanguageChange, AppResources.Ok);
    }

    SaveCommand.ChangeCanExecute();
  }

  private bool CanSave()
  {
    return _settingsService.GetLanguage() != SelectedLanguage.Key ||
           _settingsService.GetTheme() != SelectedTheme.Key;
  }

  // Zurücksetzen der App
  private async Task ResetApp()
  {
    // Warnung darstellen, dass beim Zurücksetzen alle Daten verloren gehen.
    if (await _alertService.DisplayAlert(AppResources.Warning, AppResources.ResetAppWarnining, AppResources.Ok,
            AppResources.Cancel))
    {
      // Alle Vorratseinträge löschen
      await _itemService.DeleteAllAsync();

      // Alle Lagerorte löschen
      await _storageLocationService.DeleteAllAsync();

      // Ausgewählte Sprache und ausgewähltes Thema auf Standardeintrag zurücksetzen
      SelectedLanguage = Languages.First();
      SelectedTheme = Themes.First();
      await Save();
    }
  }
}