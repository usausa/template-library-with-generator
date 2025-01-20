namespace Template.Library.Generator;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class TemplateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //context.RegisterPostInitializationOutput(ctx =>
        //{
        //    ctx.AddSource("TemplateGenerator.g.cs", SourceText.From("// dummy", Encoding.UTF8));
        //});

        var propertyProvider = context.AnalyzerConfigOptionsProvider
            .Select(SelectBuildProperty);

        var classProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Template.Library.Attributes.CustomAttribute",
                ClassPredicate,
                ClassTransform)
            .Collect();

        context.RegisterImplementationSourceOutput(
            propertyProvider.Combine(classProvider),
            static (context, provider) => Execute(context, provider.Left, provider.Right));
    }

    private static BuildPropertyModel SelectBuildProperty(AnalyzerConfigOptionsProvider provider, CancellationToken token)
    {
        var value = provider.GlobalOptions.TryGetValue("build_property.TemplateLibraryGeneratorValue", out var ret) ? ret : string.Empty;
        return new BuildPropertyModel(value);
    }

    private static bool ClassPredicate(SyntaxNode syntax, CancellationToken token) =>
        syntax is ClassDeclarationSyntax;

    private static ClassModel ClassTransform(GeneratorAttributeSyntaxContext context, CancellationToken token) =>
        new(context.TargetSymbol.Name, ((BaseTypeDeclarationSyntax)context.TargetNode).GetNamespace());

    private static void Execute(SourceProductionContext context, BuildPropertyModel buildProperty, ImmutableArray<ClassModel> classes)
    {
        var sb = new StringBuilder();
        var filename = new StringBuilder();

        foreach (var @class in classes)
        {
            sb.Clear();
            sb.AppendLine($"// Property: {buildProperty.Value}");
            sb.AppendLine($"// Class: {@class.Namespace}.{@class.Name}");

            context.AddSource(MakeFilename(filename, @class.Namespace, @class.Name), SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static string MakeFilename(StringBuilder buffer, string ns, string className)
    {
        buffer.Clear();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className);
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
