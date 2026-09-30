# Diagnostics

| ID | Severity | Description | How to fix |
|---|---|---|---|
| TP0001 | ❌ Error | `[CustomMethod]` method is not `static partial`, or has an implementation written | Declare the method as `static partial` without an implementation |
| TP0002 | ❌ Error | `[CustomMethod]` method has parameters | Remove the parameters from the method |
| TP0003 | ❌ Error | `[CustomMethod]` method does not return `void` | Change the return type to `void` |
| TP0004 | ❌ Error | A type containing the `[CustomMethod]` method is not `partial`, or is `file`-local | Declare every containing type as `partial` and not `file` |
| TP0005 | ❌ Error | `[CustomMethod]` argument is invalid (empty message, or undefined `Output` value) | Pass a non-empty message and a defined `CustomMethodOutput` value |
| TP0006 | ❌ Error | The name of a type containing `[CustomMethod]` methods differs only in case from another type in the same namespace, so the source of the type is not generated (generated file names are compared ignoring case) | Rename one of the types |
| TP0007 | ℹ️ Info | `[CustomMethod]` method is not registered for `CustomMethodProvider.FindMethod`, because the method or a containing type is `private` or `protected`, the method is generic or `static virtual`, a containing type is generic, or the method or a containing type is `[Obsolete]` as an error | Make the method and every containing type `internal` or `public` and non-generic, if the method is looked up by name |
