using DontLetMeExpire.Models;
using DontLetMeExpire.Resources.Strings;
using DontLetMeExpire.Services;
using DontLetMeExpire.Views;
using Microsoft.Extensions.AI;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DontLetMeExpire.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IItemService _itemService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly AI.RecipeService _recipeService;
    private readonly IAlertService _alertService;
    private IReadOnlyList<Item> _usedItems = [];
    private int _stockCount;
    private int _expiringSoonCount;
    private int _expiresTodayCount;
    private int _expiredCount;
    private string _recipeResult = string.Empty;
    private bool _isBottomSheetOpen;
    private bool _isGeneratingRecipe;

    public MainViewModel(INavigationService navigationService, IItemService itemService, IStorageLocationService storageLocationService, AI.RecipeService recipeService, IAlertService alertService)
    {
        _itemService = itemService;
        _storageLocationService = storageLocationService;
        _recipeService = recipeService;
        _alertService = alertService;
        CookedCommand = new Command(async () => await MarkAsCookedAsync(), () => HasUsedItems && !IsGeneratingRecipe);
        _navigationService = navigationService;
        CreateSuggestedLocationsCommand = new Command(async () => await CreateSuggestedLocations());
        CreateSuggestedLocationsAndStockCommand = new Command(async () => await CreateSuggestedLocationsAndStock());
        NavigateToAddItemCommand = new Command(async () => await NavigateToAddItem(), CanAddNewItem);
        NavigateToStockCommand = new Command(async () => await NavigateToStock());
        NavigateToExpiringSoonCommand = new Command(async () => await NavigateToExpiringSoon());
        NavigateToExpiresTodayCommand = new Command(async () => await NavigateToExpiresToday());
        NavigateToExpiredCommand = new Command(async () => await NavigateToExpired());
        NavigateToLocationCommand = new Command<string>(async (locationId) => await NavigateToLocation(locationId));
        GenerateRecipeCommand = new Command(async () => await GenerateRecipe(), CanGenerateRecipe);
        CloseBottomSheetCommand = new Command(() => IsBottomSheetOpen = false);
    }

    public ICommand NavigateToAddItemCommand { get; }
    public ICommand GenerateRecipeCommand { get; }
    public ICommand CloseBottomSheetCommand { get; }

    /// <summary>Trägt die Artikel aus, die das Rezept vollständig verbraucht.</summary>
    public Command CookedCommand { get; }

    /// <summary>Hat die KI verbrauchte Artikel ermittelt? Dann wird der Button "Gekocht" angezeigt.</summary>
    public bool HasUsedItems => _usedItems.Count > 0;

    /// <summary>
    /// Die Anzahl aller gelagerten Artikel.
    /// </summary>
    public int StockCount
    {
        get => _stockCount;
        set => SetProperty(ref _stockCount, value);
    }

    /// <summary>
    /// Die Anzahl der Artikel, die bald ablaufen.
    /// </summary>
    public int ExpiringSoonCount
    {
        get => _expiringSoonCount;
        set => SetProperty(ref _expiringSoonCount, value);
    }

    /// <summary>
    /// Die Anzahl der Artikel, die heute ablaufen.
    /// </summary>
    public int ExpiresTodayCount
    {
        get => _expiresTodayCount;
        set => SetProperty(ref _expiresTodayCount, value);
    }

    /// <summary>
    /// Die Anzahl der abgelaufenen Artikel.
    /// </summary>
    public int ExpiredCount
    {
        get => _expiredCount;
        set => SetProperty(ref _expiredCount, value);
    }

    /// <summary>
    /// Das Ergebnis der Rezeptgenerierung.
    /// </summary>
    public string RecipeResult
    {
        get => _recipeResult;
        set => SetProperty(ref _recipeResult, value);
    }

    /// <summary>
    /// Gibt an, ob das Bottom Sheet geöffnet ist.
    /// </summary>
    public bool IsBottomSheetOpen
    {
        get => _isBottomSheetOpen;
        set => SetProperty(ref _isBottomSheetOpen, value);
    }

    /// <summary>
    /// Gibt an, ob gerade ein Rezept generiert wird.
    /// </summary>
    public bool IsGeneratingRecipe
    {
        get => _isGeneratingRecipe;
        // Button während der Generierung sperren
        set => SetProperty(ref _isGeneratingRecipe, value, () =>
        {
            UpdateRecipeAvailability();
            CookedCommand.ChangeCanExecute();
        });
    }

    public ICommand NavigateToStockCommand { get; }
    public ICommand NavigateToExpiringSoonCommand { get; }
    public ICommand NavigateToExpiresTodayCommand { get; }
    public ICommand NavigateToExpiredCommand { get; }

    public ICommand CreateSuggestedLocationsCommand { get; }
    public ICommand CreateSuggestedLocationsAndStockCommand { get; }
    public Command<string> NavigateToLocationCommand { get; }

    public ObservableCollection<StorageLocationWithItemCount> StorageLocations { get; } = [];

    /// <summary>Für die KI-Rezept-Karte: ausgegraut, solange es nichts zu kochen gibt.</summary>
    public bool CanCreateRecipe => CanGenerateRecipe();

    /// <summary>Untertitel der KI-Rezept-Karte.</summary>
    public string RecipeCardSubtitle => ExpiringSoonCount > 0
        ? string.Format(AppResources.RecipeCardSubtitle, ExpiringSoonCount)
        : AppResources.RecipeCardNothing;

    /// <summary>
    /// Initialisiert das ViewModel asynchron.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            var locations = await _storageLocationService.GetWithItemCountAsync();
            StorageLocations.Clear();
            foreach (var location in locations)
            {
                StorageLocations.Add(location);
            }

            StockCount = (await _itemService.GetAsync()).Count();
            ExpiringSoonCount = (await _itemService.GetExpiresSoonAsync()).Count();
            ExpiresTodayCount = (await _itemService.GetExpiresTodayAsync()).Count();
            ExpiredCount = (await _itemService.GetExpiredAsync()).Count();

            ((Command)NavigateToAddItemCommand).ChangeCanExecute();
            UpdateRecipeAvailability();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task NavigateToStock()
    {
        await NavigateToItemsPage("Stock");
    }

    private async Task NavigateToExpiringSoon()
    {
        await NavigateToItemsPage("ExpiringSoon");
    }

    private async Task NavigateToExpiresToday()
    {
        await NavigateToItemsPage("ExpiresToday");
    }

    private async Task NavigateToExpired()
    {
        await NavigateToItemsPage("Expired");
    }

    private async Task NavigateToLocation(string locationId)
    {
        await NavigateToItemsPage("Location", locationId);
    }

    private async Task NavigateToItemsPage(string displayMode, string? locationId = null)
    {
        var navigationParams = new Dictionary<string, object>
        {
            { "DisplayMode", displayMode },
            { "LocationId", locationId }
        };
        await _navigationService.GoToAsync(nameof(ItemsPage), navigationParams);
    }

    private async Task NavigateToAddItem()
    {
        await _navigationService.GoToAsync(nameof(ItemPage));
    }

    private bool CanAddNewItem()
    {
        return StorageLocations.Any();
    }

    private bool CanGenerateRecipe()
    {
        return ExpiringSoonCount > 0 && !IsGeneratingRecipe;
    }

    private void UpdateRecipeAvailability()
    {
        ((Command)GenerateRecipeCommand).ChangeCanExecute();
        OnPropertyChanged(nameof(CanCreateRecipe));
        OnPropertyChanged(nameof(RecipeCardSubtitle));
    }

    private async Task GenerateRecipe()
    {
        try
        {
            IsGeneratingRecipe = true;
            SetUsedItems([]);
            RecipeResult = AppResources.RecipeGenerating;
            IsBottomSheetOpen = true;

            // Holen der bald ablaufenden Artikel
            var expiringItems = (await _itemService.GetExpiresSoonAsync()).ToList();
            if (expiringItems.Count == 0)
            {
                RecipeResult = AppResources.RecipeNothingExpiring;
                return;
            }

            // 1. Rezept gestreamt erzeugen: der Text erscheint, während das Modell schreibt.
            //    Cloud oder lokal (Apple Intelligence), je nach Einstellung und Gerät.
            string footer = string.Empty;
            var session = await _recipeService.StreamRecipeAsync(expiringItems, text => RecipeResult = text + footer);
            footer = $"\n\n---\n*{session.Source.SourceText}*" + (session.Source.FallbackReason is null ? "" : $" · *{session.Source.FallbackReason}*");
            var recipe = RecipeResult;
            RecipeResult = recipe + footer;

            // 2. In derselben Unterhaltung strukturiert nachfragen, was vollständig verbraucht wird
            SetUsedItems(await _recipeService.GetFullyUsedItemsAsync(session));
        }
        catch (Exception ex)
        {
            RecipeResult = string.Format(AppResources.RecipeError, ex.Message);
        }
        finally
        {
            IsGeneratingRecipe = false;
        }
    }

    private void SetUsedItems(IReadOnlyList<Item> items)
    {
        _usedItems = items;
        OnPropertyChanged(nameof(HasUsedItems));
        CookedCommand.ChangeCanExecute();
    }

    // "Gekocht": verbrauchte Artikel nach Rückfrage aus dem Vorrat austragen
    private async Task MarkAsCookedAsync()
    {
        var list = string.Join("\n", _usedItems.Select(i => $"• {i.Name}"));
        if (!await _alertService.DisplayAlert(AppResources.Cooked, $"{AppResources.CookedConfirm}\n\n{list}", AppResources.YesAnswer, AppResources.NoAnswer))
        {
            return;
        }

        foreach (var item in _usedItems)
        {
            await _itemService.DeleteAsync(item);
        }

        SetUsedItems([]);
        IsBottomSheetOpen = false;
        await InitializeAsync();
    }

    private async Task CreateSuggestedLocations()
    {
        foreach (var location in DummyData.Locations)
        {
            await _storageLocationService.SaveAsync(location);
        }
        await InitializeAsync();
    }

    private async Task CreateSuggestedLocationsAndStock()
    {
        foreach (var location in DummyData.Locations)
        {
            await _storageLocationService.SaveAsync(location);
        }

        foreach (var item in DummyData.Items)
        {
            await _itemService.SaveAsync(item);
        }

        await InitializeAsync();
    }
}

