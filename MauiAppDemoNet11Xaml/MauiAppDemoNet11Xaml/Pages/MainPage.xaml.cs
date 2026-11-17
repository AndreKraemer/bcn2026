using MauiAppDemoNet11Xaml.Models;
using MauiAppDemoNet11Xaml.PageModels;

#if WINDOWS
using MauiAppDemoNet11Xaml.Behaviors;
#endif

namespace MauiAppDemoNet11Xaml.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
#if WINDOWS
    		ProjectsCollectionView.Behaviors.Add(new SingleSelectionKeyboardGuardBehavior());
    		TasksCollectionView.Behaviors.Add(new SingleSelectionKeyboardGuardBehavior());
#endif
        }
    }
}
