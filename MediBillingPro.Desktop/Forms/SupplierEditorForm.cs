using MediBillingPro.Domain.Entities.Contacts;
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
    public partial class SupplierEditorForm : Form
    {
        private readonly TextBox txtName = new TextBox();
        private readonly TextBox txtContact = new TextBox();
        private readonly TextBox txtMobile = new TextBox();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtAddress = new TextBox();
        private readonly TextBox txtGstin = new TextBox();
        private readonly TextBox txtLicense = new TextBox();

        private readonly Color green = Color.FromArgb(8, 127, 91);
        private readonly Color page = Color.FromArgb(244, 248, 246);

        public Supplier SupplierData { get; private set; }

        public SupplierEditorForm(Supplier supplier = null)
        {
            SupplierData = supplier == null
                ? new Supplier { IsActive = true }
                : new Supplier
                {
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    ContactPerson = supplier.ContactPerson,
                    MobileNumber = supplier.MobileNumber,
                    Email = supplier.Email,
                    Address = supplier.Address,
                    GSTIN = supplier.GSTIN,
                    LicenseNumber = supplier.LicenseNumber,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt
                };

            BuildForm();
            FillFields();
        }

        private void BuildForm()
        {
            Text = SupplierData.SupplierId == 0 ? "Add Supplier" : "Edit Supplier";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = page;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(620, 600);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22),
                ColumnCount = 2,
                RowCount = 10,
                BackColor = page
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            for (int i = 1; i <= 8; i++) root.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 78 : 43));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var title = new Label
            {
                Text = Text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 60, 49),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(title, 0, 0);
            root.SetColumnSpan(title, 2);

            AddField(root, "Supplier name *", txtName, 1);
            AddField(root, "Contact person", txtContact, 2);
            AddField(root, "Mobile number", txtMobile, 3);
            AddField(root, "Email", txtEmail, 4);
            txtAddress.Multiline = true;
            txtAddress.ScrollBars = ScrollBars.Vertical;
            AddField(root, "Address", txtAddress, 5);
            AddField(root, "GSTIN", txtGstin, 6);
            AddField(root, "License number", txtLicense, 7);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };
            var btnSave = new Button
            {
                Text = "Save Supplier",
                Width = 130,
                Height = 36,
                BackColor = green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += SaveClicked;
            var btnCancel = new Button
            {
                Text = "Cancel",
                Width = 90,
                Height = 36,
                DialogResult = DialogResult.Cancel,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            root.Controls.Add(buttons, 0, 9);
            root.SetColumnSpan(buttons, 2);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private static void AddField(TableLayoutPanel panel, string label, Control input, int row)
        {
            var caption = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(55, 75, 65)
            };
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(3, 5, 3, 5);
            panel.Controls.Add(caption, 0, row);
            panel.Controls.Add(input, 1, row);
        }

        private void FillFields()
        {
            txtName.Text = SupplierData.SupplierName;
            txtContact.Text = SupplierData.ContactPerson;
            txtMobile.Text = SupplierData.MobileNumber;
            txtEmail.Text = SupplierData.Email;
            txtAddress.Text = SupplierData.Address;
            txtGstin.Text = SupplierData.GSTIN;
            txtLicense.Text = SupplierData.LicenseNumber;
        }

        private void SaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Supplier name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            //int? pharmacyId = null;
            //if (!string.IsNullOrWhiteSpace(txtPharmacyId.Text))
            //{
            //    int parsed;
            //    if (!int.TryParse(txtPharmacyId.Text.Trim(), out parsed) || parsed <= 0)
            //    {
            //        MessageBox.Show(this, "Pharmacy ID must be a positive whole number.",
            //            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        txtPharmacyId.Focus();
            //        return;
            //    }
            //    pharmacyId = parsed;
            //}

            SupplierData.SupplierName = txtName.Text.Trim();
            SupplierData.ContactPerson = NullIfEmpty(txtContact.Text);
            SupplierData.MobileNumber = NullIfEmpty(txtMobile.Text);
            SupplierData.Email = NullIfEmpty(txtEmail.Text);
            SupplierData.Address = NullIfEmpty(txtAddress.Text);
            SupplierData.GSTIN = NullIfEmpty(txtGstin.Text);
            SupplierData.LicenseNumber = NullIfEmpty(txtLicense.Text);
            
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
