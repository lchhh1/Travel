using System;

namespace Travel
{
    public sealed class TravelSummary
    {
        public TravelSummary(DateTime departureTime, DateTime arrivalTime, int cost)
        {
            DepartureTime = departureTime;
            ArrivalTime = arrivalTime;
            Cost = cost;
        }

        public DateTime DepartureTime { get; }

        public DateTime ArrivalTime { get; }

        public TimeSpan Duration => ArrivalTime - DepartureTime;

        public int Cost { get; }
    }
}
