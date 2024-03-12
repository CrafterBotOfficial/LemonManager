using Avalonia.Controls;
using LemonManager.ModManager;
using LemonManager.ViewModels;
using LemonManager.Views;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace LemonManager;

public class PromptHandler : IPromptHandler
{
    internal static PromptHandler Instance;

    public PromptHandler()
    {
        Instance = this;
    }

    public Task<bool> PromptUser(string title, string message, PromptType type)
    {
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        RxApp.MainThreadScheduler.Schedule(async () =>
        {
            Window window = new PromptWindow();
            window.DataContext = new PromptWindowViewModel(window, title, message, type);
            taskCompletionSource.SetResult(await window.ShowDialog<bool>(MainWindow.Instance));
        });
        return taskCompletionSource.Task;
    }


    public Task<int> PromptUser(string title, params string[] options)
    {
        TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
        RxApp.MainThreadScheduler.Schedule(async () =>
        {
            Window window = new MultiSelectionPromptWindow();
            window.DataContext = new MultiSelectionPromptWindowViewModel(window, title, options);
            taskCompletionSource.SetResult(await window.ShowDialog<int>(MainWindow.Instance));
        });
        return taskCompletionSource.Task;
    }
}
