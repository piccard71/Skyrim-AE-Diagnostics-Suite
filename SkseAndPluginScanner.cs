using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace SkyrimAutoDiag
{
    public class SkseScanResult
    {
        public bool IsHealthy => Warnings.Count == 0 && Errors.Count == 0;
        public string SkyrimExeVersion { get; set; } = "Unknown";
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        public string ToFormattedSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SkyrimSE.exe Version: {SkyrimExeVersion}");
            foreach (var err in Errors) sb.AppendLine($"[ERROR] {err}");
            foreach (var warn in Warnings) sb.AppendLine($"[WARN] {warn}");
            if (IsHealthy) sb.AppendLine("[OK] SKSE, VFS, ESL-Header & Savegame Check passed.");
            return sb.ToString();
        }
    }

    public class SkseAndPluginScanner
    {
        private static readonly Dictionary<string, (string De, string En)> _knownDllIssues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["NetScriptFramework.Runtime.dll"] = (
                ".NET Script Framework ist inkompatibel mit Skyrim AE (1.6.x). Nutze 'Crash Logger SSE AE VR'.",
                ".NET Script Framework is incompatible with Skyrim AE (1.6.x). Use 'Crash Logger SSE AE VR'."
            ),
            ["DynamicAnimationReplacer.dll"] = (
                "DAR ist veraltet auf AE 1.6.1130+. Empfehlung: 'Open Animation Replacer (OAR)'.",
                "DAR is outdated on AE 1.6.1130+. Recommendation: 'Open Animation Replacer (OAR)'."
            ),
            ["trainwreck.dll"] = (
                "Trainwreck erkannt. Nicht zusammen mit 'CrashLogger.dll' nutzen.",
                "Trainwreck detected. Do not run alongside 'CrashLogger.dll'."
            )
        };

        public SkseScanResult ScanEnvironment(string skyrimRootPath, string skseLogPath)
        {
            var result = new SkseScanResult();
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;

            string exePath = Path.Combine(skyrimRootPath, "SkyrimSE.exe");
            if (File.Exists(exePath))
            {
                var v = FileVersionInfo.GetVersionInfo(exePath);
                result.SkyrimExeVersion = $"{v.FileMajorPart}.{v.FileMinorPart}.{v.FileBuildPart}.{v.FilePrivatePart}";
            }

            // Mod Organizer 2 (USVFS) Check
            CheckModOrganizerVfsState(skyrimRootPath, result, isDe);

            string sksePluginsDir = Path.Combine(skyrimRootPath, "Data", "SKSE", "Plugins");
            if (Directory.Exists(sksePluginsDir))
            {
                if (result.SkyrimExeVersion != "Unknown")
                {
                    // Zukunftssicherer Doppelcheck für Steam (versionlib-*) und GOG (version-*)
                    string verDash = result.SkyrimExeVersion.Replace('.', '-');
                    string expectedSteamBin = Path.Combine(sksePluginsDir, $"versionlib-{verDash}.bin");
                    string expectedGogBin   = Path.Combine(sksePluginsDir, $"version-{verDash}.bin");

                    if (!File.Exists(expectedSteamBin) && !File.Exists(expectedGogBin))
                    {
                        result.Errors.Add(isDe
                            ? $"FEHLENDE ADDRESS LIBRARY: F\u00FCr Skyrim {result.SkyrimExeVersion} fehlt 'versionlib-{verDash}.bin' (Steam) bzw. 'version-{verDash}.bin' (GOG) in Data\\SKSE\\Plugins!"
                            : $"MISSING ADDRESS LIBRARY: Missing 'versionlib-{verDash}.bin' (Steam) or 'version-{verDash}.bin' (GOG) in Data\\SKSE\\Plugins for Skyrim {result.SkyrimExeVersion}!");
                    }
                }

                foreach (var dllPath in Directory.GetFiles(sksePluginsDir, "*.dll"))
                {
                    string fileName = Path.GetFileName(dllPath);
                    if (_knownDllIssues.TryGetValue(fileName, out var msg))
                        result.Warnings.Add($"{fileName}: {(isDe ? msg.De : msg.En)}");

                    if (!Is64BitDll(dllPath))
                        result.Errors.Add($"32-Bit Oldrim DLL: {fileName}");
                }
            }

            if (File.Exists(skseLogPath))
            {
                foreach (var line in File.ReadAllLines(skseLogPath))
                {
                    if (line.Contains("reported as incompatible", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("disabled, fatal error", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("couldn't load plugin", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add($"SKSE Log: {line.Trim()}");
                    }
                }
            }

            CheckEslHeaderVersions(result, isDe);
            CheckLatestSavegame(skyrimRootPath, result, isDe);

            return result;
        }

        private void CheckModOrganizerVfsState(string skyrimRootPath, SkseScanResult result, bool isDe)
        {
            try
            {
                string dataDir = Path.Combine(skyrimRootPath, "Data");
                if (!Directory.Exists(dataDir)) return;

                int espCount = Directory.GetFiles(dataDir, "*.esp").Length;
                bool mo2Running = Process.GetProcessesByName("ModOrganizer").Length > 0;

                bool usvfsHooked = false;
                foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
                {
                    if (mod.ModuleName != null &&
                        mod.ModuleName.Contains("usvfs", StringComparison.OrdinalIgnoreCase))
                    {
                        usvfsHooked = true;
                        break;
                    }
                }

                if ((mo2Running || espCount <= 1) && !usvfsHooked)
                {
                    result.Warnings.Add(isDe
                        ? "MOD ORGANIZER 2 HINWEIS: Es wurden kaum Mods im Data-Ordner gefunden (kein USVFS-Hook aktiv). Falls du Mod Organizer 2 nutzt, f\u00FCge SkyrimAutoDiag.exe bitte als ausf\u00FChrbare Datei in MO2 hinzu und starte es von dort!"
                        : "MOD ORGANIZER 2 NOTICE: Very few plugins found in Data folder (no active USVFS hook). If you use Mod Organizer 2, please add SkyrimAutoDiag.exe as an executable inside MO2 and launch it from there!");
                }
            }
            catch { }
        }

        private void CheckEslHeaderVersions(SkseScanResult result, bool isDe)
        {
            try
            {
                using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
                foreach (var listing in env.LoadOrder.PriorityOrder)
                {
                    var mod = listing.Mod;
                    if (mod == null) continue;

                    bool isEsl = mod.ModKey.Type == ModType.Light ||
                                 mod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Small);
                    if (!isEsl) continue;

                    float ver = mod.ModHeader.Stats.Version;
                    if (ver < 1.71f)
                    {
                        bool hasLowFormIds = mod.EnumerateMajorRecords().Any(r =>
                            r.FormKey.ModKey == mod.ModKey && r.FormKey.ID < 0x800);

                        if (hasLowFormIds)
                        {
                            result.Errors.Add(isDe
                                ? $"ESL-HEADER BUG (1.6.1130+): '{mod.ModKey.FileName}' nutzt FormIDs unter 0x800, hat aber Header-Version {ver:F2} (ben\u00F6tigt mindestens 1.71)!"
                                : $"ESL HEADER BUG (1.6.1130+): '{mod.ModKey.FileName}' uses FormIDs below 0x800 with header {ver:F2} (requires at least 1.71)!");
                        }
                    }
                }
            }
            catch { }
        }

        private void CheckLatestSavegame(string skyrimRootPath, SkseScanResult result, bool isDe)
        {
            try
            {
                string savesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Games", "Skyrim Special Edition", "Saves");
                if (!Directory.Exists(savesDir)) return;

                var latestEss = new DirectoryInfo(savesDir)
                    .GetFiles("*.ess")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                if (latestEss == null) return;

                string skseCoSave = Path.ChangeExtension(latestEss.FullName, ".skse");
                if (File.Exists(skseCoSave) && new FileInfo(skseCoSave).Length == 0)
                {
                    result.Errors.Add(isDe
                        ? $"KORRUPTES CO-SAVE: '{Path.GetFileName(skseCoSave)}' ist 0 Bytes gro\u00DF!"
                        : $"CORRUPT CO-SAVE: '{Path.GetFileName(skseCoSave)}' is 0 bytes!");
                }

                string dataDir = Path.Combine(skyrimRootPath, "Data");
                if (!Directory.Exists(dataDir)) return;

                byte[] headerBytes = new byte[Math.Min(65536, (int)latestEss.Length)];
                using (var fs = new FileStream(latestEss.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Read(headerBytes, 0, headerBytes.Length);
                }

                string ascii = Encoding.UTF8.GetString(headerBytes);
                var matches = Regex.Matches(ascii, @"[a-zA-Z0-9_\-\s\.]+\.(?:esp|esm|esl)", RegexOptions.IgnoreCase);
                var checkedMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (Match m in matches)
                {
                    string modName = m.Value.Trim();
                    if (modName.Length < 5 || !checkedMods.Add(modName)) continue;

                    if (!File.Exists(Path.Combine(dataDir, modName)))
                    {
                        result.Warnings.Add(isDe
                            ? $"SAVEGAME WARNUNG ({latestEss.Name}): Plugin '{modName}' war im Save aktiv, fehlt aber im Data-Ordner!"
                            : $"SAVEGAME WARNING ({latestEss.Name}): Plugin '{modName}' was active in save, but is missing in Data folder!");
                    }
                }
            }
            catch { }
        }

        public string AnalyzePapyrusLog()
        {
            string papyrusPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "Skyrim Special Edition", "Logs", "Script", "Papyrus.0.log");

            if (!File.Exists(papyrusPath))
                return "Papyrus.0.log not found (Papyrus logging disabled or no errors).";

            try
            {
                var allLines = File.ReadAllLines(papyrusPath);
                int stackDumps = allLines.Count(l =>
                    l.Contains("Suspended stack count is over our warning threshold", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("Dumping Stacks", StringComparison.OrdinalIgnoreCase));

                var errorCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in allLines)
                {
                    if (line.Contains("error:", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Cannot call", StringComparison.OrdinalIgnoreCase))
                    {
                        string cleaned = Regex.Replace(line, @"^\[.*?\]\s*", "").Trim();
                        errorCounts[cleaned] = errorCounts.GetValueOrDefault(cleaned) + 1;
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Papyrus Stack Dumps detected: {stackDumps}");
                foreach (var kvp in errorCounts.OrderByDescending(k => k.Value).Take(5))
                {
                    sb.AppendLine($"  [{kvp.Value}x] {kvp.Key}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Papyrus Log Error: {ex.Message}";
            }
        }

        private bool Is64BitDll(string dllPath)
        {
            try
            {
                using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var br = new BinaryReader(fs);
                if (br.ReadUInt16() != 0x5A4D) return true;
                fs.Seek(0x3C, SeekOrigin.Begin);
                int peOffset = br.ReadInt32();
                fs.Seek(peOffset + 4, SeekOrigin.Begin);
                return br.ReadUInt16() == 0x8664;
            }
            catch { return true; }
        }
    }
}