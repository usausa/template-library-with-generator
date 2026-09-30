namespace Template.Library;

using System.Globalization;
using System.Reflection;

using Microsoft.CodeAnalysis;

using Template.Library.Generator;

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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0001"], problems);
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0002"], problems);
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0003"], problems);
    }

    [Fact]
    public void Tp0001ImplementedMethodEmitsDiagnostic()
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

                public static partial void Method()
                {
                }
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0001"], problems);
    }

    [Fact]
    public void ErrorGeneratesThrowingImplementation()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod]
                public static partial int Method(int value);
            }
            """;

        // Act
        var generated = GeneratorTestHelper.GetAllGeneratedSource(source);

        // Assert
        Assert.Contains("public static partial int Method(int value)", generated, StringComparison.Ordinal);
        Assert.Contains("throw new global::System.InvalidOperationException();", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("CustomMethodInitializer", generated, StringComparison.Ordinal);
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0004", "CS8795"], problems);
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0004", "CS8795"], problems);
    }

    [Fact]
    public void Tp0004IsReportedOncePerType()
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
                    public static partial void First();

                    [CustomMethod]
                    public static partial void Second();
                }
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Single(problems, static x => x == "TP0004");
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
        Assert.Equal(["TP0005"], GeneratorTestHelper.GetProblemIds(source));
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0005"], problems);
    }

    // ------------------------------------------------------------
    // Type name
    // ------------------------------------------------------------

    [Fact]
    public void Tp0006TypeNamesDifferingOnlyInCaseEmitDiagnostic()
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

            internal static partial class target
            {
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsWithoutVerify(source);
        var generated = GeneratorTestHelper.GetAllGeneratedSource(source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TP0006", diagnostic.Id);
        Assert.Contains("type=[Test.target]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("partial class Target", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("partial class target", generated, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Registry
    // ------------------------------------------------------------

    [Fact]
    public void Tp0007PrivateMethodEmitsDiagnostic()
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0007"], problems);
    }

    [Fact]
    public void Tp0007PrivateContainingTypeEmitsDiagnostic()
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0007"], problems);
    }

    [Fact]
    public void Tp0007GenericContainingTypeEmitsDiagnostic()
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0007"], problems);
    }

    [Fact]
    public void Tp0007GenericMethodEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [CustomMethod]
                public static partial void Method<T>()
                    where T : class, new();
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0007"], problems);
    }

    [Fact]
    public void Tp0007StaticVirtualInterfaceMethodEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using Template.Library;

            namespace Test;

            internal partial interface ITarget
            {
                [CustomMethod]
                public static virtual partial void Method();
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0007"], problems);
    }

    [Fact]
    public void Tp0007ProtectedInternalMethodEmitsNoDiagnostic()
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void ObsoleteMethodIsRegisteredWithoutWarning()
    {
        // Arrange
        const string source =
            """
            using System;
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [Obsolete]
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);
        var generated = GeneratorTestHelper.GetAllGeneratedSource(source);

        // Assert
        Assert.Empty(problems);
        Assert.Contains("RegisterMethod(\"Test.Target.Method\", global::Test.Target.Method);", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Tp0007ObsoleteErrorMethodEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using System;
            using Template.Library;

            namespace Test;

            internal static partial class Target
            {
                [Obsolete("Removed", true)]
                [CustomMethod]
                public static partial void Method();
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["TP0007"], problems);
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
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    // ------------------------------------------------------------
    // Descriptor
    // ------------------------------------------------------------

    [Fact]
    public void ErrorsCannotBeSuppressed()
    {
        // Arrange
        var descriptors = typeof(TemplateGenerator).Assembly.GetType("Template.Library.Generator.Diagnostics", throwOnError: true)!
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(static x => x.PropertyType == typeof(DiagnosticDescriptor))
            .Select(static x => (DiagnosticDescriptor)x.GetValue(null)!)
            .ToList();

        // Assert
        Assert.NotEmpty(descriptors);
        Assert.All(
            descriptors.Where(static x => x.DefaultSeverity == DiagnosticSeverity.Error),
            static x => Assert.Equal([WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.Compiler], x.CustomTags));
    }
}
