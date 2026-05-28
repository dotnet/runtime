# Rebase Plan: Open Generics in Polymorphic Serialization PR

## Context

PR #127318 adds open generic derived type support in `[JsonDerivedType]`. The branch has 18 commits on top of main (from ~April 2026). Since then, **7 commits** landed in main touching `System.Text.Json`, most critically:

- **`b521e155` — "Add System.Text.Json union support (#128162)"**: This is the key commit. It restructured the source generator's polymorphism infrastructure — the exact same infrastructure our PR independently built. Specifically, it added:
  - `PolymorphismOptionsSpec` and `DerivedTypeSpec` model types (our PR has `PolymorphicDerivedTypeSpec` and a flat `ResolvedDerivedTypes` on `TypeGenerationSpec`)
  - `PolymorphismOptions` property on `TypeGenerationSpec` (replacing our `ResolvedDerivedTypes`)
  - `IsPolymorphic` flag back on `TypeGenerationSpec` (our PR removed it)
  - `PolymorphismOptions` property on `JsonObjectInfoValues<T>` (our PR used `DerivedTypes` of type `JsonDerivedType[]?`)
  - `TypeClassifierFactory` on `JsonObjectInfoValues<T>` (new union concept)
  - Emitter: `FormatPolymorphismOptions()` and `FormatDerivedType()` helpers
  - Parser: Reads `[JsonDerivedType]` into `DerivedTypeSpec` list, builds `PolymorphismOptionsSpec`
  - `JsonMetadataServices.Helpers.PopulatePolymorphismMetadata()` now accepts `JsonPolymorphismOptions?` + `JsonTypeClassifierFactory?` (our PR modified the old signature)
  - `JsonPolymorphismOptions.CreateFromAttributeDeclarations()` now returns `out JsonPolymorphicAttribute?`
  - `JsonPolymorphismOptions.IsEmpty` property added
  - `SetPolymorphismOptions()` internal method on `JsonTypeInfo` (our PR also added this)

## Strategy: Squash-then-Rebase

Because our PR has 18 commits with a tangled merge history and the infrastructure it built now partially exists in main, a clean approach is:

1. **Identify the pure open-generics delta** — what our PR adds ON TOP of what main now provides
2. **Reset to main**, then **re-apply** only the open-generics logic on top of main's infrastructure

### What our PR uniquely contributes (must be preserved):

#### Reflection path (`DefaultJsonTypeInfoResolver.Helpers.cs`)
- `PopulatePolymorphismFromAttributes()` → In main, this is now `PopulatePolymorphismMetadata()` which calls `CreateFromAttributeDeclarations`. Our open-generic resolution logic needs to be injected into this flow.
- `TryResolveOpenGenericDerivedType()` — the core reflection-based resolution helper
- The key logic: when iterating `[JsonDerivedType]` attributes, if `derivedType.IsGenericTypeDefinition`, resolve it via `MakeGenericType` using the base type's generic arguments (with direct-parameter-matching constraint)

#### Source generator (`Parser.cs`)
- `TryResolveOpenGenericDerivedType()` — Roslyn-based: checks if derived type is unbound generic, resolves against base type's type arguments using `INamedTypeSymbol.Construct()`
- Open generic derived types need to be resolved before being added to `DerivedTypeSpec` list
- Resolved types need to be enqueued for source generation
- SYSLIB1227 diagnostic for unresolvable open generic patterns

#### Source generator (`Emitter.cs`)
- No structural changes needed — main's emitter already emits `PolymorphismOptions` with `DerivedTypes`. Our resolved types just flow through the existing infrastructure.

#### Source generator model
- No new model types needed — main's `DerivedTypeSpec` and `PolymorphismOptionsSpec` serve the same purpose as our `PolymorphicDerivedTypeSpec` and `ResolvedDerivedTypes`

#### Runtime (`JsonPolymorphismOptions.cs`)
- Main already handles backward compat in `PopulatePolymorphismMetadata` in `JsonMetadataServices.Helpers`

#### Error handling
- `SR.OpenGenericDerivedTypeNotResolvable` string resource
- `ThrowHelper.ThrowInvalidOperationException_OpenGenericDerivedTypeNotSupported()` 
- SYSLIB1227 diagnostic descriptor

#### Tests
- All test classes/methods in `PolymorphicTests.CustomTypeHierarchies.cs`
- Source gen unit tests for SYSLIB1227

#### Docs
- `list-of-diagnostics.md` entry for SYSLIB1227

### What our PR built that main now supersedes (must be dropped):

- `PolymorphicDerivedTypeSpec.cs` model file → use main's `DerivedTypeSpec`
- `ResolvedDerivedTypes` property on `TypeGenerationSpec` → use main's `PolymorphismOptions`
- Removal of `IsPolymorphic` flag → main restored it
- `DerivedTypes` property on `JsonObjectInfoValues<T>` → main uses `PolymorphismOptions`
- `PopulatePolymorphismFromDerivedTypes()` in `JsonMetadataServices.Helpers` → main's `PopulatePolymorphismMetadata()` with its new signature
- `SetPolymorphismOptions()` internal method → main already has it
- Changes to `PopulatePolymorphismMetadata` flow → main restructured this
- `MemberAccessor` changes in Helpers.cs → unrelated main refactor
- Emitter restructuring for `PolymorphismOptions` emission → main already does this

## Execution Plan

### Phase 1: Prepare the rebase
- [ ] Hard-reset branch to `origin/main`
- [ ] Save our open-generics-specific changes as patches for reference

### Phase 2: Apply open-generic resolution to the reflection path
- [ ] Add `TryResolveOpenGenericDerivedType()` to `DefaultJsonTypeInfoResolver.Helpers.cs`
- [ ] Modify `PopulatePolymorphismMetadata()` in `DefaultJsonTypeInfoResolver.Helpers.cs` to resolve open generics when iterating `[JsonDerivedType]` attributes (inject into the existing `CreateFromAttributeDeclarations` flow or post-process)
- [ ] Add `SR.OpenGenericDerivedTypeNotResolvable` to resource strings + xlf files
- [ ] Add `ThrowHelper` method for open generic errors

### Phase 3: Apply open-generic resolution to the source generator
- [ ] Add `TryResolveOpenGenericDerivedType()` to `JsonSourceGenerator.Parser.cs`
- [ ] Modify the parser's `[JsonDerivedType]` handling to resolve open generics before adding to `DerivedTypeSpec` list
- [ ] Add SYSLIB1227 diagnostic descriptor to `DiagnosticDescriptors`
- [ ] Add SYSLIB1227 string resources + xlf
- [ ] Ensure resolved types are sorted deterministically

### Phase 4: Add tests
- [ ] Add polymorphic open-generic test types and test methods to `PolymorphicTests.CustomTypeHierarchies.cs`
- [ ] Add source gen unit tests for SYSLIB1227 diagnostic
- [ ] Add source gen baseline files if needed

### Phase 5: Documentation
- [ ] Add SYSLIB1227 to `docs/project/list-of-diagnostics.md`

### Phase 6: Validate
- [ ] Build the source generator
- [ ] Run System.Text.Json unit tests
- [ ] Run source gen unit tests
- [ ] Run source gen integration tests

## Questions for @eiriktsarpalis before starting

1. **Reflection path integration point**: Main's `PopulatePolymorphismMetadata()` now calls `CreateFromAttributeDeclarations()` which reads `[JsonDerivedType]` and creates options. The open-generic resolution needs to happen during or after this. Should I:
   - (a) Modify `CreateFromAttributeDeclarations()` to resolve open generics inline (it currently just reads `attr.DerivedType` and adds it), or
   - (b) Post-process the `JsonPolymorphismOptions.DerivedTypes` list after `CreateFromAttributeDeclarations()` returns, replacing open generic entries with resolved ones?
   
   Option (a) is cleaner but requires changing `CreateFromAttributeDeclarations` (which is in `JsonPolymorphismOptions.cs`). Option (b) keeps the change isolated to `DefaultJsonTypeInfoResolver.Helpers.cs`.

2. **Source generator**: Main's parser reads `[JsonDerivedType]` at line ~921 and calls `EnqueueType(derivedType, ...)` immediately. For open generics, we need to resolve first then enqueue the resolved type. Is it acceptable to modify this section inline, or would you prefer a separate pass?

3. **`PolymorphicDerivedTypeSpec.cs`**: Our PR added this file. Main has `DerivedTypeSpec.cs` which is identical in structure. I'll delete our file and use main's. Correct?

4. **Backward compat**: Main's `PopulatePolymorphismMetadata` in `JsonMetadataServices.Helpers` handles the case where `PolymorphismOptions` is null (older source gen) by falling back to `CreateFromAttributeDeclarations`. Open generic resolution in this fallback path — should it be supported or not? (I'd lean toward no, since older source gens wouldn't have open generic support anyway.)
