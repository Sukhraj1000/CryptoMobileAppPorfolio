using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CryptoApp.Converters
{
    public class PercentageColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal percentageChange)
            {
                return percentageChange >= 0 ? Colors.Green : Colors.Red;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}