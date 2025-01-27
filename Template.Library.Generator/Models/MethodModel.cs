namespace Template.Library.Generator.Models;

using Template.Library.Generator.Helpers;

using Microsoft.CodeAnalysis;

internal sealed record MethodModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    Accessibility MethodAccessibility,
    string MethodName);
