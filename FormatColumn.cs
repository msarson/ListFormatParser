namespace ListFormatParser
{
    /// <summary>
    /// Represents one parsed column from a Clarion LIST FORMAT() string.
    /// Mirrors the structure of Carl Barnes' FormatQ / ColumnzQ Clarion queues.
    /// </summary>
    public class FormatColumn
    {
        public int    ColumnNumber { get; set; }
        public string Width       { get; set; }  // e.g. "51"
        public string Alignment   { get; set; }  // L, R, C, or D
        public string Indent      { get; set; }  // content of (n) — border/indent
        public string Modifiers   { get; set; }  // F, M, *, I, Y, B, H, Z(n), Q'tip', etc.
        public string Header      { get; set; }  // between ~...~
        public string Picture     { get; set; }  // between @...@
        public bool   IsGroupStart { get; set; } // opened with [
        public bool   IsGroupEnd   { get; set; } // opened with ]
        public string RawSpec     { get; set; }  // original substring from format string

        public string ColLabel =>
            IsGroupStart ? "Grp[" : IsGroupEnd ? "]Grp" : ColumnNumber.ToString();
    }
}
