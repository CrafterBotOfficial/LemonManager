using Avalonia;
using Avalonia.ReactiveUI;
using LemonManager.ModManager;
using LemonManager.ViewModels;
using System;
using System.Threading.Tasks;

namespace LemonManager
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            test();
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // temp code for testing mod manager
        private static async void test()
        {
            await Task.Yield();
            await ModManager.AndroidDebugBridge.ServerManager.Initialize(new LogManager(), new PromptHandler());
            ApplicationLocator.ApplicationInfo info = ModManager.ApplicationLocator.GetApplications()["com.freelives.gorn"];
            var modded = await ApplicationLocator.GetModdedApplicationInfo(info);
            MainWindowViewModel.Instance.ApplicationManager = new ApplicationManager(modded);
            await MainWindowViewModel.Instance.ApplicationManager.GetLemons(false);
            await MainWindowViewModel.Instance.ApplicationManager.GetLemons(true);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}
