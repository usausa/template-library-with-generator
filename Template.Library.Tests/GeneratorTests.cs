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

    [Fact]
    public void GeneratedSourceCompilesWithoutImplicitUsings()
    {
        // Arrange & Act
        var errors = GeneratorTestHelper.GetCompilationErrors(Source);

        // Assert
        Assert.Empty(errors);
    }

    // ------------------------------------------------------------
    // Containing type
    // ------------------------------------------------------------

    [Fact]
    public void NestedTypeGeneratesNestedDeclarations()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Outer
            {
                internal static partial class Inner
                {
                    [CustomMethod]
                    public static partial void Method();
                }
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetGeneratedSource(source);

        // Assert
        var outer = generated.IndexOf("partial class Outer", StringComparison.Ordinal);
        var inner = generated.IndexOf("partial class Inner", StringComparison.Ordinal);
        Assert.True(outer >= 0);
        Assert.True(inner > outer);
    }

    [Fact]
    public void RecordGeneratesPartialRecord()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal partial record Target
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetGeneratedSource(source);

        // Assert
        Assert.Contains("partial record Target", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordStructGeneratesPartialRecordStruct()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal partial record struct Target
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetGeneratedSource(source);

        // Assert
        Assert.Contains("partial record struct Target", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void InterfaceKeepsVarianceModifier()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal partial interface ITarget<out T>
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetGeneratedSource(source);

        // Assert
        Assert.Contains("partial interface ITarget<out T>", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericTypeKeepsTypeParameters()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target<TKey, TValue>
                where TKey : notnull
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetGeneratedSource(source);

        // Assert
        Assert.Contains("partial class Target<TKey, TValue>", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalNamespaceGeneratesWithoutNamespace()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            internal static partial class Target
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetGeneratedSource(source);

        // Assert
        Assert.DoesNotContain("namespace", generated, StringComparison.Ordinal);
        Assert.Contains("partial class Target", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedTypeAndUnderscoreNameGenerateSeparateFiles()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Outer
            {
                internal static partial class Inner
                {
                    [CustomMethod]
                    public static partial void Method();
                }
            }

            internal static partial class Outer_Inner
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var sources = GeneratorTestHelper.GetGeneratedSources(source);

        // Assert
        Assert.Contains(sources.Keys, static x => x.EndsWith("Test_Outer.Inner.g.cs", StringComparison.Ordinal));
        Assert.Contains(sources.Keys, static x => x.EndsWith("Test_Outer_Inner.g.cs", StringComparison.Ordinal));
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
