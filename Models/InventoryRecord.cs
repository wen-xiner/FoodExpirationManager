using System;

namespace FoodExpirationManager.Models
{
    /// <summary>
    /// 库存操作类型枚举
    /// </summary>
    public enum InventoryOperationType
    {
        /// <summary>
        /// 入库
        /// </summary>
        StockIn,

        /// <summary>
        /// 出库
        /// </summary>
        StockOut
    }

    /// <summary>
    /// 库存记录实体
    /// </summary>
    public class InventoryRecord
    {
        /// <summary>
        /// 记录ID（主键，自增）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 关联的食品ID
        /// </summary>
        public int FoodItemId { get; set; }

        /// <summary>
        /// 操作数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 操作类型（入库/出库）
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 操作日期
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        // ===== 以下为展示用字段（不存入数据库） =====

        /// <summary>
        /// 食品名称（JOIN 查询时填充）
        /// </summary>
        public string FoodName { get; set; }

        /// <summary>
        /// 食品类别（JOIN 查询时填充）
        /// </summary>
        public string FoodCategory { get; set; }

        /// <summary>
        /// 操作类型显示文本
        /// </summary>
        public string OperationTypeText
        {
            get
            {
                return OperationType == "StockIn" ? "入库" : "出库";
            }
        }
    }
}
