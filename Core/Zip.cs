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

            var folders = new Dictionary<string, string>
            {
                [$"game_{oldVer}_{newVer}_hdiff"] = $"game_{oldVer}_{newVer}_hdiff.7z",
                [$"audio_en-us_{oldVer}_{newVer}_hdiff"] = $"audio_en-us_{oldVer}_{newVer}_hdiff.7z",
                [$"audio_ja-jp_{oldVer}_{newVer}_hdiff"] = $"audio_ja-jp_{oldVer}_{newVer}_hdiff.7z",
                [$"audio_ko-kr_{oldVer}_{newVer}_hdiff"] = $"audio_ko-kr_{oldVer}_{newVer}_hdiff.7z",
                [$"audio_zh-cn_{oldVer}_{newVer}_hdiff"] = $"audio_zh-cn_{oldVer}_{newVer}_hdiff.7z",
            };

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
                {
                    CompressFolder(entry.Key, entry.Value);
                }
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
    }
}
