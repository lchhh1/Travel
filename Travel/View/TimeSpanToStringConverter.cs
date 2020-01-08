using System;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class TimeSpanToStringConverter : IValueConverter
    {
        public string Convert(TimeSpan? value) => value?.ToString(
                (value?.Days > 0 ? "%d' 天 '" : null) + (value?.Hours > 0 ? "%h' 小时 '" : null) + "%m' 分钟'");

        public object Convert(object value, Type targetType, object parameter, string language) =>
            Convert(value as TimeSpan?);

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
