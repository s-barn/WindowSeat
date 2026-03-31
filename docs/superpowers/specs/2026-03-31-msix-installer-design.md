# MSIX Installer Design

**Date:** 2026-03-31  
**Status:** Approved  
**Distribution model:** Microsoft Store (primary) + GitHub Releases ZIP sideload (secondary)

---

## Overview

Add an MSIX installer to WindowSeat using a Windows Application Packaging Project (wapproj). The MSIX is built in CI and uploaded to the Microsoft Partner Center for Store distribution. GitHub Releases continues to distribute a plain ZIP for technical users who want to run the app without installing from the Store.

---

## Package Identity

| Field | Value |
|---|---|
| Name | `SteveBarnes.WindowSeat` |
| Publisher | `CN=4E8B8DF2-E078-41CF-871B-1CEF8CC0E1DC` |
| Version | Derived from git tag at build time (e.g. `v1.0.0` → `1.0.0.0`) |

The publisher CN matches the Partner Center app registration. Microsoft signs the package during Store ingestion — no certificate management is required in this project.

---

## Project Structure

New files added under `packaging/WindowSeat.Package/`:

```
packaging/
└── WindowSeat.Package/
    ├── WindowSeat.Package.wapproj       ← Windows Application Packaging Project
    ├── Package.appxmanifest             ← Package identity, entry point, capabilities
    └── Images/
        ├── Square44x44Logo.png          ← Required (tray/taskbar size)
        ├── Square150x150Logo.png        ← Required (Start menu tile)
        ├── Wide310x150Logo.png          ← Required (wide Start tile)
        └── StoreLogo.png               ← Required (Partner Center listing)
```

`WindowSeat.sln` is updated to include `WindowSeat.Package.wapproj` as a solution project. The wapproj holds no code — it is a thin packaging wrapper that references `WindowSeat.App.csproj` as its entry point.

---

## Package Manifest (Package.appxmanifest)

### Identity and Properties

```xml
<Identity
  Name="SteveBarnes.WindowSeat"
  Publisher="CN=4E8B8DF2-E078-41CF-871B-1CEF8CC0E1DC"
  Version="1.0.0.0" />
```

Version is injected at build time via MSBuild property (`/p:PackageVersion`).

### Entry Point

The app is declared as a full-trust desktop application via `EntryPoint="Windows.FullTrustApplication"` on the `Application` element:

```xml
<Application Id="App" Executable="WindowSeat.exe" EntryPoint="Windows.FullTrustApplication">
  <uap:VisualElements DisplayName="WindowSeat" ... />
</Application>
```

This is the correct declaration for a wapproj-packaged WPF app. (`windows.fullTrustProcess` is a different extension used by UWP apps to spawn Win32 companion processes — it is not needed here.)

The manifest also declares `TargetDeviceFamily` targeting Windows Desktop with a minimum version of `10.0.17763.0` (Windows 10 1809), consistent with the project's stated minimum.

### Capabilities

None declared. The app:
- Reads/writes `%AppData%\WindowSeat\` (implicit for packaged apps via AppData redirection)
- Writes a startup registry key under `HKCU` (no capability required)
- Calls Win32 hooks (`SetWinEventHook`) — no capability required
- Makes one outbound HTTPS call to the GitHub API (internet client capability is implicit for packaged desktop apps on Windows 10+)

### Required Images

Placeholder PNG assets are committed to `packaging/WindowSeat.Package/Images/`. These should be replaced with production-quality artwork before the first Store submission.

| Asset | Size |
|---|---|
| Square44x44Logo.png | 44×44 |
| Square150x150Logo.png | 150×150 |
| Wide310x150Logo.png | 310×150 |
| StoreLogo.png | 50×50 |

---

## CI/CD Pipeline Changes

### release.yml

Two additions alongside the existing flow:

1. **Setup MSBuild** — adds `msbuild.exe` to PATH using the `microsoft/setup-msbuild` action. VS build tools including the Windows Application Packaging toolset are pre-installed on `windows-latest`.

2. **Build wapproj** — MSBuild builds `packaging/WindowSeat.Package/WindowSeat.Package.wapproj` in Release/x64 with version injected:
   ```
   msbuild packaging/WindowSeat.Package/WindowSeat.Package.wapproj
     /p:Configuration=Release
     /p:Platform=x64
     /p:PackageVersion={version}.0
     /p:AppxPackageSigningEnabled=false
   ```
   `AppxPackageSigningEnabled=false` produces an unsigned MSIX suitable for Partner Center submission (Microsoft signs during ingestion).

3. **Upload MSIX as workflow artifact** — the MSIX is uploaded via `actions/upload-artifact`, not attached to the public GitHub Release. This keeps the release page uncluttered for end users. The maintainer downloads it from the Actions run when submitting to Partner Center.

### Existing ZIP flow (unchanged)

`dotnet publish` → `Compress-Archive` → attached to GitHub Release. No changes.

---

## Local Build Script (scripts/build.ps1)

A `-Package` switch is added. When passed:

1. Locates `msbuild.exe` via `vswhere.exe` (`${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe`)
2. Builds the wapproj (same MSBuild arguments as CI)
3. Copies the resulting MSIX to `artifacts/`

Without `-Package`, existing behaviour is unchanged.

---

## Documentation Changes (docs/user-guide.md)

The installation section is updated to reflect the new distribution model:

- **Recommended:** Install from the Microsoft Store (one click, automatic updates, clean uninstall via Settings → Apps → Installed apps)
- **Sideload (technical users):** Download the ZIP from GitHub Releases, extract, and run `WindowSeat.exe` directly. No Add/Remove Programs entry; uninstall by deleting the folder and optionally removing `%AppData%\WindowSeat\`.

---

## Out of Scope

- Code signing for the GitHub ZIP or MSIX sideload (deferred — add a trusted cert to the signing step in `release.yml` when available)
- MSIX bundle (`.msixbundle`) for multi-architecture support — single x64 package is sufficient for v1.0
- Auto-update via Store — the existing GitHub API update check remains in place; Store handles its own updates independently

---

## Future: Adding Signed GitHub Releases (Option B)

When a trusted code-signing certificate becomes available (e.g. SignPath.io OSS program):

1. Store the PFX as a base64-encoded GitHub Actions secret (`SIGNING_CERT_PFX`) and its password as `SIGNING_CERT_PASSWORD`
2. Add a `signtool.exe` step after the wapproj build in `release.yml`
3. Attach the signed MSIX to the GitHub Release alongside the ZIP

No structural changes to the wapproj, manifest, or local build script are required.
