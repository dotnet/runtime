# Contract RuntimeMutableTypeSystem

This contract exposes runtime type system information about changes that occurred after the initial type load, such as from EnC or HotReload.

## APIs of contract

```csharp
IEnumerable<TargetPointer> EnumerateAddedFieldDescs(ITypeHandle typeHandle, bool staticFields);
bool IsFieldDescEnCNew(TargetPointer fieldDescPointer);
bool DoesEnCFieldDescNeedFixup(TargetPointer encFieldDescPointer);
TargetPointer GetEnCStaticFieldDataAddress(TargetPointer encFieldDescPointer);
TargetPointer GetEnCInstanceFieldAddress(TargetPointer objectAddress, TargetPointer encFieldDescPointer);
```

## Version 1

<!-- BEGIN GENERATED: usage contract=RuntimeMutableTypeSystem version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `EnCAddedField` | `FieldData` | `ObjectHandle` | The dependent-handle pair (typed as ObjectHandle) whose primary OBJECTREF is the dependency anchor and whose secondary OBJECTREF is the System.Diagnostics.EditAndContinueHelper instance. |
| `EnCAddedField` | `FieldDesc` | `pointer` | Pointer to the EnCFieldDesc for the added instance field. |
| `EnCAddedField` | `Next` | `pointer` | Pointer to the next EnCAddedField entry in the per-object linked list hanging off the SyncBlock. |
| `EnCAddedFieldElement` | `FieldDesc` | `pointer` | Address of the embedded EnCFieldDesc (layout-compatible with FieldDesc). |
| `EnCAddedFieldElement` | `Next` | `pointer` | Pointer to the next EnCAddedFieldElement in the linked list. |
| `EnCAddedStaticField` | `FieldData` | `pointer` | Address of the first byte of static field storage on this entry. |
| `EnCEEClassData` | `AddedInstanceFields` | `pointer` | Head of the linked list of EnCAddedFieldElement for added instance fields. |
| `EnCEEClassData` | `AddedStaticFields` | `pointer` | Head of the linked list of EnCAddedFieldElement for added static fields. |
| `EnCEEClassData` | `MethodTable` | `pointer` | Pointer to the MethodTable whose EnC data is held by this entry. |
| `EnCFieldDesc` | `NeedsFixup` | `int32` | Non-zero when the EnCFieldDesc still needs fixup (i.e., it has not been fully initialized). |
| `EnCFieldDesc` | `StaticFieldData` | `pointer` | Pointer to the EnCAddedStaticField that backs this static field (NULL until storage has been allocated). |
| `EnCSyncBlockInfo` | `List` | `pointer` | Head of the linked list of EnCAddedField entries for the EnC-added instance fields associated with an object. |
| `FieldDesc` | `DWord2` | `uint32` | Packed flags and offset word containing the field's offset; the FieldOffsetNewEnc sentinel identifies an EnC-added field without assigned storage |
| `Module` | `EnCClassList` | `pointer` | Pointer to the list of classes added through Edit and Continue |
| `Object` | *(type size)* | `uint32` | Size in bytes of the fixed Object portion through its MethodTable pointer |
| `SyncBlock` | `EnCInfo` | `pointer` | Pointer to Edit-and-Continue added-field information for the object; optional when Edit and Continue is not configured |
| `SyncBlock` | `InteropInfo` | `pointer` | Pointer to optional COM interop data associated with the sync block |
| `SyncBlock` | `Lock` | `ObjectHandle` | Object handle referring to the System.Threading.Lock used for the object's monitor |
| `System.Diagnostics.EditAndContinueHelper` | `_objectReference` | `pointer` | Holds the per-field storage for an EnC-added instance field. |
| `UnorderedArrayBase` | `Count` | `uint32` | Number of valid entries currently stored in the array. |
| `UnorderedArrayBase` | `Table` | `pointer` | Pointer to the backing storage holding the array's entries. |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `FieldOffsetNewEnc` | `uint32` | Sentinel offset value stored in FieldDesc::DWord2 for added fields whose storage has not yet been allocated. |

### Contracts used

| Contract Name |
| --- |
| `GC` |
| `Loader` |
| `Object` |
| `RuntimeTypeSystem` |
<!-- END GENERATED: usage contract=RuntimeMutableTypeSystem version=c1 -->


### Required contracts


```csharp
internal enum FieldDescFlags2 : uint
{
    OffsetMask = 0x07ffffff,
}

IEnumerable<TargetPointer> EnumerateAddedFieldDescs(ITypeHandle typeHandle, bool staticFields)
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
