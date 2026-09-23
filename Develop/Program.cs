namespace Develop;

using Template.Library;

internal static class Program
{
    public static void Main()
    {
        Target.Method();
        Target.MessageMethod();
        Target.DebugMethod();

        Console.WriteLine($"Registry: {CustomMethodRegistry.Count} [{String.Join(", ", CustomMethodRegistry.Names)}]");
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
