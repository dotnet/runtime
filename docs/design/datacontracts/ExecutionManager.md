# Contract ExecutionManager

This contract


## APIs of contract

```csharp
internal struct EECodeInfoHandle
{
    // no public constructor
    public readonly TargetPointer Address;
    internal EECodeInfoHandle(TargetPointer address) => Address = address;
}
```

```csharp
    // Collect execution engine info for a code block that includes the given instruction pointer.
    // Return a handle for the information, or null if an owning code block cannot be found.
    EECodeInfoHandle? GetEECodeInfoHandle(TargetCodePointer ip);
    // Get the method descriptor corresponding to the given code block
    TargetPointer GetMethodDesc(EECodeInfoHandle codeInfoHandle) => throw new NotImplementedException();
    // Get the instruction pointer address of the start of the code block
    TargetCodePointer GetStartAddress(EECodeInfoHandle codeInfoHandle) => throw new NotImplementedException();
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| RangeSectionMap | TopLevelData | pointer to the outermost RangeSection |
| RangeSectionFragment| ? | ? |
| RangeSection | ? | ? |
| RealCodeHeader | ? | ? |
| HeapList | ? | ? |



Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| ExecutionManagerCodeRangeMapAddress | TargetPointer | Pointer to the global RangeSectionMap
| StubCodeBlockLast | uint8 | Maximum sentinel code header value indentifying a stub code block

Contracts used:
| Contract Name |
| --- |

```csharp
```

**TODO** Methods

### NibbleMap

**TODO**
