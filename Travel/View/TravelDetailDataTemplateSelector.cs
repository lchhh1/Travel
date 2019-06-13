using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Travel
{
    public sealed class WayPointDataTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var control = ItemsControl.ItemsControlFromItemContainer(container);
            return item switch
            {
                TravelStop _ => control.Resources["TravelStopTemplate"] as DataTemplate,
                TravelStep _ => control.Resources["TravelStepTemplate"] as DataTemplate,
                TravelNull _ => control.Resources["TravelNullTemplate"] as DataTemplate,
                _ => null
            };
        }
    }
}
