using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FoodExpirationManager.Models;

namespace FoodExpirationManager.Data
{
    /// <summary>
    /// 食品数据访问层 - 负责 FoodItems 表的增删改查
    /// </summary>
    public class FoodRepository
    {
        private readonly DatabaseHelper _dbHelper;

        public FoodRepository(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// 添加食品
        /// </summary>
        public int Add(FoodItem food)
        {
            const string sql = @"
                INSERT INTO FoodItems (Name, Category, ProductionDate, ExpirationDate, ShelfLife, ReminderThresholdDays, CreatedAt)
                VALUES (@Name, @Category, @ProductionDate, @ExpirationDate, @ShelfLife, @ReminderThresholdDays, @CreatedAt);
                SELECT last_insert_rowid();";

            using (var conn = _dbHelper.CreateConnection())
            {
                food.CreatedAt = DateTime.Now;
                return conn.QuerySingle<int>(sql, new
                {
                    food.Name,
                    food.Category,
                    ProductionDate = food.ProductionDate.ToString("yyyy-MM-dd"),
                    ExpirationDate = food.ExpirationDate.ToString("yyyy-MM-dd"),
                    food.ShelfLife,
                    food.ReminderThresholdDays,
                    CreatedAt = food.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        /// <summary>
        /// 更新食品信息
        /// </summary>
        public bool Update(FoodItem food)
        {
            const string sql = @"
                UPDATE FoodItems
                SET Name = @Name,
                    Category = @Category,
                    ProductionDate = @ProductionDate,
                    ExpirationDate = @ExpirationDate,
                    ShelfLife = @ShelfLife,
                    ReminderThresholdDays = @ReminderThresholdDays
                WHERE Id = @Id;";

            using (var conn = _dbHelper.CreateConnection())
            {
                int rows = conn.Execute(sql, new
                {
                    food.Id,
                    food.Name,
                    food.Category,
                    ProductionDate = food.ProductionDate.ToString("yyyy-MM-dd"),
                    ExpirationDate = food.ExpirationDate.ToString("yyyy-MM-dd"),
                    food.ShelfLife,
                    food.ReminderThresholdDays
                });
                return rows > 0;
            }
        }

        /// <summary>
        /// 删除食品（同时级联删除其库存记录，避免孤儿数据）
        /// </summary>
        public bool Delete(int id)
        {
            using (var conn = _dbHelper.CreateConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    // 先删库存记录
                    conn.Execute("DELETE FROM InventoryRecords WHERE FoodItemId = @Id;", new { Id = id }, tx);
                    // 再删食品
                    int rows = conn.Execute("DELETE FROM FoodItems WHERE Id = @Id;", new { Id = id }, tx);
                    tx.Commit();
                    return rows > 0;
                }
            }
        }

        /// <summary>
        /// 根据ID获取食品
        /// </summary>
        public FoodItem GetById(int id)
        {
            const string sql = "SELECT * FROM FoodItems WHERE Id = @Id;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.QueryFirstOrDefault<FoodItem>(sql, new { Id = id });
            }
        }

        /// <summary>
        /// 获取所有食品
        /// </summary>
        public List<FoodItem> GetAll()
        {
            const string sql = "SELECT * FROM FoodItems ORDER BY CreatedAt DESC;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.Query<FoodItem>(sql).ToList();
            }
        }

        /// <summary>
        /// 根据名称和类别综合搜索
        /// </summary>
        public List<FoodItem> Search(string name, string category)
        {
            string sql = "SELECT * FROM FoodItems WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(name))
            {
                sql += " AND Name LIKE @Name";
                parameters.Add("Name", $"%{name}%");
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += " AND Category LIKE @Category";
                parameters.Add("Category", $"%{category}%");
            }

            sql += " ORDER BY CreatedAt DESC;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.Query<FoodItem>(sql, parameters).ToList();
            }
        }

        /// <summary>
        /// 获取所有唯一的食品类别
        /// </summary>
        public List<string> GetAllCategories()
        {
            const string sql = "SELECT DISTINCT Category FROM FoodItems ORDER BY Category;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.Query<string>(sql).ToList();
            }
        }

        /// <summary>
        /// 获取食品总数
        /// </summary>
        public int GetTotalCount()
        {
            const string sql = "SELECT COUNT(*) FROM FoodItems;";

            using (var conn = _dbHelper.CreateConnection())
            {
                return conn.ExecuteScalar<int>(sql);
            }
        }
    }
}
