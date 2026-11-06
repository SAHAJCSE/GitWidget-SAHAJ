using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GitHubDesktopWidget.Models;
using GitHubDesktopWidget.Services;

namespace GitHubDesktopWidget
{
    public partial class DetailWindow : Window
    {
        private readonly string _username;
        private readonly string _theme;
        private readonly string _shape;
        private readonly GitHubService _service = new GitHubService();

        private bool _isReposExpanded = true;
        private static readonly string[] MoodsList = new[] { "happy", "angry", "sleepy", "bored", "calm", "excited" };
        private int _currentMoodIdx = 0;

        private readonly Dictionary<string, string[]> _themeColors = new Dictionary<string, string[]>
        {
            ["mono"] = new[] { "#202022", "#48484A", "#7C7C80", "#AEAEB2", "#FFFFFF" },
            ["light"] = new[] { "#EBEDF0", "#9BE9A8", "#40C463", "#30A14E", "#216E39" },
            ["liquid"] = new[] { "#1C2B20", "#0E4429", "#006D32", "#26A641", "#39D353" },
            ["classic"] = new[] { "#161B22", "#0E4429", "#006D32", "#26A641", "#39D353" },
            ["oled"] = new[] { "#121212", "#003816", "#006B2B", "#00B849", "#00FF66" },
            ["aurora"] = new[] { "#0E2429", "#0A3D42", "#087F8C", "#00BFB2", "#00FFC8" }
        };

        public DetailWindow(string username, string theme, string shape)
        {
            InitializeComponent();
            _username = username;
            _theme = theme;
            _shape = shape;

            UsernameBadge.Text = $"@{_username}";

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (string.IsNullOrWhiteSpace(_username))
            {
                DetailTitleText.Text = "No GitHub username configured";
                LoadingPanel.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                DashboardScroller.Visibility = Visibility.Collapsed;

                var calendarTask = _service.GetContributionsAsync(_username, "1y", forceRefresh: false);
                var reposTask = _service.FetchTopReposAsync(_username);

                await Task.WhenAll(calendarTask, reposTask);

                var calendarData = await calendarTask;
                var repos = await reposTask;

                LoadingPanel.Visibility = Visibility.Collapsed;
                DashboardScroller.Visibility = Visibility.Visible;

                int currentYear = DateTime.Today.Year;
                DetailTitleText.Text = $"{calendarData.TotalCount:N0} contributions in {currentYear}";

                // 1. Process Advanced Analytics & Stats
                ProcessAndRenderAnalytics(calendarData);

                // 2. Render Full 52-Week Heatmap
                RenderFullHeatmap(calendarData);

                // 3. Render Top Repositories Drawer
                RenderRepos(repos);
            }
            catch (Exception ex)
            {
                DetailTitleText.Text = "Failed to load activity details";
                LoadingPanel.Visibility = Visibility.Visible;
                if (LoadingPanel.Children[0] is StackPanel sp && sp.Children[0] is TextBlock tb)
                {
                    tb.Text = $"Error: {ex.Message}";
                    tb.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF6B6B")!;
                }
            }
        }

        private void ProcessAndRenderAnalytics(WidgetData data)
        {
            // 1. Calculate Streaks & Day stats
            var allDays = new List<ContributionDay>();
            foreach (var week in data.Weeks)
            {
                allDays.AddRange(week);
            }

            int total = data.TotalCount;
            int currentStreak = 0;
            int longestStreak = 0;
            int tempStreak = 0;

            int[] weekdayCommits = new int[7]; // 0=Sun ... 6=Sat
            int[] monthCommits = new int[12];  // 0=Jan ... 11=Dec

            var today = DateTime.Today;
            bool checkingCurrentStreak = true;

            // Iterate backwards for current streak, forwards for longest streak
            for (int i = allDays.Count - 1; i >= 0; i--)
            {
                var day = allDays[i];
                if (DateTime.TryParse(day.Date, out var dt))
                {
                    if (dt > today) continue; // Skip future

                    // Weekday & Month indexing
                    int wday = (int)dt.DayOfWeek;
                    weekdayCommits[wday] += day.Count;

                    int month = dt.Month - 1;
                    monthCommits[month] += day.Count;

                    // Current streak check
                    if (checkingCurrentStreak)
                    {
                        if (day.Count > 0)
                        {
                            currentStreak++;
                        }
                        else if (dt < today)
                        {
                            checkingCurrentStreak = false;
                        }
                    }
                }
            }

            // Longest streak
            for (int i = 0; i < allDays.Count; i++)
            {
                var day = allDays[i];
                if (DateTime.TryParse(day.Date, out var dt) && dt <= today)
                {
                    if (day.Count > 0)
                    {
                        tempStreak++;
                        if (tempStreak > longestStreak) longestStreak = tempStreak;
                    }
                    else
                    {
                        tempStreak = 0;
                    }
                }
            }

            // Peak weekday
            string[] dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            int maxDayVal = 0;
            int maxDayIdx = 3; // Wednesday default
            for (int i = 0; i < 7; i++)
            {
                if (weekdayCommits[i] > maxDayVal)
                {
                    maxDayVal = weekdayCommits[i];
                    maxDayIdx = i;
                }
            }

            double peakDayPct = total > 0 ? (double)maxDayVal / total * 100.0 : 0;

            // Update KPI Cards
            KpiTotalCommits.Text = total.ToString("N0");
            KpiCurrentStreak.Text = $"{currentStreak} {(currentStreak == 1 ? "Day" : "Days")}";
            KpiLongestStreak.Text = $"{longestStreak} {(longestStreak == 1 ? "Day" : "Days")}";
            KpiPeakDay.Text = dayNames[maxDayIdx];
            KpiPeakDaySub.Text = $"{maxDayVal} commits ({peakDayPct:F0}% of volume)";

            // Render Charts
            RenderWeekdayChart(weekdayCommits);
            RenderMonthlyChart(monthCommits);

            // Determine Initial Character Mood
            int todayCommits = 0;
            var todayStr = today.ToString("yyyy-MM-dd");
            var todayDay = allDays.FirstOrDefault(d => d.Date == todayStr);
            if (todayDay != null) todayCommits = todayDay.Count;

            string mood;
            if (todayCommits >= 5) mood = "excited";
            else if (todayCommits >= 1) mood = "happy";
            else if (currentStreak >= 3) mood = "calm";
            else if (DateTime.Now.Hour >= 23 || DateTime.Now.Hour < 6) mood = "sleepy";
            else if (todayCommits == 0) mood = "bored";
            else mood = "happy";

            ApplyHeroMood(mood, isUserClick: false);
        }

        private void RenderWeekdayChart(int[] weekdayCommits)
        {
            WeekdayBarsGrid.Children.Clear();
            WeekdayBarsGrid.ColumnDefinitions.Clear();

            string[] shortDays = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            int maxVal = Math.Max(1, weekdayCommits.Max());

            for (int i = 0; i < 7; i++)
            {
                WeekdayBarsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                int count = weekdayCommits[i];
                double ratio = (double)count / maxVal;
                double barHeight = Math.Max(4, ratio * 64.0);

                var colStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Commit Count Badge
                var countTb = new TextBlock
                {
                    Text = count.ToString(),
                    FontSize = 8.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 145)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                colStack.Children.Add(countTb);

                // Animated Vertical Bar
                var bar = new Border
                {
                    Width = 14,
                    Height = 0,
                    CornerRadius = new CornerRadius(4, 4, 1, 1),
                    Background = (i == 0 || i == 6)
                        ? new SolidColorBrush(Color.FromArgb(200, 70, 120, 90))
                        : new LinearGradientBrush(Color.FromRgb(0x39, 0xD3, 0x53), Color.FromRgb(0x23, 0x86, 0x36), 90.0),
                    ToolTip = $"{count} commits on {shortDays[i]}s"
                };

                var anim = new DoubleAnimation(0, barHeight, TimeSpan.FromMilliseconds(450 + i * 40))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                bar.BeginAnimation(FrameworkElement.HeightProperty, anim);

                bar.MouseEnter += (s, e) =>
                {
                    SoundService.PlayPop();
                    bar.Opacity = 0.8;
                };
                bar.MouseLeave += (s, e) => bar.Opacity = 1.0;

                colStack.Children.Add(bar);

                // Day Label
                var dayTb = new TextBlock
                {
                    Text = shortDays[i],
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 165)),
                    FontFamily = new FontFamily("Consolas"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                colStack.Children.Add(dayTb);

                Grid.SetColumn(colStack, i);
                WeekdayBarsGrid.Children.Add(colStack);
            }
        }

        private void RenderMonthlyChart(int[] monthCommits)
        {
            MonthlyBarsGrid.Children.Clear();
            MonthlyBarsGrid.ColumnDefinitions.Clear();

            string[] monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            int maxVal = Math.Max(1, monthCommits.Max());

            for (int i = 0; i < 12; i++)
            {
                MonthlyBarsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                int count = monthCommits[i];
                double ratio = (double)count / maxVal;
                double barHeight = Math.Max(4, ratio * 64.0);

                var colStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Commit Count Badge
                var countTb = new TextBlock
                {
                    Text = count > 0 ? count.ToString() : "",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 145)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                colStack.Children.Add(countTb);

                // Animated Vertical Bar
                var bar = new Border
                {
                    Width = 13,
                    Height = 0,
                    CornerRadius = new CornerRadius(3, 3, 1, 1),
                    Background = new LinearGradientBrush(Color.FromRgb(0x58, 0xD7, 0x7B), Color.FromRgb(0x1B, 0x68, 0x30), 90.0),
                    ToolTip = $"{count} commits in {monthNames[i]}"
                };

                var anim = new DoubleAnimation(0, barHeight, TimeSpan.FromMilliseconds(400 + i * 35))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                bar.BeginAnimation(FrameworkElement.HeightProperty, anim);

                bar.MouseEnter += (s, e) =>
                {
                    SoundService.PlayPop();
                    bar.Opacity = 0.8;
                };
                bar.MouseLeave += (s, e) => bar.Opacity = 1.0;

                colStack.Children.Add(bar);

                // Month Label
                var mTb = new TextBlock
                {
                    Text = monthNames[i],
                    FontSize = 8.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 165)),
                    FontFamily = new FontFamily("Consolas"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                colStack.Children.Add(mTb);

                Grid.SetColumn(colStack, i);
                MonthlyBarsGrid.Children.Add(colStack);
            }
        }

        private void HeroBot_Click(object sender, MouseButtonEventArgs e)
        {
            _currentMoodIdx = (_currentMoodIdx + 1) % MoodsList.Length;
            ApplyHeroMood(MoodsList[_currentMoodIdx], isUserClick: true);
        }

        private void MoodPill_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border bd && bd.Tag is string m)
            {
                ApplyHeroMood(m, isUserClick: true);
            }
        }

        public void ApplyHeroMood(string mood, bool isUserClick = false)
        {
            mood = mood.ToLowerInvariant();
            HeroLightning.Visibility = Visibility.Collapsed;
            HeroSleepyMouth.Visibility = Visibility.Collapsed;
            HeroMouth.Visibility = Visibility.Visible;

            SolidColorBrush strokeBrush;
            string moodTitle;
            string moodBadge;
            string commentary;

            switch (mood)
            {
                case "excited":
                    HeroLightning.Visibility = Visibility.Visible;
                    HeroGrad1.Color = Color.FromRgb(0xFF, 0xD2, 0x3F);
                    HeroGrad2.Color = Color.FromRgb(0xFF, 0xAA, 0x00);
                    HeroGrad3.Color = Color.FromRgb(0xC6, 0x78, 0x00);
                    strokeBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x32, 0x00));

                    HeroLeftEye.Data = Geometry.Parse("M 0,6 L 6,0 L 12,6");
                    HeroRightEye.Data = Geometry.Parse("M 0,6 L 6,0 L 12,6");
                    HeroMouth.Data = Geometry.Parse("M 0,0 Q 8,11 16,0 Z");

                    moodTitle = "Mood: Excited! ⚡";
                    moodBadge = "Electric";
                    commentary = "Incredible momentum detected! Your code contributions are surging with peak productivity.";
                    if (isUserClick) SoundService.PlayChime();
                    break;

                case "angry":
                    HeroGrad1.Color = Color.FromRgb(0xFF, 0x7E, 0x52);
                    HeroGrad2.Color = Color.FromRgb(0xE8, 0x54, 0x2A);
                    HeroGrad3.Color = Color.FromRgb(0xA8, 0x28, 0x0E);
                    strokeBrush = new SolidColorBrush(Color.FromRgb(0x45, 0x12, 0x05));

                    HeroLeftEye.Data = Geometry.Parse("M 0,2 L 12,9 M 0,11 L 11,5");
                    HeroRightEye.Data = Geometry.Parse("M 12,2 L 0,9 M 12,11 L 1,5");
                    HeroMouth.Data = Geometry.Parse("M 0,7 Q 8,-3 16,7");

                    moodTitle = "Mood: Angry! 😤";
                    moodBadge = "Frown";
                    commentary = "Zero commits registered! Time to open your code editor and push some commits!";
                    if (isUserClick) SoundService.PlayAngry();
                    break;

                case "sleepy":
                    HeroGrad1.Color = Color.FromRgb(0x89, 0xC4, 0xFF);
                    HeroGrad2.Color = Color.FromRgb(0x56, 0x9D, 0xF0);
                    HeroGrad3.Color = Color.FromRgb(0x28, 0x64, 0xB8);
                    strokeBrush = new SolidColorBrush(Color.FromRgb(0x10, 0x28, 0x48));

                    HeroLeftEye.Data = Geometry.Parse("M 0,0 Q 6,7 12,0");
                    HeroRightEye.Data = Geometry.Parse("M 0,0 Q 6,7 12,0");
                    HeroMouth.Visibility = Visibility.Collapsed;
                    HeroSleepyMouth.Visibility = Visibility.Visible;

                    moodTitle = "Mood: Sleepy... 😴";
                    moodBadge = "Drowsy";
                    commentary = "Late hours detected... Yaaawn! Coding late? Don't forget healthy sleep.";
                    if (isUserClick) SoundService.PlaySleepy();
                    break;

                case "bored":
                    HeroGrad1.Color = Color.FromRgb(0xFF, 0xA4, 0xC9);
                    HeroGrad2.Color = Color.FromRgb(0xE8, 0x74, 0xA2);
                    HeroGrad3.Color = Color.FromRgb(0xAC, 0x3E, 0x6C);
                    strokeBrush = new SolidColorBrush(Color.FromRgb(0x48, 0x10, 0x28));

                    HeroLeftEye.Data = Geometry.Parse("M 0,4 L 12,4");
                    HeroRightEye.Data = Geometry.Parse("M 0,4 L 12,4");
                    HeroMouth.Data = Geometry.Parse("M 0,0 L 16,0");

                    moodTitle = "Mood: Bored 🥱";
                    moodBadge = "Idle";
                    commentary = "Things are a bit quiet on GitHub today. Ready to build something fresh?";
                    if (isUserClick) SoundService.PlayPop();
                    break;

                case "calm":
                    HeroGrad1.Color = Color.FromRgb(0xC0, 0x8F, 0xFF);
                    HeroGrad2.Color = Color.FromRgb(0x9E, 0x64, 0xF0);
                    HeroGrad3.Color = Color.FromRgb(0x6A, 0x30, 0xC0);
                    strokeBrush = new SolidColorBrush(Color.FromRgb(0x28, 0x10, 0x48));

                    HeroLeftEye.Data = Geometry.Parse("M 6,3 A 3,3 0 1,1 5.99,3 Z");
                    HeroRightEye.Data = Geometry.Parse("M 6,3 A 3,3 0 1,1 5.99,3 Z");
                    HeroMouth.Data = Geometry.Parse("M 0,0 Q 8,7 16,0");

                    moodTitle = "Mood: Calm 🧘";
                    moodBadge = "Mindful";
                    commentary = "Steady, peaceful progress. High-quality code crafted with continuous balance.";
                    if (isUserClick) SoundService.PlayTick();
                    break;

                default: // happy
                    HeroGrad1.Color = Color.FromRgb(0x99, 0xE8, 0x70);
                    HeroGrad2.Color = Color.FromRgb(0x58, 0xB8, 0x38);
                    HeroGrad3.Color = Color.FromRgb(0x2E, 0x6A, 0x1C);
                    strokeBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0x10));

                    HeroLeftEye.Data = Geometry.Parse("M 0,6 Q 6,0 12,6");
                    HeroRightEye.Data = Geometry.Parse("M 0,6 Q 6,0 12,6");
                    HeroMouth.Data = Geometry.Parse("M 0,0 Q 8,8 16,0");

                    moodTitle = "Mood: Happy 😊";
                    moodBadge = "Active";
                    commentary = "Great consistency detected! Your commit activity is looking healthy and vibrant.";
                    if (isUserClick) SoundService.PlayChime();
                    break;
            }

            HeroLeftEye.Stroke = strokeBrush;
            HeroRightEye.Stroke = strokeBrush;
            HeroMouth.Stroke = strokeBrush;

            HeroMoodTitle.Text = moodTitle;
            HeroMoodBadge.Text = moodBadge;

            // Revealing text animation for commentary
            AnimateRevealingText(HeroCommentary, commentary);

            if (isUserClick)
            {
                var bounce = new DoubleAnimation(1.0, 1.25, TimeSpan.FromMilliseconds(130))
                {
                    AutoReverse = true,
                    EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
                };
                HeroBody.RenderTransform = new ScaleTransform(1.0, 1.0);
                HeroBody.RenderTransformOrigin = new Point(0.5, 0.5);
                HeroBody.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
                HeroBody.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
            }
        }

        private void AnimateRevealingText(TextBlock target, string newText)
        {
            target.Text = newText;
            var fadeIn = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void RenderFullHeatmap(WidgetData data)
        {
            FullHeatmapGrid.Children.Clear();
            FullHeatmapGrid.ColumnDefinitions.Clear();

            MonthLabelsGrid.Children.Clear();
            MonthLabelsGrid.ColumnDefinitions.Clear();

            int colCount = data.Weeks.Count;
            if (colCount == 0) return;

            for (int i = 0; i < colCount; i++)
            {
                FullHeatmapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                MonthLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var colors = _themeColors.ContainsKey(_theme) ? _themeColors[_theme] : _themeColors["liquid"];

            for (int col = 0; col < colCount; col++)
            {
                var colGrid = new Grid();
                for (int r = 0; r < 7; r++)
                {
                    colGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }

                var week = data.Weeks[col];
                for (int row = 0; row < week.Count; row++)
                {
                    var day = week[row];
                    SolidColorBrush fillBrush = day.Level < 0
                        ? Brushes.Transparent
                        : (SolidColorBrush)new BrushConverter().ConvertFromString(colors[Math.Clamp(day.Level, 0, 4)])!;

                    var shape = CreateDayShape(_shape, fillBrush);

                    if (DateTime.TryParse(day.Date, out var dtDay))
                    {
                        shape.ToolTip = day.IsFuture
                            ? $"Future: {dtDay:MMM dd, yyyy}"
                            : $"{day.Count} {(day.Count == 1 ? "contribution" : "contributions")} on {dtDay:MMM dd, yyyy}";
                    }

                    shape.MouseEnter += (s, e) =>
                    {
                        SoundService.PlayPop();
                        Panel.SetZIndex(shape, 100);
                        if (shape.RenderTransform is ScaleTransform st)
                        {
                            var bounce = new DoubleAnimation(1.35, TimeSpan.FromMilliseconds(130));
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
                        }
                    };

                    shape.MouseLeave += (s, e) =>
                    {
                        Panel.SetZIndex(shape, 0);
                        if (shape.RenderTransform is ScaleTransform st)
                        {
                            var reset = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120));
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, reset);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, reset);
                        }
                    };

                    Grid.SetRow(shape, row);
                    colGrid.Children.Add(shape);
                }

                Grid.SetColumn(colGrid, col);
                FullHeatmapGrid.Children.Add(colGrid);
            }

            // Month Labels
            foreach (var (idx, month) in data.MonthLabels)
            {
                if (idx < colCount)
                {
                    var lbl = new TextBlock
                    {
                        Text = month,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Color.FromRgb(123, 142, 129)),
                        FontFamily = new FontFamily("Consolas"),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetColumn(lbl, idx);
                    MonthLabelsGrid.Children.Add(lbl);
                }
            }
        }

        private FrameworkElement CreateDayShape(string shape, SolidColorBrush fill)
        {
            const double size = 10.5;
            FrameworkElement elem;

            switch ((shape ?? "circle").ToLowerInvariant())
            {
                case "square":
                    elem = new Rectangle { Width = size, Height = size, Fill = fill, Cursor = Cursors.Hand };
                    break;
                case "rounded":
                    elem = new Rectangle { Width = size, Height = size, RadiusX = 2.5, RadiusY = 2.5, Fill = fill, Cursor = Cursors.Hand };
                    break;
                case "triangle":
                    elem = new Polygon
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.Fill,
                        Points = new PointCollection { new Point(5, 0.5), new Point(9.5, 9.5), new Point(0.5, 9.5) },
                        Fill = fill,
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
                            new Point(5, 0), new Point(9.5, 2.6), new Point(9.5, 7.4),
                            new Point(5, 10), new Point(0.5, 7.4), new Point(0.5, 2.6)
                        },
                        Fill = fill,
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
                        Cursor = Cursors.Hand
                    };
                    break;
                default:
                    elem = new Ellipse { Width = size, Height = size, Fill = fill, Cursor = Cursors.Hand };
                    break;
            }

            elem.RenderTransform = new ScaleTransform(1.0, 1.0);
            elem.RenderTransformOrigin = new Point(0.5, 0.5);
            return elem;
        }

        private void RenderRepos(List<RepoContribution> repos)
        {
            AvatarStack.Children.Clear();
            ReposListPanel.Children.Clear();

            if (repos.Count == 0)
            {
                TopReposSummary.Text = "No recent public commit activity found";
                return;
            }

            int topCount = Math.Min(3, repos.Count);
            TopReposSummary.Text = string.Join(", ", repos.Take(topCount).Select(r => r.Name));

            // Render Avatar Badges
            for (int i = 0; i < topCount; i++)
            {
                var r = repos[i];
                var circle = new Border
                {
                    Width = 20,
                    Height = 20,
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromRgb(36, 41, 46)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(-6, 0, 0, 0),
                    ToolTip = $"{r.Name}: {r.Count} commits"
                };

                var initial = new TextBlock
                {
                    Text = r.Name.Substring(0, 1).ToUpperInvariant(),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                circle.Child = initial;
                AvatarStack.Children.Add(circle);
            }

            // Render Repo Rows
            foreach (var repo in repos.Take(6))
            {
                var row = new Grid { Margin = new Thickness(0, 4, 0, 4), Cursor = Cursors.Hand };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var leftStack = new StackPanel { Orientation = Orientation.Horizontal };
                var repoIcon = new System.Windows.Shapes.Path
                {
                    Width = 12,
                    Height = 12,
                    Margin = new Thickness(0, 0, 8, 0),
                    Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!,
                    Stretch = Stretch.Uniform,
                    Data = Geometry.Parse("M 2,2 L 9,2 C 10,2 10.5,2.5 10.5,3.5 L 10.5,10.5 C 10.5,11.5 10,12 9,12 L 2,12 C 1,12 0.5,11.5 0.5,10.5 L 0.5,3.5 C 0.5,2.5 1,2 2,2 Z")
                };
                leftStack.Children.Add(repoIcon);

                var nameTb = new TextBlock
                {
                    Text = repo.Name,
                    FontSize = 12,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#58D77B")!,
                    FontWeight = FontWeights.Medium
                };
                leftStack.Children.Add(nameTb);
                Grid.SetColumn(leftStack, 0);
                row.Children.Add(leftStack);

                var countTb = new TextBlock
                {
                    Text = $"{repo.Count} {(repo.Count == 1 ? "commit" : "commits")}",
                    FontSize = 11,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!,
                    FontFamily = new FontFamily("Consolas")
                };
                Grid.SetColumn(countTb, 1);
                row.Children.Add(countTb);

                string repoUrl = repo.Href ?? $"https://github.com/{repo.Name}";
                row.MouseLeftButtonDown += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo(repoUrl) { UseShellExecute = true }); } catch { }
                };

                ReposListPanel.Children.Add(row);
            }
        }

        private void ToggleRepos_Click(object sender, MouseButtonEventArgs e)
        {
            _isReposExpanded = !_isReposExpanded;
            ReposListPanel.Visibility = _isReposExpanded ? Visibility.Visible : Visibility.Collapsed;
            ChevronText.Text = _isReposExpanded ? "⌵" : "⌃";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
    }
}
