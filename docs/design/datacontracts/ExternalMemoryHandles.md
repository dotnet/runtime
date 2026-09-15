# ExternalMemoryHandles contract

The ExternalMemoryHandles contract scans memory registered with the runtime as containing managed
references outside the GC heap and managed stacks.

## APIs of contract

``` csharp
sealed class ExternalMemoryHandleRootData
{
    bool IsInteriorPointer { get; init; }
    TargetPointer Address { get; init; }
    TargetPointer Object { get; init; }
}
```

``` csharp
IReadOnlyList<ExternalMemoryHandleRootData> GetRoots(bool resolveInteriorPointers);
```

## Version 1

<!-- BEGIN GENERATED: usage contract=ExternalMemoryHandles version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `AppDomain` | `ExternalMemoryHandles` | `pointer` | Pointer to the head of the AppDomain's external memory handle list (SListTail<ExternalMemoryHandle>) |
| `Array` | `m_NumComponents` | `uint32` | Number of items in the array |
| `ExternalMemoryHandle` | `GCFlags` | `uint32` | Non-zero if the handle's memory holds a direct object pointer (interior/GC_CALL_INTERIOR root) rather than the address of an object reference slot |
| `ExternalMemoryHandle` | `Memory` | `pointer` | Pointer to the external memory tracked by this handle |
| `ExternalMemoryHandle` | `MethodTable` | `pointer` | Pointer to the MethodTable describing the type of the tracked memory |
| `ExternalMemoryHandle` | `Next` | `pointer` | Pointer to the next ExternalMemoryHandle in the owning AppDomain's list |
| `Object` | `m_pMethTab` | `pointer` | Method table for the object |
| `String` | `m_StringLength` | `uint32` | Length of the string in UTF-16 characters |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `AppDomain` | `pointer` | Pointer to the global application domain |
| `ObjectToMethodTableUnmask` | `uint8` | Bits to clear when converting an object header value to a method table address |

### Contracts used

| Contract Name |
| --- |
| `GC` |
| `RuntimeTypeSystem` |
<!-- END GENERATED: usage contract=ExternalMemoryHandles version=c1 -->

Each returned root identifies either an ordinary object-reference slot through `Address`, or an
interior root through `IsInteriorPointer` and `Object`. When `resolveInteriorPointers` is true,
`Object` is the containing managed object; null, invalid, or unresolvable interior pointers are
omitted. When it is false, `Object` is the raw pointer read from `Address`.

For reference-type handles, a zero `GCFlags` value produces an ordinary root at the handle's
`Memory` address and a non-zero value produces an interior root. For value-type handles, the
implementation reports ordinary object-reference fields described by the type's GCDesc and
recursively finds `ELEMENT_TYPE_BYREF` fields in byref-like value types, including every element of
an inline array. GCDesc offsets are adjusted from boxed-object layout to the unboxed external-memory
layout.

``` csharp
IReadOnlyList<ExternalMemoryHandleRootData> IExternalMemoryHandles.GetRoots(bool resolveInteriorPointers)
{
    TargetPointer appDomain = // read the AppDomain global
    if (appDomain == TargetPointer.Null)
        return [];

    AppDomain domain = // read AppDomain object starting at appDomain
    HashSet<TargetPointer> visited = [];
    List<ExternalMemoryHandleRootData> roots = [];
    TargetPointer current = domain.ExternalMemoryHandles;
    while (current != TargetPointer.Null)
    {
        if (!visited.Add(current))
            throw new InvalidOperationException();

        ExternalMemoryHandle handle = // read ExternalMemoryHandle object starting at current
        TypeHandle type = // get the RuntimeTypeSystem handle for handle.MethodTable
        if (type.IsValueType)
        {
            // Add GCDesc object-reference slots and recursively discovered byref-like interior roots.
        }
        else if (handle.GCFlags != 0)
        {
            // Read the pointer from handle.Memory and optionally resolve it to its containing object.
        }
        else
        {
            roots.Add(new ExternalMemoryHandleRootData { Address = handle.Memory });
        }
        current = handle.Next;
    }
    return roots;
}
```
