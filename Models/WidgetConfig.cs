namespace GitHubDesktopWidget.Models
{
    public class WidgetConfig
    {
        public string Username { get; set; } = "";
        public string Range { get; set; } = "6m";
        public string Theme { get; set; } = "liquid";
        public string Shape { get; set; } = "circle";
        public bool Locked { get; set; } = false;
        public bool Topmost { get; set; } = false;
        public double X { get; set; } = -1;
        public double Y { get; set; } = -1;
        public double Width { get; set; } = 580;
        public double Height { get; set; } = 165;
    }
}
