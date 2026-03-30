namespace WindowSeat.Settings;

/// <summary>
/// Persisted user settings for WindowSeat.
/// Serialised to %AppData%\WindowSeat\settings.json.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Schema version — used for future migrations.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Whether WindowSeat is actively repositioning windows.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Which strategy to use when determining the target monitor.</summary>
    public DetectionMethod DetectionMethod { get; set; } = DetectionMethod.CursorPosition;

    /// <summary>Whether to launch WindowSeat automatically at Windows startup.</summary>
    public bool LaunchAtStartup { get; set; } = true;

    /// <summary>Whether to check for new GitHub releases on launch.</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// Process names (e.g. "Spotify.exe") that WindowSeat will not reposition.
    /// Comparison is case-insensitive.
    /// </summary>
    public List<string> ExcludedProcessNames { get; set; } = new();
}

public enum DetectionMethod
{
    /// <summary>Move new windows to whichever monitor the cursor is on at launch time.</summary>
    CursorPosition,

    /// <summary>Move new windows to the monitor whose taskbar was most recently interacted with.</summary>
    LastActiveTaskbar
}
