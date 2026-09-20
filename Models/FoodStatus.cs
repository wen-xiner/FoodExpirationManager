// 无需额外 using

namespace FoodExpirationManager.Models
{
    /// <summary>
    /// 食品状态枚举
    /// </summary>
    public enum FoodStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal,

        /// <summary>
        /// 临期（即将过期）
        /// </summary>
        Expiring,

        /// <summary>
        /// 已过期
        /// </summary>
        Expired
    }
}
