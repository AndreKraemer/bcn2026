using MauiAppDemoNet11.Models;
using MauiAppDemoNet11.PageModels;

#if WINDOWS
using MauiAppDemoNet11.Behaviors;
#endif

namespace MauiAppDemoNet11.Pages
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
