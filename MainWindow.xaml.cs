using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            var currentDirPath = Environment.CurrentDirectory;
            var currentDir = new TabItem
            {
                Name = Path.GetFileName(currentDirPath) ?? currentDirPath,
                FullPath = currentDirPath,
                IsDirectory = true,
                IsActive = true
            };
            
            _openTabs.Add(currentDir);
            _currentTab = currentDir;
            
            TabListBox.ItemsSource = _openTabs;
            DisplayTab(currentDir);
            UpdateWindowTitle();
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

        private void DirectoryItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is FileItem selectedItem)
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
                
                if (IsCodeFile(extension))
                {
                    DisplayCodeFile(filePath, extension);
                }
                else if (IsTextFile(extension))
                {
                    DisplayTextFile(filePath);
                }
                else if (IsImageFile(extension))
                {
                    DisplayImageFile(filePath);
                }
                else if (IsKnownBinaryFile(extension))
                {
                    DisplayBinaryFileStats(filePath, extension);
                }
                else
                {
                    // Check if file is text or binary
                    if (IsTextFileContent(filePath))
                    {
                        DisplayTextFile(filePath);
                    }
                    else
                    {
                        DisplayBinaryFileStats(filePath, extension);
                    }
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
            CodeEditor.Visibility = Visibility.Collapsed;
            TextViewer.Visibility = Visibility.Collapsed;
            ImageViewer.Visibility = Visibility.Collapsed;
            StatsScrollViewer.Visibility = Visibility.Collapsed;
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
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            if (_currentTab != null)
            {
                Title = _currentTab.FullPath;
            }
        }

        private bool IsCodeFile(string extension)
        {
            string[] codeExtensions = { ".cs", ".js", ".ts", ".html", ".css", ".xml", ".json", ".yml", ".yaml", ".cpp", ".c", ".h", ".java", ".py", ".rb", ".php", ".go", ".rs", ".sql" };
            return codeExtensions.Contains(extension);
        }

        private bool IsTextFile(string extension)
        {
            string[] textExtensions = { ".txt", ".log", ".md", ".ini", ".cfg", ".conf", ".bat", ".sh" };
            return textExtensions.Contains(extension);
        }

        private bool IsKnownBinaryFile(string extension)
        {
            string[] binaryExtensions = { ".exe", ".dll", ".mp3", ".mp4", ".avi", ".zip", ".rar", ".7z", ".pdf", ".docx", ".xlsx" };
            return binaryExtensions.Contains(extension);
        }

        private bool IsTextFileContent(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                
                // Check for null bytes which indicate binary content
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == 0)
                        return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico" };
            return imageExtensions.Contains(extension);
        }

        private void DisplayCodeFile(string filePath, string extension)
        {
            var content = File.ReadAllText(filePath);
            CodeEditor.Text = content;
            
            // Set syntax highlighting based on extension
            var highlightingName = extension switch
            {
                ".cs" => "C#",
                ".js" or ".ts" => "JavaScript",
                ".html" or ".htm" => "HTML",
                ".css" => "CSS",
                ".xml" => "XML",
                ".json" => "JavaScript",
                ".sql" => "SQL",
                ".cpp" or ".c" or ".h" => "C++",
                ".java" => "Java",
                ".py" => "Python",
                ".php" => "PHP",
                _ => null
            };
            
            if (!string.IsNullOrEmpty(highlightingName))
            {
                CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(highlightingName);
            }
            
            CodeEditor.Visibility = Visibility.Visible;
        }

        private void DisplayBinaryFileStats(string filePath, string extension)
        {
            var fileInfo = new FileInfo(filePath);
            var stats = new List<KeyValuePair<string, string>>
            {
                new("File Name", fileInfo.Name),
                new("File Size", FormatFileSize(fileInfo.Length)),
                new("Created", fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new("Modified", fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new("Extension", extension.ToUpper())
            };

            // Add specific stats for known binary types
            try
            {
                switch (extension)
                {
                    case ".exe" or ".dll":
                        AddExecutableStats(stats, filePath);
                        break;
                    case ".mp3":
                        AddAudioStats(stats, filePath);
                        break;
                    case ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp":
                        AddImageStats(stats, filePath);
                        break;
                }
            }
            catch
            {
                // Ignore errors when getting specific stats
            }

            StatsTitle.Text = $"File Information: {fileInfo.Name}";
            StatsItems.ItemsSource = stats;
            StatsScrollViewer.Visibility = Visibility.Visible;
        }

        private void AddExecutableStats(List<KeyValuePair<string, string>> stats, string filePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                if (!string.IsNullOrEmpty(versionInfo.FileVersion))
                    stats.Add(new("File Version", versionInfo.FileVersion));
                if (!string.IsNullOrEmpty(versionInfo.ProductName))
                    stats.Add(new("Product Name", versionInfo.ProductName));
                if (!string.IsNullOrEmpty(versionInfo.CompanyName))
                    stats.Add(new("Company", versionInfo.CompanyName));
                if (!string.IsNullOrEmpty(versionInfo.FileDescription))
                    stats.Add(new("Description", versionInfo.FileDescription));
            }
            catch
            {
                // Ignore version info errors
            }
        }

        private void AddAudioStats(List<KeyValuePair<string, string>> stats, string filePath)
        {
            // Basic audio file stats - would need additional library for detailed metadata
            stats.Add(new("Type", "Audio File"));
        }

        private void AddImageStats(List<KeyValuePair<string, string>> stats, string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                if (decoder.Frames.Count > 0)
                {
                    var frame = decoder.Frames[0];
                    stats.Add(new("Dimensions", $"{frame.PixelWidth} x {frame.PixelHeight}"));
                    stats.Add(new("DPI", $"{frame.DpiX:F0} x {frame.DpiY:F0}"));
                    stats.Add(new("Pixel Format", frame.Format.ToString()));
                }
            }
            catch
            {
                // Ignore image stats errors
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

    }
}