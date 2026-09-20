using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FoodExpirationManager.Models;
using FoodExpirationManager.Services;

namespace FoodExpirationManager.Forms
{
    /// <summary>
    /// 临期食品列表窗体
    /// </summary>
    public partial class ExpiringListForm : Form
    {
        private readonly ExpirationService _expirationService;

        // 控件
        private ComboBox cmbFilter;
        private Button btnRefresh;
        private DataGridView dgvExpiring;
        private Label lblSummary;
        private Panel statsPanel;
        private Label lblTotal, lblExpiring, lblExpired;

        public ExpiringListForm(ExpirationService expirationService)
        {
            _expirationService = expirationService;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "⚠️ 临期预警";
            this.Size = new Size(900, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei", 9F);

            // 顶部统计面板
            statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            var lblTitle = new Label
            {
                Text = "⚠️ 临期预警",
                Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold),
                Location = new Point(15, 10),
                Size = new Size(180, 28),
                ForeColor = Color.FromArgb(231, 76, 60)
            };

            // 统计标签
            lblTotal = new Label
            {
                Text = "总数: —",
                Font = new Font("Microsoft YaHei", 10F),
                Location = new Point(220, 12),
                Size = new Size(130, 22),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            lblExpiring = new Label
            {
                Text = "⚠️ 临期: —",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Location = new Point(360, 12),
                Size = new Size(150, 22),
                ForeColor = Color.FromArgb(243, 156, 18)
            };

            lblExpired = new Label
            {
                Text = "❌ 已过期: —",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Location = new Point(520, 12),
                Size = new Size(150, 22),
                ForeColor = Color.FromArgb(231, 76, 60)
            };

            statsPanel.Controls.Add(lblTitle);
            statsPanel.Controls.Add(lblTotal);
            statsPanel.Controls.Add(lblExpiring);
            statsPanel.Controls.Add(lblExpired);

            // 筛选面板
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(10, 8, 10, 5)
            };

            var lblFilter = new Label { Text = "筛选条件：", Location = new Point(12, 10), Size = new Size(75, 23), TextAlign = ContentAlignment.MiddleRight };

            cmbFilter = new ComboBox
            {
                Location = new Point(90, 10),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilter.Items.AddRange(new object[] { "⚠️ 临期食品（即将过期）", "❌ 已过期食品", "📋 全部（临期+过期）" });
            cmbFilter.SelectedIndexChanged += (s, e) => RefreshData();

            btnRefresh = new Button
            {
                Text = "🔄 刷新",
                Location = new Point(290, 8),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => RefreshData();

            filterPanel.Controls.Add(lblFilter);
            filterPanel.Controls.Add(cmbFilter);
            filterPanel.Controls.Add(btnRefresh);

            // 数据表格
            var gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            dgvExpiring = new DataGridView
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

            gridPanel.Controls.Add(dgvExpiring);

            // 底部汇总
            lblSummary = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                Text = "",
                Padding = new Padding(10, 8, 10, 5),
                BackColor = Color.FromArgb(250, 250, 250),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            this.Controls.Add(gridPanel);
            this.Controls.Add(lblSummary);
            this.Controls.Add(filterPanel);
            this.Controls.Add(statsPanel);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            cmbFilter.SelectedIndex = 0;
        }

        private void RefreshData()
        {
            try
            {
                var summary = _expirationService.GetSummary();

                // 更新顶部统计
                lblTotal.Text = $"📦 总食品数: {summary.TotalCount}";
                lblExpiring.Text = $"⚠️ 临期: {summary.ExpiringCount}";
                lblExpired.Text = $"❌ 已过期: {summary.ExpiredCount}";

                System.Collections.Generic.List<FoodItem> foods;

                switch (cmbFilter.SelectedIndex)
                {
                    case 0: // 临期
                        foods = _expirationService.GetExpiringFoods();
                        break;
                    case 1: // 过期
                        foods = _expirationService.GetExpiredFoods();
                        break;
                    default: // 全部
                        foods = _expirationService.GetAllFoodsWithStatus()
                            .Where(f => f.Status == FoodStatus.Expiring || f.Status == FoodStatus.Expired)
                            .ToList();
                        break;
                }

                dgvExpiring.DataSource = null;
                dgvExpiring.DataSource = foods.Select(f => new
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

                // 调整列宽
                if (dgvExpiring.Columns.Count > 0)
                {
                    dgvExpiring.Columns["Id"].Width = 40;
                    dgvExpiring.Columns["Name"].MinimumWidth = 130;
                    dgvExpiring.Columns["Category"].Width = 80;
                    dgvExpiring.Columns["状态"].Width = 150;
                    dgvExpiring.Columns["库存"].Width = 50;
                }

                // 着色
                foreach (DataGridViewRow row in dgvExpiring.Rows)
                {
                    var statusText = row.Cells["状态"].Value?.ToString() ?? "";
                    if (statusText.Contains("已过期"))
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    else if (statusText.Contains("临期"))
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                }

                lblSummary.Text = $" 当前筛选结果: {foods.Count} 条记录 | ⚠️ 默认临期阈值: {_expirationService.DefaultThresholdDays} 天（每件食品可独立设置）";
            }
            catch (Exception ex)
            {
                MessageBox.Show("刷新数据失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
