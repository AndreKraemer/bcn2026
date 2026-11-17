using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet11Xaml.Models;

namespace MauiAppDemoNet11Xaml.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}