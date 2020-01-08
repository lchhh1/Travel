namespace Travel
{
    /// <summary>
    /// 路线选择策略。
    /// </summary>
    public enum Strategy
    {
        /// <summary>
        /// 最小时间
        /// </summary>
        MinimizeTime,
        /// <summary>
        /// 最小费用
        /// </summary>
        MinimizeCost,
        /// <summary>
        /// 限时最小费用
        /// </summary>
        MinimizeCostLimitedTime,

        MinimizeScore
    }
}
