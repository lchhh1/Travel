using System;
using Windows.UI.Xaml.Data;

namespace Travel
{
    public sealed class TechnologyToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            (Technology)value switch
            {
                Technology.Bus => "\uE806", // Bus
                Technology.Train => "\uE7C0", // Train
                Technology.Flight => "\uE709", // AirPlane
                _ => throw new ArgumentOutOfRangeException()
            };

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
