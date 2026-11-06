<<<<<<< HEAD
# Native Windows GitHub Desktop Widget (C# / .NET 8 / WPF)

This is the **100% native Windows implementation** built with **C# & XAML (WPF)**—the standard, native programming language for Windows desktop applications and widgets.

### Why this is the proper native Windows solution:
- **No Electron / No Web Browser overhead**: Uses pure Windows Presentation Foundation (WPF) and .NET 8.
- **Microscopic memory usage**: Consumes ~15-25 MB of RAM compared to ~150 MB for Chromium/Electron.
- **Native Windows API integration**:
  - `ShowInTaskbar="False"`: It lives directly on your Windows desktop as a widget/gadget, not as a tab in your taskbar.
  - **Auto-starts with Windows**: Writes to Windows Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) with a single click.
  - **Position persistence**: Saves and restores exact screen coordinates to `%APPDATA%\GitHubDesktopWidget\config.json`.
  - **No Gradients**: Flat, solid, crisp dark surfaces (`#0E1612`, `#0D1117`, `#000000`, `#051417`).
  - **Circular contribution cells**: Rendered as native XAML vector `Ellipse` dots.
  - **Hover tracking**: Hovering over any circle instantly displays the day's contribution count and date.
  - **30D / 6M toggle**: Switch between 30 days and 6 months anytime from the header or right-click context menu.

---

### How to Build & Run:

1. Install the free [.NET 8 SDK for Windows](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Open PowerShell in this `windows-native-csharp` folder:
   ```powershell
   cd windows-native-csharp
   ```
3. Run the widget directly:
   ```powershell
   dotnet run
   ```
4. Or compile a single standalone `.exe` with zero dependencies:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```
   The resulting `GitHubDesktopWidget.exe` will be located in:
   `bin\Release\net8.0-windows\win-x64\publish\GitHubDesktopWidget.exe`
   You can place it anywhere on your PC or desktop and double-click to run!

=======
# GitWidget-SAHAJ

1. Project overview
GitWidget-SAHAJ is an ultra-compact GitHub activity monitor built for Windows 11. It brings GitHub's contribution graph to the desktop through a minimal floating widget.

The goal is to keep the application extremely lightweight (approximately 200 KB target), responsive, visually customizable, and efficient while automatically synchronizing GitHub contribution data every 15 minutes.
2. Core features
- GitHub contribution heatmap: Displays the yearly contribution history in a compact calendar layout.
- Contribution statistics: Shows total contributions alongside the selected time range.
- Time filters: Switch between 30-day and 6-month activity views.
- Automatic updates: Refreshes GitHub data every 15 minutes.
- Account connection: Connect using a GitHub username, such as SAHAJCSE.
- Activity details: Optional expanded 12-month calendar and top repository commit details.
- Desktop-only visibility: Appears on the desktop, not over other applications or fullscreen windows.
- Compact design: Minimal screen footprint without unnecessary panels or information.
3. Customization settings
Your settings panel includes:
Setting	Available options
Color themes	Apple B&W, Apple Light, Liquid Glass, GitHub Classic, Midnight OLED, Aurora Teal
Contribution shapes	Circle, Rounded, Square, Hexagon, Triangle, Flower
Time range	Configurable activity periods
GitHub account	Username connection
Activity details	12-month calendar and top repositories

Additional adjustments should include widget position, width, opacity, transparency, spacing, and automatic startup.
4. Design language
The interface follows an Apple-inspired minimalist design:
- Liquid Glass-inspired appearance.
- Dark translucent backgrounds.
- Subtle borders and soft highlights.
- Compact typography and spacing.
- Customizable contribution shapes.
- Minimal settings interface.
- No unnecessary animations or resource-heavy effects.
5. Technical specifications
Specification	Target
Application	GitWidget-SAHAJ
Platform	Windows 11
Framework	Native C# / WPF
Target footprint	~200 KB
Refresh interval	15 minutes
Data source	GitHub public contribution data
Network usage	Only during synchronization
Interface	Lightweight desktop widget
Display	Desktop only
>>>>>>> 7c66388cdd379becbd2bb6c7f3c205d66d365dbd
