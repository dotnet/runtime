# DynamicallyAccessedMembers (DAM) Code Fix
The DAM warning pattern can be annotated in a way that makes the reflection usage statically analyzable and trim-safe. Adding attributes where certain DAM warnings are displayed to users addresses these warnings and makes user code trim-safe. Previously, users were required to figure out both where and which attribute needed to be added to their code to resolve their DAM warnings, but with the introduction of this Code Fixer users can simply use the quick fixes in Visual Studio and VSCode to resolve the warning.

## Architecture
### How the analyzer produces diagnostics
Once initialized, the analyzer walks the compiler-generated AST of the program to determine coherent  use of DAM attributes and where they may be necessary. This is achieved by considering uses of annotated fields, methods, and parameters. If an inconsistent use is detected, the analyzer will trigger a warning and report a diagnostic.

### How information passes from Analyzer to Code Fix
The DAM Analyzer reports diagnostics that carry the warning ID (`descriptor`), a primary source location (`location`), and message arguments (`messageArgs`).
Data-flow diagnostics also carry the propagated DAM requirement in `properties["attributeArgument"]` and the declaration of the symbol that needs the attribute as an additional location. The declaration location is included only when its syntax tree belongs to the current compilation. This prevents diagnostics from containing source locations from a referenced compilation.
Override and interface diagnostics use the same guarded location and property format when the local implementation is missing an attribute that is present on the related contract. They do not offer fixes that remove or replace an existing attribute.

### How the Code Fix changes the file
For data-flow diagnostics, the Code Fix resolves the document containing the additional location. `SyntaxGenerator` builds the DAM attribute from `properties["attributeArgument"]`, and `SyntaxEditor` applies it to that declaration. If the analyzer did not provide a local declaration location, no fix is offered.

## Future Work
1. **Multiple Arguments:** The Code Fix does not support adding an attribute with multiple arguments (i.e. `DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields`).
2. **Merging Arguments:** When there are two differing DAM attributes on nodes that should have the same attribute, we do not provide a Code Fix. However, we could read which attributes are present, merge them, and replace the attributes in both locations.
3. **Replace Checks in `DAMCodeFixProvider.AddAttributeAsync()`:** Changes to `AddAttribute()` and `AddReturnAttribute()` were made that should be updated in the `DAMCodeFixProvider` once the new Roslyn package is published and the repo uses the new package. We can remove the `addGenericParameterAttribute` check from `DAMCodeFixProvider.AddReturnAttribute()` entirely as the API will support adding a generic parameter using `AddAttribute()`. Additionally, we can replace the lambda function in the return attribute check with `AddReturnAttribute()`.
