# Contract PlatformMetadata

This contract exposes properties that describe the target platform

## APIs of contract

```csharp
    internal enum CodePointerFlags : byte
    {
        // Set if the target process is executing on arm32
        HasArm32ThumbBit = 0x1,
        // Set if arm64e pointer authentication is used in the target process
        HasArm64PtrAuth = 0x2,
    }
    // Returns a pointer to a structure describing platform-specific precode stubs properties
    TargetPointer GetPrecodeMachineDescriptor();

    // Returns flags describing the behavior of code pointers
    CodePointerFlags GetCodePointerFlags();
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| PlatformMetadata | PrecodeMachineDescriptor | precode stub-related platform specific properties |
| PlatformMetadata | CodePointerFlags | fields describing the behavior of target code pointers |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| PlatformMetadata | pointer | address of the `PlatformMetadata` data |

Contracts used:
| Contract Name |
| --- |
| *none* |

```csharp
TargetPointer GetPrecodeMachineDescriptor()
{
    TargetPointer metadataAddress = _target.ReadGlobalPointer("PlatformMetadata");
    return metadataAddress + /* PlatformMetadata::PrecodeMachineDescriptor */
}

CodePointerFlags GetCodePointerFlags()
{
    TargetPointer metadataAddress = _target.ReadGlobalPointer("PlatformMetadata");
    return (CodePointerFlags)_target.Read<byte>(metadataAddress  + /*PlatformMetadata::CodePointerFlags*/);
}
```
