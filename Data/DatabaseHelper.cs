using System;
using System.Configuration;
using System.Data;
using Microsoft.Data.Sqlite;

namespace FoodExpirationManager.Data
{
    /// <summary>
    /// SQLite 数据库连接帮助类，负责数据库连接和表初始化
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            // 从 App.config 读取连接字符串
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

            if (string.IsNullOrEmpty(_connectionString))
            {
                // 回退默认连接字符串（数据库文件在程序目录下）
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FoodExpiration.db");
                _connectionString = $"Data Source={dbPath}";
            }
        }

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        /// <summary>
        /// 创建并返回一个新的数据库连接
        /// </summary>
        public IDbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// 初始化数据库表（如果不存在则创建）
        /// </summary>
        public void InitializeDatabase()
        {
            using (var connection = CreateConnection())
            {
                connection.Open();

                // 创建食品信息表
                string createFoodItemsSql = @"
                    CREATE TABLE IF NOT EXISTS FoodItems (
                        Id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name                    TEXT    NOT NULL,
                        Category                TEXT    NOT NULL,
                        ProductionDate          TEXT    NOT NULL,
                        ExpirationDate          TEXT    NOT NULL,
                        ShelfLife               INTEGER NOT NULL DEFAULT 0,
                        ReminderThresholdDays   INTEGER NOT NULL DEFAULT 30,
                        CreatedAt               TEXT    NOT NULL
                    );";

                // 创建库存记录表
                string createInventoryRecordsSql = @"
                    CREATE TABLE IF NOT EXISTS InventoryRecords (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        FoodItemId      INTEGER NOT NULL,
                        Quantity        INTEGER NOT NULL,
                        OperationType   TEXT    NOT NULL,
                        RecordDate      TEXT    NOT NULL,
                        Remark          TEXT,
                        FOREIGN KEY (FoodItemId) REFERENCES FoodItems(Id)
                    );";

                using (var command = new SqliteCommand(createFoodItemsSql, (SqliteConnection)connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(createInventoryRecordsSql, (SqliteConnection)connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
