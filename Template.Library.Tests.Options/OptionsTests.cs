namespace Template.Library;

using System.Globalization;

internal static partial class OptionsTarget
{
    [CustomMethod]
    public static partial void DefaultMessage();

    [CustomMethod("Hello from attribute.")]
    public static partial void AttributeMessage();
}

public sealed class OptionsTests
{
    // ------------------------------------------------------------
    // Option
    // ------------------------------------------------------------

    [Fact]
    public void DefaultMessageIsBuildPropertyOfThisProject()
    {
        // Arrange & Act
        var output = CaptureConsole(OptionsTarget.DefaultMessage);

        // Assert
        Assert.Equal($"Hello from options project.{Environment.NewLine}", output);
    }

    [Fact]
    public void AttributeMessageOverridesBuildProperty()
    {
        // Arrange & Act
        var output = CaptureConsole(OptionsTarget.AttributeMessage);

        // Assert
        Assert.Equal($"Hello from attribute.{Environment.NewLine}", output);
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string CaptureConsole(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }
}
