@echo off
setlocal enabledelayedexpansion
title GitWidget - SAHAJ Installer
chcp 65001 >nul

echo =====================================================================
echo                     GitWidget • SAHAJ
echo    Ultra-Compact GitHub Desktop Activity Monitor (Windows 11)
echo =====================================================================
echo.

echo [1/3] Verifying Microsoft .NET 8.0 Desktop Runtime prerequisite...
dotnet --list-runtimes 2>nul | findstr /I "Microsoft.WindowsDesktop.App 8." >nul
if %ERRORLEVEL% equ 0 (
    echo   [✓] .NET 8.0 Desktop Runtime is already installed (Prerequisite satisfied).
) else (
    echo   [!] .NET 8.0 Desktop Runtime not found.
    echo   [↓] Automatically installing prerequisite via Windows Package Manager...
    winget install Microsoft.DotNet.DesktopRuntime.8 --silent --accept-package-agreements --accept-source-agreements
    if !ERRORLEVEL! neq 0 (
        echo   [↓] Downloading official installer from Microsoft CDN...
        curl -L -o "%TEMP%\dotnet8-desktop-runtime.exe" "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
        echo   [⚡] Installing .NET 8 Desktop Runtime silently...
        "%TEMP%\dotnet8-desktop-runtime.exe" /install /quiet /norestart
    )
    echo   [✓] Prerequisite installation complete!
)

echo.
echo [2/3] Checking GitWidget-SAHAJ binary...
set "EXE_PATH=%~dp0GitHubDesktopWidget.exe"
if not exist "%EXE_PATH%" (
    set "EXE_PATH=%~dp0Application\GitHubDesktopWidget.exe"
)
if exist "%EXE_PATH%" (
    echo   [✓] Binary found: %EXE_PATH%
) else (
    echo   [!] GitHubDesktopWidget.exe not found!
    pause
    exit /b 1
)

echo.
echo [3/3] Launching GitWidget-SAHAJ...
start "" "%EXE_PATH%"

echo.
echo =====================================================================
echo   GitWidget-SAHAJ is now running on your desktop!
echo   Tip: Right-click the widget for Settings, Themes, and Shapes.
echo =====================================================================
timeout /t 3 >nul
exit /b 0
