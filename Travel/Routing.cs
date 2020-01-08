using System;
using System.Collections.Generic;
using System.Linq;

namespace Travel
{
    public sealed class Routing
    {
        private List<Node> _path = new List<Node>();
        private int _cost;

        private double _factor;

        /// <summary>
        /// 队列结果生成。
        /// </summary>
        /// <param name="cities"></param>
        /// <param name="strategy"></param>
        /// <param name="departureTime"></param>
        /// <param name="arrivalTime"></param>
        /// <returns></returns>
        public QueryResult Query(IList<int> cities, Strategy strategy, DateTime departureTime, DateTime arrivalTime = default)
        {
            if (strategy == Strategy.MinimizeCostLimitedTime)
            {
                return OptimizeCostLimitedTime(cities, departureTime, arrivalTime);
            }

            _path.Add(new Node { ArrivalTime = departureTime });

            for (int i = 0; i < cities.Count - 1; i++)
            {
                var start = cities[i];
                var end = cities[i + 1];
                var succeeded = strategy switch
                {
                    //分情况进行三种策略的选择。
                    Strategy.MinimizeTime => OptimizeTime(start, end, departureTime),
                    Strategy.MinimizeCost => OptimizeCost(start, end, departureTime),
                    Strategy.MinimizeScore => OptimizeScore(start, end, departureTime),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (!succeeded)
                {
                    return null;
                }

                departureTime = _path.Last().ArrivalTime;
            }

            return new QueryResult(_path, _cost);
        }
        /// <summary>
        /// 最短时间相关算法：
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="departureTime"></param>
        /// <returns></returns>
        private bool OptimizeTime(int start, int end, DateTime departureTime) =>
            Optimize(
                start,
                end,
                new[] { Technology.Flight, Technology.Train, Technology.Bus },
                departureTime,
                node => node.ArrivalTime,
                (minimalTime, route) => GetArrivalTime(minimalTime, route));
        /// <summary>
        /// 最小花费的算法
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="departureTime"></param>
        /// <returns></returns>
        private bool OptimizeCost(int start, int end, DateTime departureTime) =>
            Optimize(
                start,
                end,
                new[] { Technology.Bus, Technology.Train, Technology.Flight },
                departureTime,
                node => node.Cost,
                (minimalCost, route) => minimalCost + route.Price);
        /// <summary>
        /// 使用二分搜索实现限时最小费用
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="departureTime"></param>
        /// <returns></returns>
        private bool OptimizeScore(int start, int end, DateTime departureTime) =>
            Optimize(
                start,
                end,
                new[] { Technology.Flight, Technology.Train, Technology.Bus },
                departureTime,
                node =>
                {
                    var time = node.ArrivalTime;
                    var cost = node.Cost;
                    var score = (time - departureTime).TotalMinutes + cost * _factor;
                    return (score, time, cost);
                },
                (minimalTuple, route) =>
                {
                    var time = GetArrivalTime(minimalTuple.time, route);
                    var cost = minimalTuple.cost + route.Price;
                    var score = (time - departureTime).TotalMinutes + cost * _factor;
                    return (score, time, cost);
                });
        /// <summary>
        /// Dijkstra算法，此处是程序的核心算法，在不同的策略中只需改变度量即可完成变化。此函数生成一个解决方案。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="technologies"></param>
        /// <param name="departureTime"></param>
        /// <param name="valueSelector"></param>
        /// <param name="newValueSelector"></param>
        /// <returns></returns>
        private bool Optimize<T>(
            int start,
            int end,
            IEnumerable<Technology> technologies,
            DateTime departureTime,
            Func<Node, T> valueSelector,
            Func<T, Route, T> newValueSelector) where T : IComparable<T>
        {
            var nodes = new Node[Map.CityNumber];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new Node();
            }

            var startNode = nodes[start];
            startNode.Cost = 0;
            startNode.ArrivalTime = departureTime;

            var closed = new bool[Map.CityNumber];

            for (int i = 0; i < Map.CityNumber; i++)
            {
                var (minimalValue, optimalCity) = Enumerable.Range(0, Map.CityNumber)
                    .Where(city => !closed[city])
                    .Min(city => (valueSelector(nodes[city]), city));

                if (optimalCity == end)
                {
                    if (nodes[optimalCity].PrevCityIndex < 0)
                    {
                        return false;
                    }

                    AppendPath();
                    _cost += nodes[optimalCity].Cost;
                    return true;
                }

                closed[optimalCity] = true;

                foreach (var technology in technologies)
                {
                    foreach (var (route, destination) in
                        from route in
                            from route in Map.Cities[optimalCity].EnumerateRoutes(technology, technology == Technology.Bus ? nodes[optimalCity].ArrivalTime.TimeOfDay : default)
                            where !closed[route.To.Index]
                            let destination = nodes[route.To.Index]
                            where destination.PrevCityIndex != optimalCity ||
                                  destination.PrevTech == technology
                            select route
                        let destination = nodes[route.To.Index]
                        let newValue = newValueSelector(minimalValue, route)
                        where newValue.CompareTo(valueSelector(destination)) < 0
                        select (route, destination))
                    {
                        destination.PrevCityIndex = optimalCity;
                        destination.PrevRoute = route;
                        destination.PrevTech = technology;
                        destination.Cost = nodes[optimalCity].Cost + route.Price;
                        destination.ArrivalTime = GetArrivalTime(nodes[optimalCity].ArrivalTime, route);
                    }
                }
            }

            return false;

            void AppendPath() => _path.AddRange(GetReversePath().Reverse());

            IEnumerable<Node> GetReversePath()
            {
                for (var city = end; city != start;)
                {
                    var node = nodes[city];
                    yield return node;

                    var prevCity = node.PrevCityIndex;
                    var prevRoute = node.PrevRoute;
                    var prevNode = prevCity == start ? _path.Last() : nodes[prevCity];

                    prevNode.DepartureTime = node.ArrivalTime - prevRoute.Duration;

                    city = prevCity;
                }
            }
        }

        private DateTime GetArrivalTime(DateTime departureTime, Route route)
        {
            if (departureTime.TimeOfDay > route.DepartureTime)
            {
                departureTime = departureTime.AddDays(1);
            }

            return departureTime.Date + route.DepartureTime + route.Duration;
        }
        /// <summary>
        /// 此处是输出队列的结果部分。
        /// </summary>
        /// <param name="cities"></param>
        /// <param name="departureTime"></param>
        /// <param name="arrivalTime"></param>
        /// <returns></returns>
        private QueryResult OptimizeCostLimitedTime(IList<int> cities, DateTime departureTime, DateTime arrivalTime)
        {
            var minTimeResult = new Routing().Query(cities, Strategy.MinimizeTime, departureTime);
            var minCostResult = new Routing().Query(cities, Strategy.MinimizeCost, departureTime);

            var minTimePath = minTimeResult.Path;
            var minCostPath = minCostResult.Path;

            if (minTimePath == null)
            {
                return null;
            }

            if (minTimePath.Last().ArrivalTime > arrivalTime)
            {
                return null;
            }

            if (minCostPath.Last().ArrivalTime <= arrivalTime)
            {
                return new QueryResult(minCostPath, minCostResult.Cost);
            }

            var minTime = GetTime(minTimePath);
            var maxTime = GetTime(minCostPath);

            var minCost = minCostResult.Cost;
            var maxCost = minTimeResult.Cost;

            var lowerFactor = minTime / maxCost;
            var upperFactor = maxTime / minCost;

            var expectedTime = (arrivalTime - departureTime).TotalMinutes;
            var expectedCost = maxCost - (maxCost - minCost) * (expectedTime - minTime) / (maxTime - minTime);

            var expectedFactor = expectedTime / expectedCost;

            var optimalPath = minTimePath;
            var optimalCost = maxCost;

            var prevTime = minTime;
            var prevCost = maxCost;
            //此处使用二分搜索，即引入一个权重变量，计算出权重来进行更新，根据计算的时间与规定时间的比较来改变最小/最大边界。
            for (var currentFactor = expectedFactor; ; currentFactor = (lowerFactor + upperFactor) / 2)
            {
                var currentResult = new Routing { _factor = currentFactor }.Query(cities, Strategy.MinimizeScore, departureTime);
                var currentPath = currentResult.Path;
                var currentTime = GetTime(currentPath);
                var currentCost = currentResult.Cost;

                if (currentTime <= expectedTime)
                {
                    if (currentCost < optimalCost)
                    {
                        optimalPath = currentPath;
                        optimalCost = currentCost;
                        lowerFactor = expectedFactor;
                    }
                    else
                    {
                        return new QueryResult(optimalPath, optimalCost);
                    }
                }
                else if (prevTime == currentTime && prevCost == currentCost)
                {
                    return new QueryResult(optimalPath, optimalCost);
                }
                else
                {
                    upperFactor = expectedFactor;
                }

                prevTime = currentTime;
                prevCost = currentCost;
            }

            double GetTime(IEnumerable<Node> path) => (path.Last().ArrivalTime - departureTime).TotalMinutes;
        }
    }

    public sealed class QueryResult
    {
        public QueryResult(IEnumerable<Node> path, int cost)
        {
            Path = path;
            Cost = cost;
        }

        public IEnumerable<Node> Path { get; }

        public int Cost { get; }
    }
}
