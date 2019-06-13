using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class ApplicationThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            (ElementTheme)value switch
            {
                ElementTheme.Light => ApplicationTheme.Light,
                ElementTheme.Dark => ApplicationTheme.Dark,
                _ => throw new ArgumentOutOfRangeException()
            };

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
