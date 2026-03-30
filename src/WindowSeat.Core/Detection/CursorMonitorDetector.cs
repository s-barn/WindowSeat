using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowSeat.Core.Detection;

/// <summary>
/// Determines the target monitor based on the current cursor position.
/// </summary>
public sealed class CursorMonitorDetector : IMonitorDetector
{
    public Rectangle? GetTargetMonitorBounds()
    {
        if (!NativeMethods.GetCursorPos(out var point))
            return null;

        var monitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
            return null;

        var info = new NativeMethods.MONITORINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            return null;

        var r = info.rcWork;
        return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
    }
}
