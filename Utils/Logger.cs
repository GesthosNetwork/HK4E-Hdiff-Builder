using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace HK4E.HdiffBuilder.Utils
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static bool DisableFileLogging = false;

        public static void Init()
        {
            string baseName = "log";
            RotateLogs(baseName);

            string logFilePath = $"{baseName}.txt";

            string? levelStr = Const.LogLevel?.Trim().ToUpperInvariant();
            DisableFileLogging = (levelStr == "NONE");

            LogEventLevel minimumLevel = ParseLogLevel(levelStr);

            var config = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .Enrich.FromLogContext();

            if (!DisableFileLogging)
            {
                config = config.WriteTo.File(
                    path: logFilePath,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Tag}] {Message:lj}{NewLine}",
                    restrictedToMinimumLevel: minimumLevel
                );
            }

            Log.Logger = config.CreateLogger();

            if (!IsRecognizedLogLevel(levelStr))
                Warning($"Unknown log_level '{levelStr}', defaulting to INFO.");
        }

        public static void Close()
        {
            Log.CloseAndFlush();
        }

        private static void RotateLogs(string baseName)
        {
            try
            {
                string todayPath = $"{baseName}.txt";
                if (!File.Exists(todayPath))
                    return;

                DateTime lastWrite = File.GetLastWriteTime(todayPath);
                if (lastWrite.Date == DateTime.Now.Date)
                    return;

                int i = 1;
                while (File.Exists($"{baseName}-{i}.txt"))
                    i++;

                for (int j = i - 1; j >= 1; j--)
                {
                    File.Move($"{baseName}-{j}.txt", $"{baseName}-{j + 1}.txt", overwrite: true);
                }

                File.Move(todayPath, $"{baseName}-1.txt", overwrite: true);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Logger rotation failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static LogEventLevel ParseLogLevel(string? level)
        {
            return level switch
            {
                "TRACE"     => LogEventLevel.Verbose,
                "DEBUG"     => LogEventLevel.Debug,
                "INFO"      => LogEventLevel.Information,
                "NOTICE"    => LogEventLevel.Information,
                "HINT"      => LogEventLevel.Information,
                "DONE"      => LogEventLevel.Information,
                "UPDATE"    => LogEventLevel.Information,
                "FINISHED"  => LogEventLevel.Information,
                "SKIP"      => LogEventLevel.Warning,
                "WARN"      => LogEventLevel.Warning,
                "WARNING"   => LogEventLevel.Warning,
                "FAIL"      => LogEventLevel.Error,
                "ERROR"     => LogEventLevel.Error,
                "FATAL"     => LogEventLevel.Fatal,
                "NONE"      => LogEventLevel.Verbose,
                _           => LogEventLevel.Information
            };
        }

        private static bool IsRecognizedLogLevel(string? level)
        {
            return level is "TRACE" or "DEBUG" or "INFO" or "NOTICE" or "HINT"
                         or "DONE" or "UPDATE" or "FINISHED"
                         or "SKIP" or "WARN" or "WARNING"
                         or "FAIL" or "ERROR" or "FATAL"
                         or "NONE";
        }

        private static void LogWithTag(string tag, string message, LogEventLevel level, ConsoleColor color)
        {
            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write($"{tag} ");
                Console.ForegroundColor = originalColor;
                Console.WriteLine(message);

                if (!DisableFileLogging && Log.IsEnabled(level))
                {
                    Log.ForContext("Tag", tag).Write(level, "{Message}", message);
                }
            }
        }

        public static void Trace(string message)     => LogWithTag("TRACE", message, LogEventLevel.Verbose, ConsoleColor.Gray);
        public static void Debug(string message)     => LogWithTag("DEBUG", message, LogEventLevel.Debug, ConsoleColor.DarkGray);
        public static void Info(string message)      => LogWithTag("INFO", message, LogEventLevel.Information, ConsoleColor.Blue);
        public static void Notice(string message)    => LogWithTag("NOTICE", message, LogEventLevel.Information, ConsoleColor.Cyan);
        public static void Hint(string message)      => LogWithTag("HINT", message, LogEventLevel.Information, ConsoleColor.Yellow);
        public static void Update(string message)    => LogWithTag("UPDATE", message, LogEventLevel.Information, ConsoleColor.Green);
        public static void Done(string message)      => LogWithTag("DONE", message, LogEventLevel.Information, ConsoleColor.Green);
        public static void Finished(string message)  => LogWithTag("FINISHED", message, LogEventLevel.Information, ConsoleColor.Green);
        public static void Skip(string message)      => LogWithTag("SKIP", message, LogEventLevel.Warning, ConsoleColor.DarkYellow);
        public static void Warning(string message)   => LogWithTag("WARN", message, LogEventLevel.Warning, ConsoleColor.Yellow);
        public static void Fail(string message)      => LogWithTag("FAIL", message, LogEventLevel.Error, ConsoleColor.Red);
        public static void Error(string message)     => LogWithTag("ERROR", message, LogEventLevel.Error, ConsoleColor.Red);
        public static void Fatal(string message)     => LogWithTag("FATAL", message, LogEventLevel.Fatal, ConsoleColor.Red);
        public static void Cleanup(string message)   => LogWithTag("CLEANUP", message, LogEventLevel.Debug, ConsoleColor.Magenta);
    }
}
