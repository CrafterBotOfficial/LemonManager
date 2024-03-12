using LemonManager.ViewModels;
using ReactiveUI;
using System.Windows.Input;

namespace LemonManager.Models;

public class LemonModel
{
    public string Name { get; set; }
    public string LemonInfoText { get; set; }

    public ICommand SetStatusCommand { get; set; }
    public ICommand DeleteLemonCommand { get; set; }    

    public LemonModel(LemonListViewModel viewModel, string name, string lemonInfoText, string remotePath)
    {
        Name = name;
        LemonInfoText = lemonInfoText;

        SetStatusCommand = ReactiveCommand.Create(async () =>
        {
            bool enabled = remotePath.EndsWith(".dll");
            MainWindowViewModel.Instance.ApplicationManager.SetLemonEnabled(remotePath, !enabled);
            await viewModel.PopulateLemons();
        });
        DeleteLemonCommand = ReactiveCommand.Create(async () =>
        {
            MainWindowViewModel.Instance.ApplicationManager.UninstallLemon(remotePath);
            await viewModel.PopulateLemons();
        });
    }
}