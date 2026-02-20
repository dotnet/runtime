# PersistedAssemblyBuilder missing test scenarios

This list captures test scenarios that appear to be uncovered in `src/libraries/System.Reflection.Emit/tests/PersistedAssemblyBuilder` after reviewing the existing persisted emit tests.

## AssemblyBuilder lifecycle and validation

- `PersistedAssemblyBuilder.Save(Stream)` with a non-writable or disposed stream.
- `PersistedAssemblyBuilder.Save(Stream)` after `GenerateMetadata(...)` has already populated metadata.
- `PersistedAssemblyBuilder.GenerateMetadata(out ..., out ..., out MetadataBuilder pdbBuilder)` happy-path coverage (including a non-empty PDB metadata result when sequence points are emitted).
- Cross-over validation between overloads: calling `GenerateMetadata` overload A then overload B should still enforce one-time metadata population.
- `DefineDynamicModule` called more than once should throw `InvalidOperationException` (persisted assemblies are single-module only).

## Assembly identity metadata

- Persisting non-default `AssemblyName` metadata and validating it after load:
  - `Version`
  - `CultureName`
  - `Flags`
  - `HashAlgorithm`
  - `ContentType`
- Persisting an assembly with a non-empty public key and validating emitted assembly flags/token behavior.

## Type/model coverage gaps

- Nested type visibility matrix for persisted emit (`NestedPrivate`, `NestedAssembly`, `NestedFamANDAssem`, `NestedFamORAssem`).
- Generic variance on interface/delegate type parameters (`Covariant`, `Contravariant`) persisted and reloaded correctly.
- Delegate shape coverage beyond event helper scenarios (standalone delegate type persisted and invoked).

## Parameter metadata and calling conventions

- Method and constructor parameter default values (`DefineParameter` + default/optional metadata) round-trip after save/load.
- `CallingConventions.VarArgs` persisted metadata validation and invocation behavior.
- Parameter marshaling metadata combinations on parameters/return values for persisted methods (beyond current focused pseudo-attribute cases).

## Multi-assembly/cross-assembly references

- Persisting and loading assemblies that reference each other in a cycle (A -> B and B -> A) with emitted tokens validated.
- Persisting generic instantiations that reference type builders from another persisted assembly in nested generic arguments.
