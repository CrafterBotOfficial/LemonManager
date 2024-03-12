using Avalonia.Controls;
using LemonManager.ModManager;
using LemonManager.Views;
using ReactiveUI;
using System.Windows.Input;

namespace LemonManager.ViewModels;

public class PromptWindowViewModel
{
    public ICommand OnOk { get; set; }
    public ICommand OnCancel { get; set; }

    public string Title { get; set; }
    public string Message { get; set; }

    public bool IsCancelable { get; set; }

    public PromptWindowViewModel(Window view, string title, string message, PromptType type)
    {
        Title = title;
        Message = message;
        IsCancelable = type == PromptType.Confirmation;

        OnCancel = ReactiveCommand.Create(() => view.Close(false));
        OnOk = ReactiveCommand.Create(() => view.Close(true));
    }
}