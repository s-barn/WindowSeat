using Microsoft.Win32;

namespace WindowSeat.Settings;

/// <summary>
/// Manages the HKCU Run registry key that launches WindowSeat at Windows startup.
/// Uses HKCU (current user) — no elevation required.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName    = "WindowSeat";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(AppName) is not null;
    }

    public static void Enable()
    {
        var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine process path.");
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException($"Cannot open registry key: {RunKeyPath}");
        key.SetValue(AppName, $"\"{exePath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    public static void Sync(bool shouldBeEnabled)
    {
        if (shouldBeEnabled) Enable();
        else Disable();
    }
}
