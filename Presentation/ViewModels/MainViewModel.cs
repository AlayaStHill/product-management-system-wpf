using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.ViewModels;

public partial class MainViewModel : ObservableObject 
{
    [ObservableProperty]
    private ObservableObject _currentViewModel = null!; 
}








