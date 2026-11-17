using DontLetMeExpire.AI;
using DontLetMeExpire.Models;
using DontLetMeExpire.OpenFoodFacts;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace DontLetMeExpire.ViewModels;

public class ItemViewModel : ValidationViewModelBase
{
  private string _name;
  private DateTime _expirationDate;
  private StorageLocation _location;
  private decimal _amount;
  private string _id;
  private string _image;
  private string _searchText;
  private bool _showBarcodeScanner;
  private string _title;
  private readonly IItemService _itemService;
  private readonly INavigationService _navigationService;
  private readonly IStorageLocationService _storageLocationService;
  private readonly IOpenFoodFactsApiClient _openFoodFactsApiClient;
  private readonly ItemRecognitionService _itemRecognitionService;
  private readonly ISettingsService _settingsService;
  private readonly IAlertService _alertService;
  private readonly VoiceInputService _voiceInputService;
  private readonly ItemAdvisorService _itemAdvisorService;
  private bool _isRecognizing;
  private bool _isListening;
  private bool _showToolCalls;
  private string _recognitionHint = string.Empty;
  private string _voiceText = string.Empty;

  public ItemViewModel(INavigationService navigationService,
                        IStorageLocationService storageLocationService,
                        IItemService itemService,
                        IOpenFoodFactsApiClient openFoodFactsApiClient,
                        ItemRecognitionService itemRecognitionService,
                        ToolCallLog toolCallLog,
                        ISettingsService settingsService,
                        IAlertService alertService,
                        VoiceInputService voiceInputService,
                        ItemAdvisorService itemAdvisorService)
  {
    _alertService = alertService;
    _voiceInputService = voiceInputService;
    _itemAdvisorService = itemAdvisorService;
    AskAiCommand = new Command(async () => await _navigationService.GoToAsync(nameof(Views.ItemChatPage),
      new Dictionary<string, object> { ["ItemId"] = Id }), () => HasId);
    OpenedCommand = new Command(async () => await MarkAsOpenedAsync(), () => !IsRecognizing && !string.IsNullOrWhiteSpace(Name));
    SpeakCorrectionCommand = new Command(async () => await SpeakCorrectionAsync(), () => !IsRecognizing);
    SaveCommand = new Command(async () => await SaveAsync());
    ScanBarcodeCommand = new Command(ScanBarcode);
    SearchBarcodeCommand = new Command(async () => await SearchBarcode());
    TakePhotoCommand = new Command(async () => await TakePhoto());
    DeletePhotoCommand = new Command(DeletePhoto);
    CaptureWithAiCommand = new Command(async () => await RecognizeAsync(fromCamera: true), () => !IsRecognizing);
    PickPhotoWithAiCommand = new Command(async () => await RecognizeAsync(fromCamera: false), () => !IsRecognizing);
    _navigationService = navigationService;
    _storageLocationService = storageLocationService;
    _openFoodFactsApiClient = openFoodFactsApiClient;
    _itemService = itemService;
    _itemRecognitionService = itemRecognitionService;
    _settingsService = settingsService;
    ToolCalls = toolCallLog.Entries;
  }

  public ObservableCollection<StorageLocation> StorageLocations { get; set; } = [];

  /// <summary>Foto aufnehmen und von der KI erfassen lassen.</summary>
  public Command CaptureWithAiCommand { get; }

  /// <summary>Vorhandenes Foto wählen und von der KI erfassen lassen (z. B. vorbereitete Demo-Fotos).</summary>
  public Command PickPhotoWithAiCommand { get; }

  /// <summary>Tool-Aufrufe der letzten Erkennung.</summary>
  public ObservableCollection<ToolCallEntry> ToolCalls { get; }

  public bool IsRecognizing
  {
    get => _isRecognizing;
    set => SetProperty(ref _isRecognizing, value, () =>
    {
      CaptureWithAiCommand.ChangeCanExecute();
      PickPhotoWithAiCommand.ChangeCanExecute();
      SpeakCorrectionCommand.ChangeCanExecute();
      OpenedCommand.ChangeCanExecute();
    });
  }

  /// <summary>Chat zu diesem Lebensmittel öffnen ("Kann ich das noch essen?"). Nur für gespeicherte Artikel.</summary>
  public Command AskAiCommand { get; }

  /// <summary>"Geöffnet": Die KI setzt das Datum auf die Haltbarkeit nach dem Öffnen.</summary>
  public Command OpenedCommand { get; }

  public bool HasId => !string.IsNullOrEmpty(Id);

  /// <summary>Formular per Sprache korrigieren oder ganz ohne Foto erfassen. Erneutes Tippen beendet das Zuhören.</summary>
  public Command SpeakCorrectionCommand { get; }

  public bool IsListening
  {
    get => _isListening;
    set => SetProperty(ref _isListening, value);
  }

  /// <summary>Live erkannter Text während der Spracheingabe.</summary>
  public string VoiceText
  {
    get => _voiceText;
    set => SetProperty(ref _voiceText, value);
  }

  /// <summary>Leiste mit den Tool-Aufrufen anzeigen (in den Einstellungen abschaltbar).</summary>
  public bool ShowToolCalls
  {
    get => _showToolCalls;
    set => SetProperty(ref _showToolCalls, value);
  }

  /// <summary>Hinweis zur Erkennung, z. B. ob das Datum gelesen oder geschätzt wurde.</summary>
  public string RecognitionHint
  {
    get => _recognitionHint;
    set => SetProperty(ref _recognitionHint, value);
  }

  public Command SaveCommand { get; }

  public Command ScanBarcodeCommand { get; }

  public Command SearchBarcodeCommand { get; }

  public Command TakePhotoCommand { get; }

  public Command DeletePhotoCommand { get; }
  public string Title
  {
    get => _title;
    set => SetProperty(ref _title, value);
  }

  public string SearchText
  {
    get => _searchText;
    set => SetProperty(ref _searchText, value);
  }


  public string Id
  {
    get => _id;
    set => SetProperty(ref _id, value, () =>
    {
      OnPropertyChanged(nameof(HasId));
      AskAiCommand.ChangeCanExecute();
    });
  }

  /// <summary>
  /// Der Name des Elements.
  /// </summary>
  [Required(ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = "RequiredErrorMessage")]
  [Length(1, 50, ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = "LengthErrorMessage")]
  [Display(Name = "Name", ResourceType = typeof(AppResources))]

  public string Name
  {
    get => _name;
    set => SetProperty(ref _name, value, () =>
    {
      SaveCommand.ChangeCanExecute();
      OpenedCommand.ChangeCanExecute();
    });
  }

  /// <summary>
  /// Das Ablaufdatum des Elements.
  /// </summary>
  public DateTime ExpirationDate
  {
    get => _expirationDate;
    set => SetProperty(ref _expirationDate, value);
  }

  /// <summary>Art des Datums: MHD, Verbrauchsdatum oder geschätzt.</summary>
  public IReadOnlyList<KeyValuePair<ExpirationDateKind, string>> DateKinds { get; } =
  [
    new(ExpirationDateKind.BestBefore, AppResources.DateTypeBestBefore),
    new(ExpirationDateKind.UseBy, AppResources.DateTypeUseBy),
    new(ExpirationDateKind.Estimated, AppResources.DateTypeEstimated),
  ];

  private KeyValuePair<ExpirationDateKind, string> _selectedDateKind;

  public KeyValuePair<ExpirationDateKind, string> SelectedDateKind
  {
    get => _selectedDateKind;
    set => SetProperty(ref _selectedDateKind, value);
  }

  /// <summary>
  /// Der Speicherort des Elements.
  /// </summary>
  public StorageLocation SelectedLocation
  {
    get => _location;
    set => SetProperty(ref _location, value);
  }

  /// <summary>
  /// Die Menge des Elements.
  /// </summary>
  [Range(1, 10000, ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = "RangeErrorMessage")]
  [Display(Name = "Amount", ResourceType = typeof(AppResources))]
  public decimal Amount
  {
    get => _amount;
    set => SetProperty(ref _amount, value, SaveCommand.ChangeCanExecute);
  }

  /// <summary>
  /// Das Bild des Elements.
  /// </summary>
  public string Image
  {
    get => _image;
    set => SetProperty(ref _image, value);
  }

  public bool ShowBarcodeScanner
  {
    get => _showBarcodeScanner;
    set => SetProperty(ref _showBarcodeScanner, value);
  }

  /// <summary>
  /// Initialisiert das ViewModel asynchron.
  /// </summary>
  public async Task InitializeAsync(string? itemId = null)
  {
    ShowToolCalls = _settingsService.GetShowToolCalls();
    RecognitionHint = string.Empty;
    VoiceText = string.Empty;
    _itemRecognitionService.Reset();

    // Speicherorte laden
    var locations = await _storageLocationService.GetAsync();

    // Die Liste der Speicherorte aktualisieren
    StorageLocations.Clear();
    foreach (var location in locations)
    {
      StorageLocations.Add(location);
    }
    if (!string.IsNullOrEmpty(itemId))
    {
      var item = await _itemService.GetByIdAsync(itemId);
      if (item != null)
      {
        Id = item.Id;
        Name = item.Name;
        ExpirationDate = item.ExpirationDate;
        SelectedLocation = StorageLocations.FirstOrDefault(x => x.Id == item.StorageLocationId);
        Image = item.Image;
        Amount = item.Amount;
        SelectedDateKind = DateKinds.First(k => k.Key == item.DateKind);
        Title = item.Name;
      }

    }
    else
    {
      Id = string.Empty;
      Name = string.Empty;
      ExpirationDate = DateTime.Today;
      SelectedLocation = StorageLocations.First();
      Image = string.Empty;
      Amount = 1;
      SelectedDateKind = DateKinds[0];
      Title = AppResources.NewEntry;
    }
  }

  /// <summary>
  /// Speichert das Element asynchron.
  /// </summary>
  private async Task SaveAsync()
  {
    if (!Validate())
    {
      return;
    }
    // Neues Element mit den
    // Daten des ViewModels erstellen
    var item = new Item
    {
      Id = Id,
      Name = Name,
      ExpirationDate = ExpirationDate,
      StorageLocationId = SelectedLocation.Id,
      Amount = Amount,
      DateKind = SelectedDateKind.Key,
      Image = Image
    };

    // Element speichern
    await _itemService.SaveAsync(item);

    // Daten für die Anzeige zurücksetzen
    Name = string.Empty;
    ExpirationDate = DateTime.Today;
    Amount = 0;
    SelectedLocation = StorageLocations.First();

    await _navigationService.GoToAsync("..");
  }

  private bool CanSave()
  {
    return !string.IsNullOrEmpty(Name)
      && Amount > 0;
  }

  private void DeletePhoto()
  {
    Image = string.Empty;
  }

  private async Task TakePhoto()
  {
    if (MediaPicker.Default.IsCaptureSupported)
    {
      var options = new MediaPickerOptions
            {
            //    MaximumHeight = 300,
            //    CompressionQuality = 80
            };
      var photo = await MediaPicker.Default.CapturePhotoAsync(options);

      if (photo != null)
      {
        // save the file into local storage
        var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

        await using var sourceStream = await photo.OpenReadAsync();
        await using var localFileStream = File.OpenWrite(localFilePath);

        await sourceStream.CopyToAsync(localFileStream);

        var fileInfo = new FileInfo(localFilePath);
        var fileSizeBytes = fileInfo.Length;
        string sizeText = fileSizeBytes >= 1_048_576
            ? $"{fileSizeBytes / 1_048_576.0:F2} MB"
            : $"{fileSizeBytes / 1024.0:F1} KB";
        System.Diagnostics.Debug.WriteLine(sizeText);
        Image = localFilePath;
      }
    }
  }

  private void ScanBarcode()
  {
    ShowBarcodeScanner = !ShowBarcodeScanner;
  }

  // Ein oder mehrere Fotos aufnehmen bzw. wählen und von der KI erfassen lassen
  private async Task RecognizeAsync(bool fromCamera)
  {
    if (!AiSettings.IsConfigured && !_settingsService.GetUseLocalAi())
    {
      RecognitionHint = AppResources.AiNotConfigured;
      return;
    }

    var photos = fromCamera ? await CapturePhotosAsync() : await PickPhotosAsync();
    if (photos.Count == 0)
    {
      return;
    }

    // Erstes Foto (Vorderseite) als Produktbild übernehmen
    Image = photos[0].Path;

    await RunRecognitionAsync(() => _itemRecognitionService.RecognizeAsync(photos.Select(p => p.Input).ToList()));
  }

  // Bild für die KI verkleinern: schneller und günstiger, die Erkennung leidet darunter nicht
  private static MediaPickerOptions CreatePickerOptions(int selectionLimit = 1) =>
    new() { MaximumWidth = 1280, MaximumHeight = 1280, CompressionQuality = 85, SelectionLimit = selectionLimit };

  // Kamera: nach jedem Foto fragen, ob noch eins dazukommt (z. B. Rückseite mit Barcode und MHD)
  private async Task<List<(string Path, PhotoInput Input)>> CapturePhotosAsync()
  {
    var photos = new List<(string, PhotoInput)>();
    if (!MediaPicker.Default.IsCaptureSupported)
    {
      return photos;
    }

    while (photos.Count < 3)
    {
      var photo = await MediaPicker.Default.CapturePhotoAsync(CreatePickerOptions());
      if (photo is null)
      {
        break;
      }

      photos.Add(await CopyPhotoAsync(photo));
      if (photos.Count == 3 ||
          !await _alertService.DisplayAlert(AppResources.AnotherPhoto, AppResources.AnotherPhotoMessage, AppResources.YesAnswer, AppResources.NoAnswer))
      {
        break;
      }
    }

    return photos;
  }

  // Galerie: bis zu drei Fotos desselben Produkts auf einmal wählen
  private async Task<List<(string Path, PhotoInput Input)>> PickPhotosAsync()
  {
    var picked = await MediaPicker.Default.PickPhotosAsync(CreatePickerOptions(selectionLimit: 3));
    var photos = new List<(string, PhotoInput)>();
    foreach (var photo in picked.Take(3))
    {
      photos.Add(await CopyPhotoAsync(photo));
    }

    return photos;
  }

  // Foto in den Cache kopieren (Produktbild) und für die KI einlesen
  private static async Task<(string Path, PhotoInput Input)> CopyPhotoAsync(FileResult photo)
  {
    var localFilePath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}");
    await using (var source = await photo.OpenReadAsync())
    await using (var target = File.Create(localFilePath))
    {
      await source.CopyToAsync(target);
    }

    var mediaType = string.IsNullOrEmpty(photo.ContentType) ? "image/jpeg" : photo.ContentType;
    return (localFilePath, new PhotoInput(await File.ReadAllBytesAsync(localFilePath), mediaType));
  }

  // Per Sprache korrigieren ("Das sind zwei, die kommen in den Gefrierschrank") oder ganz ohne Foto erfassen
  private async Task SpeakCorrectionAsync()
  {
    if (IsListening)
    {
      // Zweites Tippen beendet das Zuhören
      await _voiceInputService.StopAsync();
      return;
    }

    string? spoken;
    try
    {
      IsListening = true;
      VoiceText = AppResources.Listening;
      spoken = await _voiceInputService.ListenAsync(text => VoiceText = $"🎤 {text}");
    }
    catch (Exception ex)
    {
      VoiceText = string.Format(AppResources.VoiceError, ex.Message);
      return;
    }
    finally
    {
      IsListening = false;
    }

    if (string.IsNullOrWhiteSpace(spoken))
    {
      VoiceText = string.Empty;
      return;
    }

    VoiceText = $"🎤 „{spoken}“";
    var currentForm = new ItemDraft
    {
      Name = Name,
      ExpirationDate = DateOnly.FromDateTime(ExpirationDate),
      StorageLocationId = SelectedLocation?.Id ?? string.Empty,
      Amount = Amount,
      DateKind = SelectedDateKind.Key,
    };

    await RunRecognitionAsync(() => _itemRecognitionService.CorrectAsync(spoken, currentForm));
  }

  // "Geöffnet": Haltbarkeit nach dem Öffnen von der KI, Datum wird nur verkürzt, nie verlängert
  private async Task MarkAsOpenedAsync()
  {
    try
    {
      IsRecognizing = true;
      RecognitionHint = AppResources.AiRecognizing;
      var (newDate, shelfLife, source) = await _itemAdvisorService.GetDateAfterOpeningAsync(
        Name, SelectedLocation?.Name ?? "?", ExpirationDate);
      ExpirationDate = newDate;
      SelectedDateKind = DateKinds.First(k => k.Key == ExpirationDateKind.Estimated);
      RecognitionHint = string.Format(AppResources.OpenedHint, shelfLife.Hint) + $"\n{source.SourceText}";
    }
    catch (Exception ex)
    {
      RecognitionHint = string.Format(AppResources.AiError, ex.Message);
    }
    finally
    {
      IsRecognizing = false;
    }
  }

  // Gemeinsamer Ablauf für Foto und Sprache: KI fragen, Formular füllen, Hinweise anzeigen
  private async Task RunRecognitionAsync(Func<Task<RecognitionResult>> recognize)
  {
    try
    {
      IsRecognizing = true;
      RecognitionHint = AppResources.AiRecognizing;

      var (draft, source, fromReplay) = await recognize();

      // Formular füllen. Gespeichert wird erst, wenn der Benutzer auf "Speichern" tippt.
      Name = draft.Name;
      ExpirationDate = draft.ExpirationDate.ToDateTime(TimeOnly.MinValue);
      SelectedLocation = StorageLocations.FirstOrDefault(l => l.Id == draft.StorageLocationId) ?? SelectedLocation;
      Amount = Math.Max(1, draft.Amount);
      SelectedDateKind = DateKinds.First(k => k.Key == draft.DateKind);
      if (!string.IsNullOrEmpty(draft.Barcode))
      {
        SearchText = draft.Barcode;
      }

      RecognitionHint = draft.DateKind != ExpirationDateKind.Estimated
        ? AppResources.AiDatePrinted
        : AppResources.AiDateEstimated;

      // Zeigen, welches Modell geantwortet hat (und warum ggf. nicht das lokale)
      RecognitionHint += $"\n{source.SourceText}";
      if (fromReplay)
      {
        RecognitionHint += $" · {AppResources.FromReplay}";
      }
      if (source.FallbackReason is not null)
      {
        RecognitionHint += $" · {source.FallbackReason}";
      }
    }
    catch (Exception ex)
    {
      RecognitionHint = string.Format(AppResources.AiError, ex.Message);
    }
    finally
    {
      IsRecognizing = false;
    }
  }

  private async Task SearchBarcode()
  {
    var response = await _openFoodFactsApiClient.GetProductByCodeAsync(SearchText);
    if (response is { Status: 1, Product: not null })
    {
      // Deutschen Namen bevorzugen, sonst Name in der Hauptsprache des Produkts
      Name = response.Product.PreferredName ?? string.Empty;

      // Prüfen ob ein Bild vorhanden ist
      if (!string.IsNullOrEmpty(response.Product.ImageUrl))
      {
        // Dateinamen und Pfad generieren
        var fileName = $"{response.Code}.jpg";
        var localFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        // Prüfen ob das Bild bereits heruntergeladen wurde
        if (!File.Exists(localFilePath))
        {
          // Falls das Bild lokale noch nicht existiert,
          // wird es heruntergeladen und gespeichert
          var imageBytes = await _openFoodFactsApiClient.DownloadImage(response.Product.ImageUrl);
          await File.WriteAllBytesAsync(localFilePath, imageBytes);
        }

        // Bildpfad zur Anzeige in der View setzen
        Image = localFilePath;
      }

    }
    else
    {
      // Zu einem späteren Zeitpunkt Fehlermeldung anzeigen
    }
  }
}

