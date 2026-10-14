using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using QsysMods.Core;

namespace Designer.Modules.CommandPalette;

internal sealed class PaletteWindow : Window
{
    private readonly PaletteItem[] items;
    private readonly PaletteHistory history;
    internal TextBox Search { get; } = new() { FontSize = 17, Padding = new Thickness(9), Margin = new Thickness(0, 0, 0, 10) };
    internal ListBox Results { get; } = new() { BorderThickness = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock detail = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), MaxHeight = 68 };
    private readonly TextBlock count = new() { Opacity = .7, Margin = new Thickness(0, 8, 0, 0) };
    private readonly TextBlock empty = new() { Text = "No matching commands", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    internal PaletteItem? Chosen { get; private set; }
    internal bool RestoreOnClose { get; private set; }

    internal PaletteWindow(Window owner, PaletteItem[] items, PaletteHistory history)
    {
        this.items = items; this.history = history; Owner = owner;
        Title = "Command Palette"; ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Width = Math.Min(740, Math.Max(360, owner.ActualWidth - 60)); Height = Math.Min(520, Math.Max(300, owner.ActualHeight - 80));
        Host.Theme(this);
        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = grid;
        AutomationProperties.SetName(Search, "Search commands");
        Search.ToolTip = "Search commands by name, category, source, or shortcut";
        AutomationProperties.SetName(Results, "Commands");
        var searchArea = new Grid(); grid.Children.Add(searchArea); searchArea.Children.Add(Search);
        var prompt = new TextBlock { Text = ">  Search commands…", Margin = new Thickness(12, 10, 12, 10),
            FontSize = 17, Opacity = .55, IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Top };
        prompt.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Foreground)) { Source = Search });
        searchArea.Children.Add(prompt);
        var resultsArea = new Grid(); Grid.SetRow(resultsArea, 1); grid.Children.Add(resultsArea);
        resultsArea.Children.Add(Results); resultsArea.Children.Add(empty);
        Grid.SetRow(detail, 2); grid.Children.Add(detail); Grid.SetRow(count, 3); grid.Children.Add(count);
        Results.ItemTemplate = CreateItemTemplate();
        ScrollViewer.SetHorizontalScrollBarVisibility(Results, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(Results, ScrollBarVisibility.Auto);
        VirtualizingPanel.SetIsVirtualizing(Results, true);
        Results.SelectionChanged += (_, _) => detail.Text = (Results.SelectedItem as PaletteItem)?.Explanation ?? "Try another name or category.";
        Search.TextChanged += (_, _) => { prompt.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; Refresh(); };
        PreviewKeyDown += OnKey;
        Results.MouseDoubleClick += (_, e) =>
        {
            if (ItemsControl.ContainerFromElement(Results, e.OriginalSource as DependencyObject) is ListBoxItem row && row.Content is PaletteItem item)
            { Results.SelectedItem = item; Choose(); e.Handled = true; }
        };
        Loaded += (_, _) =>
        {
            // Place near the top of the owner while staying inside its screen work area.
            Top = Math.Max(SystemParameters.VirtualScreenTop, owner.Top + Math.Min(70, owner.ActualHeight / 6));
            Search.Focus();
        };
        Deactivated += (_, _) => { if (IsVisible) Close(); };
        Refresh();
    }
    private void Refresh()
    {
        var found = PaletteSearch.Find(items, Search.Text, history);
        Results.ItemsSource = found; Results.SelectedIndex = found.Length > 0 ? 0 : -1;
        empty.Visibility = found.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        count.Text = $"{found.Length} commands   ·   ↑ ↓ select   Enter run   Esc close";
    }
    private void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { e.Handled = true; RestoreOnClose = true; Close(); return; }
        if (e.Key == Key.Enter) { e.Handled = true; if (!e.IsRepeat) Choose(); return; }
        int delta = e.Key switch { Key.Down => 1, Key.Up => -1, Key.PageDown => 7, Key.PageUp => -7, _ => 0 };
        if (delta == 0) return;
        e.Handled = true;
        if (Results.Items.Count == 0) return;
        Results.SelectedIndex = Math.Clamp(Results.SelectedIndex + delta, 0, Results.Items.Count - 1);
        Results.ScrollIntoView(Results.SelectedItem);
    }
    private void Choose()
    {
        if (Chosen is not null || Results.SelectedItem is not PaletteItem item) return;
        if (!item.Enabled) { detail.Text = item.Explanation; return; }
        Chosen = item; RestoreOnClose = true; Close();
    }
    private static DataTemplate CreateItemTemplate()
    {
        var root = new FrameworkElementFactory(typeof(DockPanel));
        root.SetValue(FrameworkElement.MarginProperty, new Thickness(7, 5, 7, 5));
        root.SetBinding(UIElement.OpacityProperty, new Binding(nameof(PaletteItem.RowOpacity)));
        var keys = new FrameworkElementFactory(typeof(TextBlock));
        keys.SetValue(DockPanel.DockProperty, Dock.Right); keys.SetValue(FrameworkElement.MarginProperty, new Thickness(14, 2, 0, 0));
        keys.SetValue(FrameworkElement.MaxWidthProperty, 200d); keys.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        keys.SetBinding(TextBlock.TextProperty, new Binding(nameof(PaletteItem.Shortcut))); root.AppendChild(keys);
        var text = new FrameworkElementFactory(typeof(StackPanel));
        var title = new FrameworkElementFactory(typeof(TextBlock));
        title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        title.SetValue(TextBlock.FontSizeProperty, 14d); title.SetBinding(TextBlock.TextProperty, new Binding(nameof(PaletteItem.Label))); text.AppendChild(title);
        var source = new FrameworkElementFactory(typeof(TextBlock)); source.SetValue(UIElement.OpacityProperty, .7);
        source.SetValue(TextBlock.FontSizeProperty, 11d); source.SetBinding(TextBlock.TextProperty, new Binding(nameof(PaletteItem.Subtitle))); text.AppendChild(source);
        root.AppendChild(text); return new DataTemplate(typeof(PaletteItem)) { VisualTree = root };
    }
}
