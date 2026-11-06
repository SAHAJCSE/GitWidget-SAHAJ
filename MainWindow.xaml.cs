using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using GitHubDesktopWidget.Models;
using GitHubDesktopWidget.Services;

namespace GitHubDesktopWidget
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int WM_ACTIVATE = 0x0006;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_SHOWWINDOW = 0x0018;

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        private readonly GitHubService _service = new GitHubService();
        private WidgetConfig _config = new WidgetConfig();
        private WidgetData? _currentData;
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();
        private readonly DispatcherTimer _blinkTimer = new DispatcherTimer();
        private readonly string _configPath;

        private readonly Dictionary<string, string[]> _themeColors = new Dictionary<string, string[]>
        {
            ["mono"] = new[] { "#202022", "#48484A", "#7C7C80", "#AEAEB2", "#FFFFFF" },
            ["light"] = new[] { "#EBEDF0", "#9BE9A8", "#40C463", "#30A14E", "#216E39" },
            ["liquid"] = new[] { "#1C2B20", "#0E4429", "#006D32", "#26A641", "#39D353" },
            ["classic"] = new[] { "#161B22", "#0E4429", "#006D32", "#26A641", "#39D353" },
            ["oled"] = new[] { "#121212", "#003816", "#006B2B", "#00B849", "#00FF66" },
            ["aurora"] = new[] { "#0E2429", "#0A3D42", "#087F8C", "#00BFB2", "#00FFC8" }
        };

        public MainWindow()
        {
            InitializeComponent();

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = System.IO.Path.Combine(appData, "GitHubDesktopWidget");
            Directory.CreateDirectory(folder);
            _configPath = System.IO.Path.Combine(folder, "config.json");

            LoadConfig();
            ApplyTheme(_config.Theme);
            ApplyRangeUI();
            ApplyShapeUI();

            _refreshTimer.Interval = TimeSpan.FromMinutes(15);
            _refreshTimer.Tick += async (s, e) => await RefreshDataAsync(false);
            _refreshTimer.Start();

            _blinkTimer.Interval = TimeSpan.FromSeconds(3.6);
            _blinkTimer.Tick += (s, e) => BlinkBotEyes();
            _blinkTimer.Start();

            Loaded += async (s, e) =>
            {
                UpdateHeaderResponsiveness();
                if (string.IsNullOrWhiteSpace(_config.Username))
                {
                    ShowSetup(true);
                }
                else
                {
                    ShowSetup(false);
                    await RefreshDataAsync(true);
                }
            };

            LocationChanged += (s, e) => SaveBounds();
            SizeChanged += (s, e) =>
            {
                SaveBounds();
                UpdateHeaderResponsiveness();
            };

            Activated += (s, e) =>
            {
                if (_currentData != null) AnimateColumnsEntrance();
            };

            BotContainer.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                AnimateColumnsEntrance(true);
            };
        }

        private void UpdateHeaderResponsiveness()
        {
            bool isSmall = _config.Range == "30d" || ActualWidth < 340;

            // Remove switch and contribution text line on small widgets to eliminate overlapping
            if (RangeSwitchBorder != null)
                RangeSwitchBorder.Visibility = isSmall ? Visibility.Collapsed : Visibility.Visible;
            if (StatLabelText != null)
                StatLabelText.Visibility = isSmall ? Visibility.Collapsed : Visibility.Visible;
            if (BtnSettingsHeader != null)
                BtnSettingsHeader.Visibility = Visibility.Visible;
        }

        private void BlinkBotEyes()
        {
            try
            {
                var blinkAnim = new DoubleAnimationUsingKeyFrames
                {
                    Duration = TimeSpan.FromMilliseconds(220)
                };
                blinkAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                blinkAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.08, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
                blinkAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.08, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(130))));
                blinkAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));

                BotEyeScaleL?.BeginAnimation(ScaleTransform.ScaleYProperty, blinkAnim);
                BotEyeScaleR?.BeginAnimation(ScaleTransform.ScaleYProperty, blinkAnim);
            }
            catch { }
        }

        private void AttachToDesktop()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (!_config.Topmost)
                {
                    IntPtr progman = FindWindow("Progman", null);
                    if (progman != IntPtr.Zero)
                    {
                        helper.Owner = progman;
                    }
                }
                else
                {
                    helper.Owner = IntPtr.Zero;
                }
            }
            catch { }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);

                // WS_EX_TOOLWINDOW: Hides the window from Alt+Tab switcher & Task View
                // WS_EX_NOACTIVATE: Prevents stealing focus or flashing over other apps
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

                AttachToDesktop();
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWWINDOW)
            {
                // Never disappear on Win+D (Show Desktop)
                int reason = lParam.ToInt32();
                if (!_config.Topmost && wParam == IntPtr.Zero && reason != 0)
                {
                    handled = true;
                    Dispatcher.InvokeAsync(() => { if (_currentData != null) AnimateColumnsEntrance(); });
                    return IntPtr.Zero;
                }
                Dispatcher.InvokeAsync(() => { if (_currentData != null) AnimateColumnsEntrance(); });
            }

            if (msg == WM_ACTIVATE)
            {
                int state = unchecked((short)(long)wParam);
                if (state != 0) // WA_ACTIVE or WA_CLICKACTIVE
                {
                    Dispatcher.InvokeAsync(() => { if (_currentData != null) AnimateColumnsEntrance(); });
                }
            }

            if (msg == WM_NCHITTEST)
            {
                int x = unchecked((short)(long)lParam);
                int y = unchecked((short)((long)lParam >> 16));

                var clientPoint = PointFromScreen(new Point(x, y));
                const double grip = 7.0;

                double w = ActualWidth;
                double h = ActualHeight;

                bool isLeft = clientPoint.X <= grip;
                bool isRight = clientPoint.X >= w - grip;
                bool isTop = clientPoint.Y <= grip;
                bool isBottom = clientPoint.Y >= h - grip;

                if (isTop && isLeft)
                {
                    handled = true;
                    return new IntPtr(HTTOPLEFT);
                }
                if (isTop && isRight)
                {
                    handled = true;
                    return new IntPtr(HTTOPRIGHT);
                }
                if (isBottom && isLeft)
                {
                    handled = true;
                    return new IntPtr(HTBOTTOMLEFT);
                }
                if (isBottom && isRight)
                {
                    handled = true;
                    return new IntPtr(HTBOTTOMRIGHT);
                }
                if (isLeft)
                {
                    handled = true;
                    return new IntPtr(HTLEFT);
                }
                if (isRight)
                {
                    handled = true;
                    return new IntPtr(HTRIGHT);
                }
                if (isTop)
                {
                    handled = true;
                    return new IntPtr(HTTOP);
                }
                if (isBottom)
                {
                    handled = true;
                    return new IntPtr(HTBOTTOM);
                }
            }

            return IntPtr.Zero;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<WidgetConfig>(json) ?? new WidgetConfig();
                }
            }
            catch { }

            // Fixed on desktop (Topmost = false) by default
            Topmost = _config.Topmost;
            MenuTopmost.IsChecked = !_config.Topmost;
            MenuLock.Header = _config.Locked ? "Enable Moving" : "Disable Moving (Lock)";

            // Check Windows startup status
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                MenuStartup.IsChecked = key?.GetValue("GitHubDesktopWidget") != null;
            }
            catch { }

            // Restore position and size
            if (_config.Width == 580 || _config.Width <= 0) Width = 495;
            else if (_config.Width > 150) Width = _config.Width;

            if (_config.Height == 165 || _config.Height <= 0) Height = 160;
            else if (_config.Height > 100) Height = _config.Height;

            if (_config.X >= 0 && _config.Y >= 0)
            {
                Left = _config.X;
                Top = _config.Y;
            }
            else
            {
                // Default top-right of main desktop screen
                Left = SystemParameters.WorkArea.Right - Width - 30;
                Top = SystemParameters.WorkArea.Top + 36;
            }
        }

        private void SaveBounds()
        {
            if (WindowState == WindowState.Normal)
            {
                _config.X = Left;
                _config.Y = Top;
                _config.Width = Width;
                _config.Height = Height;
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private void ShowSetup(bool show)
        {
            SetupPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ContentPanel.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                UsernameInput.Text = _config.Username;
                UsernameInput.Focus();
            }
        }

        public async System.Threading.Tasks.Task RefreshDataAsync(bool forceRefresh = false)
        {
            if (string.IsNullOrWhiteSpace(_config.Username)) return;

            try
            {
                _currentData = await _service.GetContributionsAsync(_config.Username, _config.Range, forceRefresh);
                RenderHeatmap(_currentData);
                UpdateTotalDisplay();
                UpdateBotExpression();
            }
            catch (Exception ex)
            {
                StatLabelText.Text = "error loading data";
                Debug.WriteLine(ex.Message);
            }
        }

        private string _lastStatNumber = "";
        private DispatcherTimer? _countRollTimer;

        private void AnimateStatRollingChange(string newNumber, string newLabel, bool rollUp = true)
        {
            if (_lastStatNumber == newNumber && StatLabelText.Text == newLabel) return;
            _lastStatNumber = newNumber;
            StatLabelText.Text = newLabel;

            double startY = rollUp ? 14.0 : -14.0;
            StatNumberText.Text = newNumber;

            var rollAnim = new DoubleAnimation
            {
                From = startY,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
            };

            var fadeAnim = new DoubleAnimation
            {
                From = 0.35,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleYAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
            };

            StatNumberTranslate.BeginAnimation(TranslateTransform.YProperty, rollAnim);
            StatNumberText.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            StatNumberScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        private void UpdateTotalDisplay(bool animated = false)
        {
            if (_currentData == null) return;
            string targetText = _currentData.TotalCount.ToString("N0");
            string labelText = $"{(_config.Range == "30d" ? "30D" : "6M")} contributions";

            if (animated)
            {
                AnimateStatRollingChange(targetText, labelText, rollUp: false);
            }
            else
            {
                StatLabelText.Text = labelText;
                RollCountTo(_currentData.TotalCount);
            }
        }

        private void RollCountTo(int target)
        {
            _countRollTimer?.Stop();
            if (target <= 0)
            {
                StatNumberText.Text = "0";
                _lastStatNumber = "0";
                return;
            }

            int start = 0;
            int frames = 20;
            int currentFrame = 0;

            _countRollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _countRollTimer.Tick += (s, e) =>
            {
                currentFrame++;
                double progress = (double)currentFrame / frames;
                double ease = 1 - Math.Pow(1 - progress, 3);
                int val = (int)(start + target * ease);
                StatNumberText.Text = val.ToString("N0");
                _lastStatNumber = StatNumberText.Text;

                if (currentFrame >= frames)
                {
                    StatNumberText.Text = target.ToString("N0");
                    _lastStatNumber = StatNumberText.Text;
                    _countRollTimer.Stop();
                }
            };
            _countRollTimer.Start();
        }

        private static Brush CreateHoverGradient(int level, Color baseColor, bool isLight = false)
        {
            if (level <= 0)
            {
                if (isLight)
                {
                    return new RadialGradientBrush
                    {
                        Center = new Point(0.4, 0.3),
                        GradientOrigin = new Point(0.4, 0.3),
                        RadiusX = 0.75,
                        RadiusY = 0.75,
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(235, 220, 225, 230), 0.0),
                            new GradientStop(Color.FromArgb(180, 195, 200, 205), 1.0)
                        }
                    };
                }

                return new RadialGradientBrush
                {
                    Center = new Point(0.4, 0.3),
                    GradientOrigin = new Point(0.4, 0.3),
                    RadiusX = 0.75,
                    RadiusY = 0.75,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(220, 65, 105, 80), 0.0),
                        new GradientStop(Color.FromArgb(170, 22, 38, 28), 1.0)
                    }
                };
            }

            // Active contribution: Vibrant luminous pearl gradient
            Color highlight = Color.FromArgb(
                255,
                (byte)Math.Min(255, baseColor.R + 65),
                (byte)Math.Min(255, baseColor.G + 75),
                (byte)Math.Min(255, baseColor.B + 65));

            Color shadow = Color.FromArgb(
                255,
                (byte)(baseColor.R * 0.75),
                (byte)(baseColor.G * 0.75),
                (byte)(baseColor.B * 0.75));

            return new RadialGradientBrush
            {
                Center = new Point(0.35, 0.3),
                GradientOrigin = new Point(0.35, 0.3),
                RadiusX = 0.75,
                RadiusY = 0.75,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(highlight, 0.0),
                    new GradientStop(baseColor, 0.7),
                    new GradientStop(shadow, 1.0)
                }
            };
        }

        private void RenderHeatmap(WidgetData data)
        {
            HeatmapGrid.Children.Clear();
            HeatmapGrid.ColumnDefinitions.Clear();

            MonthLabelsGrid.Children.Clear();
            MonthLabelsGrid.ColumnDefinitions.Clear();

            int colCount = data.Weeks.Count;
            if (colCount == 0) return;

            bool isLight = _config.Theme == "light";

            // 1. Build synchronized proportional columns for perfect responsiveness
            for (int i = 0; i < colCount; i++)
            {
                HeatmapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                MonthLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var colors = _themeColors.ContainsKey(_config.Theme) ? _themeColors[_config.Theme] : _themeColors["liquid"];

            // 2. Render each week column as a Grid with 7 rows
            for (int col = 0; col < colCount; col++)
            {
                var colGrid = new Grid();
                for (int r = 0; r < 7; r++)
                {
                    colGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }

                var scaleTransform = new ScaleTransform(1.0, 1.0);
                colGrid.RenderTransform = scaleTransform;
                colGrid.RenderTransformOrigin = new Point(0.5, 0.5);

                var week = data.Weeks[col];
                for (int row = 0; row < week.Count; row++)
                {
                    var day = week[row];

                    SolidColorBrush fillBrush = day.Level < 0
                        ? Brushes.Transparent
                        : (SolidColorBrush)new BrushConverter().ConvertFromString(colors[Math.Clamp(day.Level, 0, 4)])!;

                    var shape = CreateDayShape(_config.Shape, fillBrush);
                    var hoverBrush = CreateHoverGradient(day.Level, fillBrush.Color, isLight);

                    // Assign clear tooltip for every card
                    if (DateTime.TryParse(day.Date, out var dtDay))
                    {
                        shape.ToolTip = day.IsFuture
                            ? $"Future: {dtDay:MMM dd, yyyy}"
                            : $"{day.Count} {(day.Count == 1 ? "contribution" : "contributions")} on {dtDay:MMM dd, yyyy}";
                    }

                    // Hover to show daily contributions with bouncy scale + gradient + rolling stat counter
                    shape.MouseEnter += (s, e) =>
                    {
                        // 1. Luminous gradient on hover
                        if (shape is Shape shp)
                        {
                            shp.Fill = hoverBrush;
                        }

                        // 2. Bouncy pop effect
                        Panel.SetZIndex(shape, 100);
                        Panel.SetZIndex(colGrid, 100);
                        if (shape.RenderTransform is ScaleTransform st)
                        {
                            var bounceAnim = new DoubleAnimation
                            {
                                To = 1.38,
                                Duration = TimeSpan.FromMilliseconds(160),
                                EasingFunction = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut }
                            };
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, bounceAnim);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, bounceAnim);
                        }

                        // 3. Animated rolling change in contribution number only
                        string countText = day.Count.ToString();
                        string dateText = DateTime.TryParse(day.Date, out var dt) ? $"on {dt:MMM dd, yyyy}" : "";
                        AnimateStatRollingChange(countText, dateText, rollUp: true);
                    };

                    shape.MouseLeave += (s, e) =>
                    {
                        // 1. Restore original solid fill
                        if (shape is Shape shp)
                        {
                            shp.Fill = fillBrush;
                        }

                        // 2. Smoothly bounce back to normal scale
                        if (shape.RenderTransform is ScaleTransform st)
                        {
                            var resetAnim = new DoubleAnimation
                            {
                                To = 1.0,
                                Duration = TimeSpan.FromMilliseconds(140),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            };
                            resetAnim.Completed += (s2, e2) =>
                            {
                                Panel.SetZIndex(shape, 0);
                                Panel.SetZIndex(colGrid, 0);
                            };
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, resetAnim);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, resetAnim);
                        }

                        // 3. Roll back to total contributions display
                        UpdateTotalDisplay(animated: true);
                    };

                    Grid.SetRow(shape, row);
                    colGrid.Children.Add(shape);
                }

                Grid.SetColumn(colGrid, col);
                HeatmapGrid.Children.Add(colGrid);
            }

            // 3. Render Month Labels directly into synchronized grid columns
            foreach (var (idx, month) in data.MonthLabels)
            {
                if (idx < colCount)
                {
                    var lbl = new TextBlock
                    {
                        Text = month,
                        FontSize = 8,
                        Foreground = isLight ? new SolidColorBrush(Color.FromRgb(110, 110, 115)) : new SolidColorBrush(Color.FromRgb(123, 142, 129)),
                        FontFamily = new FontFamily("Consolas"),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetColumn(lbl, idx);
                    MonthLabelsGrid.Children.Add(lbl);
                }
            }

            // 4. Trigger left-to-right staggered sweep entrance animation
            AnimateColumnsEntrance(true);
        }

        private DateTime _lastAnimationTime = DateTime.MinValue;

        public void AnimateColumnsEntrance(bool force = false)
        {
            if (!force && (DateTime.Now - _lastAnimationTime).TotalSeconds < 1.5) return;
            _lastAnimationTime = DateTime.Now;

            for (int col = 0; col < HeatmapGrid.Children.Count; col++)
            {
                if (HeatmapGrid.Children[col] is FrameworkElement colElem &&
                    colElem.RenderTransform is ScaleTransform scale)
                {
                    var delay = TimeSpan.FromMilliseconds(col * 14.0);

                    var fadeIn = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(220),
                        BeginTime = delay,
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    var scaleInX = new DoubleAnimation
                    {
                        From = 0.35,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(280),
                        BeginTime = delay,
                        EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                    };

                    var scaleInY = new DoubleAnimation
                    {
                        From = 0.35,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(280),
                        BeginTime = delay,
                        EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                    };

                    colElem.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleInX);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleInY);
                }
            }

            // Month labels reveal
            MonthLabelsGrid.Opacity = 0.0;
            var monthFadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            MonthLabelsGrid.BeginAnimation(UIElement.OpacityProperty, monthFadeIn);
        }

        private void UpdateBotExpression()
        {
            if (_currentData == null) return;

            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            int todayCount = 0;
            bool foundToday = false;

            for (int w = _currentData.Weeks.Count - 1; w >= 0; w--)
            {
                var week = _currentData.Weeks[w];
                for (int d = week.Count - 1; d >= 0; d--)
                {
                    var day = week[d];
                    if (day.Date == todayStr)
                    {
                        todayCount = day.Count;
                        foundToday = true;
                        break;
                    }
                }
                if (foundToday) break;
            }

            bool isMono = _config.Theme == "mono";
            bool isLight = _config.Theme == "light";

            if (todayCount > 0)
            {
                // Happy smile arc
                BotMouthPath.Data = Geometry.Parse("M 8.5,13.5 Q 11,16.5 13.5,13.5");
                BotMouthPath.Stroke = Brushes.White;
                BotLeftEye.Fill = Brushes.White;
                BotRightEye.Fill = Brushes.White;

                if (isMono)
                {
                    BotGradStop1.Color = Color.FromRgb(180, 180, 180);
                    BotGradStop2.Color = Color.FromRgb(95, 95, 95);
                    BotGradStop3.Color = Color.FromRgb(40, 40, 45);
                }
                else
                {
                    BotGradStop1.Color = Color.FromRgb(0x58, 0xD7, 0x7B);
                    BotGradStop2.Color = Color.FromRgb(0x2E, 0xA0, 0x43);
                    BotGradStop3.Color = Color.FromRgb(0x12, 0x4A, 0x24);
                }

                BotContainer.ToolTip = $"{todayCount} contributions today! Bot is happy :)";
            }
            else
            {
                // Sad frown arc
                BotMouthPath.Data = Geometry.Parse("M 8.5,16 Q 11,13.5 13.5,16");
                var dimBrush = isLight ? new SolidColorBrush(Color.FromRgb(180, 180, 180)) : new SolidColorBrush(Color.FromRgb(140, 150, 145));
                BotMouthPath.Stroke = dimBrush;
                BotLeftEye.Fill = dimBrush;
                BotRightEye.Fill = dimBrush;

                if (isLight)
                {
                    BotGradStop1.Color = Color.FromRgb(190, 195, 200);
                    BotGradStop2.Color = Color.FromRgb(140, 145, 150);
                    BotGradStop3.Color = Color.FromRgb(100, 105, 110);
                }
                else
                {
                    BotGradStop1.Color = Color.FromRgb(75, 95, 80);
                    BotGradStop2.Color = Color.FromRgb(42, 55, 46);
                    BotGradStop3.Color = Color.FromRgb(20, 28, 23);
                }

                BotContainer.ToolTip = "0 contributions today. Bot is sad :(";
            }
        }

        public void ApplyTheme(string theme)
        {
            _config.Theme = theme;
            SaveConfig();

            MenuThemeMono.IsChecked = theme == "mono";
            MenuThemeLight.IsChecked = theme == "light";
            MenuThemeLiquid.IsChecked = theme == "liquid";
            MenuThemeClassic.IsChecked = theme == "classic";
            MenuThemeOLED.IsChecked = theme == "oled";
            MenuThemeAurora.IsChecked = theme == "aurora";

            switch (theme)
            {
                case "mono":
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(240, 18, 18, 20));
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
                    StatNumberText.Foreground = Brushes.White;
                    StatLabelText.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 165));
                    GitHubLogo.Fill = Brushes.White;
                    break;
                case "light":
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(242, 245, 245, 247));
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(170, 210, 210, 215));
                    StatNumberText.Foreground = new SolidColorBrush(Color.FromRgb(28, 28, 30));
                    StatLabelText.Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 115));
                    GitHubLogo.Fill = new SolidColorBrush(Color.FromRgb(36, 41, 47));
                    break;
                case "classic":
                    MainBorder.Background = new SolidColorBrush(Color.FromRgb(13, 17, 23));
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61));
                    StatNumberText.Foreground = Brushes.White;
                    StatLabelText.Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xB1, 0xA2));
                    GitHubLogo.Fill = Brushes.White;
                    break;
                case "oled":
                    MainBorder.Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                    StatNumberText.Foreground = Brushes.White;
                    StatLabelText.Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xB1, 0xA2));
                    GitHubLogo.Fill = Brushes.White;
                    break;
                case "aurora":
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(200, 5, 20, 23));
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 0, 191, 178));
                    StatNumberText.Foreground = Brushes.White;
                    StatLabelText.Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xB1, 0xA2));
                    GitHubLogo.Fill = Brushes.White;
                    break;
                default: // liquid glass: transparent background with subtle green border
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(20, 10, 18, 13));
                    MainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(55, 88, 215, 123));
                    StatNumberText.Foreground = Brushes.White;
                    StatLabelText.Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xB1, 0xA2));
                    GitHubLogo.Fill = Brushes.White;
                    break;
            }

            if (_currentData != null)
            {
                RenderHeatmap(_currentData);
                UpdateBotExpression();
            }

            // Left-to-right sweep entrance animation across the heatmap
            AnimateColumnsEntrance(true);
        }

        private FrameworkElement CreateDayShape(string shape, SolidColorBrush fill)
        {
            const double size = 13.5;
            FrameworkElement elem;

            switch ((shape ?? "circle").ToLowerInvariant())
            {
                case "square":
                    elem = new Rectangle
                    {
                        Width = size,
                        Height = size,
                        Fill = fill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    break;
                case "rounded":
                    elem = new Rectangle
                    {
                        Width = size,
                        Height = size,
                        RadiusX = 3.2,
                        RadiusY = 3.2,
                        Fill = fill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    break;
                case "triangle":
                    elem = new Polygon
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.Fill,
                        Points = new PointCollection { new Point(5, 0.5), new Point(9.5, 9.5), new Point(0.5, 9.5) },
                        Fill = fill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    break;
                case "hexagon":
                    elem = new Polygon
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.Fill,
                        Points = new PointCollection
                        {
                            new Point(5, 0),
                            new Point(9.5, 2.6),
                            new Point(9.5, 7.4),
                            new Point(5, 10),
                            new Point(0.5, 7.4),
                            new Point(0.5, 2.6)
                        },
                        Fill = fill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    break;
                case "flower":
                    elem = new System.Windows.Shapes.Path
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.Uniform,
                        Data = Geometry.Parse("M 5,0 C 6.5,0 7,2.5 5,4 C 7.5,2 10,3.5 10,5 C 10,6.5 7.5,8 5,6 C 7,7.5 6.5,10 5,10 C 3.5,10 3,7.5 5,6 C 2.5,8 0,6.5 0,5 C 0,3.5 2.5,2 5,4 C 3,2.5 3.5,0 5,0 Z"),
                        Fill = fill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    break;
                default: // circle
                    elem = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = fill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Cursor = Cursors.Hand
                    };
                    break;
            }

            elem.RenderTransform = new ScaleTransform(1.0, 1.0);
            elem.RenderTransformOrigin = new Point(0.5, 0.5);

            return elem;
        }

        private void MenuShape_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string s)
            {
                _config.Shape = s;
                ApplyShapeUI();
                SaveConfig();
                if (_currentData != null) RenderHeatmap(_currentData);
            }
        }

        private void ApplyShapeUI()
        {
            var s = (_config.Shape ?? "circle").ToLowerInvariant();
            MenuShapeCircle.IsChecked = s == "circle";
            MenuShapeHexagon.IsChecked = s == "hexagon";
            MenuShapeTriangle.IsChecked = s == "triangle";
            MenuShapeRounded.IsChecked = s == "rounded";
            MenuShapeSquare.IsChecked = s == "square";
            MenuShapeFlower.IsChecked = s == "flower";
        }

        private void ApplyRangeUI()
        {
            bool is30d = _config.Range == "30d";
            Btn30D.Background = is30d ? new SolidColorBrush(Color.FromArgb(60, 88, 215, 123)) : Brushes.Transparent;
            Btn30D.Foreground = is30d ? (SolidColorBrush)new BrushConverter().ConvertFromString("#58D77B")! : (SolidColorBrush)new BrushConverter().ConvertFromString("#888888")!;

            Btn6M.Background = !is30d ? new SolidColorBrush(Color.FromArgb(60, 88, 215, 123)) : Brushes.Transparent;
            Btn6M.Foreground = !is30d ? (SolidColorBrush)new BrushConverter().ConvertFromString("#58D77B")! : (SolidColorBrush)new BrushConverter().ConvertFromString("#888888")!;

            MenuRange30D.IsChecked = is30d;
            MenuRange6M.IsChecked = !is30d;
            UpdateHeaderResponsiveness();
        }

        public async void SetRange(string range)
        {
            _config.Range = range;
            ApplyRangeUI();

            // When user switches to 30d, make the widget compact!
            if (range == "30d")
            {
                Width = 195;
            }
            else
            {
                Width = 495;
            }

            SaveConfig();
            // In-memory zero-latency refresh
            await RefreshDataAsync(false);
        }

        private void Btn30D_Click(object sender, RoutedEventArgs e) => SetRange("30d");
        private void Btn6M_Click(object sender, RoutedEventArgs e) => SetRange("6m");

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (QuickSettingsPill.Visibility == Visibility.Visible)
            {
                HideQuickSettingsPill();
            }
            if (!_config.Locked && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private DispatcherTimer? _pillAutoHideTimer;

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(this);
            double left = Math.Min(Math.Max(pt.X - 45, 10), Math.Max(10, ActualWidth - 110));
            double top = Math.Min(Math.Max(pt.Y - 20, 10), Math.Max(10, ActualHeight - 40));

            QuickSettingsTranslate.X = left;
            QuickSettingsTranslate.Y = top;
            QuickSettingsPill.Visibility = Visibility.Visible;

            var popScale = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
            };
            var popFade = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            QuickSettingsScale.BeginAnimation(ScaleTransform.ScaleXProperty, popScale);
            QuickSettingsScale.BeginAnimation(ScaleTransform.ScaleYProperty, popScale);
            QuickSettingsPill.BeginAnimation(UIElement.OpacityProperty, popFade);

            _pillAutoHideTimer?.Stop();
            _pillAutoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _pillAutoHideTimer.Tick += (s, ev) =>
            {
                HideQuickSettingsPill();
                _pillAutoHideTimer.Stop();
            };
            _pillAutoHideTimer.Start();
        }

        private void HideQuickSettingsPill()
        {
            var fadeOut = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fadeOut.Completed += (s, e) => QuickSettingsPill.Visibility = Visibility.Collapsed;
            QuickSettingsPill.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void QuickSettingsPill_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            HideQuickSettingsPill();
            OpenSettingsWindow();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
        }

        private SettingsWindow? _settingsWindow;

        public void OpenSettingsWindow()
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(_config, this);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        public void ApplyShape(string shape)
        {
            _config.Shape = shape;
            ApplyShapeUI();
            SaveConfig();
            if (_currentData != null) RenderHeatmap(_currentData);
        }

        public async System.Threading.Tasks.Task ApplyUsername(string user)
        {
            _config.Username = user;
            SaveConfig();
            ShowSetup(false);
            await RefreshDataAsync(true);
        }

        public void ToggleTopmost()
        {
            _config.Topmost = !_config.Topmost;
            Topmost = _config.Topmost;
            MenuTopmost.IsChecked = !_config.Topmost;
            AttachToDesktop();
            SaveConfig();
        }

        public void ToggleLock()
        {
            _config.Locked = !_config.Locked;
            SaveConfig();
            MenuLock.Header = _config.Locked ? "Enable Moving" : "Disable Moving (Lock)";
        }

        public void OpenProfile()
        {
            if (!string.IsNullOrEmpty(_config.Username))
            {
                Process.Start(new ProcessStartInfo($"https://github.com/{_config.Username}") { UseShellExecute = true });
            }
        }

        private void MenuLock_Click(object sender, RoutedEventArgs e)
        {
            _config.Locked = !_config.Locked;
            SaveConfig();
            MenuLock.Header = _config.Locked ? "Enable Moving" : "Disable Moving (Lock)";
        }

        private void MenuTopmost_Click(object sender, RoutedEventArgs e)
        {
            _config.Topmost = !_config.Topmost;
            Topmost = _config.Topmost;
            MenuTopmost.IsChecked = _config.Topmost;
            AttachToDesktop();
            SaveConfig();
        }

        private void MenuStartup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    bool enable = !MenuStartup.IsChecked;
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (enable && !string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("GitHubDesktopWidget", $"\"{exePath}\"");
                        MenuStartup.IsChecked = true;
                    }
                    else
                    {
                        key.DeleteValue("GitHubDesktopWidget", false);
                        MenuStartup.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to toggle startup: {ex.Message}");
            }
        }

        private void MenuTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string t)
            {
                ApplyTheme(t);
            }
        }

        private void MenuRange30D_Click(object sender, RoutedEventArgs e) => SetRange("30d");
        private void MenuRange6M_Click(object sender, RoutedEventArgs e) => SetRange("6m");
        private async void MenuRefresh_Click(object sender, RoutedEventArgs e) => await RefreshDataAsync(true);

        private void MenuProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_config.Username))
            {
                Process.Start(new ProcessStartInfo($"https://github.com/{_config.Username}") { UseShellExecute = true });
            }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        private void MenuHide_Click(object sender, RoutedEventArgs e) => Hide();

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var user = UsernameInput.Text.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(user)) return;

            try
            {
                SetupError.Text = "connecting...";
                _config.Username = user;
                SaveConfig();
                ShowSetup(false);
                await RefreshDataAsync(true);
            }
            catch (Exception ex)
            {
                SetupError.Text = ex.Message;
                ShowSetup(true);
            }
        }

        private void UsernameInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ConnectButton_Click(sender, e);
        }
    }
}
