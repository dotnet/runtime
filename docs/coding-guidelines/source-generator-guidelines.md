Recommended reading to better understand source generators,
[Roslyn Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md).

[Project guidance](./project-guidelines.md#directory-layout)

[Packaging guidance](./libraries-packaging.md#analyzers--source-generators)

## Source Generator Best Practices

### DOs

- **DO** generate code that looks as if a developer would write it manually.
- **DO** emit strings rather than using the Roslyn Syntax API for better performance.
- **DO** use consistent indentation and formatting in generated code.
- **DO** make generators incremental whenever possible using the [`IIncrementalGenerator`](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.iincrementalgenerator) interface.
- **DO** cache intermediate results to avoid redundant computation.
- **DO** consider the impact on build time and optimize accordingly.
- **DO** report clear diagnostics when errors occur during generation.
- **DO** provide actionable error messages that guide the user to correct issues.

### DON'Ts

- **DON'T** use the Roslyn Syntax API for emitting source code.
- **DON'T** perform expensive operations during the generation process unless absolutely necessary.
- **DON'T** emit code that introduces runtime dependencies not explicitly referenced by the project.
- **DON'T** access the file system or network during generation unless absolutely necessary.
- **DON'T** emit code that would trigger compiler warnings in normal usage scenarios.
