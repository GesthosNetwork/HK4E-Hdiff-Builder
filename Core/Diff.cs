using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using HK4E.HdiffBuilder.Utils;

namespace HK4E.HdiffBuilder.Core
{
    public static class Diff
    {
        private static bool IsLangAudioSubdir(string path)
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

        private static string GetActualAudioDir(string basePath, string langKey, out string selectedGameDataDir)
        {
            foreach (var dir in Const.GameDataDirs)
            {
                var audioAssetsPath = Path.Combine(basePath, dir, "StreamingAssets", "AudioAssets", langKey);
                if (Directory.Exists(audioAssetsPath))
                {
                    selectedGameDataDir = dir;
                    return audioAssetsPath;
                }

                var generatedPath = Path.Combine(basePath, dir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", langKey);
                if (Directory.Exists(generatedPath))
                {
                    selectedGameDataDir = dir;
                    return generatedPath;
                }
            }

            selectedGameDataDir = Const.GameDataDirs[0];
            return Path.Combine(basePath, selectedGameDataDir, "StreamingAssets", "AudioAssets", langKey);
        }

        private static string? FindOldFileFallback(string relPath)
        {
            string old1 = Path.Combine(Const.OldBase, relPath);
            if (File.Exists(old1)) return old1;

            string alt1 = relPath.Replace("AudioAssets", "Audio/GeneratedSoundBanks/Windows");
            string old2 = Path.Combine(Const.OldBase, alt1);
            if (File.Exists(old2)) return old2;

            string alt2 = relPath.Replace("Audio/GeneratedSoundBanks/Windows", "AudioAssets");
            string old3 = Path.Combine(Const.OldBase, alt2);
            if (File.Exists(old3)) return old3;

            return null;
        }

        private static void GameDiff()
        {
            var (updateFolder, _) = Const.GetDirs();
            int updated = 0;

            IEnumerable<string> allDirs = new[] { Const.NewBase }
                .Concat(Directory.EnumerateDirectories(Const.NewBase, "*", SearchOption.AllDirectories));

            foreach (string root in allDirs)
            {
                foreach (string file in Directory.GetFiles(root))
                {
                    string newPath = Path.Combine(root, Path.GetFileName(file));
                    string relPath = Path.GetRelativePath(Const.NewBase, newPath).Replace("\\", "/");

                    if (FileUtils.Ignore(relPath) || IsLangAudioSubdir(relPath))
                        continue;

                    string? oldPath = FindOldFileFallback(relPath);
                    string outPath = Path.Combine(updateFolder, relPath);

                    string? hashNew = File.Exists(newPath) ? FileUtils.Hash(newPath, "new") : null;
                    string? hashOld = oldPath != null && File.Exists(oldPath) ? FileUtils.Hash(oldPath, "old") : null;

                    if (hashOld == null || hashNew != hashOld)
                    {
                        var dir = Path.GetDirectoryName(outPath);
                        if (dir != null)
                            Directory.CreateDirectory(dir);

                        File.Copy(newPath, outPath, true);
                        string reason = (hashOld == null)
                            ? "copied: new file (not found in old version)"
                            : "copied: file exists but has different content (hash mismatch)";
                        Logger.Update($"game_{Const.OldVer}_{Const.NewVer}_hdiff/{relPath} → {reason}");
                        updated++;
                    }
                    else
                    {
                        Logger.Skip($"Unchanged file: game_{Const.OldVer}_{Const.NewVer}_hdiff/{relPath}");
                    }
                }
            }

            if (updated > 0)
                Logger.Done($"Successfully built game_{Const.OldVer}_{Const.NewVer}_hdiff\n");
            else
                Logger.Skip($"game_{Const.OldVer}_{Const.NewVer}_hdiff");
        }

        private static void AudioDiff(string langKey, string folderTag)
        {
            var (updateFolder, outputAudio) = Const.GetDirs();

            string newAudioDir = GetActualAudioDir(Const.NewBase, langKey, out string gameDataDir);
            if (!Directory.Exists(newAudioDir))
            {
                Logger.Skip($"{folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff");
                return;
            }

            bool isNewPathAudioAssets = newAudioDir.Contains("AudioAssets");

            string oldAudioDir = isNewPathAudioAssets
                ? Path.Combine(Const.OldBase, gameDataDir, "StreamingAssets", "AudioAssets", langKey)
                : Path.Combine(Const.OldBase, gameDataDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", langKey);

            string outputDir = isNewPathAudioAssets
                ? Path.Combine(outputAudio[langKey], gameDataDir, "StreamingAssets", "AudioAssets", langKey)
                : Path.Combine(outputAudio[langKey], gameDataDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", langKey);

            int updated = 0;

            IEnumerable<string> allDirs = new[] { newAudioDir }
                .Concat(Directory.EnumerateDirectories(newAudioDir, "*", SearchOption.AllDirectories));

            foreach (string root in allDirs)
            {
                foreach (string file in Directory.GetFiles(root))
                {
                    string newPath = Path.Combine(root, Path.GetFileName(file));
                    string relPath = Path.GetRelativePath(newAudioDir, newPath).Replace("\\", "/");

                    string fullRelPath = isNewPathAudioAssets
                        ? $"{gameDataDir}/StreamingAssets/AudioAssets/{langKey}/{relPath}"
                        : $"{gameDataDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows/{langKey}/{relPath}";

                    if (FileUtils.Ignore(Path.GetFileName(fullRelPath)))
                        continue;

                    string? oldPath = FindOldFileFallback(fullRelPath);
                    string outPath = Path.Combine(outputDir, relPath);

                    string? hashNew = File.Exists(newPath) ? FileUtils.Hash(newPath, "new") : null;
                    string? hashOld = oldPath != null && File.Exists(oldPath) ? FileUtils.Hash(oldPath, "old") : null;

                    if (hashOld == null || hashNew != hashOld)
                    {
                        var dir = Path.GetDirectoryName(outPath);
                        if (dir != null)
                            Directory.CreateDirectory(dir);

                        File.Copy(newPath, outPath, true);
                        string reason = (hashOld == null)
                            ? "copied: new file (not found in old version)"
                            : "copied: file exists but has different content (hash mismatch)";
                        Logger.Update($"{folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff/{fullRelPath} → {reason}");
                        updated++;
                    }
                    else
                    {
                        Logger.Skip($"Unchanged file: {folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff/{fullRelPath}");
                    }
                }
            }

            string pkgFile = $"Audio_{langKey}_pkg_version";
            string newPkg = Path.Combine(Const.NewBase, pkgFile);
            string oldPkg = Path.Combine(Const.OldBase, pkgFile);
            string outPkg = Path.Combine(outputAudio[langKey], pkgFile);

            string? pkgHashNew = File.Exists(newPkg) ? FileUtils.Hash(newPkg, "new") : null;
            string? pkgHashOld = File.Exists(oldPkg) ? FileUtils.Hash(oldPkg, "old") : null;

            if (pkgHashNew != null && (pkgHashOld == null || pkgHashNew != pkgHashOld))
            {
                Directory.CreateDirectory(outputAudio[langKey]);
                File.Copy(newPkg, outPkg, true);
                string reason = (pkgHashOld == null)
                    ? "copied: new file (not found in old version)"
                    : "copied: file exists but has different content (hash mismatch)";
                Logger.Update($"{folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff/{pkgFile} → {reason}");
                updated++;
            }
            else if (pkgHashNew != null && pkgHashNew == pkgHashOld)
            {
                Logger.Skip($"Unchanged file: {folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff/{pkgFile}");
            }

            if (updated > 0)
                Logger.Done($"Successfully built {folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff");
            else
                Logger.Skip($"{folderTag}_{Const.OldVer}_{Const.NewVer}_hdiff");
        }

        public static void RunDiff()
        {
            var start = DateTime.Now;
            var threads = new List<Thread>();

            if (Const.Mode == 1)
            {
                if (Const.RunGameDiff)
                {
                    Thread tGame = new Thread(GameDiff);
                    tGame.Start();
                    threads.Add(tGame);
                }

                foreach (var entry in Const.AudioLanguages)
                {
                    string langKey = entry.Key;
                    string folderTag = entry.Value;

                    if (Const.RunAudioDiff.TryGetValue(langKey, out bool enabled) && enabled)
                    {
                        Thread t = new Thread(() => AudioDiff(langKey, folderTag));
                        t.Start();
                        threads.Add(t);
                    }
                }

                foreach (var t in threads)
                    t.Join();
            }
            else
            {
                if (Const.RunGameDiff)
                    GameDiff();

                foreach (var entry in Const.AudioLanguages)
                {
                    string langKey = entry.Key;
                    string folderTag = entry.Value;

                    if (Const.RunAudioDiff.TryGetValue(langKey, out bool enabled) && enabled)
                        AudioDiff(langKey, folderTag);
                }
            }

            var elapsed = DateTime.Now - start;
            Logger.Finished($"All Diff process for {Const.OldVer} → {Const.NewVer} completed in {elapsed:hh\\:mm\\:ss}\n");
        }
    }
}
