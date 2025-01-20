namespace Develop;

using Template.Library.Attributes;

internal static class Program
{
    public static void Main()
    {
        _ = new Target();
    }
}

[Custom]
internal sealed class Target
{
}
