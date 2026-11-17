using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet10NoSourceGenRelease.Models;

namespace MauiAppDemoNet10NoSourceGenRelease.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}