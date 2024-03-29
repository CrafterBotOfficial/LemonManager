using Avalonia.Controls;
using Avalonia.Media.Imaging;
using LemonManager.ModManager;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using LemonManager.Views;
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
        public ICommand StartGameCommand { get; set; }

        public bool ShowStartGameButton { get; set; } = true;

        public bool HasIcon => AppIcon is object;
        public Bitmap AppIcon { get; set; }

        public MainWindowViewModel()
        {
            Instance = this;
            ChangeApplicationCommand = ReactiveCommand.Create(async () => await SelectApplication(true));
            StartGameCommand = ReactiveCommand.Create(() =>
            {
                GameControlsView.Logcat();
                GameControlsView.StartStopGame();
                ShowStartGameButton = false;
                this.RaisePropertyChanged(nameof(ShowStartGameButton));
            });
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
            if (ApplicationManager.Info.MelonLoaderInitialized)
            {
                await ModListView.PopulateLemons();
                await PluginListView.PopulateLemons();
                return;
            }
            Logger.Warning("Couldn't fill lemon lists due to the game's ML not being initiliazed!");
        }

        public async Task SelectApplication(bool forceNewSelection)
        {
            PromptHandler.Instance.SetStatus("Locating targetted application");
            var apps = ApplicationLocator.GetApplications();

            if (forceNewSelection) AppSettings.Default.SelectedApplicationId = string.Empty;

            UnityApplicationInfoModel? unityInfo = null;
            while (unityInfo is null || ((unityInfo?.IsModded).HasValue && (!unityInfo?.IsModded).Value))
            {
                await Task.Delay(10);
                if (apps.TryGetValue(AppSettings.Default.SelectedApplicationId, out var appInfo))
                {
                    if (unityInfo is null)
                    {
                        unityInfo = await ApplicationLocator.GetUnityApplicationInfo(appInfo);
                        if (unityInfo is null)
                        {
                            AppSettings.Default.SelectedApplicationId = string.Empty;
                            await PromptHandler.Instance.PromptUser("Unrecognised Game", "The selected application doesn't appear to be a Unity game, please select another.", PromptType.Notification);
                            continue;
                        }
                    }
                    else
                    if (!unityInfo.IsModded && !ModManager.GamePatcherManager.IsPatching)
                    {
                        if (!await PromptHandler.Instance.PromptUser("Patch Game?", unityInfo.Id + " isn't patched with LemonLoader, if you continue it will be patched.", PromptType.Confirmation))
                        {
                            AppSettings.Default.SelectedApplicationId = string.Empty;
                            unityInfo = null;
                        }
                        else
                        {
                            GamePatcherManager.IsPatching = true;
                            Task.Run(async () => GamePatcherManager.PatchApp(unityInfo));
                        }
                    }
                    continue;
                }
                AppSettings.Default.SelectedApplicationId = apps.ElementAt(await PromptHandler.Instance.PromptUser("Select a Application", apps.Select(app => app.Key).ToArray())).Key;
            }

            ApplicationManager = new ApplicationManager(unityInfo);
            if (!unityInfo.MelonLoaderInitialized)
            {
                ShowMelonNotReady = true;
                this.RaisePropertyChanged(nameof(ShowMelonNotReady));
            }
            else
            {
                PreferenceEditorView.Init(ApplicationManager.Info.Id);
                AppIcon = ByteArrayToBitmap(ApplicationManager.Info.Icon) ?? null;
                this.RaisePropertyChanged(nameof(AppIcon));
                this.RaisePropertyChanged(nameof(HasIcon));
                AppSettings.Default.Save();
            }
            IsLoading = false;
            this.RaisePropertyChanged(nameof(ShowLemonManager));
        }

        public static Bitmap ByteArrayToBitmap(byte[] byteArray)
        {
            using var stream = new MemoryStream(byteArray);
            return new Bitmap(stream);
        }


        #region Loading status
        public bool ShowLoadingView { get; set; } = true;
        private TextBlock loadingStatusTextBlock => MainWindow.Instance.GetControl<TextBlock>("StatusText");
        public static string LoadingStatus
        {
            set
            {
                IsLoading = true;
                Instance.loadingStatusTextBlock.Text = value;
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
                    Instance.RaisePropertyChanged(nameof(ShowLemonManager));
                }
            }
        }

        #endregion

        public bool ShowMelonNotReady { get; set; }
        public bool ShowLemonManager => !ShowMelonNotReady && !ShowLoadingView;
    }
}
