# Rebase Plan: Open Generics in Polymorphic Serialization PR #127318

## Context

PR #127318 adds open generic derived type support in `[JsonDerivedType]`. The branch has 18 commits on top of a stale main. Since the PR was last active, the **union support PR (#128162)** landed in main, restructuring the source generator's polymorphism infrastructure — the exact same infrastructure our PR independently built.

## Design Decisions (from reviewer feedback)

1. **Two methods for polymorphism resolution** (AOT boundary):
   - **Legacy/shared method** (`JsonMetadataServices.Helpers.PopulatePolymorphismMetadata`): AOT-friendly, no open generic support. Used by source-gen path and as backward-compat fallback for older source generators. Calls `CreateFromAttributeDeclarations` which reads `[JsonDerivedType]` as-is (open generic type defs pass through unresolved → will fail at runtime validation in `PolymorphicTypeResolver`).
   - **Reflection-only method** (`DefaultJsonTypeInfoResolver.Helpers.PopulatePolymorphismMetadata`): Has `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`. Adds open-generic resolution via `Type.MakeGenericType`. Lives only in the reflection resolver, never in AOT/source-gen runtime paths.

2. **Source generator**: Resolve open generics **inline** (direct computation) in the parser's `[JsonDerivedType]` handling block — no separate pass.

3. **Model files**: Delete our `PolymorphicDerivedTypeSpec.cs`, use main's `DerivedTypeSpec.cs` and `PolymorphismOptionsSpec`.

4. **Backward-compat fallback** in `JsonMetadataServices.Helpers.PopulatePolymorphismMetadata`: Does NOT resolve open generics (preserves AOT compatibility).

5. **SYSLIB1227 conflict**: Main now uses SYSLIB1227 for union case classification. Our open-generic diagnostic needs a new ID — likely **SYSLIB1229** (within the reserved STJ source-gen range 1220-1229).

## Architecture After Rebase

```
Source Generator (compile time)
  └─ Parser: TryResolveOpenGenericDerivedType() [Roslyn-based, INamedTypeSymbol.Construct()]
       → resolves Derived<> to Derived<int> using base type's type args
       → adds resolved DerivedTypeSpec to PolymorphismOptionsSpec
       → reports SYSLIB1229 if unresolvable
  └─ Emitter: FormatPolymorphismOptions() [already in main, no changes needed]
       → emits PolymorphismOptions with resolved derived types

Source-gen runtime path (AOT-safe, shared)
  └─ JsonMetadataServices.Helpers.PopulatePolymorphismMetadata(typeInfo, options, classifierFactory)
       → if options is null: legacy fallback → CreateFromAttributeDeclarations (NO open generic resolution)
       → if options.IsEmpty: no polymorphism
       → otherwise: use source-gen-provided options (already resolved at compile time)

Reflection runtime path (NOT AOT-safe)
  └─ DefaultJsonTypeInfoResolver.Helpers.PopulatePolymorphismMetadata(typeInfo)
       → calls CreateFromAttributeDeclarations to get base options
       → post-processes DerivedTypes list: resolves open generic entries via MakeGenericType
       → throws InvalidOperationException for unresolvable patterns
```

## Execution Plan

### Phase 0: Prepare the rebase
- [ ] Save current branch state as a reference tag/stash
- [ ] Hard-reset branch to `origin/main`

### Phase 1: Reflection path — open generic resolution
- [ ] Add `TryResolveOpenGenericDerivedType(Type derivedTypeDefinition, Type baseTypeDefinition, Type[] baseTypeArgs, out Type? resolvedType)` as a private static method in `DefaultJsonTypeInfoResolver.Helpers.cs`
  - Direct parameter matching constraint: derived type's generic params must exactly match base type's in number and position
  - Walks base classes and interfaces of the derived type definition to find a match
  - Returns false for unsupported patterns (wrapped args, reordered params, arity mismatch)
- [ ] Modify `DefaultJsonTypeInfoResolver.Helpers.PopulatePolymorphismMetadata()` to post-process derived types:
  - After `CreateFromAttributeDeclarations` returns options, iterate `options.DerivedTypes`
  - For any entry where `derivedType.DerivedType.IsGenericTypeDefinition`, resolve it
  - Replace the entry with the resolved closed type
  - Throw `InvalidOperationException` if base type is not generic or resolution fails
- [ ] Add `SR.OpenGenericDerivedTypeNotResolvable` string resource to `src/libraries/System.Text.Json/src/Resources/Strings.resx`
- [ ] Add corresponding `ThrowHelper` method
- [ ] Update xlf files (run `dotnet build /t:UpdateXlf` or similar)

### Phase 2: Source generator — compile-time open generic resolution
- [ ] Add `TryResolveOpenGenericDerivedType(INamedTypeSymbol unboundDerived, ITypeSymbol baseType, out INamedTypeSymbol? resolvedType)` as a private method in `JsonSourceGenerator.Parser.cs`
  - Check `baseType` is `INamedTypeSymbol { IsGenericType: true }`
  - Extract base type's type arguments
  - Verify derived type definition's generic params match base type's in number and position
  - Use `derivedDefinition.Construct(resolvedArgs)` to create the closed type
  - Return false for unsupported patterns
- [ ] Modify the parser's `[JsonDerivedType]` handling block (around line 921 in main):
  - When `derivedType` is `INamedTypeSymbol { IsUnboundGenericType: true }` (or similar), call `TryResolveOpenGenericDerivedType`
  - On success: use the resolved type for `EnqueueType` and `DerivedTypeSpec`
  - On failure: report diagnostic, skip this derived type entry
- [ ] Add diagnostic descriptor `OpenGenericDerivedTypeNotResolvable` with ID **SYSLIB1229** to `DiagnosticDescriptors`
- [ ] Add SYSLIB1229 string resources to source gen `Strings.resx` + update xlf files
- [ ] Ensure derived types remain sorted deterministically (main already sorts via `PolymorphismOptionsSpec`)

### Phase 3: Tests
- [ ] Add test types to `PolymorphicTests.CustomTypeHierarchies.cs`:
  - `Foo<T>` / `Bar<T> : Foo<T>` — basic open generic with string discriminator
  - `Foo<T>` / `Bar<T> : Foo<T>` — with int discriminator
  - Multi-param: `Base<T1,T2>` / `Derived<T1,T2> : Base<T1,T2>`
  - Interface: `IBase<T>` / `Impl<T> : IBase<T>`
  - Interface hierarchy: `IDerived<T> : IBase<T>` / `Impl<T> : IDerived<T>`
  - Nested type
  - Properties on both layers for round-trip validation
  - **Rejection tests**: wrapped type args (`Derived<T> : Base<List<T>>`), reordered params (`Derived<T1,T2> : Base<T2,T1>`), arity mismatch (`Derived<T> : Base<T, int>`), non-generic base
  - Programmatic API test (using `JsonPolymorphismOptions` directly)
  - Deserialization validation for all discriminator-bearing tests
- [ ] Add source gen unit tests for SYSLIB1229 diagnostic in `JsonSourceGeneratorDiagnosticsTests.cs`
- [ ] Add source gen baseline files if required

### Phase 4: Documentation
- [ ] Add SYSLIB1229 to `docs/project/list-of-diagnostics.md`

### Phase 5: Validate
- [ ] Build the source generator project
- [ ] Run `System.Text.Json.Tests` 
- [ ] Run `System.Text.Json.SourceGeneration.Unit.Tests`
- [ ] Run `System.Text.Json.SourceGeneration.Tests` (integration)

## Key Files to Modify

| File | Change |
|------|--------|
| `gen/JsonSourceGenerator.Parser.cs` | Add `TryResolveOpenGenericDerivedType`, modify `[JsonDerivedType]` handling |
| `gen/JsonSourceGenerator.DiagnosticDescriptors.cs` | Add SYSLIB1229 descriptor |
| `gen/Resources/Strings.resx` + xlf | Add SYSLIB1229 strings |
| `src/.../DefaultJsonTypeInfoResolver.Helpers.cs` | Add `TryResolveOpenGenericDerivedType`, modify `PopulatePolymorphismMetadata` |
| `src/Resources/Strings.resx` + xlf | Add `OpenGenericDerivedTypeNotResolvable` |
| `src/.../ThrowHelper.cs` | Add throw method |
| `tests/.../PolymorphicTests.CustomTypeHierarchies.cs` | Add test types and methods |
| `tests/.../JsonSourceGeneratorDiagnosticsTests.cs` | Add SYSLIB1229 tests |
| `docs/project/list-of-diagnostics.md` | Add SYSLIB1229 |

## Files NOT modified (handled by main)

| File | Reason |
|------|--------|
| `gen/Model/DerivedTypeSpec.cs` | Already exists in main |
| `gen/Model/PolymorphismOptionsSpec.cs` | Already exists in main |
| `gen/Model/TypeGenerationSpec.cs` | Main's `PolymorphismOptions` property sufficient |
| `gen/JsonSourceGenerator.Emitter.cs` | Main's `FormatPolymorphismOptions`/`FormatDerivedType` sufficient |
| `src/.../JsonObjectInfoValuesOfT.cs` | Main's `PolymorphismOptions` property sufficient |
| `src/.../JsonMetadataServices.Helpers.cs` | Legacy fallback stays AOT-safe, no changes |
| `src/.../JsonPolymorphismOptions.cs` | `CreateFromAttributeDeclarations` unchanged |
| `src/.../JsonTypeInfo.cs` | `SetPolymorphismOptions` already exists in main |

## Files to DELETE (from our PR, superseded by main)

| File | Reason |
|------|--------|
| `gen/Model/PolymorphicDerivedTypeSpec.cs` | Replaced by main's `DerivedTypeSpec.cs` |

