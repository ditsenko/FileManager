using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileManager3
{

    public partial class MainWindow : Window
    {
        private FileManagerViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new FileManagerViewModel();
            DataContext = viewModel;
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null && listView.SelectedItem != null)
            {
                viewModel.OpenFile();
            }
        }
    }
}
