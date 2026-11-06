namespace GitHubDesktopWidget.Models
{
    public class RepoContribution
    {
        public string Name { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Owner { get; set; } = "";
        public int Count { get; set; }
        public string Href { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
    }
}

