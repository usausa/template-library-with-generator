namespace Template.Library;

public sealed class DiagnosticTests
{
    // ------------------------------------------------------------
    // Method definition
    // ------------------------------------------------------------

    [Fact]
    public void Tp0001NonStaticMethodEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal partial class Target
            {
                [CustomMethod]
                public partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0001");
    }

    [Fact]
    public void Tp0002MethodWithParameterEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod]
                public static partial void Method(int value);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0002");
    }

    [Fact]
    public void Tp0003NonVoidMethodEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod]
                public static partial int Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0003");
    }

    // ------------------------------------------------------------
    // Containing type
    // ------------------------------------------------------------

    [Fact]
    public void Tp0004NonPartialContainingTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static class Outer
            {
                internal static partial class Inner
                {
                    [CustomMethod]
                    public static partial void Method();
                }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0004");
    }

    [Fact]
    public void Tp0004FileLocalTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            file static partial class Target
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0004");
    }

    // ------------------------------------------------------------
    // Valid
    // ------------------------------------------------------------

    [Fact]
    public void ValidDefinitionEmitsNoDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
