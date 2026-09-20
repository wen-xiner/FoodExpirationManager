using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoodExpirationManager.Data;
using FoodExpirationManager.Models;

namespace FoodExpirationManager.Services
{
    /// <summary>
    /// 命令行接口处理器，支持外部通过命令行参数对食品进行增删改查。
    /// 用法示例：
    ///   FoodExpirationManager.exe --list
    ///   FoodExpirationManager.exe --search 牛奶
    ///   FoodExpirationManager.exe --add "牛奶,乳制品,2026-07-01,180,30"
    ///   FoodExpirationManager.exe --update "1,牛奶,乳制品,2026-07-01,180,30"
    ///   FoodExpirationManager.exe --delete 1
    ///   FoodExpirationManager.exe --expiring
    ///   FoodExpirationManager.exe --expired
    ///   FoodExpirationManager.exe --stats
    /// </summary>
    public static class CliHandler
    {
        /// <summary>
        /// 处理命令行参数。
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        /// <param name="foodService">通过 Autofac 容器解析的 FoodService</param>
        /// <param name="expirationService">通过 Autofac 容器解析的 ExpirationService</param>
        /// <returns>true 表示已处理（CLI 模式），false 表示应启动 GUI</returns>
        public static bool Handle(string[] args, FoodService foodService = null, ExpirationService expirationService = null)
        {
            if (args.Length == 0) return false;

            // 回退模式（无容器时手动构建，兼容性保留）
            if (foodService == null || expirationService == null)
            {
                var dbHelper = new Data.DatabaseHelper();
                dbHelper.InitializeDatabase();
                var foodRepo = new Data.FoodRepository(dbHelper);
                var inventoryRepo = new Data.InventoryRepository(dbHelper);
                expirationService = new ExpirationService(foodRepo, inventoryRepo);
                foodService = new FoodService(foodRepo, inventoryRepo, expirationService);
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "--list":
                        return CmdList(foodService);
                    case "--search":
                        return CmdSearch(foodService, args.Length > 1 ? args[1] : "");
                    case "--add":
                        return CmdAdd(foodService, args.Length > 1 ? args[1] : "");
                    case "--update":
                        return CmdUpdate(foodService, args.Length > 1 ? args[1] : "");
                    case "--delete":
                        return CmdDelete(foodService, args.Length > 1 ? args[1] : "");
                    case "--expiring":
                        return CmdExpiring(expirationService);
                    case "--expired":
                        return CmdExpired(expirationService);
                    case "--stats":
                        return CmdStats(expirationService);
                    case "--help":
                    case "-h":
                    case "/?":
                        PrintHelp();
                        return true;
                    default:
                        return false; // 未知参数，启动 GUI
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                return true;
            }
        }

        private static bool CmdList(FoodService foodService)
        {
            var foods = foodService.GetAllFoods();
            PrintFoodTable(foods);
            return true;
        }

        private static bool CmdSearch(FoodService foodService, string keyword)
        {
            var foods = foodService.SearchFoods(keyword, "");
            Console.WriteLine($"搜索: [{keyword}] — 找到 {foods.Count} 条\n");
            PrintFoodTable(foods);
            return true;
        }

        private static bool CmdAdd(FoodService foodService, string data)
        {
            // 格式：名称,分类,生产日期,保质期,临期阈值
            var parts = data.Split(',');
            if (parts.Length < 4)
            {
                Console.WriteLine("格式错误。正确格式：名称,分类,生产日期(yyyy-MM-dd),保质期(天)[,临期阈值(天)]");
                return true;
            }

            var food = new FoodItem
            {
                Name = parts[0].Trim(),
                Category = parts[1].Trim(),
                ProductionDate = DateTime.Parse(parts[2].Trim()),
                ShelfLife = int.Parse(parts[3].Trim()),
                ReminderThresholdDays = parts.Length >= 5 ? int.Parse(parts[4].Trim()) : 30
            };

            int id = foodService.AddFood(food);
            Console.WriteLine($"✅ 食品添加成功，编号: {id}");
            return true;
        }

        private static bool CmdUpdate(FoodService foodService, string data)
        {
            // 格式：编号,名称,分类,生产日期,保质期,临期阈值
            var parts = data.Split(',');
            if (parts.Length < 5)
            {
                Console.WriteLine("格式错误。正确格式：编号,名称,分类,生产日期(yyyy-MM-dd),保质期(天)[,临期阈值(天)]");
                return true;
            }

            var food = new FoodItem
            {
                Id = int.Parse(parts[0].Trim()),
                Name = parts[1].Trim(),
                Category = parts[2].Trim(),
                ProductionDate = DateTime.Parse(parts[3].Trim()),
                ShelfLife = int.Parse(parts[4].Trim()),
                ReminderThresholdDays = parts.Length >= 6 ? int.Parse(parts[5].Trim()) : 30
            };

            foodService.UpdateFood(food);
            Console.WriteLine($"✅ 食品编号 {food.Id} 更新成功");
            return true;
        }

        private static bool CmdDelete(FoodService foodService, string idStr)
        {
            if (!int.TryParse(idStr, out int id))
            {
                Console.WriteLine("请输入有效的食品编号");
                return true;
            }

            foodService.DeleteFood(id);
            Console.WriteLine($"✅ 食品编号 {id} 已删除");
            return true;
        }

        private static bool CmdExpiring(ExpirationService expirationService)
        {
            var foods = expirationService.GetExpiringFoods();
            Console.WriteLine($"⚠️ 临期食品: {foods.Count} 件 (阈值 ≤30 天)\n");
            PrintFoodTable(foods);
            return true;
        }

        private static bool CmdExpired(ExpirationService expirationService)
        {
            var foods = expirationService.GetExpiredFoods();
            Console.WriteLine($"❌ 已过期食品: {foods.Count} 件\n");
            PrintFoodTable(foods);
            return true;
        }

        private static bool CmdStats(ExpirationService expirationService)
        {
            var summary = expirationService.GetSummary();
            Console.WriteLine("📊 食品统计:");
            Console.WriteLine($"   总数: {summary.TotalCount}");
            Console.WriteLine($"   ✅ 正常: {summary.NormalCount}");
            Console.WriteLine($"   ⚠️ 临期: {summary.ExpiringCount}");
            Console.WriteLine($"   ❌ 过期: {summary.ExpiredCount}");
            return true;
        }

        private static void PrintFoodTable(List<FoodItem> foods)
        {
            if (foods.Count == 0)
            {
                Console.WriteLine("（无数据）");
                return;
            }

            Console.WriteLine("┌────┬──────────────────┬──────────┬────────────┬────────────┬──────────┬──────────┐");
            Console.WriteLine("│ ID │ 名称             │ 分类     │ 生产日期   │ 过期日期   │ 剩余天数 │ 状态     │");
            Console.WriteLine("├────┼──────────────────┼──────────┼────────────┼────────────┼──────────┼──────────┤");

            foreach (var f in foods)
            {
                string name = PadRight(f.Name, 16);
                string cat = PadRight(f.Category, 8);
                Console.WriteLine($"│ {f.Id,2} │ {name} │ {cat} │ {f.ProductionDate:yyyy-MM-dd} │ {f.ExpirationDate:yyyy-MM-dd} │ {f.RemainingDays,8} │ {PadRight(f.StatusText, 8)} │");
            }

            Console.WriteLine("└────┴──────────────────┴──────────┴────────────┴────────────┴──────────┴──────────┘");
        }

        private static string PadRight(string text, int length)
        {
            // 中文字符占2个宽度
            int width = 0;
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                width += c > 127 ? 2 : 1;
                sb.Append(c);
                if (width >= length) break;
            }
            while (width < length) { sb.Append(' '); width++; }
            return sb.ToString();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("食品临期管理系统 - 命令行接口");
            Console.WriteLine("");
            Console.WriteLine("用法: FoodExpirationManager.exe <命令> [参数]");
            Console.WriteLine("");
            Console.WriteLine("命令:");
            Console.WriteLine("  --list                    列出所有食品");
            Console.WriteLine("  --search <关键词>          按名称搜索食品");
            Console.WriteLine("  --add <数据>               新增食品（格式：名称,分类,生产日期,保质期,临期阈值）");
            Console.WriteLine("  --update <数据>            修改食品（格式：编号,名称,分类,生产日期,保质期,临期阈值）");
            Console.WriteLine("  --delete <编号>            删除食品");
            Console.WriteLine("  --expiring                 查看临期食品列表");
            Console.WriteLine("  --expired                  查看已过期食品列表");
            Console.WriteLine("  --stats                    查看统计信息");
            Console.WriteLine("  --help                     显示此帮助");
            Console.WriteLine("");
            Console.WriteLine("示例:");
            Console.WriteLine("  FoodExpirationManager.exe --list");
            Console.WriteLine("  FoodExpirationManager.exe --search 牛奶");
            Console.WriteLine("  FoodExpirationManager.exe --add \"牛奶,乳制品,2026-07-01,180,30\"");
            Console.WriteLine("  FoodExpirationManager.exe --delete 1");
            Console.WriteLine("");
            Console.WriteLine("不带参数启动则进入图形界面。");
        }
    }
}
