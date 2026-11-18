using System.Collections.Generic;

namespace SkyrimAutoDiag
{
    public enum AppLanguage { German, English }

    public static class LocalizationService
    {
        public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.German;

        private static readonly Dictionary<string, (string De, string En)> _strings = new()
        {
            ["Btn_Launch"] = (
                "Skyrim AE (SKSE) starten & \u00FCberwachen",
                "Launch & Monitor Skyrim AE (SKSE)"),
            ["Btn_BuildPatch"] = (
                "Kompatibilit\u00E4ts-Patch + SPID erstellen",
                "Build Compatibility Patch + SPID"),
            ["Btn_ScanSkse"] = (
                "SKSE, Save, DDS & Load Order pr\u00FCfen",
                "Scan SKSE, Save, DDS & Load Order"),
            ["Btn_FixEslHeaders"] = (
                "ESL-Header (< 1.71) automatisch reparieren",
                "Auto-Fix ESL Headers (< 1.71)"),
            ["Btn_AutoDetectPaths"] = (
                "Pfade automatisch erkennen (Registry & Laufwerke)",
                "Auto-Detect Paths (Registry & Drives)"),
            ["Btn_CopyDiscord"] = (
                "Bericht f\u00FCr Discord / Reddit kopieren (Anonymisiert)",
                "Copy Report for Discord / Reddit (Anonymized)"),
            ["Btn_SaveConfig"] = (
                "Einstellungen dauerhaft speichern",
                "Save Configuration Permanently"),
            ["Btn_TestApi"] = (
                "API & Modell testen",
                "Test API & Model"),
            ["Btn_OpenStudio"] = (
                "\uD83D\uDD11 API Key erstellen (Google AI Studio)",
                "\uD83D\uDD11 Create API Key (Google AI Studio)"),

            ["Chk_OfflineMode"] = (
                "Lokaler Offline-Modus (Keine Gemini KI / Kein API-Key erforderlich)",
                "Local Offline Mode (No Gemini AI / No API Key required)"),
            ["Chk_SpidExport"] = (
                "Zus\u00E4tzlich konfliktfreie SPID-Datei (AutoDiag_DISTR.ini) erzeugen",
                "Also generate conflict-free SPID file (AutoDiag_DISTR.ini)"),
            ["Lbl_ModelSelect"] = (
                "Bevorzugtes Gemini KI-Modell (wechselt bei Ausfall automatisch auf aktuelle 3.x Modelle):",
                "Preferred Gemini AI Model (auto-switches to current 3.x models on failure):"),
            ["Lbl_ReportProgress"] = (
                "Fortschritt der Diagnose & Berichterstellung:",
                "Diagnostics & Report Generation Progress:"),
            ["Progress_Idle"] = (
                "Bereit. Warte auf Spielsitzung oder Diagnose-Start...",
                "Idle. Waiting for game session or diagnostic start..."),

            ["Tab_Overview"] = ("\u00DCbersicht", "Overview"),
            ["Tab_Diagnostics"] = ("Diagnose & L\u00F6sungen", "Diagnostics & Solutions"),
            ["Tab_Settings"] = ("Konfiguration & Pfade", "Configuration & Paths"),
            ["Tab_WebSearch"] = ("\uD83C\uDF10 Online-L\u00F6sungssuche", "\uD83C\uDF10 Online Solution Finder"),
            ["Tab_Manual"] = ("\uD83D\uDCD8 Benutzerhandbuch", "\uD83D\uDCD8 User Manual"),

            ["Header_Tools"] = ("Modding-Tools & Patcher", "Modding Tools & Patcher"),
            ["Header_Report"] = (
                "Diagnose-Bericht & Schritt-f\u00FCr-Schritt-L\u00F6sungen (Offline & KI)",
                "Diagnostic Report & Step-by-Step Solutions (Offline & AI)"),
            ["Header_Settings"] = (
                "Programm- & Tool-Konfiguration",
                "Application & Tool Configuration"),

            ["Status_Ready"] = ("Bereit.", "Ready."),
            ["Status_Backup"] = (
                "Erstelle Sicherheits-Backup (.zip) im Ordner 'Backup'...",
                "Creating safety backup (.zip) in 'Backup' folder..."),
            ["Status_Running"] = (
                "Skyrim AE l\u00E4uft... Dr\u00FCcke deinen Hotkey f\u00FCr Bug-Screenshots.",
                "Skyrim AE is running... Press your hotkey for bug screenshots."),
            ["Status_Analyzing"] = (
                "Analysiere Sitzungsdaten und erstelle HTML-Berichte (DE/EN)...",
                "Analyzing session data and building HTML reports (DE/EN)..."),
            ["Status_ConfigSaved"] = (
                "Konfiguration gespeichert.",
                "Configuration saved."),
            ["Patch_Success"] = (
                "AutoDiagPatch.esp & AutoDiag_DISTR.ini erfolgreich erstellt.",
                "AutoDiagPatch.esp & AutoDiag_DISTR.ini successfully built."),

            ["Overview_Title"] = (
                "Skyrim AE Diagnostics Suite \u2013 Funktions\u00FCbersicht",
                "Skyrim AE Diagnostics Suite \u2013 Feature Overview"),
            ["Overview_Desc"] = (
                "\u00DCberblick \u00FCber alle integrierten Funktionen dieses Programms. " +
                "Rechts oben findest du die '\uD83C\uDF10 Online-L\u00F6sungssuche' f\u00FCr eigene " +
                "Fragen sowie ganz rechts das '\uD83D\uDCD8 Benutzerhandbuch'.",
                "Overview of all integrated features in this application. " +
                "At the top right, you will find the '\uD83C\uDF10 Online Solution Finder' " +
                "and on the far right the '\uD83D\uDCD8 User Manual'."),

            ["Feat1_Title"] = (
                "1. Automatisches Vorab-Backup (.zip)",
                "1. Automated Pre-Launch Backup (.zip)"),
            ["Feat1_Desc"] = (
                "Sichert vor jedem Spielstart automatisch deine Skyrim-INIs, die " +
                "aktuelle Load Order (plugins.txt), deine AutoDiagPatch.esp sowie " +
                "das letzte intakte Savegame (.ess + .skse) als ZIP im Ordner 'Backup'.",
                "Automatically backs up your Skyrim INIs, active load order " +
                "(plugins.txt), AutoDiagPatch.esp, and latest working savegame " +
                "(.ess + .skse) as a ZIP archive in the 'Backup' folder before launch."),

            ["Feat2_Title"] = (
                "2. Vorab-Umgebungs-, SKSE-, Savegame- & DDS-Scanner",
                "2. Pre-Flight Environment, SKSE, Savegame & DDS Scanner"),
            ["Feat2_Desc"] = (
                "Pr\u00FCft SKSE-DLLs (inkl. 32/64-Bit-Check & Address Library), " +
                "erkennt fehlende Mods oder korrupte Co-Saves im letzten Spielstand, " +
                "warnt bei fehlendem Mod Organizer 2 VFS-Hook und pr\u00FCft .dds-Texturen.",
                "Validates SKSE DLLs (64-bit check & Address Library), detects missing " +
                "mods or corrupt co-saves in your latest savegame, warns if launched " +
                "outside Mod Organizer 2 (USVFS), and checks .dds texture dimensions."),

            ["Feat3_Title"] = (
                "3. 1-Klick ESL-Header 1.71 Reparatur (Skyrim AE 1.6.1130+)",
                "3. 1-Click ESL Header 1.71 Repair (Skyrim AE 1.6.1130+)"),
            ["Feat3_Desc"] = (
                "Findet Light-Plugins (.esl / ESL-flagged .esp), die FormIDs unter " +
                "0x800 mit einem veralteten Header (< 1.71) nutzen, legt ein Backup " +
                "an und aktualisiert den Header direkt auf Version 1.71.",
                "Finds light plugins (.esl / ESL-flagged .esp) using FormIDs below " +
                "0x800 with an outdated header (< 1.71), backs them up, and upgrades " +
                "their header directly to version 1.71."),

            ["Feat4_Title"] = (
                "4. In-Game Bug-Erkennung (More Informative Console + Audio-Beep)",
                "4. In-Game Bug Detection (More Informative Console + Audio Beep)"),
            ["Feat4_Desc"] = (
                "Erstellt im laufenden Spiel per frei w\u00E4hlbarem Hotkey gezielt " +
                "Screenshots von visuellen Fehlern (Black Face Bug, lila Texturen, " +
                "fehlende Meshes), best\u00E4tigt per Signalton und pr\u00FCft alle .bsa-Archive.",
                "Captures in-game screenshots of visual bugs (Black Face Bug, purple " +
                "textures, missing meshes) via custom hotkey with audio confirmation " +
                "and validates assets across loose files and .bsa archives."),

            ["Feat5_Title"] = (
                "5. Lokale Offline-Crash-Analyse & Optionale Google Gemini 3.x KI",
                "5. Local Offline Crash Analyzer & Optional Google Gemini 3.x AI"),
            ["Feat5_Desc"] = (
                "Wertet Crash-Logs und Papyrus-Logs komplett offline \u00FCber die " +
                "erweiterbare 'crash_rules.json' aus (kein API-Key n\u00F6tig!). Optional " +
                "kann die aktuelle Gemini 3.x KI (mit Auto-Fallback) zugeschaltet werden.",
                "Evaluates crash logs and Papyrus logs 100% offline via the extensible " +
                "'crash_rules.json' database (no API key required!). Optionally enables " +
                "current Gemini 3.x AI (with auto-fallback) for screenshot OCR."),

            ["Feat6_Title"] = (
                "6. Synthesis-kompatibler Mutagen- & SPID-Auto-Patcher",
                "6. Synthesis-Compatible Mutagen & SPID Auto-Patcher"),
            ["Feat6_Desc"] = (
                "Erzeugt 'AutoDiagPatch.esp' (ESL-Flag, Header 1.71) sowie " +
                "'AutoDiag_DISTR.ini' (SPID). Behebt NPC-Black-Face-Bugs (inkl. " +
                "BSA-FaceGen-Suche) und f\u00FChrt Leveled Lists sowie Truhen zusammen.",
                "Generates 'AutoDiagPatch.esp' (ESL, Header 1.71) and " +
                "'AutoDiag_DISTR.ini' (SPID). Fixes NPC Black Face Bugs (including " +
                "BSA FaceGen lookup) and merges Leveled Lists and Containers."),

            ["Feat7_Title"] = (
                "7. Zweisprachige HTML-Berichte & Interaktive Tool-Workflows",
                "7. Bilingual HTML Reports & Interactive Tool Workflows"),
            ["Feat7_Desc"] = (
                "Speichert nach jeder Sitzung je einen deutschen und englischen " +
                "HTML-Bericht im Ordner 'Berichte' und stellt ma\u00DFgeschneiderte, " +
                "abhakbare HTML-Anleitungen aus der 'tool_workflows.json' bereit.",
                "Saves both German and English HTML reports in the 'Berichte' folder " +
                "after every session and provides tailored, checkable HTML guides " +
                "from 'tool_workflows.json' for manual tool repairs."),

            ["Feat8_Title"] = (
                "8. Online-L\u00F6sungssuche (Community, Foren & Lokale Wissensbasis)",
                "8. Online Solution Finder (Community, Forums & Local Knowledge)"),
            ["Feat8_Desc"] = (
                "Beschreibe jedes Problem im Reiter '\uD83C\uDF10 Online-L\u00F6sungssuche' " +
                "einfach in eigenen Worten: Das Programm sucht live in der Modding-" +
                "Community (r/skyrimmods), in unserer Datenbank und optional per KI.",
                "Simply describe any issue in your own words under '\uD83C\uDF10 Online Solution " +
                "Finder': The app searches the modding community (r/skyrimmods) live, " +
                "checks our local database, and optionally queries AI."),

            ["Manual_Header"] = (
                "\uD83D\uDCD8 Interaktives Benutzerhandbuch & Schritt-f\u00FCr-Schritt-Hilfe",
                "\uD83D\uDCD8 Interactive User Manual & Step-by-Step Guide"),
            ["Manual_SearchPlaceholder"] = (
                "Handbuch durchsuchen (z. B. MO2, Black Face, ESL, Backup, Hotkey, SPID, KI)...",
                "Search manual (e.g., MO2, Black Face, ESL, Backup, Hotkey, SPID, AI)..."),
            ["Manual_BtnExpandAll"] = ("Alle Kapitel \u00F6ffnen", "Expand All Chapters"),
            ["Manual_BtnCollapseAll"] = ("Alle schlie\u00DFen", "Collapse All"),
            ["Manual_BtnResetSearch"] = ("Filter l\u00F6schen", "Clear Filter"),

            ["WebSearch_Header"] = (
                "\uD83C\uDF10 Online-L\u00F6sungssuche \u2013 Beschreibe dein Problem in eigenen Worten",
                "\uD83C\uDF10 Online Solution Finder \u2013 Describe Your Issue in Your Own Words"),
            ["WebSearch_Desc"] = (
                "Egal ob Absturz, Grafikfehler, festgefahrene Quest oder Animations-Bug: " +
                "Tippe unten einfach ein, was im Spiel passiert (z. B. 'Charakter steckt " +
                "in T-Pose fest', 'Absturz in Wei\u00DFlauf' oder 'Wasser flackert'). Wir " +
                "suchen automatisch nach bew\u00E4hrten L\u00F6sungen aus der Community.",
                "Whether it's a crash, visual glitch, stuck quest, or animation bug: " +
                "Simply type what happens in-game below (e.g., 'character stuck in T-pose', " +
                "'crash entering Whiterun', or 'water flickering'). We automatically " +
                "search for proven community solutions."),
            ["WebSearch_BtnSearch"] = (
                "\uD83D\uDD0D Passende L\u00F6sungen suchen",
                "\uD83D\uDD0D Find Matching Solutions"),
            ["WebSearch_BtnSaveHtml"] = (
                "\uD83D\uDCC4 Ergebnisse als Bericht speichern & \u00F6ffnen",
                "\uD83D\uDCC4 Save & Open Results as Report"),
            ["WebSearch_BtnOpenUrl"] = (
                "\uD83C\uDF10 Original-Beitrag im Browser \u00F6ffnen",
                "\uD83C\uDF10 Open Original Post in Browser")
        };

        public static string Get(string key) =>
            _strings.TryGetValue(key, out var val)
                ? (CurrentLanguage == AppLanguage.German ? val.De : val.En)
                : key;

        public static string GetGeminiLanguageInstruction() =>
            CurrentLanguage == AppLanguage.German
                ? "Antworte komplett auf DEUTSCH. Behalte jedoch g\u00E4ngige Skyrim-Modding-Fachbegriffe (wie FormID, Load Order, Black Face Bug, FaceGen, Crash Logger, SSEEdit) bei."
                : "Respond entirely in ENGLISH using standard Skyrim modding terminology.";
    }
}