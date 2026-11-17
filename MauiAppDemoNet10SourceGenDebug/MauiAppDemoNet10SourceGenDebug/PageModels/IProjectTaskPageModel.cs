using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet10SourceGenDebug.Models;

namespace MauiAppDemoNet10SourceGenDebug.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}