namespace Template.Library.Generator;

using System;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

using Template.Library.Generator.Models;

[Generator]
public sealed class TemplateGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Template.Library.CustomMethodAttribute";

    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionProvider = context.AnalyzerConfigOptionsProvider
            .Select(SelectOption);

        var methodProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeName,
                static (syntax, _) => IsMethodSyntax(syntax),
                static (context, _) => GetMethodModel(context))
            .Collect();

        context.RegisterSourceOutput(
            methodProvider,
            static (context, methods) => ReportDiagnostics(context, methods));

        var typeProvider = methodProvider
            .SelectMany(static (methods, _) => SelectTypes(methods))
            .WithTrackingName("Types");

        context.RegisterImplementationSourceOutput(
            typeProvider.Combine(optionProvider),
            static (context, provider) => Execute(context, provider.Right, provider.Left));
    }

    // ------------------------------------------------------------
    // Parser
    // ------------------------------------------------------------

    private static OptionModel SelectOption(AnalyzerConfigOptionsProvider provider, CancellationToken token)
    {
        var value = provider.GlobalOptions.GetValue<string>("TemplateLibraryGeneratorValue");
        return new OptionModel(value);
    }

    private static bool IsMethodSyntax(SyntaxNode syntax) =>
        syntax is MethodDeclarationSyntax;

    private static Result<MethodModel> GetMethodModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not { } symbol)
        {
            return Results.Errors<MethodModel>();
        }

        // Validate method definition
        if (!symbol.IsStatic || !symbol.IsPartialDefinition)
        {
            return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, syntax.GetLocation(), symbol.Name));
        }

        // Validate parameter
        if (symbol.Parameters.Length != 0)
        {
            return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
        }

        // Validate return type
        if (!symbol.ReturnsVoid)
        {
            return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodReturnType, syntax.GetLocation(), symbol.Name));
        }

        // Validate containing type
        var types = new List<ContainingTypeModel>();
        foreach (var typeSyntax in syntax.Ancestors().OfType<TypeDeclarationSyntax>())
        {
            var keyword = GetKeyword(typeSyntax);
            if ((keyword is null) ||
                !typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) ||
                typeSyntax.Modifiers.Any(SyntaxKind.FileKeyword))
            {
                return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidContainingType, typeSyntax.Identifier.GetLocation(), typeSyntax.Identifier.Text));
            }

            types.Insert(0, new ContainingTypeModel(keyword, GetTypeName(typeSyntax), GetHintName(typeSyntax)));
        }

        var containingType = symbol.ContainingType;
        var ns = String.IsNullOrEmpty(containingType.ContainingNamespace.Name)
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        return Results.Success(new MethodModel(
            ns,
            new EquatableArray<ContainingTypeModel>(types),
            symbol.DeclaredAccessibility,
            symbol.Name));
    }

    private static string? GetKeyword(TypeDeclarationSyntax syntax) =>
        syntax switch
        {
            RecordDeclarationSyntax record => record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record",
            ClassDeclarationSyntax => "class",
            StructDeclarationSyntax => "struct",
            InterfaceDeclarationSyntax => "interface",
            _ => null
        };

    private static string GetTypeName(TypeDeclarationSyntax syntax) =>
        syntax.TypeParameterList is { Parameters.Count: > 0 } list
            ? $"{syntax.Identifier.Text}<{String.Join(", ", list.Parameters.Select(static x => x.VarianceKeyword.IsKind(SyntaxKind.None) ? x.Identifier.Text : $"{x.VarianceKeyword.Text} {x.Identifier.Text}"))}>"
            : syntax.Identifier.Text;

    private static string GetHintName(TypeDeclarationSyntax syntax) =>
        syntax.TypeParameterList is { Parameters.Count: > 0 } list
            ? $"{syntax.Identifier.Text}[{String.Join(",", list.Parameters.Select(static x => x.Identifier.Text))}]"
            : syntax.Identifier.Text;

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static ImmutableArray<TypeModel> SelectTypes(ImmutableArray<Result<MethodModel>> methods) =>
        methods.SelectValue()
            .GroupBy(static x => new { x.Namespace, x.Types })
            .Select(static g => new TypeModel(
                g.Key.Namespace,
                g.Key.Types,
                new EquatableArray<MethodModel>(g)))
            .ToImmutableArray();

    private static void ReportDiagnostics(SourceProductionContext context, ImmutableArray<Result<MethodModel>> methods)
    {
        foreach (var info in methods.SelectError())
        {
            context.ReportDiagnostic(info);
        }
    }

    private static void Execute(SourceProductionContext context, OptionModel option, TypeModel type)
    {
        var builder = new SourceBuilder();
        BuildSource(builder, option, type);

        var filename = MakeFilename(type.Namespace, type.Types);
        var source = builder.ToString();
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }

    private static void BuildSource(SourceBuilder builder, OptionModel option, TypeModel type)
    {
        var ns = type.Namespace;

        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // namespace
        if (!String.IsNullOrEmpty(ns))
        {
            builder.Namespace(ns);
            builder.NewLine();
        }

        // type
        foreach (var containingType in type.Types)
        {
            builder
                .Indent()
                .Append("partial ")
                .Append(containingType.Keyword)
                .Append(" ")
                .Append(containingType.Name)
                .NewLine();
            builder.BeginScope();
        }

        builder.Indent().Append("// Option: ").Append(option.Value).NewLine();

        var first = true;
        foreach (var method in type.Methods)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                builder.NewLine();
            }

            // method
            builder
                .Indent()
                .Append(method.MethodAccessibility.ToText())
                .Append(" static partial void ")
                .Append(method.MethodName)
                .Append("()")
                .NewLine();
            builder.BeginScope();

            builder
                .Indent()
                .Append("global::System.Console.WriteLine(\"Hello world.\");")
                .NewLine();

            builder.EndScope();
        }

        for (var i = 0; i < type.Types.Count; i++)
        {
            builder.EndScope();
        }
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string MakeFilename(string ns, EquatableArray<ContainingTypeModel> types)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(String.Join(".", types.Select(static x => x.HintName)));
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
