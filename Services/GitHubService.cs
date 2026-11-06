using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubDesktopWidget.Models;

namespace GitHubDesktopWidget.Services
{
    public class GitHubService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private List<ContributionDay>? _cachedContributions;
        private string? _cachedUser;

        static GitHubService()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "GitHub-Desktop-Widget-Native");
        }

        public async Task<WidgetData> GetContributionsAsync(string username, string range = "6m", bool forceRefresh = false)
        {
            var clean = username.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(clean))
                throw new ArgumentException("Username cannot be empty");

            // Use in-memory cache for ultra-low latency (<0.1ms) when switching 30D / 6M
            if (!forceRefresh && _cachedContributions != null && string.Equals(_cachedUser, clean, StringComparison.OrdinalIgnoreCase))
            {
                return ProcessContributions(_cachedContributions, range);
            }

            var url = $"https://github-contributions-api.jogruber.de/v4/{Uri.EscapeDataString(clean)}?y=last";
            var response = await HttpClient.GetStringAsync(url);
            var parsed = JsonSerializer.Deserialize<ApiResponse>(response);

            if (parsed?.Contributions == null)
                throw new Exception("Unable to load contributions for this user.");

            _cachedContributions = parsed.Contributions;
            _cachedUser = clean;

            return ProcessContributions(_cachedContributions, range);
        }

        public WidgetData ProcessContributions(List<ContributionDay> rawContributions, string range)
        {
            var map = new Dictionary<string, ContributionDay>(rawContributions.Count);
            foreach (var item in rawContributions)
            {
                map[item.Date] = item;
            }

            var today = DateTime.Today;
            int numWeeks = range == "30d" ? 5 : (range == "1y" || range == "12m") ? 52 : 26;

            int dayOfWeek = (int)today.DayOfWeek; // 0 = Sun ... 6 = Sat
            var endOfWeek = today.AddDays(6 - dayOfWeek);
            var startSunday = endOfWeek.AddDays(-(numWeeks * 7 - 1));
            var thirtyDaysAgo = today.AddDays(-30);

            var weeks = new List<List<ContributionDay>>(numWeeks);
            var curr = startSunday;
            int totalCount = 0;

            for (int w = 0; w < numWeeks; w++)
            {
                var week = new List<ContributionDay>(7);
                for (int d = 0; d < 7; d++)
                {
                    var dateStr = curr.ToString("yyyy-MM-dd");
                    bool isFuture = curr > today;

                    map.TryGetValue(dateStr, out var item);
                    int count = (!isFuture && item != null) ? item.Count : 0;
                    int level = 0;

                    if (isFuture)
                    {
                        level = -1;
                    }
                    else if (item != null)
                    {
                        level = Math.Max(0, Math.Min(4, item.Level));
                    }
                    else if (count > 0)
                    {
                        level = count < 3 ? 1 : count < 6 ? 2 : count < 10 ? 3 : 4;
                    }

                    if (!isFuture && count > 0)
                    {
                        if (range == "30d")
                        {
                            if (curr >= thirtyDaysAgo) totalCount += count;
                        }
                        else
                        {
                            totalCount += count;
                        }
                    }

                    week.Add(new ContributionDay
                    {
                        Date = dateStr,
                        Count = count,
                        Level = level,
                        IsFuture = isFuture,
                        IsWithin30Days = curr >= thirtyDaysAgo
                    });

                    curr = curr.AddDays(1);
                }
                weeks.Add(week);
            }

            var monthLabels = new List<(int Index, string Month)>();
            string prevMonth = "";
            for (int i = 0; i < weeks.Count; i++)
            {
                var day = weeks[i].Find(d => !d.IsFuture) ?? weeks[i][0];
                if (DateTime.TryParse(day.Date, out var dt))
                {
                    string m = dt.ToString("MMM");
                    if (m != prevMonth)
                    {
                        monthLabels.Add((i, m));
                        prevMonth = m;
                    }
                }
            }

            return new WidgetData
            {
                Weeks = weeks,
                TotalCount = totalCount,
                MonthLabels = monthLabels
            };
        }

        public async Task<List<RepoContribution>> FetchTopReposAsync(string username)
        {
            var clean = username.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(clean)) return new List<RepoContribution>();

            var result = new List<RepoContribution>();
            try
            {
                var url = $"https://api.github.com/users/{Uri.EscapeDataString(clean)}/events/public?per_page=100";
                var res = await HttpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(res);

                var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "PushEvent")
                    {
                        if (element.TryGetProperty("repo", out var repoProp) && repoProp.TryGetProperty("name", out var nameProp))
                        {
                            var repoName = nameProp.GetString();
                            if (!string.IsNullOrEmpty(repoName))
                            {
                                int commitsCount = 1;
                                if (element.TryGetProperty("payload", out var payloadProp) &&
                                    payloadProp.TryGetProperty("commits", out var commitsProp) &&
                                    commitsProp.ValueKind == JsonValueKind.Array)
                                {
                                    commitsCount = Math.Max(1, commitsProp.GetArrayLength());
                                }

                                counts[repoName] = counts.GetValueOrDefault(repoName, 0) + commitsCount;
                            }
                        }
                    }
                }

                foreach (var kvp in counts.OrderByDescending(c => c.Value).Take(3))
                {
                    var parts = kvp.Key.Split('/');
                    string owner = parts.Length > 0 ? parts[0] : "";
                    string name = parts.Length > 1 ? parts[1] : kvp.Key;

                    result.Add(new RepoContribution
                    {
                        FullName = kvp.Key,
                        Name = name,
                        Owner = owner,
                        Count = kvp.Value,
                        Href = $"https://github.com/{kvp.Key}",
                        AvatarUrl = $"https://github.com/{owner}.png?size=64"
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching events: {ex.Message}");
            }

            // Fallback to top repositories if no recent push events were found
            if (result.Count == 0)
            {
                try
                {
                    var reposUrl = $"https://api.github.com/users/{Uri.EscapeDataString(clean)}/repos?sort=pushed&per_page=3";
                    var res = await HttpClient.GetStringAsync(reposUrl);
                    using var doc = JsonDocument.Parse(res);
                    foreach (var repo in doc.RootElement.EnumerateArray())
                    {
                        if (repo.TryGetProperty("full_name", out var fnProp) &&
                            repo.TryGetProperty("name", out var nProp))
                        {
                            string fn = fnProp.GetString() ?? "";
                            string n = nProp.GetString() ?? "";
                            var parts = fn.Split('/');
                            string owner = parts.Length > 0 ? parts[0] : "";
                            int stars = 0;
                            if (repo.TryGetProperty("stargazers_count", out var st)) stars = st.GetInt32();

                            result.Add(new RepoContribution
                            {
                                FullName = fn,
                                Name = n,
                                Owner = owner,
                                Count = stars,
                                Href = $"https://github.com/{fn}",
                                AvatarUrl = $"https://github.com/{owner}.png?size=64"
                            });
                        }
                    }
                }
                catch { }
            }

            return result;
        }
    }
}
