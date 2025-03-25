using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;

namespace FileManager3
{
    public class FileItem : INotifyPropertyChanged
    {
        private string name;
        private string path;
        private long size;
        private DateTime modified;
        private bool isDirectory;
        private string extension;
        private string formattedSize;
        private string formattedDate;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get => path;
            set
            {
                path = value;
                OnPropertyChanged();
            }
        }

        public long Size
        {
            get => size;
            set
            {
                size = value;
                FormattedSize = FormatFileSize(value);
                OnPropertyChanged();
            }
        }

        public DateTime Modified
        {
            get => modified;
            set
            {
                modified = value;
                FormattedDate = value.ToString("dd.MM.yyyy HH:mm:ss");
                OnPropertyChanged();
            }
        }

        public bool IsDirectory
        {
            get => isDirectory;
            set
            {
                isDirectory = value;
                OnPropertyChanged();
            }
        }

        public string Extension
        {
            get => extension;
            set
            {
                extension = value;
                OnPropertyChanged();
            }
        }

        public string FormattedSize
        {
            get => formattedSize;
            private set
            {
                formattedSize = value;
                OnPropertyChanged();
            }
        }

        public string FormattedDate
        {
            get => formattedDate;
            private set
            {
                formattedDate = value;
                OnPropertyChanged();
            }
        }

        public FileItem()
        {
        }

        public FileItem(FileInfo file)
        {
            Name = file.Name;
            Path = file.FullName;
            Size = file.Length;
            Modified = file.LastWriteTime;
            IsDirectory = false;
            Extension = file.Extension;
        }

        public FileItem(DirectoryInfo directory)
        {
            Name = directory.Name;
            Path = directory.FullName;
            Size = 0;
            Modified = directory.LastWriteTime;
            IsDirectory = true;
            Extension = "<DIR>";
        }

        private string FormatFileSize(long bytes)
        {
            if (IsDirectory) return "<DIR>";
            
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
