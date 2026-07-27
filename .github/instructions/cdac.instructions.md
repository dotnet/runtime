---
applyTo: "src/native/managed/cdac/**,docs/design/datacontracts/**,src/coreclr/**/datadescriptor/**"
---

# cDAC — Folder-Specific Guidance

## Preface

The instructions under `API Review`, `DacDbi interface` and `Dump compatibility` only apply if the SDK version in global.json is a preview, release candidate or otherwise a development branch of .NET 11. If this is a development branch of .NET 12 or later, you **MUST** ignore these three sections entirely. Check `global.json` before applying them.

## API Review (.NET 11 dev branches only)

New APIs on implementations of IContract under the folder src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/** do **NOT** need to go through API review. **MUST NOT** describe such changes as "breaking" or "public API changes".

## DacDbi interface (.NET 11 dev branches only)

The DacDbi COM interface is **internal and unstable**.

- **MUST NOT** flag changes to DacDbi as breaking changes.
- **MUST NOT** warn about COM ABI compatibility, vtable ordering, or
  GUID changes on DacDbi.

## Dump compatibility (.NET 11 dev branches only)

- **MUST NOT** raise concerns about older dumps lacking new data descriptors.
- **MUST NOT** request fallback paths, conditional reads, or versioning
  shims for missing descriptors on the grounds of "supporting older dumps".

## Exception types (ALL branches)

When porting `HRESULT`-returning APIs to throw exceptions, the following mappings **MUST** be accepted:
- `ArgumentException` → for `E_INVALIDARG`
- `NullReferenceException` → for `E_POINTER`
- `InvalidCastException` → for `E_NOINTERFACE`

## Documentation updates (ALL branches)

`docs/design/datacontracts/<Name>.md` is the authoritative spec of each contract (1:1 with `Abstractions/Contracts/I<Name>.cs`). If the PR changes any of the following without also updating the doc, **MUST** flag it as an error citing the exact doc file:

- **`Abstractions/Contracts/I<Name>.cs`** — added, removed, or renamed methods, or new exposed types/enums.
- **`Contracts/Contracts/<Name>_<N>.cs`** — a new version file (needs a new `## Version N` section), or a semantic change to an existing version's algorithm. Pure refactors don't need doc updates.
- **New contract** (new `I<Name>.cs` + `<Name>_1.cs`) — needs a new `docs/design/datacontracts/<Name>.md`.
- **`src/coreclr/**/datadescriptor/**`** — added, removed, or renamed types, fields, or globals may need a matching update in every consuming contract's doc; check whichever contracts' algorithms are affected.

Do **NOT** require doc updates for pure refactors, test-only changes, bug fixes that restore documented behavior, `Legacy/**` (SOSDacImpl, DacDbi shim), or build/CI changes.


