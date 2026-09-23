namespace Develop;

using Template.Library;

internal static class Program
{
    public static void Main()
    {
        Target.Method();
        Target.MessageMethod();
        Target.DebugMethod();

        CustomMethodProvider.FindMethod("Develop.Target.MessageMethod")?.Invoke();
    }
}

internal static partial class Target
{
    [CustomMethod]
    public static partial void Method();

    [CustomMethod("Hello from attribute.")]
    public static partial void MessageMethod();

    [CustomMethod(Output = CustomMethodOutput.Debug)]
    public static partial void DebugMethod();
}
