using MauiAppDemoNet10SourceGenDebug.Models;
using MauiAppDemoNet10SourceGenDebug.PageModels;

namespace MauiAppDemoNet10SourceGenDebug.Pages
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