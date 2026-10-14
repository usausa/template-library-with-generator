# template-library-with-generator

## What is this?

Template of a library with a source generator. The generator implements `[CustomMethod]` partial methods and registers them to be found by name.

## Usage

### Source

```csharp
namespace Example;

using Template.Library;

internal static partial class Target
{
    [CustomMethod]
    public static partial void Method();

    [CustomMethod("Hello from attribute.")]
    public static partial void MessageMethod();

    [CustomMethod(Output = CustomMethodOutput.Debug)]
    public static partial void DebugMethod();
}
```

### Result

```csharp
namespace Example;

partial class Target
{
    public static partial void Method()
    {
        global::System.Console.WriteLine("Hello world.");
    }

    public static partial void MessageMethod()
    {
        global::System.Console.WriteLine("Hello from attribute.");
    }

    public static partial void DebugMethod()
    {
        global::System.Diagnostics.Debug.WriteLine("Hello world.");
    }
}
```

### Find by name

```csharp
CustomMethodProvider.FindMethod("Example.Target.MessageMethod")?.Invoke();
```

## Attribute Parameters

| Parameter | Description |
| --- | --- |
| `message` | Message to write. The default is the MSBuild property `TemplateLibraryGeneratorValue`, or `Hello world.` |
| `Output` | `Console` (default), `Debug` or `Trace` |

## MSBuild Properties

| Property | Description |
| --- | --- |
| `TemplateLibraryGeneratorValue` | Default message |

## Limitations

- `FindMethod` finds the methods of an assembly after its module initializer has run. Methods of assemblies that are not loaded yet are not found.

## Requirements

- C# compiler of Roslyn 5.9 or later. Older compilers do not load the generator (CS9057).

## Template

Names to replace when creating a new generator from this template.

- Namespace and package ID `Template.Library`
- MSBuild property `TemplateLibraryGeneratorValue`
- Diagnostic ID prefix `TP`

Patterns not included in this template.

| Pattern | Example |
| --- | --- |
| Attribute definition injected by the generator (`RegisterPostInitializationOutput` and `AddEmbeddedAttributeDefinition()`) | BunnyTail.XamlProperty |
| Generator-only package (`IncludeBuildOutput=false` and `DevelopmentDependency=true`) | BunnyTail.EmbeddedBuildProperty |
| Default values of MSBuild properties in props (`Condition="'$(X)' == ''"`) and items passed by a target | BunnyTail.EmbeddedBuildProperty |
| Comparison with expected files | BunnyTail.DependencyInjection |
| AOT tests and benchmarks | BunnyTail.DependencyInjection, BunnyTail.MemberAccessor |
| Scanning referenced assemblies | BunnyTail.ServiceRegistration |
| Multiple generators in one assembly | BunnyTail.CommonCode, BunnyTail.XamlProperty |
