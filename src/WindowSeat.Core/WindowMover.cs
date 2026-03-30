using System.Drawing;
using System.Text;
using WindowSeat.Core.Detection;

namespace WindowSeat.Core;

/// <summary>
/// Moves a newly shown window to the target monitor returned by the active IMonitorDetector.
/// UWP / immersive shell windows are detected and skipped gracefully.
/// </summary>
public sealed class WindowMover
{
    private readonly IMonitorDetector _detector;

    // Window classes that resist or should not be repositioned
    private static readonly HashSet<string> SkippedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows.UI.Core.CoreWindow",   // UWP core window
        "ApplicationFrameWindow",       // UWP app frame
        "Shell_TrayWnd",                // Taskbar
        "Shell_SecondaryTrayWnd",       // Secondary taskbar
        "Progman",                      // Desktop
        "WorkerW",                      // Desktop worker
        "tooltips_class32",             // Tooltips
        "SysShadow",                    // Window shadows
        "#32769",                       // Desktop window class
    };

    public WindowMover(IMonitorDetector detector)
    {
        _detector = detector;
    }

    /// <summary>
    /// Attempts to move the given window handle to the target monitor.
    /// Returns false if the window was skipped or the move failed.
    /// </summary>
    public bool TryMoveWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;

        var className = GetWindowClass(hwnd);
        if (SkippedClasses.Contains(className))
        {
            Log($"Skipped {hwnd} (class: {className})");
            return false;
        }

        var targetBounds = _detector.GetTargetMonitorBounds();
        if (targetBounds is null) return false;

        if (!NativeMethods.GetWindowRect(hwnd, out var windowRect)) return false;

        var currentMonitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var targetMonitor  = NativeMethods.MonitorFromPoint(
            new NativeMethods.POINT { x = targetBounds.Value.X + 1, y = targetBounds.Value.Y + 1 },
            NativeMethods.MONITOR_DEFAULTTONEAREST);

        // Already on the right monitor — nothing to do
        if (currentMonitor == targetMonitor) return false;

        int windowWidth  = windowRect.right  - windowRect.left;
        int windowHeight = windowRect.bottom  - windowRect.top;

        // Place at top-left of the target monitor's work area, preserving window size
        int newX = targetBounds.Value.Left;
        int newY = targetBounds.Value.Top;

        bool moved = NativeMethods.SetWindowPos(
            hwnd, IntPtr.Zero,
            newX, newY, windowWidth, windowHeight,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

        if (moved)
            Log($"Moved {hwnd} (class: {className}) to {newX},{newY}");
        else
            Log($"SetWindowPos failed for {hwnd} (class: {className})");

        return moved;
    }

    private static string GetWindowClass(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    // Lightweight diagnostic trace — replace with a proper logger if desired
    private static void Log(string message) =>
        System.Diagnostics.Trace.WriteLine($"[WindowSeat] {message}");
}
