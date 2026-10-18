using MediBillingPro.Domain.Entities.Medicine_inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    public partial class MedicineEditorForm : Form
    {

        private readonly Medicine _existing;

        private readonly Color _green = Color.FromArgb(8, 127, 91);
        private readonly Color _darkGreen = Color.FromArgb(21, 60, 49);
        private readonly Color _page = Color.FromArgb(244, 248, 246);
        private readonly Color _muted = Color.FromArgb(91, 111, 101);

        private TextBox txtMedicineName;
        private TextBox txtGenericName;
        private TextBox txtManufacturer;
        private TextBox txtBarcode;
        private TextBox txtCategory;
        private TextBox txtStrength;
        private ComboBox cmbUnit;
        private NumericUpDown numReorderLevel;
        private CheckBox chkPrescription;

        private TextBox txtRackCode;
        private TextBox txtShelfCode;
        private TextBox txtTrayCode;
        private TextBox txtBoxCode;
        private TextBox txtStorageNotes;


        private ComboBox cmbProductType;
        private NumericUpDown numPurchasePrice;
        private NumericUpDown numMarginPercent;
        private ComboBox cmbGstPercent;

        private Label lblSellingPrice;
        private Label lblTaxablePrice;
        private Label lblGstAmount;

        private bool _loadingPricing;

        private decimal _calculatedTaxablePrice;
        private decimal _calculatedGstAmount;
        private decimal _calculatedSellingPrice;

        
        public Medicine MedicineData { get; private set; }

        public MedicineEditorForm(Medicine existing = null)
        {
            _existing = existing;           

            BuildUI();

            if (_existing != null)
                LoadExistingMedicine();
        }

        private void BuildUI()
        {
            Text = _existing == null
            ? "Add Medicine"
            : "Edit Medicine";

            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(620, 650);
            Size = new Size(760, 800);
            MaximizeBox = true;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = _page
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

            Controls.Add(root);

            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _green,
                Padding = new Padding(22, 12, 20, 8)
            };

            var title = new Label
            {
                Text = _existing == null ? "Add Medicine" : "Edit Medicine",
                AutoSize = true,
                Location = new Point(20, 10),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold)
            };

            var subtitle = new Label
            {
                Text = "Manage medicine information and physical storage",
                AutoSize = true,
                Location = new Point(22, 43),
                ForeColor = Color.FromArgb(220, 246, 235),
                Font = new Font("Segoe UI", 9F)
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            root.Controls.Add(header, 0, 0);

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = _page,
                Padding = new Padding(18, 12, 18, 12)
            };

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = Color.White,
                Padding = new Padding(18),
                Margin = Padding.Empty
            };

            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            scroll.Controls.Add(content);
            root.Controls.Add(scroll, 0, 1);

            AddSection(content, "MEDICINE DETAILS");

            txtMedicineName = AddTextField(
                content, "Medicine name *");

            txtGenericName = AddTextField(
                content, "Generic name");

            txtManufacturer = AddTextField(
                content, "Manufacturer");

            txtBarcode = AddTextField(
                content, "Barcode / Product code");

            txtCategory = AddTextField(
                content, "Category");

            txtStrength = AddTextField(
                content, "Strength");

            cmbUnit = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10F),
                Height = 32,
                Margin = new Padding(3, 5, 3, 8)
            };

            cmbUnit.Items.AddRange(new object[]
            {
                "Tablet", "Strip", "Capsule", "Bottle",
                "Syrup", "Box", "Tube", "Piece", "Pack",
                "Vial", "Injection", "Other"
            });

            AddField(content, "Unit", cmbUnit);

            numReorderLevel = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 150,
                Minimum = 0,
                Maximum = 1000000,
                Value = 10,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 5, 3, 8)
            };

            AddField(content, "Reorder level", numReorderLevel);

            chkPrescription = new CheckBox
            {
                Text = "Prescription required",
                AutoSize = true,
                ForeColor = _darkGreen,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(3, 8, 3, 10)
            };

            AddField(content, "Prescription", chkPrescription);


            AddSection(content, "PRICING AND TAX");

            cmbProductType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 5, 3, 8)
            };

            cmbProductType.Items.AddRange(new object[]
            {
    "Generic",
    "Company / Branded",
    "Fancy",
    "Food Product",
    "Other"
            });

            cmbProductType.SelectedIndexChanged += (s, e) =>
            {
                if (!_loadingPricing)
                {
                    numMarginPercent.Value =
                        GetDefaultMargin(cmbProductType.Text);

                    RecalculatePrices();
                }
            };

            AddField(content, "Product type", cmbProductType);

            numPurchasePrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 10000000,
                Increment = 1,
                ThousandsSeparator = true,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 5, 3, 8)
            };

            numPurchasePrice.ValueChanged += (s, e) =>
                RecalculatePrices();

            AddField(content, "Buying price (₹)", numPurchasePrice);

            numMarginPercent = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 500,
                Increment = 0.5m,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 5, 3, 8)
            };

            numMarginPercent.ValueChanged += (s, e) =>
                RecalculatePrices();

            AddField(content, "Margin / markup (%)", numMarginPercent);

            cmbGstPercent = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 5, 3, 8)
            };

            cmbGstPercent.Items.AddRange(new object[]
            {
                "0%", "5%", "12%", "18%", "28%"
            });

            cmbGstPercent.SelectedIndex = 0;

            cmbGstPercent.SelectedIndexChanged += (s, e) =>
                RecalculatePrices();

            AddField(content, "GST rate", cmbGstPercent);

            lblTaxablePrice = CreatePriceLabel();
            AddField(content, "Selling price before GST", lblTaxablePrice);

            lblGstAmount = CreatePriceLabel();
            AddField(content, "GST amount", lblGstAmount);

            lblSellingPrice = CreatePriceLabel(true);
            AddField(content, "FINAL SELLING PRICE", lblSellingPrice);

            RecalculatePrices();


            AddSection(content, "PHYSICAL STORAGE LOCATION");

            txtRackCode = AddTextField(
                content, "Rack code",
                "Example: R01");

            txtShelfCode = AddTextField(
                content, "Shelf code",
                "Example: S02");

            txtTrayCode = AddTextField(
                content, "Tray code",
                "Example: T05");

            txtBoxCode = AddTextField(
                content, "Box code",
                "Example: B12");

            txtStorageNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 65,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3, 5, 3, 8)
            };

            AddField(content, "Storage notes", txtStorageNotes);

            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 18, 10)
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(105, 38),
                BackColor = Color.White,
                ForeColor = _darkGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor =
                Color.FromArgb(205, 218, 210);

            var btnSave = new Button
            {
                Text = "Save Medicine",
                Size = new Size(145, 38),
                BackColor = _green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(21, 96, 67);

            footer.Controls.Add(btnCancel);
            footer.Controls.Add(btnSave);

            footer.Resize += (s, e) =>
            {
                btnSave.Location = new Point(
                    footer.ClientSize.Width - btnSave.Width - 18, 10);

                btnCancel.Location = new Point(
                    btnSave.Left - btnCancel.Width - 10, 10);
            };

            root.Controls.Add(footer, 0, 2);

            btnSave.Click += (s, e) => SaveMedicine();

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Shown += (s, e) =>
            {
                txtMedicineName.Focus();
            };
        }


        private decimal GetDefaultMargin(string productType)
        {
            switch (productType)
            {
                case "Generic":
                    return 25m;

                case "Company / Branded":
                    return 15m;

                case "Fancy":
                    return 30m;

                case "Food Product":
                    return 15m;

                default:
                    return 15m;
            }
        }

        private decimal GetSelectedGstPercent()
        {
            if (cmbGstPercent == null ||
                cmbGstPercent.SelectedItem == null)
                return 0m;

            string value = cmbGstPercent.SelectedItem.ToString()
                .Replace("%", "").Trim();

            decimal rate;

            return decimal.TryParse(value, out rate)
                ? rate
                : 0m;
        }

        private Label CreatePriceLabel(bool highlight = false)
        {
            return new Label
            {
                Text = "₹0.00",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(
                    "Segoe UI",
                    highlight ? 12F : 10F,
                    highlight ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = highlight ? _green : _darkGreen,
                BackColor = highlight
                    ? Color.FromArgb(231, 246, 239)
                    : Color.White,
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(3, 5, 3, 8)
            };
        }

        private void RecalculatePrices()
        {
            decimal buyingPrice = numPurchasePrice.Value;
            decimal marginPercent = numMarginPercent.Value;
            decimal gstPercent = GetSelectedGstPercent();

            // Step 1: Deduct margin/markup percentage from buying price.
            decimal marginDeduction =
                buyingPrice * marginPercent / 100m;

            decimal priceAfterMargin =
                Math.Max(0m, buyingPrice - marginDeduction);

            // Step 2: Deduct the selected percentage from the remaining price.
            decimal gstDeduction =
                priceAfterMargin * gstPercent / 100m;

            _calculatedTaxablePrice = priceAfterMargin;
            _calculatedGstAmount = gstDeduction;
            _calculatedSellingPrice =
                Math.Max(0m, priceAfterMargin - gstDeduction);

            // Update the popup labels.
            lblTaxablePrice.Text =
                "Price after margin: ₹" +
                _calculatedTaxablePrice.ToString("N2");

            lblGstAmount.Text =
                "GST deduction: ₹" +
                _calculatedGstAmount.ToString("N2");

            lblSellingPrice.Text =
                "Final calculated price: ₹" +
                _calculatedSellingPrice.ToString("N2");
        }

        private TextBox AddTextField(TableLayoutPanel table, string label, string placeholder = "")
        {
            var textbox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3, 5, 3, 8)
            };

            if (!string.IsNullOrWhiteSpace(placeholder))
                textbox.Text = "";

            AddField(table, label, textbox);
            return textbox;
        }

        private void AddField(TableLayoutPanel table, string labelText, Control input)
        {
            int row = table.RowCount;
            table.RowCount++;
            table.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 44));

            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = _darkGreen,
                Margin = new Padding(3, 3, 8, 3)
            };

            table.Controls.Add(label, 0, row);
            table.Controls.Add(input, 1, row);
        }

        private void AddSection(
            TableLayoutPanel table,
            string titleText)
        {
            int row = table.RowCount;
            table.RowCount++;

            table.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38));

            var title = new Label
            {
                Text = titleText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _green,
                BackColor = Color.FromArgb(231, 246, 239),
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(0, 6, 0, 2)
            };

            table.Controls.Add(title, 0, row);
            table.SetColumnSpan(title, 2);
        }

        private void LoadExistingMedicine()
        {
            txtMedicineName.Text = _existing.MedicineName ?? "";
            txtGenericName.Text = _existing.GenericName ?? "";
            txtManufacturer.Text = _existing.Manufacturer ?? "";
            txtBarcode.Text = _existing.Barcode ?? "";
            txtCategory.Text = _existing.Category ?? "";
            txtStrength.Text = _existing.Strength ?? "";

            string unit = string.IsNullOrWhiteSpace(_existing.Unit)
                ? "Strip"
                : _existing.Unit;

            if (!cmbUnit.Items.Contains(unit))
                cmbUnit.Items.Add(unit);

            cmbUnit.Text = unit;

            decimal reorderLevel = _existing.ReorderLevel;
            if (reorderLevel < numReorderLevel.Minimum)
                reorderLevel = numReorderLevel.Minimum;
            if (reorderLevel > numReorderLevel.Maximum)
                reorderLevel = numReorderLevel.Maximum;

            numReorderLevel.Value = reorderLevel;
            chkPrescription.Checked =
                _existing.IsPrescriptionRequired;

            txtRackCode.Text = _existing.RackCode ?? "";
            txtShelfCode.Text = _existing.ShelfCode ?? "";
            txtTrayCode.Text = _existing.TrayCode ?? "";
            txtBoxCode.Text = _existing.BoxCode ?? "";
            txtStorageNotes.Text = _existing.StorageNotes ?? "";

            _loadingPricing = true;

            string productType = string.IsNullOrWhiteSpace(_existing.ProductType)
                ? "Other"
                : _existing.ProductType;

            if (!cmbProductType.Items.Contains(productType))
                cmbProductType.Items.Add(productType);

            cmbProductType.SelectedItem = productType;

            decimal margin = Math.Max(
                numMarginPercent.Minimum,
                Math.Min(numMarginPercent.Maximum, _existing.MarginPercent));

            numMarginPercent.Value = margin;

            numPurchasePrice.Value = Math.Max(
                numPurchasePrice.Minimum,
                Math.Min(numPurchasePrice.Maximum, _existing.PurchasePrice));

            string gstText = _existing.GstPercent.ToString("0.##") + "%";

            if (cmbGstPercent.Items.Contains(gstText))
            {
                cmbGstPercent.SelectedItem = gstText;
            }
            else
            {
                cmbGstPercent.Items.Add(gstText);
                cmbGstPercent.SelectedItem = gstText;
            }

            _loadingPricing = false;
            RecalculatePrices();
        }

        private void SaveMedicine()
        {
            if (string.IsNullOrWhiteSpace(txtMedicineName.Text))
            {
                MessageBox.Show(
                    "Please enter the medicine name.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtMedicineName.Focus();
                return;
            }

            string unit = cmbUnit.Text.Trim();

            if (string.IsNullOrWhiteSpace(unit))
                unit = "Strip";

            MedicineData = new Medicine
            {
                MedicineId = _existing == null
                    ? 0
                    : _existing.MedicineId,

                MedicineName = txtMedicineName.Text.Trim(),
                GenericName = NullIfEmpty(txtGenericName.Text),
                Manufacturer = NullIfEmpty(txtManufacturer.Text),
                Barcode = NullIfEmpty(txtBarcode.Text),
                Category = NullIfEmpty(txtCategory.Text),
                Strength = NullIfEmpty(txtStrength.Text),
                Unit = unit,
                ReorderLevel = (int)numReorderLevel.Value,
                IsPrescriptionRequired = chkPrescription.Checked,

                IsActive = _existing == null
                    ? true
                    : _existing.IsActive,

                CreatedAt = _existing == null
                    ? DateTime.Now
                    : _existing.CreatedAt,

                RackCode = NullIfEmpty(txtRackCode.Text),
                ShelfCode = NullIfEmpty(txtShelfCode.Text),
                TrayCode = NullIfEmpty(txtTrayCode.Text),
                BoxCode = NullIfEmpty(txtBoxCode.Text),
                StorageNotes = NullIfEmpty(txtStorageNotes.Text),

                ProductType = cmbProductType.Text,
                PurchasePrice = numPurchasePrice.Value,
                MarginPercent = numMarginPercent.Value,
                GstPercent = GetSelectedGstPercent(),

                SellingPriceExcludingGst =  _calculatedTaxablePrice,
                GstAmount = _calculatedGstAmount,
                SellingPrice = _calculatedSellingPrice,
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
