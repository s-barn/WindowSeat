# WindowSeat User Guide

This guide walks you through installing, setting up, and using WindowSeat.

---

## What Does WindowSeat Do?

When you open an application on a multi-monitor Windows setup, Windows often ignores which monitor you are working on and places the new window wherever it likes — usually the primary display. WindowSeat watches for new windows and quietly moves them to the monitor you are actively using.

---

## System Requirements

- Windows 11 (Windows 10 22H2+ supported on a best-effort basis)
- Two or more monitors
- No administrator rights required

---

## Installation

1. Go to the [Releases page](https://github.com/stmba/WindowSeat/releases) on GitHub
2. Download the latest `WindowSeat.msix` file
3. Double-click the downloaded file
4. If Windows SmartScreen appears, click **More info** then **Run anyway**  
   _(This warning appears because WindowSeat is not yet signed with a paid certificate. The source code is fully open for review at github.com/stmba/WindowSeat.)_
5. Follow the installer prompts and click **Install**
6. WindowSeat starts automatically — look for the 🪑 icon in your system tray

---

## First Run

WindowSeat starts silently in the background the moment it is installed and on every subsequent Windows login. No configuration is required to get started — the default settings work well for most setups:

- **Detection method:** Cursor position (windows open where your mouse is)
- **Auto-start:** Enabled
- **Update notifications:** Enabled

---

## Using the Tray Icon

Right-click the 🪑 icon in your system tray to access the context menu:

| Menu item | What it does |
|---|---|
| Open Settings | Opens the settings window |
| Enable WindowSeat | Toggles WindowSeat on or off without exiting |
| Check for Updates | Manually checks for a newer version on GitHub |
| Exit | Closes WindowSeat completely |

When WindowSeat is **disabled**, the tray icon is greyed out. Your windows will behave as normal — WindowSeat will not reposition anything until you re-enable it.

---

## Settings

Double-click the tray icon or right-click and choose **Open Settings** to open the settings window.

### General

**Enable WindowSeat** — master on/off switch. Disabling this pauses all window repositioning but keeps WindowSeat running in the tray.

**Launch at Windows startup** — controls whether WindowSeat starts automatically when you log in. Enabled by default.

**Notify me when updates are available** — when enabled, WindowSeat checks GitHub once per session for a newer release and shows a tray notification if one is found.

### Monitor Detection

**Cursor position** — WindowSeat moves new windows to the monitor your mouse cursor is on at the moment the window appears. This is the default and works well for most users.

**Last active taskbar** — WindowSeat moves new windows to the monitor whose taskbar you most recently clicked. Useful if you prefer to interact with taskbars rather than move your mouse first.

### App Exclusions

Some apps manage their own window placement and do not need WindowSeat's help — or you may simply prefer they always open in a specific location. Add them to the exclusion list and WindowSeat will leave their windows alone.

To add an exclusion:
1. Type the process name (e.g. `Spotify.exe`) in the text box, or click **Browse** to select the executable
2. Click **Add**

To remove an exclusion:
1. Select it in the list
2. Click **Remove Selected**

Click **Save** to apply your changes. Click **Cancel** to discard them.

---

## Uninstalling

1. Open **Settings → Apps → Installed apps**
2. Search for **WindowSeat**
3. Click the three-dot menu and choose **Uninstall**

WindowSeat's settings file at `%AppData%\WindowSeat\settings.json` is not removed by the uninstaller. You can delete this folder manually if you want a complete clean removal.

---

## Troubleshooting

**WindowSeat is not moving a specific app's windows**

Some applications control their own window placement and will override any repositioning WindowSeat attempts. This is most common with UWP / Microsoft Store apps (Settings, Calculator, etc.), some Electron apps (Slack, VS Code in some configurations), and apps that explicitly save and restore their own window position. WindowSeat detects known problematic window types and skips them gracefully.

**Windows are moving to the wrong monitor**

Try switching the detection method in Settings. If you are using **Last active taskbar**, try **Cursor position** instead, and make sure your cursor is on the intended monitor before opening an app.

**The SmartScreen warning appears every time**

This is a one-time warning per install. Once installed, you will not see it again until you install a newer version.

**WindowSeat is not starting with Windows**

Open Settings and ensure **Launch at Windows startup** is checked, then click Save.
