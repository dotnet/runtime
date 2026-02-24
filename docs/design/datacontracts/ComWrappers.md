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
// Determine if a pointer represents a ComWrappers RCW
public bool IsComWrappersRCW(TargetPointer rcw);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `NativeObjectWrapperObject` | `ExternalComObject` | Address of the external COM object ||
| `ManagedObjectWrapperHolderObject` | `WrappedObject` | Address of the wrapped object |
| `ManagedObjectWrapperLayout` | `RefCount` | Reference count of the managed object wrapper |
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

Contracts used:
| Contract Name |
| --- |
| `Object` |
| `RuntimeTypeSystem` |
| `Loader` |


``` csharp

private enum Flags
{
    IsHandleWeak = 0x4,
}

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

public bool IsComWrappersRCW(TargetPointer rcw)
{
    // Get method table from rcw using Object contract GetMethodTableAddress
    // Find module from the system assembly
    // Then use RuntimeTypeSystem contract to look up type handle by name/namespace hardcoded in contract
    // Then compare the rcw method table with the method table found by name/namespace/module
}
```
