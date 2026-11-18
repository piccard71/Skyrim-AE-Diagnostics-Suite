using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkyrimAutoDiag
{
    public class DiagnosticReport
    {
        [JsonPropertyName("summary_de")]
        public string SummaryDe { get; set; } = string.Empty;

        [JsonPropertyName("summary_en")]
        public string SummaryEn { get; set; } = string.Empty;

        [JsonIgnore]
        public string Summary =>
            LocalizationService.CurrentLanguage == AppLanguage.German ? SummaryDe : SummaryEn;

        [JsonPropertyName("detectedIssues")]
        public List<DiagnosedIssue> DetectedIssues { get; set; } = new();
    }

    public class DiagnosedIssue
    {
        [JsonPropertyName("issueType")]
        public string IssueType { get; set; } = string.Empty;

        [JsonPropertyName("affectedFormId")]
        public string? AffectedFormId { get; set; }

        [JsonPropertyName("causingPlugin")]
        public string? CausingPlugin { get; set; }

        [JsonPropertyName("explanation_de")]
        public string ExplanationDe { get; set; } = string.Empty;

        [JsonPropertyName("explanation_en")]
        public string ExplanationEn { get; set; } = string.Empty;

        [JsonIgnore]
        public string Explanation =>
            LocalizationService.CurrentLanguage == AppLanguage.German ? ExplanationDe : ExplanationEn;

        [JsonPropertyName("stepByStepSolution_de")]
        public List<string> StepByStepSolutionDe { get; set; } = new();

        [JsonPropertyName("stepByStepSolution_en")]
        public List<string> StepByStepSolutionEn { get; set; } = new();

        [JsonIgnore]
        public List<string> StepByStepSolution =>
            LocalizationService.CurrentLanguage == AppLanguage.German ? StepByStepSolutionDe : StepByStepSolutionEn;

        [JsonPropertyName("canAutoPatchWithMutagen")]
        public bool CanAutoPatchWithMutagen { get; set; }
    }

    public class GeminiDiagnosticsService
    {
        public static readonly List<string> AvailableModels = new()
        {
            "gemini-3-flash-preview",
            "gemini-3.1-pro-preview",
            "gemini-3.1-flash-lite-preview",
            "gemini-2.5-flash"
        };

        public const string DefaultModel = "gemini-3-flash-preview";

        private readonly string _apiKey;
        private string _preferredModel;
        public string LastUsedModel { get; private set; } = DefaultModel;

        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(2) };

        public GeminiDiagnosticsService(string apiKey, string? preferredModel = null)
        {
            _apiKey = apiKey?.Trim() ?? string.Empty;
            _preferredModel = NormalizeModelName(preferredModel);
            LastUsedModel = _preferredModel;
        }

        public static string NormalizeModelName(string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return DefaultModel;

            string clean = model.Trim().Replace("models/", "");
            return clean switch
            {
                "gemini-2.5-pro" => "gemini-3.1-pro-preview",
                "gemini-3-pro-preview" => "gemini-3.1-pro-preview",
                "gemini-1.5-pro" => "gemini-3.1-pro-preview",
                "gemini-1.5-flash" => "gemini-3-flash-preview",
                "gemini-2.0-flash" => "gemini-3-flash-preview",
                _ => clean
            };
        }

        public async Task<(bool Success, string WorkingModel, string Message)> TestConnectionAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;

            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "DEIN_GEMINI_API_KEY_HIER")
            {
                return (false, _preferredModel, isDe
                    ? "Kein gültiger Gemini API-Key eingetragen. Klicke auf '🔑 API Key erstellen', um kostenlos einen Key in Google AI Studio zu generieren."
                    : "No valid Gemini API key entered. Click '🔑 Create API Key' to generate a free key in Google AI Studio.");
            }

            var modelsToTry = GetModelFallbackOrder(_preferredModel);
            var errorLog = new StringBuilder();

            foreach (var model in modelsToTry)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new { parts = new[] { new { text = "Reply with OK" } } }
                        }
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json");

                    using var response = await _httpClient.PostAsync(url, content);
                    string responseJson = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _preferredModel = model;
                        LastUsedModel = model;
                        string msg = isDe
                            ? $"Verbindung erfolgreich! Aktives Modell: {model}"
                            : $"Connection successful! Active model: {model}";
                        return (true, model, msg);
                    }

                    errorLog.AppendLine($"[{model}] HTTP {(int)response.StatusCode}: {responseJson}");
                }
                catch (Exception ex)
                {
                    errorLog.AppendLine($"[{model}] {ex.Message}");
                }
            }

            return (false, _preferredModel, errorLog.ToString().Trim());
        }

        public async Task<(bool Success, string Message)> TestApiConnectionAsync()
        {
            var (ok, _, msg) = await TestConnectionAsync();
            return (ok, msg);
        }

        public async Task<DiagnosticReport?> AnalyzeSessionAsync(
            string crashLogExcerpt,
            string skseAndAssetWarnings,
            List<string> screenshotPaths,
            IProgress<(int Percent, string StatusMessage)>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "DEIN_GEMINI_API_KEY_HIER")
            {
                throw new InvalidOperationException(
                    LocalizationService.CurrentLanguage == AppLanguage.German
                        ? "Kein gültiger Gemini API-Key hinterlegt! Bitte im Tab 'Konfiguration & Pfade' eintragen."
                        : "No valid Gemini API key configured! Please enter it in the 'Configuration & Paths' tab.");
            }

            var parts = new List<object>();

            string prompt = $@"
Du bist ein hochspezialisierter Diagnose-Assistent für Skyrim Anniversary Edition (1.6.x).
WICHTIG: Fülle für den zweisprachigen HTML-Bericht SOWOHL die deutschen Felder (_de) ALS AUCH die englischen Felder (_en) vollständig aus! Behalte gängige Modding-Fachbegriffe (FormID, Load Order, Black Face Bug, FaceGen, Crash Logger, SSEEdit) bei.

Aufgaben:
1. Analysiere alle angehängten In-Game-Screenshots:
   - Lies das geöffnete 'More Informative Console' (MIC) Fenster im Bild aus (Base FormID, RefID, Source Mod, Last Base Change).
   - Erkenne den visuellen Fehler im Bild (Black Face Bug, lila fehlende Textur, rotes Ausrufezeichen für fehlendes Mesh).
2. Analysiere den Crash-Log (Crash Logger SSE AE) und die SKSE-/Papyrus-/Savegame-Warnungen:
   - Identifiziere bei einem CTD die verursachende .dll, .esp, korrupte .nif-Mesh oder das fehlerhafte Papyrus-Skript anhand des Callstacks und der Register (RSP/RCX/RAX).
3. Erstelle für JEDES Problem eine präzise Schritt-für-Schritt-Lösung auf Deutsch UND Englisch.
4. Setze 'canAutoPatchWithMutagen' auf true, wenn es sich um einen NPC-FaceGen-Konflikt (Black Face Bug), Leveled-List- oder Container-Konflikt handelt.

Antworte AUSSCHLIESSLICH im folgenden JSON-Schema (ohne Markdown-Blöcke):
{{
  ""summary_de"": ""Deutsche Gesamtzusammenfassung der Sitzung"",
  ""summary_en"": ""English overall summary of the session"",
  ""detectedIssues"": [
    {{
      ""issueType"": ""CTD | BlackFaceBug | MissingMesh | MissingTexture | SKSEIncompatible"",
      ""affectedFormId"": ""00013BBF"",
      ""causingPlugin"": ""ModName.esp oder Plugin.dll"",
      ""explanation_de"": ""Deutsche technische Erklärung"",
      ""explanation_en"": ""English technical explanation"",
      ""stepByStepSolution_de"": [""Schritt 1..."", ""Schritt 2...""],
      ""stepByStepSolution_en"": [""Step 1..."", ""Step 2...""],
      ""canAutoPatchWithMutagen"": true
    }}
  ]
}}

--- CRASH LOG AUSZUG ---
{crashLogExcerpt}

--- SKSE, PAPYRUS & ASSET WARNUNGEN ---
{skseAndAssetWarnings}
";
            parts.Add(new { text = prompt });

            foreach (var path in screenshotPaths)
            {
                if (!File.Exists(path)) continue;
                byte[] imageBytes = await File.ReadAllBytesAsync(path);
                string base64Image = Convert.ToBase64String(imageBytes);

                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = "image/png",
                        data = base64Image
                    }
                });
            }

            var requestBody = new
            {
                contents = new[] { new { parts = parts } },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.2
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            var modelsToTry = GetModelFallbackOrder(_preferredModel);
            Exception? lastException = null;

            for (int i = 0; i < modelsToTry.Count; i++)
            {
                string model = modelsToTry[i];
                progress?.Report((50 + i * 10, $"Gemini KI-Analyse läuft ({model})..."));

                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    using var response = await _httpClient.PostAsync(url, content);
                    string responseJson = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        lastException = new HttpRequestException($"HTTP {(int)response.StatusCode} ({model}): {responseJson}");
                        continue;
                    }

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    {
                        lastException = new InvalidOperationException($"Keine Antwort-Kandidaten von {model} erhalten.");
                        continue;
                    }

                    string rawModelJson = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "{}";

                    rawModelJson = CleanJsonOutput(rawModelJson);
                    LastUsedModel = model;

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<DiagnosticReport>(rawModelJson, options);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            throw lastException ?? new HttpRequestException("Alle Gemini-Modelle konnten nicht erreicht werden.");
        }

        private static List<string> GetModelFallbackOrder(string preferred)
        {
            string normalized = NormalizeModelName(preferred);
            var list = new List<string> { normalized };

            foreach (var m in AvailableModels)
            {
                if (!list.Contains(m))
                    list.Add(m);
            }
            return list;
        }

        private static string CleanJsonOutput(string raw)
        {
            string trimmed = raw.Trim();
            if (trimmed.StartsWith("```"))
            {
                int firstNewLine = trimmed.IndexOf('\n');
                if (firstNewLine != -1)
                    trimmed = trimmed.Substring(firstNewLine + 1);

                if (trimmed.EndsWith("```"))
                    trimmed = trimmed.Substring(0, trimmed.Length - 3);
            }
            return trimmed.Trim();
        }
    }
}