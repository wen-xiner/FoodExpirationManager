using System;
using System.Collections.Generic;
using FoodExpirationManager.Data;
using FoodExpirationManager.Models;

namespace FoodExpirationManager.Services
{
    /// <summary>
    /// 食品业务逻辑层
    /// </summary>
    public class FoodService
    {
        private readonly FoodRepository _foodRepo;
        private readonly InventoryRepository _inventoryRepo;
        private readonly ExpirationService _expirationService;

        public FoodService(FoodRepository foodRepo, InventoryRepository inventoryRepo, ExpirationService expirationService)
        {
            _foodRepo = foodRepo;
            _inventoryRepo = inventoryRepo;
            _expirationService = expirationService;
        }

        /// <summary>
        /// 添加食品，自动计算到期日期 = 生产日期 + 保质期
        /// </summary>
        public int AddFood(FoodItem food)
        {
            ValidateFood(food);

            food.ExpirationDate = food.ProductionDate.AddDays(food.ShelfLife);

            return _foodRepo.Add(food);
        }

        /// <summary>
        /// 更新食品，自动重新计算到期日期
        /// </summary>
        public bool UpdateFood(FoodItem food)
        {
            ValidateFood(food);

            food.ExpirationDate = food.ProductionDate.AddDays(food.ShelfLife);

            return _foodRepo.Update(food);
        }

        /// <summary>
        /// 删除食品
        /// </summary>
        public bool DeleteFood(int id)
        {
            return _foodRepo.Delete(id);
        }

        /// <summary>
        /// 根据ID获取食品（含状态和库存信息）
        /// </summary>
        public FoodItem GetFoodById(int id)
        {
            var food = _foodRepo.GetById(id);
            if (food != null)
            {
                food.Status = _expirationService.CalculateStatus(food);
                food.StockQuantity = _inventoryRepo.GetCurrentStock(food.Id);
            }
            return food;
        }

        /// <summary>
        /// 获取所有食品（含状态和库存信息）
        /// </summary>
        public List<FoodItem> GetAllFoods()
        {
            var foods = _foodRepo.GetAll();
            var stocks = _inventoryRepo.GetAllCurrentStocks();

            foreach (var food in foods)
            {
                food.Status = _expirationService.CalculateStatus(food);
                food.StockQuantity = stocks.ContainsKey(food.Id) ? stocks[food.Id] : 0;
            }

            return foods;
        }

        /// <summary>
        /// 搜索食品
        /// </summary>
        public List<FoodItem> SearchFoods(string name, string category, FoodStatus? status = null)
        {
            var foods = _foodRepo.Search(name, category);
            var stocks = _inventoryRepo.GetAllCurrentStocks();

            foreach (var food in foods)
            {
                food.Status = _expirationService.CalculateStatus(food);
                food.StockQuantity = stocks.ContainsKey(food.Id) ? stocks[food.Id] : 0;
            }

            // 按状态筛选
            if (status.HasValue)
            {
                foods = foods.FindAll(f => f.Status == status.Value);
            }

            return foods;
        }

        /// <summary>
        /// 获取所有食品类别
        /// </summary>
        public List<string> GetAllCategories()
        {
            return _foodRepo.GetAllCategories();
        }

        /// <summary>
        /// 批量导入食品
        /// </summary>
        public int BatchImport(List<FoodItem> foods)
        {
            int successCount = 0;
            foreach (var food in foods)
            {
                try
                {
                    ValidateFood(food);
                    food.ExpirationDate = food.ProductionDate.AddDays(food.ShelfLife);
                    _foodRepo.Add(food);
                    successCount++;
                }
                catch
                {
                    // 跳过无效记录，继续导入下一条
                }
            }
            return successCount;
        }

        /// <summary>
        /// 校验食品数据
        /// </summary>
        private void ValidateFood(FoodItem food)
        {
            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("食品名称不能为空");

            if (string.IsNullOrWhiteSpace(food.Category))
                throw new ArgumentException("食品类别不能为空");

            if (food.ShelfLife <= 0)
                throw new ArgumentException("保质期必须大于0天");
        }
    }
}
