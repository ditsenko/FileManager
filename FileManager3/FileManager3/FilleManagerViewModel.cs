using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Data;

namespace FileManager3
{
    public class FileManagerViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<FileItem> leftPanelFiles;
        private ObservableCollection<FileItem> rightPanelFiles;
        private FileItem selectedLeftItem;
        private FileItem selectedRightItem;
        private string currentLeftPath;
        private string currentRightPath;
        private Stack<string> leftPathHistory;
        private Stack<string> rightPathHistory;
        private ObservableCollection<DriveInfo> availableDrives;
        private FileItem itemToPaste;
        private bool isCut;
        private bool isDualPanelMode;
        private string toggleViewModeText;
        private string statusMessage;
        private Visibility messageVisibility;
        private DispatcherTimer messageTimer;
        private bool isLeftPanelHighlighted;
        private bool isRightPanelHighlighted;
        private string lastPastedFileName;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FileItem> LeftPanelFiles
        {
            get => leftPanelFiles;
            set
            {
                leftPanelFiles = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FileItem> RightPanelFiles
        {
            get => rightPanelFiles;
            set
            {
                rightPanelFiles = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DriveInfo> AvailableDrives
        {
            get => availableDrives;
            set
            {
                availableDrives = value;
                OnPropertyChanged();
            }
        }

        public FileItem SelectedLeftItem
        {
            get => selectedLeftItem;
            set
            {
                selectedLeftItem = value;
                OnPropertyChanged();
            }
        }

        public FileItem SelectedRightItem
        {
            get => selectedRightItem;
            set
            {
                selectedRightItem = value;
                OnPropertyChanged();
            }
        }

        public bool IsDualPanelMode
        {
            get => isDualPanelMode;
            set
            {
                isDualPanelMode = value;
                OnPropertyChanged();
                ToggleViewModeText = value ? "Одна панель" : "Дві панелі";
            }
        }

        public string ToggleViewModeText
        {
            get => toggleViewModeText;
            set
            {
                toggleViewModeText = value;
                OnPropertyChanged();
            }
        }

        public Visibility RightPanelVisibility => IsDualPanelMode ? Visibility.Visible : Visibility.Collapsed;

        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        public Visibility MessageVisibility
        {
            get => messageVisibility;
            set
            {
                messageVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool IsLeftPanelHighlighted
        {
            get => isLeftPanelHighlighted;
            set
            {
                isLeftPanelHighlighted = value;
                OnPropertyChanged();
            }
        }

        public bool IsRightPanelHighlighted
        {
            get => isRightPanelHighlighted;
            set
            {
                isRightPanelHighlighted = value;
                OnPropertyChanged();
            }
        }

        public string LastPastedFileName
        {
            get => lastPastedFileName;
            set
            {
                lastPastedFileName = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenFileCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand BackLeftCommand { get; }
        public ICommand BackRightCommand { get; }
        public ICommand SelectLeftDriveCommand { get; }
        public ICommand SelectRightDriveCommand { get; }
        public ICommand ToggleViewModeCommand { get; }
        public ICommand PreviewFileCommand { get; }

        public FileManagerViewModel()
        {
            LeftPanelFiles = new ObservableCollection<FileItem>();
            RightPanelFiles = new ObservableCollection<FileItem>();
            leftPathHistory = new Stack<string>();
            rightPathHistory = new Stack<string>();
            
            LoadAvailableDrives();
            
            currentLeftPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            currentRightPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            messageTimer = new DispatcherTimer();
            messageTimer.Interval = TimeSpan.FromSeconds(2);
            messageTimer.Tick += (s, e) => 
            {
                MessageVisibility = Visibility.Collapsed;
                messageTimer.Stop();
            };

            OpenFileCommand = new RelayCommand<FileItem>(OpenFile);
            CopyCommand = new RelayCommand(CopyToBuffer);
            CutCommand = new RelayCommand(CutToBuffer);
            PasteCommand = new RelayCommand(Paste, CanPaste);
            DeleteCommand = new RelayCommand(DeleteFile);
            BackLeftCommand = new RelayCommand(GoBackLeft);
            BackRightCommand = new RelayCommand(GoBackRight);
            SelectLeftDriveCommand = new RelayCommand<String>(SelectLeftDrive);
            SelectRightDriveCommand = new RelayCommand<String>(SelectRightDrive);
            ToggleViewModeCommand = new RelayCommand(ToggleViewMode);
            PreviewFileCommand = new RelayCommand(PreviewFile, CanPreviewFile);

            IsDualPanelMode = true;
            ToggleViewModeText = "Одна панель";
            MessageVisibility = Visibility.Collapsed;

            LoadFiles();
        }

        private void ToggleViewMode()
        {
            IsDualPanelMode = !IsDualPanelMode;
            OnPropertyChanged(nameof(RightPanelVisibility));
        }

        private void LoadDirectoryContents(string path, ObservableCollection<FileItem> collection)
        {
            collection.Clear();

            // Додаємо ".." для навігації вгору
            if (path != Path.GetPathRoot(path))
            {
                collection.Add(new FileItem { Name = "..", IsDirectory = true });
            }

            // Додаємо доступні диски, якщо ми в корені
            if (path == Path.GetPathRoot(path))
            {
                foreach (var drive in AvailableDrives)
                {
                    collection.Add(new FileItem
                    {
                        Name = $"{drive.Name} ({drive.VolumeLabel})",
                        Path = drive.RootDirectory.FullName,
                        IsDirectory = true,
                        Modified = drive.RootDirectory.LastWriteTime
                    });
                }
            }

            // Додаємо директорії
            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirInfo = new DirectoryInfo(dir);
                if (!dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    collection.Add(new FileItem
                    {
                        Name = Path.GetFileName(dir),
                        Path = dir,
                        IsDirectory = true,
                        Modified = Directory.GetLastWriteTime(dir)
                    });
                }
            }

            // Додаємо файли
            foreach (var file in Directory.GetFiles(path))
            {
                var fileInfo = new FileInfo(file);
                if (!fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    collection.Add(new FileItem
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        Size = fileInfo.Length,
                        Modified = File.GetLastWriteTime(file)
                    });
                }
            }

            // Якщо є останній вставлений файл, вибираємо його
            if (!string.IsNullOrEmpty(LastPastedFileName))
            {
                var pastedItem = collection.FirstOrDefault(item => item.Name == LastPastedFileName);
                if (pastedItem != null)
                {
                    if (path == currentLeftPath)
                    {
                        SelectedLeftItem = pastedItem;
                    }
                    else if (path == currentRightPath)
                    {
                        SelectedRightItem = pastedItem;
                    }
                }
            }
        }

        private void LoadAvailableDrives()
        {
            AvailableDrives = new ObservableCollection<DriveInfo>(
                DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .OrderBy(d => d.Name)
            );
        }

        private void SelectLeftDrive(String drive)
        {
            if (drive != null)
            {
                leftPathHistory.Push(currentLeftPath);
                currentLeftPath = drive;
                LoadDirectoryContents(currentLeftPath, LeftPanelFiles);
            }
        }

        private void SelectRightDrive(string drive)
        {
            if (drive != null)
            {
                rightPathHistory.Push(currentRightPath);
                currentRightPath = drive;
                LoadDirectoryContents(currentRightPath, RightPanelFiles);
            }
        }

        private void LoadFiles()
        {
            LoadDirectoryContents(currentLeftPath, LeftPanelFiles);
            LoadDirectoryContents(currentRightPath, RightPanelFiles);
        }

        public void OpenFile(FileItem selectedItem)
        {
            //var selectedItem = SelectedLeftItem ?? SelectedRightItem;
            if (selectedItem != null)
            {
                if (selectedItem.IsDirectory)
                {
                    if (selectedItem.Name == "..")
                    {
                        if (selectedItem == SelectedLeftItem)
                            GoBackLeft();
                        else
                            GoBackRight();
                        return;
                    }

                    try
                    {
                        if (selectedItem == SelectedLeftItem)
                        {
                            leftPathHistory.Push(currentLeftPath);
                            currentLeftPath = selectedItem.Path;
                            LoadDirectoryContents(currentLeftPath, LeftPanelFiles);
                        }
                        else
                        {
                            rightPathHistory.Push(currentRightPath);
                            currentRightPath = selectedItem.Path;
                            LoadDirectoryContents(currentRightPath, RightPanelFiles);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка при відкритті папки: {ex.Message}", "Помилка", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = selectedItem.Path,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка при відкритті файлу: {ex.Message}", "Помилка", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void GoBackLeft()
        {
            if (leftPathHistory.Count > 0)
            {
                currentLeftPath = leftPathHistory.Pop();
                LoadDirectoryContents(currentLeftPath, LeftPanelFiles);
            }
        }

        private void GoBackRight()
        {
            if (rightPathHistory.Count > 0)
            {
                currentRightPath = rightPathHistory.Pop();
                LoadDirectoryContents(currentRightPath, RightPanelFiles);
            }
        }

        private void ShowMessage(string message)
        {
            StatusMessage = message;
            MessageVisibility = Visibility.Visible;
            messageTimer.Start();
        }

        private void CopyToBuffer()
        {
            itemToPaste = SelectedLeftItem ?? SelectedRightItem;
            if (itemToPaste != null)
            {
                isCut = false;
                IsLeftPanelHighlighted = itemToPaste == SelectedLeftItem;
                IsRightPanelHighlighted = itemToPaste == SelectedRightItem;
                ShowMessage("Файл скопійовано");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CutToBuffer()
        {
            itemToPaste = SelectedLeftItem ?? SelectedRightItem;
            if (itemToPaste != null)
            {
                isCut = true;
                IsLeftPanelHighlighted = itemToPaste == SelectedLeftItem;
                IsRightPanelHighlighted = itemToPaste == SelectedRightItem;
                ShowMessage("Файл вирізано");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanPaste()
        {
            return itemToPaste != null;
        }

        private void Paste()
        {
            if (itemToPaste == null) return;

            try
            {
                string targetPath;
                var selectedItem = SelectedLeftItem ?? SelectedRightItem;
                bool isLeftPanelTarget = SelectedLeftItem != null;

                // Визначаємо шлях для вставки
                if (selectedItem != null && selectedItem.IsDirectory && selectedItem.Name != "..")
                {
                    // Якщо вибрана папка, вставляємо в неї
                    targetPath = selectedItem.Path;
                }
                else
                {
                    // Якщо папка не вибрана або це не папка, вставляємо в поточну директорію активної панелі
                    targetPath = isLeftPanelTarget ? currentLeftPath : currentRightPath;
                }

                // Перевіряємо чи не намагаємось вставити файл в ту ж саму папку
                if (Path.GetDirectoryName(itemToPaste.Path) == targetPath)
                {
                    ShowMessage("Неможливо вставити файл в ту ж саму папку");
                    return;
                }

                string targetFilePath = Path.Combine(targetPath, itemToPaste.Name);

                // Перевіряємо, чи файл вже існує в цільовій папці
                bool fileExists = (itemToPaste.IsDirectory && Directory.Exists(targetFilePath)) ||
                                 (!itemToPaste.IsDirectory && File.Exists(targetFilePath));

                if (fileExists)
                {
                    var result = MessageBox.Show(
                        "Файл з такою назвою вже існує. Замінити його?",
                        "Підтвердження",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                // Зберігаємо ім'я файлу перед вставкою
                LastPastedFileName = itemToPaste.Name;

                try
                {
                    if (isCut)
                    {
                        if (itemToPaste.IsDirectory)
                        {
                            if (Directory.Exists(targetFilePath))
                            {
                                Directory.Delete(targetFilePath, true);
                            }
                            Directory.Move(itemToPaste.Path, targetFilePath);
                        }
                        else
                        {
                            if (File.Exists(targetFilePath))
                            {
                                File.Delete(targetFilePath);
                            }
                            File.Move(itemToPaste.Path, targetFilePath);
                        }
                        ShowMessage("Файл переміщено");
                        itemToPaste = null;
                    }
                    else
                    {
                        if (itemToPaste.IsDirectory)
                        {
                            if (Directory.Exists(targetFilePath))
                            {
                                Directory.Delete(targetFilePath, true);
                            }
                            CopyDirectory(itemToPaste.Path, targetFilePath);
                        }
                        else
                        {
                            File.Copy(itemToPaste.Path, targetFilePath, true);
                        }
                        ShowMessage("Файл вставлено");
                    }

                    // Після вставки знімаємо підсвітку
                    IsLeftPanelHighlighted = false;
                    IsRightPanelHighlighted = false;

                    // Оновлюємо вміст обох панелей
                    LoadDirectoryContents(currentLeftPath, LeftPanelFiles);
                    LoadDirectoryContents(currentRightPath, RightPanelFiles);

                    // Запускаємо таймер для скидання підсвітки
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Tick += (s, e) =>
                    {
                        LastPastedFileName = null;
                        timer.Stop();
                    };
                    timer.Start();

                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    LastPastedFileName = null;
                    MessageBox.Show($"Помилка при операції з файлом: {ex.Message}\nШлях: {targetFilePath}", "Помилка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LastPastedFileName = null;
                MessageBox.Show($"Помилка при вставці: {ex.Message}", "Помилка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteFile()
        {
            var selectedItem = SelectedLeftItem ?? SelectedRightItem;
            if (selectedItem != null)
            {
                try
                {
                    if (selectedItem.IsDirectory)
                    {
                        Directory.Delete(selectedItem.Path, true);
                    }
                    else
                    {
                        File.Delete(selectedItem.Path);
                    }
                    ShowMessage("Файл видалено");
                    LoadFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при видаленні: {ex.Message}", "Помилка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
            }
        }

        private bool CanPreviewFile()
        {
            var selectedItem = SelectedLeftItem ?? SelectedRightItem;
            return selectedItem != null && !selectedItem.IsDirectory;
        }

        private void PreviewFile()
        {
            var selectedItem = SelectedLeftItem ?? SelectedRightItem;
            if (selectedItem != null && !selectedItem.IsDirectory)
            {
                try
                {
                    var previewWindow = new FilePreviewWindow(selectedItem.Path);
                    previewWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при перегляді файлу: {ex.Message}", "Помилка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }

    public class FilePreviewWindow : Window
    {
        private TextBlock fileNameText;
        private TextBox contentTextBox;
        private Image imagePreview;
        private ScrollViewer scrollViewer;

        public FilePreviewWindow(string filePath)
        {
            InitializeWindow();
            LoadContent(filePath);
        }

        private void InitializeWindow()
        {
            Title = "Перегляд файлу";
            Height = 450;
            Width = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            fileNameText = new TextBlock
            {
                Margin = new Thickness(10),
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(fileNameText, 0);

            scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(10),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scrollViewer, 1);

            contentTextBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            imagePreview = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };

            scrollViewer.Content = new Grid
            {
                Children = { contentTextBox, imagePreview }
            };

            grid.Children.Add(fileNameText);
            grid.Children.Add(scrollViewer);

            Content = grid;
        }

        private void LoadContent(string filePath)
        {
            try
            {
                fileNameText.Text = Path.GetFileName(filePath);
                string extension = Path.GetExtension(filePath).ToLower();

                if (IsImageFile(extension))
                {
                    LoadImage(filePath);
                }
                else if (IsTextFile(extension))
                {
                    LoadText(filePath);
                }
                else
                {
                    ShowMessage("Цей тип файлу не підтримує перегляд.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Помилка при читанні файлу: {ex.Message}");
            }
        }

        private void LoadImage(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            imagePreview.Source = bitmap;
            imagePreview.Visibility = Visibility.Visible;
            contentTextBox.Visibility = Visibility.Collapsed;
        }

        private void LoadText(string filePath)
        {
            contentTextBox.Text = File.ReadAllText(filePath);
            contentTextBox.Visibility = Visibility.Visible;
            imagePreview.Visibility = Visibility.Collapsed;
        }

        private void ShowMessage(string message)
        {
            contentTextBox.Text = message;
            contentTextBox.Visibility = Visibility.Visible;
            imagePreview.Visibility = Visibility.Collapsed;
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
            return imageExtensions.Contains(extension);
        }

        private bool IsTextFile(string extension)
        {
            string[] textExtensions = { 
                ".txt", ".log", ".ini", ".cfg", ".conf", ".config",  // Конфігураційні файли
                ".cs", ".cpp", ".h", ".hpp", ".java", ".py", ".rb",  // Код
                ".js", ".ts", ".html", ".css", ".scss", ".less",     // Веб
                ".xml", ".json", ".yaml", ".yml",                    // Дані
                ".md", ".markdown", ".rst",                          // Документація
                ".bat", ".cmd", ".ps1", ".sh",                       // Скрипти
                ".csv", ".tsv",                                      // Таблиці
                ".sql",                                              // База даних
                ".gitignore", ".env",                               // Системні
                ".xaml"                                             // XAML
            };
            return textExtensions.Contains(extension.ToLower());
        }
    }

    public class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
