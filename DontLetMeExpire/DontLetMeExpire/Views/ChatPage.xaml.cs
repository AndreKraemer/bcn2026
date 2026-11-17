using DontLetMeExpire.ViewModels;

namespace DontLetMeExpire.Views;

public partial class ChatPage : ContentPage
{
  private readonly ChatViewModel _viewModel;

  public ChatPage(ChatViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = _viewModel = viewModel;
  }

  protected override void OnNavigatedTo(NavigatedToEventArgs args)
  {
    base.OnNavigatedTo(args);
    _viewModel.Initialize();
  }
}
