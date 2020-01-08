using System;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class PriceToStringConverter : IValueConverter
    {
        public string Convert(int value) => value.ToString() + " 元";

        public object Convert(object value, Type targetType, object parameter, string language) =>
            Convert((int)value);

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
