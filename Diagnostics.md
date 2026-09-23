# Diagnostics

| ID | Severity | Description | How to fix |
|---|---|---|---|
| TP0001 | ❌ Error | `[CustomMethod]` method is not `static partial` | Declare the method as `static partial` |
| TP0002 | ❌ Error | `[CustomMethod]` method has parameters | Remove the parameters from the method |
| TP0003 | ❌ Error | `[CustomMethod]` method does not return `void` | Change the return type to `void` |
| TP0004 | ❌ Error | A type containing the `[CustomMethod]` method is not `partial`, or is `file`-local | Declare every containing type as `partial` and not `file` |
| TP0005 | ❌ Error | `[CustomMethod]` argument is invalid (empty message, or undefined `Output` value) | Pass a non-empty message and a defined `CustomMethodOutput` value |
| TP0006 | ℹ️ Info | `[CustomMethod]` method is not registered for `CustomMethodProvider.FindMethod`, because the method or a containing type is `private` or `protected`, or a containing type is generic | Make the method and every containing type `internal` or `public` and non-generic, if the method is looked up by name |
