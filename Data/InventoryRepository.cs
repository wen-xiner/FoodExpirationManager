using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FoodExpirationManager.Models;

namespace FoodExpirationManager.Data
{
    /// <summary>
    /// 库存数据访问层 - 负责 InventoryRecords 表的增删改查
    /// </summary>
    public class InventoryRepository
    {
        private readonly DatabaseHelper _dbHelper;

        public InventoryRepository(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// 添加库存记录
        /// </summary>
        public int Add(InventoryRecord record)
        {
            const string sql = @"
                INSERT INTO InventoryRecords (FoodItemId, Quantity, OperationType, RecordDate, Remark)
                VALUES (@FoodItemId, @Quantity, @OperationType, @RecordDate, @Remark);
                SELECT last_insert_rowid();";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.QuerySingle<int>(sql, new
                {
                    record.FoodItemId,
                    record.Quantity,
                    record.OperationType,
                    RecordDate = record.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    record.Remark
                });
            }
        }

        /// <summary>
        /// 获取指定食品的所有库存记录
        /// </summary>
        public List<InventoryRecord> GetByFoodId(int foodItemId)
        {
            const string sql = @"
                SELECT ir.*, fi.Name AS FoodName, fi.Category AS FoodCategory
                FROM InventoryRecords ir
                JOIN FoodItems fi ON ir.FoodItemId = fi.Id
                WHERE ir.FoodItemId = @FoodItemId
                ORDER BY ir.RecordDate DESC;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.Query<InventoryRecord>(sql, new { FoodItemId = foodItemId }).ToList();
            }
        }

        /// <summary>
        /// 获取所有库存记录
        /// </summary>
        public List<InventoryRecord> GetAll()
        {
            const string sql = @"
                SELECT ir.*, fi.Name AS FoodName, fi.Category AS FoodCategory
                FROM InventoryRecords ir
                JOIN FoodItems fi ON ir.FoodItemId = fi.Id
                ORDER BY ir.RecordDate DESC;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.Query<InventoryRecord>(sql).ToList();
            }
        }

        /// <summary>
        /// 获取指定食品的当前库存数量（入库总量 - 出库总量）
        /// </summary>
        public int GetCurrentStock(int foodItemId)
        {
            const string sql = @"
                SELECT
                    COALESCE(SUM(CASE WHEN OperationType = 'StockIn' THEN Quantity ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN OperationType = 'StockOut' THEN Quantity ELSE 0 END), 0)
                FROM InventoryRecords
                WHERE FoodItemId = @FoodItemId;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.ExecuteScalar<int>(sql, new { FoodItemId = foodItemId });
            }
        }

        /// <summary>
        /// 获取所有食品的当前库存（食品ID -> 库存数量）
        /// </summary>
        public Dictionary<int, int> GetAllCurrentStocks()
        {
            const string sql = @"
                SELECT
                    FoodItemId,
                    COALESCE(SUM(CASE WHEN OperationType = 'StockIn' THEN Quantity ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN OperationType = 'StockOut' THEN Quantity ELSE 0 END), 0) AS Stock
                FROM InventoryRecords
                GROUP BY FoodItemId;";

            using (var conn = _dbHelper.CreateConnection())
            {
                var results = conn.Query(sql);
                var dict = new Dictionary<int, int>();
                foreach (var row in results)
                {
                    dict[(int)row.FoodItemId] = (int)row.Stock;
                }
                return dict;
            }
        }

    }
}
