using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Interfaces;
using Presentation.ViewModels;

namespace Presentation.Services;

public class ViewNavigationService : IViewNavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;

    public ViewNavigationService(IServiceProvider serviceProvider, MainViewModel mainViewModel)
    {
        _serviceProvider = serviceProvider;
        _mainViewModel = mainViewModel;
    }

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : ObservableObject
    {
        TViewModel viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        configure?.Invoke(viewModel);

        _mainViewModel.CurrentViewModel = viewModel;
    }

    public async Task NavigateToAsync<TViewModel>(Func<TViewModel, Task>? configure = null) where TViewModel : ObservableObject
    {
        TViewModel viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        if (configure is not null)
            await configure(viewModel);

        _mainViewModel.CurrentViewModel = viewModel;
    }
}

