namespace Template.Library.Generator.Models;

using SourceGenerateHelper;

internal sealed record MethodModel(
    string Namespace,
    EquatableArray<ContainingTypeModel> Types,
    string HintName,
    string Signature,
    RegistryMethodModel? Registration,
    string? Message,
    MethodOutput Output,
    bool IsFallback);
