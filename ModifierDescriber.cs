using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ListFormatParser
{
    /// <summary>
    /// Expands a raw FORMAT() modifier string (e.g. "FY*Z(3)Q'tip'") into a
    /// human-readable description (e.g. "Fixed column; Per-cell style; Per-cell colours; Default style 3; Default tip: tip").
    /// Based on the Clarion 11.1 help file reference in FORMAT-modifiers.md.
    /// </summary>
    public static class ModifierDescriber
    {
        public static string Describe(string modifiers)
        {
            if (string.IsNullOrEmpty(modifiers)) return string.Empty;

            var parts = new List<string>();
            int i = 0, n = modifiers.Length;

            while (i < n)
            {
                char c = modifiers[i];

                // --- Parameterised modifiers ---
                if ((c == 'Z' || c == 'S' || c == 'B' || c == 'E' || c == 'T') &&
                    i + 1 < n && modifiers[i + 1] == '(')
                {
                    string arg = ReadParen(modifiers, ref i); // advances past closing )
                    switch (c)
                    {
                        case 'Z': parts.Add($"Default style {arg}"); break;
                        case 'S': parts.Add($"Scroll bar ({arg} du)"); break;
                        case 'B': parts.Add($"Selection bar frame colour ({arg})"); break;
                        case 'E': parts.Add($"Default column colours ({arg})"); break;
                        case 'T': parts.Add($"Tree control{(arg.Length > 0 ? " (" + DescribeTreeOptions(arg) + ")" : "")}"); break;
                    }
                    continue;
                }

                // T with no argument
                if (c == 'T') { parts.Add("Tree control"); i++; continue; }

                // --- Q'tooltip' ---
                if (c == 'Q' && i + 1 < n && modifiers[i + 1] == '\'')
                {
                    i += 2; // skip Q and opening '
                    var sb = new StringBuilder();
                    while (i < n)
                    {
                        char q = modifiers[i];
                        if (q == '\'' && i + 1 < n && modifiers[i + 1] == '\'') { sb.Append('\''); i += 2; }
                        else if (q == '\'') { i++; break; }
                        else { sb.Append(q); i++; }
                    }
                    parts.Add($"Default tip: {sb}");
                    continue;
                }

                // --- Single-character modifiers ---
                switch (char.ToUpper(c))
                {
                    case '*': parts.Add("Per-cell colours (QUEUE fields)"); break;
                    case 'I': parts.Add("Icon from QUEUE"); break;
                    case 'J': parts.Add("Transparent icon from QUEUE"); break;
                    case 'Y': parts.Add("Per-cell style (QUEUE field)"); break;
                    case 'F': parts.Add("Fixed column (stays on scroll)"); break;
                    case 'M': parts.Add("Resizable at runtime"); break;
                    case 'P': parts.Add("Tooltip from QUEUE field"); break;
                    case '_': parts.Add("Underline"); break;
                    case '|': parts.Add("Right border"); break;
                    case '/': parts.Add("Line break (next field on new line)"); break;
                    case '?': parts.Add("Locator field (COMBO)"); break;
                    default:
                        // Unknown — include as-is so nothing is silently dropped
                        parts.Add(c.ToString());
                        break;
                }
                i++;
            }

            return string.Join("; ", parts);
        }

        // -----------------------------------------------------------------------

        /// <summary>Reads the content of a '(...)' argument starting at the '(' char.</summary>
        private static string ReadParen(string s, ref int i)
        {
            i++; // skip letter before (
            i++; // skip opening (
            int depth = 1;
            var sb = new StringBuilder();
            while (i < s.Length && depth > 0)
            {
                char c = s[i++];
                if      (c == '(') { depth++; sb.Append(c); }
                else if (c == ')') { depth--; if (depth > 0) sb.Append(c); }
                else               sb.Append(c);
            }
            return sb.ToString();
        }

        private static string DescribeTreeOptions(string opts)
        {
            // opts is the content inside T(...) e.g. "RL1"
            var desc = new List<string>();
            foreach (char o in opts.ToUpper())
            {
                switch (o)
                {
                    case '1': desc.Add("root=level 1"); break;
                    case 'R': desc.Add("no root lines"); break;
                    case 'L': desc.Add("no inter-level lines"); break;
                    case 'B': desc.Add("no expand boxes"); break;
                    case 'I': desc.Add("no indentation"); break;
                    default:  desc.Add(o.ToString()); break;
                }
            }
            return string.Join(", ", desc);
        }
    }
}
