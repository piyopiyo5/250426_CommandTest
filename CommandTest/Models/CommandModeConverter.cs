using System;
using System.Globalization;
using System.Windows.Data;

namespace CommandTest.Models
{
    public class CommandModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string mode)
            {
                return mode switch
                {
                    "Normal" => "通常モード",
                    "NoCommand" => "受信のみ",
                    "NoResponse" => "送信のみ",
                    _ => mode
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
                    "通常モード" => "Normal",
                    "受信のみ" => "NoCommand",
                    "送信のみ" => "NoResponse",
                    _ => display
                };
            }
            return value;
        }
    }
}
