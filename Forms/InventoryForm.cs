using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FoodExpirationManager.Services;

namespace FoodExpirationManager.Forms
{
    /// <summary>
    /// 入库/出库窗体
    /// </summary>
    public partial class InventoryForm : Form
    {
        private readonly FoodService _foodService;
        private readonly InventoryService _inventoryService;

        // 控件
        private ComboBox cmbFood;
        private RadioButton rbStockIn, rbStockOut;
        private NumericUpDown nudQuantity;
        private DateTimePicker dtpRecordDate;
        private TextBox txtRemark;
        private Label lblCurrentStock;
        private Button btnSave, btnCancel;

        public InventoryForm(FoodService foodService, InventoryService inventoryService)
        {
            _foodService = foodService;
            _inventoryService = inventoryService;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "📋 入库/出库管理";
            this.Size = new Size(520, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Microsoft YaHei", 9F);

            int leftLabel = 30;
            int leftControl = 150;
            int topY = 60;
            int rowHeight = 40;
            int controlWidth = 300;

            // 标题
            var lblTitle = new Label
            {
                Text = "📋 库存操作",
                Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold),
                Location = new Point(30, 15),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(30, 50),
                Size = new Size(440, 2)
            };

            // 选择食品
            var lblFood = new Label { Text = "选择食品：", Location = new Point(leftLabel, topY), Size = new Size(110, 23), TextAlign = ContentAlignment.MiddleRight };
            cmbFood = new ComboBox { Location = new Point(leftControl, topY), Size = new Size(controlWidth, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFood.SelectedIndexChanged += CmbFood_SelectedIndexChanged;
            topY += rowHeight;

            // 当前库存
            var lblStockTitle = new Label { Text = "当前库存：", Location = new Point(leftLabel, topY), Size = new Size(110, 23), TextAlign = ContentAlignment.MiddleRight };
            lblCurrentStock = new Label
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                Text = "—",
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            topY += rowHeight + 5;

            // 操作类型
            var lblOpType = new Label { Text = "操作类型：", Location = new Point(leftLabel, topY), Size = new Size(110, 23), TextAlign = ContentAlignment.MiddleRight };
            rbStockIn = new RadioButton { Text = "📥 入库", Location = new Point(leftControl, topY), Size = new Size(120, 23), Checked = true };
            rbStockOut = new RadioButton { Text = "📤 出库", Location = new Point(leftControl + 130, topY), Size = new Size(120, 23) };
            topY += rowHeight;

            // 数量
            var lblQuantity = new Label { Text = "操作数量：", Location = new Point(leftLabel, topY), Size = new Size(110, 23), TextAlign = ContentAlignment.MiddleRight };
            nudQuantity = new NumericUpDown
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                Minimum = 1,
                Maximum = 99999,
                Value = 1
            };
            topY += rowHeight;

            // 日期
            var lblDate = new Label { Text = "操作日期：", Location = new Point(leftLabel, topY), Size = new Size(110, 23), TextAlign = ContentAlignment.MiddleRight };
            dtpRecordDate = new DateTimePicker
            {
                Location = new Point(leftControl, topY),
                Size = new Size(controlWidth, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                Value = DateTime.Now
            };
            topY += rowHeight;

            // 备注
            var lblRemark = new Label { Text = "备注：", Location = new Point(leftLabel, topY), Size = new Size(110, 23), TextAlign = ContentAlignment.MiddleRight };
            txtRemark = new TextBox { Location = new Point(leftControl, topY), Size = new Size(controlWidth, 50), Multiline = true };
            topY += 65;

            // 按钮
            btnSave = new Button
            {
                Text = "💾 确认操作",
                Size = new Size(120, 36),
                Location = new Point(150, topY + 5),
                BackColor = Color.FromArgb(46, 204, 113),
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
            this.Controls.Add(lblFood);
            this.Controls.Add(cmbFood);
            this.Controls.Add(lblStockTitle);
            this.Controls.Add(lblCurrentStock);
            this.Controls.Add(lblOpType);
            this.Controls.Add(rbStockIn);
            this.Controls.Add(rbStockOut);
            this.Controls.Add(lblQuantity);
            this.Controls.Add(nudQuantity);
            this.Controls.Add(lblDate);
            this.Controls.Add(dtpRecordDate);
            this.Controls.Add(lblRemark);
            this.Controls.Add(txtRemark);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadFoodList();
        }

        private void LoadFoodList()
        {
            try
            {
                var foods = _foodService.GetAllFoods();
                cmbFood.DataSource = null;
                cmbFood.DisplayMember = "Name";
                cmbFood.ValueMember = "Id";
                cmbFood.DataSource = foods;

                if (foods.Count > 0)
                {
                    cmbFood.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载食品列表失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CmbFood_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFood.SelectedValue is int foodId && foodId > 0)
            {
                var food = _foodService.GetFoodById(foodId);
                if (food != null)
                {
                    lblCurrentStock.Text = $"{food.StockQuantity} 件";
                    lblCurrentStock.ForeColor = food.StockQuantity > 0
                        ? Color.FromArgb(46, 204, 113)
                        : Color.FromArgb(231, 76, 60);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbFood.SelectedValue == null || !(cmbFood.SelectedValue is int foodId) || foodId <= 0)
                {
                    MessageBox.Show("请选择食品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var food = _foodService.GetFoodById(foodId);
                int quantity = (int)nudQuantity.Value;
                string remark = txtRemark.Text.Trim();

                if (rbStockIn.Checked)
                {
                    _inventoryService.StockIn(foodId, quantity, remark);
                    MessageBox.Show($"入库成功！\n食品: {food.Name}\n数量: +{quantity}\n当前库存: {food.StockQuantity + quantity} 件",
                        "入库成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _inventoryService.StockOut(foodId, quantity, remark);
                    MessageBox.Show($"出库成功！\n食品: {food.Name}\n数量: -{quantity}\n剩余库存: {food.StockQuantity - quantity} 件",
                        "出库成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
