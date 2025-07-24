using System.Collections.ObjectModel;
using System.IO;

namespace FileViewer
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public ObservableCollection<FileItem> Children { get; set; }

        public FileItem()
        {
            Children = new ObservableCollection<FileItem>();
        }

        public string Icon
        {
            get
            {
                return IsDirectory ? "📁" : GetFileIcon(FullPath);
            }
        }

        private string GetFileIcon(string filePath)
        {
            var extension = Path.GetExtension(filePath)?.ToLower() ?? string.Empty;
            return extension switch
            {
                ".txt" or ".log" or ".md" => "📄",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".exe" or ".dll" => "⚙️",
                ".zip" or ".rar" or ".7z" => "📦",
                _ => "📄"
            };
        }
    }
}