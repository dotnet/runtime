# Contract ComWrappers

This contract is for getting information related to COM wrappers.

## APIs of contract

``` csharp
// Get the address of the external COM object
TargetPointer GetComWrappersIdentity(TargetPointer rcw);
// Given a ccw pointer, return the managed object wrapper
public TargetPointer GetManagedObjectWrapperFromCCW(TargetPointer ccw);
// Given a managed object wrapper, return the comwrappers pointer
public TargetPointer GetComWrappersObjectFromMOW(TargetPointer mow);
// Given a managed object wrapper, return its reference count
public long GetMOWReferenceCount(TargetPointer mow);
// Get the COM identity (IUnknown pointer) for a managed object wrapper
TargetPointer GetIdentityForMOW(TargetPointer mow);
// Get all managed object wrappers for a given managed object
List<TargetPointer> GetMOWs(TargetPointer obj, out bool hasMOWTable);
// Determine if a pointer represents a ComWrappers RCW
public bool IsComWrappersRCW(TargetPointer rcw);
// Get the ComWrappers RCW for a given managed object, or null if none exists
TargetPointer GetComWrappersRCWForObject(TargetPointer obj);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `NativeObjectWrapperObject` | `ExternalComObject` | Address of the external COM object |
| `ManagedObjectWrapperHolderObject` | `WrappedObject` | Address of the wrapped object |
| `ManagedObjectWrapperHolderObject` | `Wrapper` | Pointer to the `ManagedObjectWrapperLayout` |
| `ManagedObjectWrapperLayout` | `RefCount` | Reference count of the managed object wrapper |
| `ManagedObjectWrapperLayout` | `Flags` | `CreateComInterfaceFlagsEx` flags |
| `ManagedObjectWrapperLayout` | `UserDefinedCount` | Number of user-defined COM interface entries |
| `ManagedObjectWrapperLayout` | `UserDefined` | Pointer to array of `ComInterfaceEntry` |
| `ManagedObjectWrapperLayout` | `Dispatches` | Pointer to the dispatch section (`InternalComInterfaceDispatch` array) |
| `ComInterfaceEntry` | `IID` | The interface GUID |
| `InternalComInterfaceDispatch` | `Entries` | Start of vtable entry pointers within the dispatch block |
| `ComWrappersVtablePtrs` | `Size` | Size of vtable pointers array |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ComWrappersVtablePtrs` | TargetPointer | Pointer to struct containing ComWrappers-related function pointers |
| `DispatchThisPtrMask` | TargetPointer | Used to mask low bits of CCW pointer to the nearest valid address from which to read a managed object wrapper |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `NativeObjectWrapperNamespace` | string | Namespace of System.Runtime.InteropServices.ComWrappers+NativeObjectWrapper | `System.Runtime.InteropServices` |
| `NativeObjectWrapperName` | string | Name of System.Runtime.InteropServices.ComWrappers+NativeObjectWrapper | `ComWrappers+NativeObjectWrapper` |
| `CallerDefinedIUnknown` | int | Flag bit for `CreateComInterfaceFlagsEx` indicating caller-defined IUnknown | `1` |
| `IID_IUnknown` | Guid | The IID for IUnknown | `00000000-0000-0000-C000-000000000046` |

Contracts used:
| Contract Name |
| --- |
| `Object` |
| `RuntimeTypeSystem` |
| `Loader` |
| `ConditionalWeakTable` |


``` csharp

public TargetPointer GetComWrappersIdentity(TargetPointer address)
{
    return _target.ReadPointer(address + /* NativeObjectWrapperObject::ExternalComObject offset */);
}

private bool GetComWrappersCCWVTableQIAddress(TargetPointer ccw, out TargetPointer vtable, out TargetPointer qiAddress)
{
    vtable = TargetPointer.Null;
    qiAddress = TargetPointer.Null;

    // read two levels of indirection from the ccw to get the code pointer into qiAddress
    // if read fails, return false

    qiAddress = CodePointerUtils.AddressFromCodePointer(qiAddress, _target);
    return true;
}

private bool IsComWrappersCCW(TargetPointer ccw)
{
    if (!GetComWrappersCCWVTableQIAddress(ccw, out _, out TargetPointer qiAddress))
        return false;

    TargetPointer comWrappersVtablePtrs = _target.ReadGlobalPointer("ComWrappersVtablePtrs");

    return /* qiAddress matches any entry in ComWrappersVtablePtrs */ ;
}

public TargetPointer GetManagedObjectWrapperFromCCW(TargetPointer ccw)
{
    if (!IsComWrappersCCW(ccw))
        return TargetPointer.Null;
    try
    {
        return _target.ReadPointer(ccw & _target.ReadGlobalPointer("DispatchThisPtrMask"));
    }
    catch (VirtualReadException)
    {
        return TargetPointer.Null;
    }
}

public TargetPointer GetComWrappersObjectFromMOW(TargetPointer mow)
{
    TargetPointer mowHolderObject = /* read two layers of indirection from MOW */;
    return mowHolderObject + /* ManagedObjectWrapperHolderObject::WrappedObject offset */;
}

public long GetMOWReferenceCount(TargetPointer mow)
{
    return target.Read<long>(mow + /* ManagedObjectWrapperLayout::RefCount offset */);
}

private TargetPointer IndexIntoDispatchSection(int index, TargetPointer dispatches)
{
    // InternalComInterfaceDispatch contains a _thisPtr followed by EntriesPerThisPtr vtable entries.
    // EntriesPerThisPtr = (sizeof(InternalComInterfaceDispatch) / pointerSize) - 1
    uint dispatchSize = /* InternalComInterfaceDispatch size */;
    uint entriesPerThisPtr = (dispatchSize / target.PointerSize) - 1;

    TargetPointer dispatch = dispatches + (index / entriesPerThisPtr) * dispatchSize;
    TargetPointer entries = dispatch + /* InternalComInterfaceDispatch::Entries offset */;

    return entries + (index % entriesPerThisPtr) * target.PointerSize;
}

public TargetPointer GetIdentityForMOW(TargetPointer mow)
{
    // Read the ManagedObjectWrapperLayout fields
    int flags = target.Read<int>(mow + /* ManagedObjectWrapperLayout::Flags offset */);
    int userDefinedCount = target.Read<int>(mow + /* ManagedObjectWrapperLayout::UserDefinedCount offset */);
    TargetPointer userDefined = target.ReadPointer(mow + /* ManagedObjectWrapperLayout::UserDefined offset */);
    TargetPointer dispatches = target.ReadPointer(mow + /* ManagedObjectWrapperLayout::Dispatches offset */);

    if ((flags & CallerDefinedIUnknown) == 0)
    {
        // Standard IUnknown is at the runtime-defined slot (right after user-defined entries)
        return IndexIntoDispatchSection(userDefinedCount, dispatches);
    }

    // Search user-defined entries for IID_IUnknown
    for (int i = 0; i < userDefinedCount; i++)
    {
        Guid iid = /* read GUID at userDefined + i * ComInterfaceEntry size + ComInterfaceEntry::IID offset */;
        if (iid == IID_IUnknown)
            return IndexIntoDispatchSection(i, dispatches);
    }

    return TargetPointer.Null;
}

public List<TargetPointer> GetMOWs(TargetPointer obj, out bool hasMOWTable)
{
    // Look up the static field ComWrappers.s_allManagedObjectWrapperTable via RuntimeTypeSystem
    // Use the ConditionalWeakTable contract to find the List<ManagedObjectWrapperHolderObject> value
    // Iterate the list and return each holder's Wrapper pointer (the ManagedObjectWrapperLayout address)
}

public bool IsComWrappersRCW(TargetPointer rcw)
{
    // Get method table from rcw using Object contract GetMethodTableAddress
    // Find module from the system assembly
    // Then use RuntimeTypeSystem contract to look up type handle by name/namespace hardcoded in contract
    // Then compare the rcw method table with the method table found by name/namespace/module
}

public TargetPointer GetComWrappersRCWForObject(TargetPointer obj)
{
    // Look up the static field ComWrappers.s_nativeObjectWrapperTable via RuntimeTypeSystem
    // Use the ConditionalWeakTable contract to find the value associated with obj
    // If found, return the NativeObjectWrapper reference (tagged with low bit by caller)
    TargetPointer cwtTable = /* address of ComWrappers.s_nativeObjectWrapperTable static field */;
    if (cwtTable == TargetPointer.Null)
        return TargetPointer.Null;

    target.Contracts.ConditionalWeakTable.TryGetValue(cwtTable, obj, out TargetPointer rcw);

    return rcw;
}
```
