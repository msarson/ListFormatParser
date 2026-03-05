using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ListFormatParser
{
    /// <summary>
    /// Standalone dialog — paste a QUEUE or FILE declaration, parse it into fields,
    /// tweak preferences, and generate FORMAT() + #FIELDS() output.
    /// </summary>
    internal class Queue2FormatForm : Form
    {
        private TextBox           _txtQueue;
        private TextBox           _txtFormat;
        private TextBox           _txtFields;
        private DataGridView      _grid;
        private ToolStripStatusLabel _statusLabel;

        private Queue2Prefs       _prefs  = new Queue2Prefs();
        private List<QueueField>  _fields = new List<QueueField>();
        private string            _queuePre = "Q:";

        // Pref controls
        private NumericUpDown _numWidthMin, _numWidthMax;
        private TextBox       _txtDatePic, _txtTimePic;
        private CheckBox      _chkIntMinus, _chkIntCommas, _chkIntBlankB;
        private CheckBox      _chkDecMinus, _chkDecCommas, _chkDecBlankB;
        private CheckBox      _chkLongDateTime;
        private CheckBox      _chkResize, _chkUnderline, _chkFixed, _chkColored, _chkStyle;
        private CheckBox      _chkOneLine, _chkHdrCenter;
        private NumericUpDown _numDigByte, _numDigShort, _numDigLong;

        public Queue2FormatForm()
        {
            Text          = "Queue / File → FORMAT Generator";
            Size          = new Size(1100, 700);
            MinimumSize   = new Size(800, 500);
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Segoe UI", 9f);
            MinimizeBox   = false;

            // Icon
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly()
                       .GetManifestResourceStream("ListFormatParser.Resources.FormIcon.png"))
            {
                if (stream != null)
                    using (var bmp = new System.Drawing.Bitmap(stream))
                    {
                        IntPtr h = bmp.GetHicon();
                        using (var tmp = Icon.FromHandle(h))
                            Icon = new Icon(tmp, tmp.Size);
                        NativeMethods.DestroyIcon(h);
                    }
            }

            // ── Status bar ───────────────────────────────────────────────────────
            var statusStrip = new StatusStrip();
            _statusLabel    = new ToolStripStatusLabel("Paste a QUEUE or FILE declaration above and click Process Queue.");
            statusStrip.Items.Add(_statusLabel);

            // ── Main vertical split: top (input+prefs+grid) / bottom (output) ───
            var splitMain = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                Orientation      = Orientation.Horizontal,
                SplitterDistance = 310,
                SplitterWidth    = 6,
            };
            StyleSplitter(splitMain);

            // ── Top: queue text left | prefs + grid right ────────────────────────
            var splitTop = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                Orientation      = Orientation.Vertical,
                SplitterDistance = 320,
                SplitterWidth    = 6,
            };
            StyleSplitter(splitTop);

            // Left: queue text input
            _txtQueue = new TextBox
            {
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                Dock       = DockStyle.Fill,
                WordWrap   = false,
            };
            var btnProcess = new Button
            {
                Text      = "▶  Process Queue",
                Height    = 28,
                Dock      = DockStyle.Top,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            var lblQueueHint = new Label
            {
                Text      = "Paste QUEUE / FILE declaration:",
                Dock      = DockStyle.Top,
                Height    = 16,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = SystemColors.GrayText,
            };
            btnProcess.Click += (s, e) => ProcessQueue();
            splitTop.Panel1.Controls.Add(_txtQueue);
            splitTop.Panel1.Controls.Add(btnProcess);
            splitTop.Panel1.Controls.Add(lblQueueHint);

            // Right: prefs panel on top, parsed fields grid below
            var splitRight = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                Orientation      = Orientation.Horizontal,
                SplitterDistance = 128,
                SplitterWidth    = 6,
            };
            StyleSplitter(splitRight);
            splitRight.Panel1.Controls.Add(BuildPrefsPanel());
            splitRight.Panel2.Controls.Add(BuildFieldsGrid());
            splitTop.Panel2.Controls.Add(splitRight);

            splitMain.Panel1.Controls.Add(splitTop);

            // ── Bottom: FORMAT output left | #FIELDS output right ────────────────
            var splitBot = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                Orientation      = Orientation.Vertical,
                SplitterDistance = 560,
                SplitterWidth    = 6,
            };
            StyleSplitter(splitBot);

            _txtFormat = new TextBox
            {
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };
            _txtFields = new TextBox
            {
                Multiline  = true,
                ScrollBars = ScrollBars.Both,
                Font       = new Font("Consolas", 9f),
                WordWrap   = false,
                Dock       = DockStyle.Fill,
            };

            var btnGenerate   = new Button { Text = "⚙  Generate", Height = 26, Dock = DockStyle.Top,
                                             BackColor = Color.FromArgb(16, 124, 16), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnCopyFormat = new Button { Text = "Copy FORMAT",  Height = 26, Dock = DockStyle.Top };
            var btnCopyFields = new Button { Text = "Copy #FIELDS", Height = 26, Dock = DockStyle.Top };

            btnGenerate.Click   += (s, e) => GenerateFormat();
            btnCopyFormat.Click += (s, e) => { Clipboard.SetText(_txtFormat.Text); SetStatus("FORMAT copied."); };
            btnCopyFields.Click += (s, e) => { Clipboard.SetText(_txtFields.Text); SetStatus("#FIELDS copied."); };

            var lblFmt = new Label { Text = "FORMAT( )", Height = 16, Dock = DockStyle.Top,
                                     Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = SystemColors.GrayText };
            var lblFlds = new Label { Text = "#FIELDS( )", Height = 16, Dock = DockStyle.Top,
                                      Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = SystemColors.GrayText };

            splitBot.Panel1.Controls.Add(_txtFormat);
            splitBot.Panel1.Controls.Add(btnCopyFormat);
            splitBot.Panel1.Controls.Add(btnGenerate);
            splitBot.Panel1.Controls.Add(lblFmt);

            splitBot.Panel2.Controls.Add(_txtFields);
            splitBot.Panel2.Controls.Add(btnCopyFields);
            splitBot.Panel2.Controls.Add(lblFlds);

            splitMain.Panel2.Controls.Add(splitBot);

            Controls.Add(splitMain);
            Controls.Add(statusStrip);
        }

        // ── Preferences panel ─────────────────────────────────────────────────────
        private Panel BuildPrefsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            int x = 6, y = 5, rh = 22;

            // Row 1 — width range + date + time pics
            L(panel, "Width:", x, y);
            _numWidthMin = N(panel, x + 40, y, 20, 999, 20);
            L(panel, "–", x + 78, y);
            _numWidthMax = N(panel, x + 87, y, 20, 999, 200);
            L(panel, "Date @:", x + 128, y);
            _txtDatePic = T(panel, x + 176, y, 42, "d1");
            L(panel, "Time @:", x + 224, y);
            _txtTimePic = T(panel, x + 272, y, 42, "t1");
            y += rh;

            // Row 2 — integer opts
            L(panel, "Integer:", x, y);
            _chkIntMinus  = C(panel, "−",  x + 52,  y, true);
            _chkIntCommas = C(panel, ",",  x + 72,  y, true);
            _chkIntBlankB = C(panel, "b",  x + 90,  y, false);
            L(panel, "Decimal:", x + 118, y);
            _chkDecMinus  = C(panel, "−",  x + 170, y, true);
            _chkDecCommas = C(panel, ",",  x + 190, y, true);
            _chkDecBlankB = C(panel, "b",  x + 208, y, false);
            _chkLongDateTime = C(panel, "LONG finds Date/Time label", x + 236, y, true);
            y += rh;

            // Row 3 — digit widths
            L(panel, "Digits BYTE:", x, y);
            _numDigByte  = N(panel, x + 74, y, 1, 9, 3);
            L(panel, "SHORT:", x + 114, y);
            _numDigShort = N(panel, x + 148, y, 1, 9, 5);
            L(panel, "LONG:", x + 188, y);
            _numDigLong  = N(panel, x + 218, y, 1, 15, 10);
            _chkHdrCenter = C(panel, "Center header for right-data cols", x + 264, y, true);
            y += rh;

            // Row 4 — modifiers + layout
            L(panel, "Modifiers:", x, y);
            _chkResize    = C(panel, "M Resize",  x + 58,  y, true);
            _chkUnderline = C(panel, "_ Under",   x + 124, y, false);
            _chkFixed     = C(panel, "F Fixed",   x + 184, y, false);
            _chkColored   = C(panel, "* Colored", x + 238, y, false);
            _chkStyle     = C(panel, "Y Style",   x + 306, y, false);
            _chkOneLine   = C(panel, "One column/line", x + 362, y, true);

            return panel;
        }

        private void L(Panel p, string text, int x, int y)
            => p.Controls.Add(new Label { Text = text, Left = x, Top = y + 3, AutoSize = true });

        private NumericUpDown N(Panel p, int x, int y, int min, int max, int val)
        {
            var n = new NumericUpDown { Left = x, Top = y, Width = 36, Height = 20, Minimum = min, Maximum = max, Value = val };
            p.Controls.Add(n);
            return n;
        }

        private TextBox T(Panel p, int x, int y, int w, string text)
        {
            var t = new TextBox { Left = x, Top = y, Width = w, Height = 20, Text = text, Font = new Font("Consolas", 8.5f) };
            p.Controls.Add(t);
            return t;
        }

        private CheckBox C(Panel p, string text, int x, int y, bool chk)
        {
            var cb = new CheckBox { Text = text, Left = x, Top = y, AutoSize = true, Checked = chk };
            p.Controls.Add(cb);
            return cb;
        }

        // ── Fields grid ───────────────────────────────────────────────────────────
        private Panel BuildFieldsGrid()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var lbl = new Label { Text = "Parsed fields:", Dock = DockStyle.Top, Height = 16,
                                   Font = new Font("Segoe UI", 8f), ForeColor = SystemColors.GrayText };

            _grid = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                ReadOnly                    = true,
                AllowUserToAddRows          = false,
                AllowUserToResizeRows       = false,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible           = false,
                BackgroundColor             = SystemColors.Window,
                GridColor                   = SystemColors.ControlLight,
                Font                        = new Font("Consolas", 8.5f),
                BorderStyle                 = BorderStyle.None,
                CellBorderStyle            = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "No",    HeaderText = "#",        Width = 28 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label",    Width = 130 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",  HeaderText = "Type",     Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Omit",  HeaderText = "Omit",     Width = 40 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pic",   HeaderText = "Picture",  Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Width", HeaderText = "Width",    Width = 42 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Group", HeaderText = "[G]",      Width = 30 });

            _grid.Columns["No"].DefaultCellStyle.Alignment    = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns["Width"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            panel.Controls.Add(_grid);
            panel.Controls.Add(lbl);
            return panel;
        }

        // ── Core actions ──────────────────────────────────────────────────────────
        private void ProcessQueue()
        {
            string txt = _txtQueue.Text;
            if (string.IsNullOrWhiteSpace(txt)) { SetStatus("Paste a QUEUE/FILE declaration first."); return; }

            ReadPrefs();
            string queueName;
            _fields = QueueFieldParser.Parse(txt, out queueName, out _queuePre);
            QueueFieldParser.ComputePictures(_fields, _prefs);
            PopulateGrid();

            int active = 0;
            foreach (var f in _fields)
                if (!f.IsGroupOpen && !f.IsGroupClose && f.OmitHow != "Omit") active++;
            SetStatus($"Parsed {_fields.Count} rows → {active} active fields.  Click ⚙ Generate.");

            GenerateFormat();
        }

        private void PopulateGrid()
        {
            _grid.Rows.Clear();
            int no = 0;
            foreach (var f in _fields)
            {
                no++;
                int idx = _grid.Rows.Add();
                _grid.Rows[idx].Cells["No"].Value = no;

                if (f.IsGroupOpen)
                {
                    _grid.Rows[idx].Cells["Group"].Value = "[";
                    _grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                }
                else if (f.IsGroupClose)
                {
                    _grid.Rows[idx].Cells["Label"].Value = f.GroupHeader;
                    _grid.Rows[idx].Cells["Group"].Value = "]";
                    _grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                }
                else
                {
                    _grid.Rows[idx].Cells["Label"].Value = f.Label;
                    _grid.Rows[idx].Cells["Type"].Value  = f.Size > 0
                        ? f.Type + "(" + f.Size + (f.Decimals > 0 ? "," + f.Decimals : "") + ")"
                        : f.Type;
                    _grid.Rows[idx].Cells["Omit"].Value  = f.OmitHow;
                    _grid.Rows[idx].Cells["Pic"].Value   = f.Picture;
                    _grid.Rows[idx].Cells["Width"].Value = f.CharsWide > 0 ? f.CharsWide.ToString() : "";

                    if (f.OmitHow == "Omit")
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = SystemColors.GrayText;
                    else if (f.OmitHow == "Hide")
                        _grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(220, 220, 220);
                }
            }
        }

        private void GenerateFormat()
        {
            if (_fields.Count == 0) { SetStatus("Process a QUEUE first."); return; }
            ReadPrefs();
            QueueFieldParser.ComputePictures(_fields, _prefs);
            PopulateGrid();
            _txtFormat.Text = QueueFieldParser.GenerateFormat(_fields, _prefs);
            _txtFields.Text = QueueFieldParser.GenerateFields(_fields, _prefs);
            SetStatus("Format generated.");
        }

        private void ReadPrefs()
        {
            _prefs.WidthMin           = (int)_numWidthMin.Value;
            _prefs.WidthMax           = (int)_numWidthMax.Value;
            _prefs.DatePic            = _txtDatePic.Text.Trim().TrimStart('@');
            _prefs.TimePic            = _txtTimePic.Text.Trim().TrimStart('@');
            _prefs.IntMinus           = _chkIntMinus.Checked;
            _prefs.IntCommas          = _chkIntCommas.Checked;
            _prefs.IntBlankB          = _chkIntBlankB.Checked ? "b" : "";
            _prefs.DecMinus           = _chkDecMinus.Checked;
            _prefs.DecCommas          = _chkDecCommas.Checked;
            _prefs.DecBlankB          = _chkDecBlankB.Checked ? "b" : "";
            _prefs.LongLook4DateTime  = _chkLongDateTime.Checked;
            _prefs.HdrCenterDataRight = _chkHdrCenter.Checked;
            _prefs.Resize             = _chkResize.Checked;
            _prefs.Underline          = _chkUnderline.Checked;
            _prefs.Fixed              = _chkFixed.Checked;
            _prefs.Colored            = _chkColored.Checked;
            _prefs.CellStyle          = _chkStyle.Checked;
            _prefs.OnePerLine         = _chkOneLine.Checked;
            _prefs.DigitsByte         = (int)_numDigByte.Value;
            _prefs.DigitsShort        = (int)_numDigShort.Value;
            _prefs.DigitsLong         = (int)_numDigLong.Value;
        }

        private void SetStatus(string msg) => _statusLabel.Text = msg;

        private static void StyleSplitter(SplitContainer sc)
        {
            sc.Paint += (s, e) =>
                e.Graphics.FillRectangle(SystemBrushes.ControlDark, sc.SplitterRectangle);
        }
    }
}
