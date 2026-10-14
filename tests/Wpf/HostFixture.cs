using System.Reflection;
using System.Windows;
namespace QsysMods.Core;

// Only Designer discovery/theme are substituted. The palette view, search,
// context validation, focus restoration and one-shot invocation are production.
public static class Host
{
    public const BindingFlags Static = BindingFlags.Public | BindingFlags.Static;
    public static Type Type(string name) => throw new NotSupportedException("Use injected context in the WPF harness.");
    public static void Theme(Window window) { }
}
public static class HostSelection { public static object GlobalSelection => throw new NotSupportedException(); }
