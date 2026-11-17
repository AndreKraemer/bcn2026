using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet10NoSourceGenDebug.Models;

namespace MauiAppDemoNet10NoSourceGenDebug.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}