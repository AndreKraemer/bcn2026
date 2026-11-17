#if WINDOWS
using MauiAppDemoNet11.Behaviors;
#endif
namespace MauiAppDemoNet11.Pages
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
