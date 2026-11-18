using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace SkyrimAutoDiag
{
    public class ManualSectionItem : INotifyPropertyChanged
    {
        public string CategoryBadge { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string ProTip { get; set; } = string.Empty;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public AppConfig Config { get; } = AppConfig.LoadOrCreate();
        public ObservableCollection<DiagnosedIssue> DetectedIssues { get; } = new();
        public ObservableCollection<ManualSectionItem> FilteredManualSections { get; } = new();
        public ObservableCollection<WebSearchResultItem> WebSearchResults { get; } = new();
        public IReadOnlyList<string> AvailableGeminiModels => GeminiDiagnosticsService.AvailableModels;

        public event Action? HotkeyChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly List<string> _shots = new();
        private readonly List<ManualSectionItem> _allManualSections = new();
        private readonly SkseAndPluginScanner _scanner = new();
        private readonly CompatibleSkyrimPatcher _patcher = new();
        private readonly BackupAndReportService _backupAndReport = new();
        private readonly NexusFeaturesService _nexus = new();
        private readonly ToolWorkflowGuideService _workflowGuide = new();
        private readonly WebSolutionSearchService _webSearch = new();
        private DiagnosticReport? _lastReport;

        public ICommand LaunchSkyrimCommand { get; }
        public ICommand BuildCompatiblePatchCommand { get; }
        public ICommand RunPreFlightScanCommand { get; }
        public ICommand FixEslHeadersCommand { get; }
        public ICommand AutoDetectPathsCommand { get; }
        public ICommand CopyDiscordReportCommand { get; }
        public ICommand LaunchToolCommand { get; }
        public ICommand OpenInSseEditCommand { get; }
        public ICommand OpenWorkflowGuideCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand BrowsePathCommand { get; }
        public ICommand TestGeminiApiCommand { get; }
        public ICommand OpenGeminiStudioCommand { get; }
        public ICommand ExpandAllManualCommand { get; }
        public ICommand CollapseAllManualCommand { get; }
        public ICommand ResetManualSearchCommand { get; }
        public ICommand ExecuteWebSearchCommand { get; }
        public ICommand SaveWebSearchHtmlCommand { get; }
        public ICommand OpenWebUrlCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public MainViewModel()
        {
            _backupAndReport.EnsureDirectoriesExist();

            LaunchSkyrimCommand = new RelayCommand(async _ => await LaunchSkyrimAsync());
            BuildCompatiblePatchCommand = new RelayCommand(async _ => await BuildPatchAsync());
            RunPreFlightScanCommand = new RelayCommand(async _ => await PreFlightScanAsync());
            FixEslHeadersCommand = new RelayCommand(async _ => await FixEslHeadersAsync());
            AutoDetectPathsCommand = new RelayCommand(_ => AutoDetectPaths());
            CopyDiscordReportCommand = new RelayCommand(_ => CopyDiscordReport());
            LaunchToolCommand = new RelayCommand(p => LaunchTool(p as string));
            OpenInSseEditCommand = new RelayCommand(p => OpenSseEdit(p as string));
            OpenWorkflowGuideCommand = new RelayCommand(p => OpenWorkflowGuide(p as DiagnosedIssue));
            SaveConfigCommand = new RelayCommand(_ => SaveConfig());
            BrowsePathCommand = new RelayCommand(p => BrowsePath(p as string));
            TestGeminiApiCommand = new RelayCommand(async _ => await TestGeminiApiAsync());
            OpenGeminiStudioCommand = new RelayCommand(_ => OpenGeminiStudio());

            ExpandAllManualCommand = new RelayCommand(_ => SetAllManualExpanded(true));
            CollapseAllManualCommand = new RelayCommand(_ => SetAllManualExpanded(false));
            ResetManualSearchCommand = new RelayCommand(_ => ManualSearchQuery = string.Empty);

            ExecuteWebSearchCommand = new RelayCommand(async _ => await ExecuteWebSearchAsync());
            SaveWebSearchHtmlCommand = new RelayCommand(_ => SaveWebSearchHtml());
            OpenWebUrlCommand = new RelayCommand(p => OpenWebUrl(p as string));
            OpenFolderCommand = new RelayCommand(p => OpenFolder(p as string));

            RebuildManualSections();
        }

        public string GeminiApiKey { get => Config.GeminiApiKey; set { Config.GeminiApiKey = value; Notify(); } }
        public string SelectedGeminiModel { get => Config.SelectedGeminiModel; set { Config.SelectedGeminiModel = value; Notify(); } }
        public bool UseOfflineModeOnly { get => Config.UseOfflineModeOnly; set { Config.UseOfflineModeOnly = value; Notify(); } }
        public bool GenerateSpidIni { get => Config.GenerateSpidIni; set { Config.GenerateSpidIni = value; Notify(); } }
        public string SkyrimRootPath { get => Config.SkyrimRootPath; set { Config.SkyrimRootPath = value; Notify(); } }
        public string SseEditExePath { get => Config.SseEditExePath; set { Config.SseEditExePath = value; Notify(); } }
        public string SseEditQuickCleanExePath { get => Config.SseEditQuickCleanExePath; set { Config.SseEditQuickCleanExePath = value; Notify(); } }
        public string WryeBashExePath { get => Config.WryeBashExePath; set { Config.WryeBashExePath = value; Notify(); } }
        public string LootExePath { get => Config.LootExePath; set { Config.LootExePath = value; Notify(); } }
        public string SynthesisExePath { get => Config.SynthesisExePath; set { Config.SynthesisExePath = value; Notify(); } }
        public string PandoraExePath { get => Config.PandoraExePath; set { Config.PandoraExePath = value; Notify(); } }
        public string BodySlideExePath { get => Config.BodySlideExePath; set { Config.BodySlideExePath = value; Notify(); } }
        public string BethIniExePath { get => Config.BethIniExePath; set { Config.BethIniExePath = value; Notify(); } }

        public string ScreenshotHotkey
        {
            get => Config.ScreenshotHotkey;
            set { Config.ScreenshotHotkey = value; Notify(); Notify(nameof(HotkeyHintText)); HotkeyChanged?.Invoke(); }
        }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex { get => _selectedTabIndex; set { _selectedTabIndex = value; Notify(); } }

        private int _reportProgressValue = 0;
        public int ReportProgressValue { get => _reportProgressValue; set { _reportProgressValue = value; Notify(); } }

        private string _reportProgressStatusText = LocalizationService.Get("Progress_Idle");
        public string ReportProgressStatusText { get => _reportProgressStatusText; set { _reportProgressStatusText = value; Notify(); } }

        private string _manualSearchQuery = string.Empty;
        public string ManualSearchQuery
        {
            get => _manualSearchQuery;
            set { _manualSearchQuery = value; Notify(); ApplyManualFilter(); }
        }

        private string _webSearchQuery = string.Empty;
        public string WebSearchQuery
        {
            get => _webSearchQuery;
            set { _webSearchQuery = value; Notify(); }
        }

        private string _webSearchStatusText = string.Empty;
        public string WebSearchStatusText
        {
            get => _webSearchStatusText;
            set { _webSearchStatusText = value; Notify(); }
        }

        private int _langIndex;
        public int SelectedLanguageIndex
        {
            get => _langIndex;
            set
            {
                _langIndex = value;
                LocalizationService.CurrentLanguage = value == 0 ? AppLanguage.German : AppLanguage.English;
                if (_lastReport != null)
                {
                    ReportSummary = _lastReport.Summary;
                    var copy = _lastReport.DetectedIssues.ToList();
                    DetectedIssues.Clear();
                    copy.ForEach(i => DetectedIssues.Add(i));
                }
                if (ReportProgressValue == 0)
                    ReportProgressStatusText = LocalizationService.Get("Progress_Idle");
                RebuildManualSections();
                Notify(string.Empty);
            }
        }

        public string LaunchBtnText => LocalizationService.Get("Btn_Launch");
        public string BuildPatchBtnText => LocalizationService.Get("Btn_BuildPatch");
        public string ScanSkseBtnText => LocalizationService.Get("Btn_ScanSkse");
        public string FixEslBtnText => LocalizationService.Get("Btn_FixEslHeaders");
        public string AutoDetectBtnText => LocalizationService.Get("Btn_AutoDetectPaths");
        public string CopyDiscordBtnText => LocalizationService.Get("Btn_CopyDiscord");
        public string SaveConfigBtnText => LocalizationService.Get("Btn_SaveConfig");
        public string TestApiBtnText => LocalizationService.Get("Btn_TestApi");
        public string OpenStudioBtnText => LocalizationService.Get("Btn_OpenStudio");
        public string OfflineModeChkText => LocalizationService.Get("Chk_OfflineMode");
        public string SpidExportChkText => LocalizationService.Get("Chk_SpidExport");
        public string ModelSelectLabelText => LocalizationService.Get("Lbl_ModelSelect");
        public string ReportProgressLabelText => LocalizationService.Get("Lbl_ReportProgress");

        public string TabOverviewText => LocalizationService.Get("Tab_Overview");
        public string TabDiagnosticsText => LocalizationService.Get("Tab_Diagnostics");
        public string TabSettingsText => LocalizationService.Get("Tab_Settings");
        public string TabManualText => LocalizationService.Get("Tab_Manual");
        public string TabWebSearchText => LocalizationService.Get("Tab_WebSearch");

        public string ToolsHeaderText => LocalizationService.Get("Header_Tools");
        public string ReportHeaderText => LocalizationService.Get("Header_Report");
        public string SettingsHeaderText => LocalizationService.Get("Header_Settings");

        public string OverviewTitleText => LocalizationService.Get("Overview_Title");
        public string OverviewDescText => LocalizationService.Get("Overview_Desc");
        public string Feat1Title => LocalizationService.Get("Feat1_Title");
        public string Feat1Desc => LocalizationService.Get("Feat1_Desc");
        public string Feat2Title => LocalizationService.Get("Feat2_Title");
        public string Feat2Desc => LocalizationService.Get("Feat2_Desc");
        public string Feat3Title => LocalizationService.Get("Feat3_Title");
        public string Feat3Desc => LocalizationService.Get("Feat3_Desc");
        public string Feat4Title => LocalizationService.Get("Feat4_Title");
        public string Feat4Desc => LocalizationService.Get("Feat4_Desc");
        public string Feat5Title => LocalizationService.Get("Feat5_Title");
        public string Feat5Desc => LocalizationService.Get("Feat5_Desc");
        public string Feat6Title => LocalizationService.Get("Feat6_Title");
        public string Feat6Desc => LocalizationService.Get("Feat6_Desc");
        public string Feat7Title => LocalizationService.Get("Feat7_Title");
        public string Feat7Desc => LocalizationService.Get("Feat7_Desc");
        public string Feat8Title => LocalizationService.Get("Feat8_Title");
        public string Feat8Desc => LocalizationService.Get("Feat8_Desc");

        public string ManualHeaderText => LocalizationService.Get("Manual_Header");
        public string ManualSearchPlaceholderText => LocalizationService.Get("Manual_SearchPlaceholder");
        public string ManualExpandAllBtnText => LocalizationService.Get("Manual_BtnExpandAll");
        public string ManualCollapseAllBtnText => LocalizationService.Get("Manual_BtnCollapseAll");
        public string ManualResetSearchBtnText => LocalizationService.Get("Manual_BtnResetSearch");

        public string WebSearchHeaderText => LocalizationService.Get("WebSearch_Header");
        public string WebSearchDescText => LocalizationService.Get("WebSearch_Desc");
        public string WebSearchBtnText => LocalizationService.Get("WebSearch_BtnSearch");
        public string WebSearchSaveHtmlBtnText => LocalizationService.Get("WebSearch_BtnSaveHtml");
        public string WebSearchOpenUrlBtnText => LocalizationService.Get("WebSearch_BtnOpenUrl");

        public string SubtitleText => LocalizationService.CurrentLanguage == AppLanguage.German
            ? "Offline- & KI-Diagnose, Online-Lösungssuche, Tool-Workflows, Auto-Backup & Auto-Patcher"
            : "Offline & AI Diagnostics, Online Solution Finder, Tool Workflows, Auto-Backup & Auto-Patcher";

        public string HotkeyHintText => LocalizationService.CurrentLanguage == AppLanguage.German
            ? $"In-Game Tipp: Konsole (^) öffnen, Objekt anklicken und [{ScreenshotHotkey}] drücken."
            : $"In-Game Tip: Open console (^), click object, and press [{ScreenshotHotkey}].";

        private string _reportSummary = "Ready.";
        public string ReportSummary { get => _reportSummary; set { _reportSummary = value; Notify(); } }

        private string _status = LocalizationService.Get("Status_Ready");
        public string CurrentStatusText { get => _status; set { _status = value; Notify(); } }

        private void OpenFolder(string? target)
        {
            _backupAndReport.EnsureDirectoriesExist();
            string path = target switch
            {
                "Backup" => _backupAndReport.BackupFolderPath,
                "Reports" => _backupAndReport.ReportsFolderPath,
                "Database" => AppConfig.DatabaseFolderPath,
                _ => AppDomain.CurrentDomain.BaseDirectory
            };

            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex) { CurrentStatusText = ex.Message; }
        }

        private void OpenGeminiStudio()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://aistudio.google.com/app/apikey",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CurrentStatusText = $"Browser-Fehler: {ex.Message}";
            }
        }

        private async Task ExecuteWebSearchAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            if (string.IsNullOrWhiteSpace(WebSearchQuery))
            {
                WebSearchStatusText = isDe
                    ? "Bitte beschreibe zuerst dein Problem im Eingabefeld."
                    : "Please describe your issue in the input box first.";
                return;
            }

            WebSearchStatusText = isDe
                ? $"Suche nach Lösungen (Reddit r/skyrimmods, lokale DB & KI) für: '{WebSearchQuery}'..."
                : $"Searching for solutions (Reddit r/skyrimmods, local DB & AI) to: '{WebSearchQuery}'...";
            CurrentStatusText = WebSearchStatusText;

            var hits = await _webSearch.SearchSolutionsOnlineAsync(WebSearchQuery, Config, isDe);
            WebSearchResults.Clear();
            foreach (var h in hits)
                WebSearchResults.Add(h);

            WebSearchStatusText = isDe
                ? $"{WebSearchResults.Count} passende Lösungsansätze und Community-Beiträge gefunden."
                : $"Found {WebSearchResults.Count} matching solutions and community posts.";
            CurrentStatusText = WebSearchStatusText;
        }

        private void SaveWebSearchHtml()
        {
            if (WebSearchResults.Count == 0) return;
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            string path = _webSearch.SaveWebSearchAsHtmlReport(WebSearchQuery, WebSearchResults.ToList(), isDe);
            CurrentStatusText = isDe
                ? $"Suchergebnisse als HTML-Bericht gespeichert: Berichte\\{Path.GetFileName(path)}"
                : $"Search results saved as HTML report: Berichte\\{Path.GetFileName(path)}";
        }

        private void OpenWebUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex) { CurrentStatusText = ex.Message; }
        }

        private void RebuildManualSections()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            _allManualSections.Clear();

            var chapters = UserManualService.LoadOrCreateManual();
            bool first = true;

            foreach (var ch in chapters)
            {
                _allManualSections.Add(new ManualSectionItem
                {
                    CategoryBadge = isDe ? ch.BadgeDe : ch.BadgeEn,
                    Title = isDe ? ch.TitleDe : ch.TitleEn,
                    Summary = isDe ? ch.SummaryDe : ch.SummaryEn,
                    Steps = new List<string>(isDe ? ch.DetailsDe : ch.DetailsEn),
                    ProTip = isDe ? ch.ProTipDe : ch.ProTipEn,
                    IsExpanded = first
                });
                first = false;
            }

            ApplyManualFilter();
        }

        private void ApplyManualFilter()
        {
            FilteredManualSections.Clear();
            string q = (_manualSearchQuery ?? string.Empty).Trim();

            foreach (var sec in _allManualSections)
            {
                if (string.IsNullOrEmpty(q) ||
                    sec.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    sec.Summary.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    sec.CategoryBadge.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    sec.ProTip.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    sec.Steps.Any(s => s.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrEmpty(q))
                        sec.IsExpanded = true;
                    FilteredManualSections.Add(sec);
                }
            }
        }

        private void SetAllManualExpanded(bool expanded)
        {
            foreach (var sec in FilteredManualSections)
                sec.IsExpanded = expanded;
        }

        private void SaveConfig()
        {
            Config.Save();
            HotkeyChanged?.Invoke();
            CurrentStatusText = $"{LocalizationService.Get("Status_ConfigSaved")} [{ScreenshotHotkey}]";
        }

        private void OpenWorkflowGuide(DiagnosedIssue? issue)
        {
            if (issue == null) return;
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            string htmlPath = _workflowGuide.GenerateAndOpenStandaloneWorkflowHtml(issue, isDe);
            var wf = _workflowGuide.FindBestWorkflowForIssue(issue);

            CurrentStatusText = isDe
                ? $"Tool-Workflow ({wf.ToolDisplayName}) als HTML geöffnet: Berichte\\{Path.GetFileName(htmlPath)}"
                : $"Tool workflow ({wf.ToolDisplayName}) opened as HTML: Berichte\\{Path.GetFileName(htmlPath)}";
        }

        private void AutoDetectPaths()
        {
            int count = _nexus.AutoDetectPaths(Config);
            Notify(string.Empty);
            CurrentStatusText = LocalizationService.CurrentLanguage == AppLanguage.German
                ? $"Auto-Erkennung abgeschlossen: {count} Pfade gefunden und gespeichert."
                : $"Auto-detection completed: {count} paths found and saved.";
        }

        private async Task FixEslHeadersAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            try
            {
                ReportProgressValue = 30;
                CurrentStatusText = isDe
                    ? "Prüfe und patche veraltete ESL-Header (< 1.71) im Hintergrund..."
                    : "Scanning and upgrading outdated ESL headers (< 1.71) in background...";
                ReportProgressStatusText = CurrentStatusText;

                int fixedCount = await Task.Run(() =>
                    _nexus.UpgradeOutdatedEslHeaders(Config.SkyrimDataPath, _backupAndReport.BackupFolderPath));

                ReportProgressValue = 100;
                CurrentStatusText = isDe
                    ? $"{fixedCount} ESL-Plugins auf Header 1.71 aktualisiert (Backups im Ordner 'Backup')."
                    : $"{fixedCount} ESL plugins upgraded to header 1.71 (backups saved in 'Backup').";
                ReportProgressStatusText = CurrentStatusText;
            }
            catch (Exception ex)
            {
                ReportProgressValue = 0;
                CurrentStatusText = ex.Message;
            }
        }

        private void CopyDiscordReport()
        {
            try
            {
                string md = _nexus.BuildAnonymizedDiscordMarkdown(_lastReport, ReportSummary);
                Clipboard.SetText(md);
                CurrentStatusText = LocalizationService.CurrentLanguage == AppLanguage.German
                    ? "Anonymisierter Markdown-Bericht für Discord / Reddit in die Zwischenablage kopiert!"
                    : "Anonymized Markdown report for Discord / Reddit copied to clipboard!";
            }
            catch (Exception ex) { CurrentStatusText = ex.Message; }
        }

        private async Task TestGeminiApiAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            CurrentStatusText = isDe ? "Teste Gemini API & Modell..." : "Testing Gemini API & model...";

            var service = new GeminiDiagnosticsService(Config.GeminiApiKey, Config.SelectedGeminiModel);
            var (ok, workingModel, msg) = await service.TestConnectionAsync();

            if (ok && SelectedGeminiModel != workingModel)
            {
                SelectedGeminiModel = workingModel;
            }
            Config.Save();

            CurrentStatusText = ok ? msg : (isDe ? "API-Test fehlgeschlagen." : "API test failed.");
            MessageBox.Show(msg, ok ? "Gemini API Test: OK" : "Gemini API Info",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BrowsePath(string? key)
        {
            if (key == "SkyrimRoot")
            {
                using var fd = new FolderBrowserDialog();
                if (fd.ShowDialog() == DialogResult.OK) { SkyrimRootPath = fd.SelectedPath; Config.Save(); }
                return;
            }
            using var ofd = new OpenFileDialog { Filter = "Exe (*.exe)|*.exe" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            switch (key)
            {
                case "SSEEdit": SseEditExePath = ofd.FileName; break;
                case "SSEEditQuickClean": SseEditQuickCleanExePath = ofd.FileName; break;
                case "WryeBash": WryeBashExePath = ofd.FileName; break;
                case "LOOT": LootExePath = ofd.FileName; break;
                case "Synthesis": SynthesisExePath = ofd.FileName; break;
                case "Pandora": PandoraExePath = ofd.FileName; break;
                case "BodySlide": BodySlideExePath = ofd.FileName; break;
                case "BethINI": BethIniExePath = ofd.FileName; break;
            }
            Config.Save();
        }

        public void CaptureInGameMicScreenshot()
        {
            if (!IsSkyrimOrToolActive()) return;
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "SkyrimAutoDiag_Session");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"mic_{DateTime.Now:HHmmss}.png");
                var b = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                using var bmp = new Bitmap(b.Width, b.Height);
                using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(Point.Empty, Point.Empty, b.Size);
                bmp.Save(file, ImageFormat.Png);
                _shots.Add(file);
                SystemSounds.Beep.Play();
                CurrentStatusText = $"Screenshot [{ScreenshotHotkey}] saved ({_shots.Count}).";
            }
            catch (Exception ex) { CurrentStatusText = ex.Message; }
        }

        private bool IsSkyrimOrToolActive()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return false;
                GetWindowThreadProcessId(hwnd, out uint pid);
                using var proc = Process.GetProcessById((int)pid);
                string name = proc.ProcessName;
                return name.Contains("Skyrim", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("SkyrimAutoDiag", StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }
        }

        private async Task LaunchSkyrimAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            _shots.Clear();
            var start = DateTime.Now;
            if (!File.Exists(Config.SkseLoaderPath))
            {
                CurrentStatusText = $"Missing: {Config.SkseLoaderPath}";
                return;
            }

            SelectedTabIndex = 1;
            ReportProgressValue = 5;
            ReportProgressStatusText = LocalizationService.Get("Status_Backup");

            try
            {
                string zipName = await Task.Run(() => _backupAndReport.CreatePreLaunchBackupZip(Config.SkyrimRootPath));
                ReportProgressValue = 12;
                ReportProgressStatusText = $"Backup OK ({zipName}). {LocalizationService.Get("Status_Running")}";
            }
            catch (Exception ex) { CurrentStatusText = $"Backup Warning: {ex.Message}"; }

            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = Config.SkseLoaderPath,
                    WorkingDirectory = Config.SkyrimRootPath
                });
                if (p != null) await p.WaitForExitAsync();
            }
            catch (Exception ex) { CurrentStatusText = ex.Message; return; }

            ReportProgressValue = 20;
            ReportProgressStatusText = LocalizationService.Get("Status_Analyzing");

            var log = Directory.Exists(Config.SkseLogFolder)
                ? new DirectoryInfo(Config.SkseLogFolder).GetFiles("crash-*.log")
                    .Where(f => f.CreationTime >= start)
                    .OrderByDescending(f => f.CreationTime).FirstOrDefault()
                : null;

            string excerpt = log != null
                ? string.Join("\n", (await File.ReadAllLinesAsync(log.FullName)).Take(130))
                : "No CTD.";

            var (scan, papyrus) = await Task.Run(() =>
            {
                var s = _scanner.ScanEnvironment(Config.SkyrimRootPath, Config.SkseLogFile);
                var pap = _scanner.AnalyzePapyrusLog();
                return (s, pap);
            });

            string combinedWarnings = scan.ToFormattedSummary() + "\n--- PAPYRUS LOG ---\n" + papyrus;

            bool hasApiKey = !string.IsNullOrWhiteSpace(Config.GeminiApiKey) &&
                             Config.GeminiApiKey != "DEIN_GEMINI_API_KEY_HIER";

            DiagnosticReport? rep = null;
            string engineUsed = "Offline Engine";

            if (!Config.UseOfflineModeOnly && hasApiKey)
            {
                var progressReporter = new Progress<(int Percent, string StatusMessage)>(u =>
                {
                    ReportProgressValue = u.Percent;
                    ReportProgressStatusText = u.StatusMessage;
                });

                try
                {
                    var gemini = new GeminiDiagnosticsService(Config.GeminiApiKey, Config.SelectedGeminiModel);
                    rep = await gemini.AnalyzeSessionAsync(excerpt, combinedWarnings, _shots, progressReporter);
                    engineUsed = gemini.LastUsedModel;
                }
                catch
                {
                    rep = _nexus.AnalyzeOffline(excerpt, combinedWarnings, _shots);
                    engineUsed = "Offline Fallback";
                }
            }
            else
            {
                ReportProgressValue = 65;
                rep = await Task.Run(() => _nexus.AnalyzeOffline(excerpt, combinedWarnings, _shots));
            }

            if (rep != null)
            {
                _lastReport = rep;
                ReportSummary = rep.Summary;
                DetectedIssues.Clear();
                rep.DetectedIssues.ForEach(i => DetectedIssues.Add(i));

                var (dePath, enPath) = _backupAndReport.GenerateBilingualHtmlReports(rep, combinedWarnings, _shots);
                ReportProgressValue = 100;
                string done = isDe
                    ? $"Fertig ({engineUsed})! HTML-Berichte inkl. Tool-Workflows in: Berichte\\{Path.GetFileName(dePath)}"
                    : $"Done ({engineUsed})! HTML reports incl. tool workflows in: Berichte\\{Path.GetFileName(enPath)}";
                ReportProgressStatusText = done;
                CurrentStatusText = done;
            }
        }

        private async Task PreFlightScanAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            SelectedTabIndex = 1;
            ReportProgressValue = 25;
            CurrentStatusText = isDe
                ? "Scanne SKSE-DLLs, ESL-Header, Savegame & Papyrus-Log im Hintergrund..."
                : "Scanning SKSE DLLs, ESL headers, savegame & Papyrus log in background...";
            ReportProgressStatusText = CurrentStatusText;

            var (s, combined, rep) = await Task.Run(() =>
            {
                var scanRes = _scanner.ScanEnvironment(Config.SkyrimRootPath, Config.SkseLogFile);
                string comb = scanRes.ToFormattedSummary() + "\n" + _scanner.AnalyzePapyrusLog();
                var offlineRep = _nexus.AnalyzeOffline("No CTD.", comb, _shots);
                return (scanRes, comb, offlineRep);
            });

            _lastReport = rep;
            ReportSummary = combined;
            DetectedIssues.Clear();
            _lastReport.DetectedIssues.ForEach(i => DetectedIssues.Add(i));
            ReportProgressValue = 100;
            ReportProgressStatusText = s.IsHealthy
                ? "Scan: OK (100%)"
                : $"Errors: {s.Errors.Count}, Warnings: {s.Warnings.Count}";
            CurrentStatusText = ReportProgressStatusText;
        }

        private async Task BuildPatchAsync()
        {
            bool isDe = LocalizationService.CurrentLanguage == AppLanguage.German;
            try
            {
                ReportProgressValue = 25;
                CurrentStatusText = isDe
                    ? "Erstelle AutoDiagPatch.esp & SPID-Konfiguration (durchsuche Load Order & BSAs)..."
                    : "Building AutoDiagPatch.esp & SPID config (scanning Load Order & BSAs)...";
                ReportProgressStatusText = CurrentStatusText;

                int n = await Task.Run(() =>
                    _patcher.BuildCompatiblePatch(Config.SkyrimDataPath, Config.GenerateSpidIni));

                ReportProgressValue = 100;
                CurrentStatusText = $"{LocalizationService.Get("Patch_Success")} ({n} Records).";
                ReportProgressStatusText = CurrentStatusText;
            }
            catch (Exception ex)
            {
                ReportProgressValue = 0;
                CurrentStatusText = ex.Message;
            }
        }

        private void LaunchTool(string? key)
        {
            if (key == "SSEEditQuickClean")
            {
                string qacExe = File.Exists(Config.SseEditQuickCleanExePath)
                    ? Config.SseEditQuickCleanExePath
                    : Config.SseEditExePath;

                if (File.Exists(qacExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = qacExe,
                        Arguments = $"-quickautoclean -D:\"{Config.SkyrimDataPath}\"",
                        WorkingDirectory = Path.GetDirectoryName(qacExe),
                        UseShellExecute = true
                    });
                }
                else
                {
                    CurrentStatusText = $"SSEEdit Quick Auto Clean not found: {qacExe}";
                }
                return;
            }

            string? p = key switch
            {
                "SSEEdit" => Config.SseEditExePath,
                "WryeBash" => Config.WryeBashExePath,
                "LOOT" => Config.LootExePath,
                "Synthesis" => Config.SynthesisExePath,
                "Pandora" => Config.PandoraExePath,
                "BodySlide" => Config.BodySlideExePath,
                "BethINI" => Config.BethIniExePath,
                _ => null
            };
            if (!string.IsNullOrEmpty(p) && File.Exists(p))
                Process.Start(new ProcessStartInfo { FileName = p, WorkingDirectory = Path.GetDirectoryName(p), UseShellExecute = true });
            else
                CurrentStatusText = $"Tool not found: {p}";
        }

        private void OpenSseEdit(string? plugin)
        {
            if (File.Exists(Config.SseEditExePath))
                Process.Start(new ProcessStartInfo { FileName = Config.SseEditExePath, Arguments = $"-D:\"{Config.SkyrimDataPath}\" \"{plugin}\"" });
        }

        protected void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}