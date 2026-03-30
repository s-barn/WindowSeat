# Contributing to WindowSeat

Thank you for your interest in contributing. This document covers everything you need to get up and running.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) with the **Windows application development** workload  
  _or_ VS Code with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
- Git
- Windows 10 22H2+ or Windows 11 (required — the project uses Windows-only APIs)

---

## Getting Started

```powershell
git clone https://github.com/stmba/WindowSeat.git
cd WindowSeat
dotnet restore WindowSeat.sln
dotnet build WindowSeat.sln
```

Run tests:

```powershell
dotnet test src/WindowSeat.Tests/WindowSeat.Tests.csproj
```

Or use the build script:

```powershell
.\scripts\build.ps1
```

---

## Project Structure

| Project | Purpose |
|---|---|
| `WindowSeat.App` | WPF application — tray icon, settings UI, app entry point |
| `WindowSeat.Core` | Window event hooks, monitor detection strategies, window mover |
| `WindowSeat.Settings` | Settings model, JSON persistence, startup registry management |
| `WindowSeat.Tests` | xUnit unit tests for Core and Settings |

---

## Architecture Notes

**IMonitorDetector** — the detection strategy interface in `WindowSeat.Core.Detection`. Adding a new detection method means implementing this interface and wiring it up in `App.xaml.cs`. No other changes required.

**WindowEventListener** — runs on its own dedicated STA thread with a Win32 message pump. This is intentional — `SetWinEventHook` requires a message pump on the calling thread to deliver events. Do not move hook registration to the WPF UI thread.

**WindowMover** — receives a window handle and calls `SetWindowPos`. UWP/immersive windows are detected by class name and skipped. The skip list in `WindowMover.cs` can be extended as new problematic window classes are discovered.

**SettingsService** — thread-safe, file-backed. Accepts an optional directory override in its constructor, which is how `WindowSeat.Tests` redirects writes to a temp directory without touching `%AppData%`.

---

## Making Changes

1. Fork the repository and create a branch from `main`
2. Make your changes, including tests for any new behaviour
3. Ensure all tests pass: `dotnet test`
4. Ensure the solution builds cleanly: `dotnet build WindowSeat.sln`
5. Open a pull request against `main` with a clear description of what changed and why

---

## Pull Request Guidelines

- Keep PRs focused — one feature or fix per PR
- Update `CHANGELOG.md` under `[Unreleased]` with a brief description of your change
- If your change affects user-visible behaviour, update the relevant doc in `docs/`
- All new public types and members should have XML doc comments

---

## Reporting Issues

Please use the [GitHub issue templates](.github/ISSUE_TEMPLATE) — the bug report template asks for the details most useful for diagnosing problems quickly.

---

## Code Style

- Standard C# conventions — follow the patterns already in the codebase
- Nullable reference types enabled — no `!` suppressions without a comment explaining why
- `sealed` by default for classes unless inheritance is explicitly intended
- All Win32 P/Invoke lives in `NativeMethods.cs` — do not scatter `[DllImport]` elsewhere
