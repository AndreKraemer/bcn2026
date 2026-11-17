using MauiAppDemoNet10.Models;
using MauiAppDemoNet10.PageModels;

namespace MauiAppDemoNet10.Pages
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