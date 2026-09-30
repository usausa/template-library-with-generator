namespace Template.Library.Generator;

using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using SourceGenerateHelper;

using Template.Library.Generator.Models;

[Generator]
public sealed class TemplateGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Template.Library.CustomMethodAttribute";

    private const string OutputPropertyName = "Output";

    private const string DefaultMessage = "Hello world.";

    private static readonly SymbolDisplayFormat RegistryNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

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
            .WithTrackingName("Methods")
            .Collect();

        var treeProvider = context.ForAttributeWithMetadataNameSyntaxTrees(
            AttributeName,
            static (syntax, _) => IsMethodSyntax(syntax));

        context.RegisterSourceOutput(
            methodProvider.Combine(treeProvider),
            static (context, provider) => context.ReportDiagnostics(SelectDiagnostics(provider.Left), provider.Right));

        var typeProvider = methodProvider
            .SelectMany(static (methods, _) => SelectTypes(methods))
            .WithTrackingName("Types");

        context.RegisterImplementationSourceOutput(
            typeProvider.Combine(optionProvider),
            static (context, provider) => Execute(context, provider.Right, provider.Left));

        var registryProvider = methodProvider
            .Select(static (methods, _) => SelectRegistry(methods))
            .WithTrackingName("Registry");

        context.RegisterImplementationSourceOutput(
            registryProvider,
            static (context, registry) => ExecuteRegistry(context, registry));
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
        if (context.TargetSymbol is not IMethodSymbol symbol)
        {
            return Results.Errors<MethodModel>();
        }

        var location = syntax.Identifier.GetLocation();
        var definitionError = ValidateDefinition(symbol, location);
        var types = GetContainingTypes(syntax, out var typeError);
        var argumentError = ReadAttribute(context.Attributes[0], symbol.Name, location, out var message, out var output);

        var containingType = symbol.ContainingType;
        var ns = containingType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        var model = new MethodModel(
            ns,
            new EquatableArray<ContainingTypeModel>(types),
            HintNameBuilder.BuildFromType(containingType),
            symbol.GetImplementationSignature(syntax),
            IsRegistrable(symbol) ? new RegistryMethodModel(GetRegistryName(symbol), GetMethodReference(symbol)) : null,
            message,
            output,
            false);

        if ((definitionError ?? typeError ?? argumentError) is { } error)
        {
            return symbol.IsPartialDefinition && (symbol.PartialImplementationPart is null) && (typeError is null)
                ? new Result<MethodModel>(model with { Registration = null, IsFallback = true }, new EquatableArray<DiagnosticInfo>([error]))
                : Results.Error<MethodModel>(error);
        }

        if (model.Registration is null)
        {
            return new Result<MethodModel>(model, new EquatableArray<DiagnosticInfo>([new DiagnosticInfo(Diagnostics.MethodNotRegistered, location, symbol.Name)]));
        }

        return Results.Success(model);
    }

    private static DiagnosticInfo? ValidateDefinition(IMethodSymbol symbol, Location location)
    {
        if (!symbol.IsStatic || !symbol.IsPartialDefinition || (symbol.PartialImplementationPart is not null))
        {
            return new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, location, symbol.Name);
        }

        if (symbol.Parameters.Length != 0)
        {
            return new DiagnosticInfo(Diagnostics.InvalidMethodParameter, location, symbol.Name);
        }

        if (!symbol.ReturnsVoid)
        {
            return new DiagnosticInfo(Diagnostics.InvalidMethodReturnType, location, symbol.Name);
        }

        return null;
    }

    private static List<ContainingTypeModel> GetContainingTypes(MethodDeclarationSyntax syntax, out DiagnosticInfo? error)
    {
        var types = new List<ContainingTypeModel>();
        foreach (var typeSyntax in syntax.Ancestors().OfType<TypeDeclarationSyntax>())
        {
            var keyword = GetKeyword(typeSyntax);
            if ((keyword is null) ||
                !typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) ||
                typeSyntax.Modifiers.Any(SyntaxKind.FileKeyword))
            {
                error = new DiagnosticInfo(Diagnostics.InvalidContainingType, typeSyntax.Identifier.GetLocation(), typeSyntax.Identifier.Text);
                return types;
            }

            types.Insert(0, new ContainingTypeModel(keyword, GetTypeName(typeSyntax)));
        }

        error = null;
        return types;
    }

    private static DiagnosticInfo? ReadAttribute(AttributeData attribute, string methodName, Location location, out string? message, out MethodOutput output)
    {
        output = MethodOutput.Console;
        if (attribute.TryGetConstructorArgument(0, out message) && String.IsNullOrWhiteSpace(message))
        {
            return new DiagnosticInfo(Diagnostics.InvalidAttributeArgument, GetArgumentLocation(attribute, null) ?? location, "message", methodName);
        }

        if (attribute.TryGetNamedArgument(OutputPropertyName, out var value) &&
            (!value.TryGetValue(out output) || !Enum.IsDefined(typeof(MethodOutput), output)))
        {
            return new DiagnosticInfo(Diagnostics.InvalidAttributeArgument, GetArgumentLocation(attribute, OutputPropertyName) ?? location, OutputPropertyName, methodName);
        }

        return null;
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

    private static bool IsRegistrable(IMethodSymbol symbol)
    {
        if (!IsAssemblyAccessible(symbol.DeclaredAccessibility) || symbol.IsGenericMethod || symbol.IsVirtual || IsObsoleteError(symbol))
        {
            return false;
        }

        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (!IsAssemblyAccessible(type.DeclaredAccessibility) || (type.Arity > 0) || IsObsoleteError(type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsObsoleteError(ISymbol symbol) =>
        symbol.IsObsolete(out var isError) && isError;

    private static bool IsAssemblyAccessible(Accessibility accessibility) =>
        accessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

    private static string GetRegistryName(IMethodSymbol symbol) =>
        $"{symbol.ContainingType.ToDisplayString(RegistryNameFormat)}.{symbol.Name}";

    private static string GetMethodReference(IMethodSymbol symbol) =>
        $"{symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{CSharpIdentifier.Escape(symbol.Name)}";

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static IEnumerable<DiagnosticInfo> SelectDiagnostics(ImmutableArray<Result<MethodModel>> methods) =>
        methods.SelectError()
            .Concat(FindHintNameCollisions(methods).Select(static x => new DiagnosticInfo(Diagnostics.HintNameCollision, (Location?)null, x.Name, x.Other)))
            .Distinct();

    private static List<(string HintName, string Name, string Other)> FindHintNameCollisions(ImmutableArray<Result<MethodModel>> methods)
    {
        var collisions = new List<(string HintName, string Name, string Other)>();
        var firsts = new Dictionary<string, MethodModel>(StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in methods.SelectValue().OrderBy(static x => x.HintName, StringComparer.Ordinal))
        {
            if (!firsts.TryGetValue(method.HintName, out var first))
            {
                firsts.Add(method.HintName, method);
            }
            else if ((first.HintName != method.HintName) && reported.Add(method.HintName))
            {
                collisions.Add((method.HintName, GetTypeName(method), GetTypeName(first)));
            }
        }

        return collisions;
    }

    private static string GetTypeName(MethodModel method)
    {
        var name = String.Join(".", method.Types.Select(static x => x.Name));
        return String.IsNullOrEmpty(method.Namespace) ? name : $"{method.Namespace}.{name}";
    }

    private static ImmutableArray<TypeModel> SelectTypes(ImmutableArray<Result<MethodModel>> methods)
    {
        var collisions = new HashSet<string>(FindHintNameCollisions(methods).Select(static x => x.HintName), StringComparer.Ordinal);
        return methods.SelectValue()
            .Where(x => !collisions.Contains(x.HintName))
            .GroupBy(static x => new { x.Namespace, x.Types, x.HintName })
            .Select(static g => new TypeModel(
                g.Key.Namespace,
                g.Key.Types,
                g.Key.HintName,
                new EquatableArray<MethodModel>(g)))
            .ToImmutableArray();
    }

    private static void Execute(SourceProductionContext context, OptionModel option, TypeModel type)
    {
        var builder = new SourceBuilder();
        BuildSource(builder, option, type);

        context.AddSource(type.HintName, builder);
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
                .Append(method.Signature)
                .NewLine();
            builder.BeginScope();

            if (method.IsFallback)
            {
                builder
                    .Indent()
                    .Append("throw new global::System.InvalidOperationException();")
                    .NewLine();
            }
            else
            {
                var message = method.Message ?? (String.IsNullOrEmpty(option.Value) ? DefaultMessage : option.Value);
                builder
                    .Indent()
                    .Append(GetWriteLine(method.Output))
                    .Append("(")
                    .Append(CSharpLiteral.Format(message))
                    .Append(");")
                    .NewLine();
            }

            builder.EndScope();
        }

        for (var i = 0; i < type.Types.Count; i++)
        {
            builder.EndScope();
        }
    }

    // ------------------------------------------------------------
    // Registry
    // ------------------------------------------------------------

    private static RegistryModel SelectRegistry(ImmutableArray<Result<MethodModel>> methods) =>
        new(new EquatableArray<RegistryMethodModel>(methods.SelectValue()
            .Select(static x => x.Registration)
            .OfType<RegistryMethodModel>()));

    private static void ExecuteRegistry(SourceProductionContext context, RegistryModel registry)
    {
        if (registry.Methods.Count == 0)
        {
            return;
        }

        var builder = new SourceBuilder();
        BuildRegistrySource(builder, registry);

        context.AddSource("CustomMethodInitializer.g.cs", builder);
    }

    private static void BuildRegistrySource(SourceBuilder builder, RegistryModel registry)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Disable("CS0612, CS0618");
        builder.NewLine();

        // type
        builder
            .Indent()
            .Append("file static class CustomMethodInitializer")
            .NewLine();
        builder.BeginScope();

        // method
        builder
            .Indent()
            .Append("[global::System.Runtime.CompilerServices.ModuleInitializer]")
            .NewLine();
        builder
            .Indent()
            .Append("public static void Initialize()")
            .NewLine();
        builder.BeginScope();

        foreach (var method in registry.Methods)
        {
            builder
                .Indent()
                .Append("global::Template.Library.Internal.CustomMethodRegistry.RegisterMethod(")
                .Append(CSharpLiteral.Format(method.Name))
                .Append(", ")
                .Append(method.Reference)
                .Append(");")
                .NewLine();
        }

        builder.EndScope();
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
}
