namespace Designer.Modules.CommandPalette;

// An invocation is single-use, including failure. Focus restoration and all
// predicates run immediately before dispatch, never when a result is highlighted.
internal sealed class PaletteInvocation(Func<bool> validContext, Func<bool> restoreFocus)
{
    private bool consumed;
    internal bool TryExecute(Func<bool> available, Action execute, out string reason)
    {
        if (consumed) { reason = "This palette session has already ended."; return false; }
        consumed = true;
        if (!validContext()) { reason = "The original editor or selection changed. Open the palette again."; return false; }
        if (!restoreFocus()) { reason = "The original editor could not regain focus. Open the palette again."; return false; }
        if (!validContext() || !available()) { reason = "This command is no longer available in the original editor."; return false; }
        execute(); reason = ""; return true;
    }
}
