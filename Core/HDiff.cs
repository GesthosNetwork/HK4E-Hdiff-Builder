using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using HK4E.HdiffBuilder.Utils;
using HK4E.HdiffBuilder.Tools;

namespace HK4E.HdiffBuilder.Core
{
    public static class Hdiff
    {
        public static void RunHdiff()
        {
            string hdiffzPath = Hdiffz.Extract();
            var (updateFolder, outputAudio) = Const.GetDirs();
            string[] HDIFFZ_COMPRESSION_ARGS = new[] { "-f", "-c-lzma2-9-256m" };

            void MakeHdiff(string oldFile, string newFile, string hdiffFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(hdiffFile)!);
                ProcessStartInfo psi = new()
                {
                    FileName = hdiffzPath,
                    ArgumentList = { oldFile, newFile, hdiffFile },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                foreach (var arg in HDIFFZ_COMPRESSION_ARGS)
                    psi.ArgumentList.Insert(0, arg);
                using Process proc = Process.Start(psi)!;
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new Exception($"hdiffz failed: {proc.StandardError.ReadToEnd()}");
            }

            bool TryResolveOldFile(string defaultOldFile, string relPath, out string resolvedOldFile)
            {
                if (File.Exists(defaultOldFile))
                {
                    resolvedOldFile = defaultOldFile;
                    return true;
                }

                string altPath = relPath.Contains("AudioAssets")
                    ? relPath.Replace("AudioAssets", "Audio/GeneratedSoundBanks/Windows")
                    : relPath.Replace("Audio/GeneratedSoundBanks/Windows", "AudioAssets");

                string baseOld = Const.OldBase;
                resolvedOldFile = Path.Combine(baseOld, altPath);

                return File.Exists(resolvedOldFile);
            }

            Dictionary<string, string> ProcessSingleFile(string oldFile, string newFile, string hdiffFile, string remoteName)
            {
                if (!File.Exists(newFile))
                {
                    Logger.Skip($"New file not found: {newFile}");
                    return null!;
                }

                if (!TryResolveOldFile(oldFile, remoteName, out string resolvedOld))
                {
                    Logger.Skip($"Old file not found: {oldFile}");
                    return null!;
                }

                if (File.Exists(hdiffFile))
                {
                    Logger.Skip($"hdiff already exists: {hdiffFile}");
                    return new() { { "remoteName", remoteName } };
                }

                try
                {
                    MakeHdiff(resolvedOld, newFile, hdiffFile);
                    try
                    {
                        File.Delete(newFile);
                        Logger.Info($"{hdiffFile} + deleted {Path.GetFileName(newFile)}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to delete {newFile}: {e.Message}");
                    }
                    return new() { { "remoteName", remoteName } };
                }
                catch (Exception e)
                {
                    Logger.Error($"{hdiffFile} | {e.Message}");
                    return null!;
                }
            }

            void ScanAndProcess(string updateRoot, string oldRoot, string remotePrefix, string outputRoot, string? relBase = null)
            {
                if (!Directory.Exists(updateRoot))
                    return;

                List<Dictionary<string, string>> results = new();
                List<Task<Dictionary<string, string>>> tasks = new();
                relBase ??= updateRoot;

                if (Const.Mode == 1)
                {
                    SemaphoreSlim concurrency = new(Const.MaxThreads);
                    foreach (string file in Directory.EnumerateFiles(updateRoot, "*.pck", SearchOption.AllDirectories))
                    {
                        string relPath = Path.GetRelativePath(relBase, file).Replace("\\", "/");
                        string remoteName = Path.Combine(remotePrefix, relPath).Replace("\\", "/");
                        string defaultOldFile = Path.Combine(oldRoot, relPath);
                        string hdiffPath = file + ".hdiff";

                        tasks.Add(Task.Run(async () =>
                        {
                            await concurrency.WaitAsync();
                            try
                            {
                                return ProcessSingleFile(defaultOldFile, file, hdiffPath, remoteName);
                            }
                            finally
                            {
                                concurrency.Release();
                            }
                        }));
                    }

                    Task.WaitAll(tasks.ToArray());
                    foreach (var task in tasks)
                        if (task.Result != null)
                            results.Add(task.Result);
                }
                else
                {
                    foreach (string file in Directory.EnumerateFiles(updateRoot, "*.pck", SearchOption.AllDirectories))
                    {
                        string relPath = Path.GetRelativePath(relBase, file).Replace("\\", "/");
                        string remoteName = Path.Combine(remotePrefix, relPath).Replace("\\", "/");
                        string defaultOldFile = Path.Combine(oldRoot, relPath);
                        string hdiffPath = file + ".hdiff";

                        var res = ProcessSingleFile(defaultOldFile, file, hdiffPath, remoteName);
                        if (res != null)
                            results.Add(res);
                    }
                }

                if (results.Count > 0)
                {
                    string outTxt = Path.Combine(outputRoot, "hdifffiles.txt");
                    try
                    {
                        using var writer = new StreamWriter(outTxt, false, Encoding.UTF8);
                        foreach (var entry in results)
                            writer.WriteLine(JsonSerializer.Serialize(entry));
                        Logger.Info($"Wrote: {outTxt}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to write {outTxt}: {e.Message}");
                    }
                }
                else
                {
                    Logger.Skip($"No entries to write for {outputRoot}");
                }

                Logger.Done($"Total hdiff entries: {results.Count}\n");
            }

            string GetActualAudioDir(string basePath, string langKey, out string gameDataDir, out bool isAssets)
            {
                foreach (var dir in Const.GameDataDirs)
                {
                    var path1 = Path.Combine(basePath, dir, "StreamingAssets", "AudioAssets", langKey);
                    if (Directory.Exists(path1))
                    {
                        gameDataDir = dir;
                        isAssets = true;
                        return path1;
                    }

                    var path2 = Path.Combine(basePath, dir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", langKey);
                    if (Directory.Exists(path2))
                    {
                        gameDataDir = dir;
                        isAssets = false;
                        return path2;
                    }
                }

                gameDataDir = Const.GameDataDirs[0];
                isAssets = true;
                return Path.Combine(basePath, gameDataDir, "StreamingAssets", "AudioAssets", langKey);
            }

            Logger.Info("HDIFF .pck process started...");
            var start = DateTime.Now;

            if (Const.RunGameDiff)
            {
                foreach (var gameDir in Const.GameDataDirs)
                {
                    var root1 = Path.Combine(updateFolder, gameDir, "StreamingAssets", "AudioAssets");
                    var old1 = Path.Combine(Const.OldBase, gameDir, "StreamingAssets", "AudioAssets");
                    if (Directory.Exists(root1))
                        ScanAndProcess(root1, old1, $"{gameDir}/StreamingAssets/AudioAssets", updateFolder, root1);

                    var root2 = Path.Combine(updateFolder, gameDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows");
                    var old2 = Path.Combine(Const.OldBase, gameDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows");
                    if (Directory.Exists(root2))
                        ScanAndProcess(root2, old2, $"{gameDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows", updateFolder, root2);
                }
            }

            foreach (var lang in Const.AudioLanguages.Keys)
            {
                if (!Const.RunAudioDiff.TryGetValue(lang, out bool shouldRun) || !shouldRun)
                    continue;

                string updBase = outputAudio[lang];
                string newAudioDir = GetActualAudioDir(Const.NewBase, lang, out string gameDataDir, out bool isAssets);

                string oldAudioDir = isAssets
                    ? Path.Combine(Const.OldBase, gameDataDir, "StreamingAssets", "AudioAssets", lang)
                    : Path.Combine(Const.OldBase, gameDataDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", lang);

                string remotePrefix = isAssets
                    ? $"{gameDataDir}/StreamingAssets/AudioAssets/{lang}"
                    : $"{gameDataDir}/StreamingAssets/Audio/GeneratedSoundBanks/Windows/{lang}";

                string updateRoot = isAssets
                    ? Path.Combine(updBase, gameDataDir, "StreamingAssets", "AudioAssets", lang)
                    : Path.Combine(updBase, gameDataDir, "StreamingAssets", "Audio", "GeneratedSoundBanks", "Windows", lang);

                ScanAndProcess(updateRoot, oldAudioDir, remotePrefix, updBase, updateRoot);
            }

            var elapsed = DateTime.Now - start;
            Logger.Finished($"All Hdiff processes completed in {elapsed:hh\\:mm\\:ss}\n");
        }
    }
}
