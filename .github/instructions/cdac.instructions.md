---
applyTo: "src/native/managed/cdac/**"
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
