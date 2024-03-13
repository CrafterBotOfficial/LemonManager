using Avalonia;
using Avalonia.ReactiveUI;
using System;

namespace LemonManager;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += async (sender, arg) =>
        {
            ModManager.Logger.Error("Unhandled exception " + arg);
            await PromptHandler.Instance?.PromptUser("Unhandled Exception", "The application ran into something unexpected, please report the problem on Github.", ModManager.PromptType.Notification);
        };

        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}