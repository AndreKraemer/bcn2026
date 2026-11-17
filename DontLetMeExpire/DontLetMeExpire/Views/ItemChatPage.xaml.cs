using DontLetMeExpire.ViewModels;

namespace DontLetMeExpire.Views;

[QueryProperty(nameof(ItemId), nameof(ItemId))]
public partial class ItemChatPage : ContentPage
{
  private readonly ItemChatViewModel _viewModel;

  public ItemChatPage(ItemChatViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = _viewModel = viewModel;
  }

  public string? ItemId { get; set; }

  protected override async void OnNavigatedTo(NavigatedToEventArgs args)
  {
    base.OnNavigatedTo(args);
    if (!string.IsNullOrEmpty(ItemId))
    {
      await _viewModel.InitializeAsync(ItemId);
    }
  }
}
