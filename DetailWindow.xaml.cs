using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Dictionary<string, string[]> _themeColors = new Dictionary<string, string[]>
        {
            ["mono"] = new[] { "#202022", "#48484A", "#7C7C80", "#AEAEB2", "#FFFFFF" },
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
                HeatmapContainer.Visibility = Visibility.Collapsed;

                var calendarTask = _service.GetContributionsAsync(_username, "1y", forceRefresh: false);
                var reposTask = _service.FetchTopReposAsync(_username);

                await Task.WhenAll(calendarTask, reposTask);

                var calendarData = await calendarTask;
                var repos = await reposTask;

                LoadingPanel.Visibility = Visibility.Collapsed;
                HeatmapContainer.Visibility = Visibility.Visible;

                int currentYear = DateTime.Today.Year;
                DetailTitleText.Text = $"{calendarData.TotalCount:N0} contributions in {currentYear}";

                RenderFullHeatmap(calendarData);
                RenderRepos(repos);
            }
            catch (Exception ex)
            {
                DetailTitleText.Text = "Failed to load activity details";
                LoadingPanel.Visibility = Visibility.Collapsed;
                Debug.WriteLine(ex.Message);
            }
        }

        private void RenderFullHeatmap(WidgetData data)
        {
            FullHeatmapGrid.Children.Clear();
            FullHeatmapGrid.ColumnDefinitions.Clear();
            FullHeatmapGrid.RowDefinitions.Clear();

            MonthLabelsGrid.Children.Clear();
            MonthLabelsGrid.ColumnDefinitions.Clear();

            int colCount = data.Weeks.Count;
            if (colCount == 0) return;

            for (int i = 0; i < colCount; i++)
            {
                FullHeatmapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                MonthLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (int r = 0; r < 7; r++)
            {
                FullHeatmapGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            var colors = _themeColors.ContainsKey(_theme) ? _themeColors[_theme] : _themeColors["liquid"];

            for (int col = 0; col < colCount; col++)
            {
                var week = data.Weeks[col];
                for (int row = 0; row < week.Count; row++)
                {
                    var day = week[row];

                    SolidColorBrush fillBrush = day.Level < 0
                        ? Brushes.Transparent
                        : (SolidColorBrush)new BrushConverter().ConvertFromString(colors[Math.Clamp(day.Level, 0, 4)])!;

                    var shape = CreateDayShape(_shape, fillBrush);

                    if (DateTime.TryParse(day.Date, out var dt))
                    {
                        shape.ToolTip = day.IsFuture
                            ? $"Future: {dt:MMM dd, yyyy}"
                            : $"{day.Count} {(day.Count == 1 ? "contribution" : "contributions")} on {dt:MMM dd, yyyy}";
                    }

                    // Hover animation
                    shape.MouseEnter += (s, e) =>
                    {
                        Panel.SetZIndex(shape, 50);
                        if (shape.RenderTransform is ScaleTransform st)
                        {
                            var bounce = new DoubleAnimation(1.35, TimeSpan.FromMilliseconds(150))
                            {
                                EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
                            };
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
                        }
                    };

                    shape.MouseLeave += (s, e) =>
                    {
                        if (shape.RenderTransform is ScaleTransform st)
                        {
                            var reset = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120));
                            reset.Completed += (s2, e2) => Panel.SetZIndex(shape, 0);
                            st.BeginAnimation(ScaleTransform.ScaleXProperty, reset);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, reset);
                        }
                    };

                    Grid.SetColumn(shape, col);
                    Grid.SetRow(shape, row);
                    FullHeatmapGrid.Children.Add(shape);
                }
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
            const double size = 9.5;
            FrameworkElement elem;

            switch ((shape ?? "circle").ToLowerInvariant())
            {
                case "square":
                    elem = new Rectangle { Width = size, Height = size, Fill = fill };
                    break;
                case "rounded":
                    elem = new Rectangle { Width = size, Height = size, RadiusX = 2.0, RadiusY = 2.0, Fill = fill };
                    break;
                case "hexagon":
                    elem = new Polygon
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.Fill,
                        Points = new PointCollection { new Point(5, 0), new Point(9.5, 2.6), new Point(9.5, 7.4), new Point(5, 10), new Point(0.5, 7.4), new Point(0.5, 2.6) },
                        Fill = fill
                    };
                    break;
                default:
                    elem = new Ellipse { Width = size, Height = size, Fill = fill };
                    break;
            }

            elem.HorizontalAlignment = HorizontalAlignment.Center;
            elem.VerticalAlignment = VerticalAlignment.Center;
            elem.Cursor = Cursors.Hand;
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
                TopReposSummary.Text = "(No public activity found)";
                return;
            }

            TopReposSummary.Text = $"{repos.Count} repositories";

            // Render Avatar Stack in header (overlapping circles)
            for (int i = 0; i < repos.Count; i++)
            {
                var repo = repos[i];
                var avatarBorder = CreateAvatarElement(repo, 22, -6);
                AvatarStack.Children.Add(avatarBorder);
            }

            // Render Repos List rows
            foreach (var repo in repos)
            {
                var rowBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = Cursors.Hand
                };

                rowBorder.MouseEnter += (s, e) => rowBorder.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                rowBorder.MouseLeave += (s, e) => rowBorder.Background = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
                rowBorder.MouseLeftButtonDown += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(repo.Href))
                    {
                        Process.Start(new ProcessStartInfo(repo.Href) { UseShellExecute = true });
                    }
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Avatar
                var avatar = CreateAvatarElement(repo, 26, 0);
                avatar.Margin = new Thickness(0, 0, 10, 0);
                Grid.SetColumn(avatar, 0);
                grid.Children.Add(avatar);

                // Repo Name & Owner
                var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                nameStack.Children.Add(new TextBlock
                {
                    Text = repo.FullName,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(nameStack, 1);
                grid.Children.Add(nameStack);

                // Commit Count Badge
                var countBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x30, 0x39, 0xD3, 0x53)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 3, 8, 3),
                    VerticalAlignment = VerticalAlignment.Center
                };
                countBadge.Child = new TextBlock
                {
                    Text = $"{repo.Count} commits",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#39D353")!,
                    FontFamily = new FontFamily("Consolas")
                };
                Grid.SetColumn(countBadge, 2);
                grid.Children.Add(countBadge);

                rowBorder.Child = grid;
                ReposListPanel.Children.Add(rowBorder);
            }
        }

        private Border CreateAvatarElement(RepoContribution repo, double size, double leftMargin)
        {
            var avatarBorder = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2),
                Background = new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x2D)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x14)),
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(leftMargin, 0, 0, 0),
                ClipToBounds = true
            };

            try
            {
                if (!string.IsNullOrEmpty(repo.AvatarUrl))
                {
                    var img = new Image
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.UniformToFill
                    };
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(repo.AvatarUrl);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    img.Source = bi;
                    avatarBorder.Child = img;
                    return avatarBorder;
                }
            }
            catch { }

            // Fallback initial
            avatarBorder.Child = new TextBlock
            {
                Text = repo.Name.Length > 0 ? repo.Name.Substring(0, 1).ToUpper() : "R",
                FontSize = size * 0.45,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return avatarBorder;
        }

        private void ToggleRepos_Click(object sender, MouseButtonEventArgs e)
        {
            _isReposExpanded = !_isReposExpanded;
            ReposListPanel.Visibility = _isReposExpanded ? Visibility.Visible : Visibility.Collapsed;
            ChevronText.Text = _isReposExpanded ? "⌄" : "⌃";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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
