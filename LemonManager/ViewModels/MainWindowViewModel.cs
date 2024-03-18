using Avalonia.Media.Imaging;
using LemonManager.ModManager;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using ReactiveUI;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LemonManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public static MainWindowViewModel Instance;

        public ApplicationManager ApplicationManager;

        public LemonListViewModel ModListView { get; } = new LemonListViewModel();
        public LemonListViewModel PluginListView { get; } = new LemonListViewModel() { IsPluginView = true };
        public GameControlsViewModel GameControlsView { get; } = new GameControlsViewModel();
        public PreferenceEditorViewModel PreferenceEditorView { get; } = new PreferenceEditorViewModel();

        public ICommand ChangeApplicationCommand { get; set; }

        public bool HasIcon => AppIcon is object;
        public Bitmap AppIcon { get; set; }

        public MainWindowViewModel()
        {
            Instance = this;
            ChangeApplicationCommand = ReactiveCommand.Create(async () => await SelectApplication(true));
            Task.Run(Init);
        }

        private async Task Init()
        {
            await ServerManager.Initialize(new PromptHandler());
            await SelectApplication(false);
            await PopulateLemons();
            IsLoading = false;
        }

        public async Task PopulateLemons()
        {
            await ModListView.PopulateLemons();
            await PluginListView.PopulateLemons();
        }

        public async Task SelectApplication(bool forceNewSelection)
        {
            PromptHandler.Instance.SetStatus("Locating targetted application");
            var apps = ApplicationLocator.GetApplications();

            if (forceNewSelection) AppSettings.Default.SelectedApplicationId = string.Empty;

            ModdedApplicationModel moddedInfo = null;
            while (moddedInfo is null)
            {
                if (apps.TryGetValue(AppSettings.Default.SelectedApplicationId, out var appInfo))
                {
                    moddedInfo = await ApplicationLocator.GetModdedApplicationInfo(appInfo);
                    if (moddedInfo is null)
                    {
                        AppSettings.Default.SelectedApplicationId = string.Empty;
                        await PromptHandler.Instance.PromptUser("Game Isn't Modded", "The selected game isn't modded with LemonLoader. Please select a different game or patch this game with the LemonInstaller.", PromptType.Notification);
                    }
                    continue;
                }
                AppSettings.Default.SelectedApplicationId = apps.ElementAt(await PromptHandler.Instance.PromptUser("Select a Application", apps.Select(app => app.Key).ToArray())).Key;
            }
            ApplicationManager = new ApplicationManager(moddedInfo);
            PreferenceEditorView.Init(ApplicationManager.Info.Id);
            AppIcon = ByteArrayToBitmap(ApplicationManager.Info.Icon) ?? null;
            this.RaisePropertyChanged(nameof(AppIcon));
            this.RaisePropertyChanged(nameof(HasIcon));
            AppSettings.Default.Save();
        }

        public static Bitmap ByteArrayToBitmap(byte[] byteArray)
        {
            using var stream = new MemoryStream(byteArray);
            return new Bitmap(stream);
        }


        #region Loading status
        public bool ShowLoadingView { get; set; } = true;
        public string Status { get; set; }
        public static string LoadingStatus
        {
            get => Instance.Status;
            set
            {
                Instance.Status = value;
                Instance.RaisePropertyChanged(nameof(Status));
                IsLoading = true;
            }
        }
        public static bool IsLoading
        {
            get => Instance.ShowLoadingView;
            set
            {
                if (Instance.ShowLoadingView != value)
                {
                    Instance.ShowLoadingView = value;
                    Instance.RaisePropertyChanged(nameof(ShowLoadingView));
                }
            }
        }
        #endregion
    }
}
