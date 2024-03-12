using LemonManager.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LemonManager.ViewModels;

public class LemonListViewModel : ViewModelBase
{
    public ObservableCollection<LemonModel> Lemons { get; set; } = new ObservableCollection<LemonModel>();
    public bool IsPluginView;

    public LemonListViewModel()
    {

    }

    public async Task PopulateLemons()
    {
        Lemons.Clear();
        foreach (var info in await MainWindowViewModel.Instance.ApplicationManager.GetLemons(IsPluginView))
            Lemons.Add(new LemonModel(this, info.Name, $"By {info.Author}\nv{info.Version}", info.RemotePath));
    }
}