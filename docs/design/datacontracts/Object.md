# Contract Object

This contract is for getting information about well-known managed objects

## APIs of contract

``` csharp
// Get the method table address for the object
TargetPointer GetMethodTableAddress(TargetPointer address);

// Get the string corresponding to a managed string object. Error if address does not represent a string.
string GetStringValue(TargetPointer address);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Object` | `m_pMethTab` | Method table for the object |
| `String` | `m_FirstChar` | First character of the string - `m_StringLength` can be used to read the full string (encoded in UTF-16) |
| `String` | `m_StringLength` | Length of the string in characters (encoded in UTF-16) |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ObjectToMethodTableUnmask` | uint8 | Bits to clear for converting to a method table address |
| `StringMethodTable` | TargetPointer | The method table for System.String |

``` csharp
TargetPointer GetMethodTableAddress(TargetPointer address)
{
    TargetPointer mt = _targetPointer.ReadPointer(address + /* Object::m_pMethTab offset */);
    return mt.Value & ~target.ReadGlobal<byte>("ObjectToMethodTableUnmask");
}

string GetStringValue(TargetPointer address)
{
    TargetPointer mt = GetMethodTableAddress(address);
    TargetPointer stringMethodTable = target.ReadPointer(target.ReadGlobalPointer("StringMethodTable"));
    if (mt != stringMethodTable)
        throw new ArgumentException("Address does not represent a string object", nameof(address));

    // Validates the method table
    _ = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mt);

    Data.String str = _target.ProcessedData.GetOrAdd<Data.String>(address);
    uint length = target.Read<uint>(address + /* String::m_StringLength offset */);
    Span<byte> span = stackalloc byte[(int)length * sizeof(char)];
    target.ReadBuffer(address + /* String::m_FirstChar offset */, span);
    return new string(MemoryMarshal.Cast<byte, char>(span));
}
```
