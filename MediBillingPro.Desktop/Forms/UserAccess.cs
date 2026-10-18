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

        public bool IsAdmin { get { return Role == "Admin"; } }
        public bool IsPharmacist { get { return Role == "Pharmacist"; } }
        public bool CanBill { get { return IsAdmin || IsPharmacist || Role == "Cashier"; } }
        public bool CanManageInventory { get { return IsAdmin || Role == "Inventory Manager"; } }
    }

    internal static class UserAccess
    {
        internal static readonly string[] Roles = { "Admin", "Pharmacist", "Cashier", "Inventory Manager" };
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
IF OBJECT_ID(N'dbo.Invoices',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Invoices',N'PaymentMethod') IS NULL
    ALTER TABLE dbo.Invoices ADD PaymentMethod NVARCHAR(40) NULL;";
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                cmd.ExecuteNonQuery();
                SeedUser(cn, "admin", "Pharmacy Admin", "Admin@123", "Admin");
                SeedUser(cn, "pharmacist", "Demo Pharmacist", "Pharma@123", "Pharmacist");
                SeedUser(cn, "cashier", "Demo Cashier", "Cashier@123", "Cashier");
                SeedUser(cn, "inventory", "Demo Inventory Manager", "Stock@123", "Inventory Manager");
            }
        }

        private static void SeedUser(SqlConnection cn, string user, string display, string password, string role)
        {
            using (var cmd = new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE UserName=@u)
INSERT dbo.AppUsers(UserName,DisplayName,PasswordHash,RoleName,IsActive)
VALUES(@u,@d,@p,@r,1);", cn))
            {
                cmd.Parameters.AddWithValue("@u", user);
                cmd.Parameters.AddWithValue("@d", display);
                cmd.Parameters.AddWithValue("@p", HashPassword(password));
                cmd.Parameters.AddWithValue("@r", role);
                cmd.ExecuteNonQuery();
            }
        }

        internal static UserSession Authenticate(string username, string password)
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(@"SELECT UserId,UserName,DisplayName,RoleName FROM dbo.AppUsers
WHERE UserName=@u AND PasswordHash=@p AND IsActive=1", cn))
            {
                cmd.Parameters.AddWithValue("@u", username.Trim());
                cmd.Parameters.AddWithValue("@p", HashPassword(password));
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
        public UserSession Session { get; private set; }

        public LoginForm()
        {
            Text = "MediBilling Pro | Sign in";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(460, 430);
            BackColor = Color.FromArgb(244, 248, 246);
            Font = new Font("Segoe UI", 10F);
            var title = new Label { Text = "✚ MediBilling Pro", AutoSize = true, Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = Color.FromArgb(21,60,49), Location = new Point(34,28) };
            var sub = new Label { Text = "Sign in to your pharmacy workspace", AutoSize = true, ForeColor = Color.FromArgb(91,111,101), Location = new Point(38,75) };
            Controls.Add(title); Controls.Add(sub);
            AddLabel("Username", 38, 125); _username.SetBounds(38, 151, 380, 34); Controls.Add(_username);
            AddLabel("Password", 38, 199); _password.SetBounds(38, 225, 380, 34); _password.UseSystemPasswordChar = true; Controls.Add(_password);
            var login = new Button { Text = "Sign in", Bounds = new Rectangle(38, 280, 380, 42), BackColor = Color.FromArgb(8,127,91), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            login.Click += (s,e) => SignIn(); Controls.Add(login); AcceptButton = login;
            var hint = new Label { Text = "Demo: admin / Admin@123   •   pharmacist / Pharma@123", AutoSize = false, Bounds = new Rectangle(38, 340, 385, 42), ForeColor = Color.FromArgb(91,111,101), Font = new Font("Segoe UI", 8.5F) };
            Controls.Add(hint);
            Shown += (s,e) => { try { UserAccess.EnsureSchemaAndSeed(); } catch(Exception ex) { MessageBox.Show("Cannot initialize user access. Check SQL Server and App.config connection string.\n\n" + ex.Message, "Database connection", MessageBoxButtons.OK, MessageBoxIcon.Error); } };
        }

        private void AddLabel(string text, int x, int y) { Controls.Add(new Label { Text = text, AutoSize = true, Location = new Point(x,y), ForeColor = Color.FromArgb(21,60,49), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) }); }
        private void SignIn()
        {
            try {
                Session = UserAccess.Authenticate(_username.Text, _password.Text);
                if (Session == null) { MessageBox.Show("Username or password is incorrect, or the account is inactive.", "Sign in failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                DialogResult = DialogResult.OK; Close();
            } catch(Exception ex) { MessageBox.Show(ex.Message, "Sign in failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    public sealed class UserManagementForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        public UserManagementForm()
        {
            Text = "User Management"; Dock = DockStyle.Fill; BackColor = Color.FromArgb(244,248,246); Font = new Font("Segoe UI",9.5F);
            var root = new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(18), RowCount=3, ColumnCount=1, BackColor=BackColor };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            root.Controls.Add(new Label { Text="Users & Roles", AutoSize=true, Font=new Font("Segoe UI",22,FontStyle.Bold), ForeColor=Color.FromArgb(21,60,49), Margin=new Padding(0,0,0,14) },0,0);
            var actions = new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true, Margin=new Padding(0,0,0,12) };
            var add = new Button { Text="Create user", AutoSize=true, Height=36, BackColor=Color.FromArgb(8,127,91), ForeColor=Color.White, FlatStyle=FlatStyle.Flat };
            var toggle = new Button { Text="Enable / Disable selected", AutoSize=true, Height=36 };
            var refresh = new Button { Text="Refresh", AutoSize=true, Height=36 };
            add.Click += (s,e)=>CreateUser(); toggle.Click += (s,e)=>ToggleUser(); refresh.Click += (s,e)=>LoadUsers();
            actions.Controls.Add(add); actions.Controls.Add(toggle); actions.Controls.Add(refresh); root.Controls.Add(actions,0,1);
            _grid.Dock=DockStyle.Fill; _grid.ReadOnly=true; _grid.AllowUserToAddRows=false; _grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect=false; _grid.BackgroundColor=Color.White; _grid.RowHeadersVisible=false;
            root.Controls.Add(_grid,0,2); Controls.Add(root); LoadUsers();
        }

        private void LoadUsers()
        {
            using(var cn=new SqlConnection(UserAccess.ConnectionString))
            using(var da=new SqlDataAdapter("SELECT UserId,UserName,DisplayName,RoleName,IsActive,CreatedAt FROM dbo.AppUsers ORDER BY UserName",cn))
            { var dt=new DataTable(); da.Fill(dt); _grid.DataSource=dt; if(_grid.Columns.Contains("UserId")) _grid.Columns["UserId"].Visible=false; }
        }

        private void CreateUser()
        {
            using(var f=new Form { Text="Create user", Width=430, Height=390, StartPosition=FormStartPosition.CenterParent, FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false, MinimizeBox=false })
            {
                var p=new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(18), ColumnCount=2, RowCount=5 };
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,115)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
                Func<string,int,TextBox> field=(label,row)=> { p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row); var t=new TextBox{Dock=DockStyle.Fill}; p.Controls.Add(t,1,row); return t; };
                var user=field("Username",0); var display=field("Display name",1); var pass=field("Password",2); pass.UseSystemPasswordChar=true;
                p.Controls.Add(new Label{Text="Role",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,3);
                var role=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList}; role.Items.AddRange(UserAccess.Roles); role.SelectedIndex=1; p.Controls.Add(role,1,3);
                var save=new Button{Text="Create",Dock=DockStyle.Fill,BackColor=Color.FromArgb(8,127,91),ForeColor=Color.White}; var cancel=new Button{Text="Cancel",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel};
                p.Controls.Add(save,0,4); p.Controls.Add(cancel,1,4); f.Controls.Add(p); f.AcceptButton=save; f.CancelButton=cancel;
                save.Click+=(s,e)=> {
                    if(string.IsNullOrWhiteSpace(user.Text)||string.IsNullOrWhiteSpace(display.Text)||pass.Text.Length<8) { MessageBox.Show("Enter username, display name and a password of at least 8 characters."); return; }
                    try {
                        using(var cn=new SqlConnection(UserAccess.ConnectionString))
                        using(var cmd=new SqlCommand("INSERT dbo.AppUsers(UserName,DisplayName,PasswordHash,RoleName,IsActive) VALUES(@u,@d,@p,@r,1)",cn)) {
                            cmd.Parameters.AddWithValue("@u",user.Text.Trim()); cmd.Parameters.AddWithValue("@d",display.Text.Trim()); cmd.Parameters.AddWithValue("@p",UserAccess.HashPassword(pass.Text)); cmd.Parameters.AddWithValue("@r",Convert.ToString(role.SelectedItem)); cn.Open(); cmd.ExecuteNonQuery();
                        }
                        f.DialogResult=DialogResult.OK; f.Close();
                    } catch(Exception ex) { MessageBox.Show("Could not create user. Usernames must be unique.\n"+ex.Message); }
                };
                if(f.ShowDialog(this)==DialogResult.OK) LoadUsers();
            }
        }

        private void ToggleUser()
        {
            if(_grid.CurrentRow==null) return;
            int id=Convert.ToInt32(_grid.CurrentRow.Cells["UserId"].Value);
            string name=Convert.ToString(_grid.CurrentRow.Cells["UserName"].Value);
            if(string.Equals(name,"admin",StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("The built-in admin account cannot be disabled here."); return; }
            using(var cn=new SqlConnection(UserAccess.ConnectionString))
            using(var cmd=new SqlCommand("UPDATE dbo.AppUsers SET IsActive=CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE UserId=@id",cn))
            { cmd.Parameters.AddWithValue("@id",id); cn.Open(); cmd.ExecuteNonQuery(); }
            LoadUsers();
        }
    }
}
