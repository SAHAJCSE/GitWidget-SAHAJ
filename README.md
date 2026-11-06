# GitWidget — SAHAJ

<p align="center">
  <img src="https://cdn.simpleicons.org/github/FFFFFF" width="42" alt="GitHub Logo" />
  &nbsp;&nbsp;
  <img src="https://cdn.simpleicons.org/windows11/0078D4" width="42" alt="Windows Logo" />
</p>

<h3 align="center">Your GitHub Activity, Right on Your Desktop.</h3>

<p align="center">
  A lightweight native Windows widget that displays your GitHub contribution heatmap.
  <br />
  Built with C# and WPF.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/Language-C%23-68217A?logo=csharp&logoColor=white" />
  <img src="https://img.shields.io/badge/License-MIT-green" />
</p>

<p align="center">
  <a href="#screenshots">Screenshots</a> •
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#settings">Settings</a> •
  <a href="#development">Development</a>
</p>

---

## About

GitWidget is a small desktop widget that lets you check your GitHub activity without opening your browser.

- Displays your GitHub contribution heatmap.
- Shows your daily contribution count.
- Supports 30-day, 6-month, and 1-year views.
- Offers multiple themes and customization options.
- Runs directly on your Windows desktop.

<a id="-screenshots" name="-screenshots"></a>
## Screenshots

### Contribution Heatmap

**6-Month & 1-Year Views**

<p align="center">
  <img src="images/6monthgraph.png" width="90%" alt="GitWidget 6-month contribution heatmap" />
  <br /><br />
  <img src="images/1yeargraph.png" width="90%" alt="GitWidget 1-year contribution heatmap" />
</p>

### 30-Day Views

<table>
  <tr>
    <td width="50%" align="center">
      <b>Apple Monochrome</b>
      <br /><br />
      <img src="images/image30Dblackandwhite.png" width="100%" alt="Apple monochrome theme" />
    </td>
    <td width="50%" align="center">
      <b>Liquid Glass</b>
      <br /><br />
      <img src="images/liquidglass30d.png" width="100%" alt="Liquid glass theme" />
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <b>GitHub Classic</b>
      <br /><br />
      <img src="images/classic30d.png" width="100%" alt="GitHub classic theme" />
    </td>
    <td width="50%" align="center">
      <b>Aurora Teal</b>
      <br /><br />
      <img src="images/blueaurora30D.png" width="100%" alt="Aurora teal theme" />
    </td>
  </tr>
</table>

### Settings & Activity Details

<table>
  <tr>
    <td width="50%" align="center">
      <b>Settings</b>
      <br /><br />
      <img src="images/settings1.png" width="100%" alt="Settings panel" />
    </td>
    <td width="50%" align="center">
      <b>Activity Analytics</b>
      <br /><br />
      <img src="images/detailedanalysis.png" width="100%" alt="Activity analytics" />
    </td>
  </tr>
</table>

## Features

- **Contribution Heatmap** — View your daily GitHub activity.
- **Three Time Ranges** — Switch between 30 days, 6 months, and 1 year.
- **Six Themes** — Apple Light, Apple B&W, Liquid Glass, GitHub Classic, Midnight OLED, and Aurora Teal.
- **Custom Shapes** — Choose from six contribution cell shapes.
- **Activity Analytics** — View contribution totals, streaks, and activity trends.
- **Desktop Widget** — Keep your heatmap visible on your desktop.
- **Custom Settings** — Adjust themes, size, position, and appearance.
- **Sound Effects** — Optional sounds for widget interactions.
- **Character Companion** — An interactive character with six expressions.
- **Lightweight** — Native C# and WPF implementation without Electron.

## Installation

### Option 1 — GUI Installer (Recommended)

1. Download or clone this repository.
2. Open the `windows-native-csharp` folder.
3. Run `GitWidget-Setup.exe`.
4. Follow the installation instructions.
5. Launch GitWidget from your desktop.

### Option 2 — Direct Run

If .NET 8 Desktop Runtime is installed:

1. Open the application folder.
2. Run `GitHubDesktopWidget.exe`.

### Option 3 — Install Script

Run either:

- `Install.bat`
- `Install.ps1`

These scripts handle the required runtime installation and launch the widget.

## Controls

| Action | How to use |
|---|---|
| Move widget | Click and drag |
| Open settings | Click the settings icon |
| Quick menu | Right-click the widget |
| Change time range | Select 30D, 6M, or 1Y |
| View daily activity | Hover over a heatmap cell |
| Change character | Click the character |
| Mute sounds | Use Settings or the right-click menu |
| Lock position | Right-click → Disable Moving |
| Keep on desktop | Right-click → Keep on Desktop |
| Start with Windows | Right-click → Start with Windows |
| Exit | Right-click → Quit Widget |

## Settings

Customize your widget using the built-in settings panel.

- GitHub username
- Theme and contribution shape
- Heatmap time range
- Widget position
- Lock and desktop settings
- Sound effects
- Startup preferences

Settings are stored locally:

```text
%APPDATA%\GitHubDesktopWidget\config.json
```

## Development

### Requirements

- Windows 10 or 11
- .NET 8 SDK
- Visual Studio or VS Code

### Clone the Repository

```powershell
git clone https://github.com/SAHAJCSE/GitWidget-SAHAJ.git

cd GitWidget-SAHAJ/windows-native-csharp
```

### Build and Run

```powershell
dotnet build GitWidget.slnx

dotnet run --project GithubWidget.csproj
```

### Publish

```powershell
dotnet publish GithubWidget.csproj -c Release -r win-x64 -o Application
```

The published application will be available inside the `Application` folder.

## Project Structure

```text
windows-native-csharp/
│
├── Assets/          Application icons
├── images/          Screenshots
├── Models/          Data models and configuration
├── Services/        GitHub API and sound services
├── Setup/           Installer application
│
├── MainWindow.xaml  Main widget
├── SettingsWindow   Widget settings
├── DetailWindow     Activity analytics
│
├── GithubWidget.csproj
├── GitWidget.slnx
├── GitWidget-Setup.exe
├── Install.bat
├── Install.ps1
└── README.md
```

## Privacy

- Uses public GitHub contribution data.
- Does not require your GitHub password.
- Does not request a personal access token.
- Stores widget preferences locally.
- Does not require a browser running in the background.

## Built With

- C#
- .NET 8
- Windows Presentation Foundation (WPF)
- GitHub Contributions API

## Author

**Sahaj**

GitHub: [@SAHAJCSE](https://github.com/SAHAJCSE)

Inspired by Apple's Liquid Glass design and minimal desktop widgets.

---

<p align="center">
  Made with ❤️ by Sahaj
  <br />
  If you like this project, consider giving it a ⭐
</p>
