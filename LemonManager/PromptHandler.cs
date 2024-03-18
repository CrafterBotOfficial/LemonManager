using Avalonia.Controls;
using LemonManager.ModManager;
using LemonManager.ViewModels;
using LemonManager.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace LemonManager;

public class PromptHandler : IPromptHandler
{
    internal static PromptHandler Instance;

    private Dictionary<string, MultiSelectionPromptWindowViewModel> selectionPrompts = new();

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
            selectionPrompts.Add(title, window.DataContext as MultiSelectionPromptWindowViewModel);
            taskCompletionSource.SetResult(await window.ShowDialog<int>(MainWindow.Instance));
        });

        selectionPrompts.Remove(title);
        return taskCompletionSource.Task;
    }

    public void UpdateMultiSelectionPrompt(string target, params string[] options)
    {
        if (selectionPrompts.TryGetValue(target, out var prompt))
        {
            prompt.Options = new(options);
            prompt.RaisePropertyChanged("Options");
            return;
        }
        ModManager.Logger.Warning($"Couldn't find a prompt with the name of {target}");
    }

    public void SetStatus(string status)
    {
        MainWindowViewModel.LoadingStatus = status;   
    }
}
