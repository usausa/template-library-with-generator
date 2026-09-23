namespace Template.Library.Internal;

using System.Collections.Concurrent;
using System.ComponentModel;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class CustomMethodRegistry
{
    private static readonly ConcurrentDictionary<string, Action> Methods = new(StringComparer.Ordinal);

    // ------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------

    public static void RegisterMethod(string name, Action method)
    {
        Methods[name] = method;
    }

    // ------------------------------------------------------------
    // Lookup
    // ------------------------------------------------------------

    internal static Action? FindMethod(string name) => Methods.GetValueOrDefault(name);
}
