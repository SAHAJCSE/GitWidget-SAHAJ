using System.Collections.Generic;

namespace GitHubDesktopWidget.Models
{
    public class WidgetData
    {
        public List<List<ContributionDay>> Weeks { get; set; } = new();
        public int TotalCount { get; set; }
        public List<(int Index, string Month)> MonthLabels { get; set; } = new();
    }
}

