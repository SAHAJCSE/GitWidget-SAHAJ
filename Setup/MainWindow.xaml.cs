using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace GitWidgetSetup
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _cursorTimer = new DispatcherTimer();
        private bool _isBusy = false;

        public MainWindow()
        {
            InitializeComponent();

            _cursorTimer.Interval = TimeSpan.FromMilliseconds(500);
            _cursorTimer.Tick += (s, e) =>
            {
                BlinkingCursor.Visibility = BlinkingCursor.Visibility == Visibility.Visible
                    ? Visibility.Hidden
                    : Visibility.Visible;
            };
            _cursorTimer.Start();

            Loaded += async (s, e) =>
            {
                await AnimateHeroEntrance();
                await StartPrerequisitesCheckAndSetupAsync();
            };
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task AnimateHeroEntrance()
        {
            var fadeIn = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(450));
            SahajTitleText.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            SahajSubtitleText.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            await Task.Delay(200);
        }

        private async Task AnimateTypewriterAsync(string message)
        {
            TypewriterStatus.Text = "";
            foreach (char c in message)
            {
                TypewriterStatus.Text += c;
                await Task.Delay(18);
            }
            await Task.Delay(250);
        }

        private void AddLogLine(string text, string colorHex = "#9E9EA4")
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10.5,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)!,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 2, 0, 2),
                Opacity = 0.0
            };

            LogPanel.Children.Add(tb);

            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(260));
            tb.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            if (LogPanel.Parent is ScrollViewer scroller)
            {
                scroller.ScrollToBottom();
            }
        }

        private void SetProgress(double percent)
        {
            double targetWidth = Math.Clamp((percent / 100.0) * 476.0, 0, 476.0);
            var anim = new DoubleAnimation(ProgressBarFill.ActualWidth, targetWidth, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ProgressBarFill.BeginAnimation(FrameworkElement.WidthProperty, anim);
        }

        private async Task StartPrerequisitesCheckAndSetupAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            ActionBtn.IsEnabled = false;

            try
            {
                // Step 1: Scan System Environment
                await AnimateTypewriterAsync("Scanning Windows environment...");
                AddLogLine("[1/4] Detecting Windows OS architecture & runtimes...", "#8E8E93");
                SetProgress(15);
                await Task.Delay(350);

                // Step 2: Check .NET 8.0 Desktop Runtime
                await AnimateTypewriterAsync("Verifying Microsoft .NET 8 Desktop Runtime...");
                bool isNet8Installed = CheckNet8DesktopRuntime();

                if (isNet8Installed)
                {
                    AddLogLine("  [✓] Microsoft .NET 8.0 Desktop Runtime detected", "#39D353");
                    AddLogLine("      (Prerequisite satisfied • skipping download)", "#8E8E93");
                    SetProgress(50);
                    await Task.Delay(400);
                }
                else
                {
                    AddLogLine("  [!] .NET 8 Desktop Runtime not found on this system.", "#FFAA33");
                    await AnimateTypewriterAsync("Installing prerequisite: .NET 8 Desktop Runtime...");
                    SetProgress(30);

                    bool installed = await InstallNet8RuntimeAsync();
                    if (installed)
                    {
                        AddLogLine("  [✓] Microsoft .NET 8.0 Desktop Runtime installed successfully!", "#39D353");
                        SetProgress(60);
                    }
                    else
                    {
                        AddLogLine("  [⚠] Automatic runtime installation could not be completed silently.", "#FF5555");
                        AddLogLine("      Please install .NET 8 Desktop Runtime from https://dotnet.microsoft.com", "#FF8888");
                    }
                }

                // Step 3: Verify & Locate Widget Executable
                await AnimateTypewriterAsync("Configuring GitWidget-SAHAJ bundle...");
                string? targetExe = FindWidgetExecutable();

                if (targetExe != null && File.Exists(targetExe))
                {
                    var fileInfo = new FileInfo(targetExe);
                    double kb = fileInfo.Length / 1024.0;
                    AddLogLine($"  [✓] Widget binary verified: {fileInfo.Name} ({kb:F0} KB)", "#39D353");
                }
                else
                {
                    AddLogLine("  [!] GitHubDesktopWidget.exe not found in immediate folder.", "#FFAA33");
                }
                SetProgress(80);
                await Task.Delay(300);

                // Step 4: Configure Shortcuts & AutoStart
                if (ChkShortcut.IsChecked == true && targetExe != null)
                {
                    CreateDesktopShortcut(targetExe);
                    AddLogLine("  [✓] Desktop shortcut created: 'GitWidget - SAHAJ.lnk'", "#58D77B");
                }

                if (ChkAutoStart.IsChecked == true && targetExe != null)
                {
                    ConfigureStartup(targetExe, true);
                    AddLogLine("  [✓] Configured to automatically start with Windows", "#58D77B");
                }

                SetProgress(100);
                await AnimateTypewriterAsync("GitWidget-SAHAJ is ready to run!");
                AddLogLine("All prerequisites satisfied. Launching widget...", "#FFFFFF");

                ActionBtn.Content = "Launch Now";
                ActionBtn.IsEnabled = true;

                // Auto launch after 1.5 seconds if everything succeeded
                await Task.Delay(1400);
                LaunchWidget(targetExe);
            }
            catch (Exception ex)
            {
                AddLogLine($"Error during setup: {ex.Message}", "#FF5555");
                ActionBtn.IsEnabled = true;
            }
            finally
            {
                _isBusy = false;
            }
        }

        private bool CheckNet8DesktopRuntime()
        {
            try
            {
                // 1. Registry Inspection for Microsoft.WindowsDesktop.App 8.0
                string[] regPaths = new[]
                {
                    @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App",
                    @"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App"
                };

                foreach (var path in regPaths)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            if (subKeyName.StartsWith("8.0", StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                        foreach (var valName in key.GetValueNames())
                        {
                            if (valName.StartsWith("8.0", StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                    }
                }

                // 2. Query dotnet CLI list-runtimes if available
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);
                    if (output.Contains("Microsoft.WindowsDesktop.App 8."))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private async Task<bool> InstallNet8RuntimeAsync()
        {
            // Attempt 1: Try Winget silent install
            try
            {
                AddLogLine("      Attempting installation via Windows Package Manager (winget)...", "#8E8E93");
                var wingetPsi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install Microsoft.DotNet.DesktopRuntime.8 --silent --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var wingetProc = Process.Start(wingetPsi);
                if (wingetProc != null)
                {
                    await Task.Run(() => wingetProc.WaitForExit(120000));
                    if (wingetProc.ExitCode == 0 || CheckNet8DesktopRuntime())
                        return true;
                }
            }
            catch { }

            // Attempt 2: Direct download from Microsoft official CDN
            try
            {
                AddLogLine("      Downloading official Microsoft .NET 8 Desktop Runtime installer...", "#8E8E93");
                string downloadUrl = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe";
                string tempFile = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-8.0-win-x64.exe");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(4);
                    var data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempFile, data);
                }

                AddLogLine("      Executing silent runtime installer...", "#8E8E93");
                var instPsi = new ProcessStartInfo
                {
                    FileName = tempFile,
                    Arguments = "/install /quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var instProc = Process.Start(instPsi);
                if (instProc != null)
                {
                    await Task.Run(() => instProc.WaitForExit(120000));
                    return CheckNet8DesktopRuntime() || instProc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                AddLogLine($"      Download/Install error: {ex.Message}", "#FF5555");
            }

            return false;
        }

        private string? FindWidgetExecutable()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();

            var searchRoots = new[] { baseDir, currentDir };
            foreach (var root in searchRoots)
            {
                var dir = new DirectoryInfo(root);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    string p1 = Path.Combine(dir.FullName, "GitHubDesktopWidget.exe");
                    if (File.Exists(p1)) return Path.GetFullPath(p1);

                    string p2 = Path.Combine(dir.FullName, "windows-native-csharp", "GitHubDesktopWidget.exe");
                    if (File.Exists(p2)) return Path.GetFullPath(p2);

                    string p3 = Path.Combine(dir.FullName, "Application", "GitHubDesktopWidget.exe");
                    if (File.Exists(p3)) return Path.GetFullPath(p3);

                    string p4 = Path.Combine(dir.FullName, "windows-native-csharp", "Application", "GitHubDesktopWidget.exe");
                    if (File.Exists(p4)) return Path.GetFullPath(p4);

                    dir = dir.Parent;
                }
            }

            return null;
        }

        private void CreateDesktopShortcut(string targetExe)
        {
            try
            {
                var candidates = new HashSet<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")
                };

                string workDir = Path.GetDirectoryName(targetExe) ?? "";

                foreach (var folder in candidates)
                {
                    if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) continue;
                    string shortcutPath = Path.Combine(folder, "GitWidget - SAHAJ.lnk");

                    string script = $"$ws = New-Object -ComObject WScript.Shell; " +
                                    $"$s = $ws.CreateShortcut('{shortcutPath.Replace("'", "''")}'); " +
                                    $"$s.TargetPath = '{targetExe.Replace("'", "''")}'; " +
                                    $"$s.WorkingDirectory = '{workDir.Replace("'", "''")}'; " +
                                    $"$s.IconLocation = '{targetExe.Replace("'", "''")},0'; " +
                                    $"$s.Description = 'GitWidget - SAHAJ Desktop Activity Monitor'; " +
                                    $"$s.Save()";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(2500);
                }
            }
            catch { }
        }

        private void ConfigureStartup(string targetExe, bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable)
                        key.SetValue("GitHubDesktopWidget", $"\"{targetExe}\"");
                    else
                        key.DeleteValue("GitHubDesktopWidget", false);
                }
            }
            catch { }
        }

        private void LaunchWidget(string? targetExe = null)
        {
            try
            {
                targetExe ??= FindWidgetExecutable();
                if (targetExe != null && File.Exists(targetExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = Path.GetDirectoryName(targetExe),
                        UseShellExecute = true
                    });

                    // Smooth fade out and exit Setup
                    var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(400));
                    fadeOut.Completed += (s, e) => Close();
                    BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
                else
                {
                    MessageBox.Show("Could not find GitHubDesktopWidget.exe to launch.", "GitWidget Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch widget: {ex.Message}", "GitWidget Setup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActionBtn_Click(object sender, RoutedEventArgs e)
        {
            LaunchWidget();
        }
    }
}
