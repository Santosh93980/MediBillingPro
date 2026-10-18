using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    // Operational pages are embedded by DashboardForm. Financial documents are
    // intentionally corrected through returns/adjustments, not hard-deleted.
    public class OperationsForm : Form
    {
        private readonly string _page;
        private readonly string _connectionString;
        private readonly Color _green = Color.FromArgb(8, 127, 91);
        private readonly Color _dark = Color.FromArgb(21, 60, 49);
        private readonly Color _pageColor = Color.FromArgb(244, 248, 246);
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
            BuildPage();
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
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
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && _page == "Customers") EditCustomer(Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["CustomerId"].Value)); if (e.RowIndex >= 0 && _page == "Settings") EditSetting(Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["SettingId"].Value)); };
            body.Controls.Add(_grid);
            root.Controls.Add(body, 0, 2);
            Controls.Add(root);
            LoadGrid();
        }

        private void AddButton(Control parent, string text, Action action, bool primary)
        {
            var b = new Button { Text = text, AutoSize = true, Height = 34, Padding = new Padding(12, 2, 12, 2), FlatStyle = FlatStyle.Flat, BackColor = primary ? _green : Color.White, ForeColor = primary ? Color.White : _dark, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
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
            using (var f = new Form { Text="New Purchase Entry", Width=850, Height=610, StartPosition=FormStartPosition.CenterParent, FormBorderStyle=FormBorderStyle.Sizable })
            {
                var p=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=4,RowCount=7}; for(int i=0;i<4;i++)p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25)); for(int i=0;i<6;i++)p.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); p.RowStyles.Add(new RowStyle(SizeType.Percent,100));
                _supplier=AddCombo(p,0,0,"Supplier", "SELECT SupplierId, SupplierName FROM dbo.Suppliers WHERE IsActive=1 ORDER BY SupplierName");
                _medicine=AddCombo(p,0,2,"Medicine", "SELECT MedicineId, MedicineName FROM dbo.Medicines WHERE IsActive=1 ORDER BY MedicineName");
                _batchNumber=AddText(p,1,0,"Batch number"); _expiry=AddDate(p,1,2,"Expiry date");
                _quantity=AddNumber(p,2,0,"Quantity",1,100000); _freeQuantity=AddNumber(p,2,2,"Free qty",0,100000);
                _purchasePrice=AddMoney(p,3,0,"Purchase price"); _sellingPrice=AddMoney(p,3,2,"Selling price"); _gst=AddMoney(p,4,0,"GST %"); _paid=AddMoney(p,4,2,"Amount paid");
                var invoice=AddText(p,5,0,"Supplier invoice #"); var save=new Button{Text="Save Purchase",Dock=DockStyle.Fill,BackColor=_green,ForeColor=Color.White}; var cancel=new Button{Text="Cancel",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel}; p.Controls.Add(save,0,6);p.SetColumnSpan(save,2);p.Controls.Add(cancel,2,6);p.SetColumnSpan(cancel,2);f.Controls.Add(p);
                save.Click+=(s,e)=>{try{
                    if(_supplier.SelectedValue==null||_medicine.SelectedValue==null)throw new Exception("Select a supplier and medicine.");
                    if(string.IsNullOrWhiteSpace(_batchNumber.Text))throw new Exception("Batch number is required.");
                    decimal qty=_quantity.Value, buy=_purchasePrice.Value, gst=_gst.Value; decimal tax=Math.Round(qty*buy*gst/100m,2);
                    var purchase=new MediBillingPro.Domain.Entities.Purchasing.Purchase{SupplierId=Convert.ToInt32(_supplier.SelectedValue),InvoiceNumber=invoice.Text.Trim(),PurchaseDate=DateTime.Today,DiscountAmount=0,AmountPaid=_paid.Value};
                    var item=new MediBillingPro.Domain.Entities.Purchasing.PurchaseItem{MedicineId=Convert.ToInt32(_medicine.SelectedValue),BatchNumber=_batchNumber.Text.Trim(),ExpiryDate=_expiry.Value.Date,Quantity=(int)_quantity.Value,FreeQuantity=(int)_freeQuantity.Value,PurchasePrice=buy,SellingPrice=_sellingPrice.Value,GSTPercent=gst,TaxAmount=tax,LineTotal=qty*buy+tax};
                    var factory=new MediBillingPro.Infrastructure.Data.DatabaseConnectionFactory(_connectionString);
                    var service=new MediBillingPro.Application.Services.PurchaseService(new MediBillingPro.Infrastructure.Repositories.PurchaseRepository(factory));
                    service.Create(purchase,new System.Collections.Generic.List<MediBillingPro.Domain.Entities.Purchasing.PurchaseItem>{item});
                    MessageBox.Show("Purchase saved and batch stock updated.","Purchase",MessageBoxButtons.OK,MessageBoxIcon.Information);f.DialogResult=DialogResult.OK;f.Close();
                }catch(Exception ex){MessageBox.Show(ex.Message,"Unable to save purchase",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
                if(f.ShowDialog(this)==DialogResult.OK)LoadGrid();
            }
        }

        private void BuildSaleEditor()
        {
            using(var f=new Form{Text="New Sale / Billing",Width=700,Height=390,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog})
            {
                var p=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=5};p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                var cust=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="CustomerName",ValueMember="CustomerId"};p.Controls.Add(new Label{Text="Customer (optional)",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);p.Controls.Add(cust,1,0);
                var batch=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="BatchLabel",ValueMember="BatchId"};p.Controls.Add(new Label{Text="Medicine batch",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,1);p.Controls.Add(batch,1,1);
                var qty=new NumericUpDown{Minimum=1,Maximum=100000,Value=1,Dock=DockStyle.Left};p.Controls.Add(new Label{Text="Quantity",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,2);p.Controls.Add(qty,1,2);
                var paid=new NumericUpDown{Minimum=0,Maximum=100000000,DecimalPlaces=2,Increment=10,Dock=DockStyle.Left,Width=180};p.Controls.Add(new Label{Text="Amount received",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,3);p.Controls.Add(paid,1,3);
                using(var cn=OpenConnection()){var dt=new DataTable();using(var da=new SqlDataAdapter("SELECT CustomerId,CustomerName FROM dbo.Customers ORDER BY CustomerName",cn))da.Fill(dt);var walk=dt.NewRow();walk["CustomerId"]=DBNull.Value;walk["CustomerName"]="Walk-in customer";dt.Rows.InsertAt(walk,0);cust.DataSource=dt;
                    var bt=new DataTable();using(var da=new SqlDataAdapter("SELECT b.BatchId, CAST(m.MedicineName AS NVARCHAR(150))+' | '+b.BatchNumber+' | stock '+CAST(b.QuantityOnHand AS NVARCHAR(20)) AS BatchLabel,b.SellingPrice,b.GSTPercent,b.QuantityOnHand FROM dbo.MedicineBatches b INNER JOIN dbo.Medicines m ON m.MedicineId=b.MedicineId WHERE b.QuantityOnHand>0 AND b.ExpiryDate>CONVERT(date,GETDATE()) ORDER BY m.MedicineName,b.ExpiryDate",cn))da.Fill(bt);batch.DataSource=bt;}
                var save=new Button{Text="Complete Sale",Dock=DockStyle.Fill,BackColor=_green,ForeColor=Color.White};var cancel=new Button{Text="Cancel",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel};p.Controls.Add(save,0,4);p.Controls.Add(cancel,1,4);f.Controls.Add(p);
                save.Click+=(s,e)=>{try{
                    if(batch.SelectedValue==null)throw new Exception("No in-stock, unexpired batch is available.");
                    var selected=(DataRowView)batch.SelectedItem;int batchId=Convert.ToInt32(selected["BatchId"]);int count=(int)qty.Value;int stock=Convert.ToInt32(selected["QuantityOnHand"]);if(count>stock)throw new Exception("Quantity exceeds available stock.");
                    decimal price=Convert.ToDecimal(selected["SellingPrice"]), gst=Convert.ToDecimal(selected["GSTPercent"]);decimal sub=price*count;decimal tax=Math.Round(sub*gst/100m,2);decimal total=sub+tax;decimal received=paid.Value;if(received>total)throw new Exception("Amount received cannot exceed total.");
                    using(var cn=OpenConnection())using(var tx=cn.BeginTransaction()){
                        try{
                            int? customerId=cust.SelectedIndex<=0?(int?)null:Convert.ToInt32(cust.SelectedValue);
                            string no="INV-"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            int invoiceId;using(var cmd=new SqlCommand(@"INSERT dbo.Invoices(InvoiceNumber,CustomerId,InvoiceDate,SubTotal,DiscountAmount,TaxAmount,RoundOff,TotalAmount,AmountPaid,BalanceAmount,PaymentStatus,InvoiceStatus,CreatedAt) VALUES(@n,@c,SYSDATETIME(),@s,0,@t,0,@total,@paid,@balance,@status,'Completed',SYSDATETIME());SELECT CAST(SCOPE_IDENTITY() AS INT)",cn,tx)){
                                cmd.Parameters.AddWithValue("@n",no);cmd.Parameters.Add("@c",SqlDbType.Int).Value=(object)customerId??DBNull.Value;cmd.Parameters.AddWithValue("@s",sub);cmd.Parameters.AddWithValue("@t",tax);cmd.Parameters.AddWithValue("@total",total);cmd.Parameters.AddWithValue("@paid",received);cmd.Parameters.AddWithValue("@balance",total-received);cmd.Parameters.AddWithValue("@status",received==0?"Unpaid":received>=total?"Paid":"Partial");invoiceId=Convert.ToInt32(cmd.ExecuteScalar());}
                            using(var cmd=new SqlCommand(@"INSERT dbo.InvoiceItems(InvoiceId,MedicineId,BatchId,Quantity,UnitPrice,DiscountAmount,GSTPercent,TaxAmount,LineTotal) SELECT @id,MedicineId,BatchId,@qty,@price,0,@gst,@tax,@total FROM dbo.MedicineBatches WHERE BatchId=@batch; UPDATE dbo.MedicineBatches SET QuantityOnHand=QuantityOnHand-@qty WHERE BatchId=@batch AND QuantityOnHand>=@qty; IF @@ROWCOUNT=0 THROW 50001,'Insufficient stock.',1;",cn,tx)){
                                cmd.Parameters.AddWithValue("@id",invoiceId);cmd.Parameters.AddWithValue("@qty",count);cmd.Parameters.AddWithValue("@price",price);cmd.Parameters.AddWithValue("@gst",gst);cmd.Parameters.AddWithValue("@tax",tax);cmd.Parameters.AddWithValue("@total",total);cmd.Parameters.AddWithValue("@batch",batchId);cmd.ExecuteNonQuery();}
                            tx.Commit();MessageBox.Show("Invoice "+no+" saved. Total: "+total.ToString("C2"),"Sale complete",MessageBoxButtons.OK,MessageBoxIcon.Information);f.DialogResult=DialogResult.OK;f.Close();
                        }catch{tx.Rollback();throw;}
                    }
                }catch(Exception ex){MessageBox.Show(ex.Message,"Unable to complete sale",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
                if(f.ShowDialog(this)==DialogResult.OK)LoadGrid();
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