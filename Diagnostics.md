# Diagnostics

| ID | Severity | Description | How to fix |
|---|---|---|---|
| TP0001 | ⚠️ Warning | `[CustomMethod]` method is not `static partial` | Declare the method as `static partial` |
| TP0002 | ⚠️ Warning | `[CustomMethod]` method has parameters | Remove the parameters from the method |
| TP0003 | ⚠️ Warning | `[CustomMethod]` method does not return `void` | Change the return type to `void` |
| TP0004 | ⚠️ Warning | A type containing the `[CustomMethod]` method is not `partial`, or is `file`-local | Declare every containing type as `partial` and not `file` |
| TP0005 | ⚠️ Warning | `[CustomMethod]` argument is invalid (empty message, or undefined `Output` value) | Pass a non-empty message and a defined `CustomMethodOutput` value |
