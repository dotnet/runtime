# Contract Signature

This contract encapsulates the `VASigCookie` data structure pushed onto the stack for vararg
calls in the cDAC. It is used by the DAC/DBI `GetVarArgSig` API to recover the location of
the first argument and the raw vararg signature blob for a vararg call frame.

## APIs of contract

```csharp
// Returns the address of the first argument relative to the cookie pointer location.
TargetPointer GetVarArgArgsBase(TargetPointer vaSigCookieAddr);

// Returns the address and length of the raw vararg signature blob held by the cookie.
void GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength);
```

`vaSigCookieAddr` is the target address of the `VASigCookie*` slot pushed onto the stack by
the vararg call site (i.e. it is a pointer to a pointer to the cookie). Both APIs throw if
the address is null or the cookie pointer it references is null.

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `VASigCookie` | `SizeOfArgs` | Total size in bytes of the pushed argument list. Used on x86 to locate the args base. |
| `VASigCookie` | `SignaturePointer` | Target address of the raw vararg signature blob. |
| `VASigCookie` | `SignatureLength` | Length in bytes of the raw vararg signature blob. |

Global variables used: none.

Contracts used:
| Contract Name |
| --- |
| RuntimeInfo |

### `GetVarArgArgsBase`

```csharp
TargetPointer GetVarArgArgsBase(TargetPointer vaSigCookieAddr)
{
    TargetPointer vaSigCookie = _target.ReadPointer(vaSigCookieAddr);
    VASigCookie cookie = _target.ProcessedData.GetOrAdd<VASigCookie>(vaSigCookie);

    // On x86 the args are pushed below the cookie pointer (stack grows down on the args walk);
    // on every other platform the first argument follows the cookie pointer in memory
    // (stack grows up on the args walk).
    if (RuntimeInfo.GetTargetArchitecture() == X86)
        return vaSigCookieAddr + cookie.SizeOfArgs;
    return vaSigCookieAddr + sizeof(TargetPointer);
}
```

### `GetVarArgSignature`

```csharp
void GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength)
{
    TargetPointer vaSigCookie = _target.ReadPointer(vaSigCookieAddr);
    VASigCookie cookie = _target.ProcessedData.GetOrAdd<VASigCookie>(vaSigCookie);

    signatureAddress = cookie.SignaturePointer;
    signatureLength = cookie.SignatureLength;
}
```
