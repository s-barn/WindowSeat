using System.Runtime.InteropServices;

namespace WindowSeat.Core;

/// <summary>
/// Listens for new top-level windows via SetWinEventHook (out-of-context, WINEVENT_SKIPOWNPROCESS).
/// Runs its own dedicated thread with a Win32 message pump so the hook fires reliably
/// without blocking the WPF UI thread.
/// </summary>
public sealed class WindowEventListener : IDisposable
{
    public event EventHandler<IntPtr>? WindowShown;

    private Thread?   _thread;
    private uint      _threadId;
    private IntPtr    _hookShow       = IntPtr.Zero;
    private IntPtr    _hookForeground = IntPtr.Zero;
    private bool      _disposed;

    // Keep delegate alive for the lifetime of the hook
    private NativeMethods.WinEventProc? _procDelegate;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _thread = new Thread(MessagePumpThread)
        {
            IsBackground = true,
            Name = "WindowSeat.HookThread"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void MessagePumpThread()
    {
        _threadId = (uint)Environment.CurrentManagedThreadId;
        _procDelegate = OnWinEvent;

        _hookShow = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_SHOW,
            NativeMethods.EVENT_OBJECT_SHOW,
            IntPtr.Zero, _procDelegate, 0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

        _hookForeground = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _procDelegate, 0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

        // Standard Win32 message pump
        while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }

        if (_hookShow != IntPtr.Zero)       NativeMethods.UnhookWinEvent(_hookShow);
        if (_hookForeground != IntPtr.Zero) NativeMethods.UnhookWinEvent(_hookForeground);
    }

    private void OnWinEvent(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime)
    {
        // Only care about real top-level windows
        if (hwnd == IntPtr.Zero) return;
        if (idObject != 0)       return; // OBJID_WINDOW == 0
        if (!NativeMethods.IsWindowVisible(hwnd)) return;

        WindowShown?.Invoke(this, hwnd);
    }

    public void Stop()
    {
        if (_threadId != 0)
            NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
