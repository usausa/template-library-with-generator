namespace Template.Library.Generator.Models;

using SourceGenerateHelper;

internal sealed record MethodModel(
    string Namespace,
    EquatableArray<ContainingTypeModel> Types,
    string AccessibilityModifiers,
    string MethodName,
    bool IsRegistrable,
    string? Message,
    MethodOutput Output);
