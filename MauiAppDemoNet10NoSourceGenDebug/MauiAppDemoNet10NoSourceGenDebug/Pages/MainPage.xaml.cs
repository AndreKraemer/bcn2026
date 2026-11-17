using MauiAppDemoNet10NoSourceGenDebug.Models;
using MauiAppDemoNet10NoSourceGenDebug.PageModels;

namespace MauiAppDemoNet10NoSourceGenDebug.Pages
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