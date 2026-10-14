namespace Designer.Modules.CommandPalette;

internal sealed record PaletteItem(string Id, string Name, string Category, string Source,
    string Shortcut, string Aliases, string When, string Details, bool Enabled, string DisabledReason)
{
    public string Label => Category == "Q-SYS" ? Name : Category + ": " + Name;
    public string Subtitle => Source + (Enabled ? "" : " · Unavailable");
    public double RowOpacity => Enabled ? 1 : 0.55;
    public string Explanation => (Enabled ? When : DisabledReason) + (string.IsNullOrEmpty(Details) ? "" : "\n" + Details);
}

internal sealed class PaletteHistory
{
    private readonly List<string> ids = new();
    internal int Rank(string id) { int index = ids.IndexOf(id); return index < 0 ? int.MaxValue : index; }
    internal void Record(string id) { ids.Remove(id); ids.Insert(0, id); if (ids.Count > 30) ids.RemoveAt(ids.Count - 1); }
}

internal static class PaletteSearch
{
    internal static PaletteItem[] Find(IEnumerable<PaletteItem> items, string query, PaletteHistory history)
    {
        var words = query.Trim().TrimStart('>').Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return items.Select(item => (Item: item, Score: Score(item, words)))
            .Where(hit => hit.Score >= 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => history.Rank(hit.Item.Id))
            .ThenByDescending(hit => hit.Item.Enabled)
            .ThenBy(hit => hit.Item.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(hit => hit.Item.Id, StringComparer.Ordinal)
            .Select(hit => hit.Item).ToArray();
    }

    private static int Score(PaletteItem item, string[] words)
    {
        int total = 0;
        foreach (var word in words)
        {
            int score = new[] { Match(item.Name, word, 40), Match(item.Category, word, 15),
                Match(item.Source, word, 10), Match(item.Aliases, word, 5), Match(item.Shortcut, word, 0) }.Max();
            if (score < 0) return -1;
            total += score;
        }
        return total;
    }

    private static int Match(string text, string query, int weight)
    {
        if (string.IsNullOrEmpty(text)) return -1;
        if (text.Equals(query, StringComparison.OrdinalIgnoreCase)) return 300 + weight;
        if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 220 + weight;
        int substring = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (substring >= 0) return 170 + weight - Math.Min(substring, 40);
        int matched = 0, first = -1, last = -1, boundaries = 0;
        for (int i = 0; i < text.Length && matched < query.Length; i++)
        {
            if (char.ToUpperInvariant(text[i]) != char.ToUpperInvariant(query[matched])) continue;
            if (first < 0) first = i;
            if (i == 0 || !char.IsLetterOrDigit(text[i - 1]) || (char.IsUpper(text[i]) && char.IsLower(text[i - 1]))) boundaries++;
            last = i; matched++;
        }
        return matched == query.Length ? 60 + weight + boundaries * 8 - Math.Min(last - first - query.Length + 1, 40) : -1;
    }
}
