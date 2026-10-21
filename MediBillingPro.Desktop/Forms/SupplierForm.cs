using MediBillingPro.Application.Services;
using MediBillingPro.Domain.Entities.Contacts;
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
using MediBillingPro.Desktop.UI.Common;

namespace MediBillingPro.Desktop.Forms
{
    public partial class SupplierForm : Form
    {
        private readonly SupplierService _service;
        private readonly Color green = UiStyle.Primary;
        private readonly Color darkGreen = UiStyle.DarkGreen;
        private readonly Color page = UiStyle.PageBackground;
        private readonly Color muted = UiStyle.Muted;
        private readonly Color red = UiStyle.Danger;

        private TextBox txtSearch;
        private DataGridView grid;
        private Label lblCount;
        private Label lblNoData;
        private List<Supplier> allSuppliers = new List<Supplier>();

        private int currentPage = 1;
        private int pageSize = 10;

        private Label lblPageInfo;
        private Button btnPrevious;
        private Button btnNext;
        private ComboBox cmbPageSize;

        public SupplierForm()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MediBillingProDB"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'MediBillingProDB' is missing from App.config.");
            }

            var factory = new DatabaseConnectionFactory(connectionString);
            
            _service = new SupplierService(new SupplierRepository(factory));

            BuildUI();
            LoadSuppliers();
        }

        private void BuildUI()
        {
            Text = "MediBilling Pro | Supplier Management";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 620);
            BackColor = page;
            Font = new Font("Segoe UI", 9.5F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = page,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Fill, BackColor = darkGreen, Padding = new Padding(26, 10, 26, 10) };
            var heading = new Label
            {
                Text = "Supplier Management",
                Dock = DockStyle.Left,
                Width = 420,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var subheading = new Label
            {
                Text = "Manage supplier details and contact information",
                Dock = DockStyle.Right,
                Width = 390,
                ForeColor = Color.FromArgb(220, 238, 229),
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(subheading);
            header.Controls.Add(heading);
            root.Controls.Add(header, 0, 0);

            var toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 12), BackColor = page };
            txtSearch = new TextBox
            {

                Font = new Font("Segoe UI", 10),
                Width = 360,
                Location = new Point(24, 24),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            txtSearch.TextChanged += (s, e) =>
            {
                currentPage = 1;
                ApplyFilter();
            };
            var btnAdd = UiStyle.CreatePrimaryButton("+ Add Supplier", 145);
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.Location = new Point(Width - 205, 20);
            btnAdd.Click += (s, e) => AddSupplier();
            toolbar.Resize += (s, e) => btnAdd.Location = new Point(toolbar.ClientSize.Width - btnAdd.Width - 24, 20);
            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(btnAdd);
            root.Controls.Add(toolbar, 0, 1);

            var tableHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 8), BackColor = page };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 42 },
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 237, 233)
            };

            UiStyle.StyleGrid(grid);

            //AddTextColumn("SupplierId", "ID", 55);
            AddTextColumn("SupplierName", "Supplier Name", 170);
            AddTextColumn("ContactPerson", "Contact Person", 130);
            AddTextColumn("MobileNumber", "Mobile Number", 115);
            AddTextColumn("Email", "Email", 170);
            AddTextColumn("GSTIN", "GSTIN", 115);
            AddTextColumn("LicenseNumber", "License Number", 120);
            AddTextColumn("Address", "Address", 190);
            // One Action column with two colored buttons.
            AddTextColumn("MedicineCount", "Medicines", 75);
            grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "ViewMedicines",
                HeaderText = "View",
                Text = "👁",
                UseColumnTextForButtonValue = true,
                FillWeight = 55,
                FlatStyle = FlatStyle.Flat
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Actions",
                HeaderText = "Action",
                DataPropertyName = "Actions",
                FillWeight = 135,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SupplierId",
                DataPropertyName = "SupplierId",
                Visible = false
            });

            grid.CellPainting += GridCellPainting;
            grid.CellMouseClick += GridCellMouseClick;
            tableHost.Controls.Add(grid);

            lblNoData = new Label
            {
                Text = "No suppliers found. Click '+ Add Supplier' to create one.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = muted,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 12)
            };
            tableHost.Controls.Add(lblNoData);
            lblNoData.BringToFront();
            root.Controls.Add(tableHost, 0, 2);

            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 5, 24, 5),
                BackColor = page
            };

            lblCount = new Label { Dock = DockStyle.Left, Width = 400, ForeColor = muted, TextAlign = ContentAlignment.MiddleLeft };
            var footerText = new Label { Text = "MediBilling Pro  |  Supplier Directory", Dock = DockStyle.Right, Width = 300, ForeColor = muted, TextAlign = ContentAlignment.MiddleRight };
                       

            lblCount = new Label
            {
                Dock = DockStyle.Left,
                Width = 250,
                ForeColor = muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnPrevious = UiStyle.CreateSecondaryButton("Previous", 85);
            btnPrevious.Height = 30;
            btnPrevious.Click += (s, e) =>
            {
                if (currentPage > 1)
                {
                    currentPage--;
                    ApplyFilter();
                }
            };

            btnNext = UiStyle.CreateSecondaryButton("Next", 75);
            btnNext.Height = 30;
            btnNext.Click += (s, e) =>
            {
                currentPage++;
                ApplyFilter();
            };

            lblPageInfo = new Label
            {
                Width = 100,
                Height = 30,
                ForeColor = darkGreen,
                TextAlign = ContentAlignment.MiddleCenter
            };

            cmbPageSize = new ComboBox
            {
                Width = 65,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };

            cmbPageSize.Items.AddRange(new object[] { 10, 25, 50, 100 });
            cmbPageSize.SelectedItem = 10;

            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPageSize.SelectedItem != null)
                {
                    pageSize = Convert.ToInt32(cmbPageSize.SelectedItem);
                    currentPage = 1;
                    ApplyFilter();
                }
            };

            var lblPageSize = new Label
            {
                Text = "Rows:",
                Width = 45,
                Height = 30,
                ForeColor = muted,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pagination = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 390,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = page
            };

            
            pagination.Controls.Add(lblPageSize);
            pagination.Controls.Add(cmbPageSize);
            pagination.Controls.Add(btnPrevious);
            pagination.Controls.Add(lblPageInfo);
            pagination.Controls.Add(btnNext);

            footer.Controls.Add(pagination);

            footer.Controls.Add(footerText);
            
            footer.Controls.Add(lblCount);

            root.Controls.Add(footer, 0, 3);            
        }


        private void GridCellPainting(
            object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 ||
                grid.Columns[e.ColumnIndex].Name != "Actions")
                return;

            e.PaintBackground(e.CellBounds, true);

            int padding = 5;
            int gap = 6;
            int buttonHeight = 27;
            int buttonWidth = (e.CellBounds.Width - padding * 2 - gap) / 2;

            int y = e.CellBounds.Y +
                (e.CellBounds.Height - buttonHeight) / 2;

            Rectangle editRect = new Rectangle(
                e.CellBounds.X + padding, y,
                buttonWidth, buttonHeight);

            Rectangle deactivateRect = new Rectangle(
                editRect.Right + gap, y,
                buttonWidth, buttonHeight);

            using (var editBrush = new SolidBrush(UiStyle.Secondary))
            using (var deactivateBrush = new SolidBrush(UiStyle.Danger))
            using (var textBrush = new SolidBrush(Color.White))
            using (var font = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            {
                e.Graphics.FillRectangle(editBrush, editRect);
                e.Graphics.FillRectangle(deactivateBrush, deactivateRect);

                TextRenderer.DrawText(
                    e.Graphics, "Edit", font, editRect, Color.White,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(
                    e.Graphics, "Deactivate", font, deactivateRect,
                    Color.White,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter);
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            e.Handled = true;
        }


        private void GridCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            object idValue = grid.Rows[e.RowIndex]
                .Cells["SupplierId"].Value;

            if (idValue == null || idValue == DBNull.Value)
                return;

            int id = Convert.ToInt32(idValue);

            Supplier supplier = allSuppliers.FirstOrDefault(
                s => s.SupplierId == id);

            if (supplier == null)
                return;

            string columnName = grid.Columns[e.ColumnIndex].Name;

            if (columnName == "SupplierName")
            {
                OpenSupplierMedicines(supplier);
                return;
            }

            if (columnName == "ViewMedicines")
            {
                using (var form = new SupplierMedicineViewForm(
                    supplier.SupplierId,
                    supplier.SupplierName))
                {
                    form.ShowDialog(this);
                }

                return;
            }

            // Keep your existing Actions handling below.
            if (columnName == "Actions")
            {
                int buttonWidth =
                    (grid.Columns[e.ColumnIndex].Width - 16) / 2;

                if (e.X < 5 + buttonWidth)
                    EditSupplier(supplier);
                else
                    DeactivateSupplier(supplier);
            }
        }


        private void AddTextColumn(string name, string header, float fill)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = name,
                FillWeight = fill,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        public void RefreshSupplierList()
        {
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            try
            {
                allSuppliers = _service.GetAll() ?? new List<Supplier>();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to load suppliers.\n\n" + ex.Message,
                    "Supplier Management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ApplyFilter()
        {
            if (grid == null) return;

            string search = (txtSearch?.Text ?? "").Trim();

            List<Supplier> filtered = string.IsNullOrWhiteSpace(search)
                ? allSuppliers
                : allSuppliers.Where(s =>
                    Has(s.SupplierName, search) ||
                    Has(s.MobileNumber, search))
                  .ToList();

            int totalRecords = filtered.Count;

            int totalPages = Math.Max(
                1, (int)Math.Ceiling(totalRecords / (double)pageSize));

            // Keep the current page valid after filtering or deletion.
            if (currentPage > totalPages)
                currentPage = totalPages;

            if (currentPage < 1)
                currentPage = 1;

            List<Supplier> pageRecords = filtered
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            grid.DataSource = null;

            grid.DataSource = pageRecords.Select(s => new
            {
                s.SupplierId,
                s.SupplierName,
                s.ContactPerson,
                s.MobileNumber,
                s.Email,
                s.GSTIN,
                s.LicenseNumber,
                s.Address,
                s.MedicineCount
            }).ToList();

            lblNoData.Visible = totalRecords == 0;
            lblNoData.BringToFront();
            grid.Visible = totalRecords > 0;

            lblCount.Text = totalRecords == 0
                ? "No suppliers found"
                : "Showing " +
                  ((currentPage - 1) * pageSize + 1) +
                  "-" +
                  Math.Min(currentPage * pageSize, totalRecords) +
                  " of " + totalRecords + " suppliers";

            lblPageInfo.Text = currentPage + " / " + totalPages;

            btnPrevious.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;

            btnPrevious.BackColor = currentPage > 1
                ? UiStyle.Secondary
                : Color.LightGray;

            btnNext.BackColor = currentPage < totalPages
                ? UiStyle.Primary
                : Color.LightGray;
        }


        private static bool Has(string value, string search)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddSupplier()
        {
            using (var dialog = new SupplierEditorForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    int id = _service.Add(dialog.SupplierData);
                    LoadSuppliers();
                    MessageBox.Show(this, "Supplier saved successfully.\nSupplier : " + dialog.SupplierData.SupplierName,
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Unable to save supplier.\n\n" + ex.Message,
                        "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void GridCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            int id = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["SupplierId"].Value);
            Supplier supplier = allSuppliers.FirstOrDefault(s => s.SupplierId == id);
            if (supplier == null) return;

            if (grid.Columns[e.ColumnIndex].Name == "EditAction")
                EditSupplier(supplier);
            else if (grid.Columns[e.ColumnIndex].Name == "DeleteAction")
                DeactivateSupplier(supplier);
        }

        private void EditSupplier(Supplier supplier)
        {
            using (var dialog = new SupplierEditorForm(supplier))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    bool updated = _service.Update(dialog.SupplierData);
                    LoadSuppliers();
                    MessageBox.Show(this, updated ? "Supplier updated successfully." : "No active supplier was updated.",
                        updated ? "Updated" : "No Changes", MessageBoxButtons.OK,
                        updated ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Unable to update supplier.\n\n" + ex.Message,
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeactivateSupplier(Supplier supplier)
        {
            DialogResult answer = MessageBox.Show(this,
                "Deactivate this supplier?\n\n" + supplier.SupplierName +
                "\n\nThe supplier will be hidden from the active list; existing purchase history is preserved.",
                "Confirm Deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;

            try
            {
                bool done = _service.Delete(supplier.SupplierId);
                LoadSuppliers();
                MessageBox.Show(this, done ? "Supplier deactivated successfully." : "No active supplier was changed.",
                    done ? "Deactivated" : "No Changes", MessageBoxButtons.OK,
                    done ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to deactivate supplier.\n\n" + ex.Message,
                    "Deactivate Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void OpenSupplierMedicines(Supplier supplier)
        {
            if (supplier == null)
                return;

            try
            {
                using (var form = new MedicineForm(
                    supplier.SupplierId,
                    supplier.SupplierName, this))
                {
                    // Keep Supplier Management open behind the inventory.
                    form.ShowDialog(this);

                    // Reload supplier data after MedicineForm closes.
                    RefreshSupplierList();
                }

                // When MedicineForm closes, SupplierForm is available again.
                Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Unable to open medicines for this supplier.\n\n" + ex.Message,
                    "Medicine Inventory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

    }
}
