using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FoodExpirationManager.Models;
using FoodExpirationManager.Services;

namespace FoodExpirationManager.Forms
{
    /// <summary>
    /// 查询食品窗体，支持按名称、分类、状态筛选
    /// </summary>
    public partial class QueryForm : Form
    {
        private readonly FoodService _foodService;

        // 控件
        private TextBox txtSearchName;
        private ComboBox cmbCategoryFilter;
        private ComboBox cmbStatusFilter;
        private Button btnSearch, btnReset, btnEdit, btnDelete;
        private DataGridView dgvResults;
        private Label lblResultCount;

        public QueryForm(FoodService foodService)
        {
            _foodService = foodService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🔍 查询食品";
            this.Size = new Size(920, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 450);
            this.Font = new Font("Microsoft YaHei", 9F);

            // 搜索面板
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(15, 12, 15, 12)
            };

            var lblTitle = new Label
            {
                Text = "🔍 食品查询",
                Font = new Font("Microsoft YaHei", 13F, FontStyle.Bold),
                Location = new Point(15, 8),
                Size = new Size(200, 28),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            // 第一行：名称 + 分类
            int yRow1 = 42;
            var lblName = new Label { Text = "名称：", Location = new Point(20, yRow1), Size = new Size(45, 25), TextAlign = ContentAlignment.MiddleRight };
            txtSearchName = new TextBox { Location = new Point(68, yRow1), Size = new Size(170, 25) };
            txtSearchName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformSearch(); };

            var lblCategory = new Label { Text = "分类：", Location = new Point(248, yRow1), Size = new Size(45, 25), TextAlign = ContentAlignment.MiddleRight };
            cmbCategoryFilter = new ComboBox { Location = new Point(296, yRow1), Size = new Size(140, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            // 状态筛选（新增）
            var lblStatus = new Label { Text = "状态：", Location = new Point(446, yRow1), Size = new Size(45, 25), TextAlign = ContentAlignment.MiddleRight };
            cmbStatusFilter = new ComboBox { Location = new Point(494, yRow1), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatusFilter.Items.AddRange(new object[] { "（全部状态）", "✅ 正常", "⚠️ 临期", "❌ 已过期" });

            // 按钮行
            int yBtn = yRow1 + 3;
            btnSearch = new Button
            {
                Text = "🔍 搜索",
                Location = new Point(640, yBtn),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => PerformSearch();

            btnReset = new Button
            {
                Text = "🔄 重置",
                Location = new Point(738, yBtn),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.Click += BtnReset_Click;

            searchPanel.Controls.Add(lblTitle);
            searchPanel.Controls.Add(lblName);
            searchPanel.Controls.Add(txtSearchName);
            searchPanel.Controls.Add(lblCategory);
            searchPanel.Controls.Add(cmbCategoryFilter);
            searchPanel.Controls.Add(lblStatus);
            searchPanel.Controls.Add(cmbStatusFilter);
            searchPanel.Controls.Add(btnSearch);
            searchPanel.Controls.Add(btnReset);

            // 结果面板
            var resultPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            lblResultCount = new Label
            {
                Text = "共找到 0 条记录",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            dgvResults = new DataGridView
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

            resultPanel.Controls.Add(dgvResults);
            resultPanel.Controls.Add(lblResultCount);

            // 底部按钮面板
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };

            btnEdit = new Button
            {
                Text = "✏️ 编辑选中",
                Size = new Size(110, 34),
                Location = new Point(10, 8),
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "🗑️ 删除选中",
                Size = new Size(110, 34),
                Location = new Point(130, 8),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            bottomPanel.Controls.Add(btnEdit);
            bottomPanel.Controls.Add(btnDelete);

            this.Controls.Add(resultPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(searchPanel);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadCategories();
            cmbStatusFilter.SelectedIndex = 0;
            PerformSearch();
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _foodService.GetAllCategories();
                cmbCategoryFilter.Items.Clear();
                cmbCategoryFilter.Items.Add("（全部分类）");
                foreach (var cat in categories)
                {
                    cmbCategoryFilter.Items.Add(cat);
                }
                cmbCategoryFilter.SelectedIndex = 0;
            }
            catch { /* ignore */ }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtSearchName.Text = "";
            cmbCategoryFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndex = 0;
            PerformSearch();
        }

        /// <summary>
        /// 执行搜索，支持名称、分类、状态三维筛选
        /// </summary>
        private void PerformSearch()
        {
            try
            {
                string name = txtSearchName.Text.Trim();
                string category = cmbCategoryFilter.Text.Contains("全部") ? "" : cmbCategoryFilter.Text;

                // 解析状态筛选
                FoodStatus? statusFilter = null;
                if (cmbStatusFilter.SelectedIndex == 1) statusFilter = FoodStatus.Normal;
                else if (cmbStatusFilter.SelectedIndex == 2) statusFilter = FoodStatus.Expiring;
                else if (cmbStatusFilter.SelectedIndex == 3) statusFilter = FoodStatus.Expired;

                var foods = _foodService.SearchFoods(name, category, statusFilter);

                dgvResults.DataSource = null;
                dgvResults.DataSource = foods.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Category,
                    生产日期 = f.ProductionDate.ToString("yyyy-MM-dd"),
                    过期日期 = f.ExpirationDate.ToString("yyyy-MM-dd"),
                    保质期 = $"{f.ShelfLife}天",
                    剩余天数 = f.RemainingDays,
                    状态 = f.StatusText,
                    库存 = f.StockQuantity,
                    提醒阈值 = $"{f.ReminderThresholdDays}天"
                }).ToList();

                if (dgvResults.Columns.Count > 0)
                {
                    dgvResults.Columns["Id"].Width = 40;
                    dgvResults.Columns["Name"].MinimumWidth = 120;
                    dgvResults.Columns["Category"].Width = 70;
                    dgvResults.Columns["状态"].Width = 140;
                    dgvResults.Columns["库存"].Width = 50;
                    dgvResults.Columns["提醒阈值"].Width = 70;
                }

                // 着色
                foreach (DataGridViewRow row in dgvResults.Rows)
                {
                    var statusText = row.Cells["状态"].Value?.ToString() ?? "";
                    if (statusText.Contains("已过期"))
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                    else if (statusText.Contains("临期"))
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                }

                lblResultCount.Text = $"共找到 {foods.Count} 条记录";
            }
            catch (Exception ex)
            {
                MessageBox.Show("搜索失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvResults.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var foodId = (int)dgvResults.SelectedRows[0].Cells["Id"].Value;
            var food = _foodService.GetFoodById(foodId);

            if (food != null)
            {
                using (var form = new FoodEditForm(_foodService, food))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                        PerformSearch();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvResults.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var foodId = (int)dgvResults.SelectedRows[0].Cells["Id"].Value;
            var foodName = dgvResults.SelectedRows[0].Cells["Name"].Value.ToString();

            var result = MessageBox.Show(
                $"确定要删除食品 [{foodName}] 吗？\n\n此操作不可恢复！",
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _foodService.DeleteFood(foodId);
                    MessageBox.Show($"食品 [{foodName}] 已删除", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PerformSearch();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("删除失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
