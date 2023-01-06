# DynamicallyAccessedMembers (DAM) Code Fix
The DAM warning pattern can be annotated in a way that makes the reflection usage statically analyzable and trim-safe. Adding attributes where certain DAM warnings are displayed to users addresses these warnings and makes user code trim-safe. Previously, users were required to figure out both where and which attribute needed to be added to their code to resolve their DAM warnings, but with the introduction of this Code Fixer users can simply use the quick fixes in Visual Studio and VSCode to resolve the warning.

## Architecture
### How the analyzer produces diagnostics
Once initialized, the analyzer walks the compiler-generated AST of the program to determine coherent  use of DAM attributes and where they may be necessary. This is achieved by considering uses of annotated fields, methods, and parameters. If an inconsistent use is detected, the analyzer will trigger a warning and report a diagnostic.

### How information passes from Analyzer to Code Fix
The DAM Analyzer reports diagnostics that contain information about the specific warning, including the warning ID (`descriptor`), the location of the warning (`location`), the location where a code fix may be applied (`additionalLocations`), the argument to be included in the DAM attribute to be applied (`properties`), and any additional arguments (`messageArgs`). These diagnostics are then unpacked by the Code Fixer.

### How the Code Fix changes the file
The Code Fix uses `SyntaxGenerator` to create the DAM attribute to add from the DAM argument passed through the `properties` dictionary.  The Syntax Node that the attribute is applied to is found from the `additionalLocations` of the diagnostic. `SyntaxEditor` applies the attribute to the location specified and update the original document.

## Future Work
1. **Multiple Arguments:** The Code Fix does not support the case where there are multiple arguments present on a node (i.e. `DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)`).
2. **Merging Arguments:** When there are two differing DAM attributes on nodes that should have the same attribute, we do not provide a Code Fix. However, we could read which attributes are present, merge them, and replace the attributes in both locations.
3. **Replace Checks in `DAMCodeFixProvider.AddAttributeAsync()`:** Changes to `AddAttribute()` and `AddReturnAttribute()` were made that should be updated in the `DAMCodeFixProvider` once the new Roslyn package is published and the repo uses the new package. We can remove the `addGenericParameterAttribute` check from `DAMCodeFixProvider.AddReturnAttribute()` entirely as the API will support adding a generic parameter using `AddAttribute()`. Additionally, we can replace the lambda function in the return attribute check with `AddReturnAttribute()`.
