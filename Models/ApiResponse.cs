using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GitHubDesktopWidget.Models
{
    public class ApiResponse
    {
        [JsonPropertyName("total")]
        public Dictionary<string, object>? Total { get; set; }

        [JsonPropertyName("contributions")]
        public List<ContributionDay>? Contributions { get; set; }
    }
}

