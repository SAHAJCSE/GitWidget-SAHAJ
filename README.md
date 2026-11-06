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
