using System;
using HK4E.HdiffBuilder.Utils;
using HK4E.HdiffBuilder.Core;

namespace HK4E.HdiffBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Fatal($"Unhandled exception: {((Exception)e.ExceptionObject).Message}");
                Exit(1);
            };

            try
            {
                WindowUtils.AdjustConsoleWidth();

                if (Const.ConfigCreated)
                {
                    Logger.Warning("config.json not found. Created with default values.");
                    Logger.Hint("Please check and edit config.json");
                    Exit(0);
                }

                if (!Const.ValidateDirs())
                    Exit(1);

                Diff.RunDiff();
                Delete.RunDel();
                Hdiff.RunHdiff();
                Zip.RunZip();

                Logger.Finished("All steps completed successfully in {elapsed:hh\\:mm\\:ss}");
                Exit(0);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"An error occurred: {ex.Message}");
                Exit(1);
            }
        }

        static void Exit(int code)
        {
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
            Environment.Exit(code);
        }
    }
}
