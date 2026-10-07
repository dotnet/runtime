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
| `Array` | `m_NumComponents` | `uint32` | Number of items in the array |
| `ExternalMemoryHandle` | `Memory` | `pointer` | Pointer to the external memory tracked by this handle |
| `ExternalMemoryHandle` | `Next` | `pointer` | Pointer to the next ExternalMemoryHandle in the process-wide list |
| `ExternalMemoryHandle` | `TypeHandle` | `pointer` | Tagged type handle describing the memory as a managed local: inline value, object-reference slot, or managed-byref slot |
| `Object` | `m_pMethTab` | `pointer` | Method table for the object |
| `String` | `m_StringLength` | `uint32` | Length of the string in UTF-16 characters |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `ExternalMemoryHandles` | `pointer` | Address of the global pointer to the head of the process-wide external memory handle list (read a TargetPointer from this address to obtain the head ExternalMemoryHandle, or null if the list is empty) |
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

The `TypeHandle` describes the tracked memory exactly as a managed local of that type. Reference
types contain an object-reference slot. Value types contain inline value data. A `BYREF`
`ParamTypeDesc` describes a managed-byref slot, including `ref T` for structs and `ref object`.
The pointer in a managed-byref slot is reported as an interior root, not dereferenced and scanned
using the target's layout. Pointer and function-pointer locals contain unmanaged pointers and do
not produce GC roots.

For inline value types, the implementation reports ordinary object-reference fields described by
the type's GCDesc and
recursively finds `ELEMENT_TYPE_BYREF` fields in byref-like value types, including every element of
an inline array. GCDesc offsets are adjusted from boxed-object layout to the unboxed external-memory
layout.

``` csharp
IReadOnlyList<ExternalMemoryHandleRootData> IExternalMemoryHandles.GetRoots(bool resolveInteriorPointers)
{
    TargetPointer headPointer = // read the ExternalMemoryHandles global
    TargetPointer current = // read a pointer from headPointer

    HashSet<TargetPointer> visited = [];
    List<ExternalMemoryHandleRootData> roots = [];
    while (current != TargetPointer.Null)
    {
        if (!visited.Add(current))
            throw new InvalidOperationException();

        ExternalMemoryHandle handle = // read ExternalMemoryHandle object starting at current
        TypeHandle type = // get the RuntimeTypeSystem handle for handle.TypeHandle
        CorElementType elementType = // get the signature element type
        if (elementType == CorElementType.Byref)
        {
            // Read the pointer from handle.Memory and optionally resolve it to its containing object.
        }
        else if (type.IsValueType)
        {
            // Add GCDesc object-reference slots and recursively discovered byref-like interior roots.
        }
        else if (elementType is not (CorElementType.Ptr or CorElementType.FnPtr))
        {
            roots.Add(new ExternalMemoryHandleRootData { Address = handle.Memory });
        }
        current = handle.Next;
    }
    return roots;
}
```
