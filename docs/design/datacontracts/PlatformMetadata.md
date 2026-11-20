# Contract PlatformMetadata

This contract exposes properties that describe the target platform

## APIs of contract

```csharp
    internal enum CodePointerFlags : byte
    {
        // Set if the target process is executing on arm32
        HasArm32ThumbBit = 0x1,
    }
    // Returns a pointer to a structure describing platform-specific precode stubs properties
    TargetPointer GetPrecodeMachineDescriptor();

    // Returns flags describing the behavior of code pointers
    CodePointerFlags GetCodePointerFlags();

    // Returns bits to clear from pointer-authenticated return addresses
    ulong GetPointerAuthenticationMask();
```

## Version 1

`PointerAuthenticationMask` is optional; if absent, the mask is zero and return addresses are left unchanged.

<!-- BEGIN GENERATED: usage contract=PlatformMetadata version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `PlatformMetadata` | `CodePointerFlags` | `uint8` | fields describing the behavior of target code pointers |
| `PlatformMetadata` | `PointerAuthenticationMask` | `uint64` | Bits to clear from pointer-authenticated return addresses |
| `PlatformMetadata` | `PrecodeMachineDescriptor` | `pointer` | precode stub-related platform specific properties |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `PlatformMetadata` | `pointer` | address of the PlatformMetadata data |

### Contracts used

_None._
<!-- END GENERATED: usage contract=PlatformMetadata version=c1 -->


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
