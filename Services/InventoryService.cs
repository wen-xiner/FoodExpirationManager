using System;
using System.Collections.Generic;
using FoodExpirationManager.Data;
using FoodExpirationManager.Models;

namespace FoodExpirationManager.Services
{
    /// <summary>
    /// 库存业务逻辑层
    /// </summary>
    public class InventoryService
    {
        private readonly InventoryRepository _inventoryRepo;
        private readonly FoodRepository _foodRepo;

        public InventoryService(InventoryRepository inventoryRepo, FoodRepository foodRepo)
        {
            _inventoryRepo = inventoryRepo;
            _foodRepo = foodRepo;
        }

        /// <summary>
        /// 入库操作
        /// </summary>
        public int StockIn(int foodItemId, int quantity, string remark = "")
        {
            if (foodItemId <= 0)
                throw new ArgumentException("请选择有效的食品");

            if (quantity <= 0)
                throw new ArgumentException("入库数量必须大于0");

            var food = _foodRepo.GetById(foodItemId);
            if (food == null)
                throw new ArgumentException("指定的食品不存在");

            var record = new InventoryRecord
            {
                FoodItemId = foodItemId,
                Quantity = quantity,
                OperationType = InventoryOperationType.StockIn.ToString(),
                RecordDate = DateTime.Now,
                Remark = remark
            };

            return _inventoryRepo.Add(record);
        }

        /// <summary>
        /// 出库操作
        /// </summary>
        public int StockOut(int foodItemId, int quantity, string remark = "")
        {
            if (foodItemId <= 0)
                throw new ArgumentException("请选择有效的食品");

            if (quantity <= 0)
                throw new ArgumentException("出库数量必须大于0");

            // 检查库存是否足够
            int currentStock = _inventoryRepo.GetCurrentStock(foodItemId);
            if (currentStock < quantity)
                throw new InvalidOperationException($"库存不足！当前库存: {currentStock}，尝试出库: {quantity}");

            var food = _foodRepo.GetById(foodItemId);
            if (food == null)
                throw new ArgumentException("指定的食品不存在");

            var record = new InventoryRecord
            {
                FoodItemId = foodItemId,
                Quantity = quantity,
                OperationType = InventoryOperationType.StockOut.ToString(),
                RecordDate = DateTime.Now,
                Remark = remark
            };

            return _inventoryRepo.Add(record);
        }

        /// <summary>
        /// 获取指定食品的库存记录
        /// </summary>
        public List<InventoryRecord> GetRecordsByFoodId(int foodItemId)
        {
            return _inventoryRepo.GetByFoodId(foodItemId);
        }

        /// <summary>
        /// 获取所有库存记录
        /// </summary>
        public List<InventoryRecord> GetAllRecords()
        {
            return _inventoryRepo.GetAll();
        }

        /// <summary>
        /// 获取指定食品的当前库存
        /// </summary>
        public int GetCurrentStock(int foodItemId)
        {
            return _inventoryRepo.GetCurrentStock(foodItemId);
        }
    }
}
