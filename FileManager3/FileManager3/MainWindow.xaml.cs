using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileManager3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileManagerViewModel viewModel;

        public MainWindow()
        {
            try
            {
                // Замість виклику InitializeComponent використовуємо Application.LoadComponent
                // Це тимчасове рішення проблеми з автогенерацією коду
                Uri resourceLocator = new Uri("/FileManager3;component/MainWindow.xaml", UriKind.Relative);
                Application.LoadComponent(this, resourceLocator);
                
                // Ініціалізуємо ViewModel
                viewModel = new FileManagerViewModel();
                this.DataContext = viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка ініціалізації: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Методи для подвійного кліку
        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (viewModel == null) return;

            var listView = sender as ListView;
            Console.WriteLine(listView);
            if (listView == null || listView.SelectedItem == null) return;

            try
            {
                // Відкриваємо вибраний елемент через команду OpenFileCommand
                if (viewModel.OpenFileCommand != null && 
                    viewModel.OpenFileCommand.CanExecute(listView.SelectedItem))
                {
                    viewModel.OpenFileCommand.Execute(listView.SelectedItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при відкритті файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null && listView.SelectedItem != null)
            {
                var item = listView.SelectedItem as FileItem;
                if (item != null && item.IsDirectory)
                {
                    viewModel.OpenFile(item);
                    e.Handled = true;
                }
            }
        }
    }
}
