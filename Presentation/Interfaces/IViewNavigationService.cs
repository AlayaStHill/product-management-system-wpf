using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.Interfaces; 
public interface IViewNavigationService
{
    void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : ObservableObject;
    Task NavigateToAsync<TViewModel>(Func<TViewModel, Task>? configure = null) where TViewModel : ObservableObject;
}


