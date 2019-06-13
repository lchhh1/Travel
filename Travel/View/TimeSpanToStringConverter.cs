using System;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is TimeSpan time ? time.ToString(
                (time.Days > 0 ? "%d' 天 '" : null) + (time.Hours > 0 ? "%h' 小时 '" : null) + "%m' 分钟'") : null;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
