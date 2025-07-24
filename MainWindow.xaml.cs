using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FileViewer
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<TabItem> _openTabs = null!;
        private TabItem? _currentTab = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileTree();
        }

        private void InitializeFileTree()
        {
            _openTabs = new ObservableCollection<TabItem>();
            
            // Add current directory as the first tab
            var currentDir = new TabItem
            {
                Name = "Current Directory",
                FullPath = Environment.CurrentDirectory,
                IsDirectory = true,
                IsActive = true
            };
            
            _openTabs.Add(currentDir);
            _currentTab = currentDir;
            
            TabListBox.ItemsSource = _openTabs;
            DisplayTab(currentDir);
        }

        private ObservableCollection<FileItem> LoadDirectoryContents(string directoryPath)
        {
            var items = new ObservableCollection<FileItem>();
            
            try
            {
                if (!Directory.Exists(directoryPath))
                    return items;

                var directories = Directory.GetDirectories(directoryPath)
                    .Select(dir => new FileItem
                    {
                        Name = Path.GetFileName(dir),
                        FullPath = dir,
                        IsDirectory = true
                    });

                var files = Directory.GetFiles(directoryPath)
                    .Select(file => new FileItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        IsDirectory = false
                    });

                foreach (var item in directories.Concat(files).OrderBy(i => !i.IsDirectory).ThenBy(i => i.Name))
                {
                    items.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception)
            {
            }
            
            return items;
        }

        private void TabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
            {
                SetActiveTab(selectedTab);
                DisplayTab(selectedTab);
            }
        }

        private void DirectoryViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem selectedItem)
            {
                OpenTab(selectedItem.FullPath, selectedItem.Name, selectedItem.IsDirectory);
            }
        }

        private void DisplayTab(TabItem tab)
        {
            try
            {
                HideAllViewers();
                
                if (tab.IsDirectory)
                {
                    DisplayDirectory(tab.FullPath);
                }
                else
                {
                    DisplayFile(tab.FullPath);
                }
            }
            catch (Exception ex)
            {
                PlaceholderText.Text = $"Error loading content: {ex.Message}";
                PlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void DisplayDirectory(string directoryPath)
        {
            var items = LoadDirectoryContents(directoryPath);
            DirectoryViewer.ItemsSource = items;
            DirectoryViewer.Visibility = Visibility.Visible;
        }

        private void DisplayFile(string filePath)
        {
            try
            {
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
            DirectoryViewer.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Collapsed;
        }

        private void OpenTab(string path, string name, bool isDirectory)
        {
            // Check if tab already exists
            var existingTab = _openTabs.FirstOrDefault(t => t.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (existingTab != null)
            {
                SetActiveTab(existingTab);
                TabListBox.SelectedItem = existingTab;
                return;
            }

            // Create new tab
            var newTab = new TabItem
            {
                Name = name,
                FullPath = path,
                IsDirectory = isDirectory,
                IsActive = false
            };

            _openTabs.Add(newTab);
            SetActiveTab(newTab);
            TabListBox.SelectedItem = newTab;
        }

        private void SetActiveTab(TabItem activeTab)
        {
            foreach (var tab in _openTabs)
            {
                tab.IsActive = tab == activeTab;
            }
            _currentTab = activeTab;
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
                var fileName = Path.GetFileName(openFileDialog.FileName);
                OpenTab(openFileDialog.FileName, fileName, false);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}