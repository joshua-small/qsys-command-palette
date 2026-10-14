using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using QsysMods.Core;

namespace Designer.Modules.CommandPalette;

internal sealed class PaletteContext
{
    private readonly Window main;
    private readonly IInputElement focus;
    private readonly Func<object?[]> capture;
    private readonly object?[] before;
    internal string FocusDiagnostic { get; private set; } = "";

    internal PaletteContext(Window main, IInputElement focus, Func<object?[]> capture)
    { this.main = main; this.focus = focus; this.capture = capture; before = capture(); }

    internal static PaletteContext Capture(Window main, IInputElement focus) => new(main, focus, () =>
    {
        // These public members are retained, inspected Designer 10.5 internals.
        // Reading them does not alter the active document or selection.
        var manager = Host.Type("QSC.QSys.App").GetField("Manager", Host.Static)!.GetValue(null)!;
        var documents = Read(main, "DocumentHost")!;
        var pages = ((IEnumerable)Read(documents, "CurrentPages")!).Cast<object>();
        var selection = ((IEnumerable)Read(HostSelection.GlobalSelection, "AsObjects")!).Cast<object>();
        // Prefix lengths prevent ambiguous concatenations when a collection changes.
        var pageArray = pages.ToArray(); var selected = selection.ToArray();
        return new object?[] { Read(manager, "Document"), Read(documents, "CurrentOpenCanvas"), pageArray.Length, selected.Length }
            .Concat(pageArray).Concat(selected).ToArray();
    });

    private static object? Read(object target, string name) =>
        (target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMemberException(target.GetType().FullName, name)).GetValue(target);

    internal bool IsValid()
    {
        if (!HostWindowValid() || focus is not DependencyObject obj || Window.GetWindow(obj) != main) return false;
        if (focus is UIElement ui && (!ui.IsVisible || !ui.IsEnabled || !ui.Focusable)) return false;
        if (focus is ContentElement content && (!content.IsEnabled || !content.Focusable)) return false;
        try
        {
            var now = capture();
            return now.Length == before.Length && now.Select((value, index) =>
                value is int count ? Equals(count, before[index]) : ReferenceEquals(value, before[index])).All(equal => equal);
        }
        catch { return false; }
    }

    private bool HostWindowValid() => main.IsLoaded && main.IsVisible && main.IsEnabled;
    internal bool RestoreFocus()
    {
        // Closing an owned modeless window does not synchronously reactivate
        // Designer. This is used only after explicit Enter/Escape, never blur.
        if (!IsValid()) { FocusDiagnostic = "Context invalid at focus restore"; return false; }
        if (!main.IsActive) main.Activate();
        if (!main.IsActive) { FocusDiagnostic = "Owner activation pending"; return false; }
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope((DependencyObject)focus), focus);
        var returned = Keyboard.Focus(focus);
        bool restored = ReferenceEquals(Keyboard.FocusedElement, focus);
        FocusDiagnostic = $"Target={focus.GetType().FullName}; returned={returned?.GetType().FullName}; current={Keyboard.FocusedElement?.GetType().FullName}; exact={restored}";
        return restored;
    }

    internal static bool IsEditorFocus(IInputElement? element, Window main)
    {
        if (element is not DependencyObject obj || Window.GetWindow(obj) != main) return false;
        for (DependencyObject? parent = obj; parent is not null; parent = Parent(parent))
            if (parent is MenuBase or MenuItem or ToolBar) return false;
        return true;
    }
    private static DependencyObject? Parent(DependencyObject value) => value is Visual or System.Windows.Media.Media3D.Visual3D
        ? VisualTreeHelper.GetParent(value) ?? LogicalTreeHelper.GetParent(value) : LogicalTreeHelper.GetParent(value);
}
