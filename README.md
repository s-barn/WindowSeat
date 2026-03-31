# 🪑 WindowSeat

**WindowSeat** opens new application windows on the monitor you are actively working on — no manual dragging required.

Windows 11 has no built-in setting to control which monitor new windows appear on. WindowSeat fills that gap with a lightweight background utility that watches for new windows and quietly moves them to the right place.

---

## Features

- **Cursor-based detection** — windows open on whichever monitor your mouse is on
- **Taskbar-based detection** — windows open on the monitor whose taskbar you last interacted with
- **Per-app exclusions** — tell WindowSeat to leave specific apps alone (e.g. Spotify, Discord)
- **System tray** — runs silently in the background; right-click to access settings or toggle on/off
- **Auto-starts with Windows** — enabled by default; toggle in Settings
- **Update notifications** — notified when a new release is available on GitHub (no auto-update)
- **No telemetry** — zero data collection, no cloud dependency, settings stored locally

---

## Requirements

- Windows 11 (Windows 10 22H2+ best effort)
- No administrator rights required

---

## Installation

1. Download the latest `WindowSeat.msix` from the [Releases](https://github.com/stmba/WindowSeat/releases) page
2. Double-click the `.msix` file and follow the installer prompts
3. WindowSeat starts automatically and appears in your system tray

> **Note:** Because WindowSeat is not yet signed with a trusted certificate, Windows may show a SmartScreen warning on first install. Click **More info → Run anyway** to proceed. See [docs/user-guide.md](docs/user-guide.md) for full details.

---

## Usage

After installation, WindowSeat runs in the background. Look for the 🪑 icon in your system tray.

**Right-click the tray icon to:**
- Open Settings
- Enable / disable WindowSeat
- Check for updates
- Exit

**Settings let you:**
- Switch between cursor-position and last-active-taskbar detection
- Add or remove per-app exclusions
- Toggle auto-start at login
- Toggle update notifications

For a full walkthrough see [docs/user-guide.md](docs/user-guide.md).  
For advanced configuration see [docs/power-user-guide.md](docs/power-user-guide.md).

---

## Building from Source

**Prerequisites**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 (17.8+) with the **Windows application development** workload, or VS Code with the C# Dev Kit extension

**Clone and build**

```powershell
git clone https://github.com/stmba/WindowSeat.git
cd WindowSeat
.\scripts\build.ps1
```

**Run tests**

```powershell
dotnet test src/WindowSeat.Tests/WindowSeat.Tests.csproj
```

See [docs/contributing.md](docs/contributing.md) for full developer setup.

---

## Project Structure

```
WindowSeat/
├── src/
│   ├── WindowSeat.App        # WPF app — tray icon, settings UI, entry point
│   ├── WindowSeat.Core       # Window hooks, monitor detection, window mover
│   ├── WindowSeat.Settings   # Settings model, JSON persistence, startup registry
│   └── WindowSeat.Tests      # xUnit tests
├── docs/
│   ├── user-guide.md
│   ├── power-user-guide.md
│   └── contributing.md
├── scripts/
│   └── build.ps1
└── packaging/                # MSIX packaging project
```

---

## Known Limitations

- **UWP / Microsoft Store apps** (e.g. Settings, Calculator) resist programmatic window repositioning — WindowSeat detects and skips these gracefully
- **Apps with internal window placement logic** (e.g. some Electron apps) may override WindowSeat's repositioning
- Windows 10 support is best effort; primary target is Windows 11

---

## Contributing

Contributions are welcome. Please read [docs/contributing.md](docs/contributing.md) before opening a pull request.

---

## License

MIT — see [LICENSE](LICENSE) for details.
