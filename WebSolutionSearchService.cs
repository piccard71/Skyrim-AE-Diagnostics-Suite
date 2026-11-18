using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkyrimAutoDiag
{
    public class WebSearchResultItem
    {
        public string SourceBadge { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> SolutionSteps { get; set; } = new();
        public string Url { get; set; } = string.Empty;
        public bool HasUrl => !string.IsNullOrWhiteSpace(Url);

        // Alias-Properties, damit sowohl alte als auch neue XAML-Bindings immer funktionieren:
        public string SummaryText
        {
            get => Summary;
            set => Summary = value;
        }

        public List<string> RecommendedSteps
        {
            get => SolutionSteps;
            set => SolutionSteps = value;
        }

        public string ThreadUrl
        {
            get => Url;
            set => Url = value;
        }
    }

    public class WebSolutionSearchService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };

        static WebSolutionSearchService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("SkyrimAutoDiag/1.0 (Windows; Modding Diagnostic Suite)");
        }

        public async Task<List<WebSearchResultItem>> SearchSolutionsOnlineAsync(
            string userQuery,
            AppConfig config,
            bool isGerman)
        {
            var results = new List<WebSearchResultItem>();
            if (string.IsNullOrWhiteSpace(userQuery)) return results;

            AddLocalKnowledgeMatches(userQuery, isGerman, results);

            bool hasKey = !string.IsNullOrWhiteSpace(config.GeminiApiKey) &&
                          config.GeminiApiKey != "DEIN_GEMINI_API_KEY_HIER";

            if (!config.UseOfflineModeOnly && hasKey)
            {
                var aiItem = await TryQueryGeminiWithWebSearchAsync(userQuery, config, isGerman);
                if (aiItem != null)
                    results.Insert(0, aiItem);
            }

            var redditItems = await SearchRedditSkyrimModsAsync(userQuery, isGerman);
            results.AddRange(redditItems);

            string encoded = Uri.EscapeDataString($"Skyrim AE {userQuery} fix solved");
            results.Add(new WebSearchResultItem
            {
                SourceBadge = "GOOGLE / NEXUS",
                Title = isGerman
                    ? $"Erweiterte Online-Suche nach: \"{userQuery}\""
                    : $"Extended Online Search for: \"{userQuery}\"",
                Summary = isGerman
                    ? "Öffnet die gezielte Suche für Skyrim AE, Nexus Mods Foren und Reddit r/skyrimmods im Browser."
                    : "Opens a targeted search across Skyrim AE, Nexus Mods forums, and Reddit r/skyrimmods in your browser.",
                SolutionSteps = new List<string>
                {
                    isGerman
                        ? "Klicke unten auf 'Original-Beitrag im Browser öffnen', um alle Web-Treffer direkt im Standardbrowser anzuzeigen."
                        : "Click 'Open Original Post in Browser' below to view all web results in your default browser."
                },
                Url = $"https://www.google.com/search?q={encoded}"
            });

            return results;
        }

        private void AddLocalKnowledgeMatches(string query, bool isGerman, List<WebSearchResultItem> results)
        {
            try
            {
                var nexus = new NexusFeaturesService();
                var rules = nexus.LoadOrCreateCrashRulesDatabase();
                var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .Where(w => w.Length >= 3)
                                 .ToList();

                foreach (var r in rules)
                {
                    string blob = $"{r.IssueType} {string.Join(" ", r.Triggers)} {r.ExplanationDe} {r.ExplanationEn}";
                    if (words.Any(w => blob.Contains(w, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new WebSearchResultItem
                        {
                            SourceBadge = isGerman ? "LOKALE DATENBANK" : "LOCAL DATABASE",
                            Title = $"{r.IssueType} ({r.DefaultCulprit})",
                            Summary = isGerman ? r.ExplanationDe : r.ExplanationEn,
                            SolutionSteps = isGerman ? new List<string>(r.StepsDe) : new List<string>(r.StepsEn),
                            Url = $"https://www.google.com/search?q={Uri.EscapeDataString("Skyrim AE " + r.IssueType + " " + r.DefaultCulprit)}"
                        });
                    }
                }
            }
            catch { }
        }

        private async Task<WebSearchResultItem?> TryQueryGeminiWithWebSearchAsync(
            string query,
            AppConfig config,
            bool isGerman)
        {
            string preferred = GeminiDiagnosticsService.NormalizeModelName(config.SelectedGeminiModel);
            var modelsToTry = new List<string> { preferred };
            foreach (var m in GeminiDiagnosticsService.AvailableModels)
            {
                if (!modelsToTry.Contains(m))
                    modelsToTry.Add(m);
            }

            string langInstruction = isGerman
                ? "Antworte auf DEUTSCH (behalte Modding-Begriffe wie FormID, SSEEdit, Load Order, SKSE, Base Object Swapper, SMIM bei)."
                : "Respond in ENGLISH.";

            string prompt = $@"
Der Nutzer beschreibt folgendes Problem in Skyrim Anniversary Edition (1.6.x):
""{query}""

{langInstruction}
Suche nach bekannten Ursachen und konkreten Lösungen in der Skyrim-Modding-Community (Reddit r/skyrimmods, Nexus Mods, SMIM, Lux, JK's Interiors).
Antworte AUSSCHLIESSLICH im folgenden JSON-Format (ohne Markdown-Blöcke):
{{
  ""title"": ""Kurzer präziser Titel der Ursache"",
  ""summary"": ""Zusammenfassung, warum dieser Fehler in Skyrim AE auftritt"",
  ""steps"": [""Schritt 1..."", ""Schritt 2..."", ""Schritt 3...""],
  ""referenceUrl"": ""https://www.google.com/search?q={Uri.EscapeDataString("Skyrim AE " + query + " site:reddit.com/r/skyrimmods")}""
}}";

            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { response_mime_type = "application/json", temperature = 0.2 }
            };

            string payload = JsonSerializer.Serialize(body);

            foreach (var model in modelsToTry)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={config.GeminiApiKey.Trim()}";
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var resp = await _http.PostAsync(url, content);
                    if (!resp.IsSuccessStatusCode) continue;

                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    string rawText = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "{}";

                    rawText = CleanJson(rawText);
                    using var parsed = JsonDocument.Parse(rawText);
                    var root = parsed.RootElement;

                    var steps = new List<string>();
                    if (root.TryGetProperty("steps", out var stepsArr) && stepsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var s in stepsArr.EnumerateArray())
                        {
                            string? val = s.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) steps.Add(val);
                        }
                    }

                    string refUrl = root.TryGetProperty("referenceUrl", out var u) && !string.IsNullOrWhiteSpace(u.GetString())
                        ? u.GetString()!
                        : $"https://www.google.com/search?q={Uri.EscapeDataString("Skyrim AE " + query + " site:reddit.com/r/skyrimmods")}";

                    return new WebSearchResultItem
                    {
                        SourceBadge = $"GEMINI KI ({model})",
                        Title = root.TryGetProperty("title", out var t) ? (t.GetString() ?? query) : query,
                        Summary = root.TryGetProperty("summary", out var sum) ? (sum.GetString() ?? "") : "",
                        SolutionSteps = steps,
                        Url = refUrl
                    };
                }
                catch
                {
                    // Nächstes Fallback-Modell probieren
                }
            }

            return null;
        }

        private async Task<List<WebSearchResultItem>> SearchRedditSkyrimModsAsync(string query, bool isGerman)
        {
            var list = new List<WebSearchResultItem>();
            try
            {
                string translatedQuery = TranslateCommonGermanBugTerms(query);
                string encoded = Uri.EscapeDataString(translatedQuery);
                string url = $"https://www.reddit.com/r/skyrimmods/search.json?q={encoded}&restrict_sr=1&sort=relevance&limit=5";

                using var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return list;

                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var children = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("children");

                foreach (var child in children.EnumerateArray())
                {
                    var data = child.GetProperty("data");
                    string title = data.TryGetProperty("title", out var t) ? (t.GetString() ?? "") : "";
                    string selftext = data.TryGetProperty("selftext", out var st) ? (st.GetString() ?? "") : "";
                    string permalink = data.TryGetProperty("permalink", out var pl) ? (pl.GetString() ?? "") : "";

                    if (string.IsNullOrWhiteSpace(title)) continue;

                    string cleanSnippet = selftext.Replace("\r", " ").Replace("\n", " ").Trim();
                    if (cleanSnippet.Length > 320)
                        cleanSnippet = cleanSnippet.Substring(0, 320) + "...";

                    if (string.IsNullOrWhiteSpace(cleanSnippet))
                    {
                        cleanSnippet = isGerman
                            ? "Community-Diskussion und Lösungsvorschläge auf Reddit (r/skyrimmods)."
                            : "Community discussion and solution thread on Reddit (r/skyrimmods).";
                    }

                    list.Add(new WebSearchResultItem
                    {
                        SourceBadge = "REDDIT r/skyrimmods",
                        Title = WebUtility.HtmlDecode(title),
                        Summary = WebUtility.HtmlDecode(cleanSnippet),
                        SolutionSteps = new List<string>
                        {
                            isGerman
                                ? "Klicke auf 'Original-Beitrag im Browser öffnen', um die vollständigen Antworten und verlinkten Patches im Thread zu lesen."
                                : "Click 'Open Original Post in Browser' to read the full answers and linked patches in this thread."
                        },
                        Url = $"https://www.reddit.com{permalink}"
                    });
                }
            }
            catch { }
            return list;
        }

        private string TranslateCommonGermanBugTerms(string input)
        {
            string s = input;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["absturz"] = "crash CTD",
                ["stürzt ab"] = "crash CTD",
                ["schwarzes gesicht"] = "black face dark face bug",
                ["dunkles gesicht"] = "dark face bug",
                ["lila textur"] = "purple missing texture",
                ["stuhl"] = "chair furniture mesh texture",
                ["beflagte mähre"] = "Bannered Mare Whiterun",
                ["beflaggte mähre"] = "Bannered Mare Whiterun",
                ["weißlauf"] = "Whiterun",
                ["ausrufezeichen"] = "red exclamation mark missing mesh",
                ["t-pose"] = "t-pose animation nemesis pandora",
                ["steckt fest"] = "stuck animation freeze",
                ["unendlicher ladebildschirm"] = "infinite loading screen ILS",
                ["ladebildschirm"] = "loading screen CTD",
                ["wasser"] = "water seam flicker",
                ["flackert"] = "flickering light shadow",
                ["speicherstand"] = "savegame corrupt"
            };

            foreach (var kv in map)
            {
                if (s.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    s += " " + kv.Value;
            }
            return s;
        }

        public string SaveWebSearchAsHtmlReport(string query, List<WebSearchResultItem> items, bool isGerman)
        {
            string reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Berichte");
            Directory.CreateDirectory(reportsDir);

            string fileName = $"OnlineLoesungssuche_{(isGerman ? "DE" : "EN")}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.html";
            string fullPath = Path.Combine(reportsDir, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">");
            sb.AppendLine($"<title>Skyrim AE Online-Lösungssuche: {WebUtility.HtmlEncode(query)}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { background:#111318; color:#E2E8F0; font-family:'Segoe UI',sans-serif; padding:30px; }");
            sb.AppendLine(".container { max-width:960px; margin:0 auto; }");
            sb.AppendLine(".card { background:#1B1D24; border:1px solid #2D313E; border-radius:8px; padding:18px; margin-bottom:16px; }");
            sb.AppendLine(".badge { background:#2563EB; color:white; padding:3px 9px; border-radius:4px; font-size:12px; font-weight:bold; }");
            sb.AppendLine("a { color:#60A5FA; text-decoration:none; font-weight:bold; }");
            sb.AppendLine("</style></head><body><div class=\"container\">");
            sb.AppendLine($"<h1>🌐 {(isGerman ? "Online-Lösungssuche – Ergebnisse" : "Online Solution Finder – Results")}</h1>");
            sb.AppendLine($"<p><strong>{(isGerman ? "Problembeschreibung:" : "Issue Description:")}</strong> {WebUtility.HtmlEncode(query)}</p>");

            foreach (var item in items)
            {
                sb.AppendLine("<div class=\"card\">");
                sb.AppendLine($"<span class=\"badge\">{WebUtility.HtmlEncode(item.SourceBadge)}</span>");
                sb.AppendLine($"<h3>{WebUtility.HtmlEncode(item.Title)}</h3>");
                sb.AppendLine($"<p>{WebUtility.HtmlEncode(item.Summary)}</p>");
                if (item.SolutionSteps.Count > 0)
                {
                    sb.AppendLine("<ul>");
                    foreach (var step in item.SolutionSteps)
                        sb.AppendLine($"<li>{WebUtility.HtmlEncode(step)}</li>");
                    sb.AppendLine("</ul>");
                }
                if (item.HasUrl)
                    sb.AppendLine($"<p><a href=\"{WebUtility.HtmlEncode(item.Url)}\" target=\"_blank\">🌐 {WebUtility.HtmlEncode(item.Url)}</a></p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div></body></html>");
            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(true));

            try { Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true }); } catch { }
            return fullPath;
        }

        private static string CleanJson(string raw)
        {
            string t = raw.Trim();
            if (t.StartsWith("```"))
            {
                int nl = t.IndexOf('\n');
                if (nl != -1) t = t.Substring(nl + 1);
                if (t.EndsWith("```")) t = t.Substring(0, t.Length - 3);
            }
            return t.Trim();
        }
    }
}