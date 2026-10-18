using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    // Fast counter workspace: scanner input, name search, batch-safe cart and change calculator.
    public sealed class PharmacistCounterForm : Form
    {
        private readonly UserSession _session;
        private readonly string _cs;
        private readonly TextBox _search = new TextBox();
        private readonly DataGridView _results = new DataGridView();
        private readonly DataGridView _cartGrid = new DataGridView();
        private readonly DataTable _cart = new DataTable();
        private readonly NumericUpDown _received = new NumericUpDown();
        private readonly ComboBox _payment = new ComboBox();
        private readonly Label _totalLabel = new Label();
        private readonly Label _changeLabel = new Label();
        private readonly Label _balanceLabel = new Label();
        private readonly Label _status = new Label();
        private readonly Color _green = Color.FromArgb(8,127,91);
        private readonly Color _dark = Color.FromArgb(21,60,49);
        private readonly Color _page = Color.FromArgb(244,248,246);
        private string _upiId = "YOUR-PHARMACY-UPI-ID";

        public PharmacistCounterForm(UserSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            var cs=ConfigurationManager.ConnectionStrings["MediBillingProDB"];
            if(cs==null || string.IsNullOrWhiteSpace(cs.ConnectionString)) throw new InvalidOperationException("MediBillingProDB connection string is missing.");
            _cs=cs.ConnectionString;
            Text="Pharmacy Counter | Fast Billing"; Dock=DockStyle.Fill; BackColor=_page; Font=new Font("Segoe UI",9.5F);
            BuildCart(); BuildLayout(); LoadResults("");
        }

        private SqlConnection OpenConnection(){var c=new SqlConnection(_cs);c.Open();return c;}

        private void BuildCart()
        {
            _cart.Columns.Add("BatchId",typeof(int)); _cart.Columns.Add("MedicineId",typeof(int));
            _cart.Columns.Add("Item",typeof(string)); _cart.Columns.Add("Batch",typeof(string));
            _cart.Columns.Add("Expiry",typeof(DateTime)); _cart.Columns.Add("Qty",typeof(int));
            _cart.Columns.Add("Stock",typeof(int)); _cart.Columns.Add("UnitPrice",typeof(decimal));
            _cart.Columns.Add("GSTPercent",typeof(decimal)); _cart.Columns.Add("Discount",typeof(decimal));
        }

        private void BuildLayout()
        {
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),ColumnCount=2,RowCount=1,BackColor=_page};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,57));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,43));
            var left=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=_page};
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));left.RowStyles.Add(new RowStyle(SizeType.AutoSize));left.RowStyles.Add(new RowStyle(SizeType.AutoSize));left.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            left.Controls.Add(new Label{Text="Counter billing",AutoSize=true,Font=new Font("Segoe UI",21,FontStyle.Bold),ForeColor=_dark,Margin=new Padding(0,0,0,4)},0,0);
            left.Controls.Add(new Label{Text="Scan a barcode or search medicine / cosmetics. Press Enter to search.",AutoSize=true,ForeColor=Color.FromArgb(91,111,101),Margin=new Padding(0,0,0,12)},0,1);
            var searchRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Height=42,Margin=new Padding(0,0,0,12)};
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));
            _search.Dock=DockStyle.Fill;_search.Font=new Font("Segoe UI",12);_search.Margin=new Padding(0,0,8,0);
            _search.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){LoadResults(_search.Text.Trim());e.SuppressKeyPress=true;}};
            var search=new Button{Text="Search",Dock=DockStyle.Fill,BackColor=_green,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
            search.Click+=(s,e)=>LoadResults(_search.Text.Trim());
            var scanner=new Button{Text="▣ Scanner",Dock=DockStyle.Fill};
            scanner.Click+=(s,e)=>{_search.Focus();_search.SelectAll();_status.Text="Barcode scanner ready: scan into the search box and press Enter.";};
            searchRow.Controls.Add(_search,0,0);searchRow.Controls.Add(search,1,0);searchRow.Controls.Add(scanner,2,0);left.Controls.Add(searchRow,0,2);
            StyleGrid(_results);_results.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)AddSelectedResult(e.RowIndex);};
            _results.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter&&_results.CurrentRow!=null){AddSelectedResult(_results.CurrentRow.Index);e.SuppressKeyPress=true;}};
            left.Controls.Add(_results,0,3);
            left.RowCount=5; left.RowStyles.Add(new RowStyle(SizeType.Absolute,28));

            var right=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,BackColor=Color.White,Padding=new Padding(12)};
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));right.RowStyles.Add(new RowStyle(SizeType.AutoSize));right.RowStyles.Add(new RowStyle(SizeType.AutoSize));right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.Controls.Add(new Label{Text="Current bill",AutoSize=true,Font=new Font("Segoe UI",17,FontStyle.Bold),ForeColor=_dark,Margin=new Padding(0,0,0,10)},0,0);
            StyleGrid(_cartGrid);
            // Define columns explicitly before binding so column lookup never depends on auto-generation timing.
            _cartGrid.AutoGenerateColumns = false;
            AddCartColumn("BatchId", "BatchId", false, true);
            AddCartColumn("MedicineId", "MedicineId", false, true);
            AddCartColumn("Item", "Medicine / product", true, true);
            AddCartColumn("Batch", "Batch", true, true);
            AddCartColumn("Expiry", "Expiry", true, true);
            AddCartColumn("Qty", "Qty", true, false);
            AddCartColumn("Stock", "Stock", false, true);
            AddCartColumn("UnitPrice", "Price", true, true);
            AddCartColumn("GSTPercent", "GSTPercent", false, true);
            AddCartColumn("Discount", "Discount", false, true);
            _cartGrid.Columns["Expiry"].DefaultCellStyle.Format = "dd-MMM-yyyy";
            _cartGrid.Columns["UnitPrice"].DefaultCellStyle.Format = "N2";
            _cartGrid.DataSource = _cart;
            _cartGrid.CellEndEdit+=(s,e)=>{if(e.RowIndex>=0&&_cart.Columns.Contains("Qty")){var row=_cart.Rows[e.RowIndex];int q; if(!int.TryParse(Convert.ToString(row["Qty"]),out q)||q<1)row["Qty"]=1;else if(q>Convert.ToInt32(row["Stock"]))row["Qty"]=Convert.ToInt32(row["Stock"]);UpdateTotals();}};
            right.Controls.Add(_cartGrid,0,1);
            var remove=new Button{Text="Remove selected item",AutoSize=true};remove.Click+=(s,e)=>{if(_cartGrid.CurrentRow!=null&&_cartGrid.CurrentRow.Index>=0){_cart.Rows.RemoveAt(_cartGrid.CurrentRow.Index);UpdateTotals();}};right.Controls.Add(remove,0,2);
            var totals=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,Margin=new Padding(0,10,0,10)};
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            _totalLabel.Text="Total: ₹0.00";_totalLabel.Font=new Font("Segoe UI",16,FontStyle.Bold);_totalLabel.ForeColor=_green;
            _balanceLabel.Text="Balance due: ₹0.00";_changeLabel.Text="Change to return: ₹0.00";
            totals.Controls.Add(_totalLabel,0,0);totals.SetColumnSpan(_totalLabel,2);
            totals.Controls.Add(new Label{Text="Payment mode",AutoSize=true},0,1);_payment.DropDownStyle=ComboBoxStyle.DropDownList;_payment.Items.AddRange(new object[]{"Cash","UPI","Card","Bank transfer","Mixed"});_payment.SelectedIndex=0;_payment.Dock=DockStyle.Fill;_payment.SelectedIndexChanged+=(s,e)=>UpdateUpiVisibility();totals.Controls.Add(_payment,1,1);
            totals.Controls.Add(new Label{Text="Amount received (₹)",AutoSize=true},0,2);_received.DecimalPlaces=2;_received.Maximum=100000000;_received.Minimum=0;_received.Dock=DockStyle.Fill;_received.ValueChanged+=(s,e)=>UpdateTotals();totals.Controls.Add(_received,1,2);
            right.Controls.Add(totals,0,3);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.LeftToRight,WrapContents=true};
            var upi=new Button{Text="UPI payment details",AutoSize=true,Height=38};upi.Click+=(s,e)=>ShowUpiDetails();
            var complete=new Button{Text="Complete bill",AutoSize=true,Height=38,BackColor=_green,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};complete.Click+=(s,e)=>CompleteBill();
            var clear=new Button{Text="New / clear bill",AutoSize=true,Height=38};clear.Click+=(s,e)=>{_cart.Clear();_received.Value=0;UpdateTotals();};
            footer.Controls.Add(upi);footer.Controls.Add(complete);footer.Controls.Add(clear);footer.Controls.Add(_changeLabel);footer.Controls.Add(_balanceLabel);right.Controls.Add(footer,0,4);
            _status.Dock=DockStyle.Bottom;_status.Height=28;_status.ForeColor=Color.FromArgb(91,111,101);_status.Text="Double-click an item to add it to the bill.";
            left.Controls.Add(_status,0,4);
            root.Controls.Add(left,0,0);root.Controls.Add(right,1,0);Controls.Add(root);
            UpdateTotals();
        }

        private void AddCartColumn(string name, string header, bool visible, bool readOnly)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = name,
                HeaderText = header,
                Visible = visible,
                ReadOnly = readOnly,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            _cartGrid.Columns.Add(column);
        }

        private void StyleGrid(DataGridView g)
        {
            g.Dock=DockStyle.Fill;g.ReadOnly=false;g.AllowUserToAddRows=false;g.AllowUserToDeleteRows=false;g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;g.SelectionMode=DataGridViewSelectionMode.FullRowSelect;g.MultiSelect=false;g.BackgroundColor=Color.White;g.BorderStyle=BorderStyle.None;g.RowHeadersVisible=false;g.AutoGenerateColumns=true;
            g.EnableHeadersVisualStyles=false;g.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(231,246,239);g.ColumnHeadersDefaultCellStyle.ForeColor=_dark;g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);g.RowTemplate.Height=30;
        }

        private void LoadResults(string q)
        {
            const string sql=@"
SELECT TOP (100) m.MedicineId,b.BatchId,m.MedicineName,m.GenericName,m.Manufacturer,b.BatchNumber,b.ExpiryDate,b.SellingPrice,b.GSTPercent,b.QuantityOnHand
FROM dbo.Medicines m INNER JOIN dbo.MedicineBatches b ON b.MedicineId=m.MedicineId
WHERE m.IsActive=1 AND b.QuantityOnHand>0 AND b.ExpiryDate>=CONVERT(date,GETDATE())
AND (@q='' OR m.MedicineName LIKE @like OR m.GenericName LIKE @like OR m.Manufacturer LIKE @like OR m.Barcode=@barcode OR b.BatchNumber LIKE @like)
ORDER BY CASE WHEN m.Barcode=@barcode AND @barcode<>'' THEN 0 ELSE 1 END,b.ExpiryDate,m.MedicineName;";
            using(var cn=OpenConnection())using(var cmd=new SqlCommand(sql,cn))using(var da=new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@q",SqlDbType.NVarChar,200).Value=q;
                cmd.Parameters.Add("@like",SqlDbType.NVarChar,202).Value="%"+q+"%";
                cmd.Parameters.Add("@barcode",SqlDbType.NVarChar,100).Value=q;
                var dt=new DataTable();da.Fill(dt);_results.DataSource=dt;
                if(_results.Columns.Contains("MedicineId"))_results.Columns["MedicineId"].Visible=false;
                if(_results.Columns.Contains("BatchId"))_results.Columns["BatchId"].Visible=false;
                if(_results.Columns.Contains("GenericName"))_results.Columns["GenericName"].HeaderText="Generic";
                if(_results.Columns.Contains("Manufacturer"))_results.Columns["Manufacturer"].HeaderText="Brand / maker";
                if(_results.Columns.Contains("BatchNumber"))_results.Columns["BatchNumber"].HeaderText="Batch";
                if(_results.Columns.Contains("ExpiryDate"))_results.Columns["ExpiryDate"].DefaultCellStyle.Format="dd-MMM-yyyy";
                if(_results.Columns.Contains("SellingPrice"))_results.Columns["SellingPrice"].DefaultCellStyle.Format="N2";
                _status.Text=dt.Rows.Count+" eligible in-stock batch(es). Expired batches are excluded.";
            }
        }

        private void AddSelectedResult(int rowIndex)
        {
            if(rowIndex<0||rowIndex>=_results.Rows.Count)return;
            var r=_results.Rows[rowIndex];
            if(r.Cells["BatchId"].Value==null)return;
            int batchId=Convert.ToInt32(r.Cells["BatchId"].Value),medicineId=Convert.ToInt32(r.Cells["MedicineId"].Value);
            int stock=Convert.ToInt32(r.Cells["QuantityOnHand"].Value);
            var existing=_cart.Rows.Cast<DataRow>().FirstOrDefault(x=>Convert.ToInt32(x["BatchId"])==batchId);
            if(existing!=null){if(Convert.ToInt32(existing["Qty"])<stock)existing["Qty"]=Convert.ToInt32(existing["Qty"])+1;else MessageBox.Show("No more stock is available for this batch.");}
            else _cart.Rows.Add(batchId,medicineId,Convert.ToString(r.Cells["MedicineName"].Value),Convert.ToString(r.Cells["BatchNumber"].Value),Convert.ToDateTime(r.Cells["ExpiryDate"].Value),1,stock,Convert.ToDecimal(r.Cells["SellingPrice"].Value),Convert.ToDecimal(r.Cells["GSTPercent"].Value),0m);
            UpdateTotals();_search.Focus();_search.SelectAll();
        }

        private decimal BillTotal()
        {
            decimal total=0;
            foreach(DataRow r in _cart.Rows){decimal line=Convert.ToDecimal(r["UnitPrice"])*Convert.ToInt32(r["Qty"]);decimal tax=line*Convert.ToDecimal(r["GSTPercent"])/100m;total+=line+tax-Convert.ToDecimal(r["Discount"]);}
            return Math.Round(total,2);
        }
        private void UpdateTotals()
        {
            if(_totalLabel==null)return;
            decimal total=BillTotal(),received=_received.Value;
            _totalLabel.Text="Total: ₹"+total.ToString("N2");
            _changeLabel.Text="Change to return: ₹"+Math.Max(0,received-total).ToString("N2");
            _balanceLabel.Text="Balance due: ₹"+Math.Max(0,total-received).ToString("N2");
        }
        private void UpdateUpiVisibility(){ }

        private void ShowUpiDetails()
        {
            decimal amount=BillTotal();if(amount<=0){MessageBox.Show("Add products to the bill first.");return;}
            string intent="upi://pay?pa="+Uri.EscapeDataString(_upiId)+"&pn="+Uri.EscapeDataString("MediBilling Pro Pharmacy")+"&am="+amount.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"&cu=INR&tn="+Uri.EscapeDataString("Pharmacy bill");
            using(var f=new Form{Text="UPI payment",Width=520,Height=300,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false})
            {
                var label=new Label{Text="Bill amount: ₹"+amount.ToString("N2")+"\nConfigured UPI ID: "+_upiId+"\n\nUPI intent (configure the pharmacy's real UPI ID in code/settings):",Dock=DockStyle.Top,Height=95,Padding=new Padding(12)};
                var link=new TextBox{Text=intent,Dock=DockStyle.Top,ReadOnly=true};
                var note=new Label{Text="This is a UPI intent helper, not a connected payment gateway. Confirm payment in your merchant app before marking the amount received.",Dock=DockStyle.Top,Height=54,Padding=new Padding(12),ForeColor=Color.DarkRed};
                var copy=new Button{Text="Copy UPI intent",Dock=DockStyle.Bottom,Height=38};
                copy.Click+=(s,e)=>{Clipboard.SetText(intent);MessageBox.Show("UPI intent copied.");};
                f.Controls.Add(copy);f.Controls.Add(note);f.Controls.Add(link);f.Controls.Add(label);f.ShowDialog(this);
            }
        }

        private void CompleteBill()
        {
            try
            {
                if(_cart.Rows.Count==0)throw new Exception("Add at least one medicine or product.");
                decimal total=BillTotal(),received=_received.Value;
                if(received>total+0.001m && _payment.SelectedItem.ToString()!="Cash")throw new Exception("For non-cash payments, enter the amount actually received. Change is calculated for cash.");
                if(_payment.SelectedItem.ToString()=="UPI"&&received<total)throw new Exception("Confirm the UPI payment in your merchant app and enter the received amount before completing.");
                if(MessageBox.Show("Complete bill for ₹"+total.ToString("N2")+"?\nPayment mode: "+_payment.SelectedItem+"\nReceived: ₹"+received.ToString("N2")+"\nChange: ₹"+Math.Max(0,received-total).ToString("N2"),"Confirm sale",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
                using(var cn=OpenConnection())using(var tx=cn.BeginTransaction())
                {
                    try
                    {
                        string invoiceNo="INV-"+DateTime.Now.ToString("yyyyMMddHHmmssfff");int invoiceId;
                        decimal subtotal=_cart.Rows.Cast<DataRow>().Sum(r=>Convert.ToDecimal(r["UnitPrice"])*Convert.ToInt32(r["Qty"]));
                        decimal tax=_cart.Rows.Cast<DataRow>().Sum(r=>Convert.ToDecimal(r["UnitPrice"])*Convert.ToInt32(r["Qty"])*Convert.ToDecimal(r["GSTPercent"])/100m);
                        using(var cmd=new SqlCommand(@"INSERT dbo.Invoices(InvoiceNumber,CustomerId,InvoiceDate,SubTotal,DiscountAmount,TaxAmount,RoundOff,TotalAmount,AmountPaid,BalanceAmount,PaymentStatus,InvoiceStatus,CreatedBy,CreatedAt,PaymentMethod)
VALUES(@n,NULL,SYSDATETIME(),@sub,0,@tax,0,@total,@paid,@balance,@status,'Completed',@user,SYSDATETIME(),@mode);SELECT CAST(SCOPE_IDENTITY() AS INT)",cn,tx))
                        {
                            cmd.Parameters.AddWithValue("@n",invoiceNo);cmd.Parameters.AddWithValue("@sub",subtotal);cmd.Parameters.AddWithValue("@tax",tax);cmd.Parameters.AddWithValue("@total",total);
                            cmd.Parameters.AddWithValue("@paid",Math.Min(received,total));cmd.Parameters.AddWithValue("@balance",Math.Max(0,total-received));cmd.Parameters.AddWithValue("@status",received>=total?"Paid":received>0?"Partial":"Unpaid");
                            cmd.Parameters.AddWithValue("@user",_session.UserId);cmd.Parameters.AddWithValue("@mode",Convert.ToString(_payment.SelectedItem));invoiceId=Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        foreach(DataRow r in _cart.Rows)
                        {
                            int qty=Convert.ToInt32(r["Qty"]);decimal price=Convert.ToDecimal(r["UnitPrice"]),gst=Convert.ToDecimal(r["GSTPercent"]),line=price*qty,taxLine=line*gst/100m;
                            using(var cmd=new SqlCommand(@"UPDATE dbo.MedicineBatches SET QuantityOnHand=QuantityOnHand-@qty WHERE BatchId=@batch AND QuantityOnHand>=@qty AND ExpiryDate>=CONVERT(date,GETDATE());
IF @@ROWCOUNT=0 THROW 50001,'Stock changed or batch expired. Refresh and try again.',1;
INSERT dbo.InvoiceItems(InvoiceId,MedicineId,BatchId,Quantity,UnitPrice,DiscountAmount,GSTPercent,TaxAmount,LineTotal) VALUES(@invoice,@medicine,@batch,@qty,@price,0,@gst,@tax,@line)",cn,tx))
                            {
                                cmd.Parameters.AddWithValue("@qty",qty);cmd.Parameters.AddWithValue("@batch",Convert.ToInt32(r["BatchId"]));cmd.Parameters.AddWithValue("@invoice",invoiceId);cmd.Parameters.AddWithValue("@medicine",Convert.ToInt32(r["MedicineId"]));cmd.Parameters.AddWithValue("@price",price);cmd.Parameters.AddWithValue("@gst",gst);cmd.Parameters.AddWithValue("@tax",taxLine);cmd.Parameters.AddWithValue("@line",line+taxLine);cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();MessageBox.Show("Bill saved: "+invoiceNo+"\nTotal: ₹"+total.ToString("N2")+"\nReceived: ₹"+Math.Min(received,total).ToString("N2")+"\nChange: ₹"+Math.Max(0,received-total).ToString("N2"),"Sale completed",MessageBoxButtons.OK,MessageBoxIcon.Information);
                        _cart.Clear();_received.Value=0;LoadResults(_search.Text.Trim());UpdateTotals();
                    }
                    catch{tx.Rollback();throw;}
                }
            }
            catch(Exception ex){MessageBox.Show(ex.Message,"Billing",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}
