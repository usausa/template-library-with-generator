namespace Template.Library.Generator.Models;

using SourceGenerateHelper;

internal sealed record RegistryModel(
    EquatableArray<RegistryMethodModel> Methods);
