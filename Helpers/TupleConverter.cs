using System;
using System.Globalization;
using CryptoApp.Models;
using Microsoft.Maui.Controls;

namespace CryptoApp.Helpers
{
    public class TupleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Transaction transaction)
            {
                return (transaction.Symbol, transaction.Amount);
            }
            return (string.Empty, 0m);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}