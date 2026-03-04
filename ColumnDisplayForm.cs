using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ListFormatParser
{
    /// <summary>
    /// Tabbed WinForms dialog showing parsed FORMAT() columns.
    /// Tabs: Columns grid, Explain (plain-English per column), FORMAT Lines, FROM.
    /// </summary>
    internal class ColumnDisplayForm : Form
    {
        private readonly StatusStrip        _status;
        private readonly ToolStripStatusLabel _statusLabel;
        private string _defaultStatus;

        private void SetStatus(string msg)
        {
            _statusLabel.Text = msg;
            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, e) => { _statusLabel.Text = _defaultStatus; t.Stop(); t.Dispose(); };
            t.Start();
        }

        public ColumnDisplayForm(List<FormatColumn> columns, string flatSourceLine,
                                 List<FromParser.FromEntry> fromEntries = null,
                                 string useVar = null, bool fromFirst = false)
        {
            bool fromOnly = columns.Count == 0 && fromEntries != null && fromEntries.Count > 0;
            Text          = fromOnly ? "List Format Parser — FROM" : "List Format Parser — FORMAT";
            Size          = new Size(1060, 580);
            MinimumSize   = new Size(700, 400);
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox   = false;
            Font          = new Font("Segoe UI", 9f);

            // ── Window icon ─────────────────────────────────────────────────
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly()
                       .GetManifestResourceStream("ListFormatParser.Resources.FormIcon.png"))
            {
                if (stream != null)
                    using (var bmp = new Bitmap(stream))
                        Icon = Icon.FromHandle(bmp.GetHicon());
            }

            // ── Status bar ──────────────────────────────────────────────────
            _status      = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel(
                fromOnly
                    ? $"{(fromEntries?.Count ?? 0)} FROM entries — use tabs below to copy as CASE or CHOOSE"
                    : $"{columns.Count} column(s) — hover Modifiers cell for tooltip — use Explain tab for full detail");
            _status.Items.Add(_statusLabel);
            _defaultStatus = _statusLabel.Text;

            // ── Source strip ────────────────────────────────────────────────
            var sourceBox = new TextBox
            {
                Text       = flatSourceLine,
                ReadOnly   = true,
                Dock       = DockStyle.Top,
                Font       = new Font("Consolas", 8.5f),
                BackColor  = SystemColors.ControlLight,
                ScrollBars = ScrollBars.Horizontal,
                Height     = 22,
            };

            // ── Tab control (Explain, FORMAT Lines, FROM) ────────────────────
            var tabs = new TabControl { Dock = DockStyle.Fill };
            if (columns.Count > 0)
            {
                tabs.TabPages.Add(BuildExplainTab(columns));
                tabs.TabPages.Add(BuildFormatLinesTab(columns, flatSourceLine));
            }
            if (fromEntries != null && fromEntries.Count > 0)
                tabs.TabPages.Add(BuildFromTab(fromEntries, useVar ?? "ListUseVariable"));

            if (fromFirst && tabs.TabCount > 0)
                tabs.SelectedIndex = tabs.TabCount - 1;

            // ── Top pane: columns grid OR simple FROM grid ───────────────────
            Control topPane = (fromEntries != null && fromEntries.Count > 0 && columns.Count == 0)
                ? (Control)BuildFromGrid(fromEntries)
                : (Control)BuildColumnsGrid(columns);

            // ── Split: grid on top, tabs on bottom ──────────────────────────
            var split = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                Orientation      = Orientation.Horizontal,
                SplitterDistance = 220,
                SplitterWidth    = 6,
            };
            StyleSplitter(split);
            split.Panel1.Controls.Add(topPane);
            split.Panel2.Controls.Add(tabs);

            Controls.Add(split);
            Controls.Add(sourceBox);
            Controls.Add(_status);
        }

        // ════════════════════════════════════════════════════════════════════
        // FROM entries grid (top pane — FROM-only mode)
        // ════════════════════════════════════════════════════════════════════
        private Panel BuildFromGrid(List<FromParser.FromEntry> entries)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

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

            grid.Columns.Add("No",      "#");
            grid.Columns.Add("Display", "Display");
            grid.Columns.Add("Value",   "Value");

            grid.Columns["No"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            for (int i = 0; i < entries.Count; i++)
            {
                var row = grid.Rows[grid.Rows.Add()];
                row.Cells["No"].Value      = i + 1;
                row.Cells["Display"].Value = entries[i].Display;
                row.Cells["Value"].Value   = entries[i].Value ?? "";
            }

            panel.Controls.Add(grid);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════
        // Columns grid (top pane — not a tab)
        // ════════════════════════════════════════════════════════════════════
        private Panel BuildColumnsGrid(List<FormatColumn> columns)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

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
                MultiSelect                 = false,
            };

            grid.Columns.Add("Col",       "#");
            grid.Columns.Add("Width",     "Width");
            grid.Columns.Add("Align",     "Align");
            grid.Columns.Add("Indent",    "Indent");
            grid.Columns.Add("Modifiers", "Modifiers");
            grid.Columns.Add("Header",    "Header");
            grid.Columns.Add("HdrAlign",  "Hdr Align");
            grid.Columns.Add("HdrIndent", "Hdr Indent");
            grid.Columns.Add("Picture",   "Picture");
            grid.Columns.Add("Raw",       "Format Spec");

            grid.Columns["Width"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (var col in columns)
            {
                var row = grid.Rows[grid.Rows.Add()];
                row.Cells["Col"].Value       = col.ColLabel;
                row.Cells["Width"].Value     = col.Width;
                row.Cells["Align"].Value     = col.Alignment;
                row.Cells["Indent"].Value    = col.Indent;
                row.Cells["Modifiers"].Value = col.Modifiers;
                row.Cells["Modifiers"].ToolTipText = ModifierDescriber.Describe(col.Modifiers);
                row.Cells["Header"].Value    = col.Header;
                row.Cells["HdrAlign"].Value  = col.HeaderAlignment;
                row.Cells["HdrIndent"].Value = col.HeaderIndent;
                row.Cells["Picture"].Value   = col.Picture;
                row.Cells["Raw"].Value       = col.RawSpec;
                if (col.IsGroupStart || col.IsGroupEnd)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
            }

            // Button bar
            var btnCopy = new Button { Text = "Copy FORMAT", Width = 110, Height = 26, Font = new Font("Segoe UI", 9f), Dock = DockStyle.Right };
            btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(FormatStringGenerator.Generate(columns));
                SetStatus("FORMAT string copied to clipboard.");
            };
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnPanel.Controls.Add(btnCopy);

            // Context menu
            var ctx = new ContextMenuStrip();
            ctx.Items.Add("Copy selected row", null, (s, e) => CopyGridSelected(grid));
            ctx.Items.Add("Copy all rows",     null, (s, e) => { grid.SelectAll(); CopyGridSelected(grid); grid.ClearSelection(); });
            grid.ContextMenuStrip = ctx;

            panel.Controls.Add(grid);
            panel.Controls.Add(btnPanel);
            return panel;
        }

        // ════════════════════════════════════════════════════════════════════
        // Tab 1 — Explain
        // ════════════════════════════════════════════════════════════════════
        private TabPage BuildExplainTab(List<FormatColumn> columns)
        {
            var page = new TabPage("Explain");
            var sb   = new StringBuilder();

            foreach (var col in columns)
            {
                // Header line
                sb.Append("! ── ");
                if (col.IsGroupStart)      sb.Append("Group start");
                else if (col.IsGroupEnd)   sb.Append("Group end");
                else                       sb.Append("Column ").Append(col.ColumnNumber);

                if (!string.IsNullOrEmpty(col.Header))
                    sb.Append("  ~").Append(col.Header).Append("~");
                sb.AppendLine();

                // Width + alignment
                if (!string.IsNullOrEmpty(col.Width))
                    sb.Append("!   Width:     ").AppendLine(col.Width);
                if (!string.IsNullOrEmpty(col.Alignment))
                {
                    string alignDesc = col.Alignment == "L" ? "Left"
                                     : col.Alignment == "R" ? "Right"
                                     : col.Alignment == "C" ? "Centre"
                                     : col.Alignment == "D" ? "Default"
                                     : col.Alignment;
                    sb.Append("!   Align:     ").AppendLine(alignDesc);
                }
                if (!string.IsNullOrEmpty(col.Indent))
                    sb.Append("!   Indent:    ").AppendLine(col.Indent);

                // Header detail
                if (col.Header != null)
                {
                    sb.Append("!   Header:    '").Append(col.Header).AppendLine("'");
                    if (!string.IsNullOrEmpty(col.HeaderAlignment))
                        sb.Append("!   Hdr align: ").AppendLine(col.HeaderAlignment);
                    if (!string.IsNullOrEmpty(col.HeaderIndent))
                        sb.Append("!   Hdr indent:").AppendLine(col.HeaderIndent);
                }

                // Picture
                if (!string.IsNullOrEmpty(col.Picture))
                    sb.Append("!   Picture:   @").Append(col.Picture).AppendLine("@");

                // Modifiers — expanded
                if (!string.IsNullOrEmpty(col.Modifiers))
                {
                    sb.Append("!   Modifiers: ").AppendLine(col.Modifiers);
                    string desc = ModifierDescriber.Describe(col.Modifiers);
                    if (!string.IsNullOrEmpty(desc))
                        foreach (var part in desc.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries))
                            sb.Append("!             • ").AppendLine(part);
                }

                // Raw spec
                sb.Append("!   Spec:      ").AppendLine(col.RawSpec);
                sb.AppendLine("!");
            }

            var txt = new TextBox
            {
                Dock       = DockStyle.Fill,
                ReadOnly   = true,
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = sb.ToString(),
                WordWrap   = false,
            };

            var btnCopy = new Button { Text = "Copy Explain", Width = 110, Height = 26, Font = new Font("Segoe UI", 9f), Dock = DockStyle.Right };
            btnCopy.Click += (s, e) => { Clipboard.SetText(txt.Text); SetStatus("Explain text copied."); };
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnPanel.Controls.Add(btnCopy);

            page.Controls.Add(txt);
            page.Controls.Add(btnPanel);
            return page;
        }

        // ════════════════════════════════════════════════════════════════════
        // Tab 2 — FORMAT Lines
        // ════════════════════════════════════════════════════════════════════
        private TabPage BuildFormatLinesTab(List<FormatColumn> columns, string flatSource)
        {
            var page = new TabPage("FORMAT Lines");

            // Left: FORMAT one-per-line
            var fmtText = FormatStringGenerator.Generate(columns);

            var txtFmt = new TextBox
            {
                ReadOnly   = true,
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = fmtText,
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };

            // Right: #FIELDS list (column number → field number in source)
            var sbFields = new StringBuilder();
            int fldNo = 1;
            foreach (var col in columns)
            {
                if (col.IsGroupStart || col.IsGroupEnd) continue;
                sbFields.AppendLine($"#{fldNo++}  ! col {col.ColLabel}  {col.Header ?? ""}".TrimEnd());
            }

            var txtFields = new TextBox
            {
                ReadOnly   = true,
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = sbFields.ToString(),
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };

            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 680,
                SplitterWidth    = 6,
            };
            StyleSplitter(split);
            split.Panel1.Controls.Add(txtFmt);
            split.Panel2.Controls.Add(txtFields);

            // Button bar
            var btnCopyFmt    = new Button { Text = "Copy FORMAT",  Width = 110, Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnCopyFields = new Button { Text = "Copy #FIELDS", Width = 110, Height = 26, Font = new Font("Segoe UI", 9f) };
            btnCopyFmt.Click    += (s, e) => { Clipboard.SetText(fmtText);            SetStatus("FORMAT copied."); };
            btnCopyFields.Click += (s, e) => { Clipboard.SetText(txtFields.Text);     SetStatus("#FIELDS copied."); };

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnCopyFmt.Left    = 4;  btnCopyFmt.Top    = 3;
            btnCopyFields.Left = 120; btnCopyFields.Top = 3;
            btnPanel.Controls.Add(btnCopyFmt);
            btnPanel.Controls.Add(btnCopyFields);

            page.Controls.Add(split);
            page.Controls.Add(btnPanel);
            return page;
        }

        // ════════════════════════════════════════════════════════════════════
        // Tab 3 — FROM
        // ════════════════════════════════════════════════════════════════════
        private TabPage BuildFromTab(List<FromParser.FromEntry> entries, string useVar)
        {
            var page = new TabPage("FROM");

            // Left: FROM entries one per line
            var sbFrom = new StringBuilder();
            foreach (var e in entries)
            {
                sbFrom.Append(e.Display);
                if (e.Value != null) sbFrom.Append("  (#").Append(e.Value).Append(")");
                sbFrom.AppendLine();
            }

            var txtFrom = new TextBox
            {
                ReadOnly   = true,
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = sbFrom.ToString(),
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };

            // Right: CASE output
            string caseCode = CopyFromCaseCommand.GenerateCaseCode(entries, useVar);
            var txtCase = new TextBox
            {
                ReadOnly   = true,
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = caseCode,
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };

            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300,
                SplitterWidth    = 6,
            };
            StyleSplitter(split);
            split.Panel1.Controls.Add(txtFrom);
            split.Panel2.Controls.Add(txtCase);

            // Button bar
            var btnCopyFrom = new Button { Text = "Copy FROM lines", Width = 120, Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnCopyCase = new Button { Text = "Copy CASE",       Width = 100, Height = 26, Font = new Font("Segoe UI", 9f) };
            btnCopyFrom.Click += (s, e) => { Clipboard.SetText(txtFrom.Text); SetStatus("FROM lines copied."); };
            btnCopyCase.Click += (s, e) => { Clipboard.SetText(caseCode);     SetStatus("CASE code copied."); };

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnCopyFrom.Left = 4;   btnCopyFrom.Top = 3;
            btnCopyCase.Left = 130; btnCopyCase.Top = 3;
            btnPanel.Controls.Add(btnCopyFrom);
            btnPanel.Controls.Add(btnCopyCase);

            page.Controls.Add(split);
            page.Controls.Add(btnPanel);
            return page;
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════
        private static void StyleSplitter(SplitContainer sc)
        {
            sc.Paint += (s, e) =>
            {
                var r = sc.SplitterRectangle;
                using (var b = new System.Drawing.SolidBrush(System.Drawing.SystemColors.ControlDark))
                    e.Graphics.FillRectangle(b, r);
                // grip dots
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;
                int span = sc.Orientation == Orientation.Horizontal ? r.Width : r.Height;
                int step = 6, dots = Math.Min(7, span / step);
                int half = dots / 2 * step;
                using (var dot = new System.Drawing.SolidBrush(System.Drawing.SystemColors.ControlLight))
                {
                    for (int i = -half; i <= half; i += step)
                    {
                        int dx = sc.Orientation == Orientation.Horizontal ? cx + i : cx - 1;
                        int dy = sc.Orientation == Orientation.Horizontal ? cy - 1 : cy + i;
                        e.Graphics.FillRectangle(dot, dx, dy, 2, 2);
                    }
                }
            };
        }

        private static void CopyGridSelected(DataGridView grid)
        {
            if (grid.SelectedRows.Count == 0) return;
            var sb = new StringBuilder();
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                    sb.Append((cell.Value ?? "").ToString()).Append('\t');
                sb.AppendLine();
            }
            Clipboard.SetText(sb.ToString());
        }
    }
}
