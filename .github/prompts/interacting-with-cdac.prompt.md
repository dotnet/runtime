---
mode: 'agent'
tools: ['githubRepo', 'search/codebase', 'runCommands/terminalLastCommand']
description: 'Understand, extend, and update the cDAC (contract-based Data Access Component) data contracts and descriptors.'
---

#### 1 — Goal

Work with the cDAC to let diagnostic tooling (debuggers, profilers, post-mortem analyzers) read .NET runtime process memory reliably without a matching native DAC. Tasks include: inspecting contracts, adding/modifying data structure layouts or globals, versioning contracts, regenerating descriptors, and validating managed reader behavior.

#### 2 — Conceptual Overview

The cDAC publishes a versioned "contract descriptor" containing:
* Type layouts (selected fields + optional determinate size) for runtime internal structures
* Global values (integral constants or pointers, possibly indirect via an auxiliary pointer table)
* A set of compatible algorithmic contracts (name → version number)
* Optional sub‑descriptors (deltas) and baseline identifier

Diagnostic tooling ingests this descriptor JSON plus auxiliary pointer data and uses managed contracts (C#) over a generic memory access abstraction to query runtime state.

Terminology:
* Data Descriptor: JSON dictionary describing types + globals (+ optional baseline reference). See `docs/design/datacontracts/data_descriptor.md`.
* Contract Descriptor: Native exported symbol `DotNetRuntimeContractDescriptor` (struct + pointer to compact JSON + pointer table). See `docs/design/datacontracts/contract-descriptor.md`.
* Algorithmic Contracts: C# APIs that interpret layouts/globals to return higher‑level information. Documented per contract in `docs/design/datacontracts/*.md`.
* Baseline Descriptor: Well-known checked-in JSON under `docs/design/datacontracts/data/*`; runtime in-memory descriptor augments/overrides it.

#### 3 — File & Directory Map

Native (descriptor construction):
* `src/coreclr/vm/datadescriptor/datadescriptor.inc` — macro script declaring baseline id, types, fields, sizes, globals, sub-descriptors, and `CDAC_GLOBAL_CONTRACT(<Name>, <Version>)` records.
* `src/coreclr/vm/datadescriptor/datadescriptor.h` + `CMakeLists.txt` — build integration, macro definitions.

Managed (reader & contracts):
* `src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader/ContractDescriptorParser.cs` — parses compact JSON (size sigil `!`, field arrays, globals with direct/indirect forms).
* `src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader/ContractDescriptorTarget.cs` — creates target abstraction from one or more descriptors; merges baseline + in-memory; exposes reading APIs.
* `src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader/CachingContractRegistry.cs` & `...Abstractions/ContractRegistry.cs` — on-demand instantiation of contract implementations by version.
* `src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/` — concrete contract implementations & helpers (e.g. runtime type system, execution manager, GC, etc.).
* `src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Abstractions/` — interfaces (`IThread`, `IRuntimeTypeSystem`, etc.).
* `src/native/managed/cdac/mscordaccore_universal/` — host / glue for universal managed DAC loading.

Documentation:
* Overview & logical model: `docs/design/datacontracts/datacontracts_design.md`
* Data descriptor physical/logical spec: `data_descriptor.md`
* Contract descriptor struct & symbol: `contract-descriptor.md`
* Individual contract specs (Algorithmic docs): e.g. `Thread.md`, `GC.md`, `RuntimeTypeSystem.md`, `CodeVersions.md`, `ExecutionManager.md`, `Object.md` etc.

#### 4 — Data & JSON Shapes (Compact Format Recap)

Types section (compact):
```jsonc
"types": {
  "Thread": { "Id": 32, "State": 0, "LinkNext": 128, "!": 0 }, // size optional; size omitted => indeterminate
  "GCHandle": { "!": 8, "Value": [0, "pointer"] } // explicit size & field typed via tuple
}
```
Rules:
* Field value `N` = offset only (type supplied from baseline or inferred later)
* Field value `[N, "TypeName"]` supplies offset + type
* Size key `"!"` supplies determinate size (omit or use pattern for indeterminate)

Globals section (compact):
```jsonc
"globals": {
  "FEATURE_COMINTEROP": 0,            // direct numeric
  "s_pThreadStore": [ 0 ],            // indirect pointer via pointer table slot 0
  "RuntimeID": "windows-x64",        // direct string
  "GCInfoVersion": [1234, "uint32"]  // typed direct
}
```
Global value encodings:
* Direct numeric: `number | "123" | "0xABC"` (parsed both as numeric and string when numeric-like)
* Indirect: `[ slotIndex ]` (pointer sized value stored in auxiliary pointer array)
* Typed forms: `[ value, "TypeName" ]` or `[[ slotIndex ], "TypeName" ]`

Contracts section:
```jsonc
"contracts": { "Thread": 1, "GC": 1, "PlatformMetadata": 1 }
```
Version is an integer; higher does not imply newer—just different. Multiple versions of same contract MUST NOT appear simultaneously.

#### 5 — Versioning Rules & Strategy

* Choose a new integer version that's globally unique in main branch for that contract name.
* Preserve semantic meaning across versions: same conceptual API surface; unsupported data in a version may throw (e.g., missing field => `NotSupportedException`).
* Document all versions in the same contract markdown file (`docs/design/datacontracts/<Contract>.md`) with API surface + version-specific behavior sections.
* Never repurpose an old number for incompatible semantics.
* Bumping version required when: field offsets change meaning, field removed that existing algorithm relies on, algorithmic behavior changes beyond optional result differences.

#### 6 — Typical Change Workflow

1. Identify need (new runtime field, global, algorithm change, platform addition).
2. Update native descriptor:
   * Add or modify `CDAC_TYPE_FIELD`, `CDAC_TYPE_SIZE`, `CDAC_GLOBAL`, `CDAC_GLOBAL_POINTER`, `CDAC_GLOBAL_CONTRACT` entries in `datadescriptor.inc`.
   * Maintain grouping & ordering by subsystem (Thread, Loader, RuntimeTypeSystem, etc.)—keep diff localized.
3. If adding a contract version:
   * Append `CDAC_GLOBAL_CONTRACT(<Name>, <NewVersion>)` (ensure uniqueness; remove prior only if superseding EXACT semantics—normally keep older for compatibility).
4. Update docs:
   * Extend existing `docs/design/datacontracts/<Contract>.md` with new Version section (copy prior semantics; highlight differences and rationale).
   * If new data structures/globals, update related design doc (e.g. `RuntimeTypeSystem.md`) if required.
5. (Optional) Create/Adjust baseline descriptor JSON under `docs/design/datacontracts/data/<rid path>.json` if baseline must now include new fields; otherwise rely on in-memory descriptor overrides.
6. Build / Validate:
    * Fast iteration (Managed cDAC only – skips full runtime rebuild):
       * Windows PowerShell:
          ```powershell
          ./build.cmd tools.cdac -c Debug
          ```
       * bash (Linux/macOS):
          ```bash
          ./build.sh tools.cdac -c Debug
          ```
7. Validate symbol & JSON:
   * Ensure exported symbol `DotNetRuntimeContractDescriptor` present (e.g., by dump or debugger symbol listing).
   * Inspect generated in-memory JSON (if tooling exists) or manually verify offsets against struct definitions.
8. Managed side adjustments:
   * If new primitive or interpretation needed: update parser or target translation logic (`ContractDescriptorParser.cs`, `ContractDescriptorTarget.cs`).
   * Add new contract factory mapping in `CachingContractRegistry.cs` if creating a brand‑new contract interface.
   * Implement new or version‑specific logic inside contract implementation class (pattern: switch on `version`).
9. Add or update tests in `src/native/managed/cdac/tests/` verifying:
   * Parsing of new fields/globals
   * Contract methods behave under multiple versions (mock descriptor instances)
10. Run focused tests:
```powershell
# Run only DataContractReader tests
cd src/native/managed/cdac/tests
dotnet build /t:test
```
11. Definition of Done checklist (see section 11) & commit.

#### 7 — Adding / Modifying Type Layout

Use macros in `datadescriptor.inc`:
* `CDAC_TYPE_BEGIN(TypeName)` / `CDAC_TYPE_END(TypeName)` define a type.
* `CDAC_TYPE_SIZE(sizeof(...))` sets determinate size.
* `CDAC_TYPE_INDETERMINATE(TypeName)` marks indeterminate size.
* `CDAC_TYPE_FIELD(TypeName, /*<native-type-comment>*/, FieldName, <offsetExpr>)` — `offsetExpr` can be `offsetof()` or precomputed constant (`cdac_data<Struct>::Field`).

Guidelines:
* Prefer comments inside macro for original native field type for clarity.
* Keep same logical field name used by managed contracts to reduce translation friction.
* For platform-conditional fields wrap in appropriate preprocessor guards (example: conditional compilation for specific TARGET_* architectures); document platform variance in contract markdown.

#### 8 — Adding / Modifying Globals

Macros:
* `CDAC_GLOBAL(Name, <ctype>, <value>)` direct numeric (compile-time constant).
* `CDAC_GLOBAL_POINTER(Name, &SymbolOrExpr)` pointer value (stored indirect in auxiliary table; JSON entry will be `[ slotIndex ]`).
* `CDAC_GLOBAL_STRING(Name, literal)` string constant.
* `CDAC_GLOBAL_SUB_DESCRIPTOR(Name, &SubDescriptor)` references sub-descriptor (enables layering of additional types/globals).
* `CDAC_GLOBAL_CONTRACT(Name, Version)` declare contract compatibility entry.

Ensure pointer globals reference stable process addresses (e.g., singletons), not transient stack memory.

#### 9 — Adding a New Contract (Algorithmic)

1. Define interface in `...Abstractions/Contracts/<Name>.cs` (follow existing naming pattern `I<Contract>`).
2. Implement factory + versions in `...DataContractReader.Contracts/<Name>Contract.cs` (or similar). Use a `switch` or strategy pattern on version.
3. Register factory in `CachingContractRegistry.cs` map.
4. Add documentation file `docs/design/datacontracts/<Name>.md` with sections: description, data structures, APIs, Versions (each version code/pseudocode). Reuse style from existing files (see `Thread.md`).
5. Add `CDAC_GLOBAL_CONTRACT(Name, Version)` in `datadescriptor.inc`.
6. Tests: create mocks of descriptor JSON for multiple versions and verify contract method behavior.

#### 10 — Testing & Validation Tips

* Construct synthetic compact JSON in tests to simulate descriptor changes without rebuilding native runtime.
* Use `ContractDescriptorParser.ParseCompact` directly on `ReadOnlySpan<byte>` test blobs.
* Validate new numeric global parsing (hex/decimal/string forms) and indirect pointer resolution logic.
* For pointer fields ensure alignment expectations via `ContractDescriptorTarget.IsAlignedToPointerSize` helpers.
* Negative flows: missing field => algorithm should throw; accessing unsupported version-specific API => `NotSupportedException`.

#### 11 — Definition of Done Checklist

- [ ] `datadescriptor.inc` updated (types/globals/contracts) with correct conditional compilation
- [ ] Documentation file(s) updated with new version or contract section
- [ ] (If needed) Baseline JSON added/updated under `docs/design/datacontracts/data/`
- [ ] Managed contract interface & factory registered (`CachingContractRegistry.cs`) (if new contract)
- [ ] Version gating logic implemented in contract class(es)
- [ ] Parser / target adjustments (only if new physical format element introduced)
- [ ] Tests updated/added and pass (`dotnet build /t:test` in cdac tests folder)
- [ ] Full build succeeds (`build.cmd clr+libs` on Windows)
- [ ] Symbol `DotNetRuntimeContractDescriptor` exports inspected (optional manual verification)
- [ ] No duplicate contract versions; version numbers globally unique

#### 12 — Common Pitfalls

| Pitfall | Avoidance |
|---------|-----------|
| Forgetting to document new version | Always append new Version section in existing contract markdown |
| Reusing old version number | Choose new unique integer; never repurpose |
| Adding field only in code not in descriptor | Update both native macros & docs; managed contract expects descriptor presence |
| Accessing field before offset populated | Ensure baseline has `"unknown"` only until in-memory override supplies offset; final logical descriptor must have concrete offsets |
| Pointer globals mis-specified | Use `CDAC_GLOBAL_POINTER` not raw numeric constant for addresses that vary per process |
| Missing factory registration for new contract | Add entry to `_factories` in `CachingContractRegistry.cs` |

#### 13 — Minimal Example: Adding a Field to `Thread`

1. Add native macro line near related fields:
```c
CDAC_TYPE_FIELD(Thread, /*pointer*/, NewDebugField, cdac_data<Thread>::NewDebugField)
```
2. Rebuild runtime: `build.cmd clr+libs`
3. Document updated layout in `Thread.md` (update Version section if semantic change requires version bump).
4. If contract algorithm uses the field conditionally, guard by checking presence in type info (`TryGetField("NewDebugField", out FieldInfo f)` pattern) or version number.
5. Add test constructing descriptor JSON that includes the new offset and verify contract method reads it.

#### 14 — Reference Style (Contract Doc Template)

Reuse template from `datacontracts_design.md`:
```markdown
# Contract `<Name>`

Description...

## Data structures defined by contract
```csharp
record struct DataStruct(...);
```

## Apis of contract
```csharp
ReturnType ApiName(TargetPointer p);
```

## Version X
(Explain differences)
```

#### 15 — Cleanup & Backwards Compatibility

* Keep older versions listed until intentionally deprecated; tooling may still rely on them for older dumps.
* Avoid removing fields—prefer leaving them unused; if removal unavoidable, bump version and document missing semantics.
* Do not rename existing field names without strong reason; prefer adding new field with clearer name and deprecating old logically.

#### 16 — Quick Commands (Windows PowerShell)
```powershell
# Build coreclr + libs
./build.cmd clr+libs

# Run cDAC managed tests
cd src/native/managed/cdac/tests
dotnet build /t:test
```

#### 17 — Signal for Further Assistance

If descriptor format changes (new JSON shape element) update:
* Parser converters (`ContractDescriptorParser` — add property handling or converter logic)
* Target integration (`ContractDescriptorTarget` — mapping to DataType/Globals)
* Docs: extend `data_descriptor.md` + `contract-descriptor.md`

Return here before edits to ensure consistent multi-layer updates.

#### 18 — Self-Check Before Commit

Ensure every changed file directly relates to descriptor/contract update; avoid incidental refactors. Verify tests and build succeeded locally.

