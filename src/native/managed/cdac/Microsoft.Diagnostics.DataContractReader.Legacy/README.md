# Microsoft.Diagnostics.DataContractReader.Legacy

This project contains `SOSDacImpl`, which implements the `ISOSDacInterface*` and
`IXCLRDataProcess` COM-style APIs by delegating to the cDAC contract layer.

## Implementing a new SOSDacImpl method

When a method currently delegates to `_legacyImpl` (returning `E_NOTIMPL` when null),
replace it with a cDAC implementation following this pattern:

```csharp
int ISOSDacInterface8.ExampleMethod(uint* pResult)
{
    // 1. Validate pointer arguments before the try block
    if (pResult == null)
        return HResults.E_INVALIDARG;

    int hr = HResults.S_OK;
    try
    {
        // 2. Get the relevant contract and call it
        IGC gc = _target.Contracts.GC;
        *pResult = gc.SomeMethod();
    }
    catch (System.Exception ex)
    {
        hr = ex.HResult;
    }

    // 3. Cross-validate with legacy DAC in debug builds
#if DEBUG
    if (_legacyImpl8 is not null)
    {
        uint resultLocal;
        int hrLocal = _legacyImpl8.ExampleMethod(&resultLocal);
        Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        if (hr == HResults.S_OK)
        {
            Debug.Assert(*pResult == resultLocal);
        }
    }
#endif
    return hr;
}
```

### Key conventions

- **HResult returns**: Methods return `int` HResult codes, not exceptions.
  Use `HResults.S_OK`, `HResults.S_FALSE`, `HResults.E_INVALIDARG`, etc.
- **Null pointer checks**: Validate output pointer arguments *before* the try block
  and return `E_INVALIDARG`. This matches the native DAC behavior.
- **Exception handling**: Wrap all contract calls in try/catch. The catch converts
  exceptions to HResult codes via `ex.HResult`.
- **Debug cross-validation**: In `#if DEBUG`, call the legacy implementation (if
  available) and assert the results match. This catches discrepancies during testing.

### Sized-buffer protocol

Several `ISOSDacInterface8` methods use a two-call pattern where the caller first
queries the needed buffer size, then calls again with a sufficiently large buffer:

```csharp
int GetSomeTable(uint count, Data* buffer, uint* pNeeded)
```

The protocol is:
1. Always set `*pNeeded` to the required count (if `pNeeded` is not null).
2. If `count > 0 && buffer == null`: return `E_INVALIDARG`.
3. If `count < needed`: return `S_FALSE` (buffer too small, but `*pNeeded` is set).
4. If `count >= needed`: populate `buffer` and return `S_OK`.

This matches the native implementation in `src/coreclr/debug/daccess/request.cpp`.

### Pointer conversions

- `TargetPointer` → `ClrDataAddress`: use `pointer.ToClrDataAddress(_target)`.
  On 32-bit targets, this **sign-extends** the value (e.g., `0xAA000000` becomes
  `0xFFFFFFFF_AA000000`). This matches native DAC behavior.
- `ClrDataAddress` → `TargetPointer`: use `address.ToTargetPointer(_target)`.

Both are defined in `ConversionExtensions.cs`.
