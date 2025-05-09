using System;
using System.Globalization;
using System.Windows.Data;

namespace MyGraph.Converters
{
    public class HeightSubtractor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height && parameter is string paramStr && double.TryParse(paramStr, out double subtractValue))
            {
                return Math.Max(0, height - subtractValue);
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 