using System;
using System.Collections.Generic;
using System.Linq;
using FoodExpirationManager.Data;
using FoodExpirationManager.Models;

namespace FoodExpirationManager.Services
{
    /// <summary>
    /// 临期计算和提醒逻辑服务。
    /// 规则：剩余天数 > 30 → 正常，≤ 30 → 临期，< 0 → 已过期。
    /// 每件食品可使用独立的 ReminderThresholdDays 阈值（默认 30 天）。
    /// </summary>
    public class ExpirationService
    {
        private readonly FoodRepository _foodRepo;
        private readonly InventoryRepository _inventoryRepo;

        /// <summary>
        /// 全局默认临期阈值（天数），当食品未单独设置时使用
        /// </summary>
        public int DefaultThresholdDays { get; set; } = 30;

        public ExpirationService(FoodRepository foodRepo, InventoryRepository inventoryRepo)
        {
            _foodRepo = foodRepo;
            _inventoryRepo = inventoryRepo;
        }

        /// <summary>
        /// 计算单个食品的状态，优先使用食品自身的 ReminderThresholdDays
        /// </summary>
        public FoodStatus CalculateStatus(FoodItem food)
        {
            int remainingDays = (food.ExpirationDate.Date - DateTime.Today).Days;

            if (remainingDays < 0)
                return FoodStatus.Expired;

            int threshold = food.ReminderThresholdDays > 0 ? food.ReminderThresholdDays : DefaultThresholdDays;

            if (remainingDays <= threshold)
                return FoodStatus.Expiring;

            return FoodStatus.Normal;
        }

        /// <summary>
        /// 计算食品的剩余天数
        /// </summary>
        public int CalculateRemainingDays(FoodItem food)
        {
            return (food.ExpirationDate.Date - DateTime.Today).Days;
        }

        /// <summary>
        /// 获取所有临期食品（即将过期但尚未过期）
        /// </summary>
        public List<FoodItem> GetExpiringFoods()
        {
            var allFoods = _foodRepo.GetAll();
            var stocks = _inventoryRepo.GetAllCurrentStocks();

            var expiringFoods = new List<FoodItem>();
            foreach (var food in allFoods)
            {
                food.Status = CalculateStatus(food);
                food.StockQuantity = stocks.ContainsKey(food.Id) ? stocks[food.Id] : 0;

                if (food.Status == FoodStatus.Expiring)
                {
                    expiringFoods.Add(food);
                }
            }

            return expiringFoods.OrderBy(f => f.RemainingDays).ToList();
        }

        /// <summary>
        /// 获取所有已过期食品
        /// </summary>
        public List<FoodItem> GetExpiredFoods()
        {
            var allFoods = _foodRepo.GetAll();
            var stocks = _inventoryRepo.GetAllCurrentStocks();

            var expiredFoods = new List<FoodItem>();
            foreach (var food in allFoods)
            {
                food.Status = CalculateStatus(food);
                food.StockQuantity = stocks.ContainsKey(food.Id) ? stocks[food.Id] : 0;

                if (food.Status == FoodStatus.Expired)
                {
                    expiredFoods.Add(food);
                }
            }

            return expiredFoods.OrderBy(f => f.RemainingDays).ToList();
        }

        /// <summary>
        /// 获取临期和过期食品的统计概览
        /// </summary>
        public ExpirationSummary GetSummary()
        {
            var allFoods = _foodRepo.GetAll();
            var stocks = _inventoryRepo.GetAllCurrentStocks();

            var summary = new ExpirationSummary();

            foreach (var food in allFoods)
            {
                food.Status = CalculateStatus(food);
                food.StockQuantity = stocks.ContainsKey(food.Id) ? stocks[food.Id] : 0;

                switch (food.Status)
                {
                    case FoodStatus.Normal:
                        summary.NormalCount++;
                        break;
                    case FoodStatus.Expiring:
                        summary.ExpiringCount++;
                        break;
                    case FoodStatus.Expired:
                        summary.ExpiredCount++;
                        break;
                }
            }

            summary.TotalCount = allFoods.Count;
            return summary;
        }

        /// <summary>
        /// 获取所有食品（含状态），按剩余天数升序排列
        /// </summary>
        public List<FoodItem> GetAllFoodsWithStatus()
        {
            var allFoods = _foodRepo.GetAll();
            var stocks = _inventoryRepo.GetAllCurrentStocks();

            foreach (var food in allFoods)
            {
                food.Status = CalculateStatus(food);
                food.StockQuantity = stocks.ContainsKey(food.Id) ? stocks[food.Id] : 0;
            }

            return allFoods.OrderBy(f => f.RemainingDays).ToList();
        }

        /// <summary>
        /// 获取启动时需要提醒的食品列表（临期 + 已过期，且有库存）
        /// </summary>
        public List<FoodItem> GetReminderFoods()
        {
            return GetAllFoodsWithStatus()
                .Where(f => (f.Status == FoodStatus.Expiring || f.Status == FoodStatus.Expired)
                            && f.StockQuantity > 0)
                .ToList();
        }
    }

    /// <summary>
    /// 食品过期状态统计摘要
    /// </summary>
    public class ExpirationSummary
    {
        /// <summary>食品总数</summary>
        public int TotalCount { get; set; }

        /// <summary>正常食品数</summary>
        public int NormalCount { get; set; }

        /// <summary>临期食品数</summary>
        public int ExpiringCount { get; set; }

        /// <summary>已过期食品数</summary>
        public int ExpiredCount { get; set; }
    }
}
