using Windows.ApplicationModel.Resources;

namespace Travel
{
    public static class Localization
    {
        private static ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView();

        public static string GetString(string key) => _resourceLoader.GetString(key);
    }
}
