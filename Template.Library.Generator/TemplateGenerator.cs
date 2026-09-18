namespace Template.Library.Generator;

using System;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
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
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol symbol)
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

        var containingType = symbol.ContainingType;
        var ns = String.IsNullOrEmpty(containingType.ContainingNamespace.Name)
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        return Results.Success(new MethodModel(
            ns,
            containingType.GetClassName(),
            containingType.IsValueType,
            symbol.DeclaredAccessibility,
            symbol.Name));
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static ImmutableArray<TypeModel> SelectTypes(ImmutableArray<Result<MethodModel>> methods) =>
        methods.SelectValue()
            .GroupBy(static x => new { x.Namespace, x.ClassName })
            .Select(static g => new TypeModel(
                g.Key.Namespace,
                g.Key.ClassName,
                g.First().IsValueType,
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

        var filename = MakeFilename(type.Namespace, type.ClassName);
        var source = builder.ToString();
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }

    private static void BuildSource(SourceBuilder builder, OptionModel option, TypeModel type)
    {
        var ns = type.Namespace;
        var className = type.ClassName;
        var isValueType = type.IsValueType;

        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // namespace
        if (!String.IsNullOrEmpty(ns))
        {
            builder.Namespace(ns);
            builder.NewLine();
        }

        // class
        builder
            .Indent()
            .Append("partial ")
            .Append(isValueType ? "struct " : "class ")
            .Append(className)
            .NewLine();
        builder.BeginScope();

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
                .Append("Console.WriteLine(\"Hello world.\");")
                .NewLine();

            builder.EndScope();
        }

        builder.EndScope();
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string MakeFilename(string ns, string className)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className.Replace('<', '[').Replace('>', ']'));
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
