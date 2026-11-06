# GitWidget - SAHAJ PowerShell Installer & Launcher
# Windows 11 / 10 Native Desktop Activity Monitor

$Host.UI.RawUI.WindowTitle = "GitWidget - SAHAJ Installer"
Clear-Host

Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "                     GitWidget • SAHAJ" -ForegroundColor White
Write-Host "    Ultra-Compact GitHub Desktop Activity Monitor (Windows 11)" -ForegroundColor DarkGray
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""

# 1. Prerequisite verification
Write-Host "[1/3] Verifying Microsoft .NET 8.0 Desktop Runtime..." -ForegroundColor Cyan
$runtimes = & dotnet --list-runtimes 2>$null
$isNet8Installed = $runtimes -match "Microsoft\.WindowsDesktop\.App 8\."

if ($isNet8Installed) {
    Write-Host "  [✓] .NET 8.0 Desktop Runtime is already installed (Prerequisite satisfied)." -ForegroundColor Green
} else {
    Write-Host "  [!] .NET 8.0 Desktop Runtime not detected. Installing prerequisite..." -ForegroundColor Yellow
    
    # Try winget
    try {
        Write-Host "  [↓] Installing via Windows Package Manager (winget)..." -ForegroundColor DarkGray
        winget install Microsoft.DotNet.DesktopRuntime.8 --silent --accept-package-agreements --accept-source-agreements
    } catch {
        # Fallback to direct download
        Write-Host "  [↓] Downloading official installer from Microsoft CDN..." -ForegroundColor DarkGray
        $tempExe = "$env:TEMP\windowsdesktop-runtime-8.0-win-x64.exe"
        Invoke-WebRequest -Uri "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe" -OutFile $tempExe
        Write-Host "  [⚡] Running silent installer..." -ForegroundColor DarkGray
        Start-Process $tempExe -ArgumentList "/install /quiet /norestart" -Wait
    }
    Write-Host "  [✓] Prerequisite installation complete!" -ForegroundColor Green
}

# 2. Binary verification
Write-Host ""
Write-Host "[2/3] Checking GitWidget-SAHAJ binary..." -ForegroundColor Cyan
$baseDir = $PSScriptRoot
$exePath = Join-Path $baseDir "GitHubDesktopWidget.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $baseDir "Application\GitHubDesktopWidget.exe"
}
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $baseDir "windows-native-csharp\GitHubDesktopWidget.exe"
}

if (Test-Path $exePath) {
    $sizeKb = [Math]::Round((Get-Item $exePath).Length / 1KB, 0)
    Write-Host "  [✓] Found binary: $exePath ($sizeKb KB)" -ForegroundColor Green
} else {
    Write-Host "  [X] GitHubDesktopWidget.exe not found!" -ForegroundColor Red
    pause
    exit 1
}

# 3. Create Desktop Shortcut
try {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktop "GitWidget - SAHAJ.lnk"
    $ws = New-Object -ComObject WScript.Shell
    $shortcut = $ws.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = Split-Path $exePath -Parent
    $shortcut.Description = "GitWidget - SAHAJ Desktop Monitor"
    $shortcut.Save()
    Write-Host "  [✓] Desktop shortcut created: 'GitWidget - SAHAJ.lnk'" -ForegroundColor DarkGreen
} catch {}

# 4. Launch Widget
Write-Host ""
Write-Host "[3/3] Launching GitWidget-SAHAJ..." -ForegroundColor Cyan
Start-Process $exePath

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "  GitWidget-SAHAJ is now live on your desktop!" -ForegroundColor White
Write-Host "  Right-click widget on desktop for Settings, Themes, and Shapes." -ForegroundColor DarkGray
Write-Host "=====================================================================" -ForegroundColor Green
Start-Sleep -Seconds 2
