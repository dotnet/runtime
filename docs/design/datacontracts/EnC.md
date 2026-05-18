# Contract EnC

This contract reports Edit and Continue (EnC) function version numbers for jitted
managed methods. EnC function versions are 1-based monotonically increasing counters
that the runtime assigns to each `EnC`-emitted instance of a method body.

## APIs of contract

``` csharp
// Returns the latest EnC version number associated with the method identified by
// (module, methodDef). If no EnC-jitted instance exists for that method, returns
// the default EnC function version (1).
TargetNUInt GetLatestEnCVersion(TargetPointer module, uint methodDef);

// Returns the EnC version number for the specific jitted instance of the method
// identified by (module, methodDef) whose hot region starts at the given native
// code address. If no matching jitted instance exists (for example, the method
// was never EnC-edited), returns the default EnC function version (1).
TargetNUInt GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Type | Purpose |
| --- | --- | --- | --- |
| `Module` | `EnCDataList` | nuint | Head of the singly linked list of `EnCData` entries for jitted EnC-versioned methods in this module |
| `EnCData` | `AddrOfCode` | nuint | Native code start (TADDR) for the jitted instance |
| `EnCData` | `Token` | uint32 | `mdMethodDef` token of the method |
| `EnCData` | `EnCVersion` | nuint | EnC function version number for this jitted instance |
| `EnCData` | `Next` | nuint | Next entry in the module's `EnCData` list, or null |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `CorDBDefaultEnCFunctionVersion` | nuint | Default EnC function version reported for methods that have never been EnC-edited (matches `CorDB_DEFAULT_ENC_FUNCTION_VERSION` in `src/coreclr/inc/cordbpriv.h`) |

Contracts used: none

``` csharp
// Returns the address of the first EnCData entry on module's EnCDataList whose Token
// matches methodDef and (when addrOrZero is non-null) whose AddrOfCode matches
// addrOrZero. Returns TargetPointer.Null if no entry matches.
TargetPointer FindEnCDataEntry(TargetPointer module, uint methodDef,
                               TargetPointer addrOrZero)
{
    TargetPointer cur = _target.ReadPointer(module + /* Module::EnCDataList offset */);
    while (cur != TargetPointer.Null)
    {
        uint token = _target.Read<uint>(cur + /* EnCData::Token offset */);
        TargetPointer addrOfCode = _target.ReadPointer(cur + /* EnCData::AddrOfCode offset */);
        if (token == methodDef &&
            (addrOrZero == TargetPointer.Null || addrOfCode == addrOrZero))
        {
            return cur;
        }
        cur = _target.ReadPointer(cur + /* EnCData::Next offset */);
    }
    return TargetPointer.Null;
}
```

``` csharp
TargetNUInt GetLatestEnCVersion(TargetPointer module, uint methodDef)
{
    TargetPointer entry = FindEnCDataEntry(module, methodDef, TargetPointer.Null);
    if (entry == TargetPointer.Null)
        return new TargetNUInt(/* CorDBDefaultEnCFunctionVersion global */);

    return _target.ReadNUInt(entry + /* EnCData::EnCVersion offset */);
}
```

``` csharp
TargetNUInt GetEnCVersion(TargetPointer module, uint methodDef,
                          TargetCodePointer nativeCodeAddress)
{
    if (nativeCodeAddress.Value == 0)
        return new TargetNUInt(/* CorDBDefaultEnCFunctionVersion global */);

    TargetPointer entry = FindEnCDataEntry(module, methodDef,
                                           new TargetPointer(nativeCodeAddress.Value));
    if (entry == TargetPointer.Null)
        return new TargetNUInt(/* CorDBDefaultEnCFunctionVersion global */);

    return _target.ReadNUInt(entry + /* EnCData::EnCVersion offset */);
}
```

