using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.Forms
{
    public sealed class UserSession
    {
        public int UserId { get; private set; }
        public string UserName { get; private set; }
        public string DisplayName { get; private set; }
        public string Role { get; private set; }

        public UserSession(int id, string userName, string displayName, string role)
        { UserId = id; UserName = userName; DisplayName = displayName; Role = role; }

        public bool IsAdmin { get { return Role == "Super Admin" || Role == "Admin" || Role == "Pharmacy Owner"; } }
        public bool IsPharmacist { get { return Role == "Pharmacist"; } }
        public bool CanBill { get { return IsAdmin || IsPharmacist || Role == "Cashier"; } }
        public bool CanManageInventory { get { return IsAdmin || Role == "Inventory Manager" || IsPharmacist; } }
    }

    internal static class UserAccess
    {
        internal static string ConnectionString
        {
            get {
                var cs = ConfigurationManager.ConnectionStrings["MediBillingProDB"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                    throw new InvalidOperationException("Connection string 'MediBillingProDB' was not found in App.config.");
                return cs.ConnectionString;
            }
        }

        internal static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? "")));
        }

        internal static void EnsureSchemaAndSeed()
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.AppUsers',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AppUsers(
  UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
  UserName NVARCHAR(80) NOT NULL,
  DisplayName NVARCHAR(150) NOT NULL,
  PasswordHash NVARCHAR(100) NOT NULL,
  RoleName NVARCHAR(40) NOT NULL,
  IsActive BIT NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT(1),
  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT(SYSDATETIME()),
  CONSTRAINT UQ_AppUsers_UserName UNIQUE(UserName)
 );
END;
IF COL_LENGTH(N'dbo.AppUsers',N'FirstName') IS NULL ALTER TABLE dbo.AppUsers ADD FirstName NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'LastName') IS NULL ALTER TABLE dbo.AppUsers ADD LastName NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'MobileNumber') IS NULL ALTER TABLE dbo.AppUsers ADD MobileNumber NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'Email') IS NULL ALTER TABLE dbo.AppUsers ADD Email NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'Address') IS NULL ALTER TABLE dbo.AppUsers ADD [Address] NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyName') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyName NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyAddress') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyAddress NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyLicenseNumber') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyLicenseNumber NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.AppUsers',N'PharmacyGSTIN') IS NULL ALTER TABLE dbo.AppUsers ADD PharmacyGSTIN NVARCHAR(20) NULL;
IF OBJECT_ID(N'dbo.Roles',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.Roles(
  RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
  RoleName NVARCHAR(40) NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE,
  Description NVARCHAR(200) NULL,
  IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT(1)
 );
END;
IF OBJECT_ID(N'dbo.Invoices',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Invoices',N'PaymentMethod') IS NULL
 ALTER TABLE dbo.Invoices ADD PaymentMethod NVARCHAR(40) NULL;
UPDATE dbo.AppUsers SET RoleName='Super Admin', FirstName=COALESCE(NULLIF(FirstName,''),'System'), LastName=COALESCE(NULLIF(LastName,''),'Administrator') WHERE UserName='admin' AND RoleName='Admin';";
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                cmd.ExecuteNonQuery();
                SeedRole(cn, "Super Admin", "Full system administration and user management");
                SeedRole(cn, "Pharmacy Owner", "Manages a pharmacy and its staff");
                SeedRole(cn, "Pharmacist", "Pharmacy operations, inventory and counter billing");
                SeedRole(cn, "Cashier", "Counter billing and customer payments");
                SeedRole(cn, "Inventory Manager", "Medicines, suppliers and purchase stock");
                SeedRole(cn, "Admin", "Legacy administrator role");
                SeedUser(cn, "admin", "System", "Administrator", "Admin@123", "Super Admin");
                SeedUser(cn, "pharmacist", "Demo", "Pharmacist", "Pharma@123", "Pharmacist");
                SeedUser(cn, "cashier", "Demo", "Cashier", "Cashier@123", "Cashier");
                SeedUser(cn, "inventory", "Demo", "Inventory Manager", "Stock@123", "Inventory Manager");
            }
        }

        private static void SeedRole(SqlConnection cn, string name, string description)
        {
            using (var cmd = new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName=@n)
INSERT dbo.Roles(RoleName,Description,IsActive) VALUES(@n,@d,1);", cn))
            {
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@d", description);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedUser(SqlConnection cn, string user, string first, string last, string password, string role)
        {
            using (var cmd = new SqlCommand(@"
IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE UserName=@u)
INSERT dbo.AppUsers(UserName,DisplayName,PasswordHash,RoleName,FirstName,LastName,IsActive)
VALUES(@u,@d,@p,@r,@f,@l,1);", cn))
            {
                cmd.Parameters.AddWithValue("@u", user);
                cmd.Parameters.AddWithValue("@d", (first + " " + last).Trim());
                cmd.Parameters.AddWithValue("@p", HashPassword(password));
                cmd.Parameters.AddWithValue("@r", role);
                cmd.Parameters.AddWithValue("@f", first);
                cmd.Parameters.AddWithValue("@l", last);
                cmd.ExecuteNonQuery();
            }
        }

        internal static DataTable GetActiveRoles()
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var da = new SqlDataAdapter("SELECT RoleName FROM dbo.Roles WHERE IsActive=1 ORDER BY CASE RoleName WHEN 'Super Admin' THEN 0 WHEN 'Pharmacy Owner' THEN 1 WHEN 'Pharmacist' THEN 2 WHEN 'Cashier' THEN 3 ELSE 4 END, RoleName", cn))
            { var dt = new DataTable(); da.Fill(dt); return dt; }
        }

        internal static UserSession Authenticate(string username, string password)
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(@"SELECT UserId,UserName,COALESCE(NULLIF(DisplayName,''),CONCAT(FirstName,' ',LastName)) DisplayName,RoleName
FROM dbo.AppUsers WHERE UserName=@u AND PasswordHash=@p AND IsActive=1", cn))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 80).Value = (username ?? "").Trim();
                cmd.Parameters.Add("@p", SqlDbType.NVarChar, 100).Value = HashPassword(password);
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? new UserSession(Convert.ToInt32(r["UserId"]), Convert.ToString(r["UserName"]),
                        Convert.ToString(r["DisplayName"]), Convert.ToString(r["RoleName"])) : null;
            }
        }
    }

    public sealed class LoginForm : Form
    {
        private readonly TextBox _username = new TextBox();
        private readonly TextBox _password = new TextBox();
        private bool _schemaReady;
        public UserSession Session { get; private set; }

        public LoginForm()
        {
            Text = "MediBilling Pro | Sign in";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(460, 440);
            BackColor = MediBillingTheme.Page;
            Font = new Font("Segoe UI", 10F);
            var title = new Label { Text = "✚ MediBilling Pro", AutoSize = true, Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = MediBillingTheme.Secondary, Location = new Point(34,28) };
            var sub = new Label { Text = "Sign in to your pharmacy workspace", AutoSize = true, ForeColor = MediBillingTheme.Muted, Location = new Point(38,75) };
            Controls.Add(title); Controls.Add(sub);
            AddLabel("Username", 38, 125); _username.SetBounds(38, 151, 380, 34); Controls.Add(_username);
            AddLabel("Password", 38, 199); _password.SetBounds(38, 225, 380, 34); _password.UseSystemPasswordChar = true; Controls.Add(_password);
            var login = new Button { Text = "Sign in", Bounds = new Rectangle(38, 280, 380, 42), BackColor = MediBillingTheme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false };
            login.FlatAppearance.BorderSize = 0; login.FlatAppearance.MouseOverBackColor = MediBillingTheme.PrimaryHover;
            Resize += (s, e) => { int width = Math.Max(260, ClientSize.Width - 76); _username.SetBounds(38,151,width,34); _password.SetBounds(38,225,width,34); login.SetBounds(38,280,width,42); };
            login.Click += (s,e) => SignIn(); Controls.Add(login); AcceptButton = login;
            Controls.Add(new Label { Text = "First run demo: admin / Admin@123", AutoSize = false, Bounds = new Rectangle(38, 340, 385, 30), ForeColor = MediBillingTheme.Muted, Font = new Font("Segoe UI", 8.5F) });
            Shown += (s,e) => {
                try { UserAccess.EnsureSchemaAndSeed(); _schemaReady = true; _username.Focus(); }
                catch(Exception ex) { MessageBox.Show("Cannot initialize user access. Check SQL Server and the App.config connection string.\n\n" + ex.Message, "Database connection", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
        }

        private void AddLabel(string text, int x, int y) { Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x,y), ForeColor = MediBillingTheme.Secondary, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) }); }
        private void SignIn()
        {
            if (!_schemaReady) { MessageBox.Show("Database setup has not completed. Check the database connection and reopen the application."); return; }
            if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrEmpty(_password.Text)) { MessageBox.Show("Enter both username and password."); return; }
            try {
                Session = UserAccess.Authenticate(_username.Text, _password.Text);
                if (Session == null) { AuditService.Write("Login", "Failed login attempt", "Security", "Username="+_username.Text.Trim()); MessageBox.Show("Username or password is incorrect, or the account is inactive.", "Sign in failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                DialogResult = DialogResult.OK; Close();
            } catch(Exception ex) { MessageBox.Show(ex.Message, "Sign in failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    public sealed class UserManagementForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _search = new TextBox();
        public UserManagementForm()
        {
            Text = "User Management"; Dock = DockStyle.Fill; BackColor = MediBillingTheme.Page; Font = new Font("Segoe UI",9.5F); AutoScaleMode = AutoScaleMode.Dpi; MinimumSize = new Size(760, 460);
            var root = new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(18), RowCount=3, ColumnCount=1, BackColor=BackColor };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            root.Controls.Add(new Label { Text="Users & Roles", AutoSize=true, Font=new Font("Segoe UI",22,FontStyle.Bold), ForeColor=MediBillingTheme.Secondary, Margin=new Padding(0,0,0,14) },0,0);
            var actions = new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true, WrapContents=true, Padding=new Padding(0,4,0,4), Margin=new Padding(0,0,0,12), BackColor=MediBillingTheme.Page };
            var add = MakeButton("Create user", MediBillingTheme.Primary);
            var edit = MakeButton("Edit selected", MediBillingTheme.Secondary);
            var toggle = MakeButton("Enable / Disable", MediBillingTheme.Secondary);
            var refresh = MakeButton("Refresh", Color.FromArgb(241,245,249)); refresh.ForeColor = MediBillingTheme.Secondary;
            var print = MakeButton("Print / Export", MediBillingTheme.Primary);
            _search.Width=220; _search.Height=32; _search.Margin=new Padding(4,7,4,4); _search.PlaceholderText="Search name, username, mobile";
            add.Click += (s,e)=>EditUser(null); edit.Click += (s,e)=>EditSelected(); toggle.Click += (s,e)=>ToggleUser(); refresh.Click += (s,e)=>LoadUsers();
            _search.TextChanged += (s,e)=>LoadUsers();
            print.Click += (s,e)=>ReportOutput.Show(this,_grid,"User Management");
            actions.Controls.Add(add); actions.Controls.Add(edit); actions.Controls.Add(toggle); actions.Controls.Add(refresh); actions.Controls.Add(print); actions.Controls.Add(_search); root.Controls.Add(actions,0,1);
            _grid.Dock=DockStyle.Fill; _grid.ReadOnly=true; _grid.AllowUserToAddRows=false; _grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect=false; _grid.BackgroundColor=Color.White; _grid.RowHeadersVisible=false;
            MediBillingPro.Desktop.UI.Common.UiStyle.StyleGrid(_grid);
            root.Controls.Add(_grid,0,2); Controls.Add(root); LoadUsers();
        }

        private Button MakeButton(string text, Color color)
        {
            var b=new Button { Text=text, AutoSize=true, Height=36, Margin=new Padding(4), Padding=new Padding(10,2,10,2), BackColor=color, ForeColor=Color.White, FlatStyle=FlatStyle.Flat, UseVisualStyleBackColor=false, Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize=0; return b;
        }

        private void LoadUsers()
        {
            try {
                using(var cn=new SqlConnection(UserAccess.ConnectionString))
                using(var cmd=new SqlCommand(@"SELECT UserId,UserName,FirstName,LastName,MobileNumber,Email,[Address],RoleName,
CASE WHEN PharmacyName IS NULL OR PharmacyName='' THEN '—' ELSE PharmacyName END PharmacyName,
PharmacyAddress,PharmacyLicenseNumber,PharmacyGSTIN,CreatedByName,CreatedDate,UpdatedBy,UpdatedDate,
CASE WHEN IsActive=1 THEN 'Active' ELSE 'Disabled' END AccountStatus,CreatedAt
FROM dbo.AppUsers
WHERE (@q='' OR UserName LIKE @like OR FirstName LIKE @like OR LastName LIKE @like OR MobileNumber LIKE @like OR Email LIKE @like)
ORDER BY FirstName,LastName",cn))
                using(var da=new SqlDataAdapter(cmd)) {
                    string q=_search.Text.Trim(); cmd.Parameters.Add("@q",SqlDbType.NVarChar,150).Value=q; cmd.Parameters.Add("@like",SqlDbType.NVarChar,152).Value="%"+q+"%";
                    var dt=new DataTable(); da.Fill(dt); _grid.DataSource=dt;
                }
                if(_grid.Columns.Contains("UserId")) _grid.Columns["UserId"].Visible=false;
                if(_grid.Columns.Contains("CreatedAt")) _grid.Columns["CreatedAt"].DefaultCellStyle.Format="dd MMM yyyy";
                if(_grid.Columns.Contains("CreatedDate")) _grid.Columns["CreatedDate"].DefaultCellStyle.Format="dd MMM yyyy HH:mm";
                if(_grid.Columns.Contains("UpdatedDate")) _grid.Columns["UpdatedDate"].DefaultCellStyle.Format="dd MMM yyyy HH:mm";
                if(_grid.Columns.Contains("AccountStatus")) _grid.Columns["AccountStatus"].HeaderText="Status";
            } catch(Exception ex) { MessageBox.Show("Unable to load users.\n"+ex.Message,"User management",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }

        private void EditSelected()
        {
            if(_grid.CurrentRow==null) { MessageBox.Show("Select a user first."); return; }
            EditUser(Convert.ToInt32(_grid.CurrentRow.Cells["UserId"].Value));
        }

        private void EditUser(int? userId)
        {
            DataRow existing=null;
            if(userId.HasValue) {
                using(var cn=new SqlConnection(UserAccess.ConnectionString))
                using(var da=new SqlDataAdapter("SELECT * FROM dbo.AppUsers WHERE UserId=@id",cn)) {
                    da.SelectCommand.Parameters.Add("@id",SqlDbType.Int).Value=userId.Value; var dt=new DataTable(); da.Fill(dt);
                    if(dt.Rows.Count==0) return; existing=dt.Rows[0];
                }
            }

            using(var f=new UserEditorDialog(existing))
                if(f.ShowDialog(this)==DialogResult.OK) LoadUsers();
        }

        private void ToggleUser()
        {
            if(_grid.CurrentRow==null) { MessageBox.Show("Select a user first."); return; }
            int id=Convert.ToInt32(_grid.CurrentRow.Cells["UserId"].Value);
            string name=Convert.ToString(_grid.CurrentRow.Cells["UserName"].Value);
            if(string.Equals(name,"admin",StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("The built-in Super Admin account cannot be disabled."); return; }
            if(MessageBox.Show("Change the selected user's active status?","Confirm",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
            using(var cn=new SqlConnection(UserAccess.ConnectionString))
            using(var cmd=new SqlCommand("UPDATE dbo.AppUsers SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE UserId=@id",cn))
            { cn.Open(); AuditService.SetContext(cn,"User Management"); cmd.Parameters.Add("@id",SqlDbType.Int).Value=id; cmd.ExecuteNonQuery(); }
            AuditService.Write("User Management", "Changed active status for user "+name, "Status change", "UserId="+id);
            LoadUsers();
        }
    }

    internal sealed class UserEditorDialog : Form
    {
        private readonly DataRow _existing;
        private readonly TextBox first=new TextBox(), last=new TextBox(), username=new TextBox(), password=new TextBox(), mobile=new TextBox(), email=new TextBox(), address=new TextBox();
        private readonly TextBox pharmacyName=new TextBox(), pharmacyAddress=new TextBox(), license=new TextBox(), gstin=new TextBox();
        private readonly ComboBox role=new ComboBox();
        private readonly CheckBox active=new CheckBox { Text="Account active", Checked=true, AutoSize=true };
        private readonly Label pharmacyHeading=new Label { Text="PHARMACY DETAILS (for pharmacy staff / owner)", AutoSize=true, ForeColor=MediBillingTheme.Primary, Font=new Font("Segoe UI",9F,FontStyle.Bold) };

        internal UserEditorDialog(DataRow existing)
        {
            _existing=existing;
            Text=existing==null?"Create user":"Edit user";
            StartPosition=FormStartPosition.CenterParent; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
            ClientSize=new Size(680,760); MinimumSize=new Size(680,760); BackColor=MediBillingTheme.Page; Font=new Font("Segoe UI",9.5F);
            var root=new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(18), ColumnCount=2, RowCount=0, AutoScroll=true, BackColor=MediBillingTheme.Page };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            Controls.Add(root);
            AddField(root,"First name *",first); AddField(root,"Last name *",last); AddField(root,"Username *",username);
            password.UseSystemPasswordChar=true; password.Text="";
            AddField(root,existing==null?"Password *":"New password",password); AddField(root,"Mobile number *",mobile); AddField(root,"Email",email);
            address.Multiline=true; address.Height=55; AddField(root,"Address",address);
            role.DropDownStyle=ComboBoxStyle.DropDownList; role.DataSource=UserAccess.GetActiveRoles(); role.DisplayMember="RoleName"; role.ValueMember="RoleName"; AddField(root,"Role *",role);
            root.Controls.Add(pharmacyHeading); root.SetColumnSpan(pharmacyHeading,2);
            AddField(root,"Pharmacy name",pharmacyName); pharmacyAddress.Multiline=true; pharmacyAddress.Height=48; AddField(root,"Pharmacy address",pharmacyAddress);
            AddField(root,"Pharmacy license no.",license); AddField(root,"Pharmacy GSTIN",gstin);
            root.Controls.Add(active); root.SetColumnSpan(active,2);
            var buttons=new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true, FlowDirection=FlowDirection.RightToLeft, Margin=new Padding(0,12,0,0) };
            var save=new Button { Text="Save user", Width=120, Height=38, BackColor=MediBillingTheme.Primary, ForeColor=Color.White, FlatStyle=FlatStyle.Flat, UseVisualStyleBackColor=false };
            var cancel=new Button { Text="Cancel", Width=100, Height=38, DialogResult=DialogResult.Cancel, BackColor=MediBillingTheme.Secondary, ForeColor=Color.White, FlatStyle=FlatStyle.Flat, UseVisualStyleBackColor=false };
            save.FlatAppearance.BorderSize=0; cancel.FlatAppearance.BorderSize=0; buttons.Controls.Add(cancel); buttons.Controls.Add(save);
            root.Controls.Add(buttons); root.SetColumnSpan(buttons,2); AcceptButton=save; CancelButton=cancel;
            role.SelectedValueChanged+=(s,e)=>UpdatePharmacyFields(); save.Click+=(s,e)=>SaveUser();
            if(existing!=null) FillExisting();
            UpdatePharmacyFields();
        }

        private void AddField(TableLayoutPanel root,string label,Control input)
        {
            int row=root.RowCount++; root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var l=new Label { Text=label, Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, ForeColor=MediBillingTheme.Secondary, Margin=new Padding(2,7,8,7), AutoSize=true };
            input.Dock=DockStyle.Fill; input.Margin=new Padding(2,4,2,4); input.Height=Math.Max(input.Height,30);
            root.Controls.Add(l,0,row); root.Controls.Add(input,1,row);
        }

        private void FillExisting()
        {
            first.Text=Val("FirstName"); last.Text=Val("LastName"); username.Text=Val("UserName");
            mobile.Text=Val("MobileNumber"); email.Text=Val("Email"); address.Text=Val("Address");
            pharmacyName.Text=Val("PharmacyName"); pharmacyAddress.Text=Val("PharmacyAddress");
            license.Text=Val("PharmacyLicenseNumber"); gstin.Text=Val("PharmacyGSTIN");
            active.Checked=Convert.ToBoolean(_existing["IsActive"]); username.Enabled=false; password.Text="";
            role.SelectedValue=Val("RoleName");
        }

        private string Val(string column) { return _existing==null || _existing[column]==DBNull.Value ? "" : Convert.ToString(_existing[column]); }
        private bool IsPharmacyRole()
        {
            string r=Convert.ToString(role.SelectedValue);
            return r=="Pharmacy Owner" || r=="Pharmacist" || r=="Cashier" || r=="Inventory Manager";
        }
        private void UpdatePharmacyFields()
        {
            bool show=IsPharmacyRole();
            pharmacyHeading.Visible=show; pharmacyName.Visible=show; pharmacyAddress.Visible=show; license.Visible=show; gstin.Visible=show;
            foreach(Control c in Controls) { }
            // Keep the form compact for system-only accounts by hiding pharmacy input controls and their labels.
            var root=Controls.Count>0?Controls[0] as TableLayoutPanel:null;
            if(root!=null) foreach(Control c in root.Controls) {
                if(c==pharmacyHeading || c==pharmacyName || c==pharmacyAddress || c==license || c==gstin) continue;
                if(c is Label l && (l.Text=="Pharmacy name" || l.Text=="Pharmacy address" || l.Text=="Pharmacy license no." || l.Text=="Pharmacy GSTIN")) c.Visible=show;
            }
        }

        private void SaveUser()
        {
            string fn=first.Text.Trim(), ln=last.Text.Trim(), un=username.Text.Trim(), pw=password.Text, mob=mobile.Text.Trim(), em=email.Text.Trim();
            string roleName=Convert.ToString(role.SelectedValue);
            if(fn.Length==0||ln.Length==0||un.Length==0||mob.Length==0) { MessageBox.Show("First name, last name, username and mobile number are required."); return; }
            if(_existing==null && pw.Length<8) { MessageBox.Show("Password must contain at least 8 characters."); return; }
            if(pw.Length>0 && pw.Length<8) { MessageBox.Show("New password must contain at least 8 characters."); return; }
            if(IsPharmacyRole() && string.IsNullOrWhiteSpace(pharmacyName.Text)) { MessageBox.Show("Enter the pharmacy name for this role."); return; }
            try {
                using(var cn=new SqlConnection(UserAccess.ConnectionString)) {
                    cn.Open();
                    AuditService.SetContext(cn,"User Management");
                    string sql;
                    if(_existing==null) sql=@"INSERT dbo.AppUsers(UserName,DisplayName,PasswordHash,RoleName,FirstName,LastName,MobileNumber,Email,[Address],PharmacyName,PharmacyAddress,PharmacyLicenseNumber,PharmacyGSTIN,IsActive)
VALUES(@u,@d,@p,@r,@f,@l,@m,@e,@a,@pn,@pa,@pl,@pg,@active)";
                    else sql=@"UPDATE dbo.AppUsers SET DisplayName=@d,RoleName=@r,FirstName=@f,LastName=@l,MobileNumber=@m,Email=@e,[Address]=@a,PharmacyName=@pn,PharmacyAddress=@pa,PharmacyLicenseNumber=@pl,PharmacyGSTIN=@pg,IsActive=@active
"+(pw.Length>0?",PasswordHash=@p":"")+" WHERE UserId=@id";
                    using(var cmd=new SqlCommand(sql,cn)) {
                        cmd.Parameters.Add("@u",SqlDbType.NVarChar,80).Value=un;
                        cmd.Parameters.Add("@d",SqlDbType.NVarChar,150).Value=(fn+" "+ln).Trim();
                        cmd.Parameters.Add("@p",SqlDbType.NVarChar,100).Value=pw.Length>0?UserAccess.HashPassword(pw):(object)DBNull.Value;
                        cmd.Parameters.Add("@r",SqlDbType.NVarChar,40).Value=roleName;
                        cmd.Parameters.Add("@f",SqlDbType.NVarChar,80).Value=fn; cmd.Parameters.Add("@l",SqlDbType.NVarChar,80).Value=ln;
                        cmd.Parameters.Add("@m",SqlDbType.NVarChar,20).Value=mob;
                        cmd.Parameters.Add("@e",SqlDbType.NVarChar,150).Value=em.Length==0?(object)DBNull.Value:em;
                        cmd.Parameters.Add("@a",SqlDbType.NVarChar,500).Value=address.Text.Trim().Length==0?(object)DBNull.Value:address.Text.Trim();
                        cmd.Parameters.Add("@pn",SqlDbType.NVarChar,150).Value=IsPharmacyRole()&&pharmacyName.Text.Trim().Length>0?(object)pharmacyName.Text.Trim():DBNull.Value;
                        cmd.Parameters.Add("@pa",SqlDbType.NVarChar,500).Value=IsPharmacyRole()&&pharmacyAddress.Text.Trim().Length>0?(object)pharmacyAddress.Text.Trim():DBNull.Value;
                        cmd.Parameters.Add("@pl",SqlDbType.NVarChar,80).Value=IsPharmacyRole()&&license.Text.Trim().Length>0?(object)license.Text.Trim():DBNull.Value;
                        cmd.Parameters.Add("@pg",SqlDbType.NVarChar,20).Value=IsPharmacyRole()&&gstin.Text.Trim().Length>0?(object)gstin.Text.Trim():DBNull.Value;
                        cmd.Parameters.Add("@active",SqlDbType.Bit).Value=active.Checked;
                        if(_existing!=null) cmd.Parameters.Add("@id",SqlDbType.Int).Value=Convert.ToInt32(_existing["UserId"]);
                        cmd.ExecuteNonQuery();
                    }
                }
                AuditService.Write("User Management", (_existing==null?"Created user ":"Updated user ")+un, _existing==null?"Create":"Update", "Role="+roleName+"; Mobile="+mob);
                DialogResult=DialogResult.OK; Close();
            } catch(Exception ex) { MessageBox.Show("Could not save user. Check that the username is unique and the values are valid.\n\n"+ex.Message,"Save user",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }
    }
}
