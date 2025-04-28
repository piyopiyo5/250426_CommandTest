using System;
using System.Globalization;
using System.Windows.Data;

namespace CommandTest.Models
{
    public class DelimiterTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string delimiter)
            {
                return delimiter switch
                {
                    "CR" => "CR (\\r)",
                    "LF" => "LF (\\n)",
                    "CRLF" => "CR+LF (\\r\\n)",
                    _ => delimiter
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string display)
            {
                return display switch
                {
                    "CR (\\r)" => "CR",
                    "LF (\\n)" => "LF",
                    "CR+LF (\\r\\n)" => "CRLF",
                    _ => display
                };
            }
            return value;
        }
    }
}
