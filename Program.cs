using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Autofac;
using FoodExpirationManager.Data;
using FoodExpirationManager.Forms;
using FoodExpirationManager.Services;

namespace FoodExpirationManager
{
    internal static class Program
    {
        /// <summary>
        /// Autofac DI 容器
        /// </summary>
        public static IContainer Container { get; private set; }

        // 控制台附加/释放 API，用于 CLI 模式
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// 应用程序的主入口点。
        /// 无参数 → 启动 GUI；有参数 → CLI 模式输出到控制台。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // 统一配置 Autofac 容器
            ConfigureContainer();

            // 初始化数据库
            using (var scope = Container.BeginLifetimeScope())
            {
                var dbHelper = scope.Resolve<DatabaseHelper>();
                dbHelper.InitializeDatabase();
            }

            // CLI 模式：附加到父进程控制台，使用容器解析的服务
            if (args.Length > 0)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                try
                {
                    using (var scope = Container.BeginLifetimeScope())
                    {
                        var foodService = scope.Resolve<FoodService>();
                        var expirationService = scope.Resolve<ExpirationService>();
                        if (CliHandler.Handle(args, foodService, expirationService))
                        {
                            FreeConsole();
                            return;
                        }
                    }
                }
                finally
                {
                    FreeConsole();
                }
            }

            // GUI 模式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 启动主窗体
            using (var scope = Container.BeginLifetimeScope())
            {
                var mainForm = scope.Resolve<MainForm>();
                Application.Run(mainForm);
            }
        }

        /// <summary>
        /// 配置 Autofac 容器：注册所有依赖
        /// </summary>
        private static void ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            // 注册 Data 层
            builder.RegisterType<DatabaseHelper>().SingleInstance();
            builder.RegisterType<FoodRepository>().InstancePerLifetimeScope();
            builder.RegisterType<InventoryRepository>().InstancePerLifetimeScope();

            // 注册 Service 层
            builder.RegisterType<ExpirationService>().InstancePerLifetimeScope();
            builder.RegisterType<FoodService>().InstancePerLifetimeScope();
            builder.RegisterType<InventoryService>().InstancePerLifetimeScope();

            // 注册 Forms
            builder.RegisterType<MainForm>();
            builder.RegisterType<FoodEditForm>();
            builder.RegisterType<InventoryForm>();
            builder.RegisterType<QueryForm>();
            builder.RegisterType<ExpiringListForm>();
            builder.RegisterType<BatchImportForm>();

            Container = builder.Build();
        }
    }
}
