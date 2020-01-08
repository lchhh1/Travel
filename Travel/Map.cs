using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Devices.Geolocation;
using Windows.Storage;

namespace Travel
{
/// <summary>
/// 用于读取相关数据文件
/// </summary>
    public static class Map
    {
        private static Province[] _provinces;
        private static City[] _cities;

        public static int CityNumber => _cities.Length;

        public static IList<Province> Provinces => _provinces;

        public static IList<City> Cities => _cities;

        public static City GetNearestCity(BasicGeoposition position)
        {
            var result = _cities.Min(city => (distance: GetDistance(city.Geoposition, position), city));
            return result.distance < 0.5 ? result.city : null;
        }

        public static async void Load()
        {
            await LoadMap();
            await Task.WhenAll(LoadBuses(), LoadTrains(), LoadFlights());
        }

        private static async Task LoadMap()
        {
            var uri = new Uri("ms-appx:///Map.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var items = JsonArray.Parse(await FileIO.ReadTextAsync(file));
            _provinces = new Province[items.Count];
            _cities = new City[items.Count];
            int index = 0;
            foreach (var (province, city) in
                from item in
                    from value in items select value.GetObject()
                let provinceShort = item.GetNamedString("province_short")
                let provinceLong = item.GetNamedString("province_long")
                let special = item.GetNamedBoolean("special", false)
                let cityShort = special ? provinceShort : item.GetNamedString("city_short")
                let cityLong = special ? provinceLong : item.GetNamedString("city_long")
                let location = item.GetNamedObject("geoposition")
                let longtitude = location.GetNamedNumber("longtitude")
                let latitude = location.GetNamedNumber("latitude")
                select (new Province(provinceShort, provinceLong, special),
                        new City(index++, cityShort, cityLong, longtitude, latitude)))
            {
                province.City = city;
                city.Province = province;
                _provinces[city.Index] = province;
                _cities[city.Index] = city;
            }
        }

        private static async Task LoadBuses()
        {
            var uri = new Uri("ms-appx:///Buses.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var items = JsonArray.Parse(await FileIO.ReadTextAsync(file));
            foreach (var _ in
                from item in
                    from value in items select value.GetObject()
                let fromName = item.GetNamedString("from")
                let fromCity = FindCity(fromName)
                let toArray = item.GetNamedArray("to")
                from toName in
                    from value in toArray select value.GetString()
                let toCity = FindCity(toName)
                let distance = Distance(fromCity, toCity)
                select Route.Create(
                    Technology.Bus,
                    "汽车",
                    fromCity,
                    toCity,
                    (int)(distance * 0.4),
                    default,
                    TimeSpan.FromHours(distance / 100)))
            {
            }

            City FindCity(string name) => Array.Find(_cities, city => city.ShortName == name);

            static double Distance(City from, City to) =>
                100 * Math.Sqrt(
                    Math.Pow(to.Geoposition.Longitude - from.Geoposition.Longitude, 2) +
                    Math.Pow(to.Geoposition.Latitude - from.Geoposition.Latitude, 2));
        }

        private static async Task LoadTrains()
        {
            var stations = await LoadTrainStations();
            var uri = new Uri("ms-appx:///Trains.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var items = JsonArray.Parse(await FileIO.ReadTextAsync(file));
            foreach (var _ in
                from item in
                    from value in items select value.GetObject()
                let name = item.GetNamedString("train_name")
                let fromStation = item.GetNamedString("ticket_from")
                let fromCity = FindCity(fromStation)
                let toStation = item.GetNamedString("ticket_to")
                let toCity = FindCity(toStation)
                let price = (int)float.Parse(item.GetNamedString("price"))
                let departureTime = TimeSpan.Parse(item.GetNamedString("ticket_start_time"))
                let durationArray = item.GetNamedString("ticket_length").Split(':')
                let durationHours = int.Parse(durationArray[0])
                let durationMinutes = int.Parse(durationArray[1])
                select fromCity != null && toCity != null ?
                    Route.Create(Technology.Train,
                    name,
                    fromCity,
                    toCity,
                    price,
                    departureTime,
                    new TimeSpan(durationHours, durationMinutes, 0)) : null)
            {
            }

            City FindCity(string name)
            {
                var line = Array.Find(stations, s => s[1] == name);
                return line != null ? Array.Find(_cities, city => line[2].StartsWith(
                    city.ShortName, StringComparison.OrdinalIgnoreCase)) : null;
            }
        }

        private static async Task<string[][]> LoadTrainStations()
        {
            var uri = new Uri("ms-appx:///TrainStations.txt");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            return (from line in await FileIO.ReadLinesAsync(file) select line.Split(' ')).ToArray();
        }

        private static async Task LoadFlights()
        {
            var uri = new Uri("ms-appx:///Flights.json");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var items = JsonArray.Parse(await FileIO.ReadTextAsync(file));
            foreach (var _ in
                from item in
                    from value in items select value.GetObject()
                let name = item.GetNamedString("number")
                let fromName = item.GetNamedString("from")
                let fromCity = FindCity(fromName)
                let toName = item.GetNamedString("to")
                let toCity = FindCity(toName)
                let price = (int)item.GetNamedNumber("price")
                let departureDateTime = DateTime.Parse(item.GetNamedString("depart_time"))
                let arrivalDateTime = DateTime.Parse(item.GetNamedString("arrival_time"))
                select Route.Create(Technology.Flight,
                    name,
                    fromCity,
                    toCity,
                    price,
                    departureDateTime.TimeOfDay,
                    arrivalDateTime - departureDateTime))
            {
            }

            City FindCity(string name) => Array.Find(_cities, city => city.LocalName.StartsWith(name));
        }

        private static double GetDistance(BasicGeoposition from, BasicGeoposition to) =>
            Math.Sqrt(Math.Pow(to.Longitude - from.Longitude, 2) +Math.Pow(to.Latitude - from.Latitude, 2));
    }
}
