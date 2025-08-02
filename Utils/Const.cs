using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace HK4E.HdiffBuilder.Utils
{
    public static class Const
    {
        public static readonly string ConfigPath = "config.json";
        public static bool ConfigCreated = false;

        public static string OldVer;
        public static string NewVer;
        public static int Mode;
        public static int MaxThreads;
        public static bool KeepSourceFolder;
        public static string LogLevel;

        public static readonly string[] GameRootDirs = { "GenshinImpact", "Genshin", "YuanShen" };
        public static readonly string[] GameDataDirs = { "GenshinImpact_Data", "Genshin_Data", "YuanShen_Data" };

        public static string ActiveGameRoot { get; private set; }
        public static string ActiveGameData { get; private set; }

        public static readonly Dictionary<string, string> AudioLanguages = new()
        {
            ["English(US)"] = "audio_en-us",
            ["Japanese"]    = "audio_ja-jp",
            ["Korean"]      = "audio_ko-kr",
            ["Chinese"]     = "audio_zh-cn"
        };

        public static readonly HashSet<string> VersionWhitelist = new()
        {
            "0.7.0", "0.7.1", "1.0.1", "1.3.1", "1.3.2",
            "1.4.1", "1.5.1", "1.6.1", "4.0.1"
        };

        public static string OldBase => $"{ActiveGameRoot}_{OldVer}";
        public static string NewBase => $"{ActiveGameRoot}_{NewVer}";

        public static bool RunGameDiff = true;
        public static readonly Dictionary<string, bool> RunAudioDiff = new();

        #nullable disable
        static Const()
        {
            int logicalCoreCount = Math.Max(1, Environment.ProcessorCount / 2);

            var defaultConfig = new Dictionary<string, object>
            {
                ["old_ver"]           = "5.5.0",
                ["new_ver"]           = "5.6.0",
                ["mode"]              = 0,
                ["max_threads"]       = logicalCoreCount,
                ["keep_source_folder"] = false,
                ["log_level"]         = "DEBUG",
                ["game"]              = true,
                ["audio_en-us"]       = true,
                ["audio_ja-jp"]       = true,
                ["audio_ko-kr"]       = true,
                ["audio_zh-cn"]       = true
            };

            if (!File.Exists(ConfigPath))
            {
                var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                ConfigCreated = true;

                LogLevel = "INFO";
                Logger.Init();
                Logger.Warning("config.json not found. Created with default values.");
                Logger.Hint("Please check and edit config.json");

                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();
                }
                Environment.Exit(0);
            }

            Dictionary<string, JsonElement> temp = null;
            var validationErrors = new List<string>();

            try
            {
                var configText = File.ReadAllText(ConfigPath).Trim();

                if (string.IsNullOrWhiteSpace(configText) || configText == "null")
                {
                    validationErrors.Add("config.json is malformed. JSON content is null.");
                }
                else
                {
                    temp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configText);
                    if (temp == null)
                        validationErrors.Add("config.json is malformed. JSON content is null.");
                }
            }
            catch
            {
                validationErrors.Add("config.json is malformed. JSON syntax is invalid.");
            }

            LogLevel = temp != null && temp.TryGetValue("log_level", out var levelVal)
                && levelVal.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(levelVal.GetString())
                    ? levelVal.GetString().Trim().ToUpperInvariant()
                    : "INFO";

            Logger.Init();

            if (validationErrors.Count > 0)
            {
                foreach (var err in validationErrors)
                    Logger.Fatal(err);

                Logger.Hint("Please check and edit config.json");
                PauseExit();
            }

            var config = temp;
            ReadConfigValues(config, validationErrors);

            if (validationErrors.Count == 0)
            {
                var oldTuple = ValidateVersion(OldVer, validationErrors);
                var newTuple = ValidateVersion(NewVer, validationErrors);

                if (OldVer == NewVer)
                    validationErrors.Add("'old_ver' and 'new_ver' cannot be the same.");
                else if (CompareTuple(oldTuple, newTuple) >= 0)
                    validationErrors.Add("'old_ver' must be lower than 'new_ver'.");
            }

            if (validationErrors.Count > 0)
            {
                foreach (var err in validationErrors)
                    Logger.Fatal(err);

                Logger.Hint("Please check and edit config.json");
                PauseExit();
            }

            for (int i = 0; i < GameRootDirs.Length; i++)
            {
                string oldBase = $"{GameRootDirs[i]}_{OldVer}";
                string newBase = $"{GameRootDirs[i]}_{NewVer}";

                if (Directory.Exists(oldBase) && Directory.Exists(newBase))
                {
                    ActiveGameRoot = GameRootDirs[i];
                    ActiveGameData = GameDataDirs[i];
                    break;
                }
            }

            if (ActiveGameRoot == null)
            {
                Logger.Error($"None of the expected game folders found for versions {OldVer} and {NewVer}");
                Logger.Hint($"Make sure one of these folders exist: {string.Join(", ", GameRootDirs.Select(x => $"{x}_{OldVer}"))}");
                PauseExit();
            }

            RunGameDiff = config.TryGetValue("game", out var gameVal) && gameVal.ValueKind == JsonValueKind.True;

            foreach (var pair in AudioLanguages)
            {
                string key = pair.Value;
                RunAudioDiff[pair.Key] = config.TryGetValue(key, out var val) && val.ValueKind == JsonValueKind.True;
            }
        }

        private static void ReadConfigValues(Dictionary<string, JsonElement> config, List<string> errors)
        {
            if (!config.TryGetValue("old_ver", out var oldVal) || oldVal.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(oldVal.GetString()))
                errors.Add("'old_ver' is missing or not a valid string.");
            else
                OldVer = oldVal.GetString().Trim();

            if (!config.TryGetValue("new_ver", out var newVal) || newVal.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(newVal.GetString()))
                errors.Add("'new_ver' is missing or not a valid string.");
            else
                NewVer = newVal.GetString().Trim();

            if (!config.TryGetValue("mode", out var modeVal) || modeVal.ValueKind != JsonValueKind.Number)
                errors.Add("'mode' is missing or not a valid number.");
            else
                Mode = modeVal.GetInt32();

            if (!config.TryGetValue("max_threads", out var threadVal) || threadVal.ValueKind != JsonValueKind.Number)
            {
                errors.Add("'max_threads' is missing or not a valid number.");
            }
            else
            {
                MaxThreads = threadVal.GetInt32();
                if (MaxThreads < 1 || MaxThreads > Environment.ProcessorCount)
                    errors.Add($"'max_threads' must be an integer between 1 and {Environment.ProcessorCount}, based on your CPU.");
            }

            if (!config.TryGetValue("keep_source_folder", out var keepVal))
            {
                errors.Add("'keep_source_folder' is missing.");
            }
            else if (keepVal.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                errors.Add("'keep_source_folder' must be true or false (without quotes).");
            }
            else
            {
                KeepSourceFolder = keepVal.ValueKind == JsonValueKind.True;
            }

            if (Mode is not (0 or 1))
                errors.Add("'mode' must be 0 (sequential) or 1 (parallel).");
        }

        private static Tuple<int, int, int> ValidateVersion(string ver, List<string> errors)
        {
            var match = Regex.Match(ver, @"^(\d+)\.(\d+)\.(\d+)$");

            if (!match.Success)
            {
                errors.Add($"Invalid version format '{ver}'. Use format like 5.6.0");
                return Tuple.Create(0, 0, 0);
            }

            int x = int.Parse(match.Groups[1].Value);
            int y = int.Parse(match.Groups[2].Value);
            int z = int.Parse(match.Groups[3].Value);
            string full = $"{x}.{y}.{z}";

            if (VersionWhitelist.Contains(full))
                return Tuple.Create(x, y, z);

            if (x == 0)
            {
                if (y == 9)
                {
                    if (z < 0 || z > 20)
                        errors.Add($"Invalid version '{ver}' not allowed.");
                }
                else if (y != 7)
                {
                    errors.Add($"Invalid version '{ver}' not allowed.");
                }
            }
            else if (x == 1)
            {
                if (y < 0 || y > 6)
                    errors.Add($"Invalid version '{ver}' not allowed.");
            }
            else
            {
                if (y > 8)
                    errors.Add($"Invalid version '{ver}' not allowed.");
            }

            if (!(x == 0 && y == 9) && !new HashSet<int> { 0, 50, 51, 52, 53, 54, 55 }.Contains(z))
                errors.Add($"Invalid version '{ver}' not allowed.");

            return Tuple.Create(x, y, z);
        }

        private static int CompareTuple(Tuple<int, int, int> a, Tuple<int, int, int> b)
        {
            int cmp = a.Item1.CompareTo(b.Item1);
            if (cmp != 0) return cmp;

            cmp = a.Item2.CompareTo(b.Item2);
            if (cmp != 0) return cmp;

            return a.Item3.CompareTo(b.Item3);
        }

        private static void PauseExit()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
            Environment.Exit(1);
        }

        public static (string game, Dictionary<string, string> audio) GetDirs()
        {
            string game = $"game_{OldVer}_{NewVer}_hdiff";
            var audio = new Dictionary<string, string>();

            foreach (var pair in AudioLanguages)
                audio[pair.Key] = $"{pair.Value}_{OldVer}_{NewVer}_hdiff";

            return (game, audio);
        }

        public static bool ValidateDirs()
        {
            bool hasError = false;

            if (!Directory.Exists(OldBase))
            {
                Logger.Error($"Folder not found: {OldBase}");
                hasError = true;
            }

            if (!Directory.Exists(NewBase))
            {
                Logger.Error($"Folder not found: {NewBase}");
                hasError = true;
            }

            return !hasError;
        }
    }
}
