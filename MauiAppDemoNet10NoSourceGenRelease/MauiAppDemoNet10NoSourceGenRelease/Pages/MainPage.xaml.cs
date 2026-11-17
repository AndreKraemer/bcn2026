using MauiAppDemoNet10NoSourceGenRelease.Models;
using MauiAppDemoNet10NoSourceGenRelease.PageModels;

namespace MauiAppDemoNet10NoSourceGenRelease.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}