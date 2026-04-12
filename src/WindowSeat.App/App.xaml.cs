using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using WindowSeat.App.Services;
using WindowSeat.App.Views;
using WindowSeat.Core;
using WindowSeat.Core.Detection;
using WindowSeat.Settings;

namespace WindowSeat.App;

public partial class App : Application
{
    private TaskbarIcon?        _trayIcon;
    private WindowEventListener? _listener;
    private WindowMover?        _mover;
    private SettingsService     _settingsService = new();
    private SettingsWindow?     _settingsWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService.Load();
        var settings = _settingsService.Current;

        // Sync startup registry key with persisted setting
        StartupService.Sync(settings.LaunchAtStartup);

        // Wire up core components
        IMonitorDetector detector = settings.DetectionMethod == DetectionMethod.CursorPosition
            ? new CursorMonitorDetector()
            : new TaskbarMonitorDetector();

        _mover    = new WindowMover(detector);
        _listener = new WindowEventListener();
        _listener.WindowShown += OnWindowShown;
        _listener.Start();

        // Build tray icon
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

        // Check for updates in background (non-blocking)
        if (settings.CheckForUpdates)
        {
            var update = await UpdateChecker.CheckAsync();
            if (update is not null)
                _trayIcon.ShowBalloonTip(
                    "WindowSeat update available",
                    $"{update.TagName} is available on GitHub.",
                    BalloonIcon.Info);
        }
    }

    private void OnWindowShown(object? sender, IntPtr hwnd)
    {
        if (_settingsService.Current.IsEnabled)
            _mover?.TryMoveWindow(hwnd);
    }

    // ── Tray menu commands ────────────────────────────────────────────────────

    public void OpenSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_settingsService);
        _settingsWindow.Show();
    }

    public void ToggleEnabled()
    {
        _settingsService.Update(s => s.IsEnabled = !s.IsEnabled);
    }

    public bool IsEnabled => _settingsService.Current.IsEnabled;

    protected override void OnExit(ExitEventArgs e)
    {
        _listener?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
