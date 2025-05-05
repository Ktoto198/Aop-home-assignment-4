using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ResSim.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return booleanValue ?  Brushes.White : Brushes.Transparent;
            }
            return Brushes.Transparent;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // ConvertBack is not needed
        }
    }
}
