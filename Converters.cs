using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FileViewer
{
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
                return new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Blue background for active
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDirectory)
                return isDirectory ? "Folder" : "File";
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}