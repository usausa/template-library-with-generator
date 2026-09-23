namespace Template.Library.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal sealed record MethodModel(
    string Namespace,
    EquatableArray<ContainingTypeModel> Types,
    Accessibility MethodAccessibility,
    string MethodName,
    string? Message,
    MethodOutput Output);
