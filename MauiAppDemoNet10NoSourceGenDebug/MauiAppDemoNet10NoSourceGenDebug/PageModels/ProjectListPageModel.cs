using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiAppDemoNet10NoSourceGenDebug.Data;
using MauiAppDemoNet10NoSourceGenDebug.Models;
using MauiAppDemoNet10NoSourceGenDebug.Services;

namespace MauiAppDemoNet10NoSourceGenDebug.PageModels
{
    public partial class ProjectListPageModel : ObservableObject
    {
        private readonly ProjectRepository _projectRepository;

        [ObservableProperty]
        private List<Project> _projects = [];

        [ObservableProperty]
        private Project? selectedProject;

        public ProjectListPageModel(ProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        [RelayCommand]
        private async Task Appearing()
        {
            Projects = await _projectRepository.ListAsync();
        }

        [RelayCommand]
        Task? NavigateToProject(Project project)
            => project is null ? Task.CompletedTask : Shell.Current.GoToAsync($"project?id={project.ID}");

        [RelayCommand]
        async Task AddProject()
        {
            await Shell.Current.GoToAsync($"project");
        }
    }
}