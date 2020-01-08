using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Geolocation;

namespace Travel
{
    /// <summary>
    /// 城市的命名规则
    /// </summary>
    public sealed class City
    {
        public City(int index, string shortName, string longName, double longtitude, double latitude)
        {
            Index = index;
            ShortName = shortName;
            LongName = longName;
            LocalName = Localization.GetString(ShortName);
            Geoposition = new BasicGeoposition { Longitude = longtitude, Latitude = latitude };
        }

        public int Index { get; }

        public string ShortName { get; }

        public string LongName { get; }

        public string FullName => (Province.Special ? null : Province.LongName) + LongName;

        public string LocalName { get; }

        public string FullLocalName => (Province.Special ? null : Province.LocalName) + LocalName;

        public BasicGeoposition Geoposition { get; }

        public Province Province { get; set; }

        public ICollection<Route> Buses { get; } = new List<Route>();

        public ICollection<Route> Trains { get; } = new List<Route>();

        public ICollection<Route> Flights { get; } = new List<Route>();

        public void AddRoute(Technology technology, Route route) =>
            (technology switch
            {
                Technology.Bus => Buses,
                Technology.Train => Trains,
                Technology.Flight => Flights,
                _ => throw new ArgumentOutOfRangeException()
            }).Add(route);

        public IEnumerable<Route> EnumerateRoutes(Technology technology, TimeSpan timeOfDay) =>
            technology switch
            {
                Technology.Bus => Buses.Select(bus => new Route(bus.Name, bus.To, bus.Price, TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours)), bus.Duration)),
                Technology.Train => Trains,
                Technology.Flight => Flights,
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}
