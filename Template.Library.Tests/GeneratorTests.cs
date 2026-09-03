namespace Template.Library;

public sealed class GeneratorTests
{
    private const string Source =
        """
        using Template.Library;

        namespace Test;

        internal static partial class Target
        {
            [CustomMethod]
            public static partial void Method();
        }
        """;

    // ------------------------------------------------------------
    // Basic
    // ------------------------------------------------------------

    [Fact]
    public void CustomMethodGeneratesPartialImplementation()
    {
        // Arrange & Act
        var generated = GeneratorTestHelper.GetGeneratedSource(Source);

        // Assert
        Assert.Contains("partial class Target", generated, StringComparison.Ordinal);
        Assert.Contains("static partial void Method()", generated, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Option
    // ------------------------------------------------------------

    [Fact]
    public void BuildPropertyIsEmbeddedInGeneratedSource()
    {
        // Arrange & Act
        var generated = GeneratorTestHelper.GetGeneratedSource(Source, "custom");

        // Assert
        Assert.Contains("// Option: custom", generated, StringComparison.Ordinal);
    }
}
