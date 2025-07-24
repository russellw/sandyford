using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FileViewer
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileItem> _rootItems = null!;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileTree();
        }

        private void InitializeFileTree()
        {
            _rootItems = new ObservableCollection<FileItem>();
            
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            foreach (var drive in drives)
            {
                var driveItem = new FileItem
                {
                    Name = drive.Name,
                    FullPath = drive.Name,
                    IsDirectory = true
                };
                LoadDirectoryChildren(driveItem);
                _rootItems.Add(driveItem);
            }
            
            FileTreeView.ItemsSource = _rootItems;
        }

        private void LoadDirectoryChildren(FileItem parent)
        {
            try
            {
                if (!Directory.Exists(parent.FullPath))
                    return;

                var directories = Directory.GetDirectories(parent.FullPath)
                    .Take(50)
                    .Select(dir => new FileItem
                    {
                        Name = Path.GetFileName(dir),
                        FullPath = dir,
                        IsDirectory = true
                    });

                var files = Directory.GetFiles(parent.FullPath)
                    .Take(100)
                    .Select(file => new FileItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        IsDirectory = false
                    });

                parent.Children.Clear();
                foreach (var item in directories.Concat(files))
                {
                    if (item.IsDirectory)
                        LoadDirectoryChildren(item);
                    parent.Children.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception)
            {
            }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileItem selectedItem && !selectedItem.IsDirectory)
            {
                DisplayFile(selectedItem.FullPath);
            }
        }

        private void DisplayFile(string filePath)
        {
            try
            {
                HideAllViewers();
                
                var extension = Path.GetExtension(filePath)?.ToLower() ?? string.Empty;
                
                if (IsTextFile(extension))
                {
                    DisplayTextFile(filePath);
                }
                else if (IsImageFile(extension))
                {
                    DisplayImageFile(filePath);
                }
                else
                {
                    PlaceholderText.Text = $"Cannot preview files of type: {extension}";
                    PlaceholderText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                PlaceholderText.Text = $"Error loading file: {ex.Message}";
                PlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void DisplayTextFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            TextViewer.Text = content;
            TextViewer.Visibility = Visibility.Visible;
        }

        private void DisplayImageFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                ImageViewer.Source = bitmap;
                ImageViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PlaceholderText.Text = $"Error loading image: {ex.Message}";
                PlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void HideAllViewers()
        {
            TextViewer.Visibility = Visibility.Collapsed;
            ImageViewer.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Collapsed;
        }

        private bool IsTextFile(string extension)
        {
            string[] textExtensions = { ".txt", ".log", ".md", ".cs", ".js", ".html", ".css", ".xml", ".json", ".yml", ".yaml", ".ini", ".cfg", ".conf" };
            return textExtensions.Contains(extension);
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico" };
            return imageExtensions.Contains(extension);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a file to view",
                Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Image files (*.jpg;*.png;*.gif;*.bmp)|*.jpg;*.png;*.gif;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                DisplayFile(openFileDialog.FileName);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}