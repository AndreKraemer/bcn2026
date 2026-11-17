using MauiAppDemoNet10SourceGenDebug.Models;

namespace MauiAppDemoNet10SourceGenDebug.Pages
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
