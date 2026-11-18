﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace SkyrimAutoDiag
{
    public class CrashRuleEntry
    {
        public string RuleId { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public List<string> Triggers { get; set; } = new();
        public string DefaultCulprit { get; set; } = string.Empty;
        public string ExplanationDe { get; set; } = string.Empty;
        public string ExplanationEn { get; set; } = string.Empty;
        public List<string> StepsDe { get; set; } = new();
        public List<string> StepsEn { get; set; } = new();
        public bool CanAutoPatch { get; set; }
    }

    public class NexusFeaturesService
    {
        private static string RulesFilePath =>
            Path.Combine(AppConfig.DatabaseFolderPath, "crash_rules.json");

        public int AutoDetectPaths(AppConfig cfg)
        {
            int found = 0;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Bethesda Softworks\Skyrim Special Edition");
                string? regPath = key?.GetValue("installed path") as string;
                if (!string.IsNullOrWhiteSpace(regPath) && Directory.Exists(regPath))
                {
                    cfg.SkyrimRootPath = regPath.TrimEnd('\\', '/');
                    found++;
                }
            }
            catch { }

            var searchRoots = new List<string>();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                string r = drive.RootDirectory.FullName;
                searchRoots.Add(Path.Combine(r, "Modding"));
                searchRoots.Add(Path.Combine(r, "Skyrim AE Modding"));
                searchRoots.Add(Path.Combine(r, "Skyrim Modding"));
                searchRoots.Add(Path.Combine(r, "Games"));
                searchRoots.Add(Path.Combine(r, "SteamLibrary", "steamapps", "common"));
                searchRoots.Add(Path.Combine(r, "Program Files (x86)", "Steam", "steamapps", "common"));
            }

            if (!File.Exists(Path.Combine(cfg.SkyrimRootPath, "SkyrimSE.exe")))
            {
                string? skyExe = FindFileInRoots(searchRoots, "SkyrimSE.exe", 3);
                if (skyExe != null) { cfg.SkyrimRootPath = Path.GetDirectoryName(skyExe)!; found++; }
            }

            if (TryLocateTool(searchRoots, new[] { "SSEEdit.exe", "SSEEdit64.exe" }, out string sse))
            { cfg.SseEditExePath = sse; found++; }

            if (TryLocateTool(searchRoots, new[] { "SSEEditQAC.exe", "SSEEdit.exe", "SSEEdit64.exe" }, out string qac))
            { cfg.SseEditQuickCleanExePath = qac; found++; }

            if (TryLocateTool(searchRoots, new[] { "Wrye Bash.exe" }, out string wb))
            { cfg.WryeBashExePath = wb; found++; }

            if (TryLocateTool(searchRoots, new[] { "LOOT.exe" }, out string loot))
            { cfg.LootExePath = loot; found++; }

            if (TryLocateTool(searchRoots, new[] { "Synthesis.exe" }, out string syn))
            { cfg.SynthesisExePath = syn; found++; }

            if (TryLocateTool(searchRoots, new[] { "Pandora Behaviour Engine+.exe", "Nemesis Unlimited Behavior Engine.exe" }, out string pan))
            { cfg.PandoraExePath = pan; found++; }

            if (TryLocateTool(searchRoots, new[] { "BodySlide x64.exe", "BodySlide.exe" }, out string bs))
            { cfg.BodySlideExePath = bs; found++; }

            if (TryLocateTool(searchRoots, new[] { "BethINI.exe" }, out string beth))
            { cfg.BethIniExePath = beth; found++; }

            cfg.Save();
            return found;
        }

        private bool TryLocateTool(List<string> roots, string[] exeNames, out string foundPath)
        {
            foreach (var name in exeNames)
            {
                string? hit = FindFileInRoots(roots, name, 3);
                if (hit != null) { foundPath = hit; return true; }
            }
            foundPath = string.Empty;
            return false;
        }

        private string? FindFileInRoots(List<string> roots, string exeName, int maxDepth)
        {
            foreach (var root in roots.Where(Directory.Exists))
            {
                var res = SearchDirectorySafe(root, exeName, 0, maxDepth);
                if (res != null) return res;
            }
            return null;
        }

        private string? SearchDirectorySafe(string dir, string exeName, int depth, int maxDepth)
        {
            if (depth > maxDepth) return null;
            try
            {
                string direct = Path.Combine(dir, exeName);
                if (File.Exists(direct)) return direct;
                foreach (var sub in Directory.GetDirectories(dir))
                {
                    var found = SearchDirectorySafe(sub, exeName, depth + 1, maxDepth);
                    if (found != null) return found;
                }
            }
            catch { }
            return null;
        }

        public int UpgradeOutdatedEslHeaders(string dataFolderPath, string backupFolderPath)
        {
            int upgradedCount = 0;
            Directory.CreateDirectory(backupFolderPath);

            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            foreach (var listing in env.LoadOrder.PriorityOrder)
            {
                var roMod = listing.Mod;
                if (roMod == null) continue;

                bool isLight = roMod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Small)
                               || listing.ModKey.Type == ModType.Light;
                if (!isLight || roMod.ModHeader.Stats.Version >= 1.71f) continue;

                bool hasLowFormIds = roMod.EnumerateMajorRecords()
                    .Any(r => r.FormKey.ModKey == listing.ModKey && r.FormKey.ID < 0x800);
                if (!hasLowFormIds) continue;

                string pluginPath = Path.Combine(dataFolderPath, listing.ModKey.FileName);
                if (!File.Exists(pluginPath)) continue;

                try
                {
                    string backupCopy = Path.Combine(backupFolderPath,
                        $"{listing.ModKey.FileName}.bak_{DateTime.Now:yyyyMMdd_HHmmss}");
                    File.Copy(pluginPath, backupCopy, true);

                    var mutableMod = SkyrimMod.CreateFromBinary(pluginPath, SkyrimRelease.SkyrimSE);
                    mutableMod.ModHeader.Stats.Version = 1.71f;
                    mutableMod.WriteToBinary(pluginPath, new Mutagen.Bethesda.Plugins.Binary.Parameters.BinaryWriteParameters
                    {
                        ModKey = Mutagen.Bethesda.Plugins.Binary.Parameters.ModKeyOption.NoCheck
                    });
                    upgradedCount++;
                }
                catch { }
            }
            return upgradedCount;
        }

        public List<CrashRuleEntry> LoadOrCreateCrashRulesDatabase()
        {
            try
            {
                if (File.Exists(RulesFilePath))
                {
                    var loaded = JsonSerializer.Deserialize<List<CrashRuleEntry>>(File.ReadAllText(RulesFilePath));
                    if (loaded != null && loaded.Count > 0) return loaded;
                }
            }
            catch { }

            var defaults = GetDefaultCommunityCrashRules();
            try
            {
                File.WriteAllText(RulesFilePath, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
            return defaults;
        }

        public DiagnosticReport AnalyzeOffline(
            string crashLogExcerpt,
            string skseAndPapyrusSummary,
            List<string> screenshotPaths)
        {
            var rules = LoadOrCreateCrashRulesDatabase();
            var report = new DiagnosticReport
            {
                SummaryDe = $"Lokale Offline-Diagnose Ã¼ber 'crash_rules.json' ({rules.Count} aktive Signaturen) abgeschlossen.",
                SummaryEn = $"Local offline diagnostics via 'crash_rules.json' ({rules.Count} active signatures) completed."
            };

            string combined = (crashLogExcerpt ?? "") + "\n" + (skseAndPapyrusSummary ?? "");
            string? detectedFormId = ExtractFirstFormId(crashLogExcerpt);
            string? detectedPlugin = ExtractTopPluginOrDll(crashLogExcerpt);

            foreach (var rule in rules)
            {
                if (rule.Triggers.Any(t => combined.Contains(t, StringComparison.OrdinalIgnoreCase)))
                {
                    report.DetectedIssues.Add(new DiagnosedIssue
                    {
                        IssueType = rule.IssueType,
                        AffectedFormId = detectedFormId ?? "N/A",
                        CausingPlugin = detectedPlugin ?? rule.DefaultCulprit,
                        ExplanationDe = rule.ExplanationDe,
                        ExplanationEn = rule.ExplanationEn,
                        StepByStepSolutionDe = new List<string>(rule.StepsDe),
                        StepByStepSolutionEn = new List<string>(rule.StepsEn),
                        CanAutoPatchWithMutagen = rule.CanAutoPatch
                    });
                }
            }

            if (crashLogExcerpt != null &&
                crashLogExcerpt.Contains("EXCEPTION_ACCESS_VIOLATION", StringComparison.OrdinalIgnoreCase) &&
                report.DetectedIssues.Count == 0)
            {
                string culprit = detectedPlugin ?? "SkyrimSE.exe / Unknown Mod";
                report.DetectedIssues.Add(new DiagnosedIssue
                {
                    IssueType = "CTD (Access Violation)",
                    AffectedFormId = detectedFormId ?? "N/A",
                    CausingPlugin = culprit,
                    ExplanationDe = $"Speicherzugriffsverletzung (EXCEPTION_ACCESS_VIOLATION) in Modul '{culprit}' erkannt.",
                    ExplanationEn = $"Memory access violation (EXCEPTION_ACCESS_VIOLATION) detected in module '{culprit}'.",
                    StepByStepSolutionDe = new List<string>
                    {
                        $"Pr\u00FCfe '{culprit}' in SSEEdit auf fehlerhafte Referenzen.",
                        "FÃ¼hre links 'KompatibilitÃ¤ts-Patch + SPID erstellen' aus."
                    },
                    StepByStepSolutionEn = new List<string>
                    {
                        $"Inspect '{culprit}' in SSEEdit for broken references.",
                        "Run 'Build Compatibility Patch + SPID' on the left."
                    },
                    CanAutoPatchWithMutagen = true
                });
            }

            if (screenshotPaths.Count > 0)
            {
                report.DetectedIssues.Add(new DiagnosedIssue
                {
                    IssueType = "In-Game Screenshot (Offline)",
                    AffectedFormId = "See HTML Report",
                    CausingPlugin = $"{screenshotPaths.Count} Screenshot(s)",
                    ExplanationDe = $"Es wurden {screenshotPaths.Count} In-Game-Screenshots aufgenommen und in den HTML-Bericht eingebettet.",
                    ExplanationEn = $"{screenshotPaths.Count} in-game screenshot(s) captured and embedded into the HTML report.",
                    StepByStepSolutionDe = new List<string>
                    {
                        "Klicke auf 'Auto-Fix in AutoDiagPatch.esp (Mutagen)', um NPC-FaceGen-Konflikte (auch aus BSAs) zu reparieren."
                    },
                    StepByStepSolutionEn = new List<string>
                    {
                        "Click 'Auto-Fix in AutoDiagPatch.esp (Mutagen)' to resolve NPC FaceGen conflicts (including BSAs)."
                    },
                    CanAutoPatchWithMutagen = true
                });
            }

            return report;
        }

        private List<CrashRuleEntry> GetDefaultCommunityCrashRules() => new()
        {
            new CrashRuleEntry
            {
                RuleId = "MISSING_MASTER_OFFSET",
                IssueType = "CTD on Startup (Missing Master)",
                Triggers = new() { "+0198090", "+05E1F22", "Missing Master" },
                DefaultCulprit = "Load Order / Missing Master (.esm/.esp)",
                ExplanationDe = "Bekannter Startup-Crash-Offset (+0198090 / +05E1F22): Einem aktiven Plugin fehlt eine vorausgesetzte Master-Datei (.esm/.esp).",
                ExplanationEn = "Known startup crash offset (+0198090 / +05E1F22): An active plugin is missing a required master file (.esm/.esp).",
                StepsDe = new() { "Starte LOOT oder prÃ¼fe die Warnungen in Mod Organizer 2 auf fehlende Master-Dateien.", "Ã–ffne SSEEdit â€“ es bricht genau bei dem Plugin ab, dem ein Master fehlt." },
                StepsEn = new() { "Run LOOT or check Mod Organizer 2 warnings for missing master files.", "Launch SSEEdit â€“ it will stop loading right at the plugin missing a master." },
                CanAutoPatch = false
            },
            new CrashRuleEntry
            {
                RuleId = "ESL_HEADER_171",
                IssueType = "ESL Header < 1.71 Bug",
                Triggers = new() { "ESL-HEADER BUG", "ESL HEADER BUG", "requires 1.71", "benÃ¶tigt 1.71" },
                DefaultCulprit = "Outdated ESL Plugin (< 1.71)",
                ExplanationDe = "Ein Light-Plugin nutzt FormIDs unter 0x800 mit einem alten Header (< 1.71), was in Skyrim AE 1.6.1130+ Referenzen zerstÃ¶rt.",
                ExplanationEn = "A light plugin uses FormIDs below 0x800 with an old header (< 1.71), breaking references in Skyrim AE 1.6.1130+.",
                StepsDe = new() { "Klicke links auf den lila Button 'ESL-Header (< 1.71) automatisch reparieren'." },
                StepsEn = new() { "Click the purple 'Auto-Fix ESL Headers (< 1.71)' button on the left." },
                CanAutoPatch = true
            },
            new CrashRuleEntry
            {
                RuleId = "SHADOW_SCENE_LIGHTING",
                IssueType = "CTD (ShadowSceneNode / Lighting)",
                Triggers = new() { "ShadowSceneNode", "BSLightingShaderProperty", "NiPointLight", "BSGeometryListCullingProcess" },
                DefaultCulprit = "Lighting / Mesh Shader Mod",
                ExplanationDe = "Absturz in der Beleuchtungs- oder Shader-Pipeline (ShadowSceneNode / BSLightingShaderProperty).",
                ExplanationEn = "Crash in the lighting or shader pipeline (ShadowSceneNode / BSLightingShaderProperty).",
                StepsDe = new() { "Pr\u00FCfe die im Crash-Log direkt Ã¼ber dem Callstack genannten .nif-Dateien.", "Pr\u00FCfe Licht-Mods (Lux, ELFX) auf fehlende KompatibilitÃ¤ts-Patches." },
                StepsEn = new() { "Check the .nif files listed right above the callstack in your crash log.", "Check lighting mods (Lux, ELFX) for missing compatibility patches." },
                CanAutoPatch = false
            },
            new CrashRuleEntry
            {
                RuleId = "CORRUPT_NIF_TRISHAPE",
                IssueType = "CTD (Corrupt .nif Mesh / TriShape)",
                Triggers = new() { "BSDynamicTriShape", "BSTriShape", "NiNode", "BSFadeNode", "+0D6DDDA" },
                DefaultCulprit = "Corrupted .nif Mesh / Outfit",
                ExplanationDe = "Absturz beim Laden eines 3D-Modells (BSTriShape / BSDynamicTriShape / +0D6DDDA).",
                ExplanationEn = "Crash while loading a 3D mesh (BSTriShape / BSDynamicTriShape / +0D6DDDA).",
                StepsDe = new() { "Baue deine Meshes in BodySlide x64 neu.", "Klicke auf 'KompatibilitÃ¤ts-Patch + SPID erstellen', falls ein NPC-HeadPart betroffen ist." },
                StepsEn = new() { "Rebuild your outfits in BodySlide x64.", "Click 'Build Compatibility Patch + SPID' if an NPC HeadPart is involved." },
                CanAutoPatch = true
            },
            new CrashRuleEntry
            {
                RuleId = "HDT_SMP_PHYSICS",
                IssueType = "CTD (Faster HDT-SMP Physics)",
                Triggers = new() { "hdtSMP64.dll" },
                DefaultCulprit = "hdtSMP64.dll",
                ExplanationDe = "Faster HDT-SMP ist beim Berechnen einer Haar-, Umhang- oder RÃ¼stungs-Physik abgestÃ¼rzt.",
                ExplanationEn = "Faster HDT-SMP crashed while calculating hair, cloak, or armor physics.",
                StepsDe = new() { "Aktualisiere Faster HDT-SMP auf die neueste Version f\u00FCr dein Skyrim AE." },
                StepsEn = new() { "Update Faster HDT-SMP to the latest build for your Skyrim AE." },
                CanAutoPatch = false
            },
            new CrashRuleEntry
            {
                RuleId = "ANIMATION_HKX_BEHAVIOR",
                IssueType = "CTD (Animation / Behavior .hkx)",
                Triggers = new() { "hkbBehaviorGraph", "BShkbAnimationGraph", "OpenAnimationReplacer.dll", "DynamicAnimationReplacer.dll", ".hkx" },
                DefaultCulprit = "Animation Graph / Pandora / Nemesis",
                ExplanationDe = "Absturz im Animations-System (hkbBehaviorGraph / .hkx).",
                ExplanationEn = "Crash in the animation system (hkbBehaviorGraph / .hkx).",
                StepsDe = new() { "Starte links im Tool-Hub 'Pandora / Nemesis Engine' und generiere die Behavior-Dateien neu." },
                StepsEn = new() { "Launch 'Pandora / Nemesis Engine' from the left Tool Hub and regenerate behavior files." },
                CanAutoPatch = false
            },
            new CrashRuleEntry
            {
                RuleId = "NAVMESH_PATHING",
                IssueType = "CTD (NavMesh / AI Pathing)",
                Triggers = new() { "NavMesh", "PathingCell", "BSPathing", "MovementPathing" },
                DefaultCulprit = "NavMesh Conflict (.esp)",
                ExplanationDe = "Ein NPC versucht Ã¼ber ein gelÃ¶schtes oder kaputtes NavMesh (Wegfindung) zu laufen.",
                ExplanationEn = "An NPC is trying to walk across a deleted or broken NavMesh (pathfinding).",
                StepsDe = new() { "Pr\u00FCfe die im Crash-Log genannte Zelle (Cell) in SSEEdit auf 'Deleted NavMeshes'." },
                StepsEn = new() { "Check the Cell mentioned in the crash log in SSEEdit for 'Deleted NavMeshes'." },
                CanAutoPatch = false
            },
            new CrashRuleEntry
            {
                RuleId = "NPC_FACEGEN_HEADPART",
                IssueType = "CTD / Bug (NPC FaceGen / BGSHeadPart)",
                Triggers = new() { "BGSHeadPart", "BSFaceGenNiNode", "BSFaceGenModel", "facegendata" },
                DefaultCulprit = "NPC Overhaul / FaceGen Conflict",
                ExplanationDe = "Konflikt oder Absturz beim Laden eines NPC-Gesichts (BSFaceGenNiNode / BGSHeadPart).",
                ExplanationEn = "Conflict or crash while loading an NPC face (BSFaceGenNiNode / BGSHeadPart).",
                StepsDe = new() { "Klicke auf 'Auto-Fix in AutoDiagPatch.esp (Mutagen)', um alle NPC-HeadParts automatisch abzugleichen." },
                StepsEn = new() { "Click 'Auto-Fix in AutoDiagPatch.esp (Mutagen)' to automatically sync all NPC HeadParts." },
                CanAutoPatch = true
            },
            new CrashRuleEntry
            {
                RuleId = "GPU_DRIVER_VRAM_PAGEFILE",
                IssueType = "CTD (GPU Driver / VRAM / Pagefile)",
                Triggers = new() { "nvwgf2umx.dll", "amdxx64.dll", "d3d11.dll", "DXGI_ERROR_DEVICE_REMOVED" },
                DefaultCulprit = "Graphics Driver / ENB / VRAM",
                ExplanationDe = "Grafikkartentreiber-Reset oder voller VRAM / zu kleine Windows-Auslagerungsdatei.",
                ExplanationEn = "GPU driver reset or exhausted VRAM / Windows pagefile.",
                StepsDe = new() { "Stelle die Windows-Auslagerungsdatei (Pagefile) fest auf mindestens 20.000 MB (20 GB) auf deiner SSD ein." },
                StepsEn = new() { "Set your Windows Pagefile to a fixed minimum of 20,000 MB (20 GB) on your SSD." },
                CanAutoPatch = false
            },
            new CrashRuleEntry
            {
                RuleId = "MO2_VFS_NOT_HOOKED",
                IssueType = "Config Warning (Mod Organizer 2 VFS)",
                Triggers = new() { "MOD ORGANIZER 2 HINWEIS", "MOD ORGANIZER 2 NOTICE" },
                DefaultCulprit = "Mod Organizer 2 (USVFS)",
                ExplanationDe = "Das Programm wurde auÃŸerhalb von Mod Organizer 2 gestartet und sieht daher das virtuelle Dateisystem (USVFS) nicht.",
                ExplanationEn = "The tool was launched outside of Mod Organizer 2 and cannot see the virtual file system (USVFS).",
                StepsDe = new() { "FÃ¼ge 'SkyrimAutoDiag.exe' in Mod Organizer 2 unter 'Executables' hinzu und starte es direkt Ã¼ber MO2." },
                StepsEn = new() { "Add 'SkyrimAutoDiag.exe' as an Executable inside Mod Organizer 2 and launch it through MO2." },
                CanAutoPatch = false
            }
        };

        public string BuildAnonymizedDiscordMarkdown(DiagnosticReport? report, string statusSummary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("### ðŸ›¡ï¸ Skyrim AE Diagnostics Suite â€“ Report");
            sb.AppendLine($"**Date:** `{DateTime.Now:yyyy-MM-dd HH:mm}`");
            sb.AppendLine();

            if (report != null)
            {
                sb.AppendLine($"**Summary:** {report.Summary}");
                sb.AppendLine();
                foreach (var issue in report.DetectedIssues)
                {
                    sb.AppendLine($"- **[{issue.IssueType}]** | FormID: `{issue.AffectedFormId ?? "N/A"}` | Source: `{issue.CausingPlugin ?? "N/A"}`");
                    sb.AppendLine($"  - *{issue.Explanation}*");
                    foreach (var step in issue.StepByStepSolution.Take(3))
                        sb.AppendLine($"  - -> {step}");
                }
            }
            else
            {
                sb.AppendLine("```text");
                sb.AppendLine(statusSummary);
                sb.AppendLine("```");
            }

            string raw = sb.ToString();
            string userName = Environment.UserName;
            if (!string.IsNullOrWhiteSpace(userName))
                raw = Regex.Replace(raw, Regex.Escape(userName), "<User>", RegexOptions.IgnoreCase);
            return raw;
        }

        private string? ExtractFirstFormId(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"\b([0-9A-Fa-f]{8})\b");
            return m.Success ? m.Groups[1].Value : null;
        }

        private string? ExtractTopPluginOrDll(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"([a-zA-Z0-9_\-\.]+\.(?:dll|esp|esm|esl))", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
