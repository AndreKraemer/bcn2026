using MauiAppDemoNet10NoSourceGenRelease.Models;

namespace MauiAppDemoNet10NoSourceGenRelease.Pages
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
