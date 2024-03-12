using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using LemonManager.ViewModels;
using System.Collections.Generic;

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
            if (files is object)
            {
                var viewModel = ((MainWindowViewModel)DataContext);
                MainWindowViewModel.IsLoading = true;
                foreach (IStorageItem item in files)
                {
                    await viewModel.ApplicationManager.InstallLemon(item.Path.LocalPath);
                }
                await viewModel.PopulateLemons();
            }
        }
    }
}