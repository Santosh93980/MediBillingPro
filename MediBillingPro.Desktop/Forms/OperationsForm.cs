using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MediBillingPro.Desktop.UI.Common;

namespace MediBillingPro.Desktop.Forms
{
    // Operational pages are embedded by DashboardForm. Financial documents are
    // intentionally corrected through returns/adjustments, not hard-deleted.
    public class OperationsForm : Form
    {
        private readonly string _page;
        private readonly string _connectionString;
        private readonly Color _green = MediBillingTheme.Primary;
        private readonly Color _dark = MediBillingTheme.Secondary;
        private readonly Color _pageColor = MediBillingTheme.Page;
        private DataGridView _grid;
        private TextBox _search;
        private Label _status;
        private ComboBox _supplier;
        private ComboBox _medicine;
        private ComboBox _batch;
        private NumericUpDown _quantity;
        private NumericUpDown _freeQuantity;
        private NumericUpDown _purchasePrice;
        private NumericUpDown _sellingPrice;
        private NumericUpDown _gst;
        private NumericUpDown _paid;
        private TextBox _batchNumber;
        private DateTimePicker _expiry;
        private ComboBox _customer;
        private NumericUpDown _saleQuantity;
        private NumericUpDown _saleDiscount;
        private TextBox _customerName;
        private TextBox _mobile;
        private TextBox _email;
        private TextBox _address;
        private TextBox _gstin;
        private TextBox _settingKey;
        private TextBox _settingValue;
        private TextBox _settingDescription;

        public OperationsForm(string page)
        {
            _page = page ?? "Customers";
            var cs = ConfigurationManager.ConnectionStrings["MediBillingProDB"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                throw new InvalidOperationException("Connection string 'MediBillingProDB' was not found in App.config.");
            _connectionString = cs.ConnectionString;
            Text = _page;
            BackColor = _pageColor;
            Font = new Font("Segoe UI", 9.5F);
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(760, 500);
            BuildPage();
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            AuditService.SetContext(connection, "Purchases / Operations");
            return connection;
        }

        private void BuildPage()
        {
            Dock = DockStyle.Fill;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20), BackColor = _pageColor };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new Label { Text = _page, AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = _dark, Margin = new Padding(0, 0, 0, 14) };
            root.Controls.Add(heading, 0, 0);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 0, 0, 12) };
            _search = new TextBox { Width = 250 };
            _search.TextChanged += (s, e) => { if (_page == "Customers" || _page == "Settings") LoadGrid(); };
            actions.Controls.Add(_search);
            AddButton(actions, "Refresh", LoadGrid, false);
            if (_page == "Customers") { AddButton(actions, "Add Customer", () => EditCustomer(null), true); AddButton(actions, "Delete Customer", DeleteCustomer, false); }
            if (_page == "Settings") { AddButton(actions, "Add Setting", () => EditSetting(null), true); AddButton(actions, "Delete Setting", DeleteSetting, false); }
            if (_page == "Purchases") AddButton(actions, "New Purchase", BuildPurchaseEditor, true);
            if (_page == "Sales & Billing") AddButton(actions, "New Sale", BuildSaleEditor, true);
            if (_page == "Reports") AddButton(actions, "Inventory Report", () => LoadReport("inventory"), false);
            if (_page == "Stock & Batches") AddButton(actions, "Expiring Stock", () => LoadReport("expiry"), false);
            root.Controls.Add(actions, 0, 1);
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AutoGenerateColumns = true };
            UiStyle.StyleGrid(_grid);
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && _page == "Customers") EditCustomer(Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["CustomerId"].Value)); if (e.RowIndex >= 0 && _page == "Settings") EditSetting(Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["SettingId"].Value)); };
            body.Controls.Add(_grid);
            root.Controls.Add(body, 0, 2);
            Controls.Add(root);
            LoadGrid();
        }

        private void AddButton(Control parent, string text, Action action, bool primary)
        {
            var b = new Button { Text = text, AutoSize = true, Height = 34, Padding = new Padding(12, 2, 12, 2), FlatStyle = FlatStyle.Flat, BackColor = primary ? _green : Color.White, ForeColor = primary ? Color.White : _dark, Cursor = Cursors.Hand, Margin = new Padding(4, 3, 4, 3) };
            b.FlatAppearance.BorderColor = _green;
            b.Click += (s, e) => { try { action(); } catch (Exception ex) { MessageBox.Show(ex.Message, _page, MessageBoxButtons.OK, MessageBoxIcon.Error); } };
            parent.Controls.Add(b);
        }

        private void LoadGrid()
        {
            if (_grid == null) return;
            string sql;
            switch (_page)
            {
                case "Customers":
                    sql = @"SELECT CustomerId, CustomerName, MobileNumber, Email, Address, GSTIN, CreatedAt FROM dbo.Customers WHERE (@q = '' OR CustomerName LIKE @like OR MobileNumber LIKE @like) ORDER BY CustomerName";
                    break;
                case "Purchases":
                    sql = @"SELECT p.PurchaseId, p.PurchaseNumber, s.SupplierName, p.InvoiceNumber, p.PurchaseDate, p.TotalAmount, p.AmountPaid, p.Status FROM dbo.Purchases p INNER JOIN dbo.Suppliers s ON s.SupplierId=p.SupplierId ORDER BY p.PurchaseDate DESC";
                    break;
                case "Sales & Billing":
                    sql = @"SELECT i.InvoiceId, i.InvoiceNumber, i.InvoiceDate, COALESCE(c.CustomerName,'Walk-in') AS CustomerName, i.TotalAmount, i.AmountPaid, i.BalanceAmount, i.PaymentStatus, i.InvoiceStatus FROM dbo.Invoices i LEFT JOIN dbo.Customers c ON c.CustomerId=i.CustomerId ORDER BY i.InvoiceDate DESC";
                    break;
                case "Stock & Batches":
                    sql = @"SELECT b.BatchId, m.MedicineName, b.BatchNumber, b.ExpiryDate, b.PurchasePrice, b.SellingPrice, b.QuantityOnHand, b.GSTPercent FROM dbo.MedicineBatches b INNER JOIN dbo.Medicines m ON m.MedicineId=b.MedicineId ORDER BY b.ExpiryDate";
                    break;
                case "Reports":
                    sql = @"SELECT 'Purchases' AS ReportType, COUNT(*) AS DocumentCount, COALESCE(SUM(TotalAmount),0) AS TotalValue FROM dbo.Purchases UNION ALL SELECT 'Sales', COUNT(*), COALESCE(SUM(TotalAmount),0) FROM dbo.Invoices WHERE InvoiceStatus <> 'Voided'";
                    break;
                case "Settings":
                    sql = @"SELECT SettingId, SettingKey, SettingValue, Description, UpdatedAt FROM dbo.AppSettings WHERE (@q='' OR SettingKey LIKE @like OR Description LIKE @like) ORDER BY SettingKey";
                    break;
                default:
                    sql = @"SELECT b.BatchId, m.MedicineName, b.BatchNumber, b.ExpiryDate, b.QuantityOnHand FROM dbo.MedicineBatches b INNER JOIN dbo.Medicines m ON m.MedicineId=b.MedicineId";
                    break;
            }
            using (var cn = OpenConnection())
            using (var cmd = new SqlCommand(sql, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                if (_page == "Customers" || _page == "Settings")
                {
                    string q = _search == null ? "" : _search.Text.Trim();
                    cmd.Parameters.Add("@q", SqlDbType.NVarChar, 200).Value = q;
                    cmd.Parameters.Add("@like", SqlDbType.NVarChar, 202).Value = "%" + q + "%";
                }
                var table = new DataTable();
                da.Fill(table);
                _grid.DataSource = table;
                if (_grid.Columns.Contains("CustomerId")) _grid.Columns["CustomerId"].Visible = false;
                if (_grid.Columns.Contains("SettingId")) _grid.Columns["SettingId"].Visible = false;
                if (_grid.Columns.Contains("PurchaseId")) _grid.Columns["PurchaseId"].Visible = false;
                if (_grid.Columns.Contains("InvoiceId")) _grid.Columns["InvoiceId"].Visible = false;
            }
        }

        private void EditCustomer(int? id)
        {
            string name = "", mobile = "", email = "", address = "", gstin = "";
            if (id.HasValue)
            {
                using (var cn = OpenConnection())
                using (var cmd = new SqlCommand("SELECT CustomerName,MobileNumber,Email,Address,GSTIN FROM dbo.Customers WHERE CustomerId=@id", cn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader()) if (r.Read()) { name = S(r, 0); mobile = S(r, 1); email = S(r, 2); address = S(r, 3); gstin = S(r, 4); }
                }
            }
            using (var f = new Form { Text = id.HasValue ? "Edit Customer" : "Add Customer", Width = 460, Height = 430, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                var fields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 6 };
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var n = AddField(fields, 0, "Customer name", name); var m = AddField(fields, 1, "Mobile number", mobile); var em = AddField(fields, 2, "Email", email); var a = AddField(fields, 3, "Address", address); var g = AddField(fields, 4, "GSTIN", gstin);
                var save = new Button { Text = "Save", Dock = DockStyle.Fill, BackColor = _green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", Dock = DockStyle.Fill, DialogResult = DialogResult.Cancel };
                fields.Controls.Add(save, 0, 5); fields.Controls.Add(cancel, 1, 5); f.Controls.Add(fields); f.AcceptButton = save; f.CancelButton = cancel;
                save.Click += (s, e) => {
                    if (string.IsNullOrWhiteSpace(n.Text)) { MessageBox.Show("Customer name is required."); return; }
                    using (var cn = OpenConnection())
                    using (var cmd = new SqlCommand(id.HasValue ? "UPDATE dbo.Customers SET CustomerName=@n,MobileNumber=@m,Email=@e,Address=@a,GSTIN=@g WHERE CustomerId=@id" : "INSERT dbo.Customers(CustomerName,MobileNumber,Email,Address,GSTIN,CreatedAt) VALUES(@n,@m,@e,@a,@g,SYSDATETIME())", cn)) {
                        cmd.Parameters.AddWithValue("@n", n.Text.Trim()); cmd.Parameters.AddWithValue("@m", Db(m.Text)); cmd.Parameters.AddWithValue("@e", Db(em.Text)); cmd.Parameters.AddWithValue("@a", Db(a.Text)); cmd.Parameters.AddWithValue("@g", Db(g.Text)); if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value); cmd.ExecuteNonQuery();
                    }
                    f.DialogResult = DialogResult.OK; f.Close();
                };
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }


        private void DeleteCustomer()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("CustomerId")) { MessageBox.Show("Select a customer first."); return; }
            int id = Convert.ToInt32(_grid.CurrentRow.Cells["CustomerId"].Value);
            if (MessageBox.Show("Delete the selected customer? Customers linked to invoices cannot be deleted.", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            using (var cn = OpenConnection()) using (var cmd = new SqlCommand("DELETE dbo.Customers WHERE CustomerId=@id", cn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                try { cmd.ExecuteNonQuery(); } catch (SqlException ex) { MessageBox.Show("This customer is linked to a billing record and cannot be deleted. " + ex.Message); }
            }
            LoadGrid();
        }

        private void DeleteSetting()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("SettingId")) { MessageBox.Show("Select a setting first."); return; }
            int id = Convert.ToInt32(_grid.CurrentRow.Cells["SettingId"].Value);
            if (MessageBox.Show("Delete the selected setting?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            using (var cn = OpenConnection()) using (var cmd = new SqlCommand("DELETE dbo.AppSettings WHERE SettingId=@id", cn))
            { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
            LoadGrid();
        }

        private void EditSetting(int? id)
        {
            string key = "", value = "", description = "";
            if (id.HasValue) using (var cn = OpenConnection()) using (var cmd = new SqlCommand("SELECT SettingKey,SettingValue,Description FROM dbo.AppSettings WHERE SettingId=@id", cn)) { cmd.Parameters.AddWithValue("@id", id.Value); using (var r = cmd.ExecuteReader()) if (r.Read()) { key=S(r,0); value=S(r,1); description=S(r,2); } }
            using (var f = new Form { Text = id.HasValue ? "Edit Setting" : "Add Setting", Width = 460, Height = 330, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog })
            {
                var p = new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(18), ColumnCount=2, RowCount=4 }; p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                var k=AddField(p,0,"Key",key); var v=AddField(p,1,"Value",value); var d=AddField(p,2,"Description",description);
                var save=new Button{Text="Save",Dock=DockStyle.Fill,BackColor=_green,ForeColor=Color.White}; var cancel=new Button{Text="Cancel",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel}; p.Controls.Add(save,0,3);p.Controls.Add(cancel,1,3);f.Controls.Add(p);
                save.Click+=(s,e)=>{if(string.IsNullOrWhiteSpace(k.Text)){MessageBox.Show("Setting key is required.");return;} using(var cn=OpenConnection())using(var cmd=new SqlCommand(id.HasValue?"UPDATE dbo.AppSettings SET SettingKey=@k,SettingValue=@v,Description=@d,UpdatedAt=SYSDATETIME() WHERE SettingId=@id":"INSERT dbo.AppSettings(SettingKey,SettingValue,Description,UpdatedAt) VALUES(@k,@v,@d,SYSDATETIME())",cn)){cmd.Parameters.AddWithValue("@k",k.Text.Trim());cmd.Parameters.AddWithValue("@v",Db(v.Text));cmd.Parameters.AddWithValue("@d",Db(d.Text));if(id.HasValue)cmd.Parameters.AddWithValue("@id",id.Value);cmd.ExecuteNonQuery();} f.DialogResult=DialogResult.OK;f.Close();};
                if(f.ShowDialog(this)==DialogResult.OK)LoadGrid();
            }
        }

        private TextBox AddField(TableLayoutPanel p, int row, string label, string value)
        {
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            p.Controls.Add(new Label { Text=label, Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft },0,row);
            var t=new TextBox { Text=value ?? "", Dock=DockStyle.Fill, Margin=new Padding(3,8,3,8) }; p.Controls.Add(t,1,row); return t;
        }

        private void BuildPurchaseEditor()
        {
            using (var f = new Form { Text = "Purchase Entry | MediBilling Pro", Width = 1120, Height = 760, MinimumSize = new Size(980, 650), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable })
            {
                var p = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 4, RowCount = 10, BackColor = _pageColor };
                for (int i = 0; i < 4; i++) p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                for (int i = 0; i <= 5; i++) p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

                var supplier = AddCombo(p, 0, 0, "Supplier", "SELECT SupplierId, SupplierName FROM dbo.Suppliers WHERE IsActive=1 ORDER BY SupplierName");
                var medicine = AddCombo(p, 0, 2, "Medicine", "SELECT MedicineId, MedicineName FROM dbo.Medicines WHERE IsActive=1 ORDER BY MedicineName");
                var batchNo = AddText(p, 1, 0, "Batch number");
                var expiry = AddDate(p, 1, 2, "Expiry date");
                var qty = AddNumber(p, 2, 0, "Quantity", 1, 100000);
                var freeQty = AddNumber(p, 2, 2, "Free qty", 0, 100000);
                var buyPrice = AddMoney(p, 3, 0, "Purchase price");
                var sellPrice = AddMoney(p, 3, 2, "Selling price");
                var gst = AddMoney(p, 4, 0, "GST %");
                var supplierInvoice = AddText(p, 4, 2, "Supplier invoice #");

                // Reuse the medicine master prices to reduce duplicate typing on purchase entry.
                medicine.SelectedIndexChanged += (s, e) =>
                {
                    if (medicine.SelectedValue == null || medicine.SelectedValue == DBNull.Value)
                        return;
                    int medicineId;
                    if (!int.TryParse(Convert.ToString(medicine.SelectedValue), out medicineId) || medicineId <= 0)
                        return;
                    try
                    {
                        using (var cn = OpenConnection())
                        using (var cmd = new SqlCommand(@"SELECT PurchasePrice, SellingPrice FROM dbo.Medicines WHERE MedicineId=@id AND IsActive=1", cn))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = medicineId;
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    buyPrice.Value = Math.Max(buyPrice.Minimum, Math.Min(buyPrice.Maximum, Convert.ToDecimal(reader["PurchasePrice"])));
                                    sellPrice.Value = Math.Max(sellPrice.Minimum, Math.Min(sellPrice.Maximum, Convert.ToDecimal(reader["SellingPrice"])));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not load the saved medicine prices. You can enter them manually.\\n" + ex.Message,
                            "Purchase entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                var cart = new DataTable();
                cart.Columns.Add("MedicineId", typeof(int));
                cart.Columns.Add("Medicine", typeof(string));
                cart.Columns.Add("BatchNumber", typeof(string));
                cart.Columns.Add("ExpiryDate", typeof(DateTime));
                cart.Columns.Add("Quantity", typeof(int));
                cart.Columns.Add("FreeQuantity", typeof(int));
                cart.Columns.Add("PurchasePrice", typeof(decimal));
                cart.Columns.Add("SellingPrice", typeof(decimal));
                cart.Columns.Add("GSTPercent", typeof(decimal));
                cart.Columns.Add("TaxAmount", typeof(decimal));
                cart.Columns.Add("LineTotal", typeof(decimal));

                var addItem = new Button { Text = "+ Add medicine to purchase", Dock = DockStyle.Fill, BackColor = _green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                p.Controls.Add(addItem, 0, 5); p.SetColumnSpan(addItem, 2);

                var cartGrid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
                cartGrid.DataSource = cart;
                // Optional columns may not exist in every cart schema.
                if (cartGrid.Columns.Contains("MedicineId")) cartGrid.Columns["MedicineId"].Visible = false;
                if (cartGrid.Columns.Contains("BatchId")) cartGrid.Columns["BatchId"].Visible = false;
                p.Controls.Add(cartGrid, 0, 6); p.SetColumnSpan(cartGrid, 4);

                var summary = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = _dark, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Text = "Subtotal: ₹0.00    GST: ₹0.00    Discount: ₹0.00    Total: ₹0.00" };
                p.Controls.Add(summary, 0, 7); p.SetColumnSpan(summary, 4);

                var discount = AddMoney(p, 8, 0, "Bill discount");
                var paid = AddMoney(p, 8, 2, "Amount paid");
                var save = new Button { Text = "Save Purchase", Dock = DockStyle.Fill, BackColor = _green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
                var cancel = new Button { Text = "Cancel", Dock = DockStyle.Fill, DialogResult = DialogResult.Cancel };
                p.Controls.Add(save, 0, 9); p.SetColumnSpan(save, 2); p.Controls.Add(cancel, 2, 9); p.SetColumnSpan(cancel, 2);
                f.Controls.Add(p);

                Action updateSummary = () =>
                {
                    decimal sub = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["PurchasePrice"]) * Convert.ToInt32(r["Quantity"]));
                    decimal tax = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["TaxAmount"]));
                    decimal disc = discount.Value;
                    decimal total = Math.Max(0m, sub - disc + tax);
                    summary.Text = "Subtotal: " + sub.ToString("C2") + "    GST: " + tax.ToString("C2") + "    Discount: " + disc.ToString("C2") + "    Total: " + total.ToString("C2") + "    Due: " + Math.Max(0m, total - paid.Value).ToString("C2");
                };
                cart.RowChanged += (s, e) => updateSummary();
                cart.RowDeleted += (s, e) => updateSummary();
                discount.ValueChanged += (s, e) => updateSummary();
                paid.ValueChanged += (s, e) => updateSummary();

                addItem.Click += (s, e) =>
                {
                    if (supplier.SelectedValue == null || medicine.SelectedValue == null) { MessageBox.Show("Select a supplier and medicine first."); return; }
                    if (string.IsNullOrWhiteSpace(batchNo.Text)) { MessageBox.Show("Batch number is required."); return; }
                    if (expiry.Value.Date <= DateTime.Today) { MessageBox.Show("Expiry date must be in the future."); return; }
                    decimal lineBase = qty.Value * buyPrice.Value;
                    decimal lineTax = Math.Round(lineBase * gst.Value / 100m, 2);
                    cart.Rows.Add(Convert.ToInt32(medicine.SelectedValue), Convert.ToString(medicine.Text), batchNo.Text.Trim(), expiry.Value.Date, (int)qty.Value, (int)freeQty.Value, buyPrice.Value, sellPrice.Value, gst.Value, lineTax, lineBase + lineTax);
                    batchNo.Clear();
                    qty.Value = 1; freeQty.Value = 0;
                    updateSummary();
                };

                var removeItem = new Button { Text = "Remove selected item", Dock = DockStyle.Bottom, Height = 32, BackColor = Color.White, ForeColor = _dark, FlatStyle = FlatStyle.Flat };
                // Place removal action above the grid as a small secondary action.
                removeItem.Click += (s, e) => { if (cartGrid.CurrentRow != null && cartGrid.CurrentRow.Index >= 0) cart.Rows.RemoveAt(cartGrid.CurrentRow.Index); updateSummary(); };
                p.Controls.Add(removeItem, 2, 5); p.SetColumnSpan(removeItem, 2);

                save.Click += (s, e) =>
                {
                    try
                    {
                        if (supplier.SelectedValue == null) throw new Exception("Select a supplier.");
                        if (cart.Rows.Count == 0) throw new Exception("Add at least one medicine to the purchase.");
                        decimal sub = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["PurchasePrice"]) * Convert.ToInt32(r["Quantity"]));
                        decimal tax = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["TaxAmount"]));
                        decimal total = Math.Max(0m, sub - discount.Value + tax);
                        if (discount.Value > sub) throw new Exception("Bill discount cannot exceed the subtotal.");
                        if (paid.Value > total) throw new Exception("Amount paid cannot exceed the purchase total.");

                        var purchase = new MediBillingPro.Domain.Entities.Purchasing.Purchase
                        {
                            SupplierId = Convert.ToInt32(supplier.SelectedValue),
                            InvoiceNumber = supplierInvoice.Text.Trim(),
                            PurchaseDate = DateTime.Today,
                            DiscountAmount = discount.Value,
                            AmountPaid = paid.Value
                        };
                        var items = new System.Collections.Generic.List<MediBillingPro.Domain.Entities.Purchasing.PurchaseItem>();
                        foreach (DataRow r in cart.Rows)
                        {
                            items.Add(new MediBillingPro.Domain.Entities.Purchasing.PurchaseItem
                            {
                                MedicineId = Convert.ToInt32(r["MedicineId"]),
                                BatchNumber = Convert.ToString(r["BatchNumber"]),
                                ExpiryDate = Convert.ToDateTime(r["ExpiryDate"]),
                                Quantity = Convert.ToInt32(r["Quantity"]),
                                FreeQuantity = Convert.ToInt32(r["FreeQuantity"]),
                                PurchasePrice = Convert.ToDecimal(r["PurchasePrice"]),
                                SellingPrice = Convert.ToDecimal(r["SellingPrice"]),
                                GSTPercent = Convert.ToDecimal(r["GSTPercent"]),
                                TaxAmount = Convert.ToDecimal(r["TaxAmount"]),
                                LineTotal = Convert.ToDecimal(r["LineTotal"])
                            });
                        }
                        var factory = new MediBillingPro.Infrastructure.Data.DatabaseConnectionFactory(_connectionString);
                        var service = new MediBillingPro.Application.Services.PurchaseService(new MediBillingPro.Infrastructure.Repositories.PurchaseRepository(factory));
                        service.Create(purchase, items);
                        MessageBox.Show("Purchase saved successfully.\nTotal: " + total.ToString("C2") + "\nStock has been updated.", "Purchase saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        f.DialogResult = DialogResult.OK;
                        f.Close();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Unable to save purchase", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                updateSummary();
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void BuildSaleEditor()
        {
            using (var f = new Form { Text = "Sales & Billing | Point of Sale", Width = 1120, Height = 760, MinimumSize = new Size(980, 650), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable })
            {
                var p = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 4, RowCount = 8, BackColor = _pageColor };
                for (int i = 0; i < 4; i++) p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                p.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

                var customer = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "CustomerName", ValueMember = "CustomerId" };
                p.Controls.Add(new Label { Text = "Customer", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                p.Controls.Add(customer, 1, 0);
                var batch = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "BatchLabel", ValueMember = "BatchId" };
                p.Controls.Add(new Label { Text = "Medicine / batch", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
                p.Controls.Add(batch, 3, 0);

                var qty = AddNumber(p, 1, 0, "Quantity", 1, 100000);
                var lineDiscount = AddMoney(p, 1, 2, "Line discount");
                var add = new Button { Text = "+ Add to bill", Dock = DockStyle.Fill, BackColor = _green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                p.Controls.Add(add, 0, 2); p.SetColumnSpan(add, 2);
                var remove = new Button { Text = "Remove selected line", Dock = DockStyle.Fill, BackColor = Color.White, ForeColor = _dark, FlatStyle = FlatStyle.Flat };
                p.Controls.Add(remove, 2, 2); p.SetColumnSpan(remove, 2);

                var cart = new DataTable();
                cart.Columns.Add("BatchId", typeof(int));
                cart.Columns.Add("MedicineId", typeof(int));
                cart.Columns.Add("Medicine", typeof(string));
                cart.Columns.Add("Batch", typeof(string));
                cart.Columns.Add("Quantity", typeof(int));
                cart.Columns.Add("StockAvailable", typeof(int));
                cart.Columns.Add("UnitPrice", typeof(decimal));
                cart.Columns.Add("GSTPercent", typeof(decimal));
                cart.Columns.Add("DiscountAmount", typeof(decimal));
                cart.Columns.Add("TaxAmount", typeof(decimal));
                cart.Columns.Add("LineTotal", typeof(decimal));

                var cartGrid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
                cartGrid.DataSource = cart;
                // Guard optional hidden columns before indexing the collection.
                if (cartGrid.Columns.Contains("BatchId")) cartGrid.Columns["BatchId"].Visible = false;
                if (cartGrid.Columns.Contains("MedicineId")) cartGrid.Columns["MedicineId"].Visible = false;
                if (cartGrid.Columns.Contains("StockAvailable")) cartGrid.Columns["StockAvailable"].Visible = false;
                p.Controls.Add(cartGrid, 0, 4); p.SetColumnSpan(cartGrid, 4);

                var summary = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = _dark, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
                p.Controls.Add(summary, 0, 5); p.SetColumnSpan(summary, 4);
                var paid = AddMoney(p, 6, 0, "Cash received");
                var totalsHelp = new Label { Text = "Total and balance update automatically", Dock = DockStyle.Fill, ForeColor = _dark, TextAlign = ContentAlignment.MiddleCenter };
                p.Controls.Add(totalsHelp, 2, 6); p.SetColumnSpan(totalsHelp, 2);
                var complete = new Button { Text = "Complete Sale / Save Invoice", Dock = DockStyle.Fill, BackColor = _green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
                var cancel = new Button { Text = "Cancel", Dock = DockStyle.Fill, DialogResult = DialogResult.Cancel };
                p.Controls.Add(complete, 0, 7); p.SetColumnSpan(complete, 2); p.Controls.Add(cancel, 2, 7); p.SetColumnSpan(cancel, 2);
                f.Controls.Add(p);

                using (var cn = OpenConnection())
                {
                    var customers = new DataTable();
                    customers.Columns.Add("CustomerId", typeof(int));
                    customers.Columns["CustomerId"].AllowDBNull = true;
                    customers.Columns.Add("CustomerName", typeof(string));
                    using (var da = new SqlDataAdapter("SELECT CustomerId, CustomerName FROM dbo.Customers ORDER BY CustomerName", cn)) da.Fill(customers);
                    var walk = customers.NewRow(); walk["CustomerId"] = DBNull.Value; walk["CustomerName"] = "Walk-in customer"; customers.Rows.InsertAt(walk, 0);
                    customer.DataSource = customers;

                    var batches = new DataTable();
                    using (var da = new SqlDataAdapter(@"SELECT b.BatchId, b.MedicineId, m.MedicineName, b.BatchNumber, b.SellingPrice, b.GSTPercent, b.QuantityOnHand, b.ExpiryDate,
CAST(m.MedicineName AS NVARCHAR(150))+' | Batch '+b.BatchNumber+' | Stock '+CAST(b.QuantityOnHand AS NVARCHAR(20))+' | Exp '+CONVERT(NVARCHAR(10),b.ExpiryDate,103) AS BatchLabel
FROM dbo.MedicineBatches b INNER JOIN dbo.Medicines m ON m.MedicineId=b.MedicineId
WHERE b.QuantityOnHand>0 AND b.ExpiryDate>CONVERT(date,GETDATE()) ORDER BY m.MedicineName,b.ExpiryDate", cn)) da.Fill(batches);
                    batch.DataSource = batches;
                }

                Action updateSummary = () =>
                {
                    decimal sub = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["UnitPrice"]) * Convert.ToInt32(r["Quantity"]));
                    decimal discountTotal = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["DiscountAmount"]));
                    decimal tax = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["TaxAmount"]));
                    decimal total = Math.Max(0m, sub - discountTotal + tax);
                    decimal tender = paid.Value;
                    decimal received = Math.Min(tender, total);
                    decimal change = Math.Max(0m, tender - total);
                    summary.Text = "Subtotal: " + sub.ToString("C2") + "    Discount: " + discountTotal.ToString("C2") + "    GST: " + tax.ToString("C2") + "    TOTAL: " + total.ToString("C2") + "    Due: " + Math.Max(0m, total - received).ToString("C2") + "    Change: " + change.ToString("C2");
                };
                cart.RowChanged += (s, e) => updateSummary();
                cart.RowDeleted += (s, e) => updateSummary();
                paid.ValueChanged += (s, e) => updateSummary();

                add.Click += (s, e) =>
                {
                    if (batch.SelectedItem == null) { MessageBox.Show("No in-stock, unexpired batch is available."); return; }
                    var selected = (DataRowView)batch.SelectedItem;
                    int batchId = Convert.ToInt32(selected["BatchId"]);
                    int stock = Convert.ToInt32(selected["QuantityOnHand"]);
                    int count = (int)qty.Value;
                    if (count > stock) { MessageBox.Show("Quantity exceeds available stock."); return; }
                    decimal price = Convert.ToDecimal(selected["SellingPrice"]);
                    decimal gst = Convert.ToDecimal(selected["GSTPercent"]);
                    decimal baseAmount = price * count;
                    if (lineDiscount.Value > baseAmount) { MessageBox.Show("Line discount cannot exceed line subtotal."); return; }
                    decimal tax = Math.Round((baseAmount - lineDiscount.Value) * gst / 100m, 2);
                    decimal total = baseAmount - lineDiscount.Value + tax;
                    DataRow existing = cart.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["BatchId"]) == batchId);
                    if (existing != null)
                    {
                        int newQty = Convert.ToInt32(existing["Quantity"]) + count;
                        if (newQty > stock) { MessageBox.Show("Combined quantity exceeds available stock."); return; }
                        decimal newBase = price * newQty;
                        decimal newDiscount = Convert.ToDecimal(existing["DiscountAmount"]) + lineDiscount.Value;
                        if (newDiscount > newBase) { MessageBox.Show("Combined discount cannot exceed line subtotal."); return; }
                        decimal newTax = Math.Round((newBase - newDiscount) * gst / 100m, 2);
                        existing["Quantity"] = newQty; existing["DiscountAmount"] = newDiscount; existing["TaxAmount"] = newTax; existing["LineTotal"] = newBase - newDiscount + newTax;
                    }
                    else
                    {
                        cart.Rows.Add(batchId, Convert.ToInt32(selected["MedicineId"]), Convert.ToString(selected["MedicineName"]), Convert.ToString(selected["BatchNumber"]), count, stock, price, gst, lineDiscount.Value, tax, total);
                    }
                    qty.Value = 1; lineDiscount.Value = 0;
                    updateSummary();
                };
                remove.Click += (s, e) => { if (cartGrid.CurrentRow != null && cartGrid.CurrentRow.Index >= 0) cart.Rows.RemoveAt(cartGrid.CurrentRow.Index); updateSummary(); };

                complete.Click += (s, e) =>
                {
                    try
                    {
                        if (cart.Rows.Count == 0) throw new Exception("Add at least one medicine to the bill.");
                        decimal sub = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["UnitPrice"]) * Convert.ToInt32(r["Quantity"]));
                        decimal discountTotal = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["DiscountAmount"]));
                        decimal tax = cart.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["TaxAmount"]));
                        decimal total = Math.Max(0m, sub - discountTotal + tax);
                        decimal received = Math.Min(paid.Value, total);
                        using (var cn = OpenConnection())
                        using (var tx = cn.BeginTransaction())
                        {
                            try
                            {
                                int? customerId = customer.SelectedIndex <= 0 || customer.SelectedValue == DBNull.Value ? (int?)null : Convert.ToInt32(customer.SelectedValue);
                                string no = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                                int invoiceId;
                                using (var cmd = new SqlCommand(@"INSERT dbo.Invoices(InvoiceNumber,CustomerId,InvoiceDate,SubTotal,DiscountAmount,TaxAmount,RoundOff,TotalAmount,AmountPaid,BalanceAmount,PaymentStatus,InvoiceStatus,CreatedAt)
VALUES(@n,@c,SYSDATETIME(),@s,@d,@t,0,@total,@paid,@balance,@status,'Completed',SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS INT)", cn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@n", no);
                                    cmd.Parameters.Add("@c", SqlDbType.Int).Value = (object)customerId ?? DBNull.Value;
                                    cmd.Parameters.AddWithValue("@s", sub); cmd.Parameters.AddWithValue("@d", discountTotal); cmd.Parameters.AddWithValue("@t", tax);
                                    cmd.Parameters.AddWithValue("@total", total); cmd.Parameters.AddWithValue("@paid", received); cmd.Parameters.AddWithValue("@balance", total - received);
                                    cmd.Parameters.AddWithValue("@status", received == 0 ? "Unpaid" : received >= total ? "Paid" : "Partial");
                                    invoiceId = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                                foreach (DataRow r in cart.Rows)
                                {
                                    using (var cmd = new SqlCommand(@"INSERT dbo.InvoiceItems(InvoiceId,MedicineId,BatchId,Quantity,UnitPrice,DiscountAmount,GSTPercent,TaxAmount,LineTotal)
VALUES(@id,@medicine,@batch,@qty,@price,@discount,@gst,@tax,@lineTotal);
UPDATE dbo.MedicineBatches SET QuantityOnHand=QuantityOnHand-@qty WHERE BatchId=@batch AND QuantityOnHand>=@qty;
IF @@ROWCOUNT=0 THROW 50001,'Insufficient stock. Refresh the batch list and try again.',1;", cn, tx))
                                    {
                                        cmd.Parameters.AddWithValue("@id", invoiceId); cmd.Parameters.AddWithValue("@medicine", Convert.ToInt32(r["MedicineId"]));
                                        cmd.Parameters.AddWithValue("@batch", Convert.ToInt32(r["BatchId"])); cmd.Parameters.AddWithValue("@qty", Convert.ToInt32(r["Quantity"]));
                                        cmd.Parameters.AddWithValue("@price", Convert.ToDecimal(r["UnitPrice"])); cmd.Parameters.AddWithValue("@discount", Convert.ToDecimal(r["DiscountAmount"]));
                                        cmd.Parameters.AddWithValue("@gst", Convert.ToDecimal(r["GSTPercent"])); cmd.Parameters.AddWithValue("@tax", Convert.ToDecimal(r["TaxAmount"]));
                                        cmd.Parameters.AddWithValue("@lineTotal", Convert.ToDecimal(r["LineTotal"])); cmd.ExecuteNonQuery();
                                    }
                                }
                                tx.Commit();
                                MessageBox.Show("Invoice " + no + " saved.\nTotal: " + total.ToString("C2") + "\nBalance due: " + (total - received).ToString("C2"), "Sale complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                f.DialogResult = DialogResult.OK; f.Close();
                            }
                            catch { tx.Rollback(); throw; }
                        }
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Unable to complete sale", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                updateSummary();
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private ComboBox AddCombo(TableLayoutPanel p,int row,int col,string label,string sql)
        {
            p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},col,row);
            var c=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};var dt=new DataTable();
            using(var cn=OpenConnection())using(var da=new SqlDataAdapter(sql,cn))da.Fill(dt);
            if(dt.Columns.Count>=2){dt.Columns[0].ColumnName="Id";dt.Columns[1].ColumnName="Name";} c.DataSource=dt;p.Controls.Add(c,col+1,row);return c;
        }
        private TextBox AddText(TableLayoutPanel p,int row,int col,string label){p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},col,row);var t=new TextBox{Dock=DockStyle.Fill};p.Controls.Add(t,col+1,row);return t;}
        private DateTimePicker AddDate(TableLayoutPanel p,int row,int col,string label){p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},col,row);var d=new DateTimePicker{Dock=DockStyle.Fill,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddYears(1)};p.Controls.Add(d,col+1,row);return d;}
        private NumericUpDown AddNumber(TableLayoutPanel p,int row,int col,string label,int min,int max){p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},col,row);var n=new NumericUpDown{Dock=DockStyle.Fill,Minimum=min,Maximum=max,Value=min};p.Controls.Add(n,col+1,row);return n;}
        private NumericUpDown AddMoney(TableLayoutPanel p,int row,int col,string label){p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},col,row);var n=new NumericUpDown{Dock=DockStyle.Fill,Minimum=0,Maximum=100000000,DecimalPlaces=2,Increment=1};p.Controls.Add(n,col+1,row);return n;}
        private object Db(string s){return string.IsNullOrWhiteSpace(s)?(object)DBNull.Value:s.Trim();}
        private static string S(SqlDataReader r,int i){return r.IsDBNull(i)?"":Convert.ToString(r.GetValue(i));}

        private void LoadReport(string report)
        {
            string sql = report=="expiry"
                ? @"SELECT m.MedicineName,b.BatchNumber,b.ExpiryDate,b.QuantityOnHand FROM dbo.MedicineBatches b INNER JOIN dbo.Medicines m ON m.MedicineId=b.MedicineId WHERE b.QuantityOnHand>0 AND b.ExpiryDate<=DATEADD(day,90,CONVERT(date,GETDATE())) ORDER BY b.ExpiryDate"
                : @"SELECT m.MedicineId,m.MedicineName,m.GenericName,m.Manufacturer,COALESCE(SUM(b.QuantityOnHand),0) AS StockOnHand,MIN(CASE WHEN b.QuantityOnHand>0 THEN b.ExpiryDate END) AS NearestExpiry FROM dbo.Medicines m LEFT JOIN dbo.MedicineBatches b ON b.MedicineId=m.MedicineId GROUP BY m.MedicineId,m.MedicineName,m.GenericName,m.Manufacturer ORDER BY m.MedicineName";
            using(var cn=OpenConnection())using(var da=new SqlDataAdapter(sql,cn)){var dt=new DataTable();da.Fill(dt);_grid.DataSource=dt;}
        }
    }
}