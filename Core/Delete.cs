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
            bool IsAudio(string path)
            {
                foreach (var gameDir in Const.GameDataDirs)
                {
                    foreach (var lang in Const.AudioLanguages.Keys)
                    {
                        if (
                            path.StartsWith($"{gameDir}/StreamingAssets/AudioAssets/{lang}/") ||
                            path.StartsWith($"{gameDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows/{lang}/")
                        )
                            return true;
                    }
                }
                return false;
            }

            bool FileExistsWithFallback(string relPath, string newRoot)
            {
                string path1 = Path.Combine(newRoot, relPath);
                if (File.Exists(path1))
                    return true;

                string alt1 = relPath.Replace("AudioAssets", "Audio/GeneratedSoundBanks/Windows");
                if (alt1 != relPath)
                {
                    string path2 = Path.Combine(newRoot, alt1);
                    if (File.Exists(path2))
                        return true;
                }

                string alt2 = relPath.Replace("Audio/GeneratedSoundBanks/Windows", "AudioAssets");
                if (alt2 != relPath)
                {
                    string path3 = Path.Combine(newRoot, alt2);
                    if (File.Exists(path3))
                        return true;
                }

                return false;
            }

            List<string> Find(string oldRoot, string newRoot, string relPrefix = "", bool skipAudio = false)
            {
                List<string> deleted = new();

                foreach (var oldFile in Directory.EnumerateFiles(oldRoot, "*", SearchOption.AllDirectories))
                {
                    string relPath = Path.GetRelativePath(oldRoot, oldFile).Replace("\\", "/");
                    string fullRelPath = string.IsNullOrEmpty(relPrefix) ? relPath : $"{relPrefix}{relPath}";

                    if (skipAudio && IsAudio(fullRelPath))
                        continue;

                    if (!FileExistsWithFallback(fullRelPath, newRoot))
                        deleted.Add(fullRelPath);
                }

                return deleted;
            }

            void Save(List<string> deleteList, string outputDir)
            {
                if (deleteList.Count == 0)
                    return;

                Directory.CreateDirectory(outputDir);
                string outputPath = Path.Combine(outputDir, "deletefiles.txt");
                File.WriteAllLines(outputPath, deleteList);
                Logger.Info($"Create {outputPath} ({deleteList.Count} files)");
            }

            Logger.Info($"Checking deleted files between {Const.OldVer} -> {Const.NewVer}");

            var (updateFolder, outputAudio) = Const.GetDirs();

            var deletedGame = Find(Const.OldBase, Const.NewBase, "", skipAudio: true);
            Save(deletedGame, updateFolder);

            foreach (var lang in Const.AudioLanguages.Keys)
            {
                foreach (var gameDir in Const.GameDataDirs)
                {
                    string relPrefixAssets = $"{gameDir}/StreamingAssets/AudioAssets/{lang}/";
                    string oldAssetsPath = Path.Combine(Const.OldBase, gameDir, "StreamingAssets", "AudioAssets", lang);
                    string newAssetsPath = Path.Combine(Const.NewBase, gameDir, "StreamingAssets", "AudioAssets", lang);

                    if (Directory.Exists(oldAssetsPath))
                    {
                        var deletedAudioAssets = Find(oldAssetsPath, newAssetsPath, relPrefixAssets);
                        Save(deletedAudioAssets, outputAudio[lang]);
                    }

                    string relPrefixGen = $"{gameDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows/{lang}/";
                    string oldGenPath = Path.Combine(Const.OldBase, gameDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", lang);
                    string newGenPath = Path.Combine(Const.NewBase, gameDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", lang);

                    if (Directory.Exists(oldGenPath))
                    {
                        var deletedAudioGen = Find(oldGenPath, newGenPath, relPrefixGen);
                        Save(deletedAudioGen, outputAudio[lang]);
                    }
                }
            }

            Logger.Done("deletefiles.txt has been successfully generated.\n");
        }
    }
}
