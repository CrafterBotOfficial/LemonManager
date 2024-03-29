using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using LemonManager.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace LemonManager.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            AddHandler(DragDrop.DropEvent, Drop);
        }

        public async void Drop(object sender, DragEventArgs e)
        {
            IEnumerable<IStorageItem>? files = e.Data.GetFiles();
            var viewModel = MainWindowViewModel.Instance;
            if (files is object && viewModel is object && viewModel.ApplicationManager is object && viewModel.ApplicationManager.Info.MelonLoaderInitialized)
            {
                for (int i = 0; i < files.Count(); i++)
                {
                    await viewModel.ApplicationManager.InstallLemon(files.ElementAt(i).Path.LocalPath);
                }
                await viewModel.PopulateLemons();
                MainWindowViewModel.IsLoading = false;
            }
        }
    }
}