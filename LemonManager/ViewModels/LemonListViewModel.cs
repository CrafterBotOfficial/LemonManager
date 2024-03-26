using DynamicData;
using LemonManager.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LemonManager.ViewModels
{

    public class LemonListViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public bool LemonsExist => Lemons.Count > 0;
        public ObservableCollection<LemonModel> Lemons { get; set; } = new ObservableCollection<LemonModel>();
        public bool IsPluginView;

        public LemonListViewModel()
        {

        }

        public async Task PopulateLemons()
        {
            var lemons = await MainWindowViewModel.Instance.ApplicationManager.GetLemons(IsPluginView);
            Lemons.Clear();
            foreach (var info in lemons)
                Lemons.Add(new LemonModel(this, info.Name, $"By {info.Author}\nv{info.Version}", info.RemotePath));
            this.RaisePropertyChanged(nameof(LemonsExist));
        }
    }
}