using System;
using System.IO;
using System.Collections.Generic;
using HK4E.HdiffBuilder.Utils;

namespace HK4E.HdiffBuilder.Core
{
    public static class Delete
    {
        public static void RunDel()
        {
            Logger.Info($"Checking deleted files between {Const.OldVer} -> {Const.NewVer}");

            var (updateFolder, outputAudio) = Const.GetDirs();

            if (Const.RunGameDiff)
            {
                var deleted = FindDeletedFiles(Const.OldBase, Const.NewBase, skipAudio: true);
                SaveList(deleted, updateFolder);
            }

            foreach (var lang in Const.AudioLanguages.Keys)
            {
                if (!Const.RunAudioDiff.TryGetValue(lang, out bool enabled) || !enabled)
                    continue;

                foreach (var gameDir in Const.GameDataDirs)
                {
                    string oldAssets = Path.Combine(Const.OldBase, gameDir, "StreamingAssets", "AudioAssets", lang);
                    string newAssets = Path.Combine(Const.NewBase, gameDir, "StreamingAssets", "AudioAssets", lang);
                    string prefixAssets = $"{gameDir}/StreamingAssets/AudioAssets/{lang}/";

                    if (Directory.Exists(oldAssets))
                    {
                        var deleted = FindDeletedFiles(oldAssets, newAssets, prefixAssets);
                        SaveList(deleted, outputAudio[lang]);
                    }

                    string oldGen = Path.Combine(Const.OldBase, gameDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", lang);
                    string newGen = Path.Combine(Const.NewBase, gameDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", lang);
                    string prefixGen = $"{gameDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows/{lang}/";

                    if (Directory.Exists(oldGen))
                    {
                        var deleted = FindDeletedFiles(oldGen, newGen, prefixGen);
                        SaveList(deleted, outputAudio[lang]);
                    }
                }
            }

            Logger.Done("deletefiles.txt has been successfully generated.\n");
        }

        private static List<string> FindDeletedFiles(string oldRoot, string newRoot, string relPrefix = "", bool skipAudio = false)
        {
            List<string> deleted = new();

            foreach (var oldFile in Directory.EnumerateFiles(oldRoot, "*", SearchOption.AllDirectories))
            {
                string relPath = Path.GetRelativePath(oldRoot, oldFile).Replace("\\", "/");
                string fullRelPath = string.IsNullOrEmpty(relPrefix) ? relPath : $"{relPrefix}{relPath}";

                if (skipAudio && IsAudio(fullRelPath))
                    continue;

                string newPath = Path.Combine(newRoot, relPath);
                if (!File.Exists(newPath))
                    deleted.Add(fullRelPath);
            }

            return deleted;
        }

        private static void SaveList(List<string> list, string outputDir)
        {
            if (list.Count == 0)
                return;

            Directory.CreateDirectory(outputDir);
            string outPath = Path.Combine(outputDir, "deletefiles.txt");
            File.WriteAllLines(outPath, list);
            Logger.Info($"Create {outPath} ({list.Count} files)");
        }

        private static bool IsAudio(string relPath)
        {
            foreach (var gameDir in Const.GameDataDirs)
            {
                foreach (var lang in Const.AudioLanguages.Keys)
                {
                    if (
                        relPath.StartsWith($"{gameDir}/StreamingAssets/AudioAssets/{lang}/") ||
                        relPath.StartsWith($"{gameDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows/{lang}/")
                    )
                        return true;
                }
            }
            return false;
        }
    }
}
