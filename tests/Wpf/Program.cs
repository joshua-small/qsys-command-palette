using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Designer.Modules.CommandPalette;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static Task Idle() => Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    private static void Press(Window window, Key key) => window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
        PresentationSource.FromVisual(window), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
    [STAThread]
    private static int Main(string[] args)
    {
        int result = 1;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var text = new TextBox { Text = "Original selected text", Margin = new Thickness(40), FontSize = 18 };
        var main = new Window { Title = "Command Palette WPF test", Width = 920, Height = 700, Content = text };
        app.Startup += async (_, _) =>
        {
            try
            {
                main.Show(); main.Activate(); text.Focus(); text.Select(0, 8); await Idle();
                object document = new(), selected = new();
                var context = new PaletteContext(main, text, () => new[] { document, selected });
                Check(context.IsValid(), "Captured live WPF target");
                var items = new[] {
                    new PaletteItem("copy", "Copy", "Q-SYS", "Q-SYS", "Ctrl+C", "", "Selected text in the original editor", "", true, ""),
                    new PaletteItem("disabled", "Delete", "Q-SYS", "Q-SYS", "Delete", "", "Selection required", "", false, "No eligible selection"),
                    new PaletteItem("pack", "Pack Left", "Q-SYS", "Better Packing / Q-SYS", "Ctrl+Shift+L", "", "Schematic selection", "Grouped undo", true, "") };
                var history = new PaletteHistory();
                var palette = new PaletteWindow(main, items, history); palette.Show(); await Idle();
                Check(ReferenceEquals(Keyboard.FocusedElement, palette.Search), "Search receives focus");
                Check(palette.Results.Items.Count == 3, "Registry snapshot shown");
                palette.Search.Text = "delete"; await Idle(); Press(palette, Key.Enter);
                Check(palette.IsVisible && palette.Chosen is null, "Unavailable command cannot execute");
                palette.Search.Text = "no-such-command"; await Idle(); Press(palette, Key.Down); Press(palette, Key.Enter);
                Check(palette.Results.Items.Count == 0 && palette.IsVisible, "Empty results safe");
                palette.Search.Text = ""; await Idle(); Press(palette, Key.Down);
                Check(palette.Results.SelectedIndex == 1 && ReferenceEquals(Keyboard.FocusedElement, palette.Search), "Arrow navigation retains typing focus");
                Press(palette, Key.PageDown); Check(palette.Results.SelectedIndex == 2, "Page navigation clamps");
                Press(palette, Key.PageUp); Check(palette.Results.SelectedIndex == 0, "Page up clamps");
                if (args.Length > 0)
                {
                    Directory.CreateDirectory(args[0]);
                    palette.UpdateLayout();
                    var bitmap = new RenderTargetBitmap((int)palette.ActualWidth, (int)palette.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(palette); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(args[0], "palette-wpf.png")); encoder.Save(stream);
                }
                palette.Search.Text = "copy"; await Idle(); Press(palette, Key.Enter); await Idle();
                Check(palette.Chosen?.Id == "copy" && !palette.IsVisible, "Enter chooses once and closes");
                main.Activate(); await Idle();
                int copies = 0;
                text.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, e) => { copies++; e.Handled = true; }, (_, e) => { e.CanExecute = text.SelectionLength > 0; e.Handled = true; }));
                var invocation = new PaletteInvocation(context.IsValid, context.RestoreFocus);
                Check(invocation.TryExecute(() => ApplicationCommands.Copy.CanExecute(null, text), () => ApplicationCommands.Copy.Execute(null, text), out _), "Execute after exact focus restoration");
                Check(copies == 1 && ReferenceEquals(Keyboard.FocusedElement, text) && text.SelectedText == "Original", "Original text target and selection preserved");
                selected = new(); Check(!context.IsValid(), "Changed selection invalidates snapshot");
                context = new PaletteContext(main, text, () => new[] { document, selected });
                document = new(); Check(!context.IsValid(), "Changed document invalidates snapshot");
                context = new PaletteContext(main, text, () => new[] { document, selected });
                text.IsEnabled = false; Check(!context.IsValid(), "Disabled target invalidates snapshot"); text.IsEnabled = true;
                palette = new PaletteWindow(main, items, history); palette.Show(); await Idle(); Press(palette, Key.Escape); await Idle();
                Check(!palette.IsVisible && palette.Chosen is null && palette.RestoreOnClose, "Escape cancels");
                main.Activate(); text.Focus(); await Idle(); palette = new PaletteWindow(main, items, history); palette.Show(); await Idle();
                main.Activate(); await Idle(); Check(!palette.IsVisible && palette.Chosen is null && !palette.RestoreOnClose, "Deactivation cancels without stealing focus");
                Console.WriteLine($"PASS: {checks} WPF interaction and context checks (synthetic host)."); result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { foreach (Window window in app.Windows.Cast<Window>().ToArray()) window.Close(); app.Shutdown(); }
        };
        app.Run(); return result;
    }
}
