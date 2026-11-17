using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet10Xaml.Models;

namespace MauiAppDemoNet10Xaml.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}