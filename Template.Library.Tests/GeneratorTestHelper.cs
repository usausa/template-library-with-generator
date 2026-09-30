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
        .WithDiagnosticPrefix("TP")
        .VerifyCompiles();

    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) => Runner.GetDiagnostics(source);

    public static IReadOnlyList<Diagnostic> GetDiagnosticsWithoutVerify(string source) =>
        Runner.VerifyCompiles(false).GetDiagnostics(source);

    public static IReadOnlyList<Diagnostic> GetCompilationErrors(string source) =>
        Runner.VerifyCompiles(false).Run(source).CompilationErrors;

    public static IReadOnlyList<string> GetProblemIds(string source) =>
        [.. Runner.VerifyCompiles(false).GetProblems(source).Select(static x => x.Id)];

    public static string GetGeneratedSource(string source) => Runner.GetGeneratedSource(source);

    public static string GetAllGeneratedSource(string source) => Runner.VerifyCompiles(false).Run(source).AllGeneratedText;

    public static IReadOnlyDictionary<string, string> GetGeneratedSources(string source) => Runner.Run(source).GeneratedSources;

    public static string GetGeneratedSource(string source, string optionValue) => Runner
        .WithGlobalOption("build_property.TemplateLibraryGeneratorValue", optionValue)
        .GetGeneratedSource(source);

    public static IncrementalRunResult RunIncremental(string source, string addedSource) =>
        Runner.WithTracking().RunIncremental(source, addedSource);

    public static IncrementalRunResult RunIncrementalEdit(string source, string editedSource) =>
        Runner.WithTracking().RunIncrementalEdit(source, editedSource);
}
