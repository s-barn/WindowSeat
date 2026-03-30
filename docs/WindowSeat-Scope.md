# WindowSeat — Project Scope Document

**Version:** 0.1 (Pre-Development)  
**Last Updated:** 2026-03-30  
**License:** MIT  
**Target Platform:** Windows 11 (primary), Windows 10 22H2+ (best effort)  
**Stack:** .NET 8 LTS, C#, WPF, PowerShell (build/packaging tooling)

---

## Overview

WindowSeat is a lightweight Windows utility that ensures new application windows open on the monitor you are actively working on. It runs silently in the system tray and requires no manual window repositioning from the user.

The problem it solves: Windows 11 has no native setting to open windows on the monitor where the Start menu was clicked or where the cursor is. WindowSeat fills that gap with a configurable, unobtrusive background process.

---

## Goals

- **Easy to install** — MSIX package distributed via GitHub Releases; no technical knowledge required
- **Easy to use** — runs silently in the tray after install; sensible defaults require zero configuration for most users
- **Configurable** — power users can tune detection method, exclusions, and startup behavior via a clean WPF settings UI
- **Secure** — no network calls except GitHub release version checks; no telemetry; no data collection; settings stored locally
- **Well documented** — README and docs written for both casual and power users
- **Publicly maintainable** — clean OSS project structure suitable for community contributions

---

## MVP Feature Set (v1.0)

### Core Behavior
- Monitor `SetWinEventHook` (Win32 API via P/Invoke) for `EVENT_OBJECT_SHOW` and `EVENT_SYSTEM_FOREGROUND` events
- On new window detection, determine the target monitor using the user's selected detection method
- Reposition the new window to the target monitor using `SetWindowPos` (Win32 API via P/Invoke)
- Graceful handling of UWP/Store apps that resist programmatic repositioning (log and skip, no crash)

### Detection Methods (User Configurable)
| Method | Behavior |
|---|---|
| Cursor Position | Move new window to whichever monitor the mouse cursor is on at launch time |
| Last Active Taskbar | Move new window to the monitor whose taskbar was most recently interacted with |

### System Tray
- Minimal tray icon with right-click context menu
- Menu options: Open Settings, Enable/Disable WindowSeat, Check for Updates, Exit
- Visual indicator in tray icon when WindowSeat is disabled (greyed icon)

### Settings UI (WPF)
- Detection method selector (Cursor Position / Last Active Taskbar)
- Enable/disable WindowSeat toggle
- Launch at Windows startup toggle (enabled by default)
- Per-app exclusions list (add/remove by process name or browsing for executable)
- Update notification toggle
- "About" section with version, license, and link to GitHub repo

### Per-App Exclusions
- Users can specify process names (e.g. `Spotify.exe`) that WindowSeat will ignore
- Exclusions stored in settings JSON
- Add via text entry or file browser in Settings UI

### Auto-Start
- Enabled by default on first run
- Implemented via Windows startup registry key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)
- Toggled cleanly from Settings UI (adds/removes the registry key)

### Update Notifications
- On launch, WindowSeat checks the GitHub Releases API for a newer version tag
- If a newer version is available, a non-intrusive tray balloon notification is shown
- Notification links to the GitHub Releases page; no auto-download or auto-install
- Check is performed once per session, not on a recurring timer
- Can be disabled in Settings UI

### Settings Storage
- Location: `%AppData%\WindowSeat\settings.json`
- Human-readable JSON format
- Schema versioned to support future migrations
- No registry-based settings (aside from the startup key)

---

## Out of Scope for v1.0

The following are explicitly deferred to future versions to keep MVP scope manageable:

- Multi-rule window routing (e.g. "always open Outlook on monitor 2")
- Per-app monitor pinning (beyond exclusions)
- Window size/position memory per app
- Hotkey support for manual window moving
- Auto-update (download and install) — v1.0 notifies only
- Dark/light mode theming in Settings UI
- Localization / non-English language support
- Windows 10 MSIX packaging validation (best effort only)

---

## Architecture Overview

```
WindowSeat.sln
├── WindowSeat.App          # WPF application — tray icon, settings UI, entry point
├── WindowSeat.Core         # Core logic — window event hooks, monitor detection, window mover
├── WindowSeat.Settings     # Settings model, JSON serialization, schema versioning
└── WindowSeat.Tests        # Unit tests (xUnit) for Core and Settings logic
```

### Key Components

**WindowEventListener** (Core)
- Wraps `SetWinEventHook` / `UnhookWinEvent` P/Invoke
- Raises C# events on new window creation
- Runs on a dedicated thread with its own message pump

**MonitorDetector** (Core)
- Implements both detection strategies behind a common `IMonitorDetector` interface
- `CursorMonitorDetector` — calls `GetCursorPos` + `MonitorFromPoint`
- `TaskbarMonitorDetector` — tracks last taskbar interaction via additional hooks

**WindowMover** (Core)
- Calls `SetWindowPos` to reposition windows
- Contains UWP detection logic (checks window class name) to skip gracefully
- Logs skip events for diagnostics

**SettingsService** (Settings)
- Reads/writes `settings.json`
- Handles schema version migrations
- Provides strongly-typed `AppSettings` model

**TrayApplication** (App)
- WPF app running without a main window
- Manages `NotifyIcon` lifecycle
- Hosts settings window on demand

**UpdateChecker** (App)
- Calls `https://api.github.com/repos/[owner]/WindowSeat/releases/latest`
- Compares tag version to `Assembly.GetExecutingAssembly().GetName().Version`
- Fires notification if newer version found

---

## Security Considerations

- **No telemetry, no analytics, no data collection of any kind**
- **Network access:** GitHub Releases API only, one call per session, can be disabled
- **No elevated privileges required** — runs as standard user
- **MSIX packaging** provides install isolation and clean uninstall
- **Code signing:** GitHub Actions pipeline will sign the MSIX with a self-signed certificate for v1.0; a trusted certificate (e.g. SignPath.io free tier for OSS) to be evaluated for v1.1
- **Win32 hooks scope:** `SetWinEventHook` is called out-of-context and scoped to window show/foreground events only — no keyboard or mouse hook
- **Settings file:** stored in user's own `%AppData%` — no shared or elevated paths

---

## Project Structure (GitHub Repository)

```
WindowSeat/
├── .github/
│   ├── workflows/
│   │   ├── build.yml           # CI — build and test on push/PR
│   │   └── release.yml         # CD — build, sign, package MSIX on tag push
│   └── ISSUE_TEMPLATE/
│       ├── bug_report.md
│       └── feature_request.md
├── src/
│   ├── WindowSeat.App/
│   ├── WindowSeat.Core/
│   ├── WindowSeat.Settings/
│   └── WindowSeat.Tests/
├── docs/
│   ├── user-guide.md           # Casual user setup and usage guide
│   ├── power-user-guide.md     # Advanced configuration, exclusions, settings file reference
│   └── contributing.md         # Developer setup, build instructions, PR guidelines
├── packaging/
│   └── WindowSeat.wapproj      # Windows Application Packaging Project for MSIX
├── scripts/
│   └── build.ps1               # Local build and packaging script
├── README.md
├── CHANGELOG.md
├── LICENSE                     # MIT
└── WindowSeat.sln
```

---

## Documentation Plan

### README.md
- What WindowSeat does (one paragraph, non-technical)
- Screenshot of tray icon and settings UI
- Quick install instructions (download MSIX from Releases, run)
- Link to full user guide
- Link to contributing guide

### User Guide (docs/user-guide.md)
- System requirements
- Installation walkthrough with screenshots
- First-run behavior explanation
- Settings UI walkthrough
- How to add an exclusion
- How to disable/re-enable
- Uninstall instructions

### Power User Guide (docs/power-user-guide.md)
- Detection method explanation and trade-offs
- `settings.json` schema reference with all fields documented
- Known limitations (UWP apps, Win32 apps with internal window placement logic)
- How to run from source

### Contributing Guide (docs/contributing.md)
- Prerequisites (.NET 8 SDK, Visual Studio 2022 or VS Code)
- Clone and build instructions
- Project architecture overview
- PR and issue guidelines

---

## Build and Release Pipeline

### CI (GitHub Actions — build.yml)
- Triggers on push to `main` and all pull requests
- Steps: restore → build → test
- Publishes test results as workflow artifact

### CD (GitHub Actions — release.yml)
- Triggers on push of a version tag (e.g. `v1.0.0`)
- Steps: restore → build → test → publish → package MSIX → attach to GitHub Release
- MSIX signed with certificate stored as GitHub Actions secret

### Local Build (scripts/build.ps1)
```powershell
# Usage: .\scripts\build.ps1 [-Configuration Release|Debug] [-Package]
```
- Wraps `dotnet build`, `dotnet test`, and MSIX packaging steps
- Allows developers to produce a local MSIX without GitHub Actions

---

## Version Roadmap

| Version | Scope |
|---|---|
| v1.0 | Full MVP as defined above |
| v1.1 | Code signing via trusted cert; dark/light mode settings UI theming |
| v1.2 | Per-app monitor pinning (always open app X on monitor Y) |
| v2.0 | Multi-rule routing engine; hotkey support |

---

## Open Questions / Decisions Pending

| # | Question | Notes |
|---|---|---|
| 1 | GitHub org or personal account for repo? | Affects repo URL and release signing identity |
| 2 | App icon / branding assets | Needed before first public release |
| 3 | Trusted code signing cert for v1.1 | SignPath.io OSS program worth evaluating |
| 4 | Minimum Windows 10 version for best-effort support | 22H2 is a reasonable baseline |
