using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ListFormatParser
{
    internal class QueueField
    {
        public string Label       = "";
        public string Type        = "";     // STRING, LONG, SHORT, BYTE, DATE, TIME, DECIMAL …
        public int    Size        = 0;      // STRING(30) → 30
        public int    Decimals    = 0;      // DECIMAL(9,2) → 2
        public string OmitHow    = "";     // "" | "Omit" | "Hide"
        public string BangPic    = "";     // !@xxx override picture from comment
        // Group markers — mutually exclusive with field fields
        public bool   IsGroupOpen  = false;
        public bool   IsGroupClose = false;
        public string GroupHeader  = "";   // text after !]
        // Computed by ComputePictures()
        public string Picture      = "";
        public int    CharsWide    = 0;
        public char   Justification = 'L'; // L | R | C
        public string PreLabel    = "";   // Pre:Label
    }

    internal class Queue2Prefs
    {
        public int    WidthMin           = 20;
        public int    WidthMax           = 200;
        public string DatePic            = "d1";
        public string TimePic            = "t1";
        public bool   IntMinus           = true;
        public bool   IntCommas          = true;
        public string IntBlankB          = "";
        public bool   DecMinus           = true;
        public bool   DecCommas          = true;
        public string DecBlankB          = "";
        public int    DigitsByte         = 3;
        public int    DigitsShort        = 5;
        public int    DigitsLong         = 10;
        public int    DigitsBool         = 1;
        public bool   LongLook4DateTime  = true;
        public bool   Resize             = true;   // M
        public bool   Underline          = false;  // _
        public bool   Fixed              = false;  // F
        public bool   Colored            = false;  // *
        public bool   CellStyle          = false;  // Y
        public bool   FieldNumbered      = false;  // #n#
        public bool   HdrRow             = true;
        public char   HdrJust            = 'L';
        public int    HdrIndent          = 0;
        public bool   HdrCenterDataRight = true;
        public int    DataIndent         = 2;
        public bool   OnePerLine         = true;
    }

    internal static class QueueFieldParser
    {
        // ── Parse a QUEUE / FILE declaration into a list of fields ─────────────
        internal static List<QueueField> Parse(string queueText, out string queueName, out string queuePre)
        {
            queueName = "Q";
            queuePre  = "Q:";
            var fields = new List<QueueField>();
            if (string.IsNullOrWhiteSpace(queueText)) return fields;

            string[] lines = queueText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inDecl = false;

            foreach (string rawLine in lines)
            {
                string line     = rawLine.Trim();
                if (line.Length == 0) continue;

                string lineUpper = line.ToUpperInvariant();

                // Group open marker in comment: ![
                if (lineUpper.StartsWith("!["))
                {
                    fields.Add(new QueueField { IsGroupOpen = true });
                    inDecl = true;
                    continue;
                }
                // Group close marker: !]optional header text
                if (lineUpper.StartsWith("!]"))
                {
                    string header = line.Length > 2 ? line.Substring(2).Trim() : "";
                    fields.Add(new QueueField { IsGroupClose = true, GroupHeader = header });
                    continue;
                }
                // Pure comment line
                if (line.StartsWith("!")) continue;

                // END — stop
                if (lineUpper == "END" || lineUpper.StartsWith("END ") || lineUpper.StartsWith("END\t") ||
                    lineUpper.StartsWith("END!"))
                    break;

                // QUEUE / FILE declaration line — extract name and PRE()
                if (lineUpper.Contains("QUEUE") || lineUpper.Contains(" FILE(") || lineUpper.Contains(",FILE("))
                {
                    if (!inDecl)
                    {
                        inDecl = true;
                        string[] tok = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tok.Length > 0) queueName = tok[0];
                        var mPre = Regex.Match(line, @"PRE\s*\(\s*(\w+)\s*\)", RegexOptions.IgnoreCase);
                        if (mPre.Success) queuePre = mPre.Groups[1].Value + ":";
                        else              queuePre = queueName + ":";
                    }
                    continue;
                }

                if (!inDecl) inDecl = true;

                var field = ParseFieldLine(line);
                if (field != null) fields.Add(field);
            }

            // Set PreLabel for all regular fields
            foreach (var f in fields)
                if (!f.IsGroupOpen && !f.IsGroupClose && f.Label.Length > 0)
                    f.PreLabel = queuePre + f.Label;

            return fields;
        }

        private static QueueField ParseFieldLine(string line)
        {
            // Separate code from comment
            string code    = line;
            string comment = "";
            int bangPos    = FindBangOutsideString(line);
            if (bangPos >= 0)
            {
                code    = line.Substring(0, bangPos).TrimEnd();
                comment = line.Substring(bangPos + 1).Trim();
            }

            string[] tokens = code.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2) return null;

            string label    = tokens[0];
            string typePart = tokens[1];
            string typeUpper = typePart.ToUpperInvariant();

            // Skip keywords that aren't field declarations
            string bareType = typeUpper.Split('(')[0];
            if (bareType == "QUEUE" || bareType == "FILE" || bareType == "GROUP" ||
                bareType == "END"   || bareType == "WINDOW" || bareType == "APPLICATION")
                return null;

            // Extract TYPE and optional size params
            var mType = Regex.Match(typePart, @"^(\w+)(?:\(([^)]+)\))?$", RegexOptions.IgnoreCase);
            string typeName = mType.Success ? mType.Groups[1].Value.ToUpperInvariant() : bareType;
            int size = 0, decimals = 0;
            if (mType.Success && mType.Groups[2].Success)
            {
                string[] nums = mType.Groups[2].Value.Split(',');
                int.TryParse(nums[0].Trim(), out size);
                if (nums.Length > 1) int.TryParse(nums[1].Trim(), out decimals);
            }

            if (typeName == "LIKE" || typeName == "OVER") return null;

            // Parse comment directives
            string omitHow = "";
            string bangPic = "";
            if (comment.Length > 0)
            {
                string cu = comment.ToUpperInvariant();
                if (cu.StartsWith("@"))
                    bangPic = comment.Substring(1).Trim().TrimEnd('@');
                else if (cu.Contains("OMIT"))  omitHow = "Omit";
                else if (cu.Contains("HIDE"))  omitHow = "Hide";
            }

            return new QueueField
            {
                Label   = label,
                Type    = typeName,
                Size    = size,
                Decimals = decimals,
                OmitHow = omitHow,
                BangPic = bangPic,
            };
        }

        // Find '!' not inside a Clarion string literal
        private static int FindBangOutsideString(string line)
        {
            bool inStr = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (!inStr && c == '\'') { inStr = true; continue; }
                if (inStr && c == '\'' && i + 1 < line.Length && line[i + 1] == '\'') { i++; continue; }
                if (inStr && c == '\'') { inStr = false; continue; }
                if (!inStr && c == '!') return i;
            }
            return -1;
        }

        // ── Compute picture and width for each field ─────────────────────────────
        internal static void ComputePictures(List<QueueField> fields, Queue2Prefs prefs)
        {
            foreach (var f in fields)
            {
                if (f.IsGroupOpen || f.IsGroupClose) continue;
                if (f.OmitHow == "Omit") continue;
                ComputeOne(f, prefs);
            }
        }

        private static void ComputeOne(QueueField f, Queue2Prefs prefs)
        {
            // Override from !@xxx comment
            if (f.BangPic.Length > 0)
            {
                f.Picture = f.BangPic;
                var ms = Regex.Match(f.BangPic, @"^s(\d+)", RegexOptions.IgnoreCase);
                f.CharsWide = ms.Success
                    ? Math.Min(int.Parse(ms.Groups[1].Value), prefs.WidthMax)
                    : DatePicWidth(f.BangPic);
                f.CharsWide = Math.Max(f.CharsWide, prefs.WidthMin);
                f.Justification = 'L';
                return;
            }

            // LONG/ULONG — check label for Date/Time keywords
            if ((f.Type == "LONG" || f.Type == "ULONG") && prefs.LongLook4DateTime)
            {
                string lu = f.Label.ToUpperInvariant();
                if (lu.Contains("DATE")) { ApplyDate(f, prefs); return; }
                if (lu.Contains("TIME")) { ApplyTime(f, prefs); return; }
            }

            switch (f.Type)
            {
                case "STRING":
                    f.Picture      = "s" + Math.Max(f.Size, 1);
                    f.CharsWide    = Math.Min(Math.Max(f.Size, 1), prefs.WidthMax);
                    f.Justification = 'L';
                    break;
                case "CSTRING":
                case "PSTRING":
                    int cs = Math.Max(f.Size - 1, 1);
                    f.Picture      = "s" + cs;
                    f.CharsWide    = Math.Min(cs, prefs.WidthMax);
                    f.Justification = 'L';
                    break;
                case "DATE":
                    ApplyDate(f, prefs);
                    break;
                case "TIME":
                    ApplyTime(f, prefs);
                    break;
                case "LONG":
                case "ULONG":
                case "SIGNED":
                case "UNSIGNED":
                case "COUNT_T":
                case "POINTER_T":
                    f.Picture      = BuildNumPic(prefs.DigitsLong, 0, prefs.IntMinus, prefs.IntCommas, prefs.IntBlankB);
                    f.CharsWide    = NumPicWidth(prefs.DigitsLong, 0, prefs.IntMinus, prefs.IntCommas);
                    f.Justification = 'R';
                    break;
                case "SHORT":
                case "USHORT":
                    f.Picture      = BuildNumPic(prefs.DigitsShort, 0, prefs.IntMinus, prefs.IntCommas, prefs.IntBlankB);
                    f.CharsWide    = NumPicWidth(prefs.DigitsShort, 0, prefs.IntMinus, prefs.IntCommas);
                    f.Justification = 'R';
                    break;
                case "BYTE":
                case "UBYTE":
                    f.Picture      = BuildNumPic(prefs.DigitsByte, 0, false, false, "");
                    f.CharsWide    = NumPicWidth(prefs.DigitsByte, 0, false, false);
                    f.Justification = 'R';
                    break;
                case "BOOL":
                    f.Picture      = "n" + prefs.DigitsBool;
                    f.CharsWide    = prefs.DigitsBool;
                    f.Justification = 'C';
                    break;
                case "DECIMAL":
                    int td = f.Size > 0 ? f.Size : 10;
                    f.Picture      = BuildNumPic(td, f.Decimals, prefs.DecMinus, prefs.DecCommas, prefs.DecBlankB);
                    f.CharsWide    = NumPicWidth(td, f.Decimals, prefs.DecMinus, prefs.DecCommas);
                    f.Justification = 'R';
                    break;
                case "REAL":
                case "FLOAT":
                case "DOUBLE":
                case "BFLOAT4":
                case "BFLOAT8":
                    f.Picture      = "n12.2";
                    f.CharsWide    = 14;
                    f.Justification = 'R';
                    break;
                default:
                    int ds = f.Size > 0 ? f.Size : 10;
                    f.Picture      = "s" + ds;
                    f.CharsWide    = Math.Min(ds, prefs.WidthMax);
                    f.Justification = 'L';
                    break;
            }

            f.CharsWide = Math.Max(prefs.WidthMin, Math.Min(f.CharsWide, prefs.WidthMax));
        }

        private static void ApplyDate(QueueField f, Queue2Prefs p)
        {
            f.Picture      = p.DatePic;
            f.CharsWide    = Math.Max(p.WidthMin, DatePicWidth(p.DatePic));
            f.Justification = 'R';
        }

        private static void ApplyTime(QueueField f, Queue2Prefs p)
        {
            f.Picture      = p.TimePic;
            f.CharsWide    = Math.Max(p.WidthMin, TimePicWidth(p.TimePic));
            f.Justification = 'C';
        }

        internal static int DatePicWidth(string pic)
        {
            var m = Regex.Match(pic ?? "", @"d(\d+)", RegexOptions.IgnoreCase);
            if (!m.Success) return 10;
            switch (int.Parse(m.Groups[1].Value))
            {
                case 1:  return 8;
                case 2:  return 10;
                case 3:  return 9;
                case 4:  return 11;
                case 8:  return 10;
                case 9:  return 10;
                case 10: return 10;
                default: return 10;
            }
        }

        internal static int TimePicWidth(string pic)
        {
            var m = Regex.Match(pic ?? "", @"t(\d+)", RegexOptions.IgnoreCase);
            if (!m.Success) return 8;
            switch (int.Parse(m.Groups[1].Value))
            {
                case 1: return 11;
                case 2: return 8;
                case 3: return 8;
                case 4: return 5;
                case 5: return 5;
                case 6: return 8;
                case 7: return 8;
                default: return 8;
            }
        }

        internal static string BuildNumPic(int digits, int decimals, bool minus, bool commas, string blank)
        {
            int intDig = Math.Max(digits - decimals, 1);
            var sb = new StringBuilder("n");
            if (minus)  sb.Append('-');
            if (!commas) sb.Append('_');
            int w = intDig;
            if (commas && intDig > 3) w += (intDig - 1) / 3;
            if (minus)   w += 1;
            if (decimals > 0) w += 1 + decimals;
            sb.Append(w);
            if (decimals > 0) sb.Append('.').Append(decimals);
            if (blank.Length > 0) sb.Append(blank);
            return sb.ToString();
        }

        internal static int NumPicWidth(int digits, int decimals, bool minus, bool commas)
        {
            int intDig = Math.Max(digits - decimals, 1);
            int w = intDig;
            if (commas && intDig > 3) w += (intDig - 1) / 3;
            if (minus)   w += 1;
            if (decimals > 0) w += 1 + decimals;
            return w;
        }

        // ── Generate FORMAT() string ──────────────────────────────────────────────
        // Each column spec: {width}{just}({dataIndent})|{mods}~{label}~{hdrJust}({hdrIndent})@{pic}@
        // Groups: [{inner_specs}]({totalWidth})|{mods}~{header}~
        // Columns are directly concatenated (no separator between them).
        internal static string GenerateFormat(List<QueueField> fields, Queue2Prefs prefs)
        {
            string mods = BuildMods(prefs);
            var specs = BuildColumnSpecs(fields, prefs, mods);

            if (specs.Count == 0) return "FORMAT('')";

            if (prefs.OnePerLine)
            {
                var sb = new StringBuilder();
                sb.Append("FORMAT('");
                for (int i = 0; i < specs.Count; i++)
                {
                    bool isLast = (i == specs.Count - 1);
                    if (i > 0) sb.Append("  '");
                    sb.Append(specs[i]);
                    if (!isLast)
                        sb.Append("' &|\r\n");
                    else
                        sb.Append("')");
                }
                return sb.ToString();
            }
            else
            {
                return "FORMAT('" + string.Concat(specs) + "')";
            }
        }

        // Returns a flat list of column-spec strings at the top level.
        // Groups are collapsed into a single "[...](...)|~header~" spec.
        private static List<string> BuildColumnSpecs(List<QueueField> fields, Queue2Prefs prefs, string mods)
        {
            var stack   = new Stack<List<string>>();
            var current = new List<string>();

            foreach (var f in fields)
            {
                if (f.IsGroupOpen)
                {
                    stack.Push(current);
                    current = new List<string>();
                    continue;
                }

                if (f.IsGroupClose)
                {
                    var inner  = current;
                    current    = stack.Count > 0 ? stack.Pop() : new List<string>();

                    // Sum column widths inside the group
                    int groupWidth = 0;
                    foreach (var spec in inner)
                    {
                        var wm = Regex.Match(spec, @"^(\d+)");
                        if (wm.Success) groupWidth += int.Parse(wm.Groups[1].Value);
                    }
                    string hdr = f.GroupHeader.Length > 0 ? f.GroupHeader : "";
                    // Group spec: [innerCols](width)|mods~header~
                    string groupSpec = "[" + string.Concat(inner) + "](" + groupWidth + ")|" +
                                       mods + "~" + hdr + "~";
                    current.Add(groupSpec);
                    continue;
                }

                if (f.OmitHow == "Omit") continue;

                if (f.OmitHow == "Hide")
                {
                    current.Add("0L(0)|");
                    continue;
                }

                if (f.Picture.Length == 0) continue;

                char hdrJust = (f.Justification == 'R' && prefs.HdrCenterDataRight) ? 'C' : prefs.HdrJust;
                string colSpec = f.CharsWide + f.Justification.ToString() + "(" + prefs.DataIndent + ")|" +
                                 mods + "~" + f.Label + "~" +
                                 hdrJust + "(" + prefs.HdrIndent + ")" +
                                 "@" + f.Picture + "@";
                current.Add(colSpec);
            }

            // If there are unclosed groups, flatten them
            while (stack.Count > 0)
            {
                var orphan = current;
                current    = stack.Pop();
                current.AddRange(orphan);
            }

            return current;
        }

        private static string BuildMods(Queue2Prefs prefs)
        {
            var sb = new StringBuilder();
            if (prefs.Resize)    sb.Append('M');
            if (prefs.Fixed)     sb.Append('F');
            if (prefs.Underline) sb.Append('_');
            if (prefs.Colored)   sb.Append('*');
            if (prefs.CellStyle) sb.Append('Y');
            return sb.ToString();
        }

        // ── Generate #FIELDS() list ───────────────────────────────────────────────
        internal static string GenerateFields(List<QueueField> fields, Queue2Prefs prefs)
        {
            var labels = new List<string>();
            foreach (var f in fields)
            {
                if (f.IsGroupOpen || f.IsGroupClose) continue;
                if (f.OmitHow == "Omit") continue;
                if (f.PreLabel.Length > 0) labels.Add(f.PreLabel);
            }
            if (labels.Count == 0) return "#FIELDS()";

            if (prefs.OnePerLine)
            {
                var sb = new StringBuilder();
                sb.Append("#FIELDS(");
                for (int i = 0; i < labels.Count; i++)
                {
                    if (i > 0) sb.Append(",\r\n        ");
                    sb.Append(labels[i]);
                }
                sb.Append(")");
                return sb.ToString();
            }
            else
            {
                return "#FIELDS(" + string.Join(", ", labels.ToArray()) + ")";
            }
        }
    }
}
