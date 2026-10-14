using Designer.Modules.CommandPalette;

int checks = 0;
void Check(bool result, string name) { if (!result) throw new Exception(name); checks++; }
PaletteItem Item(string id, string name, string category = "Q-SYS", bool enabled = true, string source = "Q-SYS", string aliases = "") =>
    new(id, name, category, source, "Ctrl+Shift+P", aliases, "Selection", "", enabled, enabled ? "" : "No selection");
var history = new PaletteHistory();
var items = new[] { Item("save", "Save"), Item("save-as", "Save As"), Item("palette", "Command Palette", "Command Palette"),
    Item("pack", "Pack Left", "Q-SYS", source: "Better Packing / Q-SYS"), Item("check", "Check Resources", aliases: "Check Design"),
    Item("disabled", "Delete", enabled: false), Item("zoom", "Zoom Fit to Screen") };
PaletteItem[] Search(string query) => PaletteSearch.Find(items, query, history);
Check(Search("save")[0].Id == "save", "Exact before prefix");
Check(Search("sAvE AS")[0].Id == "save-as", "Case insensitive multiple words");
Check(Search("cmdplt")[0].Id == "palette", "Fuzzy subsequence");
Check(Search("zf ts")[0].Id == "zoom", "Word initials across tokens");
Check(Search("better left")[0].Id == "pack", "Source and name tokens");
Check(Search("check design")[0].Id == "check", "Native aliases");
Check(Search("Ctrl+Shift+P").Length == items.Length, "Search shortcut");
Check(Search("  > save ")[0].Id == "save", "VS Code style prefix");
Check(Search("zzzzzzz").Length == 0, "No match");
Check(Search("").Length == items.Length && Search("Delete")[0].Enabled == false, "Unavailable stays discoverable");
history.Record("zoom"); history.Record("pack"); history.Record("zoom");
Check(Search("")[0].Id == "zoom" && Search("")[1].Id == "pack", "Recent commands deduplicated");
Check(Search("save")[0].Id == "save", "Search relevance precedes recency");
for (int i = 0; i < 35; i++) history.Record("recent" + i);
Check(history.Rank("zoom") == int.MaxValue && history.Rank("recent34") == 0, "Recent history bounded");
Check(Search("").Select(x => x.Id).SequenceEqual(Search("").Select(x => x.Id)), "Stable ordering");

int runs = 0, restored = 0;
var invocation = new PaletteInvocation(() => true, () => { restored++; return true; });
Check(invocation.TryExecute(() => true, () => runs++, out _), "Available action dispatched");
Check(!invocation.TryExecute(() => true, () => runs++, out _) && runs == 1 && restored == 1, "Exactly once");
invocation = new(() => false, () => { restored++; return true; });
Check(!invocation.TryExecute(() => true, () => runs++, out var reason) && restored == 1 && reason.Contains("changed"), "Stale target rejected before focus");
invocation = new(() => true, () => false);
Check(!invocation.TryExecute(() => true, () => runs++, out _), "Focus failure blocks action");
bool valid = true;
invocation = new(() => valid, () => { valid = false; return true; });
Check(!invocation.TryExecute(() => true, () => runs++, out _), "Context rechecked after focus changes");
invocation = new(() => true, () => true);
Check(!invocation.TryExecute(() => false, () => runs++, out _) && runs == 1, "Availability rechecked");
invocation = new(() => true, () => true);
try { invocation.TryExecute(() => true, () => throw new InvalidOperationException(), out _); } catch (InvalidOperationException) { }
Check(!invocation.TryExecute(() => true, () => runs++, out _) && runs == 1, "No retry after partial failure");
Console.WriteLine($"PASS: {checks} search, history, and invocation checks.");
