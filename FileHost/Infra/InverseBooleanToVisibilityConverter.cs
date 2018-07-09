using System;
using System.Windows;
using System.Windows.Data;

namespace FileHost.Infra
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            var flag = false;
            if (value is bool b)
                flag = b;

            return (Visibility)(flag ? 2 : 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Collapsed;
            return false;
        }
    }
}
