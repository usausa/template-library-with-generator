namespace Template.Library.Generator;

using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class Extensions
{
    public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
    {
        var ns = string.Empty;
        var node = syntax.Parent;
        while ((node is not null) && (node is not NamespaceDeclarationSyntax) && (node is not FileScopedNamespaceDeclarationSyntax))
        {
            node = node.Parent;
        }

        if (node is BaseNamespaceDeclarationSyntax namespaceSyntax)
        {
            ns = namespaceSyntax.Name.ToString();
            while (true)
            {
                if (namespaceSyntax.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                ns = $"{namespaceSyntax.Name}.{ns}";
                namespaceSyntax = parent;
            }
        }

        return ns;
    }
}
