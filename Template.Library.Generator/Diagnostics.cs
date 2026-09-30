namespace Template.Library.Generator;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "TP0001",
        title: "Invalid method definition",
        messageFormat: "[CustomMethod] method must be static partial without an implementation. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidMethodParameter { get; } = new(
        id: "TP0002",
        title: "Invalid method parameter",
        messageFormat: "[CustomMethod] method must not have parameters. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidMethodReturnType { get; } = new(
        id: "TP0003",
        title: "Invalid method return type",
        messageFormat: "[CustomMethod] method must return void. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidContainingType { get; } = new(
        id: "TP0004",
        title: "Invalid containing type",
        messageFormat: "[CustomMethod] containing type must be partial and must not be file-local. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidAttributeArgument { get; } = new(
        id: "TP0005",
        title: "Invalid attribute argument",
        messageFormat: "[CustomMethod] argument is invalid. argument=[{0}], method=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor HintNameCollision { get; } = new(
        id: "TP0006",
        title: "Type name differs only in case",
        messageFormat: "[CustomMethod] type name differs only in case from another type, and its source is not generated. type=[{0}], other=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor MethodNotRegistered { get; } = new(
        id: "TP0007",
        title: "Method not registered",
        messageFormat: "[CustomMethod] method is not registered. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
