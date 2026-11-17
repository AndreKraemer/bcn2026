using MauiAppDemoNet10NoSourceGenDebug.Models;

namespace MauiAppDemoNet10NoSourceGenDebug.Pages
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
