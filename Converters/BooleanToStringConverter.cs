using System;
using System.Globalization;
using System.Windows.Data;

namespace MyGraph.Converters
{
  /// <summary>
  /// Converts a boolean value to a string based on parameter in the format "TrueValue;FalseValue"
  /// </summary>
  public class BooleanToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is bool boolValue))
        return string.Empty;
      
      string paramString = parameter as string;
      if (string.IsNullOrEmpty(paramString))
        return boolValue.ToString();

      string[] parts = paramString.Split(';');
      if (parts.Length != 2)
        return boolValue.ToString();

      return boolValue ? parts[0] : parts[1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
} 