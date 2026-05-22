---
applyTo: "src/native/managed/cdac/**"
---

# cDAC — Folder-Specific Guidance

## Preface

The instructions under `API Review`, `DacDbi interface` and `Dump compatibility` only apply if the SDK version in global.json is a preview, release candidate or otherwise a development branch of .NET 11. If this is a development branch of .NET 12 or later, please ignore these instructions.

## API Review

New APIs on implementations of IContract under the folder src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/** do **NOT** need to go through API review. Changes to said APIs are not breaking changes.

## DacDbi interface

Do not warn about changing the DacDbi COM interface; this interface is not public and is completely subject to change.

## Dump compatibility

We do not care about supporting older dumps that may not have a particular datadescriptor.

## Exception types

Use these exception types:
- `ArgumentException` → for `E_INVALIDARG` checks (zero addresses, invalid enum values)
- `NullReferenceException` → for `E_POINTER` checks (null output pointers)
- `InvalidCastException` → for `E_NOINTERFACE`

## Documentation

All new APIs on implementations of IContract under the folder src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/** must have corresponding documentation in docs/design/datacontracts.