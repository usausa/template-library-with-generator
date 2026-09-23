namespace Template.Library;

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper.Testing;

using Template.Library.Generator;

internal static class GeneratorTestHelper
{
    private const string RegistryHintName = "CustomMethod-Registry.g.cs";

    private static GeneratorTestRunner Runner => GeneratorTestRunner
        .For<TemplateGenerator>()
        .WithReference(typeof(CustomMethodAttribute).Assembly)
        .WithDiagnosticPrefix("TP")
        .VerifyCompiles();

    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) => Runner.GetDiagnostics(source);

    public static IReadOnlyList<Diagnostic> GetDiagnosticsWithoutVerify(string source) =>
        Runner.VerifyCompiles(false).GetDiagnostics(source);

    public static string GetRegistrySourceWithoutVerify(string source) => Runner
        .VerifyCompiles(false)
        .Run(source)
        .GeneratedSource(RegistryHintName);

    public static IReadOnlyList<Diagnostic> GetCompilationErrors(string source) =>
        Runner.VerifyCompiles(false).Run(source).CompilationErrors;

    public static string GetGeneratedSource(string source) => GetTypeSource(Runner.Run(source));

    public static IReadOnlyDictionary<string, string> GetGeneratedSources(string source) => Runner.Run(source).GeneratedSources;

    public static string GetGeneratedSource(string source, string optionValue) => GetTypeSource(Runner
        .WithGlobalOption("build_property.TemplateLibraryGeneratorValue", optionValue)
        .Run(source));

    public static string GetRegistrySource(string source, string rootNamespace) => Runner
        .WithGlobalOption("build_property.RootNamespace", rootNamespace)
        .Run(source)
        .GeneratedSource(RegistryHintName);

    public static IncrementalRunResult RunIncremental(string source, string addedSource) =>
        Runner.WithTracking().RunIncremental(source, addedSource);

    private static string GetTypeSource(GeneratorTestResult result) =>
        result.GeneratedSources.First(static x => x.Key != RegistryHintName).Value;
}
