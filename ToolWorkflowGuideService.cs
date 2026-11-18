﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyrimAutoDiag
{
    public class ToolWorkflowTemplate
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("tool_display_name")]
        public string ToolDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("match_keywords")]
        public List<string> MatchKeywords { get; set; } = new();

        [JsonPropertyName("title_de")]
        public string TitleDe { get; set; } = string.Empty;

        [JsonPropertyName("title_en")]
        public string TitleEn { get; set; } = string.Empty;

        [JsonPropertyName("steps_de")]
        public List<string> StepsDe { get; set; } = new();

        [JsonPropertyName("steps_en")]
        public List<string> StepsEn { get; set; } = new();
    }

    public class ToolWorkflowGuideService
    {
        public static string WorkflowsFilePath =>
            Path.Combine(AppConfig.DatabaseFolderPath, "tool_workflows.json");

        public List<ToolWorkflowTemplate> LoadOrCreateWorkflows()
        {
            try
            {
                if (File.Exists(WorkflowsFilePath))
                {
                    string json = File.ReadAllText(WorkflowsFilePath, Encoding.UTF8);
                    var loaded = JsonSerializer.Deserialize<List<ToolWorkflowTemplate>>(json);
                    if (loaded != null && loaded.Count > 0)
                        return loaded;
                }
            }
            catch { }

            var defaults = GetDefaultWorkflows();
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(WorkflowsFilePath, JsonSerializer.Serialize(defaults, opts), Encoding.UTF8);
            }
            catch { }

            return defaults;
        }

        public ToolWorkflowTemplate FindBestWorkflowForIssue(DiagnosedIssue issue)
        {
            var all = LoadOrCreateWorkflows();
            string hay = $"{issue.IssueType} {issue.CausingPlugin} {issue.ExplanationDe} {issue.ExplanationEn}";

            foreach (var wf in all)
            {
                if (wf.MatchKeywords.Any(k => hay.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    return wf;
            }

            return all.First();
        }

        public string BuildInlineWorkflowHtmlBlock(DiagnosedIssue issue, bool isGerman)
        {
            var wf = FindBestWorkflowForIssue(issue);
            string title = isGerman ? wf.TitleDe : wf.TitleEn;
            var rawSteps = isGerman ? wf.StepsDe : wf.StepsEn;

            string plugin = string.IsNullOrWhiteSpace(issue.CausingPlugin) ? "Mod.esp" : issue.CausingPlugin!;
            string formId = string.IsNullOrWhiteSpace(issue.AffectedFormId) ? "00000000" : issue.AffectedFormId!;

            var sb = new StringBuilder();
            sb.AppendLine("<div style=\"margin-top:14px; padding:14px; background:#141720; border-left:4px solid #F59E0B; border-radius:6px;\">");
            sb.AppendLine($"<div style=\"color:#FBBF24; font-weight:bold; margin-bottom:8px;\">ðŸ“˜ {WebUtility.HtmlEncode(wf.ToolDisplayName)} â€“ {WebUtility.HtmlEncode(title)}</div>");
            sb.AppendLine("<ul style=\"list-style:none; padding-left:4px; margin:0; line-height:1.6;\">");

            foreach (var step in rawSteps)
            {
                string resolved = step
                    .Replace("{PLUGIN}", plugin)
                    .Replace("{FORMID}", formId);
                sb.AppendLine($"<li><label style=\"cursor:pointer;\"><input type=\"checkbox\" style=\"margin-right:8px;\"/>{WebUtility.HtmlEncode(resolved)}</label></li>");
            }

            sb.AppendLine("</ul></div>");
            return sb.ToString();
        }

        public string GenerateAndOpenStandaloneWorkflowHtml(DiagnosedIssue issue, bool isGerman)
        {
            var wf = FindBestWorkflowForIssue(issue);
            string reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Berichte");
            Directory.CreateDirectory(reportsDir);

            string fileName = $"ToolWorkflow_{wf.WorkflowId}_{DateTime.Now:HHmmss}.html";
            string fullPath = Path.Combine(reportsDir, fileName);

            string title = isGerman ? wf.TitleDe : wf.TitleEn;
            string sub = isGerman
                ? $"Interaktive Schritt-f\u00FCr-Schritt-Checkliste f\u00FCr {wf.ToolDisplayName}"
                : $"Interactive Step-by-Step Checklist for {wf.ToolDisplayName}";

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">");
            sb.AppendLine($"<title>{WebUtility.HtmlEncode(title)}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { background:#121318; color:#E2E8F0; font-family:'Segoe UI',sans-serif; padding:30px; }");
            sb.AppendLine(".box { max-width:900px; margin:0 auto; background:#1B1D24; border:1px solid #2D313E; border-radius:10px; padding:24px; }");
            sb.AppendLine("h1 { color:#F8FAFC; margin-top:0; font-size:22px; }");
            sb.AppendLine(".meta { color:#94A3B8; margin-bottom:18px; }");
            sb.AppendLine("</style></head><body><div class=\"box\">");
            sb.AppendLine($"<h1>ðŸ“˜ {WebUtility.HtmlEncode(title)}</h1>");
            sb.AppendLine($"<div class=\"meta\">{WebUtility.HtmlEncode(sub)} | Plugin: <strong>{WebUtility.HtmlEncode(issue.CausingPlugin ?? "N/A")}</strong> | FormID: <strong>{WebUtility.HtmlEncode(issue.AffectedFormId ?? "N/A")}</strong></div>");
            sb.AppendLine(BuildInlineWorkflowHtmlBlock(issue, isGerman));
            sb.AppendLine("</div></body></html>");

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            try
            {
                Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
            }
            catch { }

            return fullPath;
        }

        private List<ToolWorkflowTemplate> GetDefaultWorkflows()
        {
            return new List<ToolWorkflowTemplate>
            {
                new ToolWorkflowTemplate
                {
                    WorkflowId = "SSEEDIT_INSPECT",
                    ToolDisplayName = "SSEEdit (xEdit)",
                    MatchKeywords = new List<string> { "NavMesh", "Access Violation", "SSEEdit", "Conflict", "BlackFace", "FaceGen" },
                    TitleDe = "Gezielte Konflikt- & FormID-PrÃ¼fung in SSEEdit",
                    TitleEn = "Targeted Conflict & FormID Inspection in SSEEdit",
                    StepsDe = new List<string>
                    {
                        "Klicke auf der Fehlerkarte auf 'In SSEEdit prÃ¼fen', um SSEEdit mit '{PLUGIN}' zu \u00F6ffnen.",
                        "Warte, bis unten im Protokoll 'Background Loader: finished' erscheint.",
                        "Trage oben links in das Suchfeld 'FormID' den Wert '{FORMID}' ein und drÃ¼cke Enter.",
                        "Pr\u00FCfe im rechten Teilfenster alle rot hinterlegten Konflikt-Spalten und korrigiere fehlerhafte Ãœberschreibungen (Speichern mit Strg + S)."
                    },
                    StepsEn = new List<string>
                    {
                        "Click 'Inspect in SSEEdit' on the issue card to launch SSEEdit with '{PLUGIN}'.",
                        "Wait until the log at the bottom displays 'Background Loader: finished'.",
                        "Enter '{FORMID}' into the top-left 'FormID' search box and press Enter.",
                        "Inspect red conflict columns in the right pane and resolve broken overrides (press Ctrl + S to save)."
                    }
                },
                new ToolWorkflowTemplate
                {
                    WorkflowId = "SSEEDIT_QAC",
                    ToolDisplayName = "SSEEdit Quick Auto Clean",
                    MatchKeywords = new List<string> { "ITM", "UDR", "Deleted", "Clean" },
                    TitleDe = "Automatische Plugin-Bereinigung (ITMs & UDRs)",
                    TitleEn = "Automated Plugin Cleaning (ITMs & UDRs)",
                    StepsDe = new List<string>
                    {
                        "Klicke links im Tool-Hub auf den grÃ¼nen Button 'SSEEdit Quick Auto Clean'.",
                        "Klicke im Auswahlfenster mit Rechtsklick auf 'Select None' und hake nur '{PLUGIN}' an.",
                        "BestÃ¤tige mit 'OK' und warte, bis SSEEdit alle ITMs und gelÃ¶schten Referenzen bereinigt hat.",
                        "SchlieÃŸe SSEEdit, um das bereinigte Plugin automatisch zu speichern."
                    },
                    StepsEn = new List<string>
                    {
                        "Click the green 'SSEEdit Quick Auto Clean' button in the left Tool Hub.",
                        "Right-click the plugin list -> 'Select None', then check only '{PLUGIN}'.",
                        "Click 'OK' and wait for SSEEdit to clean all ITMs and deleted references.",
                        "Close SSEEdit to save the cleaned plugin automatically."
                    }
                },
                new ToolWorkflowTemplate
                {
                    WorkflowId = "PANDORA_NEMESIS",
                    ToolDisplayName = "Pandora / Nemesis Engine",
                    MatchKeywords = new List<string> { "Animation", "hkx", "hkbBehaviorGraph", "Pandora", "Nemesis", "OAR", "DAR" },
                    TitleDe = "Animations-Baum (Behavior Graph) neu generieren",
                    TitleEn = "Rebuild Animation Behavior Graph",
                    StepsDe = new List<string>
                    {
                        "Klicke links im Tool-Hub auf 'Pandora / Nemesis Engine'.",
                        "Pr\u00FCfe, ob alle installierten Animations-Mods in der Liste angehakt sind.",
                        "Klicke auf 'Launch' (Pandora) bzw. 'Update Engine' -> 'Launch Nemesis Behavior Engine'.",
                        "Warte bis 100 % ('0 Errors') und schlieÃŸe erst danach das Fenster."
                    },
                    StepsEn = new List<string>
                    {
                        "Click 'Pandora / Nemesis Engine' in the left Tool Hub.",
                        "Ensure all installed animation mods are checked in the list.",
                        "Click 'Launch' (Pandora) or 'Update Engine' -> 'Launch Nemesis Behavior Engine'.",
                        "Wait until completion reaches 100% ('0 Errors') before closing the window."
                    }
                },
                new ToolWorkflowTemplate
                {
                    WorkflowId = "BODYSLIDE_SMP",
                    ToolDisplayName = "BodySlide x64",
                    MatchKeywords = new List<string> { "TriShape", "nif", "hdtSMP64", "BodySlide", "Outfit", "0D6DDDA" },
                    TitleDe = "3D-RÃ¼stungs-Meshes & Physik in BodySlide neu erstellen",
                    TitleEn = "Rebuild 3D Outfit Meshes & Physics in BodySlide",
                    StepsDe = new List<string>
                    {
                        "Klicke links im Tool-Hub auf 'BodySlide x64'.",
                        "WÃ¤hle oben links unter 'Outfit/Body' deinen KÃ¶rpertyp und darunter dein 'Preset' aus.",
                        "Setze unten links das HÃ¤kchen bei 'Build Morphs' und klicke auf 'Batch Build...'.",
                        "BestÃ¤tige mit 'Build', um defekte .nif-Dateien sauber neu zu erstellen."
                    },
                    StepsEn = new List<string>
                    {
                        "Click 'BodySlide x64' in the left Tool Hub.",
                        "Select your body type under 'Outfit/Body' and your desired 'Preset' in the top left.",
                        "Check 'Build Morphs' at the bottom left and click 'Batch Build...'.",
                        "Confirm with 'Build' to cleanly regenerate all outfit .nif meshes."
                    }
                },
                new ToolWorkflowTemplate
                {
                    WorkflowId = "LOOT_LOADORDER",
                    ToolDisplayName = "LOOT",
                    MatchKeywords = new List<string> { "Missing Master", "0198090", "05E1F22", "Load Order", "LOOT" },
                    TitleDe = "Ladereihenfolge sortieren & fehlende Master prÃ¼fen",
                    TitleEn = "Sort Load Order & Check Missing Masters",
                    StepsDe = new List<string>
                    {
                        "Klicke links im Tool-Hub auf 'LOOT (Load Order)'.",
                        "Klicke oben auf 'Plugins sortieren' und prÃ¼fe rote Fehlermeldungen auf fehlende Master-Mods.",
                        "Klicke auf 'Ãœbernehmen' (Apply) und stelle sicher, dass AutoDiagPatch.esp ganz unten bleibt."
                    },
                    StepsEn = new List<string>
                    {
                        "Click 'LOOT (Load Order)' in the left Tool Hub.",
                        "Click 'Sort Plugins' at the top and check red error cards for missing master files.",
                        "Click 'Apply' and verify that AutoDiagPatch.esp stays at the very bottom of your load order."
                    }
                },
                new ToolWorkflowTemplate
                {
                    WorkflowId = "BETHINI_VRAM",
                    ToolDisplayName = "BethINI Pie & Windows Pagefile",
                    MatchKeywords = new List<string> { "ShadowSceneNode", "BSLightingShaderProperty", "d3d11", "nvwgf2umx", "amdxx64", "VRAM" },
                    TitleDe = "Grafik-INI & Schatten-Optimierung mit BethINI Pie",
                    TitleEn = "Graphics INI & Shadow Optimization via BethINI Pie",
                    StepsDe = new List<string>
                    {
                        "SchlieÃŸe Skyrim und klicke links im Tool-Hub auf 'BethINI Pie'.",
                        "Aktiviere 'Recommended Tweaks' und reduziere bei Schatten-Crashes die 'Shadow Resolution' von 4096 auf 2048.",
                        "Speichere die INI-Dateien und stelle in Windows eine Auslagerungsdatei (Pagefile) von mindestens 20 GB auf deiner SSD ein."
                    },
                    StepsEn = new List<string>
                    {
                        "Close Skyrim and click 'BethINI Pie' in the left Tool Hub.",
                        "Enable 'Recommended Tweaks' and lower 'Shadow Resolution' from 4096 to 2048 if experiencing shadow crashes.",
                        "Save your INIs and configure a Windows Pagefile of at least 20 GB on your SSD."
                    }
                }
            };
        }
    }
}
