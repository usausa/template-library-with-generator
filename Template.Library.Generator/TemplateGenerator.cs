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

    private const string OutputPropertyName = "Output";

    private const string DefaultMessage = "Hello world.";

    private const string RegistryClassName = "CustomMethodRegistry";

    private const string RegistryHintName = "CustomMethod-Registry.g.cs";

    private static readonly SymbolDisplayFormat FullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

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

        var rootNamespaceProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => provider.GlobalOptions.GetValue<string>("RootNamespace"));

        var registryProvider = methodProvider
            .Select(static (methods, _) => SelectNames(methods))
            .WithTrackingName("Registry");

        context.RegisterSourceOutput(
            registryProvider.Combine(rootNamespaceProvider),
            static (context, provider) => ExecuteRegistry(context, provider.Right, provider.Left));
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

        // Validate attribute argument
        var attribute = context.Attributes[0];
        var message = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
        if ((message is not null) && String.IsNullOrWhiteSpace(message))
        {
            return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidAttributeArgument, GetArgumentLocation(attribute, null) ?? syntax.GetLocation(), "message", symbol.Name));
        }

        var output = MethodOutput.Console;
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != OutputPropertyName)
            {
                continue;
            }

            if ((argument.Value.Value is not int value) || !Enum.IsDefined(typeof(MethodOutput), value))
            {
                return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidAttributeArgument, GetArgumentLocation(attribute, OutputPropertyName) ?? syntax.GetLocation(), OutputPropertyName, symbol.Name));
            }

            output = (MethodOutput)value;
        }

        var containingType = symbol.ContainingType;
        var ns = String.IsNullOrEmpty(containingType.ContainingNamespace.Name)
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        return Results.Success(new MethodModel(
            ns,
            new EquatableArray<ContainingTypeModel>(types),
            symbol.DeclaredAccessibility,
            symbol.Name,
            $"{containingType.ToDisplayString(FullNameFormat)}.{symbol.Name}",
            message,
            output));
    }

    private static Location? GetArgumentLocation(AttributeData attribute, string? name)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax { ArgumentList: { } list })
        {
            return null;
        }

        var argument = name is null
            ? list.Arguments.FirstOrDefault(static x => x.NameEquals is null)
            : list.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.Text == name);
        return argument?.GetLocation();
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

    private static EquatableArray<string> SelectNames(ImmutableArray<Result<MethodModel>> methods) =>
        new(methods.SelectValue().Select(static x => x.FullName).OrderBy(static x => x, StringComparer.Ordinal));

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

            var message = method.Message ?? (String.IsNullOrEmpty(option.Value) ? DefaultMessage : option.Value);
            builder
                .Indent()
                .Append(GetWriteLine(method.Output))
                .Append("(")
                .Append(SymbolDisplay.FormatLiteral(message, true))
                .Append(");")
                .NewLine();

            builder.EndScope();
        }

        for (var i = 0; i < type.Types.Count; i++)
        {
            builder.EndScope();
        }
    }

    private static void ExecuteRegistry(SourceProductionContext context, string rootNamespace, EquatableArray<string> names)
    {
        if (names.Count == 0)
        {
            return;
        }

        var builder = new SourceBuilder();
        BuildRegistrySource(builder, rootNamespace, names);

        var source = builder.ToString();
        context.AddSource(RegistryHintName, SourceText.From(source, Encoding.UTF8));
    }

    private static void BuildRegistrySource(SourceBuilder builder, string ns, EquatableArray<string> names)
    {
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
            .Append("internal static class ")
            .Append(RegistryClassName)
            .NewLine();
        builder.BeginScope();

        builder
            .Indent()
            .Append("public const int Count = ")
            .Append(names.Count)
            .Append(";")
            .NewLine();
        builder.NewLine();

        builder
            .Indent()
            .Append("public static global::System.Collections.Generic.IReadOnlyList<string> Names { get; } = new[]")
            .NewLine();
        builder.BeginScope();
        foreach (var name in names)
        {
            builder
                .Indent()
                .Append(SymbolDisplay.FormatLiteral(name, true))
                .Append(",")
                .NewLine();
        }

        builder.EndScope(true);

        builder.EndScope();
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string GetWriteLine(MethodOutput output) =>
        output switch
        {
            MethodOutput.Debug => "global::System.Diagnostics.Debug.WriteLine",
            MethodOutput.Trace => "global::System.Diagnostics.Trace.WriteLine",
            _ => "global::System.Console.WriteLine"
        };

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
