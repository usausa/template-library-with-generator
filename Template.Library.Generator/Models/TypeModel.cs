namespace Template.Library.Generator.Models;

using SourceGenerateHelper;

// 生成ファイル1つ = 1単位
internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    EquatableArray<MethodModel> Methods);
