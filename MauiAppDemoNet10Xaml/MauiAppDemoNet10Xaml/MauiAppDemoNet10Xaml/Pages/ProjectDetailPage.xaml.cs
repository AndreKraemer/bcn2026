using MauiAppDemoNet10Xaml.Models;

namespace MauiAppDemoNet10Xaml.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();

            BindingContext = model;
        }
    }
}
