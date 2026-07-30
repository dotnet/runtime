# Microsoft.Diagnostics.DataContractReader.Legacy

This project contains `SOSDacImpl`, which implements the `ISOSDacInterface*` and
`IXCLRDataProcess` COM-style APIs by delegating to the cDAC contract layer. It is
standalone: it never loads or calls the legacy DAC.

## Implementing a new SOSDacImpl method

`SOSDacImpl` is standalone: it answers every API from the cDAC contract layer alone, and
returns `E_NOTIMPL` for APIs the cDAC does not implement yet. Follow this pattern:

```csharp
int ISOSDacInterface8.ExampleMethod(uint* pResult)
{
    int hr = HResults.S_OK;
    try
    {
        // 1. Validate pointer arguments inside the try block
        if (pResult is null)
            throw new ArgumentException();

        // 2. Get the relevant contract and call it
        IGC gc = _target.Contracts.GC;
        *pResult = gc.SomeMethod();
    }
    catch (System.Exception ex)
    {
        hr = ex.HResult;
    }

    return hr;
}
```

Cross-validation against the legacy DAC is *not* part of this project. It lives in the
test-only [validation shim](../mscordaccore_cdac_validation_shim/README.md), which loads
this cDAC and the legacy DAC side by side and compares them.

### Key conventions

- **HResult returns**: Methods return `int` HResult codes, not exceptions.
  Use `HResults.S_OK`, `HResults.S_FALSE`, `HResults.E_INVALIDARG`, etc.
- **Null pointer checks**: Validate output pointer arguments *inside* the try block
  and throw `ArgumentException`. The catch block converts this to an HResult code.
- **Exception handling**: Wrap all contract calls in try/catch. The catch converts
  exceptions to HResult codes via `ex.HResult`. When the native DAC has an explicit
  readability check (e.g., `ptr.IsValid()` or `DACGetMethodTableFromObjectPointer`
  returning NULL), catch `VirtualReadException` specifically and return the same
  HResult the native DAC returns (typically `E_INVALIDARG`). Avoid catching all
  exceptions and mapping to a single HRESULT, as this can mask unrelated bugs.
- **Unimplemented APIs**: return `E_NOTIMPL`. The validation shim treats that as the
  signal that an API may delegate to the legacy DAC in fallback mode.

### Child objects

Some cDAC methods create child objects (for example `ClrDataMethodInstance` or
`ClrDataFrame`). They are constructed purely from cDAC state; there is no legacy
counterpart to pair them with. The validation shim pairs each child the cDAC returns with
the child the legacy DAC returned for the same call, so a child handed back to the shim is
always unwrapped to the right side before being forwarded.

### Sized-buffer protocol

Several `ISOSDacInterface8` methods use a two-call pattern where the caller first
queries the needed buffer size, then calls again with a sufficiently large buffer:

```csharp
int GetSomeTable(uint count, Data* buffer, uint* pNeeded)
```

The protocol is:
1. Always set `*pNeeded` to the required count (if `pNeeded` is not null).
2. If `count > 0 && buffer is null`: throw `ArgumentException`.
3. If `count < needed`: return `S_FALSE` (buffer too small, but `*pNeeded` is set).
4. If `count >= needed`: populate `buffer` and return `S_OK`.

This matches the native implementation in `src/coreclr/debug/daccess/request.cpp`.

### Pointer conversions

- `TargetPointer` → `ClrDataAddress`: use `pointer.ToClrDataAddress(_target)`.
  On 32-bit targets, this **sign-extends** the value (e.g., `0xAA000000` becomes
  `0xFFFFFFFF_AA000000`). This matches native DAC behavior.
- `ClrDataAddress` → `TargetPointer`: use `address.ToTargetPointer(_target)`.

Both are defined in `ConversionExtensions.cs`.
