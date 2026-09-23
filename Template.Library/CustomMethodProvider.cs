namespace Template.Library;

using Template.Library.Internal;

public static class CustomMethodProvider
{
    public static Action? FindMethod(string name) => CustomMethodRegistry.FindMethod(name);
}
