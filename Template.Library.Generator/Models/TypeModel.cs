namespace Template.Library.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    EquatableArray<ContainingTypeModel> Types,
    EquatableArray<MethodModel> Methods);
