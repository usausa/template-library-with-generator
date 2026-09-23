namespace Template.Library;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTests
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

    private const string NestedSource =
        """
        using Template.Library;

        namespace Test;

        internal static partial class Outer<T>
        {
            internal partial record struct Inner
            {
                [CustomMethod]
                public static partial void Method();
            }
        }
        """;

    private const string UnrelatedSource =
        """
        namespace Other;

        internal sealed class Unrelated;
        """;

    private const string AddedTargetSource =
        """
        using Template.Library;

        namespace Test;

        internal static partial class AddedTarget
        {
            [CustomMethod]
            public static partial void Method();
        }
        """;

    // ------------------------------------------------------------
    // Cache
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void NestedTypeUnrelatedEditKeepsModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(NestedSource, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void TargetEditRebuildsModel()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void AddedTargetKeepsExistingTypeCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => !x.IsChanged());
    }

    [Fact]
    public void AddedTargetRebuildsRegistry()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        var steps = result.SecondResult.Results[0].TrackedSteps;
        Assert.Contains(steps["Registry"].SelectMany(static x => x.Outputs), static x => x.Reason.IsChanged());
        Assert.Contains(steps["Types"].SelectMany(static x => x.Outputs), static x => !x.Reason.IsChanged());
    }

    [Fact]
    public void UnrelatedEditKeepsRegistryCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, UnrelatedSource);

        // Assert
        var steps = result.SecondResult.Results[0].TrackedSteps;
        Assert.DoesNotContain(steps["Registry"].SelectMany(static x => x.Outputs), static x => x.Reason.IsChanged());
    }
}
