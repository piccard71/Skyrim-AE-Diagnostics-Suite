using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace SkyrimAutoDiag
{
    public class BackupAndReportService
    {
        private readonly ToolWorkflowGuideService _workflowGuide = new();

        public string BackupFolderPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

        public string ReportsFolderPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Berichte");

        public void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(BackupFolderPath))
                Directory.CreateDirectory(BackupFolderPath);

            if (!Directory.Exists(ReportsFolderPath))
                Directory.CreateDirectory(ReportsFolderPath);
        }

        public string CreatePreLaunchBackupZip(string skyrimRootPath)
        {
            EnsureDirectoriesExist();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string zipFileName = $"SkyrimBackup_{timestamp}.zip";
            string zipFullPath = Path.Combine(BackupFolderPath, zipFileName);

            string docsMyGames = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "Skyrim Special Edition");

            string localAppDataSkyrim = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Skyrim Special Edition");

            using (var zipStream = new FileStream(zipFullPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                AddFileIfExists(archive, Path.Combine(docsMyGames, "Skyrim.ini"), "INIs/Skyrim.ini");
                AddFileIfExists(archive, Path.Combine(docsMyGames, "SkyrimPrefs.ini"), "INIs/SkyrimPrefs.ini");
                AddFileIfExists(archive, Path.Combine(docsMyGames, "SkyrimCustom.ini"), "INIs/SkyrimCustom.ini");

                AddFileIfExists(archive, Path.Combine(localAppDataSkyrim, "plugins.txt"), "LoadOrder/plugins.txt");
                AddFileIfExists(archive, Path.Combine(localAppDataSkyrim, "loadorder.txt"), "LoadOrder/loadorder.txt");

                string dataDir = Path.Combine(skyrimRootPath, "Data");
                AddFileIfExists(archive, Path.Combine(dataDir, "AutoDiagPatch.esp"), "Data/AutoDiagPatch.esp");
                AddFileIfExists(archive, Path.Combine(dataDir, "AutoDiag_DISTR.ini"), "Data/AutoDiag_DISTR.ini");

                string savesDir = Path.Combine(docsMyGames, "Saves");
                if (Directory.Exists(savesDir))
                {
                    var latestWorkingSave = FindLatestWorkingSavegame(savesDir);
                    if (latestWorkingSave != null)
                    {
                        AddFileIfExists(archive, latestWorkingSave.FullName, $"Savegame/{latestWorkingSave.Name}");

                        string sksePath = Path.ChangeExtension(latestWorkingSave.FullName, ".skse");
                        if (File.Exists(sksePath))
                        {
                            AddFileIfExists(archive, sksePath, $"Savegame/{Path.GetFileName(sksePath)}");
                        }
                    }
                }
            }

            // Automatische Backup-Rotation: Nur die 15 neuesten ZIP-Archive behalten
            try
            {
                var oldZips = new DirectoryInfo(BackupFolderPath)
                    .GetFiles("SkyrimBackup_*.zip")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Skip(15)
                    .ToList();

                foreach (var oldFile in oldZips)
                    oldFile.Delete();
            }
            catch { }

            return zipFileName;
        }

        private FileInfo? FindLatestWorkingSavegame(string savesDir)
        {
            var essFiles = new DirectoryInfo(savesDir)
                .GetFiles("*.ess")
                .Where(f => f.Length > 0)
                .OrderByDescending(f => f.LastWriteTime);

            foreach (var ess in essFiles)
            {
                string skseFile = Path.ChangeExtension(ess.FullName, ".skse");
                if (File.Exists(skseFile) && new FileInfo(skseFile).Length == 0)
                    continue;
                return ess;
            }
            return null;
        }

        private void AddFileIfExists(ZipArchive archive, string sourceFilePath, string entryName)
        {
            if (!File.Exists(sourceFilePath)) return;

            try
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = new FileStream(
                    sourceFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                fileStream.CopyTo(entryStream);
            }
            catch { }
        }

        public (string DePath, string EnPath) GenerateBilingualHtmlReports(
            DiagnosticReport report,
            string skseAndPapyrusSummary,
            List<string> screenshotPaths)
        {
            EnsureDirectoriesExist();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string deFilePath = Path.Combine(ReportsFolderPath, $"DiagnoseBericht_DE_{timestamp}.html");
            string enFilePath = Path.Combine(ReportsFolderPath, $"DiagnosticReport_EN_{timestamp}.html");

            string htmlDe = BuildHtmlContent(report, skseAndPapyrusSummary, screenshotPaths, isGerman: true);
            string htmlEn = BuildHtmlContent(report, skseAndPapyrusSummary, screenshotPaths, isGerman: false);

            File.WriteAllText(deFilePath, htmlDe, Encoding.UTF8);
            File.WriteAllText(enFilePath, htmlEn, Encoding.UTF8);

            return (deFilePath, enFilePath);
        }

        private string BuildHtmlContent(
            DiagnosticReport report,
            string skseSummary,
            List<string> screenshotPaths,
            bool isGerman)
        {
            string title = isGerman
                ? "Skyrim AE – KI- & Offline-Fehleranalyse & L\u00F6sungsbericht"
                : "Skyrim AE – AI & Offline Diagnostic & Solution Report";

            string subtitle = isGerman
                ? $"Erstellt am {DateTime.Now:dd.MM.yyyy um HH:mm:ss} Uhr"
                : $"Generated on {DateTime.Now:yyyy-MM-dd at HH:mm:ss}";

            string summaryTitle = isGerman ? "Zusammenfassung der Sitzung" : "Session Summary";
            string summaryText = isGerman ? report.SummaryDe : report.SummaryEn;
            if (string.IsNullOrWhiteSpace(summaryText)) summaryText = report.Summary;

            string issuesHeader = isGerman
                ? $"Erkannte Probleme & Schritt-für-Schritt-L\u00F6sungen ({report.DetectedIssues.Count})"
                : $"Detected Issues & Step-by-Step Solutions ({report.DetectedIssues.Count})";

            string envHeader = isGerman
                ? "Vorab-Scan, SKSE & Papyrus-Protokoll"
                : "Pre-Flight Scan, SKSE & Papyrus Log";

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine($"<html lang=\"{(isGerman ? "de" : "en")}\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine($"<title>{WebUtility.HtmlEncode(title)}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { background-color: #121318; color: #E2E8F0; font-family: 'Segoe UI', Tahoma, sans-serif; margin: 0; padding: 30px; }");
            sb.AppendLine(".container { max-width: 1050px; margin: 0 auto; }");
            sb.AppendLine(".header { background: #1B1D24; padding: 24px; border-radius: 10px; border-left: 5px solid #3B82F6; margin-bottom: 24px; }");
            sb.AppendLine("h1 { margin: 0 0 8px 0; color: #F8FAFC; font-size: 24px; }");
            sb.AppendLine("h2 { color: #60A5FA; border-bottom: 1px solid #2D313E; padding-bottom: 8px; margin-top: 28px; }");
            sb.AppendLine(".card { background: #1B1D24; border: 1px solid #2D313E; border-radius: 8px; padding: 20px; margin-bottom: 18px; }");
            sb.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 4px; font-weight: bold; font-size: 12px; margin-right: 8px; background: #DC2626; color: white; }");
            sb.AppendLine(".badge-auto { background: #059669; }");
            sb.AppendLine(".meta { color: #94A3B8; font-size: 14px; margin: 8px 0; }");
            sb.AppendLine("pre { background: #0F1014; padding: 14px; border-radius: 6px; overflow-x: auto; color: #CBD5E1; border: 1px solid #262936; }");
            sb.AppendLine("ol { padding-left: 20px; line-height: 1.6; }");
            sb.AppendLine(".shot { max-width: 100%; border-radius: 6px; margin-top: 12px; border: 1px solid #334155; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body><div class=\"container\">");

            sb.AppendLine($"<div class=\"header\"><h1>{WebUtility.HtmlEncode(title)}</h1>");
            sb.AppendLine($"<div class=\"meta\">{WebUtility.HtmlEncode(subtitle)}</div></div>");

            sb.AppendLine($"<h2>{WebUtility.HtmlEncode(summaryTitle)}</h2>");
            sb.AppendLine($"<div class=\"card\"><p>{WebUtility.HtmlEncode(summaryText)}</p></div>");

            sb.AppendLine($"<h2>{WebUtility.HtmlEncode(issuesHeader)}</h2>");

            if (report.DetectedIssues.Count == 0)
            {
                string noIssues = isGerman
                    ? "Keine kritischen Fehler oder Abstürze in dieser Sitzung erkannt."
                    : "No critical issues or crashes detected in this session.";
                sb.AppendLine($"<div class=\"card\"><p>{WebUtility.HtmlEncode(noIssues)}</p></div>");
            }
            else
            {
                foreach (var issue in report.DetectedIssues)
                {
                    string explanation = isGerman ? issue.ExplanationDe : issue.ExplanationEn;
                    var steps = isGerman ? issue.StepByStepSolutionDe : issue.StepByStepSolutionEn;

                    sb.AppendLine("<div class=\"card\">");
                    sb.AppendLine($"<span class=\"badge\">{WebUtility.HtmlEncode(issue.IssueType)}</span>");
                    if (issue.CanAutoPatchWithMutagen)
                    {
                        string autoTxt = isGerman ? "Auto-Fix via Mutagen möglich" : "Mutagen Auto-Fix Available";
                        sb.AppendLine($"<span class=\"badge badge-auto\">{autoTxt}</span>");
                    }

                    sb.AppendLine($"<div class=\"meta\"><strong>FormID:</strong> {WebUtility.HtmlEncode(issue.AffectedFormId ?? "N/A")} | ");
                    sb.AppendLine($"<strong>Plugin/DLL:</strong> {WebUtility.HtmlEncode(issue.CausingPlugin ?? "N/A")}</div>");
                    sb.AppendLine($"<p>{WebUtility.HtmlEncode(explanation)}</p>");

                    if (steps != null && steps.Count > 0)
                    {
                        string stepTitle = isGerman ? "Schritt-für-Schritt-L\u00F6sung:" : "Step-by-Step Solution:";
                        sb.AppendLine($"<strong>{stepTitle}</strong><ol>");
                        foreach (var s in steps)
                        {
                            sb.AppendLine($"<li>{WebUtility.HtmlEncode(s)}</li>");
                        }
                        sb.AppendLine("</ol>");
                    }

                    sb.AppendLine(_workflowGuide.BuildInlineWorkflowHtmlBlock(issue, isGerman));
                    sb.AppendLine("</div>");
                }
            }

            sb.AppendLine($"<h2>{WebUtility.HtmlEncode(envHeader)}</h2>");
            sb.AppendLine($"<pre>{WebUtility.HtmlEncode(skseSummary)}</pre>");

            if (screenshotPaths.Count > 0)
            {
                string shotHeader = isGerman ? "Aufgenommene In-Game-Screenshots" : "Captured In-Game Screenshots";
                sb.AppendLine($"<h2>{WebUtility.HtmlEncode(shotHeader)}</h2>");
                foreach (var path in screenshotPaths)
                {
                    if (!File.Exists(path)) continue;
                    byte[] bytes = File.ReadAllBytes(path);
                    string b64 = Convert.ToBase64String(bytes);
                    sb.AppendLine($"<div class=\"card\"><div class=\"meta\">{WebUtility.HtmlEncode(Path.GetFileName(path))}</div>");
                    sb.AppendLine($"<img class=\"shot\" src=\"data:image/png;base64,{b64}\" /></div>");
                }
            }

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }
    }
}