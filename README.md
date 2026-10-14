# template-library-with-generator

Template for a NuGet library with a source generator.

## Generators based on this template

| Repository | Package | Description |
| --- | --- | --- |
| [bunnytail-common-code](https://github.com/usausa/bunnytail-common-code) | BunnyTail.CommonCode | ToString, Equality, CompareTo, DeepClone and DelegateTo |
| [bunnytail-dependency-injection](https://github.com/usausa/bunnytail-dependency-injection) | BunnyTail.DependencyInjection | AOT-safe engine for Microsoft.Extensions.DependencyInjection |
| [bunnytail-embedded-build-property](https://github.com/usausa/bunnytail-embedded-build-property) | BunnyTail.EmbeddedBuildProperty | MSBuild property values as compile-time constants |
| [bunnytail-member-accessor](https://github.com/usausa/bunnytail-member-accessor) | BunnyTail.MemberAccessor | Reflection-free property get/set, constructor invocation and member enumeration |
| [bunnytail-service-registration](https://github.com/usausa/bunnytail-service-registration) | BunnyTail.ServiceRegistration | `IServiceCollection` registration methods from attributes |
| [bunnytail-xaml-property](https://github.com/usausa/bunnytail-xaml-property) | BunnyTail.XamlProperty.Wpf / .Maui / .Avalonia | `DependencyProperty`, `BindableProperty` and `StyledProperty` from a single attribute |

## Projects

| Project | Description |
| --- | --- |
| `Template.Library` | Package with the attributes and the runtime types, containing the generator and the props / targets |
| `Template.Library.Generator` | Source generator |
| `Template.Library.Tests` | Tests of the generated code, the diagnostics, the incremental cache and the runtime |
| `Template.Library.Tests.Options` | Tests with the MSBuild properties set in the project |
| `Develop` | Console to try the generator |

## Creating a generator

Names to replace.

- Namespace and package ID `Template.Library`
- MSBuild property `TemplateLibraryGeneratorValue`
- Diagnostic ID prefix `TP`

[README.sample.md](README.sample.md) is the README of the generator in this template, to replace this README. The diagnostics are listed in [Diagnostics.md](Diagnostics.md), not in the README.

## Patterns not included

| Pattern | Example |
| --- | --- |
| Attribute definition injected by the generator (`RegisterPostInitializationOutput` and `AddEmbeddedAttributeDefinition()`) | BunnyTail.XamlProperty |
| Generator-only package (`IncludeBuildOutput=false` and `DevelopmentDependency=true`) | BunnyTail.EmbeddedBuildProperty |
| Default values of MSBuild properties in props (`Condition="'$(X)' == ''"`) and items passed by a target | BunnyTail.EmbeddedBuildProperty |
| Comparison with expected files | BunnyTail.DependencyInjection |
| AOT tests and benchmarks | BunnyTail.DependencyInjection, BunnyTail.MemberAccessor |
| Scanning referenced assemblies | BunnyTail.ServiceRegistration |
| Multiple generators in one assembly | BunnyTail.CommonCode, BunnyTail.XamlProperty |
