# Template.Library

## What is this?

Source generator that implements `[CustomMethod]` partial methods and registers them to be found by name.

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
