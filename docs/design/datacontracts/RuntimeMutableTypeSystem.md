# Contract RuntimeMutableTypeSystem

This contract exposes runtime type system information about changes that occurred after the initial type load, such as from EnC or HotReload.

## APIs of contract

```csharp
IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields);
bool IsFieldDescEnCNew(TargetPointer fieldDescPointer);
bool DoesEnCFieldDescNeedFixup(TargetPointer encFieldDescPointer);
TargetPointer GetEnCStaticFieldDataAddress(TargetPointer encFieldDescPointer);
TargetPointer GetEnCInstanceFieldAddress(TargetPointer objectAddress, TargetPointer encFieldDescPointer);
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
| `EnCFieldDesc`         | `NeedsFixup`             | Non-zero when the `EnCFieldDesc` still needs fixup (i.e., it has not been fully initialized). |
| `EnCFieldDesc`         | `StaticFieldData`        | Pointer to the `EnCAddedStaticField` that backs this static field (NULL until storage has been allocated). |
| `EnCAddedStaticField`  | `FieldDesc`              | Pointer back to the `EnCFieldDesc` that owns this storage. |
| `EnCAddedStaticField`  | `FieldData`              | Address of the first byte of static field storage on this entry. |
| `EnCAddedField`        | `Next`                   | Pointer to the next `EnCAddedField` entry in the per-object linked list hanging off the SyncBlock. |
| `EnCAddedField`        | `FieldDesc`              | Pointer to the `EnCFieldDesc` for the added instance field. |
| `EnCAddedField`        | `FieldData`              | The dependent-handle pair (typed as `ObjectHandle`) whose primary OBJECTREF is the dependency anchor and whose secondary OBJECTREF is the `System.Diagnostics.EditAndContinueHelper` instance. |
| `EnCSyncBlockInfo`     | `List`                   | Head of the linked list of `EnCAddedField` entries for the EnC-added instance fields associated with an object. |
| `SyncBlock`            | `EnCInfo`                | Pointer to the `EnCSyncBlockInfo` for this object (NULL if the object has no added EnC fields). Optional: only present when EditAndContinue is configured. |
| `FieldDesc`            | `DWord2`                 | Packed flags/offset word containing the field's offset; the EnC-new sentinel `FieldOffsetNewEnc` is stored here for fields that have been added but do not yet have storage assigned. |
| `System.Diagnostics.EditAndContinueHelper` | `_objectReference` | Holds the per-field storage for an EnC-added instance field. |

### Globals used

| Global Name           | Type   | Purpose |
| ---                   | ---    | --- |
| `FieldOffsetNewEnc`   | uint   | Sentinel offset value stored in `FieldDesc::DWord2` for added fields whose storage has not yet been allocated. |

### Required contracts

| Contract            |
| ---                 |
| `IRuntimeTypeSystem` |
| `ILoader`            |
| `IObject`            |
| `IGC`                |

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

bool DoesEnCFieldDescNeedFixup(TargetPointer encFieldDescPointer)
{
    int needsFixup = target.Read<int>(encFieldDescPointer + /* EnCFieldDesc::NeedsFixup offset */);
    return needsFixup != 0;
}

TargetPointer GetEnCStaticFieldDataAddress(TargetPointer encFieldDescPointer)
{
    TargetPointer staticFieldData = target.ReadPointer(encFieldDescPointer + /* EnCFieldDesc::StaticFieldData offset */);
    if (staticFieldData == TargetPointer.Null)
        return TargetPointer.Null;

    // [FieldAddress] on EnCAddedStaticField::FieldData returns the address of the field slot
    return staticFieldData + /* EnCAddedStaticField::FieldData offset */;
}

TargetPointer GetEnCInstanceFieldAddress(TargetPointer objectAddress, TargetPointer encFieldDescPointer)
{
    // get the SyncBlock from the object header via IObject
    TargetPointer syncBlockAddress = target.Contracts.Object.GetSyncBlockAddress(objectAddress);
    if (syncBlockAddress == TargetPointer.Null)
        return TargetPointer.Null;

    // SyncBlock::EnCInfo is an optional field; if absent the object has no EnC-added fields
    TargetPointer encInfoAddress = target.ReadPointer(syncBlockAddress + /* SyncBlock::EnCInfo offset */);
    if (encInfoAddress == TargetPointer.Null)
        return TargetPointer.Null;

    // Walk the linked list of EnCAddedField entries to find the matching FieldDesc
    TargetPointer entryPtr = target.ReadPointer(encInfoAddress + /* EnCSyncBlockInfo::List offset */);
    while (entryPtr != TargetPointer.Null)
    {
        TargetPointer entryFieldDesc = target.ReadPointer(entryPtr + /* EnCAddedField::FieldDesc offset */);
        if (entryFieldDesc == encFieldDescPointer)
        {
            // The FieldData is a dependent handle; the secondary is the EnCHelper object
            TargetPointer handleAddress = ReadObjectHandle(entryPtr + /* EnCAddedField::FieldData offset */);
            if (handleAddress == TargetPointer.Null)
                return TargetPointer.Null;

            TargetNUInt secondary = target.Contracts.GC.GetHandleExtraInfo(handleAddress);
            TargetPointer helperObjectAddress = new TargetPointer(secondary.Value);
            if (helperObjectAddress == TargetPointer.Null)
                return TargetPointer.Null;

            // [FieldAddress] on _objectReference yields the address of the OBJECTREF slot
            TargetPointer objectReferenceAddress = helperObjectAddress + /* EditAndContinueHelper::_objectReference offset */;
            TargetPointer fieldObject = target.ReadPointer(objectReferenceAddress);

            CorElementType fieldType = target.Contracts.RuntimeTypeSystem.GetFieldDescType(encFieldDescPointer);
            if (fieldType == CorElementType.ValueType)
            {
                // Value-typed field stored as a boxed object; unbox to get the data.
                if (fieldObject == TargetPointer.Null)
                    return TargetPointer.Null;
                return fieldObject + /* Object::Data offset */;
            }
            else if (fieldType == CorElementType.Class)
            {
                // The OBJECTREF slot itself is the field's value location.
                return objectReferenceAddress;
            }
            else
            {
                // Primitive stored in a 1-element array. Return the address of the first element.
                if (fieldObject == TargetPointer.Null)
                    return TargetPointer.Null;
                return target.Contracts.Object.GetArrayData(fieldObject, out _, out _, out _);
            }
        }
        entryPtr = target.ReadPointer(entryPtr + /* EnCAddedField::Next offset */);
    }

    return TargetPointer.Null;
}
```
