using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.TextEditor.Document;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ListFormatParser
{
    /// <summary>
    /// "Clean List Format" command — finds the FORMAT() attribute in the current
    /// continuation block, parses it, regenerates it in normalised form (one column
    /// per line) and replaces only the FORMAT('...') span in the document.
    ///
    /// Everything outside FORMAT(...) is left completely untouched.
    /// The replacement is wrapped in a single undo action.
    /// </summary>
    public class CleanListFormatCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
            if (window == null) return;
            var provider = window.ActiveViewContent as ITextEditorControlProvider;
            if (provider == null) return;

            var tec  = provider.TextEditorControl;
            var doc  = tec.Document;
            var area = tec.ActiveTextAreaControl;
            int caretLine = area.Caret.Line;

            string[] lines = doc.TextContent.Split(
                new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            // 1. Find the continuation block the caret sits in
            int blockStart = FindGroupStart(lines, caretLine);
            int blockEnd   = FindGroupEnd(lines, blockStart);

            // 2. Build a flat version of the block, tracking each source char's
            //    original (line, col) position.
            FlatMap map;
            string  flat = BuildFlatWithMap(lines, blockStart, blockEnd, out map);

            // 3. Locate FORMAT(...) in the flat string
            string codeOnly = ClarionCodeParser.MakeCodeOnlyLine(flat);
            int begAtt, begParen, endParen;
            if (!ClarionCodeParser.FindAttrParen(codeOnly, "FORMAT",
                    out begAtt, out begParen, out endParen))
            {
                MessageService.ShowMessage("No FORMAT() attribute found in the current block.");
                return;
            }

            // 4. Extract and parse the FORMAT string value
            string parenContent = flat.Substring(begParen, endParen - begParen + 1);
            string formatValue  = ExtractStringValue(parenContent);
            if (string.IsNullOrEmpty(formatValue))
            {
                MessageService.ShowMessage("Could not extract the FORMAT() string value.");
                return;
            }

            var columns = FormatStringParser.Parse(formatValue);
            if (columns.Count == 0)
            {
                MessageService.ShowMessage("No columns found in FORMAT().");
                return;
            }

            // 5. Generate the clean replacement (e.g. FORMAT('...' &|\n       '...'))
            string cleanFormat = FormatStringGenerator.Generate(columns);

            // 6. Map flat positions back to document offsets
            //    begAtt = start of "FORMAT", endParen = closing ')'
            int docOffsetStart = map.FlatToDocOffset(begAtt, doc);
            int docOffsetEnd   = map.FlatToDocOffset(endParen, doc) + 1; // +1 to include ')'

            if (docOffsetStart < 0 || docOffsetEnd < 0 || docOffsetEnd <= docOffsetStart)
            {
                MessageService.ShowMessage("Could not map FORMAT() position back to source.");
                return;
            }

            // 7. Replace in document — one undo action
            doc.UndoStack.StartUndoGroup();
            try
            {
                doc.Replace(docOffsetStart, docOffsetEnd - docOffsetStart, cleanFormat);
            }
            finally
            {
                doc.UndoStack.EndUndoGroup();
            }

            tec.Refresh();
        }

        // -----------------------------------------------------------------------
        // Block navigation
        // -----------------------------------------------------------------------

        private static int FindGroupStart(string[] lines, int caretLine)
        {
            int i = caretLine;
            while (i > 0 && ClarionCodeParser.HasContinuation(lines[i - 1]))
                i--;
            return i;
        }

        private static int FindGroupEnd(string[] lines, int start)
        {
            int i = start;
            while (i < lines.Length - 1 && ClarionCodeParser.HasContinuation(lines[i]))
                i++;
            return i;
        }

        // -----------------------------------------------------------------------
        // Position-tracked flatten
        // -----------------------------------------------------------------------

        /// <summary>
        /// Joins continuation lines into a single flat string.
        /// For each segment of real content, records (flatStart, origLine, origColStart, length)
        /// so any flat position can be mapped back to its document offset.
        ///
        /// Mirrors the Flattener join logic: continuation lines are TrimStart'd.
        /// </summary>
        private static string BuildFlatWithMap(
            string[] lines, int start, int end, out FlatMap map)
        {
            map = new FlatMap();
            var sb = new StringBuilder();

            for (int i = start; i <= end; i++)
            {
                string line = lines[i];
                int    pipe = ClarionCodeParser.FindContinuationPipe(line);
                int    len  = pipe >= 0 ? pipe : line.Length;

                // Continuation lines are joined with leading whitespace stripped
                // (matches how Flattener.cs works)
                int colStart = (i > start) ? (line.Length - line.TrimStart().Length) : 0;
                if (colStart > len) colStart = len;

                // Extract content, trim trailing whitespace before the pipe
                string part = line.Substring(colStart, len - colStart).TrimEnd();
                if (part.Length == 0) continue;

                // Add a synthetic joining space (not tracked — won't land on FORMAT chars)
                if (sb.Length > 0 && sb[sb.Length - 1] != ' ' && part[0] != ' ')
                    sb.Append(' ');

                // Register this segment: flatStart = current sb length
                map.AddSegment(sb.Length, i, colStart, part.Length);
                sb.Append(part);
            }

            return sb.ToString();
        }

        // -----------------------------------------------------------------------
        // String value extraction
        // -----------------------------------------------------------------------

        private static string ExtractStringValue(string parenContent)
        {
            var  sb    = new StringBuilder();
            bool inStr = false;
            for (int i = 0; i < parenContent.Length; i++)
            {
                char c = parenContent[i];
                if (!inStr) { if (c == '\'') inStr = true; }
                else
                {
                    if (c == '\'' && i + 1 < parenContent.Length && parenContent[i + 1] == '\'')
                    { sb.Append('\''); i++; }
                    else if (c == '\'') inStr = false;
                    else sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }

    // -----------------------------------------------------------------------
    // Flat-to-document position map
    // -----------------------------------------------------------------------

    /// <summary>
    /// Tracks segments of the flat string back to their original (line, col) positions.
    /// Each segment = a contiguous run of characters from one source line.
    /// </summary>
    internal class FlatMap
    {
        private struct Segment
        {
            public int FlatStart;
            public int Length;
            public int OrigLine;
            public int OrigColStart; // column in original line where this segment begins
        }

        private readonly List<Segment> _segs = new List<Segment>();

        public void AddSegment(int flatStart, int origLine, int origColStart, int length)
        {
            _segs.Add(new Segment
            {
                FlatStart    = flatStart,
                Length       = length,
                OrigLine     = origLine,
                OrigColStart = origColStart
            });
        }

        /// <summary>Returns the IDocument character offset for flat position <paramref name="flatPos"/>.</summary>
        public int FlatToDocOffset(int flatPos, IDocument doc)
        {
            foreach (var seg in _segs)
            {
                if (flatPos >= seg.FlatStart && flatPos < seg.FlatStart + seg.Length)
                {
                    int col = seg.OrigColStart + (flatPos - seg.FlatStart);
                    return doc.GetLineSegment(seg.OrigLine).Offset + col;
                }
            }
            return -1;
        }
    }
}
