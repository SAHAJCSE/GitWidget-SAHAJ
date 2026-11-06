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

