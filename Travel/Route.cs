using System;

namespace Travel
{
    public sealed class Route
    {
        public Route(string name, City to, int price, TimeSpan departureTime, TimeSpan duration)
        {
            Name = name;
            To = to;
            Price = price;
            DepartureTime = departureTime;
            Duration = duration;
        }

        public string Name { get; }

        public City To { get; }

        public int Price { get; }

        public TimeSpan DepartureTime { get; }

        public TimeSpan Duration { get; }

        public static Route Create(Technology technology, string name, City source, City to, int price, TimeSpan departureTime, TimeSpan duration)
        {
            Route route = new Route(name, to, price, departureTime, duration);
            source.AddRoute(technology, route);
            return route;
        }
    }
}
