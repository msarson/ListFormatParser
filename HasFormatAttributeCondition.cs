using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace ListFormatParser
{
    /// <summary>
    /// Shows the Parse List Format menu item only when the caret is inside a block
    /// that contains a FORMAT() attribute.
    /// </summary>
    public class HasFormatAttributeCondition : IConditionEvaluator
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

            // Walk back to the start of the continuation block the caret is in.
            // A line is part of the block if the line ABOVE it ends with |.
            int start = line;
            while (start > 0 && ClarionCodeParser.HasContinuation(lines[start - 1]))
                start--;

            // Walk forward through the whole block (including the final line which
            // has no | but is still logically part of the control definition).
            for (int i = start; i < lines.Length; i++)
            {
                if (ClarionCodeParser.MakeCodeOnlyLine(lines[i]).Contains("FORMAT"))
                    return true;
                // Stop once we reach a line that has no continuation (end of block).
                // We check AFTER testing for FORMAT so the final line is included.
                if (!ClarionCodeParser.HasContinuation(lines[i]))
                    break;
            }
            return false;
        }
    }
}
