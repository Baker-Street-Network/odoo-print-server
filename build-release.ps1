# Odoo Print Server — Build & Package Script
# Publishes the app and creates a Velopack installer + update feed in one command.
#
# Prerequisites:
#   dotnet tool install -g vpk
#
# Usage:
#   .\build-release.ps1 -Version 1.0.0
#   .\build-release.ps1 -Version 1.1.0 -ReleaseDir .\releases

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    # Where Velopack writes the installer and update feed files.
    [string]$ReleaseDir = ".\releases"
)

$ErrorActionPreference = "Stop"

$ProjectDir = "$PSScriptRoot"
$PublishDir = "$PSScriptRoot\publish"
$PackId     = "OdooPrintServer"
$MainExe    = "OdooPrintServer.exe"

# 1. Clean previous publish output
Write-Host "`n[1/4] Cleaning publish directory..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

# 2. Publish self-contained single-file exe
Write-Host "`n[2/4] Publishing ($Version)..." -ForegroundColor Cyan
dotnet publish "$ProjectDir\OdooPrintServer.csproj" `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:Version=$Version `
    -o "$PublishDir"

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit 1
}
Write-Host "Published to: $PublishDir" -ForegroundColor Green

# 3. Package with Velopack
Write-Host "`n[3/4] Packaging with Velopack..." -ForegroundColor Cyan
vpk pack `
    --packId      $PackId `
    --packVersion $Version `
    --packDir     $PublishDir `
    --mainExe     $MainExe `
    --outputDir   $ReleaseDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed."
    exit 1
}

# 4. Summary
Write-Host "`n[4/4] Done!" -ForegroundColor Green
Write-Host ""
Write-Host "Release artifacts written to: $ReleaseDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "  OdooPrintServer-$Version-Setup.exe  -> distribute this to users"
Write-Host "  releases\                            -> host this folder for auto-updates"
Write-Host ""
Write-Host "To release via GitHub, push a version tag:" -ForegroundColor Yellow
Write-Host "  git tag v$Version"
Write-Host "  git push origin v$Version"
Write-Host ""
