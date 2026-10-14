using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Designer.Modules.Common;
using Designer.Modules.CommandPalette;
using QsysMods.Core;

namespace Designer.Modules.CommandPaletteProbe;

// A lab-only module, never part of the package. Requires an exact application
// directory allowlist in addition to Core's normal real-binary hash guards.
public sealed class Probe : IDesignerModule
{
    private sealed record Config(string ApplicationDirectory, string OutputDirectory);
    private Config? config;
    private DispatcherTimer? timer;
    private int attempts;
    private readonly List<string> checks = new();
    public string Description => "Command Palette isolated acceptance probe";
    public string MenuTitle => "Palette lab probe";
    public string Parent => "mainMenu_Tools";
    public string NestedMenu => "";
    public Key ShortcutKey => Key.None;
    public ModifierKeys ModifierKey => ModifierKeys.None;
    public string ShortcutInputGestureText => "";
    public Action Execute => () => { };
    public Probe()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ".palette-lab.json");
        if (!Host.IsLab || !Host.Enabled || !File.Exists(path)) return;
        config = JsonSerializer.Deserialize<Config>(File.ReadAllText(path))!;
        if (!Path.GetFullPath(config.ApplicationDirectory).TrimEnd('\\').Equals(AppContext.BaseDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
            || File.Exists(Path.Combine(config.OutputDirectory, "result.json"))) return;
        timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += async (_, _) => await Run(); timer.Start();
    }
    private void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks.Add(name); }
    private static Task Idle() => Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static PaletteWindow Palette() => Application.Current.Windows.OfType<PaletteWindow>().Single();
    private static void Press(PaletteWindow window, Key key) => window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
        PresentationSource.FromVisual(window), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
    private void Render(Window window, string name)
    {
        window.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(config!.OutputDirectory, name + ".png")); png.Save(file);
    }
    private async Task Run()
    {
        if (++attempts < 16 || Application.Current.MainWindow is not { IsLoaded: true, IsEnabled: true } main) return;
        if (!Mods.IsReady("command-palette") && attempts < 100) return;
        timer!.Stop(); Directory.CreateDirectory(config!.OutputDirectory);
        bool success = false; string? failure = null;
        try
        {
            Check(main.Title.StartsWith("untitled", StringComparison.OrdinalIgnoreCase), "Blank isolated design");
            Check(Host.Enabled && Mods.IsReady("command-palette"), "Real host hashes and module initialization");
            Check(!Mods.IsReady("better-shortcuts"), "Palette operates without Better Shortcuts");
            main.Activate(); main.Focus(); await Idle();
            var open = Commands.All.Single(c => c.Definition.Id == "command-palette/open");
            Check(ModControls.Bindings.Display(open.Definition.Id, ModControls.Current.Bindings.Overrides) == "Ctrl+Shift+P", "Reserved default from Core binding engine");
            Check(main.InputBindings.OfType<KeyBinding>().Count(b => b.Key == Key.P && b.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && ReferenceEquals(b.Command, open.Command)) == 1, "One main-window input binding");
            Check(open.Command.CanExecute(null, main), "Open command available");
            open.Command.Execute(null, main); await Idle();
            var window = Palette();
            Check(window.Results.Items.Count > 50, "Native registry populated");
            Check(ReferenceEquals(Keyboard.FocusedElement, window.Search), "Native palette search focus");
            Check(!window.Results.Items.Cast<PaletteItem>().Any(item => item.Id == "command-palette/open"), "Palette excludes recursive self action");
            Check(window.Results.Items.Cast<PaletteItem>().Any(item => !item.Enabled), "Unavailable native commands remain discoverable");
            Render(window, "palette-before");
            window.Search.Text = "not-a-command-xyz"; await Idle();
            Check(window.Results.Items.Count == 0, "Native empty search"); Press(window, Key.Escape); await Idle();
            Check(!Application.Current.Windows.OfType<PaletteWindow>().Any(), "Native Escape cancellation");
            main.Activate(); main.Focus(); await Idle();
            int executions = 0; bool originalFocus = false; string? actualFocusType = null;
            var expectedFocus = Keyboard.FocusedElement;
            Commands.Register(new("palette-test/focus", "Palette Focus Test", "Palette Test", () => { executions++; originalFocus = ReferenceEquals(Keyboard.FocusedElement, expectedFocus); actualFocusType = Keyboard.FocusedElement?.GetType().FullName; }, () => main.IsActive));
            open.Command.Execute(null, main); await Idle(); window = Palette(); window.Search.Text = "Palette Focus Test"; await Idle();
            Check(window.Results.Items.Cast<PaletteItem>().Single().Enabled, "Availability captured before opening");
            Press(window, Key.Enter); await Idle();
            Check(executions == 1 && originalFocus, $"Native controller dispatches once after restoring original editor focus (runs={executions}, expected={expectedFocus?.GetType().FullName}, actual={actualFocusType}, chosen={window.Chosen?.Id})");
            main.Activate(); main.Focus(); await Idle(); open.Command.Execute(null, main); await Idle(); window = Palette();
            Check(((PaletteItem)window.Results.Items[0]).Id == "palette-test/focus", "Recent command promoted");
            Press(window, Key.Escape); await Idle();
            var theme = Commands.Native("ColorSchemeToggle"); int themeRuns = 0;
            ExecutedRoutedEventHandler track = (_, e) => { if (ReferenceEquals(e.Command, theme)) themeRuns++; };
            main.AddHandler(CommandManager.ExecutedEvent, track, true);
            main.Activate(); main.Focus(); await Idle(); open.Command.Execute(null, main); await Idle(); window = Palette();
            var themeItem = window.Results.Items.Cast<PaletteItem>().Single(item => item.Id == Commands.NativeId(theme));
            window.Search.Text = themeItem.Name; await Idle(); window.Results.SelectedItem = window.Results.Items.Cast<PaletteItem>().Single(item => item.Id == themeItem.Id);
            Press(window, Key.Enter); await Idle(); Check(themeRuns == 1, "Native routed theme command executes once");
            main.Activate(); main.Focus(); await Idle(); open.Command.Execute(null, main); await Idle(); window = Palette(); Render(window, "palette-after-theme");
            Press(window, Key.Escape); await Idle(); theme.Execute(null, main); main.RemoveHandler(CommandManager.ExecutedEvent, track);
            Check(HostSelection.ReadHostState().GetType().GetProperty("dirty")!.GetValue(HostSelection.ReadHostState()) is false, "Probe leaves blank design clean");
            success = true;
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally
        {
            File.WriteAllText(Path.Combine(config.OutputDirectory, "result.json"), JsonSerializer.Serialize(new
            { success, checks, failure, paletteVersion = typeof(PaletteModule).Assembly.GetName().Version?.ToString(), commands = Commands.All.Count }, new JsonSerializerOptions { WriteIndented = true }));
            // Terminate only this throwaway process, avoiding writes to native user preferences.
            Environment.Exit(success ? 0 : 1);
        }
    }
}
