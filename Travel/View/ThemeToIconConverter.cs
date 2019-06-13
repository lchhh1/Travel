using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class ThemeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            (ElementTheme)value switch
            {
                ElementTheme.Light => "\uE706", // Brightness
                ElementTheme.Dark => "\uE708", // QuietHours
                _ => throw new ArgumentOutOfRangeException()
            };

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
