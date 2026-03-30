#Requires -Version 7.0
<#
.SYNOPSIS
    Build, test, and optionally package WindowSeat locally.

.PARAMETER Configuration
    Build configuration. Default: Release

.PARAMETER Package
    If specified, produces a zip of the published output.

.PARAMETER SkipTests
    Skip running unit tests.

.EXAMPLE
    .\scripts\build.ps1
    .\scripts\build.ps1 -Configuration Debug -SkipTests
    .\scripts\build.ps1 -Package
#>
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',

    [switch]$Package,
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot   = Split-Path $PSScriptRoot -Parent
$SolutionFile = Join-Path $RepoRoot 'WindowSeat.sln'
$PublishDir = Join-Path $RepoRoot 'publish'
$TestProject = Join-Path $RepoRoot 'src/WindowSeat.Tests/WindowSeat.Tests.csproj'

function Write-Step([string]$message) {
    Write-Host "`n==> $message" -ForegroundColor Cyan
}

# ── Restore ───────────────────────────────────────────────────────────────────
Write-Step "Restoring NuGet packages"
dotnet restore $SolutionFile
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }

# ── Build ─────────────────────────────────────────────────────────────────────
Write-Step "Building ($Configuration)"
dotnet build $SolutionFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# ── Test ──────────────────────────────────────────────────────────────────────
if (-not $SkipTests) {
    Write-Step "Running tests"
    dotnet test $TestProject --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
}

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

    Write-Step "Creating zip"
    $zipPath = Join-Path $RepoRoot "WindowSeat-local-win-x64.zip"
    Compress-Archive -Path "$PublishDir/*" -DestinationPath $zipPath -Force
    Write-Host "Package created: $zipPath" -ForegroundColor Green
}

Write-Host "`nBuild complete." -ForegroundColor Green
