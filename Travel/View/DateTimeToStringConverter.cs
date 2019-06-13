using System;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is DateTime time ? time.ToString("MM-dd HH:mm") : null;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
