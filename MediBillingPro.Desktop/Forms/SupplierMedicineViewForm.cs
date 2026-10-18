using MediBillingPro.Application.Services;
using MediBillingPro.Infrastructure.Data;
using MediBillingPro.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    public partial class SupplierMedicineViewForm : Form
    {
        private readonly int _supplierId;
        private readonly string _supplierName;
        private readonly MedicineService _service;

        private readonly Color darkGreen =
            Color.FromArgb(21, 60, 49);

        private readonly Color page =
            Color.FromArgb(244, 248, 246);

        private readonly Color muted =
            Color.FromArgb(91, 111, 101);

        private DataGridView grid;
        private Label lblCount;
        private Label lblNoData;

        public SupplierMedicineViewForm(
            int supplierId,
            string supplierName)
        {
            if (supplierId <= 0)
                throw new ArgumentException(
                    "A valid supplier ID is required.",
                    nameof(supplierId));

            _supplierId = supplierId;
            _supplierName = supplierName ?? "Supplier";

            string connectionString =
                ConfigurationManager
                    .ConnectionStrings["MediBillingProDB"]
                    ?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'MediBillingProDB' is missing.");

            var factory =
                new DatabaseConnectionFactory(connectionString);

            _service = new MedicineService(
                new MedicineRepository(factory));

            BuildUI();
            LoadMedicines();
        }

        private void BuildUI()
        {
            Text = "Supplier Medicines | " + _supplierName;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1150, 650);
            MinimumSize = new Size(900, 500);
            BackColor = page;
            Font = new Font("Segoe UI", 9.5F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = page,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 48));

            Controls.Add(root);

            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = darkGreen,
                Padding = new Padding(24, 8, 24, 8)
            };

            var heading = new Label
            {
                Text = _supplierName + " - Medicines",
                Dock = DockStyle.Top,
                Height = 34,
                ForeColor = Color.White,
                Font = new Font(
                    "Segoe UI", 17, FontStyle.Bold)
            };

            var subtitle = new Label
            {
                Text = "View medicines linked to this supplier",
                Dock = DockStyle.Bottom,
                Height = 24,
                ForeColor = Color.FromArgb(220, 238, 229)
            };

            header.Controls.Add(subtitle);
            header.Controls.Add(heading);
            root.Controls.Add(header, 0, 0);

            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 8),
                BackColor = page
            };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect
            };

            MediBillingPro.Desktop.UI.Common.UiStyle
                .StyleGrid(grid);

            AddColumn("MedicineName", "Medicine Name", 180);
            AddColumn("GenericName", "Generic Name", 140);
            AddColumn("Manufacturer", "Manufacturer", 130);
            AddColumn("Barcode", "Barcode", 100);
            AddColumn("Category", "Category", 100);
            AddColumn("Strength", "Strength", 75);
            AddColumn("Unit", "Unit", 65);
            AddColumn("SellingPrice", "Selling Price", 90);
            AddColumn("ReorderLevel", "Reorder Level", 85);

            host.Controls.Add(grid);

            lblNoData = new Label
            {
                Text = "No medicines found for this supplier.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = muted,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 12),
                Visible = false
            };

            host.Controls.Add(lblNoData);
            lblNoData.BringToFront();

            root.Controls.Add(host, 0, 1);

            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 5, 24, 5),
                BackColor = page
            };

            lblCount = new Label
            {
                Dock = DockStyle.Left,
                Width = 300,
                ForeColor = muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnClose =
                MediBillingPro.Desktop.UI.Common.UiStyle
                    .CreateSecondaryButton("Close", 100);

            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => Close();

            footer.Controls.Add(btnClose);
            footer.Controls.Add(lblCount);

            root.Controls.Add(footer, 0, 2);
        }

        private void AddColumn(
            string property, string header, float fillWeight)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = property,
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = fillWeight,
                SortMode =
                    DataGridViewColumnSortMode.Automatic
            });
        }

        private void LoadMedicines()
        {
            try
            {
                var medicines = _service.GetAll(_supplierId);

                grid.DataSource = null;
                grid.DataSource = medicines;

                lblNoData.Visible =
                    medicines == null || medicines.Count == 0;

                lblNoData.BringToFront();
                grid.Visible =
                    medicines != null && medicines.Count > 0;

                lblCount.Text =
                    "Total medicines: " +
                    (medicines?.Count ?? 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Unable to load supplier medicines.\n\n" +
                    ex.Message,
                    "Supplier Medicines",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
