using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FoodExpirationManager.Models;
using FoodExpirationManager.Services;

namespace FoodExpirationManager.Forms
{
    /// <summary>
    /// 批量导入食品窗体，支持 CSV 文本粘贴和 TXT 文件导入。
    /// 模板格式：食品名称,分类,生产日期(yyyy-MM-dd),保质期(天),临期阈值(天)
    /// </summary>
    public partial class BatchImportForm : Form
    {
        private readonly FoodService _foodService;

        private TextBox txtContent;
        private Button btnLoadFile, btnPreview, btnImport, btnCancel;
        private DataGridView dgvPreview;
        private Label lblPreviewCount, lblHint;
        private List<FoodItem> _parsedFoods = new List<FoodItem>();

        public BatchImportForm(FoodService foodService)
        {
            _foodService = foodService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "📥 批量导入食品";
            this.Size = new Size(820, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(650, 500);
            this.Font = new Font("Microsoft YaHei", 9F);

            // 顶部说明
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(12) };

            var lblTitle = new Label
            {
                Text = "📥 批量导入食品",
                Font = new Font("Microsoft YaHei", 13F, FontStyle.Bold),
                Location = new Point(12, 8),
                Size = new Size(300, 28),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            lblHint = new Label
            {
                Text = "在下方粘贴 CSV 格式数据（每行一条），或点击 [加载文件] 从 TXT/CSV 文件导入。\n" +
                       "格式：食品名称,分类,生产日期(yyyy-MM-dd),保质期(天),临期阈值(天)\n" +
                       "示例：牛奶,乳制品,2026-07-01,180,30",
                Location = new Point(12, 38),
                Size = new Size(780, 50),
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Consolas", 8.5F)
            };

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblHint);

            // 输入区域
            var inputPanel = new Panel { Dock = DockStyle.Top, Height = 170, Padding = new Padding(12, 5, 12, 5) };

            txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F)
            };

            inputPanel.Controls.Add(txtContent);

            // 按钮行
            var btnPanel = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(12, 6, 12, 6) };

            btnLoadFile = new Button
            {
                Text = "📂 加载文件",
                Size = new Size(100, 32),
                Location = new Point(12, 6),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLoadFile.Click += BtnLoadFile_Click;

            btnPreview = new Button
            {
                Text = "🔍 预览数据",
                Size = new Size(100, 32),
                Location = new Point(120, 6),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPreview.FlatAppearance.BorderSize = 0;
            btnPreview.Click += BtnPreview_Click;

            btnImport = new Button
            {
                Text = "💾 确认导入",
                Size = new Size(110, 32),
                Location = new Point(228, 6),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnImport.FlatAppearance.BorderSize = 0;
            btnImport.Click += BtnImport_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 32),
                Location = new Point(346, 6),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnPanel.Controls.Add(btnLoadFile);
            btnPanel.Controls.Add(btnPreview);
            btnPanel.Controls.Add(btnImport);
            btnPanel.Controls.Add(btnCancel);

            // 预览表格
            var previewPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 8) };

            lblPreviewCount = new Label
            {
                Text = "预览结果：尚未解析",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            dgvPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None
            };

            previewPanel.Controls.Add(dgvPreview);
            previewPanel.Controls.Add(lblPreviewCount);

            this.Controls.Add(previewPanel);
            this.Controls.Add(btnPanel);
            this.Controls.Add(inputPanel);
            this.Controls.Add(topPanel);
        }

        /// <summary>
        /// 从 TXT/CSV 文件加载内容到文本框
        /// </summary>
        private void BtnLoadFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "选择导入文件";
                ofd.Filter = "文本文件 (*.txt;*.csv)|*.txt;*.csv|所有文件 (*.*)|*.*";
                ofd.FilterIndex = 1;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        txtContent.Text = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                        MessageBox.Show($"已加载文件：{Path.GetFileName(ofd.FileName)}", "加载成功",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("读取文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 解析文本框中的 CSV 数据并预览
        /// </summary>
        private void BtnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                _parsedFoods = ParseCsvContent(txtContent.Text);

                dgvPreview.DataSource = null;
                dgvPreview.DataSource = _parsedFoods.Select(f => new
                {
                    序号 = _parsedFoods.IndexOf(f) + 1,
                    食品名称 = f.Name,
                    分类 = f.Category,
                    生产日期 = f.ProductionDate.ToString("yyyy-MM-dd"),
                    保质期天 = f.ShelfLife,
                    到期日期 = f.ExpirationDate.ToString("yyyy-MM-dd"),
                    临期阈值天 = f.ReminderThresholdDays
                }).ToList();

                lblPreviewCount.Text = $"预览结果：成功解析 {_parsedFoods.Count} 条记录";
                lblPreviewCount.ForeColor = _parsedFoods.Count > 0 ? Color.FromArgb(46, 204, 113) : Color.Red;
                btnImport.Enabled = _parsedFoods.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("解析数据失败: " + ex.Message, "格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 执行批量导入
        /// </summary>
        private void BtnImport_Click(object sender, EventArgs e)
        {
            if (_parsedFoods.Count == 0)
            {
                MessageBox.Show("没有可导入的数据，请先预览", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确认导入 {_parsedFoods.Count} 条食品记录？",
                "确认批量导入", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                int successCount = _foodService.BatchImport(_parsedFoods);
                int failCount = _parsedFoods.Count - successCount;

                string msg = $"导入完成！\n成功: {successCount} 条";
                if (failCount > 0) msg += $"\n跳过: {failCount} 条（数据无效）";

                MessageBox.Show(msg, "导入结果", MessageBoxButtons.OK,
                    failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                if (successCount > 0)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 解析 CSV 文本内容为 FoodItem 列表
        /// </summary>
        private List<FoodItem> ParseCsvContent(string content)
        {
            var items = new List<FoodItem>();
            if (string.IsNullOrWhiteSpace(content)) return items;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 跳过注释行和表头行
                if (trimmed.StartsWith("#") || trimmed.StartsWith("//")) continue;
                if (trimmed.StartsWith("食品名称") || trimmed.StartsWith("名称")) continue;

                var parts = trimmed.Split(',');

                if (parts.Length < 4) continue; // 至少需要：名称,分类,生产日期,保质期

                try
                {
                    var food = new FoodItem
                    {
                        Name = parts[0].Trim(),
                        Category = parts[1].Trim(),
                        ProductionDate = DateTime.Parse(parts[2].Trim()),
                        ShelfLife = int.Parse(parts[3].Trim()),
                        ReminderThresholdDays = parts.Length >= 5 && int.TryParse(parts[4].Trim(), out int threshold)
                            ? threshold : 30
                    };

                    if (!string.IsNullOrEmpty(food.Name) && !string.IsNullOrEmpty(food.Category)
                        && food.ShelfLife > 0)
                    {
                        food.ExpirationDate = food.ProductionDate.AddDays(food.ShelfLife);
                        items.Add(food);
                    }
                }
                catch
                {
                    // 跳过解析失败的行
                }
            }

            return items;
        }
    }
}
