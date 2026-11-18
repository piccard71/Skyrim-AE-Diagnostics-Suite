using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyrimAutoDiag
{
    public class ManualChapterEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("badge_de")]
        public string BadgeDe { get; set; } = string.Empty;

        [JsonPropertyName("badge_en")]
        public string BadgeEn { get; set; } = string.Empty;

        [JsonPropertyName("title_de")]
        public string TitleDe { get; set; } = string.Empty;

        [JsonPropertyName("title_en")]
        public string TitleEn { get; set; } = string.Empty;

        [JsonPropertyName("summary_de")]
        public string SummaryDe { get; set; } = string.Empty;

        [JsonPropertyName("summary_en")]
        public string SummaryEn { get; set; } = string.Empty;

        [JsonPropertyName("details_de")]
        public List<string> DetailsDe { get; set; } = new();

        [JsonPropertyName("details_en")]
        public List<string> DetailsEn { get; set; } = new();

        [JsonPropertyName("protip_de")]
        public string ProTipDe { get; set; } = string.Empty;

        [JsonPropertyName("protip_en")]
        public string ProTipEn { get; set; } = string.Empty;
    }

    public static class UserManualService
    {
        public static string ManualFilePath =>
            Path.Combine(AppConfig.DatabaseFolderPath, "user_manual.json");

        public static List<ManualChapterEntry> LoadOrCreateManual()
        {
            try
            {
                if (File.Exists(ManualFilePath))
                {
                    string json = File.ReadAllText(ManualFilePath, Encoding.UTF8);
                    // Falls eine alte JSON-Datei kaputte UTF-8-Zeichen ("Ã") enth\u00E4lt, sofort neu generieren!
                    if (!json.Contains("\u00C3"))
                    {
                        var loaded = JsonSerializer.Deserialize<List<ManualChapterEntry>>(json);
                        if (loaded != null && loaded.Count >= 7)
                            return loaded;
                    }
                }
            }
            catch { }

            var defaults = GetDefaultChapters();
            SaveManual(defaults);
            return defaults;
        }

        public static void SaveManual(List<ManualChapterEntry> chapters)
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(ManualFilePath, JsonSerializer.Serialize(chapters, opts), new UTF8Encoding(true));
            }
            catch { }
        }

        private static List<ManualChapterEntry> GetDefaultChapters()
        {
            return new List<ManualChapterEntry>
            {
                new ManualChapterEntry
                {
                    Id = "chapter_1_setup",
                    BadgeDe = "SETUP & MO2",
                    BadgeEn = "SETUP & MO2",
                    TitleDe = "1. Ersteinrichtung, Pfad-Erkennung & Mod Organizer 2 (USVFS)",
                    TitleEn = "1. Initial Setup, Path Detection & Mod Organizer 2 (USVFS)",
                    SummaryDe = "Damit das Diagnose- und Patch-System alle installierten Mods, SKSE-Plugins und Archive korrekt erfassen kann, " +
                                "muss das Programm einmalig mit deiner Skyrim-Installation und deinen Modding-Werkzeugen verkn\u00FCpft werden.",
                    SummaryEn = "For the diagnostic and patching engine to properly detect all installed mods, SKSE plugins, and archives, " +
                                "the application must be linked to your Skyrim installation and modding tools.",
                    DetailsDe = new List<string>
                    {
                        "Automatische Pfad-Erkennung (Auto-Detect): \u00D6ffne nach dem ersten Programmstart den Reiter 'Konfiguration & Pfade' und klicke oben rechts auf den gr\u00FCnen Button 'Pfade automatisch erkennen'. " +
                        "Das Programm liest daraufhin den offiziellen Installationspfad deiner Skyrim Special / Anniversary Edition direkt aus der Windows-Registry aus und durchsucht deine Laufwerke nach installierten Werkzeugen wie SSEEdit, SSEEditQAC, Wrye Bash, LOOT, Synthesis, Pandora/Nemesis, BodySlide und BethINI. " +
                        "Alle gefundenen Pfade werden sofort eingetragen und beim Klick auf 'Speichern' dauerhaft in der Datei config.json abgelegt.",

                        "Start \u00FCber Mod Organizer 2 (Wichtige USVFS-Regel): Wenn du Mod Organizer 2 (MO2) verwendest, speichert MO2 deine Mods nicht direkt im echten Skyrim-Data-Ordner, sondern blendet sie erst beim Programmstart \u00FCber ein virtuelles Dateisystem (USVFS) ein. " +
                        "W\u00FCrdest du SkyrimAutoDiag einfach per Doppelklick vom Desktop starten, s\u00E4he das Programm nur das leere Grundspiel ohne Mods. " +
                        "F\u00FCge deshalb die SkyrimAutoDiag.exe in Mod Organizer 2 unter 'Executables' (Ausf\u00FChrbare Dateien) hinzu und starte das Tool immer direkt \u00FCber den Start-Button innerhalb von MO2. Unser integrierter Scanner pr\u00FCft beim Start automatisch, ob das virtuelle Dateisystem aktiv ist, und warnt dich, falls der Start \u00FCber MO2 vergessen wurde.",

                        "Vortex & Manuelle Installationen: Nutzer des Vortex Mod Managers arbeiten mit sogenannten Hardlinks direkt im Data-Verzeichnis. " +
                        "Hier kann SkyrimAutoDiag sowohl \u00FCber das Vortex-Dashboard als auch direkt per Desktop-Verkn\u00FCpfung gestartet werden, da alle Plugin- und Archivdateien physisch im Spielordner sichtbar sind.",

                        "Konfiguration des In-Game Screenshot-Hotkeys: Im Konfigurations-Tab findest du ein blau hervorgehobenes Eingabefeld f\u00FCr deinen Screenshot-Hotkey. " +
                        "Klicke dieses Feld mit der Maus an und dr\u00FCcke anschlie\u00DFend deine gew\u00FCnschte Taste auf der Tastatur (standardm\u00E4\u00DFig F11). Diese Taste wird sp\u00E4ter im laufenden Spiel genutzt, um visuelle Fehler samt ge\u00F6ffnetem Konsolenfenster f\u00FCr die Diagnose zu fotografieren."
                    },
                    DetailsEn = new List<string>
                    {
                        "Automated Path Detection (Auto-Detect): After launching the application for the first time, open the 'Configuration & Paths' tab and click the green 'Auto-Detect Paths' button in the top right corner. " +
                        "The tool queries the Windows Registry for your official Skyrim Special / Anniversary Edition installation path and scans standard modding directories for SSEEdit, SSEEditQAC, Wrye Bash, LOOT, Synthesis, Pandora/Nemesis, BodySlide, and BethINI. Click 'Save' to store these settings permanently in config.json.",

                        "Launching via Mod Organizer 2 (Crucial USVFS Rule): Mod Organizer 2 keeps your actual Skyrim Data folder clean and injects mods dynamically using a Userland Virtual File System (USVFS). " +
                        "If you launch SkyrimAutoDiag directly from your Windows desktop, it will only see vanilla Skyrim files. You must add SkyrimAutoDiag.exe as an executable inside MO2 and always launch it from the MO2 dropdown menu. " +
                        "Our built-in scanner automatically checks if USVFS is active and alerts you if you accidentally launched the tool outside of MO2.",

                        "Vortex & Manual Installations: Vortex deploys mods using hardlinks directly inside the Skyrim Data folder. " +
                        "Therefore, Vortex users can launch SkyrimAutoDiag either from the Vortex dashboard or directly via a desktop shortcut with full access to all plugins and archives.",

                        "Configuring the In-Game Screenshot Hotkey: Inside the configuration tab, click the blue highlighted hotkey box and press any key on your keyboard (default is F11). " +
                        "This hotkey will be monitored while Skyrim is running in the foreground to capture visual bugs alongside the open console window."
                    },
                    ProTipDe = "Tipp: Vergisst du den Start \u00FCber Mod Organizer 2, erkennt unser Vorab-Scanner das leere virtuelle Dateisystem sofort und gibt dir einen gezielten Hinweis!",
                    ProTipEn = "Pro Tip: If you accidentally launch the tool outside of Mod Organizer 2, our pre-flight scanner immediately detects the unmounted VFS and alerts you!"
                },
                new ManualChapterEntry
                {
                    Id = "chapter_2_modes",
                    BadgeDe = "OFFLINE vs. KI",
                    BadgeEn = "OFFLINE vs. AI",
                    TitleDe = "2. Lokaler Offline-Modus vs. Optionale Google Gemini 3.x KI-Analyse",
                    TitleEn = "2. Local Offline Mode vs. Optional Google Gemini 3.x AI Analysis",
                    SummaryDe = "SkyrimAutoDiag funktioniert vollst\u00E4ndig ohne Cloud-Zwang oder API-Schl\u00FCssel. Du entscheidest selbst, ob du ausschlie\u00DFlich die lokale Regel-Datenbank nutzt oder zus\u00E4tzlich die multimodale Bilderkennung von Google Gemini 3.x zuschaltest.",
                    SummaryEn = "SkyrimAutoDiag works completely out of the box without requiring any cloud subscription or API key. Choose between the local rule-based diagnostic engine or enable multimodal image and log analysis via Google Gemini 3.x.",
                    DetailsDe = new List<string>
                    {
                        "Arbeiten im reinen Offline-Modus (Kein API-Key n\u00F6tig): Wenn du keinen Gemini API-Schl\u00FCssel eintr\u00E4gst oder im Konfigurations-Tab das H\u00E4kchen bei 'Lokaler Offline-Modus' setzt, arbeitet das Programm zu 100 % lokal auf deinem PC. " +
                        "Es analysiert Absturzberichte (Crash Logs) anhand der lokalen Datenbank 'crash_rules.json', f\u00FChrt alle SKSE-, Savegame-, Papyrus- und Asset-Pr\u00FCfungen durch und stellt dir den kompletten Mutagen-Auto-Patcher sowie den 1-Klick ESL-Header-Fixer uneingeschr\u00E4nkt zur Verf\u00FCgung.",

                        "Erweiterte Diagnose mit Google Gemini 3.x KI (Optional): Wenn du \u00FCber den Button '\uD83D\uDD11 API Key erstellen (Google AI Studio)' einen kostenlosen API-Schl\u00FCssel erstellst und eintr\u00E4gst, schaltet das Programm nach dem Beenden von Skyrim zus\u00E4tzlich die KI-Auswertung frei. " +
                        "Der gro\u00DFe Vorteil liegt in der Kombination mehrerer Datenquellen: Gemini liest per Bilderkennung die abfotografierten Fenster der 'More Informative Console' (MIC) direkt aus deinen Screenshots aus, verkn\u00FCpft die dort sichtbare Base FormID und das letzte \u00FCberschreibende Plugin mit den Callstacks und CPU-Registern (RSP, RCX, RAX) deines Crash-Logs und formuliert daraus eine ma\u00DFgeschneiderte Erkl\u00E4rung.",

                        "Automatischer Modell-Fallback (Gemini 3.x): Sollte das gew\u00E4hlte Modell (z. B. gemini-3-flash-preview oder gemini-3.1-pro-preview) vor\u00FCbergehend ausgelastet sein, wechselt das Programm automatisch auf die weiteren verf\u00FCgbaren Modelle und schaltet im Notfall nahtlos auf die lokale Offline-Analyse um, sodass du nach jeder Spielsitzung garantiert einen fertigen Diagnosebericht erh\u00E4ltst."
                    },
                    DetailsEn = new List<string>
                    {
                        "Running in Pure Offline Mode (No API Key Required): If you leave the API key field blank or check 'Local Offline Mode' in the settings tab, the application operates 100% locally on your machine. " +
                        "It evaluates crash logs against the expandable 'crash_rules.json' database, runs all SKSE, savegame, Papyrus, and asset validations, and provides full access to the Mutagen Auto-Patcher and 1-Click ESL Header Fixer.",

                        "Enhanced Diagnostics with Google Gemini 3.x AI (Optional): By clicking '\uD83D\uDD11 Create API Key (Google AI Studio)' and entering a free API key, you unlock multimodal post-session analysis. " +
                        "Gemini uses computer vision to read the open 'More Informative Console' (MIC) windows directly from your in-game screenshots, extracts the exact Base FormID and winning plugin, correlates them with CPU registers (RSP, RCX, RAX) and callstacks from your crash log, and generates a tailored technical explanation.",

                        "Automated Model Fallback (Gemini 3.x): If your selected Gemini model (e.g., gemini-3-flash-preview or gemini-3.1-pro-preview) experiences temporary congestion, the tool automatically switches to backup models and seamlessly falls back to the local offline engine if needed."
                    },
                    ProTipDe = "Tipp: Du kannst die lokale Datei 'crash_rules.json' im Ordner 'Datenbanken' jederzeit mit einem Texteditor um eigene Crash-Signaturen erweitern.",
                    ProTipEn = "Pro Tip: You can expand 'crash_rules.json' in the 'Datenbanken' folder anytime using a standard text editor."
                },
                new ManualChapterEntry
                {
                    Id = "chapter_3_preflight",
                    BadgeDe = "VORAB-SCAN & ESL 1.71",
                    BadgeEn = "PRE-FLIGHT & ESL 1.71",
                    TitleDe = "3. Vorab-Umgebungsscan, Savegame-Schutz & 1-Klick ESL-Header 1.71 Fix",
                    TitleEn = "3. Pre-Flight Environment Scan, Savegame Protection & 1-Click ESL 1.71 Fix",
                    SummaryDe = "Die meisten Abst\u00FCrze beim Spielstart (CTD vor dem Hauptmen\u00FC) oder beim Laden eines Spielstands lassen sich bereits erkennen, bevor Skyrim \u00FCberhaupt gestartet wird. Der Vorab-Scanner untersucht deine gesamte Installation in wenigen Sekunden auf bin\u00E4rer Ebene.",
                    SummaryEn = "Most crashes on launch (CTD before reaching the main menu) or during savegame loading can be detected before Skyrim even starts. The pre-flight scanner inspects your entire installation at the binary level within seconds.",
                    DetailsDe = new List<string>
                    {
                        "SKSE-DLL & Address-Library-Pr\u00FCfung: Beim Klick auf 'SKSE, Save, DDS & Load Order pr\u00FCfen' liest das Programm zuerst die exakte Dateiversion deiner SkyrimSE.exe aus (z. B. 1.6.1170.0) und kontrolliert im Ordner Data\\SKSE\\Plugins, ob genau die dazu passende Adress-Bibliothek (z. B. versionlib-1-6-1170-0.bin) vorhanden ist. " +
                        "Anschlie\u00DFend \u00F6ffnet der Scanner den Bin\u00E4r-Header (PE-Header) jeder einzelnen .dll-Datei, entlarvt versehentlich installierte 32-Bit-Mods aus Skyrim Legendary Edition (Oldrim) und durchsucht die skse64.log nach Ladefehlern oder bekannten Inkompatibilit\u00E4ten.",

                        "Der Skyrim AE 1.6.1130+ ESL-Header-Bug & 1-Klick-Reparatur: Seit dem Skyrim-Update 1.6.1130 unterst\u00FCtzt die Engine bei Light-Plugins (.esl oder .esp mit ESL-Flag) einen erweiterten FormID-Bereich unterhalb von 0x800, setzt daf\u00FCr aber zwingend die Plugin-Header-Version 1.71 voraus. " +
                        "Besitzt ein solches Plugin noch den alten Header 1.70, zerschie\u00DFt Skyrim im laufenden Spiel Referenzen oder st\u00FCrzt sofort beim Start ab. Unser Scanner sp\u00FCrt diese Plugins \u00FCber Mutagen pr\u00E4zise auf. Mit einem Klick auf den lila Button 'ESL-Header (< 1.71) automatisch reparieren' legt das Tool eine Sicherheitskopie (.bak) der betroffenen Datei im Ordner 'Backup' an und hebt den Header in Sekundenschnelle sauber auf Version 1.71 an.",

                        "Savegame- & Co-Save-Gesundheitscheck: Das Programm untersucht vor dem Start den Bin\u00E4r-Header deines neuesten Spielstands (*.ess im Saves-Ordner). " +
                        "Es pr\u00FCft, ob das zugeh\u00F6rige SKSE-Co-Save (*.skse) intakt und gr\u00F6\u00DFer als 0 Bytes ist, und gleicht die im Spielstand eingebettete Mod-Liste mit deiner aktuellen Ladereihenfolge ab. Fehlt eine Mod, die beim letzten Speichern noch aktiv war, wirst du sofort gewarnt, bevor dein Spielstand beim Laden besch\u00E4digt wird.",

                        "Bin\u00E4re .nif-Mesh- & .dds-Textur-Pr\u00FCfung (inkl. BSAs): F\u00FCr erkannte Objekte pr\u00FCft der Asset-Validator nicht nur, ob die 3D-Modelle (.nif) und Texturen (.dds) als lose Dateien oder gepackt in .bsa-Archiven existieren. " +
                        "Er liest auch die ersten 20 Bytes des .dds-Bin\u00E4rheaders aus und kontrolliert, ob Breite und H\u00F6he der Textur glatt durch 4 teilbar sind \u2013 denn ungerade Texturformate bei BC7- oder DXT-Kompression f\u00FChren in DirectX 11 zu sofortigen Abst\u00FCrzen ohne klaren Hinweis im Log."
                    },
                    DetailsEn = new List<string>
                    {
                        "SKSE DLL & Address Library Validation: Clicking 'Scan SKSE, Save, DDS & Load Order' reads the exact file version of your SkyrimSE.exe (e.g., 1.6.1170.0) and verifies that the matching Address Library file (e.g., versionlib-1-6-1170-0.bin) exists inside Data\\SKSE\\Plugins. " +
                        "It then inspects the binary PE header of every installed .dll plugin to catch incompatible 32-bit Oldrim DLLs and parses skse64.log for loading errors.",

                        "The Skyrim AE 1.6.1130+ ESL Header Bug & 1-Click Fixer: Starting with Skyrim AE 1.6.1130, light plugins (.esl or ESL-flagged .esp) using FormIDs below 0x800 strictly require header version 1.71. " +
                        "If an older plugin uses low FormIDs with a 1.70 header, Skyrim corrupts references or crashes immediately. Our Mutagen scanner detects every affected plugin, and clicking the purple 'Auto-Fix ESL Headers (< 1.71)' button automatically backs up the original file (.bak) to the 'Backup' folder before upgrading its header to 1.71.",

                        "Savegame & Co-Save Health Check: Before launching the game, the binary header of your most recent savegame (*.ess) is analyzed. " +
                        "The scanner verifies that the accompanying *.skse co-save is not corrupted (0 bytes) and compares the savegame's embedded plugin list against your active Data folder to warn you about missing plugins before you load into a broken save.",

                        "Binary .nif Mesh & .dds Texture Validation (incl. BSAs): Beyond checking whether 3D meshes (.nif) and textures (.dds) exist as loose files or inside compressed .bsa archives, the asset validator inspects the binary header of .dds files to verify that texture dimensions (width and height) are multiples of 4. " +
                        "Non-multiple-of-4 textures using BC7/DXT compression cause instant DirectX 11 crashes."
                    },
                    ProTipDe = "Tipp: Der 1-Klick ESL-Header-Fixer behebt einen der h\u00E4ufigsten Startup-Crashes unter Skyrim AE 1.6.1130 und 1.6.1170 vollautomatisch!",
                    ProTipEn = "Pro Tip: The 1-Click ESL Header Fixer automatically resolves one of the most common startup crashes on Skyrim AE 1.6.1130 and 1.6.1170!"
                },
                new ManualChapterEntry
                {
                    Id = "chapter_4_autopatcher",
                    BadgeDe = "AUTO-PATCHER & SPID",
                    BadgeEn = "AUTO-PATCHER & SPID",
                    TitleDe = "4. Synthesis-kompatibler Mutagen Auto-Patcher & SPID-Generator",
                    TitleEn = "4. Synthesis-Compatible Mutagen Auto-Patcher & SPID Generator",
                    SummaryDe = "Wenn mehrere Mods denselben NPC, dieselbe Beuteliste oder dieselbe Truhe ver\u00E4ndern, gewinnt in Skyrim normalerweise nur die letzte Mod in der Ladereihenfolge \u2013 alle anderen \u00C4nderungen gehen verloren oder erzeugen den gef\u00FCrchteten Black-Face-Bug. Unser integrierter Patcher l\u00F6st dieses Problem vollautomatisch.",
                    SummaryEn = "When multiple mods edit the same NPC, loot list, or container, Skyrim normally applies only the last loaded plugin\u2014discarding earlier changes or causing the notorious Black Face Bug. Our built-in patcher resolves these conflicts automatically.",
                    DetailsDe = new List<string>
                    {
                        "Wie der Black-Face-Bug entsteht und automatisch behoben wird: Das dunkle oder verf\u00E4rbte Gesicht bei NPCs tritt immer dann auf, wenn eine Mod zwar das Aussehen eines Charakters samt passender 3D-Gesichtsdatei (FaceGen .nif) ver\u00E4ndert, sp\u00E4ter in der Ladereihenfolge aber eine andere Mod (z. B. ein Gameplay-, KI- oder Perks-Overhaul) denselben NPC-Eintrag nochmals \u00FCberschreibt, dabei aber die alten Vanilla-Gesichtsdaten zur\u00FCckschreibt. " +
                        "Unser Mutagen-Patcher durchsucht deine gesamte Ladereihenfolge sowie alle .bsa-Archive nach der letzten g\u00FCltigen FaceGen-Datei eines NPCs und f\u00FChrt deren visuelle Eintr\u00E4ge (HeadParts, TintLayers, FaceMorphs, TextureLighting) nahtlos mit den Gameplay-Werten (Perks, Inventar, KI-Pakete) des Gewinner-Plugins zusammen.",

                        "100 % Kompatibilit\u00E4t mit Synthesis & Wrye Bash: Im Gegensatz zu einfachen \u00DCberschreib-Patches liest unser CompatibleSkyrimPatcher den aktuellen Endzustand deiner gesamten Ladereihenfolge (inklusive bereits vorhandener Patches wie Synthesis.esp oder Bashed Patch, 0.esp) als Basis ein und schreibt das zusammengef\u00FChrte Ergebnis in die Datei AutoDiagPatch.esp (mit platzsparendem ESL-Flag und aktuellem Header 1.71). Dadurch geht keine einzige \u00C4nderung aus Synthesis verloren.",

                        "Verlustfreier Leveled-List- & Container-Merger: F\u00FCgen verschiedene Mods neue Waffen, R\u00FCstungen oder Tr\u00E4nke zu denselben H\u00E4ndlertruhen (CONT) oder Gegner-Beutelisten (LVLI) hinzu, sammelt der Patcher alle Einzeleintr\u00E4ge s\u00E4mtlicher installierter Mods ein und vereint sie vollst\u00E4ndig in der AutoDiagPatch.esp.",

                        "Konfliktfreie SPID-Verteilung (AutoDiag_DISTR.ini): Ist im Konfigurations-Tab die Option f\u00FCr SPID (Spell Perk Item Distributor) aktiviert, erzeugt das Programm beim Patchen zus\u00E4tzlich eine Datei namens AutoDiag_DISTR.ini. " +
                        "\u00DCber diese Datei werden erkannte NPC-Inventar-Erg\u00E4nzungen zur Laufzeit \u00FCber SKSE verteilt, ohne dass daf\u00FCr \u00FCberhaupt ein hartes \u00DCberschreiben im Plugin n\u00F6tig ist."
                    },
                    DetailsEn = new List<string>
                    {
                        "How the Black Face Bug Happens and Gets Fixed Automatically: The dark or discolored face bug occurs when a visual overhaul provides new facial geometry (FaceGen .nif) for an NPC, but a later-loading plugin (such as an AI, combat, or perk overhaul) modifies the same NPC record while reverting its visual entries back to vanilla. " +
                        "Our Mutagen patcher scans both loose files and all .bsa archives to locate the true FaceGen provider and merges its visual data (HeadParts, TintLayers, FaceMorphs, TextureLighting) into the winning gameplay record.",

                        "100% Compatibility with Synthesis & Wrye Bash: Rather than blindly overwriting records from base mods, CompatibleSkyrimPatcher reads the final winning override of your entire load order (including existing patches like Synthesis.esp or Bashed Patch, 0.esp) and writes the merged result into AutoDiagPatch.esp (ESL-flagged with header 1.71). No Synthesis changes are ever lost.",

                        "Lossless Leveled List & Container Merging: When multiple mods add new weapons, armor, or loot to the same merchant chests (CONT) or enemy leveled lists (LVLI), the patcher collects every unique entry across all installed plugins and combines them cleanly inside AutoDiagPatch.esp.",

                        "Zero-Conflict SPID Distribution (AutoDiag_DISTR.ini): When the SPID (Spell Perk Item Distributor) option is enabled in your settings, the patcher additionally generates an AutoDiag_DISTR.ini file to distribute NPC inventory items dynamically at runtime without requiring hard ESP record overrides."
                    },
                    ProTipDe = "Tipp: Da AutoDiagPatch.esp automatisch als Light-Plugin (ESL-Flag mit Header 1.71) markiert wird, belegt der Patch keinen regul\u00E4ren Platz im 254-Plugin-Limit!",
                    ProTipEn = "Pro Tip: Because AutoDiagPatch.esp is automatically ESL-flagged with header 1.71, it does not take up a regular slot in your 254 plugin limit!"
                },
                new ManualChapterEntry
                {
                    Id = "chapter_5_ingame",
                    BadgeDe = "IN-GAME & BACKUP",
                    BadgeEn = "IN-GAME & BACKUP",
                    TitleDe = "5. Sicherheits-Backup vor Spielstart & In-Game Bug-Erfassung per Hotkey",
                    TitleEn = "5. Pre-Launch Safety Backup & In-Game Bug Capture via Hotkey",
                    SummaryDe = "Sobald du Skyrim \u00FCber den gr\u00FCnen Hauptbutton startest, \u00FCbernimmt SkyrimAutoDiag im Hintergrund den Schutz deiner Dateien und wartet auf deine Eingaben w\u00E4hrend des Spielens.",
                    SummaryEn = "As soon as you launch Skyrim via the green main button, SkyrimAutoDiag automatically protects your files and monitors your session in the background.",
                    DetailsDe = new List<string>
                    {
                        "Vollautomatisches .zip-Backup vor jedem Start: Noch bevor die skse64_loader.exe ausgef\u00FChrt wird, erstellt das Programm im Unterordner 'Backup' ein mit Datum und Uhrzeit versehenes Archiv (SkyrimBackup_YYYY-MM-DD_HH-mm-ss.zip). " +
                        "Darin werden deine Konfigurationsdateien (Skyrim.ini, SkyrimPrefs.ini, SkyrimCustom.ini), deine aktuelle Ladereihenfolge (plugins.txt, loadorder.txt), deine AutoDiagPatch.esp sowie dein neuester, garantiert intakter Spielstand (.ess samt nicht-korruptem .skse-Co-Save) gesichert. Sollte eine neue Mod deine Konfiguration oder deinen Spielstand besch\u00E4digen, kannst du jederzeit auf dieses Archiv zur\u00FCckgreifen.",

                        "Visuelle Fehler im laufenden Spiel markieren (More Informative Console): Wenn du w\u00E4hrend des Spielens vor einem Fehler stehst \u2013 etwa einem NPC mit dunklem Gesicht (Black Face Bug), einem lila leuchtenden Gegenstand (fehlende Textur) oder einem gro\u00DFen roten Ausrufezeichen (fehlendes 3D-Mesh) \u2013, \u00F6ffne mit der Taste [^] (links neben der 1) die Skyrim-Konsole. " +
                        "Klicke das fehlerhafte Objekt oder den NPC mit der Maus an, sodass das Zusatzfenster von 'More Informative Console' (MIC) die genaue FormID und die ver\u00E4ndernden Plugins auf dem Bildschirm anzeigt.",

                        "Fokussierter Hotkey mit akustischer Best\u00E4tigung: Dr\u00FCcke nun bei ge\u00F6ffneter Konsole deinen eingestellten Hotkey (z. B. F11). " +
                        "Das Programm pr\u00FCft im Hintergrund zuerst, ob Skyrim tats\u00E4chlich das aktive Fenster im Vordergrund ist (damit au\u00DFerhalb des Spiels keine ungewollten Aufnahmen entstehen), speichert einen hochaufl\u00F6senden Screenshot f\u00FCr die sp\u00E4tere Auswertung nach Spielende und gibt sofort einen kurzen Signalton (Beep) \u00FCber deine Lautsprecher oder Kopfh\u00F6rer aus. Du kannst danach die Konsole schlie\u00DFen und einfach weiterspielen."
                    },
                    DetailsEn = new List<string>
                    {
                        "Automated Pre-Launch .zip Backup: Right before skse64_loader.exe starts, the tool creates a timestamped archive (SkyrimBackup_YYYY-MM-DD_HH-mm-ss.zip) inside the 'Backup' folder. " +
                        "This archive preserves your INI files (Skyrim.ini, SkyrimPrefs.ini, SkyrimCustom.ini), your active load order (plugins.txt, loadorder.txt), your AutoDiagPatch.esp, and your latest verified working savegame (.ess plus non-corrupted .skse co-save).",

                        "Marking Visual Bugs In-Game (More Informative Console): Whenever you encounter a visual glitch while playing\u2014such as an NPC with a dark face, a purple object missing its texture, or a red exclamation mark indicating a missing mesh\u2014open the in-game console using the [~] key. " +
                        "Click on the affected NPC or object with your mouse so that 'More Informative Console' (MIC) displays its Base FormID and modifying plugins on screen.",

                        "Focused Hotkey with Audio Confirmation: With the console open, press your configured screenshot hotkey (e.g., F11). " +
                        "The watcher verifies that Skyrim is the active foreground window, saves the screenshot for post-game analysis, and plays an immediate confirmation beep through your speakers so you know the bug was recorded without leaving fullscreen mode."
                    },
                    ProTipDe = "Tipp: Der Screenshot-Hotkey l\u00F6st nur dann aus, wenn Skyrim oder SkyrimAutoDiag das aktive Fenster im Vordergrund ist!",
                    ProTipEn = "Pro Tip: The screenshot hotkey only triggers when Skyrim or SkyrimAutoDiag is the active foreground window!"
                },
                new ManualChapterEntry
                {
                    Id = "chapter_6_reports",
                    BadgeDe = "BERICHTE & ONLINE-SUCHE",
                    BadgeEn = "REPORTS & ONLINE SEARCH",
                    TitleDe = "6. Berichte-Ordner, Discord-Export & \uD83C\uDF10 Online-L\u00F6sungssuche",
                    TitleEn = "6. Reports Folder, Discord Export & \uD83C\uDF10 Online Solution Finder",
                    SummaryDe = "Nach dem Beenden von Skyrim (egal ob regul\u00E4r oder durch einen Crash to Desktop) werden alle gesammelten Daten ausgewertet, \u00FCbersichtlich im Diagnose-Tab aufbereitet und zus\u00E4tzlich als dauerhafte Berichte gespeichert.",
                    SummaryEn = "After exiting Skyrim (whether normally or due to a Crash to Desktop), all collected telemetry is analyzed, presented in the Diagnostics tab, and archived as permanent HTML reports.",
                    DetailsDe = new List<string>
                    {
                        "Zweisprachige HTML-Berichte im Ordner 'Berichte': Nach jeder Sitzung generiert das Programm im Unterordner 'Berichte' automatisch zwei komplett gestaltete HTML-Dokumente im dunklen Skyrim-Design \u2013 eines auf Deutsch (DiagnoseBericht_DE_...) und eines auf Englisch (DiagnosticReport_EN_...). " +
                        "Diese Berichte enthalten die Gesamtzusammenfassung, alle erkannten Fehlerkarten, eingebettete Tool-Workflows sowie deine im Spiel aufgenommenen Screenshots und lassen sich jederzeit im Webbrowser \u00F6ffnen oder archivieren.",

                        "Anonymisierter Export f\u00FCr Discord & Reddit: Wenn du in Modding-Communities wie r/skyrimmods oder auf Discord-Servern (z. B. Nolvus oder Lorerim) um Rat fragen m\u00F6chtest, klicke links auf 'Bericht f\u00FCr Discord / Reddit kopieren'. " +
                        "Das Programm erstellt eine \u00FCbersichtlich formatierte Markdown-Zusammenfassung deiner Skyrim-Version, der erkannten Absturzursache und der wichtigsten L\u00F6sungsschritte in deiner Zwischenablage und ersetzt dabei aus Datenschutzgr\u00FCnden automatisch deinen pers\u00F6nlichen Windows-Benutzernamen in allen Dateipfaden durch '<User>'.",

                        "Arbeiten mit dem Tab '\uD83C\uDF10 Online-L\u00F6sungssuche': Sollte ein seltener Fehler auftreten (oder ein allgemeines Gameplay-, Quest- oder Grafikproblem), \u00F6ffne oben rechts den Reiter '\uD83C\uDF10 Online-L\u00F6sungssuche'. " +
                        "Dort kannst du dein Problem frei in eigenen Worten, als Fehlercode (z. B. SkyrimSE.exe+0D6DDDA) oder Mod-Namen beschreiben. Das Programm durchsucht daraufhin gleichzeitig unsere lokale Wissensdatenbank, live das Reddit-Forum r/skyrimmods (komplett ohne API-Key) sowie optional \u00FCber Gemini aktuelle Forenbeitr\u00E4ge und l\u00E4sst dich die Ergebnisse per Klick auch als HTML-Bericht speichern."
                    },
                    DetailsEn = new List<string>
                    {
                        "Bilingual HTML Reports in the 'Berichte' Folder: Following every game session, the application automatically generates two styled dark-mode HTML documents inside the 'Berichte' folder\u2014one in German and one in English. " +
                        "These self-contained reports include the session summary, all diagnosed issues, embedded tool workflows, and your in-game screenshots.",

                        "Anonymized Export for Discord & Reddit: If you need help from modding communities like r/skyrimmods or Discord servers, click 'Copy Report for Discord / Reddit' on the left. " +
                        "This copies a cleanly formatted Markdown summary to your clipboard while automatically replacing your personal Windows username in all file paths with '<User>' for privacy.",

                        "Using the '\uD83C\uDF10 Online Solution Finder' Tab: For any gameplay, quest, or rare modding bug, switch to the '\uD83C\uDF10 Online Solution Finder' tab in the top right. " +
                        "Describe your issue in your own words, enter a crash offset (e.g., SkyrimSE.exe+0D6DDDA), or type a mod name to simultaneously search the local database, live Reddit threads on r/skyrimmods (no API key needed), and optional AI web summaries. You can also export these search results as a permanent HTML report."
                    },
                    ProTipDe = "Tipp: Die Live-Suche auf Reddit r/skyrimmods im Reiter '\uD83C\uDF10 Online-L\u00F6sungssuche' funktioniert komplett ohne API-Key!",
                    ProTipEn = "Pro Tip: The live Reddit r/skyrimmods search inside the '\uD83C\uDF10 Online Solution Finder' tab works completely without an API key!"
                },
                new ManualChapterEntry
                {
                    Id = "chapter_7_workflows",
                    BadgeDe = "TOOL-WORKFLOWS im DETAIL",
                    BadgeEn = "DETAILED TOOL WORKFLOWS",
                    TitleDe = "7. Detaillierte Tool-Workflows & Manuelle Reparatur-Abl\u00E4ufe (Schritt f\u00FCr Schritt)",
                    TitleEn = "7. Detailed Tool Workflows & Manual Repair Procedures (Step by Step)",
                    SummaryDe = "Rund 90 % aller typischen Probleme (wie Black-Face-Bugs, Leveled-List-Konflikte oder alte ESL-Header) behebt SkyrimAutoDiag per Knopfdruck selbst. F\u00FCr Spezialf\u00E4lle (wie gel\u00F6schte NavMeshes, schmutzige Plugins oder neue Animationen) findest du hier jeden einzelnen Tool-Workflow ausf\u00FChrlich erkl\u00E4rt.",
                    SummaryEn = "While SkyrimAutoDiag resolves roughly 90% of common conflicts automatically with one click, specialized engine issues (such as deleted NavMeshes, dirty edits, or behavior animations) require external modding utilities. Every built-in tool workflow is explained in detail below.",
                    DetailsDe = new List<string>
                    {
                        "Workflow 1 \u2013 Automatischer In-App-Reparaturablauf (ESL 1.71 & Mutagen Auto-Patch): Bevor du externe Programme \u00F6ffnest, f\u00FChre nach \u00C4nderungen an deiner Modliste immer zuerst die internen Reparatur-Schritte in der linken Werkzeugleiste aus: " +
                        "Klicke zuerst auf 'SKSE, Save, DDS & Load Order pr\u00FCfen'. Meldet der Scanner veraltete ESL-Header (< 1.71), klicke direkt auf den lila Button 'ESL-Header (< 1.71) automatisch reparieren'. " +
                        "Klicke anschlie\u00DFend oben links auf 'Kompatibilit\u00E4ts-Patch + SPID erstellen'. Dadurch werden die Dateien AutoDiagPatch.esp und AutoDiag_DISTR.ini in deinem Data-Ordner aktualisiert. Stelle sicher, dass AutoDiagPatch.esp in deinem Mod-Manager aktiviert ist und ganz am Ende deiner Ladereihenfolge (auch nach Synthesis.esp) steht.",

                        "Workflow 2 \u2013 SSEEdit Quick Auto Clean (Dirty Plugins, ITMs & UDRs bereinigen): Wenn LOOT oder unsere Diagnose meldet, dass eine Mod sogenannte 'Identical to Master'-Eintr\u00E4ge (ITMs, also unn\u00F6tige identische \u00DCberschreibungen aus dem Grundspiel) oder 'Undeleted and Disabled References' (UDRs, also hart gel\u00F6schte Objekte, die beim Betreten einer Zelle zum sofortigen Crash f\u00FChren) enth\u00E4lt, klicke links im Tool-Hub auf den gr\u00FCnen Button 'SSEEdit Quick Auto Clean'. " +
                        "Das Programm startet deine hinterlegte SSEEditQAC.exe (bzw. SSEEdit.exe) automatisch im speziellen Reinigungsmodus (-quickautoclean) mit deinem korrekten Skyrim-Data-Pfad. Es \u00F6ffnet sich ein Auswahlfenster: Klicke dort mit der rechten Maustaste in die Liste, w\u00E4hle 'Select None' (Keine ausw\u00E4hlen), setze das H\u00E4kchen ausschlie\u00DFlich bei dem betroffenen Plugin und klicke auf 'OK'. SSEEdit entfernt daraufhin vollautomatisch alle ITMs, wandelt gel\u00F6schte Referenzen in sichere deaktivierte Objekte ('Initially Disabled') um und speichert das bereinigte Plugin beim Schlie\u00DFen des Fensters ab.",

                        "Workflow 3 \u2013 SSEEdit Zielgenaue Konflikt- & NavMesh-Reparatur: Wenn in einer Fehlerkarte ein spezifischer Record-Konflikt oder ein NavMesh-Absturz (Wegfindungs-Crash der KI durch ein gel\u00F6schtes Navigation Mesh) gemeldet wird, klicke direkt unter der Fehlermeldung auf den blauen Button 'In SSEEdit pr\u00FCfen' sowie auf den orangen Button '\uD83D\uDCD8 Tool-Workflow (HTML) \u00F6ffnen'. " +
                        "Dadurch \u00F6ffnet sich in deinem Webbrowser eine abhakbare Schritt-f\u00FCr-Schritt-Checkliste mit deiner exakt vorausgef\u00FCllten FormID und dem Plugin-Namen, w\u00E4hrend SSEEdit bereits vorgefiltert startet. Warte in SSEEdit, bis unten im Protokoll 'Background Loader: finished' steht. Kopiere nun die 8-stellige FormID oben links in das Suchfeld 'FormID' von SSEEdit und dr\u00FCcke Enter. Im rechten Fenster siehst du alle Mods nebeneinander, die diesen Datensatz ver\u00E4ndern (rot hinterlegte Zeilen zeigen Konflikte an). Bei einem gel\u00F6schten NavMesh ('Deleted'-Flag) kannst du entweder Quick Auto Clean \u00FCber das Plugin laufen lassen oder die Ersatz-NavMesh-FormID der Mod per Rechtsklick -> 'Change FormID' auf die originale Vanilla-FormID umbiegen und mit Strg + S speichern.",

                        "Workflow 4 \u2013 Pandora / Nemesis Behaviour Engine (Animations-Crashes & T-Pose beheben): Wenn du neue Kampf-, Bewegungs- oder Kreaturen-Animationen (z. B. MCO, Precision, True Directional Movement) installiert hast oder der Diagnosebericht einen Absturz in einer .hkx-Datei bzw. im 'hkbBehaviorGraph' meldet (im Spiel oft daran erkennbar, dass Charaktere mit starr ausgestreckten Armen in der sogenannten T-Pose einfrieren), muss der Animations-Graph neu kompiliert werden. " +
                        "Klicke links im Tool-Hub auf 'Pandora / Nemesis Engine'. Pr\u00FCfe im ge\u00F6ffneten Fenster, ob alle von dir installierten Animations-Mods in der Liste angehakt sind. Nutzt du Nemesis, klicke zuerst auf 'Update Engine' und warte auf 100 %. Klicke anschlie\u00DFend auf 'Launch Pandora' bzw. 'Launch Nemesis Behavior Engine', warte bis der Durchlauf mit '0 Errors' abgeschlossen ist, und schlie\u00DFe das Programm erst danach.",

                        "Workflow 5 \u2013 BodySlide x64 (Defekte 3D-R\u00FCstungs-Meshes & SMP-Physik-Abst\u00FCrze reparieren): St\u00FCrzt das Spiel in der N\u00E4he bestimmter NPCs oder beim Anlegen einer R\u00FCstung mit einem Fehler in 'hdtSMP64.dll' (Faster HDT-SMP Physik), 'BSDynamicTriShape' oder 'BSTriShape' (+0D6DDDA) ab, oder sind K\u00F6rperteile im Spiel unsichtbar, passen die 3D-Modelle (.nif) nicht zum installierten K\u00F6rpertyp. " +
                        "Klicke links im Tool-Hub auf 'BodySlide x64'. W\u00E4hle oben links unter 'Outfit/Body' deinen installierten K\u00F6rper (z. B. CBBE 3BA oder BHUNP) und direkt darunter unter 'Preset' dein gew\u00FCnschtes K\u00F6rper-Preset aus. Setze unten links unbedingt das H\u00E4kchen bei 'Build Morphs' (damit RaceMenu- und OBody-K\u00F6rperregler im Spiel funktionieren) und klicke unten auf 'Batch Build...'. Best\u00E4tige das Auswahlfenster mit 'Build', damit BodySlide alle R\u00FCstungs-.nif-Dateien sauber f\u00FCr deine Ladereihenfolge neu generiert.",

                        "Workflow 6 \u2013 LOOT & Wrye Bash (Fehlende Master-Dateien & Ladereihenfolge optimieren): St\u00FCrzt Skyrim bereits wenige Sekunden nach dem Start noch vor dem Hauptmen\u00FC ab (oft mit Offset +0198090 oder +05E1F22), fehlt einem aktiven Plugin eine vorausgesetzte Master-Datei (.esm/.esp), oder Mods werden in der falschen Reihenfolge geladen. " +
                        "Klicke links im Tool-Hub auf 'LOOT (Load Order)'. Klicke oben in LOOT auf 'Plugins sortieren' ('Sort Plugins') und pr\u00FCfe die roten Fehlerkarten ('Errors') in der Hauptansicht: Dort steht exakt, welchem Plugin welche Master-Mod fehlt. Nachdem du die fehlende Mod aktiviert hast, klicke oben in LOOT auf '\u00DCbernehmen' ('Apply'), damit die sortierte Reihenfolge in deine plugins.txt geschrieben wird. Achte darauf, dass deine AutoDiagPatch.esp auch nach dem Sortieren ganz unten am Ende deiner Ladereihenfolge bleibt.",

                        "Workflow 7 \u2013 BethINI Pie & Windows Pagefile (VRAM-\u00DCberlauf, Treiber-Resets & Schatten-Crashes l\u00F6sen): Meldet der Diagnosebericht einen Grafik- oder Speichercrash in 'd3d11.dll', 'nvwgf2umx.dll' (NVIDIA), 'amdxx64.dll' (AMD) oder im 'ShadowSceneNode' / 'BSLightingShaderProperty', sto\u00DFen entweder die Schattenberechnung der Engine oder der virtuelle Arbeitsspeicher an ihre Grenzen. " +
                        "Schlie\u00DFe Skyrim und klicke links im Tool-Hub auf 'BethINI Pie'. W\u00E4hle ein passendes Preset (z. B. High), aktiviere 'Recommended Tweaks' und senke bei 'ShadowSceneNode'-Crashes die Schattenaufl\u00F6sung ('Shadow Resolution') von 4096 auf 2048. Speichere die Einstellungen in BethINI ab. Zus\u00E4tzlich solltest du bei stark gemoddeten Installationen in den erweiterten Windows-Systemeinstellungen auf deiner schnellsten SSD eine feste Auslagerungsdatei (Pagefile) von mindestens 20.480 MB (20 GB) bis 40.960 MB (40 GB) einstellen, um pl\u00F6tzliche Speicher-Abst\u00FCrze dauerhaft zu verhindern."
                    },
                    DetailsEn = new List<string>
                    {
                        "Workflow 1 \u2013 Automated In-App Repair Sequence (ESL 1.71 & Mutagen Auto-Patch): Whenever you install or remove mods, always run the internal repair steps in the left sidebar first before opening external utilities: " +
                        "Click 'Scan SKSE, Save, DDS & Load Order'. If outdated ESL headers (< 1.71) are detected, click the purple 'Auto-Fix ESL Headers (< 1.71)' button right below it. " +
                        "Next, click 'Build Compatibility Patch + SPID' at the top left to regenerate AutoDiagPatch.esp and AutoDiag_DISTR.ini inside your Data folder. Ensure AutoDiagPatch.esp is enabled in your mod manager and placed at the very bottom of your load order (below Synthesis.esp).",

                        "Workflow 2 \u2013 SSEEdit Quick Auto Clean (Cleaning Dirty Plugins, ITMs & UDRs): When LOOT or our diagnostic scan reports that a plugin contains 'Identical to Master' (ITM) records or 'Undeleted and Disabled References' (UDRs, which cause instant crashes when entering a cell), click the green 'SSEEdit Quick Auto Clean' button in the left Tool Hub. " +
                        "This launches SSEEditQAC.exe automatically with the -quickautoclean parameter and your exact Skyrim Data path. In the plugin selection dialog, right-click the list -> 'Select None', check the box ONLY for the reported plugin, and click 'OK'. SSEEdit will automatically strip redundant ITM overrides, convert deleted references into safe disabled records ('Initially Disabled'), and save the cleaned plugin when you close the window.",

                        "Workflow 3 \u2013 SSEEdit Targeted Conflict & NavMesh Repair: When a diagnostic issue card reports a specific record conflict or a NavMesh AI pathfinding crash, click the blue 'Inspect in SSEEdit' button and the orange '\uD83D\uDCD8 Open Tool Guide (HTML)' button right on the issue card. " +
                        "This opens an interactive HTML checklist in your browser pre-filled with the exact FormID and plugin name while launching SSEEdit filtered to the causing plugin. Wait until SSEEdit says 'Background Loader: finished' at the bottom, paste the 8-digit FormID into the top-left 'FormID' search box, and press Enter. The right pane displays all plugins modifying that record side by side (conflicts are highlighted in red). Resolve broken overrides or run Quick Auto Clean on the plugin, then press Ctrl + S to save.",

                        "Workflow 4 \u2013 Pandora / Nemesis Behaviour Engine (Fixing Animation Crashes & T-Posing): After installing new combat, movement, or creature animation mods (e.g., MCO, Precision, True Directional Movement)\u2014or if a crash log points to a corrupted .hkx file or 'hkbBehaviorGraph' (often accompanied by NPCs stuck in a stiff T-pose in-game)\u2014you must rebuild your animation behavior graph. " +
                        "Click 'Pandora / Nemesis Engine' in the left Tool Hub. Verify that all your installed animation mods are checked in the list. If using Nemesis, click 'Update Engine' first and wait for 100%. Then click 'Launch Pandora' or 'Launch Nemesis Behavior Engine', wait for the process to finish with '0 Errors', and close the window.",

                        "Workflow 5 \u2013 BodySlide x64 (Rebuilding Armor Meshes & Fixing SMP Physics Crashes): If Skyrim crashes near certain NPCs or when equipping outfits with an error in 'hdtSMP64.dll' (Faster HDT-SMP physics), 'BSDynamicTriShape', or 'BSTriShape' (+0D6DDDA), or if body parts turn invisible, your 3D outfit meshes (.nif) need to be rebuilt. " +
                        "Click 'BodySlide x64' in the left Tool Hub. Select your installed body type under 'Outfit/Body' (e.g., CBBE 3BA or BHUNP) and your desired shape under 'Preset' in the top left. Check the 'Build Morphs' box at the bottom left, click 'Batch Build...', and confirm with 'Build' to generate clean .nif files for your entire load order.",

                        "Workflow 6 \u2013 LOOT & Wrye Bash (Fixing Missing Masters & Optimizing Load Order): If Skyrim crashes a few seconds after launch before reaching the main menu (often with offset +0198090 or +05E1F22), an active plugin is missing a required master file (.esm/.esp) or your load order is out of sequence. " +
                        "Click 'LOOT (Load Order)' in the left Tool Hub, click 'Sort Plugins' at the top, and check any red 'Error' cards in the main view to see exactly which master file is missing. After enabling the missing master mod, click 'Apply' in LOOT to write the sorted order to your plugins.txt. Make sure AutoDiagPatch.esp remains at the very bottom of your load order.",

                        "Workflow 7 \u2013 BethINI Pie & Windows Pagefile (Resolving VRAM Exhaustion, Driver Resets & Shadow Crashes): If your diagnostic report indicates crashes in 'd3d11.dll', 'nvwgf2umx.dll' (NVIDIA), 'amdxx64.dll' (AMD), or 'ShadowSceneNode' / 'BSLightingShaderProperty', your shadow pipeline or virtual memory is overloaded. " +
                        "Close Skyrim and click 'BethINI Pie' in the left Tool Hub. Select an appropriate preset (e.g., High), check 'Recommended Tweaks', and lower 'Shadow Resolution' from 4096 to 2048 if you experience ShadowSceneNode crashes. Save your changes in BethINI. Additionally, for heavily modded setups, open Windows Advanced System Settings and configure a custom Pagefile of 20,480 MB (20 GB) to 40,960 MB (40 GB) on your fastest SSD to prevent out-of-memory crashes."
                    },
                    ProTipDe = "Tipp: Du kannst sowohl dieses Handbuch ('user_manual.json') als auch die interaktiven HTML-Tool-Anleitungen ('tool_workflows.json') im Ordner 'Datenbanken' jederzeit mit einem Texteditor beliebig erweitern!",
                    ProTipEn = "Pro Tip: Both this user manual ('user_manual.json') and the interactive HTML tool checklists ('tool_workflows.json') are stored as plain JSON files in your 'Datenbanken' folder and can be customized anytime!"
                }
            };
        }
    }
}