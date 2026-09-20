using System;

namespace FoodExpirationManager.Models
{
    /// <summary>
    /// 食品信息实体
    /// </summary>
    public class FoodItem
    {
        /// <summary>
        /// 食品编号（主键，自动编号）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 食品名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 食品分类（如：乳制品、蔬菜、肉类等）
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 生产日期
        /// </summary>
        public DateTime ProductionDate { get; set; }

        /// <summary>
        /// 保质期（天数），用户输入
        /// </summary>
        public int ShelfLife { get; set; }

        /// <summary>
        /// 到期日期（自动计算 = 生产日期 + 保质期）
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 临期提醒阈值（天数），每件食品可独立设置。
        /// 当剩余天数 ≤ 此值时视为"临期"，弹出提醒。
        /// 默认 30 天。
        /// </summary>
        public int ReminderThresholdDays { get; set; } = 30;

        /// <summary>
        /// 记录创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // ===== 以下为计算属性（不存入数据库） =====

        /// <summary>
        /// 当前状态（通过 ExpirationService 计算）
        /// </summary>
        public FoodStatus Status { get; set; }

        /// <summary>
        /// 距离过期的剩余天数（负数表示已过期天数）
        /// </summary>
        public int RemainingDays
        {
            get
            {
                return (ExpirationDate.Date - DateTime.Today).Days;
            }
        }

        /// <summary>
        /// 当前库存数量（通过 InventoryRepository 查询）
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// 状态描述文本
        /// </summary>
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case FoodStatus.Normal:
                        return "正常";
                    case FoodStatus.Expiring:
                        return $"临期（剩余 {RemainingDays} 天）";
                    case FoodStatus.Expired:
                        return $"已过期 {Math.Abs(RemainingDays)} 天";
                    default:
                        return "未知";
                }
            }
        }
    }
}
