# MSIX Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Windows Application Packaging Project (wapproj) that produces an unsigned MSIX for Microsoft Store submission, while keeping the existing GitHub Releases ZIP distribution unchanged.

**Architecture:** A `.wapproj` in `packaging/WindowSeat.Package/` wraps `WindowSeat.App.csproj` and declares the package identity, entry point, and assets in `Package.appxmanifest`. CI builds the wapproj with MSBuild and uploads the MSIX as a workflow artifact for manual Partner Center submission. The existing `dotnet publish` → ZIP → GitHub Release flow is untouched.

**Tech Stack:** MSBuild (wapproj toolset, pre-installed in VS 2022), `microsoft/setup-msbuild` GitHub Action, PowerShell 7 (manifest version patching, image generation), `actions/upload-artifact@v4`

---

## File Map

**Create:**
- `packaging/WindowSeat.Package/Images/Square44x44Logo.png` — 44×44 placeholder tile asset
- `packaging/WindowSeat.Package/Images/Square150x150Logo.png` — 150×150 placeholder tile asset
- `packaging/WindowSeat.Package/Images/Wide310x150Logo.png` — 310×150 placeholder tile asset
- `packaging/WindowSeat.Package/Images/StoreLogo.png` — 50×50 placeholder Store logo
- `packaging/WindowSeat.Package/Package.appxmanifest` — package identity, entry point, capabilities
- `packaging/WindowSeat.Package/WindowSeat.Package.wapproj` — Windows Application Packaging Project

**Modify:**
- `WindowSeat.sln` — add wapproj as a solution project
- `.github/workflows/release.yml` — Setup MSBuild, patch manifest version, build wapproj, upload MSIX artifact
- `scripts/build.ps1` — extend `-Package` switch to also produce an MSIX via MSBuild
- `docs/user-guide.md` — update Installation and Uninstalling sections for the Store/ZIP model

---

### Task 1: Create packaging directory and placeholder images

> The MSIX manifest references four image assets. They must exist at the declared sizes or the wapproj build will fail. These are placeholders — replace with real artwork before the first Store submission.

**Files:**
- Create: `packaging/WindowSeat.Package/Images/Square44x44Logo.png`
- Create: `packaging/WindowSeat.Package/Images/Square150x150Logo.png`
- Create: `packaging/WindowSeat.Package/Images/Wide310x150Logo.png`
- Create: `packaging/WindowSeat.Package/Images/StoreLogo.png`

- [ ] **Step 1: Create the directory and generate placeholder PNGs**

Run this PowerShell script from the repo root. It creates solid-colour PNGs at the exact sizes required by the manifest:

```powershell
New-Item -ItemType Directory -Force -Path packaging/WindowSeat.Package/Images | Out-Null

Add-Type -AssemblyName System.Drawing

function New-PlaceholderPng {
    param([string]$Path, [int]$Width, [int]$Height)
    $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 92, 45, 145))   # #5C2D91 purple
    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

New-PlaceholderPng -Path "packaging/WindowSeat.Package/Images/Square44x44Logo.png"   -Width 44  -Height 44
New-PlaceholderPng -Path "packaging/WindowSeat.Package/Images/Square150x150Logo.png" -Width 150 -Height 150
New-PlaceholderPng -Path "packaging/WindowSeat.Package/Images/Wide310x150Logo.png"   -Width 310 -Height 150
New-PlaceholderPng -Path "packaging/WindowSeat.Package/Images/StoreLogo.png"         -Width 50  -Height 50
```

- [ ] **Step 2: Verify the files exist at the right sizes**

```powershell
Get-ChildItem packaging/WindowSeat.Package/Images/ | Select-Object Name, Length
```

Expected: four `.png` files, each non-zero bytes.

- [ ] **Step 3: Commit**

```bash
git add packaging/WindowSeat.Package/Images/
git commit -m "feat(packaging): add placeholder MSIX image assets"
```

---

### Task 2: Create Package.appxmanifest

> The manifest declares the package identity (must match Partner Center exactly), the entry point, the target OS range, and the `runFullTrust` restricted capability (required whenever `EntryPoint="Windows.FullTrustApplication"` is used).

**Files:**
- Create: `packaging/WindowSeat.Package/Package.appxmanifest`

- [ ] **Step 1: Create the file**

Create `packaging/WindowSeat.Package/Package.appxmanifest` with this exact content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="SteveBarnes.WindowSeat"
    Publisher="CN=4E8B8DF2-E078-41CF-871B-1CEF8CC0E1DC"
    Version="1.0.0.0"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>WindowSeat</DisplayName>
    <PublisherDisplayName>SteveBarnes</PublisherDisplayName>
    <Logo>Images\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily
      Name="Windows.Desktop"
      MinVersion="10.0.17763.0"
      MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="App" Executable="WindowSeat.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="WindowSeat"
        Description="Opens new windows on the monitor you are actively using."
        BackgroundColor="transparent"
        Square150x150Logo="Images\Square150x150Logo.png"
        Square44x44Logo="Images\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Images\Wide310x150Logo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>

</Package>
```

- [ ] **Step 2: Commit**

```bash
git add packaging/WindowSeat.Package/Package.appxmanifest
git commit -m "feat(packaging): add MSIX package manifest"
```

---

### Task 3: Create WindowSeat.Package.wapproj

> The wapproj is an MSBuild project that ties together the manifest, image assets, and the reference to `WindowSeat.App.csproj`. It uses the Desktop Bridge MSBuild targets installed with Visual Studio's "Windows application development" workload.
>
> **Prerequisite:** Visual Studio 2022 must be installed with the **"Windows application development"** workload (provides `Microsoft.DesktopBridge.props` and `Microsoft.DesktopBridge.targets`). On GitHub Actions `windows-latest`, these are present via the "Universal Windows Platform development" workload.

**Files:**
- Create: `packaging/WindowSeat.Package/WindowSeat.Package.wapproj`

- [ ] **Step 1: Create the file**

Create `packaging/WindowSeat.Package/WindowSeat.Package.wapproj` with this exact content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" DefaultTargets="Build"
         xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
          Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />

  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">x86</Platform>
    <ProjectGuid>{D1E2F3A4-0005-0005-0005-000000000005}</ProjectGuid>
    <ProjectTypeGuids>{C7167F0D-BC9F-4E6A-9F78-5F2B13F8E92E};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <OutputType>Package</OutputType>
    <AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>
    <AppxAutoIncrementPackageRevision>false</AppxAutoIncrementPackageRevision>
    <AppxBundle>Never</AppxBundle>
    <GenerateTestArtifacts>false</GenerateTestArtifacts>
    <TargetPlatformVersion>10.0.22621.0</TargetPlatformVersion>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x64' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>false</Optimize>
    <AppxPackageDir>bin\x64\Debug\AppPackages\</AppxPackageDir>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x64' ">
    <DebugType>none</DebugType>
    <Optimize>true</Optimize>
    <AppxPackageDir>bin\x64\Release\AppPackages\</AppxPackageDir>
  </PropertyGroup>

  <ItemGroup>
    <AppxManifest Include="Package.appxmanifest">
      <SubType>Designer</SubType>
    </AppxManifest>
  </ItemGroup>

  <ItemGroup>
    <Content Include="Images\Square44x44Logo.png" />
    <Content Include="Images\Square150x150Logo.png" />
    <Content Include="Images\Wide310x150Logo.png" />
    <Content Include="Images\StoreLogo.png" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\WindowSeat.App\WindowSeat.App.csproj">
      <SkipGetTargetFrameworkProperties>true</SkipGetTargetFrameworkProperties>
    </ProjectReference>
  </ItemGroup>

  <Import Project="$(MSBuildExtensionsPath)\Microsoft\DesktopBridge\Microsoft.DesktopBridge.props" />
  <Import Project="$(WapProjPath)\Microsoft.DesktopBridge.targets" />

</Project>
```

- [ ] **Step 2: Commit**

```bash
git add packaging/WindowSeat.Package/WindowSeat.Package.wapproj
git commit -m "feat(packaging): add Windows Application Packaging Project"
```

---

### Task 4: Add wapproj to WindowSeat.sln

> The solution file must reference the wapproj so Visual Studio can open it and so `dotnet build WindowSeat.sln` is aware of it. The wapproj uses a specific project type GUID (`{C7167F0D-...}`) that Visual Studio recognises as a packaging project.
>
> **Note:** `dotnet build` on the solution will skip the wapproj (it requires MSBuild, not the .NET CLI). This is expected and fine — the wapproj is only built explicitly via `msbuild`.

**Files:**
- Modify: `WindowSeat.sln`

- [ ] **Step 1: Add the project declaration**

In `WindowSeat.sln`, insert this block immediately before the `Global` keyword (after the last `EndProject` line):

```
Project("{C7167F0D-BC9F-4E6A-9F78-5F2B13F8E92E}") = "WindowSeat.Package", "packaging\WindowSeat.Package\WindowSeat.Package.wapproj", "{D1E2F3A4-0005-0005-0005-000000000005}"
EndProject
```

- [ ] **Step 2: Add build configuration mappings**

In `WindowSeat.sln`, inside `GlobalSection(ProjectConfigurationPlatforms) = postSolution`, add these four lines (before `EndGlobalSection`):

```
		{D1E2F3A4-0005-0005-0005-000000000005}.Debug|Any CPU.ActiveCfg = Debug|x64
		{D1E2F3A4-0005-0005-0005-000000000005}.Debug|Any CPU.Build.0 = Debug|x64
		{D1E2F3A4-0005-0005-0005-000000000005}.Release|Any CPU.ActiveCfg = Release|x64
		{D1E2F3A4-0005-0005-0005-000000000005}.Release|Any CPU.Build.0 = Release|x64
```

- [ ] **Step 3: Commit**

```bash
git add WindowSeat.sln
git commit -m "feat(packaging): add WindowSeat.Package to solution"
```

---

### Task 5: Verify local wapproj build

> Before touching CI, verify the wapproj builds on the local machine. This catches manifest errors, missing VS workloads, and path issues early.
>
> **Requires:** Visual Studio 2022 with the "Windows application development" workload installed.

- [ ] **Step 1: Locate MSBuild**

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
Write-Host "MSBuild: $msbuild"
```

Expected: a path like `C:\Program Files\Microsoft Visual Studio\2022\...\MSBuild\Current\Bin\MSBuild.exe`

If `vswhere.exe` is not found or returns nothing, install the "Windows application development" workload in the Visual Studio Installer and retry.

- [ ] **Step 2: Build the wapproj**

```powershell
& $msbuild packaging/WindowSeat.Package/WindowSeat.Package.wapproj `
    /p:Configuration=Release `
    /p:Platform=x64 `
    /p:AppxPackageSigningEnabled=false `
    /p:AppxAutoIncrementPackageRevision=false `
    /p:GenerateTestArtifacts=false
```

Expected: `Build succeeded.` with no errors. Warnings about placeholder images are fine.

- [ ] **Step 3: Verify the MSIX was produced**

```powershell
Get-ChildItem packaging/WindowSeat.Package/bin/x64/Release/AppPackages/ -Recurse -Filter *.msix
```

Expected: one `.msix` file (e.g. `WindowSeat.Package_1.0.0.0_x64.msix`).

- [ ] **Step 4: Inspect the MSIX contents (optional sanity check)**

An MSIX is a zip file. Rename it to `.zip` and open it, or use:

```powershell
$msix = Get-ChildItem packaging/WindowSeat.Package/bin/x64/Release/AppPackages/ -Recurse -Filter *.msix | Select-Object -First 1
$zip  = $msix.FullName -replace '\.msix$', '.zip'
Copy-Item $msix.FullName $zip
Expand-Archive $zip -DestinationPath "$($msix.Directory)\msix-contents" -Force
Get-ChildItem "$($msix.Directory)\msix-contents" | Select-Object Name
```

Expected to see: `WindowSeat.exe`, `AppxManifest.xml`, `AppxBlockMap.xml`, image files, and the app's DLLs.

---

### Task 6: Update release.yml

> Add three things to the CD pipeline: (1) `setup-msbuild` action so `msbuild.exe` is on PATH, (2) a PowerShell step that patches the manifest version from the git tag before building, (3) an MSBuild step to build the wapproj, and (4) an `upload-artifact` step to preserve the MSIX for Partner Center submission. The MSIX is NOT attached to the GitHub Release — it is a workflow artifact only.

**Files:**
- Modify: `.github/workflows/release.yml`

- [ ] **Step 1: Add `setup-msbuild` after the `Setup .NET 8` step**

In `.github/workflows/release.yml`, after the `Setup .NET 8` step block, insert:

```yaml
      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v2
```

- [ ] **Step 2: Add manifest version-patch step after "Publish app"**

After the `Publish app` step block, insert:

```yaml
      # ── MSIX packaging ────────────────────────────────────────────────────
      - name: Patch manifest version
        shell: pwsh
        run: |
          $v = "${{ steps.version.outputs.VERSION }}.0"
          $path = "packaging/WindowSeat.Package/Package.appxmanifest"
          (Get-Content $path -Raw) `
            -replace '(?<=<Identity[^>]*Version=")[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+', $v |
            Set-Content $path
          Write-Host "Manifest version set to $v"
```

- [ ] **Step 3: Add MSBuild wapproj build step**

Immediately after the manifest-patch step, insert:

```yaml
      - name: Build MSIX package
        shell: pwsh
        run: |
          msbuild packaging/WindowSeat.Package/WindowSeat.Package.wapproj `
            /p:Configuration=Release `
            /p:Platform=x64 `
            /p:AppxPackageSigningEnabled=false `
            /p:AppxAutoIncrementPackageRevision=false `
            /p:GenerateTestArtifacts=false `
            "/p:AppxPackageDir=${{ github.workspace }}\artifacts\msix\\"
```

- [ ] **Step 4: Add upload-artifact step**

Immediately after the `Build MSIX package` step, insert:

```yaml
      - name: Upload MSIX (Store submission artifact)
        uses: actions/upload-artifact@v4
        with:
          name: WindowSeat-${{ steps.version.outputs.VERSION }}-store-package
          path: artifacts/msix/*.msix
```

- [ ] **Step 5: Update the TODO comment**

Replace the existing TODO comment block in `release.yml`:

```yaml
      # ── MSIX packaging ────────────────────────────────────────────────────
      # TODO: Wire up Windows Application Packaging Project (wapproj) here
      # once packaging/ scaffold is complete. For v1.0, zip the publish output
      # as a stopgap so users can still download and run.
```

with just the section heading (the actual steps are now the ones added above):

```yaml
      # ── ZIP (GitHub Releases sideload) ────────────────────────────────────
```

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "feat(ci): build MSIX via wapproj and upload as Store submission artifact"
```

---

### Task 7: Update build.ps1

> Extend the existing `-Package` switch so that, after producing the ZIP, it also locates MSBuild and builds the wapproj to produce a local MSIX in `artifacts/`. If `vswhere.exe` or the VS workload is absent the script prints a clear warning and skips MSIX (not a hard failure — the ZIP is still produced).

**Files:**
- Modify: `scripts/build.ps1`

- [ ] **Step 1: Add `$ArtifactsDir` variable to the existing variable block**

In `scripts/build.ps1`, after the `$TestProject` line, add:

```powershell
$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$WaprojFile   = Join-Path $RepoRoot 'packaging\WindowSeat.Package\WindowSeat.Package.wapproj'
```

- [ ] **Step 2: Replace the existing `-Package` block**

Replace the existing `if ($Package)` block (lines 58–73) with:

```powershell
# ── Publish ───────────────────────────────────────────────────────────────────
if ($Package) {
    Write-Step "Publishing"
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

    dotnet publish (Join-Path $RepoRoot 'src/WindowSeat.App/WindowSeat.App.csproj') `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained false `
        --output $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "Publish failed." }

    Write-Step "Creating ZIP"
    $zipPath = Join-Path $RepoRoot "WindowSeat-local-win-x64.zip"
    Compress-Archive -Path "$PublishDir/*" -DestinationPath $zipPath -Force
    Write-Host "ZIP created: $zipPath" -ForegroundColor Green

    # ── MSIX (requires VS 2022 with Windows application development workload) ──
    Write-Step "Building MSIX package"
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        Write-Warning "vswhere.exe not found — skipping MSIX. Install Visual Studio 2022 to enable local MSIX packaging."
    } else {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild `
            -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
        if (-not $msbuild) {
            Write-Warning "MSBuild not found via vswhere — skipping MSIX."
        } else {
            $msixOut = Join-Path $ArtifactsDir 'msix'
            if (Test-Path $msixOut) { Remove-Item $msixOut -Recurse -Force }
            New-Item -ItemType Directory -Force -Path $msixOut | Out-Null

            & $msbuild $WaprojFile `
                /p:Configuration=$Configuration `
                /p:Platform=x64 `
                /p:AppxPackageSigningEnabled=false `
                /p:AppxAutoIncrementPackageRevision=false `
                /p:GenerateTestArtifacts=false `
                "/p:AppxPackageDir=$msixOut\\"
            if ($LASTEXITCODE -ne 0) { throw "MSIX build failed." }

            $msix = Get-ChildItem $msixOut -Filter *.msix | Select-Object -First 1
            Write-Host "MSIX created: $($msix.FullName)" -ForegroundColor Green
        }
    }
}
```

- [ ] **Step 3: Verify the script runs cleanly without `-Package`**

```powershell
.\scripts\build.ps1 -SkipTests
```

Expected: `Build complete.` with no errors.

- [ ] **Step 4: Verify the script produces both ZIP and MSIX with `-Package`**

```powershell
.\scripts\build.ps1 -Package -SkipTests
```

Expected: `ZIP created: ...` and `MSIX created: ...` (or a warning about missing VS workload if not installed).

- [ ] **Step 5: Commit**

```bash
git add scripts/build.ps1
git commit -m "feat(build): add local MSIX packaging to build.ps1 -Package switch"
```

---

### Task 8: Update docs/user-guide.md

> Replace the current GitHub-MSIX install instructions with the new Store-primary / ZIP-sideload model. The Uninstalling section already covers the Store path correctly; add the ZIP uninstall path.

**Files:**
- Modify: `docs/user-guide.md`

- [ ] **Step 1: Replace the Installation section**

Replace the current `## Installation` section (lines 23–29) with:

```markdown
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
```

- [ ] **Step 2: Replace the Uninstalling section**

Replace the current `## Uninstalling` section (lines 93–98) with:

```markdown
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
```

- [ ] **Step 3: Commit**

```bash
git add docs/user-guide.md
git commit -m "docs: update installation and uninstall instructions for Store/ZIP model"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Task |
|---|---|
| wapproj in `packaging/WindowSeat.Package/` | Task 3 |
| Package identity: `SteveBarnes.WindowSeat` / `CN=4E8B8DF2-...` | Task 2 |
| Entry point: `Windows.FullTrustApplication` | Task 2 |
| `runFullTrust` capability | Task 2 |
| `TargetDeviceFamily` min `10.0.17763.0` | Task 2 |
| Placeholder image assets at correct sizes | Task 1 |
| wapproj added to `WindowSeat.sln` | Task 4 |
| CI: `setup-msbuild` action | Task 6 |
| CI: manifest version patched from git tag | Task 6 |
| CI: wapproj built with `AppxPackageSigningEnabled=false` | Task 6 |
| CI: MSIX uploaded as workflow artifact (not GitHub Release attachment) | Task 6 |
| CI: ZIP flow unchanged | Task 6 (no-touch) |
| `build.ps1 -Package` builds MSIX locally | Task 7 |
| `build.ps1` gracefully skips MSIX if VS not installed | Task 7 |
| `docs/user-guide.md` reflects Store/ZIP model | Task 8 |
| Future signed-release path documented | Spec only (no code needed) |

All spec requirements covered. No placeholders in any task.
