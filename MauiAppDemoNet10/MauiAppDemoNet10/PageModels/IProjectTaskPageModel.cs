using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet10.Models;

namespace MauiAppDemoNet10.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}