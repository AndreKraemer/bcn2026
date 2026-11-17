using MauiAppDemoNet10Xaml.Models;
using MauiAppDemoNet10Xaml.PageModels;

namespace MauiAppDemoNet10Xaml.Pages
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