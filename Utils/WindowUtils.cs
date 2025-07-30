using System;
using System.Reflection;

namespace HK4E.HdiffBuilder.Utils
{
    public static class WindowUtils
    {
        public static string Y
        {
            get
            {
                string version = Assembly.GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                       ?? "Unknown";
                return $"HK4E Hdiff Builder v{version} - Copyright © GesthosNetwork";
            }
        }

        public static void AdjustConsoleWidth()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    int targetWidth = 180;
                    if (Console.WindowWidth < targetWidth)
                        Console.WindowWidth = targetWidth;

                    if (Console.BufferWidth < targetWidth)
                        Console.BufferWidth = targetWidth;
                }

                Console.Title = Y;
                SystemTasks.A05xF();
            }
            catch {}
        }
    }
}
