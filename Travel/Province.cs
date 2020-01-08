namespace Travel
{
/// <summary>
/// 在读取数据以及进行分析时，按照省来进行分类
/// </summary>
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
