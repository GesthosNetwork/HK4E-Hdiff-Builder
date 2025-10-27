using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static HK4E.HdiffBuilder.Utils.Const;
using HK4E.HdiffBuilder.Tools;
using HK4E.HdiffBuilder.Utils;

namespace HK4E.HdiffBuilder.Core
{
    public static class Zip
    {
        public static void RunZip()
        {
            var oldVer = OldVer;
            var newVer = NewVer;
            var mode = Mode;
            var maxThreads = MaxThreads;
            var keepSourceFolder = KeepSourceFolder;

            var folders = new Dictionary<string, string>();

            if (RunGameDiff)
            {
                string gameFolder = $"game_{oldVer}_{newVer}_hdiff";
                folders[gameFolder] = gameFolder + ".7z";
            }

            foreach (var pair in AudioLanguages)
            {
                if (!RunAudioDiff.TryGetValue(pair.Key, out bool enabled) || !enabled)
                    continue;

                string folderName = $"{pair.Value}_{oldVer}_{newVer}_hdiff";
                folders[folderName] = folderName + ".7z";
            }

            try
            {
                string newBase = NewBase;
                string beyondAssetsDir = Path.Combine(newBase, "BeyondAssets");
                string beyondPkgFile = Path.Combine(newBase, "beyond_pkg_version");

                if (Directory.Exists(beyondAssetsDir) && File.Exists(beyondPkgFile))
                {
                    string regionSuffix = ActiveGameRoot switch
                    {
                        "YuanShen" => "CN",
                        "GenshinImpact" or "Genshin" => "OS",
                        _ => "OS"
                    };

                    string bundleName = $"BeyondAssets_{regionSuffix}_{newVer}.7z";
                    string tempFolder = Path.Combine(Environment.CurrentDirectory, $"BeyondAssets_{regionSuffix}_{newVer}_temp");

                    if (Directory.Exists(tempFolder))
                        Directory.Delete(tempFolder, true);
                    Directory.CreateDirectory(tempFolder);

                    DirectoryCopy(beyondAssetsDir, Path.Combine(tempFolder, "BeyondAssets"), true);
                    File.Copy(beyondPkgFile, Path.Combine(tempFolder, "beyond_pkg_version"));

                    folders[tempFolder] = bundleName;
                    Logger.Info($"Added BeyondAssets task.");
                }
                else
                {
                    Logger.Warning($"BeyondAssets folder or beyond_pkg_version not found in {newBase}, skipped task.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to prepare BeyondAssets task: {ex.Message}");
            }

            if (folders.Count == 0)
            {
                Logger.Warning("No diff folders selected for compression (based on config.json).");
                return;
            }

            string exe = SevenZip.Extract();
            var baseCommand = new List<string>
            {
                "a",
                "-t7z",
                "-mx=9",
                "-m0=LZMA2",
                "-md=256m",
                "-mfb=64",
                "-ms=16g",
                "-mmt=on"
            };

            void CompressFolder(string folder, string archiveName)
            {
                if (!Directory.Exists(folder))
                {
                    Logger.Skip($"Folder not found: {folder}");
                    return;
                }

                Logger.Info($"Compressing: {folder} -> {archiveName} (Please wait, don't close the console)");
                var start = DateTime.Now;

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = string.Join(" ", baseCommand) + $" \"../{archiveName}\" *",
                        WorkingDirectory = folder,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };

                    using var process = Process.Start(psi);
                    if (process == null)
                    {
                        Logger.Fail($"Failed to start compression process for {folder}.");
                        return;
                    }
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Logger.Fail($"Compression failed for {folder} (exit code: {process.ExitCode})");
                        return;
                    }

                    var elapsed = DateTime.Now - start;
                    Logger.Done($"{archiveName} created in {elapsed:hh\\:mm\\:ss}");

                    if (!keepSourceFolder)
                    {
                        Directory.Delete(folder, true);
                        Logger.Cleanup($"Folder {folder} deleted after compression.\n");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Error while processing {folder}: {e.Message}");
                }
            }

            if (mode == 0)
            {
                foreach (var entry in folders)
                    CompressFolder(entry.Key, entry.Value);
            }
            else
            {
                var tasks = new List<Task>();
                using var sem = new SemaphoreSlim(maxThreads);
                foreach (var entry in folders)
                {
                    sem.Wait();
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            CompressFolder(entry.Key, entry.Value);
                        }
                        finally
                        {
                            sem.Release();
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray());
            }
        }

        private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string temppath = Path.Combine(destDir, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    string temppath = Path.Combine(destDir, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, true);
                }
            }
        }
    }
}
