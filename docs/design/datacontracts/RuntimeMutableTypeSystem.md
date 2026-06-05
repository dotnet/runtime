# Contract RuntimeMutableTypeSystem

This contract exposes runtime type system information about changes that occurred after the initial type load, such as from EnC or HotReload.

## APIs of contract

```csharp
IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields);
bool IsFieldDescEnCNew(TargetPointer fieldDescPointer);
```

## Version 1

### Data descriptors

| Data Descriptor Name   | Field                    | Meaning |
| ---                    | ---                      | --- |
| `Module`               | `EnCClassList`           | Address of the embedded `CUnorderedArray` (whose `CUnorderedArrayBase` portion holds `Count`/`Table`) holding the module's `EnCEEClassData*` entries. Optional: only present when EditAndContinue is configured. |
| `UnorderedArrayBase`       | `Count`                  | Number of valid entries currently stored in the array. |
| `UnorderedArrayBase`       | `Table`                  | Pointer to the backing storage holding the array's entries. |
| `EnCEEClassData`       | `MethodTable`            | Pointer to the `MethodTable` whose EnC data is held by this entry. |
| `EnCEEClassData`       | `AddedInstanceFields`    | Head of the linked list of `EnCAddedFieldElement` for added instance fields. |
| `EnCEEClassData`       | `AddedStaticFields`      | Head of the linked list of `EnCAddedFieldElement` for added static fields. |
| `EnCAddedFieldElement` | `Next`                   | Pointer to the next `EnCAddedFieldElement` in the linked list. |
| `EnCAddedFieldElement` | `FieldDesc`              | Address of the embedded `EnCFieldDesc` (layout-compatible with `FieldDesc`). |
| `FieldDesc`            | `DWord2`                 | Packed flags/offset word containing the field's offset; the EnC-new sentinel `FieldOffsetNewEnc` is stored here for fields that have been added but do not yet have storage assigned. |

### Globals used

| Global Name           | Type   | Purpose |
| ---                   | ---    | --- |
| `FieldOffsetNewEnc`   | uint   | Sentinel offset value stored in `FieldDesc::DWord2` for added fields whose storage has not yet been allocated. |

### Required contracts

| Contract            |
| ---                 |
| `IRuntimeTypeSystem` |
| `ILoader`            |

```csharp
internal enum FieldDescFlags2 : uint
{
    OffsetMask = 0x07ffffff,
}

IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields)
{
    // get modulePtr and moduleHandle from typeHandle
    // if there is no EnC data, yield break
    TargetPointer classList = modulePtr + /* Module::EnCClassList offset */;
    uint classListCount = target.Read<uint>(classList + /* UnorderedArrayBase::Count offset */);
    TargetPointer classListTable = target.ReadPointer(classList + /* UnorderedArrayBase::Table offset */);
    TargetPointer classDataPtr = TargetPointer.Null;

    for (uint i = 0; i < classListCount; i++)
    {
        // search on EnC data for data that matches the method table
        TargetPointer entry = target.ReadPointer(classListTable + i * target.PointerSize);
        TargetPointer mt = target.ReadPointer(entry + /* EnCEEClassData::MethodTable offset */);
        if (mt == typeHandle.Address)
        {
            classDataPtr = entry;
            break;
        }
    }

    // enumerate fields that have been added
    TargetPointer node = staticFields ? target.ReadPointer(classDataPtr + /* EnCEEClassData::AddedStaticFields offset */)
        : target.ReadPointer(classDataPtr + /* EnCEEClassData::AddedInstanceFields offset */);
    while (node != TargetPointer.Null)
    {
        yield return node + /* EnCAddedFieldElement::FieldDesc offset */;
        node = target.ReadPointer(node + /* EnCAddedFieldElement::Next offset */);
    }
}

bool IsFieldDescEnCNew(TargetPointer fieldDescPointer)
{
    uint DWord2 = target.Read<uint>(fieldDescPointer + /* FieldDesc::DWord2 offset */);
    uint offset = DWord2 & (uint)FieldDescFlags2.OffsetMask;
    return offset == target.ReadGlobal<uint>("FieldOffsetNewEnc");
}
```
