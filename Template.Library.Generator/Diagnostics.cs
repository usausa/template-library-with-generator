namespace Template.Library.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidMethodDefinition => new(
        id: "TP0001",
        title: "Invalid method definition",
        messageFormat: "Method must be static partial. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMethodParameter => new(
        id: "TP0002",
        title: "Invalid method parameter",
        messageFormat: "Parameter type must be IServiceCollection. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
