using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediBillingPro.Desktop.UI.Common
{
    public static class UiStyle
    {
        // Common application colors
        public static readonly Color Primary =
            Color.FromArgb(13, 148, 136);

        public static readonly Color DarkGreen =
            Color.FromArgb(30, 41, 59);

        public static readonly Color Secondary =
            Color.FromArgb(71, 85, 105);

        public static readonly Color Danger =
            Color.FromArgb(185, 28, 28);

        public static readonly Color PageBackground =
            Color.FromArgb(246, 248, 251);

        public static readonly Color Muted =
            Color.FromArgb(100, 116, 139);

        public static readonly Color HeaderBackground =
            Color.FromArgb(225, 247, 244);

        // Primary: Add, Save, Submit
        public static Button CreatePrimaryButton(
            string text, int width = 130)
        {
            return CreateButton(text, Primary, width);
        }

        // Secondary: Edit, Cancel, Back
        public static Button CreateSecondaryButton(
            string text, int width = 110)
        {
            return CreateButton(text, Secondary, width);
        }

        // Danger: Delete, Deactivate
        public static Button CreateDangerButton(
            string text, int width = 120)
        {
            return CreateButton(text, Danger, width);
        }

        private static Button CreateButton(
            string text, Color background, int width)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 38,
                BackColor = background,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    "Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(5)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor =
                ControlPaint.Dark(background, 0.10F);

            return button;
        }

        // Apply common styling to DataGridView controls.
        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;

            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor =
                HeaderBackground;

            grid.ColumnHeadersDefaultCellStyle.ForeColor =
                DarkGreen;

            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor =
                HeaderBackground;

            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor =
                DarkGreen;

            grid.DefaultCellStyle.SelectionBackColor =
                Color.FromArgb(220, 241, 230);

            grid.DefaultCellStyle.SelectionForeColor =
                DarkGreen;

            grid.RowsDefaultCellStyle.BackColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(249, 252, 250);

            grid.ColumnHeadersHeight = 44;
            grid.RowTemplate.Height = 42;
            grid.GridColor = Color.FromArgb(230, 237, 233);
            grid.CellBorderStyle =
                DataGridViewCellBorderStyle.SingleHorizontal;
        }
    }
}
