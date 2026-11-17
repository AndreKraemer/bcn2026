#if WINDOWS
using MauiAppDemoNet11Xaml.Behaviors;
#endif
namespace MauiAppDemoNet11Xaml.Pages
{
    public partial class ProjectListPage : ContentPage
    {
        public ProjectListPage(ProjectListPageModel model)
        {
            BindingContext = model;
            InitializeComponent();
#if WINDOWS
    		ProjectsCollectionView.Behaviors.Add(new SingleSelectionKeyboardGuardBehavior());
#endif
        }
    }
}
