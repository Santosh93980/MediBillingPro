using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    internal static class AuditService
    {
        private static UserSession _session;
        internal static int? CurrentUserId { get { return _session == null ? (int?)null : _session.UserId; } }
        internal static string CurrentUserName { get { return _session == null ? "SYSTEM" : _session.UserName; } }
        internal static void SetSession(UserSession session) { _session = session; MediBillingPro.Domain.Security.CurrentUserContext.Set(session == null ? (int?)null : session.UserId, session == null ? "SYSTEM" : session.UserName); }

        internal static void SetContext(SqlConnection connection, string screenName)
        {
            if (connection == null || connection.State != ConnectionState.Open) return;
            using (var cmd = new SqlCommand(@"
EXEC sys.sp_set_session_context @key=N'UserId', @value=@uid;
EXEC sys.sp_set_session_context @key=N'UserName', @value=@uname;
EXEC sys.sp_set_session_context @key=N'ScreenName', @value=@screen;", connection))
            {
                cmd.Parameters.Add("@uid", SqlDbType.Int).Value = (object)CurrentUserId ?? DBNull.Value;
                cmd.Parameters.Add("@uname", SqlDbType.NVarChar, 80).Value = CurrentUserName;
                cmd.Parameters.Add("@screen", SqlDbType.NVarChar, 150).Value = screenName ?? "Unknown";
                cmd.ExecuteNonQuery();
            }
        }

        internal static void Write(string screen, string activity, string activityType, string details)
        {
            try
            {
                using (var cn = new SqlConnection(UserAccess.ConnectionString))
                using (var cmd = new SqlCommand(@"IF OBJECT_ID(N'dbo.AuditLogs',N'U') IS NOT NULL
INSERT dbo.AuditLogs(ActionedBy,ActionedByUserId,ActionedOn,ScreenName,Activity,ActivityType,Details,Category)
VALUES(@name,@id,SYSUTCDATETIME(),@screen,@activity,@type,@details,
CASE WHEN @screen IN ('Login','User Management','Role Management','Administration Audit') THEN 'Administration' ELSE 'Operations' END);", cn))
                {
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar, 150).Value = CurrentUserName;
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = (object)CurrentUserId ?? DBNull.Value;
                    cmd.Parameters.Add("@screen", SqlDbType.NVarChar, 150).Value = screen ?? "Unknown";
                    cmd.Parameters.Add("@activity", SqlDbType.NVarChar, 500).Value = activity ?? "";
                    cmd.Parameters.Add("@type", SqlDbType.NVarChar, 40).Value = activityType ?? "Information";
                    cmd.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value = (object)details ?? DBNull.Value;
                    cn.Open();
                    SetContext(cn, screen);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Auditing must not hide the original user action; database triggers also audit tracked tables.
            }
        }
    }

    internal static class ReportOutput
    {
        internal static void Show(Form owner, DataGridView grid, string reportName)
        {
            if (grid == null || grid.DataSource == null) { MessageBox.Show(owner, "There is no data to export."); return; }
            using (var form = new Form { Text = reportName + " - Print preview and export", StartPosition = FormStartPosition.CenterParent, Size = new Size(950, 650), MinimumSize = new Size(700, 450), BackColor = MediBillingTheme.Page })
            {
                var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(12, 8, 12, 4), WrapContents = false, BackColor = Color.White };
                var preview = MakeButton("Print preview"); var print = MakeButton("Print / PDF"); var excel = MakeButton("Export Excel (CSV)"); var word = MakeButton("Export Word"); var close = MakeButton("Close");
                top.Controls.Add(preview); top.Controls.Add(print); top.Controls.Add(excel); top.Controls.Add(word); top.Controls.Add(close);
                var info = new Label { Dock = DockStyle.Top, Height = 32, Padding = new Padding(12, 8, 0, 0), Text = "Excel export uses CSV. Word export creates a Word-compatible document. To save PDF, choose Microsoft Print to PDF in the printer dialog.", ForeColor = MediBillingTheme.Muted, BackColor = MediBillingTheme.Page };
                var table = BuildPrintableTable(grid, reportName);
                var browser = new WebBrowser { Dock = DockStyle.Fill, DocumentText = table };
                form.Controls.Add(browser); form.Controls.Add(info); form.Controls.Add(top);
                preview.Click += (s,e) => PrintGrid(grid, reportName, true);
                print.Click += (s,e) => PrintGrid(grid, reportName, false);
                excel.Click += (s,e) => ExportCsv(grid, reportName);
                word.Click += (s,e) => ExportWord(grid, reportName);
                close.Click += (s,e) => form.Close();
                form.ShowDialog(owner);
            }
        }

        private static Button MakeButton(string text)
        {
            var b = new Button { Text=text, AutoSize=true, Height=34, Margin=new Padding(4), Padding=new Padding(8,2,8,2), BackColor=MediBillingTheme.Primary, ForeColor=Color.White, FlatStyle=FlatStyle.Flat, UseVisualStyleBackColor=false };
            b.FlatAppearance.BorderSize=0; return b;
        }

        private static DataTable TableFromGrid(DataGridView grid)
        {
            var table = new DataTable();
            var visibleColumns = new System.Collections.Generic.List<DataGridViewColumn>();
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible) continue;
                visibleColumns.Add(column);
                table.Columns.Add(column.HeaderText);
            }
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var values = new System.Collections.Generic.List<object>();
                foreach (DataGridViewColumn column in visibleColumns)
                    values.Add(row.Cells[column.Index].Value ?? DBNull.Value);
                table.Rows.Add(values.ToArray());
            }
            return table;
        }

        private static string Escape(string s) { return (s ?? "").Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;"); }
        private static string BuildPrintableTable(DataGridView grid, string title)
        {
            var dt=TableFromGrid(grid); var b=new StringBuilder();
            b.Append("<html><head><meta charset='utf-8'><style>body{font-family:Segoe UI,Arial;margin:24px}h2{color:#153c31}table{border-collapse:collapse;width:100%;font-size:10pt}th{background:#087f5b;color:white}td,th{border:1px solid #ccd6d1;padding:6px;text-align:left}tr:nth-child(even){background:#f4f8f6}</style></head><body><h2>")
             .Append(Escape(title)).Append("</h2><p>Generated: ").Append(DateTime.Now.ToString("dd MMM yyyy HH:mm")).Append("</p><table><thead><tr>");
            foreach(DataColumn col in dt.Columns) b.Append("<th>").Append(Escape(col.ColumnName)).Append("</th>");
            b.Append("</tr></thead><tbody>");
            foreach(DataRow row in dt.Rows){b.Append("<tr>");foreach(DataColumn col in dt.Columns)b.Append("<td>").Append(Escape(Convert.ToString(row[col]))).Append("</td>");b.Append("</tr>");}
            return b.Append("</tbody></table></body></html>").ToString();
        }

        private static void ExportCsv(DataGridView grid, string title)
        {
            using(var save=new SaveFileDialog { Filter="Excel CSV (*.csv)|*.csv", FileName=SafeName(title)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmm")+".csv", AddExtension=true })
            {
                if(save.ShowDialog()!=DialogResult.OK)return;
                var dt=TableFromGrid(grid); var b=new StringBuilder();
                for(int i=0;i<dt.Columns.Count;i++){if(i>0)b.Append(',');b.Append(Csv(dt.Columns[i].ColumnName));}b.AppendLine();
                foreach(DataRow row in dt.Rows){for(int i=0;i<dt.Columns.Count;i++){if(i>0)b.Append(',');b.Append(Csv(Convert.ToString(row[i])));}b.AppendLine();}
                File.WriteAllText(save.FileName,b.ToString(),new UTF8Encoding(true));
                MessageBox.Show("Export saved successfully.","Export",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
        }
        private static string Csv(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }
        private static void ExportWord(DataGridView grid, string title)
        {
            using(var save=new SaveFileDialog { Filter="Word document (*.doc)|*.doc", FileName=SafeName(title)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmm")+".doc", AddExtension=true })
            {
                if(save.ShowDialog()!=DialogResult.OK)return;
                File.WriteAllText(save.FileName,BuildPrintableTable(grid,title),new UTF8Encoding(true));
                MessageBox.Show("Word-compatible document saved successfully.","Export",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
        }
        private static string SafeName(string s) { foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_'); return s.Replace(' ','_'); }
        private static void PrintGrid(DataGridView grid, string title, bool previewOnly)
        {
            var dt=TableFromGrid(grid); int rowIndex=0;
            using(var doc=new PrintDocument())
            {
                doc.DocumentName=title;
                doc.DefaultPageSettings.Landscape=true;
                doc.PrintPage+=(s,e)=>{
                    float left=e.MarginBounds.Left, top=e.MarginBounds.Top, width=e.MarginBounds.Width;
                    using(var font=new Font("Segoe UI",8))
                    using(var bold=new Font("Segoe UI",8,FontStyle.Bold))
                    using(var titleFont=new Font("Segoe UI",14,FontStyle.Bold))
                    using(var pen=new Pen(Color.LightGray))
                    {
                        e.Graphics.DrawString(title,titleFont,Brushes.DarkGreen,left,top); top+=30;
                        float colWidth=width/Math.Max(1,dt.Columns.Count); float x=left;
                        for(int i=0;i<dt.Columns.Count;i++){e.Graphics.DrawString(dt.Columns[i].ColumnName,bold,Brushes.Black,new RectangleF(x,top,colWidth,34));x+=colWidth;}
                        top+=36;
                        while(rowIndex<dt.Rows.Count && top<e.MarginBounds.Bottom-24)
                        {
                            x=left;
                            for(int i=0;i<dt.Columns.Count;i++){string val=Convert.ToString(dt.Rows[rowIndex][i]);if(val.Length>36)val=val.Substring(0,33)+"...";e.Graphics.DrawString(val,font,Brushes.Black,new RectangleF(x,top,colWidth,30));x+=colWidth;}
                            e.Graphics.DrawLine(pen,left,top+22,left+width,top+22);top+=26;rowIndex++;
                        }
                        e.HasMorePages=rowIndex<dt.Rows.Count;
                    }
                };
                if(previewOnly){using(var p=new PrintPreviewDialog { Document=doc, Width=1000, Height=700, StartPosition=FormStartPosition.CenterParent })p.ShowDialog();}
                else {using(var dlg=new PrintDialog { Document=doc, UseEXDialog=true })if(dlg.ShowDialog()==DialogResult.OK)doc.Print();}
            }
        }
    }

    public sealed class AuditReportForm : Form
    {
        private readonly string _category;
        private readonly DataGridView _grid=new DataGridView();
        private readonly DateTimePicker _from=new DateTimePicker { Format=DateTimePickerFormat.Short, Value=DateTime.Today.AddDays(-30) };
        private readonly DateTimePicker _to=new DateTimePicker { Format=DateTimePickerFormat.Short, Value=DateTime.Today };
        private readonly TextBox _actionedBy=new TextBox(), _screen=new TextBox(), _activity=new TextBox(), _type=new TextBox();
        private readonly Label _count=new Label { AutoSize=true, ForeColor=MediBillingTheme.Muted };
        public AuditReportForm(string category)
        {
            _category=category;
            Text=category+" Audit"; Dock=DockStyle.Fill; BackColor=MediBillingTheme.Page; Font=new Font("Segoe UI",9.5F); MinimumSize=new Size(760,460);
            var root=new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(16), ColumnCount=1, RowCount=3, BackColor=BackColor };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            root.Controls.Add(new Label { Text=category+" Audit Log",AutoSize=true,Font=new Font("Segoe UI",22,FontStyle.Bold),ForeColor=MediBillingTheme.Secondary,Margin=new Padding(0,0,0,12) },0,0);
            var filters=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,BackColor=BackColor,Margin=new Padding(0,0,0,10) };
            filters.Controls.Add(new Label{Text="From",AutoSize=true,Margin=new Padding(4,9,2,0)});filters.Controls.Add(_from);
            filters.Controls.Add(new Label{Text="To",AutoSize=true,Margin=new Padding(8,9,2,0)});filters.Controls.Add(_to);
            foreach(var item in new[]{Tuple.Create("Actioned by",_actionedBy),Tuple.Create("Screen",_screen),Tuple.Create("Activity",_activity),Tuple.Create("Type",_type)})
            { item.Item2.Width=130;new ToolTip().SetToolTip(item.Item2,item.Item1);filters.Controls.Add(item.Item2); }
            var search= new Button{Text="Apply filters",AutoSize=true,Height=30,BackColor=MediBillingTheme.Primary,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
            var export=new Button{Text="Print / Export",AutoSize=true,Height=30,BackColor=MediBillingTheme.Secondary,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
            search.Click+=(s,e)=>LoadData();export.Click+=(s,e)=>ReportOutput.Show(this,_grid,_category+" Audit");
            filters.Controls.Add(search);filters.Controls.Add(export);filters.Controls.Add(_count);root.Controls.Add(filters,0,1);
            _grid.Dock=DockStyle.Fill;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_grid.MultiSelect=false;_grid.BackgroundColor=Color.White;_grid.RowHeadersVisible=false;
            MediBillingPro.Desktop.UI.Common.UiStyle.StyleGrid(_grid);root.Controls.Add(_grid,0,2);Controls.Add(root);
            Load += (s,e)=>LoadData();
        }
        private void LoadData()
        {
            if(_to.Value.Date<_from.Value.Date){MessageBox.Show("The To date must be on or after the From date.");return;}
            try{
                using(var cn=new SqlConnection(UserAccess.ConnectionString))
                using(var cmd=new SqlCommand(@"SELECT AuditLogId,ActionedBy,ActionedOn,ScreenName,Activity,ActivityType,Details,Category
FROM dbo.AuditLogs
WHERE Category=@cat AND ActionedOn>=@from AND ActionedOn<@toExclusive
AND (@by='' OR ActionedBy LIKE @byLike) AND (@screen='' OR ScreenName LIKE @screenLike)
AND (@activity='' OR Activity LIKE @activityLike) AND (@type='' OR ActivityType LIKE @typeLike)
ORDER BY ActionedOn DESC",cn))
                using(var da=new SqlDataAdapter(cmd)){
                    cmd.Parameters.Add("@cat",SqlDbType.NVarChar,30).Value=_category;
                    cmd.Parameters.Add("@from",SqlDbType.DateTime2).Value=_from.Value.Date.ToUniversalTime();
                    cmd.Parameters.Add("@toExclusive",SqlDbType.DateTime2).Value=_to.Value.Date.AddDays(1).ToUniversalTime();
                    AddFilter(cmd,"by",_actionedBy.Text);AddFilter(cmd,"screen",_screen.Text);AddFilter(cmd,"activity",_activity.Text);AddFilter(cmd,"type",_type.Text);
                    var dt=new DataTable();da.Fill(dt);_grid.DataSource=dt;_count.Text=dt.Rows.Count+" records";
                    if(_grid.Columns.Contains("AuditLogId"))_grid.Columns["AuditLogId"].Visible=false;
                    if(_grid.Columns.Contains("ActionedOn"))_grid.Columns["ActionedOn"].DefaultCellStyle.Format="dd MMM yyyy HH:mm:ss";
                    if(_grid.Columns.Contains("Details"))_grid.Columns["Details"].Width=300;
                }
            }catch(Exception ex){MessageBox.Show("Could not load audit logs. Run Database/Scripts/005_AuditAndRecordMetadata.sql first.\n\n"+ex.Message,"Audit",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        }
        private static void AddFilter(SqlCommand cmd,string name,string value)
        {string v=(value??"").Trim();cmd.Parameters.Add("@"+name,SqlDbType.NVarChar,200).Value=v;cmd.Parameters.Add("@"+name+"Like",SqlDbType.NVarChar,202).Value="%"+v+"%";}
    }
}
