# Contract CDacMetadata

This contract exposes properties that describe the target platform

## APIs of contract

```csharp
    // Returns a pointer to a structure describing platform specific precode stubs properties
    TargetPointer GetPrecodeMachineDescriptor();
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| CDacMetadata | PrecodeMachineDescriptor | precode stub-related platform specific properties |


Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| CDacMetadata | pointer | address of the `CDacMetadata` data |

Contracts used:
| Contract Name |
| --- |
| *none* |

```csharp
TargetPointer GetPrecodeMachineDescriptor()
{
    TargetPointer metadataAddress = _target.ReadGlobalPointer("CDacMetadata");
    return metadataAddress + /* CDacMetadata::PrecodeMachineDescriptor */
}
```
