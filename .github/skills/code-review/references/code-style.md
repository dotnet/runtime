# Code Style & Formatting

_Rules for naming, formatting, language idioms, and managed/native style conventions. Part of the [code-review skill](../SKILL.md)._

- **Use well-named constants instead of magic numbers.** No raw hex or decimal constants without explanation. Don't duplicate magic constants across files.
- **Use `var` only when the type is obvious from context.** Use explicit types for casts, method returns, and async infrastructure. Never use `var` for numeric types.
- **Use PascalCase for constants; descriptive names for booleans.** All constant locals and fields use PascalCase (except interop constants matching external names). Boolean fields should be positive and descriptive (`_hasCurrent` not `valid`).
- **Name methods to accurately reflect their behavior.** Update names when behavior changes. `Get*` implies a return value; use `Print*/Display*` for void. `ThrowIf` not `ThrowExceptionIf`.
- **Prefer early return to reduce nesting.** Use early returns for short/error cases to avoid unnecessary nesting. Put the error case first, success return last.
- **Avoid `using static` and `#region` in new code.** `using static` is costly when reading code outside IDEs (e.g., GitHub review). `#region` gets out of date quickly.
- **Place local functions at method end, fields first in types.** Local functions go at the end of the containing method. Fields are the first members declared in a type.
- **Narrow warning suppression to smallest scope.** Avoid file-wide `#pragma` suppressions. Disable only around the specific line that triggers the warning.
- **Use pattern matching and `is`/`or`/`and` patterns.** Prefer `is` patterns and C# pattern matching over manual type checks and comparisons. Use named parameters for boolean arguments.
- **Do not initialize managed fields to default values (CA1805).** The CLR zero-initializes all fields in managed code. Explicit `= false`, `= 0`, `= null` is redundant. (This does not apply to native C/C++ code, where fields and locals must be explicitly initialized.)
- **Sealed classes do not need the full Dispose pattern.** A simple `Dispose()` is sufficient since no derived class can introduce a finalizer.
- **Prefer table-driven approaches over excessive case statements.** For hardware intrinsics and pattern-heavy code, use lookup tables (`AuxiliaryJitType`, `SpecialCodeGen` flags) instead of many explicit case entries.
- **Order struct fields to minimize padding.** In C/C++ struct definitions, order fields by size (pointers first) to reduce padding.
