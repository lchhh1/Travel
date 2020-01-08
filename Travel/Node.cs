using System;

namespace Travel
{
/// <summary>
/// 节点定义，包含前一个城市、前一段路线以及开始和到达时间等信息
/// </summary>
    public class Node
    {
        public City PrevCity => PrevCityIndex >= 0 ? Map.Cities[PrevCityIndex] : null;

        public int PrevCityIndex { get; set; } = -1;

        public Route PrevRoute { get; set; }

        public Technology PrevTech { get; set; }

        public int Cost { get; set; } = int.MaxValue;

        public DateTime ArrivalTime { get; set; } = DateTime.MaxValue;

        public DateTime DepartureTime { get; set; }
    }
}
