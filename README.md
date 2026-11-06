 <div align="center">

<img src="assets/hero.svg" width="100%" alt="GitWidget-SAHAJ">

# GitWidget-SAHAJ

### Your GitHub activity. Right on your Windows desktop.

A minimal, ultra-lightweight native Windows widget that brings your GitHub contribution heatmap directly to your desktop.

<p>
<img src="https://img.shields.io/badge/Platform-Windows%2011-0078D4?style=flat-square&logo=windows" />
<img src="https://img.shields.io/badge/Language-C%23-68217A?style=flat-square&logo=csharp" />
<img src="https://img.shields.io/badge/Framework-WPF-512BD4?style=flat-square&logo=dotnet" />
<img src="https://img.shields.io/badge/Refresh-15%20Minutes-16A34A?style=flat-square" />
<img src="https://img.shields.io/badge/Design-Liquid%20Glass-black?style=flat-square" />
</p>

[Features](#-features) · [Screenshots](#-screenshots) · [Installation](#-installation) · [Customization](#-customization) · [Roadmap](#-roadmap)

</div>

---

## ✦ About the project

**GitWidget-SAHAJ** is a lightweight GitHub contribution monitoring widget built specifically for Windows.

Instead of opening GitHub every time you want to check your coding activity, see your contribution heatmap directly on your desktop.

The philosophy is simple:

**Less UI. Less resource usage. More focus.**

The application is built using native C# and Windows Presentation Foundation (WPF), avoiding the overhead of Electron and browser-based desktop wrappers.

## 🖥 Screenshots

<div align="center">

### Desktop Widget — Liquid Glass / GitHub Green

<img src="assets/widget-green-desktop.png" width="85%" alt="GitWidget green contribution heatmap on desktop">

*Minimal contribution heatmap with GitHub activity count.*

<br/>

### Desktop Widget — Dark Theme / Triangle Grid

<img src="assets/widget-dark-desktop.png" width="85%" alt="GitWidget dark theme with triangle contribution cells">

*Custom contribution shapes with a compact dark interface.*

</div>

---

## ✨ Features

| Feature | Description |
|---|---|
| GitHub Heatmap | View your contribution activity visually |
| Contribution Count | See your total contributions |
| 30D / 6M | Switch between activity periods |
| Auto Refresh | Automatic synchronization every 15 minutes |
| Account Connection | Connect using your GitHub username |
| Activity Details | Expanded 12-month calendar and top repository details |
| Desktop Widget | Compact desktop-first interface |
| Lightweight | Designed for minimal resource consumption |

## 🎨 Customization

### Color Themes

Choose the appearance that matches your desktop.

| Theme | Style |
|---|---|
| Apple B&W | Minimal monochrome |
| Apple Light | Clean light appearance |
| Liquid Glass | Translucent glass-inspired appearance |
| GitHub Classic | Traditional GitHub green |
| Midnight OLED | Deep black interface |
| Aurora Teal | Modern teal accents |

### Contribution Shapes

Customize the appearance of individual contribution cells.

`● Circle` · `▢ Rounded` · `■ Square` · `⬢ Hexagon` · `▲ Triangle` · `✿ Flower`

### Widget Settings

- GitHub username connection
- Contribution time range
- Theme selection
- Contribution shape selection
- Detailed activity and top repositories
- Widget positioning and appearance adjustments

---

## ⚡ Lightweight Architecture

Built with native Windows technologies rather than a browser-based runtime.

| Component | Technology |
|---|---|
| Language | C# |
| UI Framework | WPF |
| Platform | Windows 10 / 11 |
| Data Source | GitHub contribution data |
| Refresh Interval | 15 minutes |
| Configuration | Local application settings |

**Optimization target:** Approximately 200 KB application footprint. Actual packaged size depends on deployment and runtime dependencies.

---

## 🚀 Installation

### Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Git

### Clone the repository

```bash
git clone https://github.com/SAHAJCSE/GitWidget-SAHAJ.git
```

### Navigate to the application

```bash
cd GitWidget-SAHAJ/windows-native-csharp
```

### Restore dependencies

```bash
dotnet restore
```

### Run the widget

```bash
dotnet run
```

### Build for Windows

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

The published application will be available inside the project's `bin/Release` directory.

---

## 📂 Project Structure

```text
GitWidget-SAHAJ/
│
├── assets/
│   ├── hero.svg
│   ├── widget-green-desktop.png
│   └── widget-dark-desktop.png
│
├── windows-native-csharp/
│   ├── Assets/
│   ├── Models/
│   ├── Services/
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── SettingsWindow.xaml
│   └── GithubWidget.csproj
│
└── README.md
```

---

## 🗺 Roadmap

- [x] Native Windows widget concept
- [x] GitHub contribution heatmap
- [x] Compact desktop interface
- [x] Multiple color themes
- [x] Custom contribution shapes
- [x] 30-day and 6-month views
- [x] Settings interface
- [ ] Further memory and startup optimization
- [ ] Multi-monitor positioning improvements
- [ ] Final application packaging and size optimization

---

## 💡 Why GitWidget?

Most developers use GitHub to track their progress, but checking contribution activity requires opening a browser.

GitWidget makes that information instantly accessible.

No unnecessary dashboard. No browser tabs. Just your coding activity, always in sight.

---

## 🤝 Contributing

Contributions, suggestions and improvements are welcome.

1. Fork this repository.
2. Create a feature branch.
3. Make your changes.
4. Submit a Pull Request.

---

<div align="center">

### Built for developers. Designed for simplicity.

**GitWidget-SAHAJ**

[GitHub Repository](https://github.com/SAHAJCSE/GitWidget-SAHAJ)

⭐ If you like the project, consider giving it a star!

</div>
