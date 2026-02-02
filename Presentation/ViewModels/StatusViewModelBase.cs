using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.ViewModels;

public partial class StatusViewModelBase : ObservableObject
{
    private CancellationTokenSource? _statusCts;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _statusColor;

    public void SetStatus(string? message, string? color, int clearAfterMs = 3000)
    {
        StatusMessage = message;
        StatusColor = color;
        _ = ClearStatusAfterAsync(clearAfterMs);
    }

    public async Task ClearStatusAfterAsync(int ms = 3000)
    {
        _statusCts?.Cancel();
        _statusCts?.Dispose();
        _statusCts = new CancellationTokenSource();
        CancellationToken ctoken = _statusCts.Token;

        try
        {
            await Task.Delay(ms, ctoken);
            StatusMessage = null;
            StatusColor = null;
        }
        catch (TaskCanceledException) { }
    }
}