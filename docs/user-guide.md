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

### Microsoft Store (Recommended)

1. Open the **Microsoft Store** on your PC
2. Search for **WindowSeat** or follow the link from the [GitHub Releases page](https://github.com/stmba/WindowSeat/releases)
3. Click **Get** (or **Install**)
4. WindowSeat starts automatically — look for the 🪑 icon in your system tray

The Store version updates automatically and can be uninstalled cleanly from Settings → Apps.

### ZIP Download (Technical Users / Sideload)

If you prefer not to use the Store:

1. Go to the [Releases page](https://github.com/stmba/WindowSeat/releases) on GitHub
2. Download the latest `WindowSeat-win-x64.zip`
3. Extract the ZIP to a folder of your choice (e.g. `C:\Program Files\WindowSeat\`)
4. Run `WindowSeat.exe`
5. Look for the 🪑 icon in your system tray

> **Note:** The ZIP version has no installer. There is no entry in Add/Remove Programs. Auto-start at login works via the in-app Settings toggle as normal.

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

### Store version

1. Open **Settings → Apps → Installed apps**
2. Search for **WindowSeat**
3. Click the three-dot menu and choose **Uninstall**

### ZIP version

1. Right-click the 🪑 tray icon and choose **Exit**
2. Delete the folder you extracted WindowSeat into
3. If you enabled auto-start, open **Task Manager → Startup apps** and remove the WindowSeat entry (or toggle it off in Settings before exiting)

In both cases, WindowSeat's settings file at `%AppData%\WindowSeat\settings.json` is not removed automatically. Delete that folder manually if you want a complete clean removal.

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
