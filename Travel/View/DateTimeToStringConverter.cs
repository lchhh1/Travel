using System;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class DateTimeToStringConverter : IValueConverter
    {
        public string Convert(DateTime? value) => value?.ToString("MM-dd HH:mm");

        public object Convert(object value, Type targetType, object parameter, string language) =>
            Convert(value as DateTime?);

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
