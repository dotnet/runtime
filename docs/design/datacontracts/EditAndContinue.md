# Contract EditAndContinue

This contract describes Edit-and-Continue (EnC) bookkeeping in the target process. It exposes
fields that were added to a class through metadata updates after the original type was loaded.
EnC data structures only exist in builds compiled with `FEATURE_METADATA_UPDATER` and on modules
for which Edit-and-Continue has been enabled at runtime.

## APIs of contract

```csharp
// Enumerate FieldDesc pointers for fields added to `typeHandle` via Edit-and-Continue.
// The enumeration is empty when the owning module is not EnC-enabled, when no EnC fields
// have been added for the type, or when the target build did not include EnC support.
IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields);
```

The enumeration yields each `FieldDesc*` in the same order as the native runtime maintains
the linked list (insertion order). The returned pointers are layout-compatible with regular
`FieldDesc` instances, so callers can pass them to any `IRuntimeTypeSystem` FieldDesc API
(for example `GetFieldDescType`, `GetFieldDescOffset`, `IsFieldDescStatic`,
`IsFieldDescEnCNew`).

## Version 1

### Data descriptors

| Data Descriptor Name   | Field                    | Meaning |
| ---                    | ---                      | --- |
| `Module`               | `EnCClassListCount`      | Number of `EnCEEClassData*` entries in the module's class list. Optional: only present when the target was compiled with `FEATURE_METADATA_UPDATER`. |
| `Module`               | `EnCClassListTable`      | Pointer to the backing array of `EnCEEClassData*` entries. Optional: only present when the target was compiled with `FEATURE_METADATA_UPDATER`. |
| `EnCEEClassData`       | `MethodTable`            | Pointer to the `MethodTable` whose EnC data is held by this entry. |
| `EnCEEClassData`       | `NumAddedInstanceFields` | Number of instance fields added to this class via EnC. |
| `EnCEEClassData`       | `NumAddedStaticFields`   | Number of static fields added to this class via EnC. |
| `EnCEEClassData`       | `AddedInstanceFields`    | Head of the linked list of `EnCAddedFieldElement` for added instance fields. |
| `EnCEEClassData`       | `AddedStaticFields`      | Head of the linked list of `EnCAddedFieldElement` for added static fields. |
| `EnCAddedFieldElement` | `Next`                   | Pointer to the next `EnCAddedFieldElement` in the linked list. |
| `EnCAddedFieldElement` | `FieldDesc`              | Address of the embedded `EnCFieldDesc` (layout-compatible with `FieldDesc`). |

### Required contracts

| Contract            | Use |
| ---                 | --- |
| `IRuntimeTypeSystem` | `GetModule` to locate the owning module of the given `TypeHandle`. |
| `ILoader`            | `GetModuleHandleFromModulePtr` and `GetFlags` to check whether the module has the `EditAndContinue` flag set. |

### Pseudocode

```csharp
IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields)
{
    // Only MethodTable type handles can have EnC-added fields. TypeDescs cannot.
    if (!typeHandle.IsMethodTable())
        yield break;

    TargetPointer modulePtr = RuntimeTypeSystem.GetModule(typeHandle);
    if (modulePtr == TargetPointer.Null)
        yield break;

    ModuleHandle moduleHandle = Loader.GetModuleHandleFromModulePtr(modulePtr);
    if (!Loader.GetFlags(moduleHandle).HasFlag(ModuleFlags.EditAndContinue))
        yield break;

    // The Module fields backing the EnC class list are only present when the target was
    // compiled with FEATURE_METADATA_UPDATER. If absent, there can be no EnC data.
    if (!target.HasField(DataType.Module, "EnCClassListCount") ||
        !target.HasField(DataType.Module, "EnCClassListTable"))
    {
        yield break;
    }

    uint count = target.Read<uint>(modulePtr + offsetof(Module.EnCClassListCount));
    if (count == 0)
        yield break;

    TargetPointer table = target.ReadPointer(modulePtr + offsetof(Module.EnCClassListTable));
    if (table == TargetPointer.Null)
        yield break;

    // Linear scan over the (small, typically <16-entry) array to find the entry for this MT.
    TargetPointer mtPtr = typeHandle.Address;
    TargetPointer classDataPtr = TargetPointer.Null;
    for (uint i = 0; i < count; i++)
    {
        TargetPointer entry = target.ReadPointer(table + i * sizeof(TargetPointer));
        if (entry == TargetPointer.Null)
            continue;
        EnCEEClassData candidate = target.GetOrAdd<EnCEEClassData>(entry);
        if (candidate.MethodTable == mtPtr)
        {
            classDataPtr = entry;
            break;
        }
    }
    if (classDataPtr == TargetPointer.Null)
        yield break;

    EnCEEClassData classData = target.GetOrAdd<EnCEEClassData>(classDataPtr);
    TargetPointer node = staticFields ? classData.AddedStaticFields : classData.AddedInstanceFields;
    while (node != TargetPointer.Null)
    {
        EnCAddedFieldElement element = target.GetOrAdd<EnCAddedFieldElement>(node);
        yield return element.FieldDesc;
        node = element.Next;
    }
}
```
