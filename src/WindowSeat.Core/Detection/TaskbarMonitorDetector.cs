using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowSeat.Core.Detection;

/// <summary>
/// Determines the target monitor based on which taskbar was most recently interacted with.
/// Falls back to the cursor position if no taskbar interaction has been recorded.
/// </summary>
public sealed class TaskbarMonitorDetector : IMonitorDetector
{
    private volatile IntPtr _lastActiveTaskbarMonitor = IntPtr.Zero;
    private readonly CursorMonitorDetector _fallback = new();

    /// <summary>
    /// Called by the window event listener when taskbar interaction is detected.
    /// </summary>
    public void OnTaskbarInteraction(IntPtr monitorHandle)
    {
        _lastActiveTaskbarMonitor = monitorHandle;
    }

    public Rectangle? GetTargetMonitorBounds()
    {
        var monitor = _lastActiveTaskbarMonitor;

        if (monitor == IntPtr.Zero)
            return _fallback.GetTargetMonitorBounds();

        var info = new NativeMethods.MONITORINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            return _fallback.GetTargetMonitorBounds();

        var r = info.rcWork;
        return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
    }
}
