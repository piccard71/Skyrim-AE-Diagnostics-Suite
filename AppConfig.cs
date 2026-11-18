using System;
using System.IO;
using System.Text.Json;

namespace SkyrimAutoDiag
{
    public class AppConfig
    {
        public string GeminiApiKey { get; set; } = "DEIN_GEMINI_API_KEY_HIER";
        public string SelectedGeminiModel { get; set; } = GeminiDiagnosticsService.DefaultModel;
        public bool UseOfflineModeOnly { get; set; } = false;
        public bool GenerateSpidIni { get; set; } = true;
        public string ScreenshotHotkey { get; set; } = "F11";
        public string SkyrimRootPath { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition";

        public string SseEditExePath { get; set; } = @"C:\Modding\SSEEdit\SSEEdit.exe";
        public string SseEditQuickCleanExePath { get; set; } = @"C:\Modding\SSEEdit\SSEEditQAC.exe";
        public string WryeBashExePath { get; set; } = @"C:\Modding\Wrye Bash\Wrye Bash.exe";
        public string LootExePath { get; set; } = @"C:\Modding\LOOT\LOOT.exe";
        public string SynthesisExePath { get; set; } = @"C:\Modding\Synthesis\Synthesis.exe";
        public string PandoraExePath { get; set; } = @"C:\Modding\Pandora\Pandora Behaviour Engine+.exe";
        public string BodySlideExePath { get; set; } = @"C:\Modding\BodySlide\BodySlide x64.exe";
        public string BethIniExePath { get; set; } = @"C:\Modding\BethINI\BethINI.exe";

        public string SkseLoaderPath => Path.Combine(SkyrimRootPath, "skse64_loader.exe");
        public string SkyrimDataPath => Path.Combine(SkyrimRootPath, "Data");

        public string SkseLogFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "My Games", "Skyrim Special Edition", "SKSE");

        public string SkseLogFile => Path.Combine(SkseLogFolder, "skse64.log");

        public static string DatabaseFolderPath
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datenbanken");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private static string ConfigFilePath => Path.Combine(DatabaseFolderPath, "config.json");

        public static void EnsureDatabaseFolder()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dbDir = DatabaseFolderPath;
            string[] jsonFiles = { "config.json", "user_manual.json", "crash_rules.json", "tool_workflows.json" };

            foreach (var file in jsonFiles)
            {
                string oldPath = Path.Combine(baseDir, file);
                string newPath = Path.Combine(dbDir, file);
                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    try { File.Move(oldPath, newPath); } catch { }
                }
            }
        }

        public static AppConfig LoadOrCreate()
        {
            EnsureDatabaseFolder();

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                    if (cfg != null)
                    {
                        string normalized = GeminiDiagnosticsService.NormalizeModelName(cfg.SelectedGeminiModel);
                        bool changed = normalized != cfg.SelectedGeminiModel;
                        cfg.SelectedGeminiModel = normalized;

                        if (string.IsNullOrWhiteSpace(cfg.SseEditQuickCleanExePath))
                            cfg.SseEditQuickCleanExePath = cfg.SseEditExePath;

                        if (changed)
                            cfg.Save();

                        return cfg;
                    }
                }
            }
            catch { }

            var defaultConfig = new AppConfig();
            defaultConfig.Save();
            return defaultConfig;
        }

        public void Save()
        {
            EnsureDatabaseFolder();
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}