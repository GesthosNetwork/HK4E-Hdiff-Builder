using System;

namespace HK4E.HdiffBuilder.Utils
{
    public static class WindowUtils
    {
        public static readonly string Y = "HK4E Hdiff Builder v1.0 - Copyright © GesthosNetwork";

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
