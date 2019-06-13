namespace Travel
{
    public sealed class WayPoint
    {
        public WayPoint(City city = null) => City = city;

        public City City { get; }

        public string Name => City?.FullLocalName;
    }
}
