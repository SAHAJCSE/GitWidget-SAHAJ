using System.Text.Json.Serialization;

namespace GitHubDesktopWidget.Models
{
    public class ContributionDay
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        public bool IsFuture { get; set; }
        public bool IsWithin30Days { get; set; }
    }
}

