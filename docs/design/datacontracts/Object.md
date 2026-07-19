# Contract Object

This contract is for getting information about well-known managed objects

## APIs of contract

``` csharp
public enum DelegateType
{
    Unknown,
    Closed,
    Open,
}

public readonly record struct DelegateInfo(
    TargetPointer TargetObject,
    TargetCodePointer TargetMethodPtr,
    DelegateType DelegateType);

// DiagnosticIP is TargetPointer.Null when the continuation has no ResumeInfo.
public readonly record struct ContinuationInfo(
    TargetPointer Next,
    TargetPointer DiagnosticIP,
    uint State);

// Get the method table address for the object
TargetPointer GetMethodTableAddress(TargetPointer address);

// Get the string corresponding to a managed string object. Error if address does not represent a string.
string GetStringValue(TargetPointer address);

// Get the pointer to the data corresponding to a managed array object. Error if address does not represent a array.
TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);

// Get the length (in chars) and the offset from the object base to the first character
// for a managed string object. Error if address does not represent a string.
void GetStringData(TargetPointer address, out uint length, out uint offsetToFirstChar);

// Get built-in COM data for the object if available. Returns false if address does not represent a COM object using built-in COM.
bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);

// Try to get the runtime-assigned hash code for the object. Returns 0 if the runtime has not
// assigned a default hash code. This will never be 0 for objects that have been hashed.
int TryGetHashCode(TargetPointer address);

// Returns the SyncBlock address for the object, or TargetPointer.Null if no sync block is associated with it.
TargetPointer GetSyncBlockAddress(TargetPointer address);

DelegateInfo GetDelegateInfo(TargetPointer address);

// Get the linked-list / diagnostic-IP / state triple for a runtime-async continuation object.
ContinuationInfo GetContinuationInfo(TargetPointer address);
// Returns the logical size of the object in bytes (base size plus any variable-size component data).
ulong GetSize(TargetPointer address);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Array` | `m_NumComponents` | Number of items in the array |
| `Object` | `m_pMethTab` | Method table for the object |
| `String` | `m_FirstChar` | First character of the string - `m_StringLength` can be used to read the full string (encoded in UTF-16) |
| `String` | `m_StringLength` | Length of the string in characters (encoded in UTF-16) |
| `SyncTableEntry` | `SyncBlock` | `SyncBlock` corresponding to the entry |
| `ObjectHeader` | `SyncBlockValue` | Sync block value from the object header |
| `SyncBlock` | `HashCode` | Hash code stored in the sync block |
| `Delegate` | `HelperObject` | Invocation list for multicast, MethodInfo otherwise |
| `Delegate` | `Target` | Bound `this` reference for closed delegates |
| `Delegate` | `MethodPtr` | Primary method pointer |
| `Delegate` | `MethodPtrAux` | Auxiliary method pointer |
| `Delegate` | `ExtraData` | Invocation count for multicast, UnmanagedMarker for unmanaged, MethodDesc otherwise |
| `ContinuationObject` | `Next` | Pointer to the next continuation in the linked list |
| `ContinuationObject` | `ResumeInfo` | Pointer to the `ResumeInfo` for this suspension point (may be null) |
| `ContinuationObject` | `State` | State index identifying the suspension point within the resumed method |
| `AsyncResumeInfo` | `DiagnosticIP` | Native IP into the resumed method used for diagnostics (may be null) |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ArrayBoundsZero` | TargetPointer | Known value for single dimensional, zero-lower-bound array |
| `ObjectHeaderSize` | uint32 | Size of the object header (sync block and alignment) |
| `ObjectToMethodTableUnmask` | uint8 | Bits to clear for converting to a method table address |
| `StringMethodTable` | TargetPointer | The method table for System.String |
| `SyncTableEntries` | TargetPointer | The `SyncTableEntry` list |
| `SyncBlockValueToObjectOffset` | uint16 | Offset from the sync block value (in the object header) to the object itself |
| `SyncBlockIsHashOrSyncBlockIndex` | uint32 | Check bit indicating that the sync block value represents either a hash code or a sync block index rather than a thin-lock state. |
| `SyncBlockIsHashCode` | uint32 | Check bit that, when `SyncBlockIsHashOrSyncBlockIndex` is set, specifies that the remaining bits hold the hash code; when clear, the remaining bits hold the sync block index. |
| `SyncBlockHashCodeMask` | uint32 | Mask for extracting the hash code from the sync block value. |
| `SyncBlockIndexMask` | uint32 | The mask for sync block index field. |

Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `UnmanagedMarker` | nint | Sentinel value for detecting unmanaged pointer delegates. | `-1` |

Contracts used:
| Contract Name |
| --- |
| `RuntimeTypeSystem` |
| `SyncBlock` |

``` csharp
TargetPointer GetMethodTableAddress(TargetPointer address)
{
    TargetPointer mt = target.ReadPointer(address + /* Object::m_pMethTab offset */);
    return mt.Value & ~target.ReadGlobal<byte>("ObjectToMethodTableUnmask");
}

string GetStringValue(TargetPointer address)
{
    TargetPointer mt = GetMethodTableAddress(address);
    if (mt == TargetPointer.Null)
        throw new ArgumentException("Address represents a set-free object");
    TargetPointer stringMethodTable = target.ReadPointer(target.ReadGlobalPointer("StringMethodTable"));
    if (mt != stringMethodTable)
        throw new ArgumentException("Address does not represent a string object", nameof(address));

    uint length = target.Read<uint>(address + /* String::m_StringLength offset */);
    Span<byte> span = stackalloc byte[(int)length * sizeof(char)];
    target.ReadBuffer(address + /* String::m_FirstChar offset */, span);
    return new string(MemoryMarshal.Cast<byte, char>(span));
}

void GetStringData(TargetPointer address, out uint length, out uint offsetToFirstChar)
{
    TargetPointer mt = GetMethodTableAddress(address);
    if (mt == TargetPointer.Null)
        throw new ArgumentException("Address represents a set-free object");
    TargetPointer stringMethodTable = target.ReadPointer(target.ReadGlobalPointer("StringMethodTable"));
    if (mt != stringMethodTable)
        throw new ArgumentException("Address does not represent a string object", nameof(address));

    length = target.Read<uint>(address + /* String::m_StringLength offset */);
    offsetToFirstChar = /* String::m_FirstChar offset */;
}

TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds)
{
    TargetPointer mt = GetMethodTableAddress(address);
    if (mt == TargetPointer.Null)
        throw new ArgumentException("Address represents a set-free object");
    Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    TypeHandle typeHandle = rts.GetTypeHandle(mt);
    uint rank;
    if (!rts.IsArray(typeHandle, out rank))
        throw new ArgumentException("Address does not represent an array object", nameof(address));

    count = target.Read<uint>(address + /* Array::m_NumComponents offset */;

    CorElementType corType = rts.GetSignatureCorElementType(typeHandle);
    if (corType == CorElementType.Array)
    {
        // Multi-dimensional - has bounds as part of the array object
        // The object is allocated with:
        //   << fields that are part of the array type info >>
        //   int32_t bounds[rank];
        //   int32_t lowerBounds[rank];
        boundsStart = address + /* Array size */;
        lowerBounds = boundsStart + (rank * sizeof(int));
    }
    else
    {
        // Single-dimensional, zero-based - doesn't have bounds
        boundsStart = address + /* Array::m_NumComponents offset */;
        lowerBounds = target.ReadGlobalPointer("ArrayBoundsZero");
    }

    // Sync block is before `this` pointer, so substract the object header size
    ulong dataOffset = typeSystemContract.GetBaseSize(typeHandle) - target.ReadGlobal<uint>("ObjectHeaderSize");
    return address + dataOffset;
}

bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf)
{
    rcw = TargetPointer.Null;
    ccw = TargetPointer.Null;
    ccf = TargetPointer.Null;

    TargetPointer syncBlockPtr = GetSyncBlockAddress(address);
    if (syncBlockPtr == TargetPointer.Null)
        return false;

    // Delegate to the SyncBlock contract so that the interop data can also be read directly
    // from a sync block address without going through the object (e.g. during cleanup).
    return target.Contracts.SyncBlock.GetBuiltInComData(syncBlockPtr, out rcw, out ccw, out ccf);
}

int TryGetHashCode(TargetPointer address)
{
    // Read the sync block value from the ObjectHeader preceding the object
    uint syncBlockValue = target.Read<uint>(address - /* ObjectHeader size */ + /* ObjectHeader::SyncBlockValue offset */);

    if ((syncBlockValue & target.ReadGlobal<uint>("SyncBlockIsHashOrSyncBlockIndex")) == 0)
        return 0;

    if ((syncBlockValue & target.ReadGlobal<uint>("SyncBlockIsHashCode")) != 0)
    {
        // Hash code is stored inline in the sync block value
        return (int)(syncBlockValue & target.ReadGlobal<uint>("SyncBlockHashCodeMask"));
    }

    // Hash code is stored in the sync block
    TargetPointer syncBlock = GetSyncBlockAddress(address);
    if (syncBlock == TargetPointer.Null)
        return 0;

    return (int)target.Read<uint>(syncBlock + /* SyncBlock::HashCode offset */);
}

TargetPointer GetSyncBlockAddress(TargetPointer address)
{
    uint syncBlockValue = target.Read<uint>(address - target.ReadGlobal<ushort>("SyncBlockValueToObjectOffset"));

    // Check if the sync block value represents a sync block index (not a hash code)
    if ((syncBlockValue & (target.ReadGlobal<uint>("SyncBlockIsHashCode") | target.ReadGlobal<uint>("SyncBlockIsHashOrSyncBlockIndex")))
            != target.ReadGlobal<uint>("SyncBlockIsHashOrSyncBlockIndex"))
        return TargetPointer.Null;

    uint index = syncBlockValue & target.ReadGlobal<uint>("SyncBlockIndexMask");
    return target.Contracts.SyncBlock.GetSyncBlock(index);
}

DelegateInfo GetDelegateInfo(TargetPointer address)
{
    Data.Delegate del = new Data.Delegate(target, address);

    // Check for multicast and unmanaged first.
    bool isMulticast = false;
    TargetPointer helperObject = target.ReadPointer(address + /* Delegate::HelperObject offset */);
    if (helperObject != TargetPointer.Null)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        TargetPointer mt = GetMethodTableAddress(helperObject);
        Debug.Assert(mt != TargetPointer.Null);

        isMulticast = rts.IsArray(rts.GetTypeHandle(mt), out _);
    }

    const nint UnmanagedMarker = -1;
    DelegateType delegateType = DelegateType.Unknown;
    if (!isMulticast && target.ReadNInt(address + /* Delegate::ExtraData offset */) != UnmanagedMarker)
    {
        delegateType = del.MethodPtrAux == TargetCodePointer.Null ? DelegateType.Closed : DelegateType.Open;
    }

    // Pick the bound object and primary entry point based on the classification.
    // For Closed delegates the target is the bound `this` and MethodPtr is invoked on it.
    // For Open delegates MethodPtrAux is the unbound entry point; the bound object is not meaningful.
    // For Unknown do not provide any info.
    (TargetPointer targetObject, TargetCodePointer targetMethodPtr) = delegateType switch
    {
        DelegateType.Closed => (target.ReadPointer(address + /* Delegate::Target offset */), target.ReadPointer(address + /* Delegate::MethodPtr offset */)),
        DelegateType.Open   => (TargetPointer.Null, target.ReadPointer(address + /* Delegate::MethodPtrAux offset */)),
        _                   => (TargetPointer.Null, TargetCodePointer.Null),
    };

    return new DelegateInfo(targetObject, targetMethodPtr, delegateType);
}

ContinuationInfo GetContinuationInfo(TargetPointer address)
{
    TargetPointer next       = target.ReadPointer(address + /* ContinuationObject::Next offset */);
    TargetPointer resumeInfo = target.ReadPointer(address + /* ContinuationObject::ResumeInfo offset */);
    uint state               = (uint)target.Read<int>(address + /* ContinuationObject::State offset */);

    // ResumeInfo may be null
    TargetPointer diagnosticIP = resumeInfo != TargetPointer.Null
        ? target.ReadPointer(resumeInfo + /* AsyncResumeInfo::DiagnosticIP offset */)
        : TargetPointer.Null;

    return new ContinuationInfo(
        Next: next,
        DiagnosticIP: diagnosticIP,
        State: state);
}

ulong GetSize(TargetPointer address)
{
    TargetPointer mt = GetMethodTableAddress(address);
    if (mt == TargetPointer.Null)
        throw new ArgumentException("Address represents a set-free object");

    Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    TypeHandle typeHandle = rts.GetTypeHandle(mt);

    ulong size = rts.GetBaseSize(typeHandle);
    uint componentSize = rts.GetComponentSize(typeHandle);
    if (componentSize > 0)
    {
        // Variable-size object (array or string): add the component data size.
        // Both Array and String share the m_NumComponents/m_StringLength field layout.
        uint numComponents = target.Read<uint>(address + /* Array::m_NumComponents offset */);
        size += (ulong)numComponents * componentSize;
    }
    return size;
}
```
