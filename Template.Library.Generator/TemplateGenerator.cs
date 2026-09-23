namespace Template.Library.Generator;

using System;
using System.Collections.Immutable;
using System.Text;

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

        var model = new MethodModel(
            ns,
            new EquatableArray<ContainingTypeModel>(types),
            GetAccessibilityModifiers(syntax),
            symbol.Name,
            IsRegistrable(symbol),
            message,
            output);

        // Validate registration
        if (!model.IsRegistrable)
        {
            return new Result<MethodModel>(model, new EquatableArray<DiagnosticInfo>([new DiagnosticInfo(Diagnostics.MethodNotRegistered, syntax.GetLocation(), symbol.Name)]));
        }

        return Results.Success(model);
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

    private static string GetAccessibilityModifiers(MethodDeclarationSyntax syntax) =>
        String.Join(" ", syntax.Modifiers
            .Where(static x => x.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.PrivateKeyword)
            .Select(static x => x.Text));

    private static bool IsRegistrable(IMethodSymbol symbol)
    {
        if (!IsAssemblyAccessible(symbol.DeclaredAccessibility))
        {
            return false;
        }

        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (!IsAssemblyAccessible(type.DeclaredAccessibility) || (type.Arity > 0))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAssemblyAccessible(Accessibility accessibility) =>
        accessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

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

        context.AddSource(MakeFilename(type.Namespace, type.Types), builder);
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
            builder.Indent();
            if (!String.IsNullOrEmpty(method.AccessibilityModifiers))
            {
                builder
                    .Append(method.AccessibilityModifiers)
                    .Append(" ");
            }

            builder
                .Append("static partial void ")
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

    // ------------------------------------------------------------
    // Registry
    // ------------------------------------------------------------

    private static RegistryModel SelectRegistry(ImmutableArray<Result<MethodModel>> methods) =>
        new(new EquatableArray<RegistryMethodModel>(methods.SelectValue()
            .Where(static x => x.IsRegistrable)
            .Select(static x => new RegistryMethodModel(
                x.Namespace,
                String.Join(".", x.Types.Select(static y => y.Name)),
                x.MethodName))));

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
        builder.NewLine();

        // type
        builder
            .Indent()
            .Append("internal static class CustomMethodInitializer")
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
            var name = MakeMethodName(method);
            builder
                .Indent()
                .Append("global::Template.Library.Internal.CustomMethodRegistry.RegisterMethod(")
                .Append(SymbolDisplay.FormatLiteral(name, true))
                .Append(", global::")
                .Append(name)
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

    private static string MakeMethodName(RegistryMethodModel method) =>
        String.IsNullOrEmpty(method.Namespace)
            ? $"{method.TypeName}.{method.MethodName}"
            : $"{method.Namespace}.{method.TypeName}.{method.MethodName}";

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
