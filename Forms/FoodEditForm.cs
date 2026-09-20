using System;
using System.Drawing;
using System.Windows.Forms;
using FoodExpirationManager.Models;
using FoodExpirationManager.Services;

namespace FoodExpirationManager.Forms
{
    /// <summary>
    /// 新增/修改食品窗体
    /// </summary>
    public partial class FoodEditForm : Form
    {
        private readonly FoodService _foodService;
        private readonly FoodItem _existingFood;
        private readonly bool _isEditMode;

        // 控件
        private TextBox txtName;
        private ComboBox cmbCategory;
        private DateTimePicker dtpProductionDate;
        private NumericUpDown nudShelfLife;
        private Label lblExpirationDate;
        private NumericUpDown nudReminderThreshold;
        private Button btnSave, btnCancel;

        public FoodEditForm(FoodService foodService, FoodItem foodToEdit = null)
        {
            _foodService = foodService;
            _existingFood = foodToEdit;
            _isEditMode = foodToEdit != null;

            InitializeComponent();

            if (_isEditMode)
            {
                LoadFoodData();
            }
        }

        private void InitializeComponent()
        {
            this.Text = _isEditMode ? "修改食品" : "新增食品";
            this.Size = new Size(520, 460);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Microsoft YaHei", 9F);

            int leftLabel = 30;
            int leftControl = 170;
            int topY = 60;
            int rowHeight = 40;
            int controlWidth = 280;

            // 标题
            var lblTitle = new Label
            {
                Text = _isEditMode ? "✏️ 修改食品信息" : "➕ 新增食品",
                Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold),
                Location = new Point(30, 15),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            // 分隔线
            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(30, 50),
                Size = new Size(440, 2)
            };

            // 食品名称
            var lblName = new Label { Text = "食品名称：", Location = new Point(leftLabel, topY), Size = new Size(130, 23), TextAlign = ContentAlignment.MiddleRight };
            txtName = new TextBox { Location = new Point(leftControl, topY), Size = new Size(controlWidth, 23) };
            topY += rowHeight;

            // 食品类别
            var lblCategory = new Label { Text = "食品分类：", Location = new Point(leftLabel, topY), Size = new Size(130, 23), TextAlign = ContentAlignment.MiddleRight };
            cmbCategory = new ComboBox { Location = new Point(leftControl, topY), Size = new Size(controlWidth, 23), DropDownStyle = ComboBoxStyle.DropDown };
            cmbCategory.Items.AddRange(new object[] {
                "乳制品", "蔬菜", "水果", "肉类", "海鲜", "调味品",
                "饮料", "零食", "罐头", "冷冻食品", "烘焙", "其他"
            });
            topY += rowHeight;

            // 生产日期
            var lblProductionDate = new Label { Text = "生产日期：", Location = new Point(leftLabel, topY), Size = new Size(130, 23), TextAlign = ContentAlignment.MiddleRight };
            dtpProductionDate = new DateTimePicker
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd",
                Value = DateTime.Today.AddMonths(-1)
            };
            dtpProductionDate.ValueChanged += OnDateOrShelfLifeChanged;
            topY += rowHeight;

            // 保质期（天）
            var lblShelfLife = new Label { Text = "保质期（天）：", Location = new Point(leftLabel, topY), Size = new Size(130, 23), TextAlign = ContentAlignment.MiddleRight };
            nudShelfLife = new NumericUpDown
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                Minimum = 1,
                Maximum = 36500, // 最大约100年
                Value = 180
            };
            nudShelfLife.ValueChanged += OnDateOrShelfLifeChanged;
            topY += rowHeight;

            // 到期日期（自动计算，只读）
            var lblExpDateTitle = new Label { Text = "到期日期：", Location = new Point(leftLabel, topY), Size = new Size(130, 23), TextAlign = ContentAlignment.MiddleRight };
            lblExpirationDate = new Label
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(39, 174, 96),
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };
            UpdateExpirationDateDisplay();
            topY += rowHeight;

            // 临期提醒阈值（天）
            var lblReminder = new Label { Text = "临期提醒阈值（天）：", Location = new Point(leftLabel, topY), Size = new Size(130, 23), TextAlign = ContentAlignment.MiddleRight };
            nudReminderThreshold = new NumericUpDown
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                Minimum = 1,
                Maximum = 365,
                Value = 30
            };
            topY += rowHeight + 15;

            // 提示文字
            var lblHint = new Label
            {
                Text = "💡 到期日期 = 生产日期 + 保质期，自动计算",
                Location = new Point(leftControl, topY - 10),
                Size = new Size(controlWidth, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Microsoft YaHei", 8F)
            };
            topY += 20;

            // 按钮
            btnSave = new Button
            {
                Text = _isEditMode ? "💾 保存修改" : "💾 保存",
                Size = new Size(120, 36),
                Location = new Point(150, topY + 5),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(100, 36),
                Location = new Point(280, topY + 5),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblTitle);
            this.Controls.Add(separator);
            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblCategory);
            this.Controls.Add(cmbCategory);
            this.Controls.Add(lblProductionDate);
            this.Controls.Add(dtpProductionDate);
            this.Controls.Add(lblShelfLife);
            this.Controls.Add(nudShelfLife);
            this.Controls.Add(lblExpDateTitle);
            this.Controls.Add(lblExpirationDate);
            this.Controls.Add(lblReminder);
            this.Controls.Add(nudReminderThreshold);
            this.Controls.Add(lblHint);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadFoodData()
        {
            txtName.Text = _existingFood.Name;
            cmbCategory.Text = _existingFood.Category;
            dtpProductionDate.Value = _existingFood.ProductionDate;
            nudShelfLife.Value = _existingFood.ShelfLife;
            nudReminderThreshold.Value = _existingFood.ReminderThresholdDays > 0
                ? _existingFood.ReminderThresholdDays
                : 30;
            UpdateExpirationDateDisplay();
        }

        private void OnDateOrShelfLifeChanged(object sender, EventArgs e)
        {
            UpdateExpirationDateDisplay();
        }

        private void UpdateExpirationDateDisplay()
        {
            DateTime expDate = dtpProductionDate.Value.AddDays((int)nudShelfLife.Value);
            lblExpirationDate.Text = expDate.ToString("yyyy-MM-dd") + $" （共 {expDate.Subtract(dtpProductionDate.Value).Days} 天）";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("请输入食品名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cmbCategory.Text))
                {
                    MessageBox.Show("请输入或选择食品分类", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbCategory.Focus();
                    return;
                }

                var food = _isEditMode ? _existingFood : new FoodItem();
                food.Name = txtName.Text.Trim();
                food.Category = cmbCategory.Text.Trim();
                food.ProductionDate = dtpProductionDate.Value;
                food.ShelfLife = (int)nudShelfLife.Value;
                food.ReminderThresholdDays = (int)nudReminderThreshold.Value;

                if (_isEditMode)
                {
                    _foodService.UpdateFood(food);
                    MessageBox.Show("食品信息已更新！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _foodService.AddFood(food);
                    MessageBox.Show("食品添加成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
