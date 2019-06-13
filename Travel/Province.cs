namespace Travel
{
    public sealed class Province
    {
        public Province(string shortName, string longName, bool special)
        {
            ShortName = shortName;
            LongName = longName;
            Special = special;
            LocalName = Localization.GetString(ShortName);
        }

        public string ShortName { get; }

        public string LongName { get; }

        public string LocalName { get; }

        public bool Special { get; }

        public City City { get; set; }
    }
}
