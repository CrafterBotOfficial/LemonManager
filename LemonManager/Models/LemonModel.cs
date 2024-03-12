using Avalonia.Automation;
using LemonManager.ViewModels;
using ReactiveUI;
using System.Windows.Input;

namespace LemonManager.Models;

public class LemonModel
{
    public string Name { get; set; }
    public string LemonInfoText { get; set; }

    public string StatusButtonText => lemonEnabled ? "Disable" : "Enable";

    public ICommand SetStatusCommand { get; set; }
    public ICommand DeleteLemonCommand { get; set; }

    private bool lemonEnabled;

    public LemonModel(LemonListViewModel viewModel, string name, string lemonInfoText, string remotePath)
    {
        Name = name;
        LemonInfoText = lemonInfoText;
        lemonEnabled = remotePath.EndsWith(".dll");

        SetStatusCommand = ReactiveCommand.Create(async () =>
        {
            MainWindowViewModel.Instance.ApplicationManager.SetLemonEnabled(remotePath, !lemonEnabled);
            await viewModel.PopulateLemons();
        });
        DeleteLemonCommand = ReactiveCommand.Create(async () =>
        {
            MainWindowViewModel.Instance.ApplicationManager.UninstallLemon(remotePath);
            await viewModel.PopulateLemons();
        });
    }
}