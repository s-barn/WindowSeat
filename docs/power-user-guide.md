# WindowSeat Power User Guide

This guide covers advanced configuration, the settings file schema, known limitations, and how to run WindowSeat from source.

---

## Settings File

WindowSeat stores all settings in a human-readable JSON file:

```
%AppData%\WindowSeat\settings.json
```

On a typical system this resolves to:

```
C:\Users\<YourUsername>\AppData\Roaming\WindowSeat\settings.json
```

You can edit this file directly in any text editor. WindowSeat reads settings on startup. If the file is missing or malformed, WindowSeat recreates it with defaults.

### Full Schema Reference

```json
{
  "SchemaVersion": 1,
  "IsEnabled": true,
  "DetectionMethod": "CursorPosition",
  "LaunchAtStartup": true,
  "CheckForUpdates": true,
  "ExcludedProcessNames": [
    "Spotify.exe",
    "Discord.exe"
  ]
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `SchemaVersion` | integer | `1` | Internal version — do not modify |
| `IsEnabled` | boolean | `true` | Master on/off switch |
| `DetectionMethod` | string | `"CursorPosition"` | `"CursorPosition"` or `"LastActiveTaskbar"` |
| `LaunchAtStartup` | boolean | `true` | Controls the Windows startup registry key |
| `CheckForUpdates` | boolean | `true` | Check GitHub Releases API once per session |
| `ExcludedProcessNames` | string array | `[]` | Process names to ignore (case-insensitive) |

---

## Startup Registry Key

When **Launch at startup** is enabled, WindowSeat writes the following registry value:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
Name:  WindowSeat
Value: "C:\...\WindowSeat.exe"
```

This is a per-user key (`HKCU`) — no administrator rights are required. Toggling the setting in the UI adds or removes this value cleanly.

---

## Detection Methods — Technical Detail

### Cursor Position

On each `EVENT_OBJECT_SHOW` event, WindowSeat calls `GetCursorPos` to get the current mouse coordinates and `MonitorFromPoint` to identify which monitor those coordinates fall on. The window is then moved to the work area (excluding taskbar) of that monitor using `SetWindowPos`.

This method is the most reliable across all application types and is recommended for most users.

### Last Active Taskbar

WindowSeat watches for `EVENT_SYSTEM_FOREGROUND` events on taskbar window class names (`Shell_TrayWnd`, `Shell_SecondaryTrayWnd`) to track which monitor's taskbar was most recently interacted with. It falls back to cursor position if no taskbar interaction has been recorded in the current session.

This method suits workflows where you tend to launch apps from pinned taskbar icons rather than from the Start menu or keyboard shortcuts.

---

## Known Limitations

### UWP / Microsoft Store Apps

Apps built on the Universal Windows Platform (UWP) use a windowing model that resists programmatic repositioning via `SetWindowPos`. WindowSeat detects these by window class name and skips them rather than attempting a move that would fail or behave unpredictably.

Affected class names (skipped by default):
- `Windows.UI.Core.CoreWindow`
- `ApplicationFrameWindow`

### Apps with Internal Window Placement Logic

Some apps save and immediately restore their own window position after `SetWindowPos` is called, effectively undoing the move. Common examples include:

- Some Electron apps (Teams, Slack, VS Code in certain configurations)
- Apps that use `RegisterWindowMessage` + `WM_SETTINGCHANGE` to track position
- Apps using WinUI 3 with custom placement

For these apps, add them to the **exclusion list** and manage their placement manually (move the window once to the right monitor, then close it — most apps remember the last position on next launch).

### Multi-Monitor Scaling

WindowSeat moves windows to the top-left of the target monitor's work area, preserving the window's current size. On mixed-DPI setups (e.g. a 4K display at 200% next to a 1080p display at 100%), the window may appear a different physical size after moving due to DPI scaling. This is a Windows DPI behaviour, not a WindowSeat limitation.

---

## Running from Source

```powershell
git clone https://github.com/stmba/WindowSeat.git
cd WindowSeat
dotnet run --project src/WindowSeat.App/WindowSeat.App.csproj
```

Or build a local release package:

```powershell
.\scripts\build.ps1 -Package
```

This produces `WindowSeat-local-win-x64.zip` in the repo root.

See [contributing.md](contributing.md) for full developer setup instructions.

---

## Diagnostic Tracing

WindowSeat writes lightweight diagnostic messages via `System.Diagnostics.Trace`. To capture these, attach [DebugView](https://learn.microsoft.com/en-us/sysinternals/downloads/debugview) (Sysinternals) and filter on `[WindowSeat]`. This is useful for verifying which windows are being moved or skipped.
