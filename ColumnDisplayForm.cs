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

        // FROM CRUD state — populated when a FROM tab is shown
        private DataGridView _fromGrid;
        private TextBox      _txtFromLines;
        private TextBox      _txtCase;
        private TextBox      _txtReformat;
        private Action<string> _writeBackFrom;
        private string       _useVar;
        private string       _flatSourceLine;

        private void SetStatus(string msg)
        {
            _statusLabel.Text = msg;
            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, e) =>
            {
                t.Stop(); t.Dispose();
                if (!_statusLabel.IsDisposed)
                    _statusLabel.Text = _defaultStatus;
            };
            t.Start();
        }

        public ColumnDisplayForm(List<FormatColumn> columns, string flatSourceLine,
                                 List<FromParser.FromEntry> fromEntries = null,
                                 string useVar = null, bool fromFirst = false,
                                 Action<string> writeBackFrom = null)
        {
            _writeBackFrom  = writeBackFrom;
            _useVar         = useVar ?? "ListUseVariable";
            _flatSourceLine = flatSourceLine;
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
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        // Clone into a managed Icon so we own the resource, then free the raw handle
                        using (var tmp = Icon.FromHandle(hIcon))
                            Icon = new Icon(tmp, tmp.Size);
                        NativeMethods.DestroyIcon(hIcon);
                    }
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
                tabs.TabPages.Add(BuildFromTab(fromEntries, useVar ?? "ListUseVariable", flatSourceLine));

            tabs.TabPages.Add(BuildHelpTab());

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
        // FROM entries grid (top pane — editable CRUD)
        // ════════════════════════════════════════════════════════════════════
        private Panel BuildFromGrid(List<FromParser.FromEntry> entries)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _fromGrid = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                ReadOnly                    = false,
                AllowUserToAddRows          = false,
                AllowUserToResizeRows       = false,
                AllowUserToDeleteRows       = false,
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

            var colNo      = new DataGridViewTextBoxColumn { Name = "No",      HeaderText = "#",       ReadOnly = true,  Width = 30 };
            var colDisplay = new DataGridViewTextBoxColumn { Name = "Display",  HeaderText = "Display", Width = 220 };
            var colValue   = new DataGridViewTextBoxColumn { Name = "Value",    HeaderText = "Value",   Width = 100 };
            colNo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            _fromGrid.Columns.AddRange(colNo, colDisplay, colValue);

            for (int i = 0; i < entries.Count; i++)
                AddFromRow(i + 1, entries[i].Display, entries[i].Value ?? "");

            _fromGrid.CellValueChanged += (s, e) => { RenumberFromGrid(); RegenerateFromOutputs(); };
            _fromGrid.RowsRemoved      += (s, e) => { RenumberFromGrid(); RegenerateFromOutputs(); };

            // ── CRUD toolbar ────────────────────────────────────────────────
            var btnAdd    = new Button { Text = "Add",    Width = 50,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnDelete = new Button { Text = "Delete", Width = 60,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnUp     = new Button { Text = "▲ Up",   Width = 60,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnDown   = new Button { Text = "▼ Down", Width = 65,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnUpdate = new Button
            {
                Text    = "Update Source",
                Width   = 110,
                Height  = 26,
                Font    = new Font("Segoe UI", 9f),
                Visible = (_writeBackFrom != null),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };

            btnAdd.Click += (s, e) =>
            {
                int idx = _fromGrid.CurrentRow?.Index ?? _fromGrid.Rows.Count - 1;
                int insertAt = Math.Min(idx + 1, _fromGrid.Rows.Count);
                _fromGrid.Rows.Insert(insertAt, "", "", "");
                _fromGrid.Rows[insertAt].Cells["No"].Value = insertAt + 1;
                RenumberFromGrid();
                _fromGrid.CurrentCell = _fromGrid.Rows[insertAt].Cells["Display"];
                _fromGrid.BeginEdit(true);
            };

            btnDelete.Click += (s, e) =>
            {
                if (_fromGrid.CurrentRow == null || _fromGrid.Rows.Count == 0) return;
                int idx = _fromGrid.CurrentRow.Index;
                _fromGrid.Rows.RemoveAt(idx);
                RenumberFromGrid();
                RegenerateFromOutputs();
            };

            btnUp.Click += (s, e) =>
            {
                int idx = _fromGrid.CurrentRow?.Index ?? -1;
                if (idx <= 0) return;
                SwapFromRows(idx, idx - 1);
                _fromGrid.CurrentCell = _fromGrid.Rows[idx - 1].Cells["Display"];
                RegenerateFromOutputs();
            };

            btnDown.Click += (s, e) =>
            {
                int idx = _fromGrid.CurrentRow?.Index ?? -1;
                if (idx < 0 || idx >= _fromGrid.Rows.Count - 1) return;
                SwapFromRows(idx, idx + 1);
                _fromGrid.CurrentCell = _fromGrid.Rows[idx + 1].Cells["Display"];
                RegenerateFromOutputs();
            };

            btnUpdate.Click += (s, e) =>
            {
                var current = GetEntriesFromGrid();
                if (current.Count == 0) { SetStatus("No entries to write."); return; }
                string fromStr = FromParser.GenerateFromLines(current);
                _writeBackFrom(fromStr);
                SetStatus("Source updated.");
            };

            int x = 4;
            foreach (var btn in new Button[] { btnAdd, btnDelete, btnUp, btnDown, btnUpdate })
            { btn.Left = x; btn.Top = 3; x += btn.Width + 4; }

            var crudPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            crudPanel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnUp, btnDown, btnUpdate });

            panel.Controls.Add(_fromGrid);
            panel.Controls.Add(crudPanel);
            return panel;
        }

        private void AddFromRow(int no, string display, string value)
        {
            int idx = _fromGrid.Rows.Add();
            _fromGrid.Rows[idx].Cells["No"].Value      = no;
            _fromGrid.Rows[idx].Cells["Display"].Value = display;
            _fromGrid.Rows[idx].Cells["Value"].Value   = value;
        }

        private void RenumberFromGrid()
        {
            for (int i = 0; i < _fromGrid.Rows.Count; i++)
                _fromGrid.Rows[i].Cells["No"].Value = i + 1;
        }

        private void SwapFromRows(int a, int b)
        {
            string dispA = _fromGrid.Rows[a].Cells["Display"].Value as string ?? "";
            string valA  = _fromGrid.Rows[a].Cells["Value"].Value  as string ?? "";
            string dispB = _fromGrid.Rows[b].Cells["Display"].Value as string ?? "";
            string valB  = _fromGrid.Rows[b].Cells["Value"].Value  as string ?? "";
            _fromGrid.Rows[a].Cells["Display"].Value = dispB;
            _fromGrid.Rows[a].Cells["Value"].Value   = valB;
            _fromGrid.Rows[b].Cells["Display"].Value = dispA;
            _fromGrid.Rows[b].Cells["Value"].Value   = valA;
        }

        private List<FromParser.FromEntry> GetEntriesFromGrid()
        {
            var list = new List<FromParser.FromEntry>();
            if (_fromGrid == null) return list;
            foreach (DataGridViewRow row in _fromGrid.Rows)
            {
                string disp = row.Cells["Display"].Value as string ?? "";
                if (string.IsNullOrWhiteSpace(disp)) continue;
                string val  = row.Cells["Value"].Value as string ?? "";
                list.Add(new FromParser.FromEntry
                {
                    Display = disp,
                    Value   = string.IsNullOrEmpty(val) ? null : val,
                });
            }
            return list;
        }

        private void RegenerateFromOutputs()
        {
            var entries = GetEntriesFromGrid();
            if (_txtFromLines != null)
                _txtFromLines.Text = FromParser.GenerateFromLines(entries);
            if (_txtCase != null)
                _txtCase.Text = CopyFromCaseCommand.GenerateCaseCode(entries, _useVar);
            if (_txtReformat != null)
            {
                string r = FromParser.BuildReformattedSourceLine(_flatSourceLine, entries);
                _txtReformat.Text = r ?? "(source line unavailable)";
            }
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
        private TabPage BuildFromTab(List<FromParser.FromEntry> entries, string useVar, string flatSourceLine)
        {
            var page = new TabPage("FROM");

            // Left: FROM entries in Clarion continuation format (editable so align/split work)
            _txtFromLines = new TextBox
            {
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = FromParser.GenerateFromLines(entries),
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };
            var txtFrom = _txtFromLines;

            // Right: CASE output (read-only)
            string caseCode = CopyFromCaseCommand.GenerateCaseCode(entries, useVar);
            _txtCase = new TextBox
            {
                ReadOnly   = true,
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Text       = caseCode,
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };
            var txtCase = _txtCase;

            var split = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                Orientation      = Orientation.Vertical,
                SplitterDistance = 300,
                SplitterWidth    = 6,
            };
            StyleSplitter(split);
            split.Panel1.Controls.Add(txtFrom);
            split.Panel2.Controls.Add(txtCase);

            // ── Reformatted source line at bottom ───────────────────────────
            string reformatted = FromParser.BuildReformattedSourceLine(flatSourceLine, entries);
            var reformatPanel = new Panel { Dock = DockStyle.Bottom, Height = 46 };
            var lblReformat   = new Label
            {
                Text      = "Reformatted source line:",
                Dock      = DockStyle.Top,
                Height    = 16,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = SystemColors.GrayText,
            };
            _txtReformat = new TextBox
            {
                ReadOnly   = true,
                Multiline  = false,
                ScrollBars = ScrollBars.Horizontal,
                Font       = new Font("Consolas", 8.5f),
                BackColor  = SystemColors.ControlLight,
                Text       = reformatted ?? "(source line unavailable)",
                Dock       = DockStyle.Fill,
            };
            reformatPanel.Controls.Add(_txtReformat);
            reformatPanel.Controls.Add(lblReformat);

            // ── Button bar ──────────────────────────────────────────────────
            var btnAlignHash  = new Button { Text = "Align #",        Width = 70,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnAlignQuote = new Button { Text = "Align \"",       Width = 70,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnSplitHash  = new Button { Text = "Split #",        Width = 70,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnCopyFrom   = new Button { Text = "Copy FROM",      Width = 90,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnCopyCase   = new Button { Text = "Copy CASE",      Width = 90,  Height = 26, Font = new Font("Segoe UI", 9f) };
            var btnCopyReformat = new Button { Text = "Copy Source Line", Width = 110, Height = 26, Font = new Font("Segoe UI", 9f) };

            btnAlignHash.Click  += (s, e) => { txtFrom.Text = AlignHash(txtFrom.Text);  SetStatus("# values aligned."); };
            btnAlignQuote.Click += (s, e) => { txtFrom.Text = AlignQuote(txtFrom.Text); SetStatus("Quotes aligned."); };
            btnSplitHash.Click  += (s, e) => { txtFrom.Text = SplitHash(txtFrom.Text);  SetStatus("# values split."); };
            btnCopyFrom.Click   += (s, e) => { Clipboard.SetText(txtFrom.Text); SetStatus("FROM lines copied."); };
            btnCopyCase.Click   += (s, e) => { Clipboard.SetText(txtCase.Text); SetStatus("CASE code copied."); };
            btnCopyReformat.Click += (s, e) =>
            {
                if (_txtReformat != null && !string.IsNullOrEmpty(_txtReformat.Text))
                { Clipboard.SetText(_txtReformat.Text); SetStatus("Source line copied."); }
            };

            int x = 4;
            foreach (var btn in new[] { btnAlignHash, btnAlignQuote, btnSplitHash, btnCopyFrom, btnCopyCase, btnCopyReformat })
            { btn.Left = x; btn.Top = 3; x += btn.Width + 4; }

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            btnPanel.Controls.AddRange(new Control[] { btnAlignHash, btnAlignQuote, btnSplitHash, btnCopyFrom, btnCopyCase, btnCopyReformat });

            page.Controls.Add(split);
            page.Controls.Add(reformatPanel);
            page.Controls.Add(btnPanel);
            return page;
        }

        // ── FROM alignment/split helpers (ported from Carl's Clarion routines) ──

        private static string AlignHash(string fromLines)
        {
            string[] lines = fromLines.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            int maxPos = 0;
            foreach (string line in lines)
            {
                int p = line.IndexOf("|#", System.StringComparison.Ordinal);
                if (p > maxPos) maxPos = p;
            }
            if (maxPos == 0) return fromLines;
            var sb = new StringBuilder();
            foreach (string line in lines)
            {
                int p = line.IndexOf("|#", System.StringComparison.Ordinal);
                if (p > 0 && p < maxPos)
                    sb.AppendLine(line.Substring(0, p) + new string(' ', maxPos - p) + line.Substring(p));
                else
                    sb.AppendLine(line);
            }
            return sb.ToString().TrimEnd('\r', '\n');
        }

        private static string AlignQuote(string fromLines)
        {
            // Remove leading spaces from quote-starting lines, then re-indent to 6 spaces
            string[] lines = fromLines.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            var sb = new StringBuilder();
            foreach (string line in lines)
            {
                string trimmed = line.TrimStart();
                if (trimmed.Length > 0 && trimmed[0] == '\'')
                {
                    // Determine indent: '| = 6 spaces, '' (closing) = 6 spaces, else 7
                    int spaces = (trimmed.Length >= 2 && (trimmed[1] == '|' || trimmed[1] == '\'')) ? 6 : 7;
                    sb.AppendLine(new string(' ', spaces) + trimmed);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString().TrimEnd('\r', '\n');
        }

        private static string SplitHash(string fromLines)
        {
            // First normalize indentation, then split 'Item|#value' onto two lines
            string normalized = AlignQuote(fromLines);
            string[] lines = normalized.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            int maxPos = 0;
            foreach (string line in lines)
            {
                int p = line.IndexOf("|#", System.StringComparison.Ordinal);
                if (p <= 0) continue;
                // Skip if already split (preceded by &')
                if (p >= 2 && line.Substring(p - 2, 2) == "&'") continue;
                if (p > maxPos) maxPos = p;
            }
            if (maxPos == 0) return normalized;
            var sb = new StringBuilder();
            foreach (string line in lines)
            {
                int p = line.IndexOf("|#", System.StringComparison.Ordinal);
                if (p > 0 && !(p >= 2 && line.Substring(p - 2, 2) == "&'"))
                {
                    // Split: 'Item'  padding &'|#value'
                    string before = line.Substring(0, p);
                    string after  = line.Substring(p);
                    int pad = maxPos - p;
                    sb.AppendLine(before + "'" + new string(' ', pad > 0 ? pad : 0) + " &'" + after);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString().TrimEnd('\r', '\n');
        }

        // ════════════════════════════════════════════════════════════════════
        // Modifiers Help tab — static FORMAT() modifier reference
        // ════════════════════════════════════════════════════════════════════
        private static TabPage BuildHelpTab()
        {
            var page = new TabPage("Modifiers");

            // Syntax summary at top
            var syntax = new TextBox
            {
                ReadOnly   = true,
                Multiline  = true,
                Dock       = DockStyle.Top,
                Height     = 46,
                Font       = new Font("Consolas", 8.5f),
                BackColor  = SystemColors.ControlLight,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.None,
                Text       = "Column syntax:  Width  Justification LRCD  (Indent)  Modifiers  ~Header~LRCD(Indent)  @picture@  #FieldNo#\r\n" +
                             "Group  syntax:  [Columns]  (GroupWidth)  Modifiers  ~GroupHeader~Justification(Indent)\r\n" +
                             "Six modifiers require extra QUEUE fields after the data field:  * Color   I Icon   J Icon(Transparent)   T Tree   Y Style   P Tip",
            };

            // Full description panel at bottom
            var descBox = new TextBox
            {
                ReadOnly    = true,
                Multiline   = true,
                Dock        = DockStyle.Bottom,
                Height      = 70,
                Font        = new Font("Segoe UI", 9f),
                BackColor   = SystemColors.Info,
                ForeColor   = SystemColors.InfoText,
                BorderStyle = BorderStyle.None,
                ScrollBars  = ScrollBars.Vertical,
                Text        = "Click a modifier row to see the full description here.",
            };

            // Modifier grid
            var grid = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                ReadOnly                    = true,
                AllowUserToAddRows          = false,
                AllowUserToResizeRows       = false,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                 = false,
                RowHeadersVisible           = false,
                BackgroundColor             = SystemColors.Window,
                GridColor                   = SystemColors.ControlLight,
                Font                        = new Font("Consolas", 9f),
                BorderStyle                 = BorderStyle.None,
                CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.AllCells,
            };

            grid.Columns.Add("Char",     "Char");
            grid.Columns.Add("Type",     "Type");
            grid.Columns.Add("PropList", "PROPLIST:");
            grid.Columns.Add("Name",     "Name / Purpose");
            grid.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Type column — colour-coded background
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Type") return;
                string t = e.Value as string ?? "";
                switch (t)
                {
                    case "Align": e.CellStyle.BackColor = System.Drawing.Color.FromArgb(220, 235, 255); break;
                    case "Head":  e.CellStyle.BackColor = System.Drawing.Color.FromArgb(220, 255, 220); break;
                    case "Color": e.CellStyle.BackColor = System.Drawing.Color.FromArgb(255, 230, 220); break;
                    case "Icon":  e.CellStyle.BackColor = System.Drawing.Color.FromArgb(255, 245, 200); break;
                    case "Tree":  e.CellStyle.BackColor = System.Drawing.Color.FromArgb(230, 255, 240); break;
                    case "Style": e.CellStyle.BackColor = System.Drawing.Color.FromArgb(245, 220, 255); break;
                    case "Tip":   e.CellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 210); break;
                    case "Flag":  e.CellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240); break;
                    case "Group": e.CellStyle.BackColor = System.Drawing.Color.FromArgb(210, 240, 255); break;
                    case "Data":  e.CellStyle.BackColor = System.Drawing.Color.FromArgb(255, 250, 230); break;
                }
                e.FormattingApplied = true;
            };

            // Populate rows
            foreach (var entry in ModifierData.All)
                grid.Rows.Add(entry.Char, entry.Type, entry.PropList, entry.Name);

            // Tag each row with its full description for the detail panel
            for (int i = 0; i < ModifierData.All.Count; i++)
                grid.Rows[i].Tag = ModifierData.All[i].Description;

            // Show description when row selected
            grid.SelectionChanged += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0)
                    descBox.Text = grid.SelectedRows[0].Tag as string ?? "";
            };

            // Column header click sorting
            grid.ColumnHeaderMouseClick += (s, e) =>
            {
                var col = grid.Columns[e.ColumnIndex];
                var dir = (col.HeaderCell.SortGlyphDirection == System.Windows.Forms.SortOrder.Ascending)
                    ? System.Windows.Forms.SortOrder.Descending
                    : System.Windows.Forms.SortOrder.Ascending;
                grid.Sort(col, dir == System.Windows.Forms.SortOrder.Ascending
                    ? System.ComponentModel.ListSortDirection.Ascending
                    : System.ComponentModel.ListSortDirection.Descending);
                col.HeaderCell.SortGlyphDirection = dir;
            };

            page.Controls.Add(grid);
            page.Controls.Add(descBox);
            page.Controls.Add(syntax);
            return page;
        }


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
