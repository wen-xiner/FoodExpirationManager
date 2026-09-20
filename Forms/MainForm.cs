using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FoodExpirationManager.Models;
using FoodExpirationManager.Services;

namespace FoodExpirationManager.Forms
{
    /// <summary>
    /// 首页（含统计信息、导航和临期提醒）
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly FoodService _foodService;
        private readonly ExpirationService _expirationService;
        private readonly InventoryService _inventoryService;

        // 统计标签
        private Label lblTotalValue;
        private Label lblNormalValue;
        private Label lblExpiringValue;
        private Label lblExpiredValue;

        // 导航按钮
        private Button btnAddFood, btnInventory, btnQuery, btnExpiringList, btnRefresh;

        // 数据表格
        private DataGridView dgvFoods;

        // 状态栏
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;

        public MainForm(FoodService foodService, ExpirationService expirationService, InventoryService inventoryService)
        {
            _foodService = foodService;
            _expirationService = expirationService;
            _inventoryService = inventoryService;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "食品临期管理系统";
            this.Size = new Size(980, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);
            this.Font = new Font("Microsoft YaHei", 9F);

            // ===== 顶部统计面板 =====
            var statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            var lblTitle = new Label
            {
                Text = "📊 食品临期管理系统",
                Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold),
                Location = new Point(15, 10),
                Size = new Size(350, 35),
                ForeColor = Color.FromArgb(33, 33, 33)
            };

            int cardWidth = 180;
            int cardHeight = 55;
            int startX = 400;
            int startY = 5;

            var pnlTotal = CreateStatCard("📦 食品总数", "0", startX, startY, cardWidth, cardHeight, Color.FromArgb(52, 152, 219));
            lblTotalValue = (Label)pnlTotal.Controls[1];

            var pnlNormal = CreateStatCard("✅ 正常", "0", startX + cardWidth + 10, startY, cardWidth, cardHeight, Color.FromArgb(46, 204, 113));
            lblNormalValue = (Label)pnlNormal.Controls[1];

            var pnlExpiring = CreateStatCard("⚠️ 临期", "0", startX, startY + cardHeight + 5, cardWidth, cardHeight, Color.FromArgb(243, 156, 18));
            lblExpiringValue = (Label)pnlExpiring.Controls[1];

            var pnlExpired = CreateStatCard("❌ 已过期", "0", startX + cardWidth + 10, startY + cardHeight + 5, cardWidth, cardHeight, Color.FromArgb(231, 76, 60));
            lblExpiredValue = (Label)pnlExpired.Controls[1];

            statsPanel.Controls.Add(lblTitle);
            statsPanel.Controls.Add(pnlTotal);
            statsPanel.Controls.Add(pnlNormal);
            statsPanel.Controls.Add(pnlExpiring);
            statsPanel.Controls.Add(pnlExpired);

            // ===== 按钮面板 =====
            var btnPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 8, 10, 8)
            };

            btnAddFood = CreateNavButton("➕ 新增食品", 10, Color.FromArgb(52, 152, 219));
            btnAddFood.Click += (s, e) => OpenDialogForm(() => new FoodEditForm(_foodService));

            btnInventory = CreateNavButton("📋 入库/出库", 140, Color.FromArgb(46, 204, 113));
            btnInventory.Click += (s, e) => OpenDialogForm(() => new InventoryForm(_foodService, _inventoryService));

            btnQuery = CreateNavButton("🔍 查询食品", 270, Color.FromArgb(155, 89, 182));
            btnQuery.Click += (s, e) => OpenModelessForm(() => new QueryForm(_foodService));

            btnExpiringList = CreateNavButton("⚠️ 临期预警", 400, Color.FromArgb(243, 156, 18));
            btnExpiringList.Click += (s, e) => OpenModelessForm(() => new ExpiringListForm(_expirationService));

            var btnBatchImport = CreateNavButton("📥 批量导入", 530, Color.FromArgb(142, 68, 173));
            btnBatchImport.Click += (s, e) => OpenDialogForm(() => new BatchImportForm(_foodService));

            btnRefresh = new Button
            {
                Text = "🔄 刷新数据",
                Size = new Size(120, 34),
                Location = new Point(660, 8),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshData();

            btnPanel.Controls.Add(btnAddFood);
            btnPanel.Controls.Add(btnInventory);
            btnPanel.Controls.Add(btnQuery);
            btnPanel.Controls.Add(btnExpiringList);
            btnPanel.Controls.Add(btnBatchImport);
            btnPanel.Controls.Add(btnRefresh);

            // ===== 数据表格 =====
            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            dgvFoods = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None
            };
            dgvFoods.CellDoubleClick += DgvFoods_CellDoubleClick;

            gridPanel.Controls.Add(dgvFoods);

            // ===== 状态栏 =====
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("就绪");
            statusStrip.Items.Add(lblStatus);

            this.Controls.Add(gridPanel);
            this.Controls.Add(btnPanel);
            this.Controls.Add(statsPanel);
            this.Controls.Add(statusStrip);
        }

        /// <summary>
        /// 创建导航按钮
        /// </summary>
        private Button CreateNavButton(string text, int x, Color color)
        {
            return new Button
            {
                Text = text,
                Size = new Size(120, 34),
                Location = new Point(x, 8),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
        }

        /// <summary>
        /// 创建统计卡片控件
        /// </summary>
        private Panel CreateStatCard(string title, string value, int x, int y, int width, int height, Color accentColor)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Microsoft YaHei", 9F),
                Location = new Point(8, 5),
                Size = new Size(width - 16, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Microsoft YaHei", 18F, FontStyle.Bold),
                Location = new Point(8, 24),
                Size = new Size(width - 16, 28),
                ForeColor = accentColor
            };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblValue);

            var bar = new Panel
            {
                Size = new Size(width - 2, 3),
                Location = new Point(0, 0),
                BackColor = accentColor
            };
            panel.Controls.Add(bar);

            return panel;
        }

        /// <summary>
        /// 以模态方式打开数据录入子窗口，关闭后自动刷新主界面（确保同一类型不重复打开）
        /// </summary>
        private void OpenDialogForm<T>(Func<T> formFactory) where T : Form
        {
            // 检查同类型窗口是否已打开
            var existing = Application.OpenForms.OfType<T>().FirstOrDefault();
            if (existing != null)
            {
                existing.Activate();
                return;
            }

            var form = formFactory();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                RefreshData();
            }
        }

        /// <summary>
        /// 以非模态方式打开查询/浏览子窗口（确保同一类型不重复打开），关闭时刷新
        /// </summary>
        private void OpenModelessForm<T>(Func<T> formFactory) where T : Form
        {
            var existing = Application.OpenForms.OfType<T>().FirstOrDefault();
            if (existing != null)
            {
                existing.Activate();
                return;
            }

            var form = formFactory();
            form.FormClosed += (s, e) => RefreshData();
            form.Show(this);
        }

        // ===== 事件处理 =====

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RefreshData();
            ShowReminderIfNeeded();
        }

        /// <summary>
        /// 启动时检查临期/过期食品，弹窗提醒用户
        /// </summary>
        private void ShowReminderIfNeeded()
        {
            try
            {
                var reminderFoods = _expirationService.GetReminderFoods();
                if (reminderFoods.Count == 0) return;

                var sb = new StringBuilder();
                sb.AppendLine("⚠️ 以下食品需要注意：\n");

                var expiredList = reminderFoods.Where(f => f.Status == FoodStatus.Expired).ToList();
                var expiringList = reminderFoods.Where(f => f.Status == FoodStatus.Expiring).ToList();

                if (expiredList.Count > 0)
                {
                    sb.AppendLine($"❌ 已过期 ({expiredList.Count} 件)：");
                    foreach (var f in expiredList.Take(5))
                        sb.AppendLine($"   • {f.Name} — 已过期 {Math.Abs(f.RemainingDays)} 天 | 库存: {f.StockQuantity}");
                    if (expiredList.Count > 5)
                        sb.AppendLine($"   ... 还有 {expiredList.Count - 5} 件");
                    sb.AppendLine();
                }

                if (expiringList.Count > 0)
                {
                    sb.AppendLine($"⚠️ 临期 ({expiringList.Count} 件)：");
                    foreach (var f in expiringList.Take(5))
                        sb.AppendLine($"   • {f.Name} — 剩余 {f.RemainingDays} 天 | 库存: {f.StockQuantity}");
                    if (expiringList.Count > 5)
                        sb.AppendLine($"   ... 还有 {expiringList.Count - 5} 件");
                }

                sb.AppendLine("\n请及时处理临期和过期食品！");

                MessageBox.Show(sb.ToString(), "临期提醒", MessageBoxButtons.OK,
                    expiredList.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch
            {
                // 提醒失败不影响主流程
            }
        }

        private void DgvFoods_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var foodId = (int)dgvFoods.Rows[e.RowIndex].Cells["Id"].Value;
            var food = _foodService.GetFoodById(foodId);

            if (food != null)
            {
                using (var form = new FoodEditForm(_foodService, food))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                        RefreshData();
                }
            }
        }

        /// <summary>
        /// 刷新首页数据
        /// </summary>
        public void RefreshData()
        {
            try
            {
                lblStatus.Text = "正在刷新...";

                var summary = _expirationService.GetSummary();
                lblTotalValue.Text = summary.TotalCount.ToString();
                lblNormalValue.Text = summary.NormalCount.ToString();
                lblExpiringValue.Text = summary.ExpiringCount.ToString();
                lblExpiredValue.Text = summary.ExpiredCount.ToString();

                var foods = _expirationService.GetAllFoodsWithStatus();

                dgvFoods.DataSource = null;
                dgvFoods.DataSource = foods.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Category,
                    生产日期 = f.ProductionDate.ToString("yyyy-MM-dd"),
                    过期日期 = f.ExpirationDate.ToString("yyyy-MM-dd"),
                    剩余天数 = f.RemainingDays,
                    状态 = f.StatusText,
                    库存 = f.StockQuantity
                }).ToList();

                if (dgvFoods.Columns.Count > 0)
                {
                    dgvFoods.Columns["Id"].Width = 40;
                    dgvFoods.Columns["Name"].MinimumWidth = 120;
                    dgvFoods.Columns["Category"].Width = 80;
                    dgvFoods.Columns["状态"].Width = 140;
                    dgvFoods.Columns["库存"].Width = 60;
                }

                foreach (DataGridViewRow row in dgvFoods.Rows)
                {
                    var statusText = row.Cells["状态"].Value?.ToString() ?? "";
                    if (statusText.Contains("已过期"))
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    else if (statusText.Contains("临期"))
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                }

                lblStatus.Text = $"就绪 - 共 {summary.TotalCount} 种食品 | 上次刷新: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "刷新失败: " + ex.Message;
                MessageBox.Show("刷新数据时出错: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
