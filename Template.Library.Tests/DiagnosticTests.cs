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
    // Attribute argument
    // ------------------------------------------------------------

    [Fact]
    public void Tp0005EmptyMessageEmitsDiagnosticAtArgument()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod("")]
                public static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        var diagnostic = Assert.Single(diagnostics, static x => x.Id == "TP0005");
        var span = diagnostic.Location.SourceSpan;
        Assert.Equal("\"\"", source[span.Start..span.End]);
    }

    [Fact]
    public void Tp0005UndefinedOutputEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod(Output = (CustomMethodOutput)99)]
                public static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0005");
    }

    // ------------------------------------------------------------
    // Registry
    // ------------------------------------------------------------

    [Fact]
    public void Tp0006PrivateMethodEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod]
                private static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0006");
    }

    [Fact]
    public void Tp0006PrivateContainingTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Outer
            {
                private static partial class Inner
                {
                    [CustomMethod]
                    public static partial void Method();
                }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0006");
    }

    [Fact]
    public void Tp0006GenericContainingTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target<T>
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "TP0006");
    }

    [Fact]
    public void Tp0006ProtectedInternalMethodEmitsNoDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal partial class Target
            {
                [CustomMethod]
                protected internal static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Empty(diagnostics);
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
