using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace ListFormatParser
{
    /// <summary>
    /// Shows the "Format FROM Selections" menu item only when the caret is inside a
    /// continuation block that contains a FROM() attribute with a string literal.
    /// </summary>
    public class HasFromStringCondition : IConditionEvaluator
    {
        public bool IsValid(object caller, Condition condition)
        {
            if (WorkbenchSingleton.Workbench == null) return false;
            var window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
            if (window == null) return false;
            var provider = window.ActiveViewContent as ITextEditorControlProvider;
            if (provider?.TextEditorControl?.ActiveTextAreaControl == null) return false;

            var doc  = provider.TextEditorControl.ActiveTextAreaControl.Document;
            int line = provider.TextEditorControl.ActiveTextAreaControl.Caret.Line;

            string[] lines = doc.TextContent.Split(new[] { "\r\n", "\r", "\n" },
                                System.StringSplitOptions.None);
            if (line >= lines.Length) return false;

            int start = line;
            while (start > 0 && ClarionCodeParser.HasContinuation(lines[start - 1]))
                start--;

            for (int i = start; i < lines.Length; i++)
            {
                // Check for FROM(' — a FROM with a string literal (case-insensitive)
                if (lines[i].ToUpper().Contains("FROM('"))
                    return true;
                if (!ClarionCodeParser.HasContinuation(lines[i]))
                    break;
            }
            return false;
        }
    }
}
