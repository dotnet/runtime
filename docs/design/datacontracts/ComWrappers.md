# Contract ComWrappers

This contract is for getting information related to COM wrappers.

## APIs of contract

``` csharp
// Get the address of the external COM object
TargetPointer GetComWrappersIdentity(TargetPointer rcw);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `NativeObjectWrapperObject` | `ExternalComObject` | Address of the external COM object |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |

Contracts used:
| Contract Name |
| --- |


``` csharp
public TargetPointer GetComWrappersIdentity(TargetPointer address)
{
    return _target.ReadPointer(address + /* NativeObjectWrapperObject::ExternalComObject offset */);
}
```
