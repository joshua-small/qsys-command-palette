using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using QsysMods.Core;

namespace Designer.Modules.CommandPalette;

public sealed class PaletteModule : ModModule
{
    internal const string Id = "command-palette";
    internal const string OpenId = Id + "/open";
    private PaletteController? controller;
    public PaletteModule() => Mods.Register(Id, main =>
    {
        controller = new PaletteController(main);
        Commands.Register(new(OpenId, "Command Palette…", "Command Palette", controller.Open, controller.CanOpen,
            new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift), InMenu: true, AllowInText: true)
        { When = "Designer main window; executes in the editor focused before opening" });
        Host.Receipt(Id, "ready", new { version = typeof(PaletteModule).Assembly.GetName().Version?.ToString(), shortcut = "Ctrl+Shift+P" });
    });
}

internal sealed class PaletteController
{
    private readonly Window main;
    private readonly PaletteHistory history = new();
    private IInputElement? previousFocus;
    private PaletteWindow? palette;
    private bool executing;

    internal PaletteController(Window main)
    {
        this.main = main;
        main.PreviewGotKeyboardFocus += FocusChanged;
        Commands.Changed += RegistryChanged; ModControls.Changed += RegistryChanged;
        main.Closed += Closed;
    }
    private void FocusChanged(object sender, KeyboardFocusChangedEventArgs e)
    { if (PaletteContext.IsEditorFocus(e.NewFocus, main)) previousFocus = e.NewFocus; }
    internal bool CanOpen() => Host.Enabled && main.IsActive && main.IsEnabled && !executing;
    private void RegistryChanged() => palette?.Close();
    private void Closed(object? sender, EventArgs e)
    {
        palette?.Close(); main.PreviewGotKeyboardFocus -= FocusChanged;
        Commands.Changed -= RegistryChanged; ModControls.Changed -= RegistryChanged; main.Closed -= Closed;
    }
    internal void Open()
    {
        if (palette is not null) { palette.Activate(); return; }
        if (!CanOpen()) return;
        try
        {
            Commands.DiscoverNative(main);
            var focus = PaletteContext.IsEditorFocus(Keyboard.FocusedElement, main) ? Keyboard.FocusedElement : previousFocus;
            focus ??= FocusManager.GetFocusedElement(main);
            if (focus is null || !PaletteContext.IsEditorFocus(focus, main)) focus = main;
            // Menu invocation must inspect the same editor as keyboard invocation.
            if (!ReferenceEquals(Keyboard.Focus(focus), focus)) return;
            var context = PaletteContext.Capture(main, focus);
            if (!context.IsValid()) return;
            var settings = ModControls.Current;
            var entries = Commands.All.Where(c => c.Definition.Id != PaletteModule.OpenId).ToDictionary(c => c.Definition.Id);
            var items = entries.Values.Select(entry =>
            {
                var def = entry.Definition;
                var description = ModControls.Describe(entry, settings);
                bool enabled = Available(entry);
                string reason = !Commands.IsAvailable(entry) ? "The command's mod is not ready."
                    : "Unavailable in the original editor. " + description.When;
                return new PaletteItem(def.Id, def.Name, def.Group, description.Source,
                    ModControls.Bindings.Display(def.Id, settings.Bindings.Overrides),
                    def.Native?.Name ?? def.Id, description.When, description.Details, enabled, enabled ? "" : reason);
            }).ToArray();
            var window = new PaletteWindow(main, items, history);
            palette = window;
            window.Closed += (_, _) =>
            {
                palette = null;
                if (window.Chosen is { } selected && entries.TryGetValue(selected.Id, out var command))
                {
                    executing = true;
                    main.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        try
                        {
                            var invocation = new PaletteInvocation(() => Host.Enabled && context.IsValid(), context.RestoreFocus);
                            bool success = invocation.TryExecute(() => Commands.All.Any(c => ReferenceEquals(c, command)) && Available(command),
                                () => command.Definition.Execute(), out var reason);
                            Host.Receipt(PaletteModule.Id, success ? "executed" : "canceled", new { command = selected.Id, reason,
                                active = main.IsActive, focus = Keyboard.FocusedElement?.GetType().FullName, handoff = context.FocusDiagnostic });
                            if (success) history.Record(selected.Id);
                            else MessageBox.Show(main, reason, "Command Palette", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex) { Host.Error(PaletteModule.Id, ex, true); }
                        finally { executing = false; }
                    }));
                }
                else if (window.RestoreOnClose) main.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => context.RestoreFocus()));
            };
            window.Show();
        }
        catch (Exception ex) { palette?.Close(); Host.Error(PaletteModule.Id, ex, true); }
    }
    private static bool Available(RegisteredCommand entry)
    { try { return Commands.IsAvailable(entry) && entry.Definition.CanExecute(); } catch { return false; } }
}
