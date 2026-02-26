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
            var provider = WorkbenchSingleton.Workbench.ActiveContent as ITextEditorControlProvider;
            if (provider?.TextEditorControl?.ActiveTextAreaControl == null) return false;

            var doc  = provider.TextEditorControl.ActiveTextAreaControl.Document;
            int line = provider.TextEditorControl.ActiveTextAreaControl.Caret.Line;

            string[] lines = doc.TextContent.Split(new[] { "\r\n", "\r", "\n" },
                                System.StringSplitOptions.None);

            // Walk back to the start of the continuation group
            int start = line;
            while (start > 0 && ClarionCodeParser.HasContinuation(lines[start - 1]))
                start--;

            // Check each line in the block for FORMAT(
            for (int i = start; i < lines.Length; i++)
            {
                if (ClarionCodeParser.MakeCodeOnlyLine(lines[i]).Contains("FORMAT"))
                    return true;
                if (!ClarionCodeParser.HasContinuation(lines[i]))
                    break;
            }
            return false;
        }
    }
}
