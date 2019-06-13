using System;

namespace Travel
{
    public abstract class TravelDetail
    {
        public abstract string Name { get; }
    }

    public sealed class TravelStop : TravelDetail
    {
        public TravelStop(City city, DateTime arrivalTime, DateTime? departureTime)
        {
            City = city;
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
        }

        public City City { get; }

        public DateTime ArrivalTime { get; }

        public DateTime? DepartureTime { get; }

        public override string Name => City.LocalName;

        public TimeSpan? Duration => DepartureTime - ArrivalTime;
    }

    public sealed class TravelStep : TravelDetail
    {
        private Route _route;

        public TravelStep(Technology technology, Route route)
        {
            Technology = technology;
            _route = route;
        }

        public Technology Technology { get; }

        public override string Name => _route.Name;

        public TimeSpan Duration => _route.Duration;

        public int Price => _route.Price;
    }

    public sealed class TravelNull : TravelDetail
    {
        public override string Name => "No results";
    }
}
