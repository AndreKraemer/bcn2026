using MauiAppDemoNet11Xaml.Models;

#if WINDOWS
using MauiAppDemoNet11Xaml.Behaviors;
#endif

namespace MauiAppDemoNet11Xaml.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();

            BindingContext = model;
#if WINDOWS
    		ProjectTasksCollectionView.Behaviors.Add(new SingleSelectionKeyboardGuardBehavior());
    		IconsCollectionView.Behaviors.Add(new SingleSelectionKeyboardGuardBehavior());
#endif
        }
    }
}
