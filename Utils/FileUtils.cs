using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HK4E.HdiffBuilder.Utils;

namespace HK4E.HdiffBuilder.Utils
{
    public static class FileUtils
    {
        private static readonly HashSet<string> IgnoreFiles = new()
        {
            "config.ini", "vulkan_gpu_list_config.txt", "version.dll"
        };

        private static readonly HashSet<string> IgnoreExtensions = new()
        {
            ".log", ".dmp", ".bak"
        };

        private static readonly HashSet<string> IgnoreDirs = new()
        {
            "SDKCaches", "webCaches", "Persistent",
            "SDK", "LauncherPlugins", "blob_storage", "ldiff"
        };

        private static bool VersionIsAtLeast(string ver, int x, int y, int z)
        {
            var parts = ver.Split('.');
            if (parts.Length != 3) return true;

            if (!int.TryParse(parts[0], out int vx)) return true;
            if (!int.TryParse(parts[1], out int vy)) return true;
            if (!int.TryParse(parts[2], out int vz)) return true;

            if (vx > x) return true;
            if (vx < x) return false;
            if (vy > y) return true;
            if (vy < y) return false;
            return vz >= z;
        }

        public static bool Ignore(string path)
        {
            string basename = Path.GetFileName(path);
            string ext = Path.GetExtension(basename);
            string[] parts = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (ext == ".pck")
            {
                if (basename.StartsWith("SFX_") || basename.StartsWith("Music_"))
                {
                    Logger.Skip($"Ignored file by prefix rule: {basename}");
                    return true;
                }

                if (basename.StartsWith("VO_"))
                {
                    if (VersionIsAtLeast(Const.NewVer, 2, 7, 0))
                    {
                        Logger.Skip($"Ignored file by prefix rule: {basename}");
                        return true;
                    }
                }
            }

            if (IgnoreFiles.Contains(basename))
            {
                Logger.Skip($"Ignored file: {basename}");
                return true;
            }

            if (IgnoreExtensions.Contains(ext))
            {
                Logger.Skip($"Ignored extension: {basename}");
                return true;
            }

            if (parts.Any(part => IgnoreDirs.Contains(part)))
            {
                Logger.Skip($"Ignored directory pattern: {path}");
                return true;
            }

            if (basename.StartsWith("Audio_") && basename.EndsWith("_pkg_version"))
            {
                return true;
            }

            return false;
        }

        public static string Hash(string filepath, string? sourceTag = null)
        {
            try
            {
                using SHA256 sha256 = SHA256.Create();
                using FileStream fs = File.OpenRead(filepath);
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                byte[]? hashBytes = sha256.Hash;

                if (hashBytes == null)
                    throw new InvalidOperationException("SHA256 hash computation failed.");

                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                string tag = string.IsNullOrEmpty(sourceTag) ? "" : $" {sourceTag}";
                Logger.Info($"File hashed{tag}: {Path.GetFileName(filepath)} => {hash}");
                return hash;
            }
            catch (Exception ex)
            {
                Logger.Error($"Hashing failed for {filepath}: {ex.Message}");
                throw;
            }
        }
    }
}
