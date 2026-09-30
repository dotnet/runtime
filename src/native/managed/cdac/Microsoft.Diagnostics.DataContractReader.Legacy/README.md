# Microsoft.Diagnostics.DataContractReader.Legacy

This project contains `SOSDacImpl`, which implements the `ISOSDacInterface*` and
`IXCLRDataProcess` COM-style APIs by delegating to the cDAC contract layer.

## Implementing a new SOSDacImpl method

When a method currently delegates to `_legacyImpl` (returning `E_NOTIMPL` when null),
replace it with a cDAC implementation following this pattern:

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
- **Null pointer checks**: Validate output pointer arguments *inside* the try block
  and throw `ArgumentException`. The catch block converts this to an HResult code.
- **Exception handling**: Wrap all contract calls in try/catch. The catch converts
  exceptions to HResult codes via `ex.HResult`. When the native DAC has an explicit
  readability check (e.g., `ptr.IsValid()` or `DACGetMethodTableFromObjectPointer`
  returning NULL), catch `VirtualReadException` specifically and return the same
  HResult the native DAC returns (typically `E_INVALIDARG`). Avoid catching all
  exceptions and mapping to a single HRESULT, as this can mask unrelated bugs.
- **Debug cross-validation**: In `#if DEBUG`, call the legacy implementation (if
  available) and assert the results match. This catches discrepancies during testing.

### Legacy comparison state

cDAC APIs do not fall back to the legacy DAC. APIs without a cDAC implementation
return `E_NOTIMPL`.

Some stateful validation paths create a corresponding legacy enumeration or child
object and keep it in sync with the cDAC object. These legacy objects are used only
to compare results; their failures and return values must not determine cDAC
behavior. Legacy interfaces and comparison state are initialized only in Debug builds;
Release builds retain the fallback interface only as an opaque lifetime root. The
assertions that compare HResults and values belong inside `#if DEBUG`.

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
