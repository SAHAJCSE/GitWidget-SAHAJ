using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using GitHubDesktopWidget.Models;
using GitHubDesktopWidget.Services;

namespace GitHubDesktopWidget
{
    public partial class SettingsWindow : Window
    {
        private readonly WidgetConfig _config;
        private readonly MainWindow _mainWindow;

        public SettingsWindow(WidgetConfig config, MainWindow mainWindow)
        {
            InitializeComponent();
            _config = config;
            _mainWindow = mainWindow;

            LoadUIFromConfig();
        }

        private void LoadUIFromConfig()
        {
            UsernameInput.Text = _config.Username;

            UpdateThemeUI(_config.Theme);
            UpdateShapeUI(_config.Shape);
            UpdateRangeUI(_config.Range);
            UpdateSoundSwitch(!_config.MuteSound);
            UpdatePinSwitch(!_config.Topmost);
            UpdateLockSwitch(_config.Locked);
            UpdateStartupSwitch(CheckStartupEnabled());
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private async void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            var user = UsernameInput.Text.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(user)) return;

            try
            {
                StatusMessage.Text = "Connecting...";
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x93));
                StatusMessage.Visibility = Visibility.Visible;

                await _mainWindow.ApplyUsername(user);

                StatusMessage.Text = $"Connected as @{user}";
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x39, 0xD3, 0x53));
            }
            catch (Exception ex)
            {
                StatusMessage.Text = ex.Message;
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55));
            }
        }

        private void UsernameInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SaveUser_Click(sender, e);
        }

        private void Theme_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border bd && bd.Tag is string theme)
            {
                _mainWindow.ApplyTheme(theme);
                UpdateThemeUI(theme);
            }
        }

        private void UpdateThemeUI(string activeTheme)
        {
            SetPillActive(ThemeMonoBtn, activeTheme == "mono");
            SetPillActive(ThemeLightBtn, activeTheme == "light");
            SetPillActive(ThemeLiquidBtn, activeTheme == "liquid");
            SetPillActive(ThemeClassicBtn, activeTheme == "classic");
            SetPillActive(ThemeOLEDBtn, activeTheme == "oled");
            SetPillActive(ThemeAuroraBtn, activeTheme == "aurora");
        }

        private void Shape_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border bd && bd.Tag is string shape)
            {
                _mainWindow.ApplyShape(shape);
                UpdateShapeUI(shape);
            }
        }

        private void UpdateShapeUI(string activeShape)
        {
            var s = (activeShape ?? "circle").ToLowerInvariant();
            SetPillActive(ShapeCircleBtn, s == "circle");
            SetPillActive(ShapeRoundedBtn, s == "rounded");
            SetPillActive(ShapeSquareBtn, s == "square");
            SetPillActive(ShapeHexagonBtn, s == "hexagon");
            SetPillActive(ShapeTriangleBtn, s == "triangle");
            SetPillActive(ShapeFlowerBtn, s == "flower");
        }

        private void SetPillActive(Border pill, bool isActive)
        {
            if (pill == null) return;
            pill.Background = isActive ? Brushes.White : (SolidColorBrush)new BrushConverter().ConvertFromString("#242428")!;
            pill.BorderBrush = isActive ? Brushes.White : (SolidColorBrush)new BrushConverter().ConvertFromString("#38383E")!;

            if (pill.Child is StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    if (child is TextBlock tb)
                    {
                        tb.Foreground = isActive ? Brushes.Black : Brushes.White;
                        tb.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
                    }
                    else if (child is Shape shp)
                    {
                        if (shp is not Ellipse && pill != ThemeMonoBtn && pill != ThemeLiquidBtn && pill != ThemeClassicBtn && pill != ThemeOLEDBtn && pill != ThemeAuroraBtn)
                        {
                            shp.Fill = isActive ? Brushes.Black : Brushes.White;
                        }
                    }
                }
            }
        }

        private void Range30D_Click(object sender, MouseButtonEventArgs e)
        {
            _mainWindow.SetRange("30d");
            UpdateRangeUI("30d");
        }

        private void Range6M_Click(object sender, MouseButtonEventArgs e)
        {
            _mainWindow.SetRange("6m");
            UpdateRangeUI("6m");
        }

        private void Range1Y_Click(object sender, MouseButtonEventArgs e)
        {
            _mainWindow.SetRange("1y");
            UpdateRangeUI("1y");
        }

        private void UpdateRangeUI(string range)
        {
            string r = (range ?? "6m").ToLowerInvariant();
            bool is30d = r == "30d";
            bool is6m = r == "6m";
            bool is1y = r == "1y" || r == "12m";

            Range30DBtn.Background = is30d ? (SolidColorBrush)new BrushConverter().ConvertFromString("#2C2C32")! : Brushes.Transparent;
            Range6MBtn.Background = is6m ? (SolidColorBrush)new BrushConverter().ConvertFromString("#2C2C32")! : Brushes.Transparent;
            Range1YBtn.Background = is1y ? (SolidColorBrush)new BrushConverter().ConvertFromString("#2C2C32")! : Brushes.Transparent;
        }

        private void ToggleSound_Click(object sender, MouseButtonEventArgs e)
        {
            _config.MuteSound = !_config.MuteSound;
            SoundService.IsMuted = _config.MuteSound;
            UpdateSoundSwitch(!_config.MuteSound);
        }

        private void UpdateSoundSwitch(bool isEnabled)
        {
            ToggleSoundSwitch.Background = isEnabled
                ? (SolidColorBrush)new BrushConverter().ConvertFromString("#34C759")!
                : (SolidColorBrush)new BrushConverter().ConvertFromString("#38383E")!;
            ToggleSoundThumb.HorizontalAlignment = isEnabled ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private void TogglePin_Click(object sender, MouseButtonEventArgs e)
        {
            _mainWindow.ToggleTopmost();
            UpdatePinSwitch(!_config.Topmost);
        }

        private void UpdatePinSwitch(bool isPinned)
        {
            TogglePinSwitch.Background = isPinned
                ? (SolidColorBrush)new BrushConverter().ConvertFromString("#34C759")!
                : (SolidColorBrush)new BrushConverter().ConvertFromString("#38383E")!;
            TogglePinThumb.HorizontalAlignment = isPinned ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private void ToggleLock_Click(object sender, MouseButtonEventArgs e)
        {
            _mainWindow.ToggleLock();
            UpdateLockSwitch(_config.Locked);
        }

        private void UpdateLockSwitch(bool isLocked)
        {
            ToggleLockSwitch.Background = isLocked
                ? (SolidColorBrush)new BrushConverter().ConvertFromString("#34C759")!
                : (SolidColorBrush)new BrushConverter().ConvertFromString("#38383E")!;
            ToggleLockThumb.HorizontalAlignment = isLocked ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private void ToggleStartup_Click(object sender, MouseButtonEventArgs e)
        {
            bool current = CheckStartupEnabled();
            SetStartup(!current);
            UpdateStartupSwitch(!current);
        }

        private bool CheckStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("GitHubDesktopWidget") != null;
            }
            catch { return false; }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (enable && !string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("GitHubDesktopWidget", $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue("GitHubDesktopWidget", false);
                    }
                }
            }
            catch { }
        }

        private void UpdateStartupSwitch(bool isEnabled)
        {
            ToggleStartupSwitch.Background = isEnabled
                ? (SolidColorBrush)new BrushConverter().ConvertFromString("#34C759")!
                : (SolidColorBrush)new BrushConverter().ConvertFromString("#38383E")!;
            ToggleStartupThumb.HorizontalAlignment = isEnabled ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private async void RefreshAction_Click(object sender, RoutedEventArgs e)
        {
            await _mainWindow.RefreshDataAsync(true);
            StatusMessage.Text = "Activity cache refreshed!";
            StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(0x39, 0xD3, 0x53));
            StatusMessage.Visibility = Visibility.Visible;
        }

        private void OpenProfileAction_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.OpenProfile();
        }

        private DetailWindow? _detailWindow;

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (_detailWindow != null && _detailWindow.IsLoaded)
            {
                _detailWindow.Activate();
                return;
            }

            _detailWindow = new DetailWindow(_config.Username, _config.Theme, _config.Shape);
            _detailWindow.Closed += (s, ev) => _detailWindow = null;
            _detailWindow.Show();
        }
    }
}
