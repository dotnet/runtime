# Contract ExecutionManager

This contract is for mapping a PC address to information about the
managed method corresponding to that address.


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

The execution manager uses two data structures to map the entire target address space to native executable code.
The range section map is used to partition the address space into large chunks which point to range section fragments.  Each chunk is relatively large.  If there is any executable code in the chunk, the chunk will contain one or more range section fragments that cover subsets of the chunk.  Conversely if a massive method is JITed a single range section fragment may span multiple adjacent chunks.

Within a range section fragment, a nibble map structure is used to map arbitrary IP addresses back to the start of the method (and to the code header which immediately preceeeds the entrypoint to the code).

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

### RangeSectionMap

The range section map logically partitions the entire 32-bit or 64-bit addressable space into chunks.
The map is implemented with multiple levels, where the bits of an address are used as indices into an array of pointers.  The upper levels of the map point to the next level down. At the lowest level of the map, the pointers point to the first range section fragment containing addresses in the chunk.

On 32-bit targets a 2 level map is used

| 31-24 | 23-16 | 15-0 |
|:----:|:----:|:----:|
| L2 | L1 | chunk |

That is, level 2 in the map has 256 entries pointing to level 1 maps (or null if there's nothing allocated), each level 1 map has 256 entries pointing covering a 64 KiB chunk and pointing to a linked list of range section fragments that fall within that 64 KiB chunk.

On 64-bit targets, we take advantage of the fact that the most architectures don't support a full 64-bit addressable space: arm64 supports 52 bits of addressable memoryy and x86-64 supports 57 bits.  The runtime ignores the top bits 63-57 and uses 5 levels of mapping

| 63-57 | 56-49 | 48-41 | 40-33 | 32-25 | 24-17 | 16-0 |
|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|:----:|
| unused | L5 | L4 | L3 | L2 | L1 | chunk |

That is, level 5 has 256 entires pointing to level 4 maps (or nothing if there's no
code allocated in that address range), level 4 entires point to level 3 maps and so on.  Each level 1 map has 256 entries cover a 128 KiB chunk and pointing to a linked list of range section fragments that fall within that 128 KiB chunk.

### NibbleMap

Version 1 of this contract depends on a "nibble map" data structure
that allows mapping of a code address in a contiguous subsection of
the address space to the pointer to the start of that a code sequence.
It takes advantage of the fact that the code starts are aligned and
are spaced apart to represent their addresses as a 4-bit nibble value.

