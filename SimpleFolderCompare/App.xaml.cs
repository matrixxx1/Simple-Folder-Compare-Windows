using System;
using System.IO;
using System.Windows;

namespace SimpleFolderCompare;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogUnhandled(args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            LogUnhandled(args.ExceptionObject as Exception);
        };

        base.OnStartup(e);
    }

    private static void LogUnhandled(Exception? exception)
    {
        if (exception == null)
        {
            return;
        }

        var logPath = Path.Combine(Path.GetTempPath(), "SimpleFolderCompare-startup.log");
        File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {exception}\r\n");
    }
}
