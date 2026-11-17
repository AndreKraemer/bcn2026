using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet11.Models;

namespace MauiAppDemoNet11.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}