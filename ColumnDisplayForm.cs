using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ListFormatParser
{
    /// <summary>
    /// Simple WinForms dialog showing parsed FORMAT() columns in a grid.
    /// </summary>
    internal class ColumnDisplayForm : Form
    {
        public ColumnDisplayForm(List<FormatColumn> columns, string flatSourceLine)
        {
            Text            = "List Format Columns";
            Size            = new Size(1000, 480);
            StartPosition   = FormStartPosition.CenterScreen;
            MinimizeBox     = false;
            Font            = new Font("Segoe UI", 9f);

            // Source context strip at top
            var sourceBox = new TextBox
            {
                Text      = flatSourceLine,
                ReadOnly  = true,
                Dock      = DockStyle.Top,
                Font      = new Font("Consolas", 8.5f),
                BackColor = SystemColors.ControlLight,
                ScrollBars = ScrollBars.Horizontal,
            };

            // Column grid
            var grid = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                ReadOnly                    = true,
                AllowUserToAddRows          = false,
                AllowUserToResizeRows       = false,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.AllCells,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible           = false,
                BackgroundColor             = SystemColors.Window,
                GridColor                   = SystemColors.ControlLight,
                Font                        = new Font("Consolas", 9f),
                BorderStyle                 = BorderStyle.None,
                CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            };

            grid.Columns.Add("Col",         "#");
            grid.Columns.Add("Width",       "Width");
            grid.Columns.Add("Align",       "Align");
            grid.Columns.Add("Indent",      "Indent");
            grid.Columns.Add("Modifiers",   "Modifiers");
            grid.Columns.Add("Header",      "Header");
            grid.Columns.Add("HdrAlign",    "Hdr Align");
            grid.Columns.Add("HdrIndent",   "Hdr Indent");
            grid.Columns.Add("Picture",     "Picture");
            grid.Columns.Add("Raw",         "Format Spec");

            // Right-align the numeric columns
            grid.Columns["Width"].DefaultCellStyle.Alignment    = DataGridViewContentAlignment.MiddleRight;

            foreach (var col in columns)
            {
                var row = grid.Rows[grid.Rows.Add()];
                row.Cells["Col"].Value         = col.ColLabel;
                row.Cells["Width"].Value       = col.Width;
                row.Cells["Align"].Value       = col.Alignment;
                row.Cells["Indent"].Value      = col.Indent;
                row.Cells["Modifiers"].Value   = col.Modifiers;
                row.Cells["Modifiers"].ToolTipText = ModifierDescriber.Describe(col.Modifiers);
                row.Cells["Header"].Value      = col.Header;
                row.Cells["HdrAlign"].Value    = col.HeaderAlignment;
                row.Cells["HdrIndent"].Value   = col.HeaderIndent;
                row.Cells["Picture"].Value     = col.Picture;
                row.Cells["Raw"].Value         = col.RawSpec;

                // Highlight group rows
                if (col.IsGroupStart || col.IsGroupEnd)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
            }

            // Status bar
            var status = new StatusStrip();
            status.Items.Add(new ToolStripStatusLabel(
                $"{columns.Count} column(s) — hover Modifiers cell for meaning — right-click to copy"));

            // Button panel
            var btnCopyFormat = new Button
            {
                Text    = "Copy FORMAT",
                Dock    = DockStyle.Right,
                Width   = 110,
                Height  = 26,
                Font    = new Font("Segoe UI", 9f),
            };
            btnCopyFormat.Click += (s, e) =>
            {
                string fmt = FormatStringGenerator.Generate(columns);
                System.Windows.Forms.Clipboard.SetText(fmt);
                status.Items[0].Text = "FORMAT string copied to clipboard.";
            };

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnPanel.Controls.Add(btnCopyFormat);

            // Copy to clipboard on right-click
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Copy selected row", null, (s, e) => CopySelected(grid));
            ctxMenu.Items.Add("Copy all", null, (s, e) => CopyAll(grid));
            grid.ContextMenuStrip = ctxMenu;

            Controls.Add(grid);
            Controls.Add(sourceBox);
            Controls.Add(btnPanel);
            Controls.Add(status);
        }

        private void CopySelected(DataGridView grid)
        {
            if (grid.SelectedRows.Count == 0) return;
            var sb = new System.Text.StringBuilder();
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                    sb.Append((cell.Value ?? "").ToString().PadRight(cell.OwningColumn.Width / 7)).Append('\t');
                sb.AppendLine();
            }
            Clipboard.SetText(sb.ToString());
        }

        private void CopyAll(DataGridView grid)
        {
            grid.SelectAll();
            CopySelected(grid);
            grid.ClearSelection();
        }
    }
}
