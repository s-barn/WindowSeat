using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WindowSeat.App.Services;

namespace WindowSeat.App;

/// <summary>
/// Singleton command source for the tray icon context menu.
/// Implements INotifyPropertyChanged so the IsEnabled checkbox binding stays live.
/// </summary>
public sealed class TrayCommands : INotifyPropertyChanged
{
    public static readonly TrayCommands Instance = new();

    private TrayCommands()
    {
        OpenSettingsCommand  = new RelayCommand(() => GetApp().OpenSettings());
        ToggleEnabledCommand = new RelayCommand(() =>
        {
            GetApp().ToggleEnabled();
            OnPropertyChanged(nameof(IsEnabled));
        });
        CheckUpdatesCommand  = new RelayCommand(async () => await CheckUpdatesAsync());
        ExitCommand          = new RelayCommand(() => Application.Current.Shutdown());
    }

    public ICommand OpenSettingsCommand  { get; }
    public ICommand ToggleEnabledCommand { get; }
    public ICommand CheckUpdatesCommand  { get; }
    public ICommand ExitCommand          { get; }

    public bool IsEnabled => GetApp().IsEnabled;

    private static App GetApp() => (App)Application.Current;

    private static async Task CheckUpdatesAsync()
    {
        var update = await UpdateChecker.CheckAsync();
        if (update is null)
        {
            MessageBox.Show("WindowSeat is up to date.", "WindowSeat", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"{update.TagName} is available. Open the releases page?",
            "WindowSeat Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
            OpenUrl(update.HtmlUrl);
    }

    private static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* best effort */ }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Minimal ICommand implementation for tray menu actions.</summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public bool CanExecute(object? _) => true;
    public void Execute(object? _)    => _execute();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
