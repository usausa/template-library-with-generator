namespace Template.Library;

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper.Testing;

using Template.Library.Generator;

internal static class GeneratorTestHelper
{
    private static GeneratorTestRunner Runner => GeneratorTestRunner
        .For<TemplateGenerator>()
        .WithReference(typeof(CustomMethodAttribute).Assembly)
        .WithDiagnosticPrefix("TP");

    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) => Runner.GetDiagnostics(source);

    public static string GetGeneratedSource(string source) => Runner.GetGeneratedSource(source);

    public static string GetGeneratedSource(string source, string optionValue) => Runner
        .WithGlobalOption("build_property.TemplateLibraryGeneratorValue", optionValue)
        .GetGeneratedSource(source);

    public static IncrementalRunResult RunIncremental(string source, string addedSource) =>
        Runner.WithTracking().RunIncremental(source, addedSource);
}
