using System.IO;

namespace FileViewer
{
    public class TabItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public bool IsActive { get; set; }

        public string DisplayName
        {
            get
            {
                if (IsDirectory)
                    return string.IsNullOrEmpty(Name) ? FullPath : Name;
                return Name;
            }
        }

        public string Icon
        {
            get
            {
                if (IsDirectory)
                    return "📁";
                
                var extension = Path.GetExtension(FullPath)?.ToLower() ?? string.Empty;
                return extension switch
                {
                    ".txt" or ".log" or ".md" => "📄",
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                    ".exe" or ".dll" => "⚙️",
                    ".zip" or ".rar" or ".7z" => "📦",
                    ".cs" => "💻",
                    ".js" or ".ts" => "🟨",
                    ".html" or ".htm" => "🌐",
                    _ => "📄"
                };
            }
        }
    }
}