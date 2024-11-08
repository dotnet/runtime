# Contract ExecutionManager

This contract is for mapping a PC address to information about the
managed method corresponding to that address.


## APIs of contract

```csharp
struct CodeBlockHandle
{
    public readonly TargetPointer Address;
    // no public constructor
    internal CodeBlockHandle(TargetPointer address) => Address = address;
}
```

```csharp
    // Collect execution engine info for a code block that includes the given instruction pointer.
    // Return a handle for the information, or null if an owning code block cannot be found.
    CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip);
    // Get the method descriptor corresponding to the given code block
    TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle);
    // Get the instruction pointer address of the start of the code block
    TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle);
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

The bulk of the work is done by the `GetCodeBlockHandle` API that maps a code pointer to information about the containing jitted method.

```csharp
    private CodeBlock? GetCodeBlock(TargetCodePointer jittedCodeAddress)
    {
        RangeSection range = RangeSection.Find(_topRangeSectionMap, jittedCodeAddress);
        if (range.Data == null)
        {
            return null;
        }
        JitManager jitManager = GetJitManager(range.Data);
        if (jitManager.GetMethodInfo(range, jittedCodeAddress, out CodeBlock? info))
        {
            return info;
        }
        else
        {
            return null;
        }
    }
    CodeBlockHandle? IExecutionManager.GetCodeBlockHandle(TargetCodePointer ip)
    {
        TargetPointer key = ip.AsTargetPointer;
        if (/*cache*/.ContainsKey(key))
        {
            return new CodeBlockHandle(key);
        }
        CodeBlock? info = GetCodeBlock(ip);
        if (info == null || !info.Valid)
        {
            return null;
        }
        /*cache*/.TryAdd(key, info);
        return new CodeBlockHandle(key);
    }
```

Here `RangeSection.Find` implements the range section lookup, summarized below.

There are two `JitManager`s: the "EE JitManager" for jitted code and "R2R JitManager" for ReadyToRun code.

The EE JitManager `GetMethodInfo` implements the nibble map lookup, summarized below, followed by returning the `RealCodeHeader` data:

```csharp
    bool GetMethodInfo(RangeSection rangeSection, TargetCodePointer jittedCodeAddress, [NotNullWhen(true)] out CodeBlock? info)
    {
        TargetPointer start = FindMethodCode(rangeSection, jittedCodeAddress); // nibble map lookup
        if (start == TargetPointer.Null)
        {
            return false;
        }
        TargetNUInt relativeOffset = jittedCodeAddress - start;
        int codeHeaderOffset = Target.PointerSize;
        TargetPointer codeHeaderIndirect = start - codeHeaderOffset;
        if (RangeSection.IsStubCodeBlock(Target, codeHeaderIndirect))
        {
            return false;
        }
        TargetPointer codeHeaderAddress = Target.ReadPointer(codeHeaderIndirect);
        Data.RealCodeHeader realCodeHeader = Target.ProcessedData.GetOrAdd<Data.RealCodeHeader>(codeHeaderAddress);
        info = new CodeBlock(jittedCodeAddress, codeHeaderOffset, relativeOffset, realCodeHeader, rangeSection.Data!.JitManager);
        return true;
    }
```

The `CodeBlock` encapsulates the `RealCodeHeader` data from the target runtime together with the start of the jitted method

```csharp
class CodeBlock
{
    private readonly int _codeHeaderOffset;

    public TargetCodePointer StartAddress { get; }
    // note: this is the address of the pointer to the "real code header", you need to
    // dereference it to get the address of _codeHeaderData
    public TargetPointer CodeHeaderAddress => StartAddress - _codeHeaderOffset;
    private Data.RealCodeHeader _codeHeaderData;
    public TargetPointer JitManagerAddress { get; }
    public TargetNUInt RelativeOffset { get; }
    public CodeBlock(TargetCodePointer startAddress, int codeHeaderOffset, TargetNUInt relativeOffset, Data.RealCodeHeader codeHeaderData, TargetPointer jitManagerAddress)
    {
        _codeHeaderOffset = codeHeaderOffset;
        StartAddress = startAddress;
        _codeHeaderData = codeHeaderData;
        RelativeOffset = relativeOffset;
        JitManagerAddress = jitManagerAddress;
    }

    public TargetPointer MethodDescAddress => _codeHeaderData.MethodDesc;
    public bool Valid => JitManagerAddress != TargetPointer.Null;
}
```

The remaining contract APIs extract fields of the `CodeBlock`:

```csharp
    TargetPointer IExecutionManager.GetMethodDesc(CodeBlockHandle codeInfoHandle)
    {
        /* find EECodeBlock info for codeInfoHandle.Address*/
        return info.MethodDescAddress;
    }

    TargetCodePointer IExecutionManager.GetStartAddress(CodeBlockHandle codeInfoHandle)
    {
        /* find EECodeBlock info for codeInfoHandle.Address*/
        return info.StartAddress;
    }
```

### RangeSectionMap

The range section map logically partitions the entire 32-bit or 64-bit addressable space into chunks.
The map is implemented with multiple levels, where the bits of an address are used as indices into an array of pointers.  The upper levels of the map point to the next level down. At the lowest level of the map, the pointers point to the first range section fragment containing addresses in the chunk.

On 32-bit targets a 2 level map is used

| 31-24 | 23-16 | 15-0 |
|:----:|:----:|:----:|
| L2 | L1 | chunk |

That is, level 2 in the map has 256 entries pointing to level 1 maps (or null if there's nothing allocated), each level 1 map has 256 entries covering a 64 KiB chunk and pointing to a linked list of range section fragments that fall within that 64 KiB chunk.

On 64-bit targets, we take advantage of the fact that most architectures don't support a full 64-bit addressable space: arm64 supports 52 bits of addressable memory and x86-64 supports 57 bits.  The runtime ignores the top bits 63-57 and uses 5 levels of mapping

| 63-57 | 56-49 | 48-41 | 40-33 | 32-25 | 24-17 | 16-0 |
|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|:----:|
| unused | L5 | L4 | L3 | L2 | L1 | chunk |

That is, level 5 has 256 entires pointing to level 4 maps (or nothing if there's no
code allocated in that address range), level 4 entires point to level 3 maps and so on.  Each level 1 map has 256 entries covering a 128 KiB chunk and pointing to a linked list of range section fragments that fall within that 128 KiB chunk.

### NibbleMap

Version 1 of this contract depends on a "nibble map" data structure
that allows mapping of a code address in a contiguous subsection of
the address space to the pointer to the start of that a code sequence.
It takes advantage of the fact that the code starts are aligned and
are spaced apart to represent their addresses as a 4-bit nibble value.

Given a contiguous region of memory in which we lay out a collection of non-overlapping code blocks that are
not too small (so that two adjacent ones aren't too close together) and  where the start of each code block is aligned on some power of 2 and preceeded by a code header,
we can break up the whole memory space into buckets of a fixed size (32-bytes in the current implementation), where
each bucket either has a code block or not.
Thinking of each code block address as a hex number, we can view it as: [index, offset]
where each index gives us a bucket and the offset gives us the position of the header within the bucket.
In the current implementation code must be 4 byte aligned therefore there are 8 possible offsets in a bucket.
These are encoded as values 1-8 in the 4-bit nibble, with 0 reserved to mark the places in the map where a method doesn't start.

To find the start of a method given an address we first convert it into a bucket index (giving the map unit)
and an offset which we can then turn into the index of the nibble that covers that address.
If the nibble is non-zero, we have the start of a method and it is near the given address.
If the nibble is zero, we have to search backward first through the current map unit, and then through previous map
units until we find a non-zero nibble.

For example (all code addresses are relative to some unspecified base):

Suppose there is code starting at address 304 (0x130)

* Then the map index will be 304 / 32 = 9 and the byte offset will be 304 % 32 = 16
* Because addresses are 4-byte aligned, the nibble value will be 1 + 16 / 4 = 5  (we reserve 0 to mean no method).
* So the map unit containing index 9 will contain the value 0x5 << 24 (the map index 9 means we want the second nibble in the second map unit, and we number the nibbles starting from the most significant) , or 0x05000000


Now suppose we do a lookup for address 306 (0x132)
* The map index will be 306 / 32 = 9 and the byte offset will be 306 % 32 = 18
* The nibble value will be 1 + 18 / 4 = 5
* To do the lookup, we will load the map unit with index 9 (so the second 32-bit unit in the map) and get the value 0x05000000
* We will then shift to focus on the nibble with map index 9 (which again has nibble shift 24), so
 the map unit will be 0x00000005 and we will get the nibble value 5.
* Therefore we know that there is a method start at map index 9, nibble value 5.
* The map index corresponds to an offset of 288 bytes and the nibble value 5 corresponds to an offset of (5 - 1) * 4 = 16 bytes
* So the method starts at offset 288 + 16 = 304, which is the address we were looking for.

Now suppose we do a lookup for address 302 (0x12E)

* The map index will be 302 / 32 = 9 and the byte offset will be 302 % 32 = 14
* The nibble value will be 1 + 14 / 4 = 4
* To do the lookup, we will load the map unit containing map index 9 and get the value 0x05000000
* We will then shift to focus on the nibble with map index 9 (which again has nibble shift 22), so we will get
  the nibble value 5.
* Therefore we know that there is a method start at map index 9, nibble value 5.
* But the address we're looking for is map index 9, nibble value 4.
* We know that methods can't start within 32-bytes of each other, so we know that the method we're looking for is not in the current nibble.
* We will then try to shift to the previous nibble in the map unit (0x00000005 >> 4 = 0x00000000)
* Therefore we know there is no method start at any map index in the current map unit.
* We will then align the map index to the start of the current map unit (map index 8) and move back to the previous map unit (map index 7)
* At that point, we scan backwards for a non-zero map unit and a non-zero nibble within the first non-zero map unit. Since there are none, we return null.


## Version 2

Version 2 of the contract uses the new `NibbleMapConstantLookup` algorithm which has O(1) lookup time compared to the `NibbleMapLinearLookup` O(n) lookup time.

With the exception of the nibblemap change, version 2 is identical to version 1.

### NibbleMap

The `NibbleMapConstantLookup` implementation is very similar to `NibbleMapLinearLookup` with the addition
of writing relative pointers into the nibblemap whenever a code block completely covers the code region
represented by a DWORD, with the current values 256 bytes.
This allows for O(1) lookup time with the cost of O(n) write time.

Pointers are encoded using the top 28 bits of the DWORD as normal. The bottom 4 bits of the pointer
are reduced to 2 bits of data using the fact that code start must be 4 byte aligned. This is encoded into
the nibble in bits 28 .. 31 of the DWORD with values 9-12. This is also used to differentiate DWORDs
filled with nibble values and DWORDs with pointer values.

| Nibble Value | Meaning | How to decode |
|:------------:|:--------|:--------------:|
| 0            | empty | |
| 1-8          | Nibble | value - 1 |
| 9-12         | Pointer | value - 1 << 2
| 13-15        | unused | |

To read the nibblemap, we check if the DWORD is a pointer. If so, then we know the value currentPC is
part of a managed code block beginning at the mapBase + decoded pointer. Otherwise we can check for nibbles
as normal. If the DWORD is empty (no pointer or previous nibbles), then we check the previous DWORD for a
pointer or preceeding nibble. If that DWORD is empty, then we must not be in a managed function. If we were,
the write algorithm would have written a relative pointer in the DWORD or we would have seen the start nibble.

Note, a currentPC pointing to bytes outside a function has undefined lookup behavior.
In this implementation we may "extend" the lookup period of a function several hundred bytes
if there is not another function immediately following it.

We will go through the same example as above with the new algorithm. Suppose there is code starting at address 304 (0x130) with length 1024 (0x400).

* There will be a nibble at the start of the function as before.
    * The map index will be 304 / 32 = 9 and the byte offset will be 304 % 32 = 16
    * Because addresses are 4-byte aligned, the nibble value will be 1 + 16 / 4 = 5  (we reserve 0 to mean no method).
    * So the map unit containing index 9 will contain the value 0x5 << 24 (the map index 9 means we want the second nibble in the second map unit, and we number the nibbles starting from the most significant) , or 0x05000000
* Since the function starts at 304 with a length of 1024, the last byte of the function is at 1327 (0x52F). Map units (DWORDs) contain 256 bytes (0x100) algined to the map base. Therefore map units represnting 0x200-0x2FF, 0x300-0x3FF and 0x400-0x4ff are completely covered by the function and will have a relative pointer.
    * To get the relative pointer value we split the code start value at the bottom 4 bits. The top 28 bits are included as normal. We shift the bottom 4 bits 2 to the right and add 9, to get the bottom 4 bits encoding. This gives us a relative pointer value of 311 (0x137).
        * 304 = 0b100110000
        * Top 28 bits: 304 = 0b10011xxxx
        * Bottom 4 bits: 0 = 0b0000
        * Bottom 4 bits encoding: 9 = (0 >> 2) + 9
        * Relative Pointer Encoding: 311 = 304 + 9

Now suppose we do a lookup for address 1300 (0x514)
* The map index will be 1300 / 32 = 40 which is located in the 40 / 8 = 5th map unit (DWORD).
* We read the value of the 5th map unit and find it is empty.
* We read the value of the 4th map unit and find that the nibble in the lowest bits has the value of 9 implying that this map unit is a relative pointer.
* Since we found a relative pointer we can decode the entire map unit as a relative pointer and return that address added to the base.
