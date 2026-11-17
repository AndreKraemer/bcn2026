using MauiAppDemoNet11.Models;

#if WINDOWS
using MauiAppDemoNet11.Behaviors;
#endif

namespace MauiAppDemoNet11.Pages
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
