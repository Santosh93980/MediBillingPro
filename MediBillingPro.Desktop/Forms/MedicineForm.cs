using MediBillingPro.Application.Services;
using MediBillingPro.Desktop.UI.Common;
using MediBillingPro.Desktop.Forms;
using MediBillingPro.Domain.Entities.Medicine_inventory;
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
    public partial class MedicineForm : Form
    {
        private readonly MedicineService _service;

        private readonly Color _green = MediBillingTheme.Primary;
        private readonly Color _darkGreen = MediBillingTheme.Secondary;
        private readonly Color _lightGreen = MediBillingTheme.PrimaryLight;
        private readonly Color _page = MediBillingTheme.Page;
        private readonly Color _muted = MediBillingTheme.Muted;
        private readonly Color _red = Color.FromArgb(180, 35, 24);

        private TextBox txtSearch;
        private ComboBox cmbCategory;
        private DataGridView dgvMedicines;

        private Label lblTotal;
        private Label lblReorder;
        private Label lblPrescription;
        private Label lblFooter;

        private List<Medicine> _allMedicines = new List<Medicine>();
        private List<Medicine> _visibleMedicines = new List<Medicine>();

        private bool _loadingCategories;

        private Label lblNoData;
        private string _hoverAction = "";
        private int _hoverRowIndex = -1;

        private readonly int? _supplierIdFilter;
        private readonly string _supplierNameFilter;

        private SupplierForm _supplierForm;

        public MedicineForm()
            : this(null, null, null)
        {
        }

        public MedicineForm(int? supplierId, string supplierName, SupplierForm supplierForm)
        {
            InitializeComponent();
            Controls.Clear();

            _supplierIdFilter = supplierId;
            _supplierNameFilter = supplierName;
            _supplierForm = supplierForm;
            string connectionString =
                ConfigurationManager.ConnectionStrings["MediBillingProDB"]
                    ?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'MediBillingProDB' is missing from App.config.");
            }

            var factory = new DatabaseConnectionFactory(connectionString);
            var repository = new MedicineRepository(factory);

            _service = new MedicineService(repository);

            BuildUI();
            LoadMedicines();
        }


        private void BuildUI()
        {
            Text = "MediBilling Pro | Medicine Inventory";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 650);
            BackColor = _page;
            Font = new Font("Segoe UI", 9.5F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = _page,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            Controls.Add(root);

            BuildHeader(root);
            BuildToolbarAndSummary(root);
            BuildMedicineTable(root);
            BuildFooter(root);
        }

        private void BuildHeader(TableLayoutPanel root)
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _green,
                Padding = new Padding(22, 10, 20, 10)
            };

            var logo = new Label
            {
                Text = "✚",
                AutoSize = true,
                Font = new Font("Segoe UI", 27F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(22, 13)
            };

            var title = new Label
            {
                Text = "MediBilling Pro",
                AutoSize = true,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(68, 10)
            };

            var subtitle = new Label
            {
                Text = "PHARMACY MANAGEMENT SYSTEM",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 246, 235),
                Location = new Point(71, 45)
            };

            var section = new Label
            {
                Text = "MEDICINE INVENTORY",
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White
            };

            header.Resize += (s, e) =>
            {
                section.Location = new Point(
                    Math.Max(10, header.ClientSize.Width - section.Width - 24),
                    28);
            };

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(section);

            root.Controls.Add(header, 0, 0);
        }

        private void BuildToolbarAndSummary(TableLayoutPanel root)
        {
            var area = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _page,
                Padding = new Padding(20, 10, 20, 8)
            };

            var heading = new Label
            {
                Text = _supplierIdFilter.HasValue ? "Medicines - " + _supplierNameFilter : "Medicine Inventory",
                AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = _darkGreen,
                Location = new Point(20, 7)
            };

            var description = new Label
            {
                Text = _supplierIdFilter.HasValue ? "Manage medicines supplied by " + _supplierNameFilter : "Search and manage your pharmacy products",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = _muted,
                Location = new Point(22, 34)
            };

            var toolbar = new FlowLayoutPanel
            {
                Location = new Point(20, 58),
                Height = 39,
                Width = 900,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                BackColor = _page,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };


            if (_supplierIdFilter.HasValue)
            {
                var btnBack = CreateButton(
                    "← Back to Suppliers",
                    Color.FromArgb(108, 117, 125),
                    150);

                btnBack.Click += (s, e) =>
                {
                    _supplierForm?.RefreshSupplierList();
                    Close();
                };

                toolbar.Controls.Add(btnBack);
            }


            txtSearch = new TextBox
            {
                Width = 280,
                Height = 32,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 8, 0)
            };

            txtSearch.TextChanged += (s, e) => ApplyFilters();

            var btnSearch = CreateButton("⌕  Search", _darkGreen, 100);
            btnSearch.Click += (s, e) => ApplyFilters();

            cmbCategory = new ComboBox
            {
                Width = 155,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 2, 8, 0)
            };

            cmbCategory.SelectedIndexChanged += (s, e) =>
            {
                if (!_loadingCategories)
                    ApplyFilters();
            };

            var btnAdd = UiStyle.CreatePrimaryButton("+ Add Medicine", 145);
            btnAdd.Click += (s, e) => AddMedicine();

            var btnRefresh = CreateButton("↻  Refresh", Color.FromArgb(77, 96, 87), 100);
            btnRefresh.Click += (s, e) =>
            {
                txtSearch.Clear();
                LoadMedicines();
            };

            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(btnSearch);
            toolbar.Controls.Add(cmbCategory);
            toolbar.Controls.Add(btnAdd);
            toolbar.Controls.Add(btnRefresh);

            var cards = new TableLayoutPanel
            {
                Location = new Point(20, 103),
                Height = 38,
                Width = 850,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = _page,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            lblTotal = CreateSummaryLabel("Total medicines: 0");
            lblReorder = CreateSummaryLabel("At reorder level: 0");
            lblPrescription = CreateSummaryLabel("Prescription items: 0");

            cards.Controls.Add(lblTotal, 0, 0);
            cards.Controls.Add(lblReorder, 1, 0);
            cards.Controls.Add(lblPrescription, 2, 0);

            area.Resize += (s, e) =>
            {
                toolbar.Width = Math.Max(300, area.ClientSize.Width - 40);
                cards.Width = Math.Max(300, area.ClientSize.Width - 40);
            };

            area.Controls.Add(heading);
            area.Controls.Add(description);
            area.Controls.Add(toolbar);
            area.Controls.Add(cards);

            root.Controls.Add(area, 0, 1);
        }

        private Label CreateSummaryLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _darkGreen,
                BackColor = Color.White,
                Padding = new Padding(10, 0, 5, 0),
                Margin = new Padding(0, 0, 8, 0)
            };
        }

        private void BuildMedicineTable(TableLayoutPanel root)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _page,
                Padding = new Padding(20, 0, 20, 8)
            };

            dgvMedicines = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(228, 235, 231),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 43,
                RowTemplate = { Height = 39 },
                Font = new Font("Segoe UI", 9F)
            };


            dgvMedicines.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(230, 243, 236);
            dgvMedicines.ColumnHeadersDefaultCellStyle.ForeColor = _darkGreen;
            dgvMedicines.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvMedicines.ColumnHeadersDefaultCellStyle.SelectionBackColor =
                Color.FromArgb(230, 243, 236);

            dgvMedicines.DefaultCellStyle.BackColor = Color.White;
            dgvMedicines.DefaultCellStyle.ForeColor = _darkGreen;
            dgvMedicines.DefaultCellStyle.SelectionBackColor = _lightGreen;
            dgvMedicines.DefaultCellStyle.SelectionForeColor = _darkGreen;
            dgvMedicines.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(249, 252, 250);
            dgvMedicines.CellBorderStyle =
                DataGridViewCellBorderStyle.SingleHorizontal;

            AddTextColumn("MedicineId", "ID", 45, false);
            AddTextColumn("MedicineName", "MEDICINE NAME", 140, true);
            AddTextColumn("ProductType", "PRODUCT TYPE", 105, true);
            AddTextColumn("GenericName", "GENERIC NAME", 110, true);
            AddTextColumn("Manufacturer", "MANUFACTURER", 100, true);
            AddTextColumn("Barcode", "BARCODE", 95, true);
            AddTextColumn("Category", "CATEGORY", 85, true);
            AddTextColumn("Strength", "STRENGTH", 70, true);
            AddTextColumn("Unit", "UNIT", 60, true);


            var storageViewColumn = new DataGridViewButtonColumn
            {
                Name = "StorageLocation",
                HeaderText = "Storage",
                Text = "View",
                UseColumnTextForButtonValue = true,
                Width = 85,
                FlatStyle = FlatStyle.Flat
            };

            dgvMedicines.Columns.Add(storageViewColumn);

            var priceViewColumn = new DataGridViewButtonColumn
            {
                Name = "PriceView",
                HeaderText = "Price",
                Text = "View",
                UseColumnTextForButtonValue = true,
                Width = 75,
                FlatStyle = FlatStyle.Flat
            };

            dgvMedicines.Columns.Add(priceViewColumn);



            AddTextColumn("ReorderLevel", "REORDER LEVEL", 80, true);
            AddTextColumn("Prescription", "PRESCRIPTION", 85, true);

            dgvMedicines.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Actions",
                    HeaderText = "ACTIONS",
                    ReadOnly = true,
                    FillWeight = 155,
                    MinimumWidth = 270,
                    FillWeight = 270,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });


            lblNoData = new Label
            {
                Text = "No medicines found",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = MediBillingTheme.Muted,
                BackColor = Color.White,
                Visible = false
            };

            dgvMedicines.CellPainting += DgvMedicines_CellPainting;
            dgvMedicines.CellMouseClick += DgvMedicines_CellMouseClick;
            dgvMedicines.CellMouseMove += DgvMedicines_CellMouseMove;
            dgvMedicines.CellMouseLeave += DgvMedicines_CellMouseLeave;

            dgvMedicines.CellContentClick += DgvMedicines_ViewDetailsClick;


            host.Controls.Add(dgvMedicines);
            host.Controls.Add(lblNoData);
            lblNoData.BringToFront();
            root.Controls.Add(host, 0, 2);
        }

        private void DgvMedicines_ViewDetailsClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            string columnName =
                dgvMedicines.Columns[e.ColumnIndex].Name;

            if (columnName != "StorageLocation" &&
                columnName != "PriceView")
                return;

            int medicineId = Convert.ToInt32(
                dgvMedicines.Rows[e.RowIndex]
                    .Cells["MedicineId"].Value);

            Medicine medicine = _visibleMedicines.Find(
                m => m.MedicineId == medicineId);

            if (medicine == null)
                return;

            if (columnName == "StorageLocation")
            {
                using (var popup = new StorageLocationForm(medicine))
                {
                    popup.ShowDialog(this);
                }
            }
            else if (columnName == "PriceView")
            {
                using (var popup = new PriceDetailsForm(medicine))
                {
                    popup.ShowDialog(this);
                }
            }
        }

        private void AddTextColumn(
            string name, string heading, float weight, bool visible)
        {
            dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = heading,
                DataPropertyName = name,
                FillWeight = weight,
                Visible = visible,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        private void BuildFooter(TableLayoutPanel root)
        {
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };

            lblFooter = new Label
            {
                Text = "Total Medicines: 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = _darkGreen,
                Location = new Point(20, 11)
            };

            var footerRight = new Label
            {
                Text = "MediBilling Pro  |  Pharmacy Inventory",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = _muted,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            footer.Resize += (s, e) =>
            {
                footerRight.Location = new Point(
                    Math.Max(10, footer.ClientSize.Width - footerRight.Width - 20),
                    12);
            };

            footer.Controls.Add(lblFooter);
            footer.Controls.Add(footerRight);

            root.Controls.Add(footer, 0, 3);
        }

        private Button CreateButton(string text, Color backColor, int width)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 2, 8, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor =
                ControlPaint.Dark(backColor, 0.08F);

            return button;
        }


        private void LoadMedicines()
        {
            try
            {
                if (_supplierIdFilter.HasValue)
                {
                    _allMedicines = _service.GetAll(
                        _supplierIdFilter.Value) ?? new List<Medicine>();
                }
                else
                {
                    // Load all medicines when no supplier is selected.
                    _allMedicines = _service.GetAll() ?? new List<Medicine>();
                }

                PopulateCategories();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Unable to load medicines.\n\n" + ex.Message,
                    "Medicine Inventory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void PopulateCategories()
        {
            string previous = cmbCategory.SelectedItem == null
                ? "All categories"
                : cmbCategory.SelectedItem.ToString();

            _loadingCategories = true;
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("All categories");

            foreach (string category in _allMedicines
                .Select(m => m.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c))
            {
                cmbCategory.Items.Add(category);
            }

            int index = cmbCategory.Items.IndexOf(previous);
            cmbCategory.SelectedIndex = index >= 0 ? index : 0;
            _loadingCategories = false;
        }

        private void ApplyFilters()
        {
            if (dgvMedicines == null)
                return;

            string search = txtSearch == null
                ? ""
                : txtSearch.Text.Trim();

            string category = cmbCategory == null ||
                             cmbCategory.SelectedItem == null
                ? "All categories"
                : cmbCategory.SelectedItem.ToString();

            IEnumerable<Medicine> query = _allMedicines;

            if (_supplierIdFilter.HasValue && _supplierIdFilter.Value > 0)
            {
                query = query.Where(m =>
                    m.SupplierId == _supplierIdFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    Contains(m.MedicineName, search) ||
                    Contains(m.ProductType, search) ||
                    Contains(m.GenericName, search) ||
                    Contains(m.Manufacturer, search) ||
                    Contains(m.Barcode, search) ||
                    Contains(m.Category, search) ||
                    Contains(m.Strength, search) ||
                    Contains(m.RackCode, search) ||
                    Contains(m.ShelfCode, search) ||
                    Contains(m.TrayCode, search) ||
                    Contains(m.BoxCode, search));
            }

            if (!string.Equals(category, "All categories",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m =>
                    string.Equals(m.Category, category,
                        StringComparison.OrdinalIgnoreCase));
            }

            _visibleMedicines = query.ToList();

            dgvMedicines.Rows.Clear();

            foreach (Medicine medicine in _visibleMedicines)
            {

                //string storageLocation = FormatStorageLocation(medicine);

                int rowIndex = dgvMedicines.Rows.Add(
                    medicine.MedicineId,                              // 1. MedicineId
                    medicine.MedicineName,                            // 2. MedicineName
                    medicine.ProductType,                             // 3. ProductType
                    medicine.GenericName,                             // 4. GenericName
                    medicine.Manufacturer,                            // 5. Manufacturer
                    medicine.Barcode,                                 // 6. Barcode
                    medicine.Category,                                // 7. Category
                    medicine.Strength,                                // 8. Strength
                    medicine.Unit,                                    // 9. Unit
                    "View",                                           // 10. StorageLocation
                    "View",                                           // 11. PriceView
                    medicine.ReorderLevel,                            // 12. ReorderLevel
                    medicine.IsPrescriptionRequired ? "Yes" : "No",   // 13. Prescription
                    ""                                                // 14. Actions
                );


                if (medicine.ReorderLevel > 0)
                {
                    dgvMedicines.Rows[rowIndex]
                        .Cells["ReorderLevel"].Style.ForeColor =
                        Color.FromArgb(169, 101, 13);
                }
            }

            bool hasResults = _visibleMedicines.Count > 0;

            dgvMedicines.Visible = hasResults;
            lblNoData.Visible = !hasResults;

            int total = _allMedicines.Count;
            int reorderCount = _allMedicines.Count(m => m.ReorderLevel > 0);
            int prescriptionCount =
                _allMedicines.Count(m => m.IsPrescriptionRequired);

            lblTotal.Text = "Total medicines: " + total;
            lblReorder.Text = "Reorder level set: " + reorderCount;
            lblPrescription.Text = "Prescription items: " + prescriptionCount;

            lblFooter.Text = "Showing " + _visibleMedicines.Count +
                " of " + total + " medicines";
        }

        private bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddMedicine()
        {
            using (var dialog = new MedicineEditorForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    Medicine medicine = dialog.MedicineData;

                    // Assign the selected supplier when opened from Supplier Management.
                    if (_supplierIdFilter.HasValue)
                    {
                        medicine.SupplierId = _supplierIdFilter.Value;
                    }

                    if (string.IsNullOrWhiteSpace(medicine.Barcode))
                    {
                        medicine.Barcode = "MED" +
                            Guid.NewGuid().ToString("N")
                                .Substring(0, 12).ToUpperInvariant();
                    }

                    int newId = _service.Add(medicine);
                    LoadMedicines();
                    _supplierForm?.RefreshSupplierList();
                    MessageBox.Show(
                        "Medicine saved successfully.\n\nMedicine: " +
                        medicine.MedicineName + "\nBarcode: " + medicine.Barcode,
                        "Saved Successfully",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to save medicine.\n\n" + ex.Message,
                        "Save Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void EditMedicine(Medicine medicine)
        {
            using (var dialog = new MedicineEditorForm(medicine))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    Medicine updated = dialog.MedicineData;

                    if (_supplierIdFilter.HasValue)
                    {
                        updated.SupplierId = _supplierIdFilter.Value;
                    }

                    if (string.IsNullOrWhiteSpace(updated.Barcode))
                    {
                        updated.Barcode = "MED" +
                            Guid.NewGuid().ToString("N")
                                .Substring(0, 12).ToUpperInvariant();
                    }

                    bool success = _service.Update(updated);
                    LoadMedicines();

                    MessageBox.Show(
                        success
                            ? "Medicine updated successfully."
                            : "No active medicine was updated. The list has been refreshed.",
                        success ? "Updated" : "Update Not Completed",
                        MessageBoxButtons.OK,
                        success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to update medicine.\n\n" + ex.Message,
                        "Update Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteMedicine(Medicine medicine)
        {
            DialogResult answer = MessageBox.Show(
                "Remove this medicine from the active inventory?\n\n" +
                medicine.MedicineName + "\nMedicine ID: " +
                medicine.MedicineId +
                "\n\nHistorical sales and purchase records will be preserved.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes)
                return;

            try
            {
                bool deleted = _service.Delete(medicine.MedicineId);

                // Reload from SQL Server, not just from the displayed grid.
                _allMedicines = _supplierIdFilter.HasValue ? (_service.GetAll(_supplierIdFilter.Value) ?? new List<Medicine>()) : (_service.GetAll() ?? new List<Medicine>());
                PopulateCategories();
                ApplyFilters();

                MessageBox.Show(
                    deleted
                        ? "Medicine removed from active inventory."
                        : "No active record was changed. The list has been refreshed.",
                    deleted ? "Delete Successful" : "No Changes Made",
                    MessageBoxButtons.OK,
                    deleted ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                try
                {
                    _allMedicines = _supplierIdFilter.HasValue ? (_service.GetAll(_supplierIdFilter.Value) ?? new List<Medicine>()) : (_service.GetAll() ?? new List<Medicine>());
                    PopulateCategories();
                    ApplyFilters();
                }
                catch
                {
                    // Preserve the original database error for display.
                }

                MessageBox.Show(
                    "The delete operation could not be confirmed.\n\n" + ex.Message,
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void DgvMedicines_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvMedicines.Columns[e.ColumnIndex].Name != "Actions") return;
            e.PaintBackground(e.CellBounds, true);
            string[] names = { "View", "Edit", "Delete", "Print" };
            Color[] colors = { Color.FromArgb(51, 95, 128), MediBillingTheme.Primary, Color.FromArgb(185, 45, 45), Color.FromArgb(77, 96, 87) };
            int pad = 5, gap = 4, bw = Math.Max(42, (e.CellBounds.Width - pad * 2 - gap * 3) / 4), bh = 26;
            int y = e.CellBounds.Y + (e.CellBounds.Height - bh) / 2;
            using (var font = new Font("Segoe UI", 8F, FontStyle.Bold))
            {
                for (int i = 0; i < names.Length; i++)
                {
                    int x = e.CellBounds.X + pad + i * (bw + gap);
                    var rect = new Rectangle(x, y, bw, bh);
                    bool hovered = _hoverRowIndex == e.RowIndex && _hoverAction == names[i];
                    using (var brush = new SolidBrush(hovered ? ControlPaint.Dark(colors[i], 0.12F) : colors[i]))
                        e.Graphics.FillRectangle(brush, rect);
                    TextRenderer.DrawText(e.Graphics, names[i], font, rect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
            }
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            e.Handled = true;
        }

        private void DgvMedicines_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvMedicines.Columns[e.ColumnIndex].Name != "Actions")
            {
                _hoverAction = ""; _hoverRowIndex = -1; dgvMedicines.Cursor = Cursors.Default; return;
            }
            int pad = 5, gap = 4, bw = Math.Max(42, (dgvMedicines.Columns[e.ColumnIndex].Width - pad * 2 - gap * 3) / 4);
            int index = (e.X - pad) / Math.Max(1, bw + gap);
            string[] names = { "View", "Edit", "Delete", "Print" };
            string action = e.X < pad || index < 0 || index >= names.Length ? "" : names[index];
            if (_hoverAction != action || _hoverRowIndex != e.RowIndex)
            {
                _hoverAction = action; _hoverRowIndex = e.RowIndex;
                dgvMedicines.Cursor = string.IsNullOrEmpty(action) ? Cursors.Default : Cursors.Hand;
                dgvMedicines.InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
        }

        private void DgvMedicines_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            _hoverAction = ""; _hoverRowIndex = -1;
            if (dgvMedicines != null) { dgvMedicines.Cursor = Cursors.Default; dgvMedicines.Invalidate(); }
        }

        private void DgvMedicines_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvMedicines.Columns[e.ColumnIndex].Name != "Actions") return;
            object idValue = dgvMedicines.Rows[e.RowIndex].Cells["MedicineId"].Value;
            if (idValue == null || idValue == DBNull.Value) return;
            int id = Convert.ToInt32(idValue);
            Medicine selected = _visibleMedicines.FirstOrDefault(m => m.MedicineId == id);
            if (selected == null) return;
            int pad = 5, gap = 4, bw = Math.Max(42, (dgvMedicines.Columns[e.ColumnIndex].Width - pad * 2 - gap * 3) / 4);
            int actionIndex = (e.X - pad) / Math.Max(1, bw + gap);
            if (e.X < pad || actionIndex < 0 || actionIndex > 3) return;
            switch (actionIndex)
            {
                case 0: ViewMedicine(selected); break;
                case 1: EditMedicine(selected); break;
                case 2: DeleteMedicine(selected); break;
                case 3: PrintMedicine(selected); break;
            }
        }

        private void ViewMedicine(Medicine medicine)
        {
            string details = "Medicine: " + medicine.MedicineName +
                "\nProduct type: " + medicine.ProductType +
                "\nGeneric name: " + medicine.GenericName +
                "\nManufacturer: " + medicine.Manufacturer +
                "\nBarcode: " + medicine.Barcode +
                "\nCategory: " + medicine.Category +
                "\nStrength / Unit: " + medicine.Strength + " / " + medicine.Unit +
                "\nReorder level: " + medicine.ReorderLevel +
                "\nPrescription required: " + (medicine.IsPrescriptionRequired ? "Yes" : "No") +
                "\nStorage: " + medicine.RackCode + " / " + medicine.ShelfCode + " / " + medicine.TrayCode + " / " + medicine.BoxCode;
            MessageBox.Show(this, details, "Medicine Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PrintMedicine(Medicine medicine)
        {
            var dt = new DataTable("Medicine");
            dt.Columns.Add("Field"); dt.Columns.Add("Value");
            dt.Rows.Add("Medicine ID", medicine.MedicineId.ToString());
            dt.Rows.Add("Medicine Name", medicine.MedicineName ?? "");
            dt.Rows.Add("Product Type", medicine.ProductType ?? "");
            dt.Rows.Add("Generic Name", medicine.GenericName ?? "");
            dt.Rows.Add("Manufacturer", medicine.Manufacturer ?? "");
            dt.Rows.Add("Barcode", medicine.Barcode ?? "");
            dt.Rows.Add("Category", medicine.Category ?? "");
            dt.Rows.Add("Strength", medicine.Strength ?? "");
            dt.Rows.Add("Unit", medicine.Unit ?? "");
            dt.Rows.Add("Reorder Level", medicine.ReorderLevel.ToString());
            dt.Rows.Add("Prescription Required", medicine.IsPrescriptionRequired ? "Yes" : "No");
            var reportGrid = new DataGridView { DataSource = dt, AutoGenerateColumns = true };
            ReportOutput.Show(this, reportGrid, "Medicine Record");
        }

    }
}
