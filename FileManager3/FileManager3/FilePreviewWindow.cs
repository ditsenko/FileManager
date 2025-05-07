using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FileManager3
{
    public class FilePreviewWindow : Window
    {
        private TextBlock fileNameText;
        private TextBox contentTextBox;

        public FilePreviewWindow(string filePath)
        {
            InitializeWindow();
            LoadFileContent(filePath);
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

            contentTextBox = new TextBox
            {
                Margin = new Thickness(10),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(contentTextBox, 1);

            grid.Children.Add(fileNameText);
            grid.Children.Add(contentTextBox);

            Content = grid;
        }

        private void LoadFileContent(string filePath)
        {
            try
            {
                fileNameText.Text = Path.GetFileName(filePath);
                string extension = Path.GetExtension(filePath).ToLower();

                if (IsTextFile(extension))
                {
                    contentTextBox.Text = File.ReadAllText(filePath);
                }
                else
                {
                    contentTextBox.Text = "Цей тип файлу не підтримує перегляд.";
                }
            }
            catch (Exception ex)
            {
                contentTextBox.Text = $"Помилка при читанні файлу: {ex.Message}";
            }
        }

        private bool IsTextFile(string extension)
        {
            string[] textExtensions = { ".txt", ".cs", ".xaml", ".xml", ".json", ".html", ".css", ".js", ".log", ".md", ".ini", ".config", ".bat", ".ps1", ".sh" };
            return textExtensions.Contains(extension);
        }
    }
} 