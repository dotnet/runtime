ReadyToRun File Format
======================

Revisions:
* 1.1 - [Jan Kotas](https://github.com/jkotas) - 2015
* 3.1 - [Tomas Rylek](https://github.com/trylek) - 2019

# Introduction

This document describes ReadyToRun format implemented in CoreCLR as of June 2019.

# PE Headers and CLI Headers

ReadyToRun images conform to CLI file format as described in ECMA-335, with the following 
customizations:

- The PE file is always platform specific
- CLI Header Flags field has set `COMIMAGE_FLAGS_IL_LIBRARY` (0x00000004) bit set
- CLI Header `ManagedNativeHeader` points to READYTORUN_HEADER

The image contains full copy of the IL and metadata that it was generated from.

## Future Improvements

The limitations of the current format are:

- **Type loading from IL metadata**: All types are built from IL metadata at runtime currently.
  It is bloating the size - prevents stripping full metadata from the image, and fragile -
  assumes fixed field layout algorithm. A new section with compact type layout description
  optimized for runtime type loading is needed to address it. (Similar concept as CTL.)

- **Debug info size**: The debug information is unnecessarily bloating the image. This solution was 
  chosen for compatibility with the current desktop/CoreCLR debugging pipeline. Ideally, the 
  debug information should be stored in separate file.

# Structures

The structures and accompanying constants are defined in the [readytorun.h](https://github.com/dotnet/coreclr/blob/master/src/inc/readytorun.h) header file.

## READYTORUN_HEADER

```C++
struct READYTORUN_HEADER
{
    DWORD                   Signature;      // READYTORUN_SIGNATURE
    USHORT                  MajorVersion;   // READYTORUN_VERSION_XXX
    USHORT                  MinorVersion;

    DWORD                   Flags;          // READYTORUN_FLAG_XXX

    DWORD                   NumberOfSections;

    // Array of sections follows. The array entries are sorted by Type
    // READYTORUN_SECTION   Sections[];
};

struct READYTORUN_SECTION
{
    DWORD                   Type;           // READYTORUN_SECTION_XXX
    IMAGE_DATA_DIRECTORY    Section;
};
```

### READYTORUN_HEADER::Signature

Always set to 0x00525452 (ASCII encoding for RTR). The signature can be used to distinguish 
ReadyToRun images from other CLI images with ManagedNativeHeader (e.g. NGen images).

### READYTORUN_HEADER::MajorVersion/MinorVersion

The current format version is 3.1. MajorVersion increments are meant for file format breaking changes. 
MinorVersion increments are meant to compatible file format changes.  
 
**Example**: Assume the highest version supported by the runtime is 2.3. The runtime should be able to 
successfully execute native code from images of version 2.9. The runtime should refuse to execute 
native code from image of version 3.0.

### READYTORUN_HEADER::Flags

| Flag                                    |      Value | Description
|:----------------------------------------|-----------:|:-----------
| READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE | 0x00000001 | Set if the original IL image was platform neutral. The platform neutrality is part of assembly name. This flag can be used to reconstruct the full original assembly name.

## READYTORUN_SECTION

```C++
struct READYTORUN_SECTION
{
    DWORD                   Type;           // READYTORUN_SECTION_XXX
    IMAGE_DATA_DIRECTORY    Section;
};
```
 
This section contains array of `READYTORUN_SECTION` records immediately follows 
`READYTORUN_HEADER`. Number of elements in the array is `READYTORUN_HEADER::NumberOfSections`. 
Each record contains section type and its location within the binary. The array is sorted by section type 
to allow binary searching.

This setup allows adding new or optional section types, and obsoleting existing section types, without 
file format breaking changes. The runtime is not required to understand all section types in order to load 
and execute the ready to run file.

The following section types are defined and described later in this document:

```C++
enum ReadyToRunSectionType
{
    READYTORUN_SECTION_COMPILER_IDENTIFIER          = 100,
    READYTORUN_SECTION_IMPORT_SECTIONS              = 101,
    READYTORUN_SECTION_RUNTIME_FUNCTIONS            = 102,
    READYTORUN_SECTION_METHODDEF_ENTRYPOINTS        = 103,
    READYTORUN_SECTION_EXCEPTION_INFO               = 104,
    READYTORUN_SECTION_DEBUG_INFO                   = 105,
    READYTORUN_SECTION_DELAYLOAD_METHODCALL_THUNKS  = 106,
    // 107 used by an older format of READYTORUN_SECTION_AVAILABLE_TYPES
    READYTORUN_SECTION_AVAILABLE_TYPES              = 108,
    READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS  = 109,
    READYTORUN_SECTION_INLINING_INFO                = 110, // Added in V2.1
    READYTORUN_SECTION_PROFILEDATA_INFO             = 111, // Added in V2.2
    READYTORUN_SECTION_MANIFEST_METADATA            = 112, // Added in V2.3
    READYTORUN_SECTION_ATTRIBUTEPRESENCE            = 113, // Added in V3.1
};
```

## READYTORUN_SECTION_COMPILER_IDENTIFIER

This section contains zero terminated ASCII string that identifies the compiler used to produce the 
image.

**Example**: `CoreCLR 4.6.22727.0 PROJECTK`

## READYTORUN_SECTION_IMPORT_SECTIONS

This section contains array of READYTORUN_IMPORT_SECTION structures. Each entry describes range of 
slots that had to be filled with the value from outside the module (typically lazily). The initial values of 
slots in each range are either zero or pointers to lazy initialization helper.

```C++
struct READYTORUN_IMPORT_SECTION
{
    IMAGE_DATA_DIRECTORY    Section;            // Section containing values to be fixed up
    USHORT                  Flags;              // One or more of ReadyToRunImportSectionFlags
    BYTE                    Type;               // One of ReadyToRunImportSectionType
    BYTE                    EntrySize;
    DWORD                   Signatures;         // RVA of optional signature descriptors
    DWORD                   AuxiliaryData;      // RVA of optional auxiliary data (typically GC info)
};
```

### READYTORUN_IMPORT_SECTIONS::Flags

| ReadyToRunImportSectionFlags           | Value  | Description
|:---------------------------------------|-------:|:-----------
| READYTORUN_IMPORT_SECTION_FLAGS_EAGER  | 0x0001 | Set if the slots in the section have to be initialized at image load time. It is used to avoid lazy initialization when it cannot be done or when it would have undesirable reliability or performance effects (unexpected failure or GC trigger points, overhead of lazy initialization).

### READYTORUN_IMPORT_SECTIONS::Type

| ReadyToRunImportSectionType            | Value  | Description
|:---------------------------------------|-------:|:-----------
| READYTORUN_IMPORT_SECTION_TYPE_UNKNOWN | 0      | The type of slots in this section is unspecified.

*Future*: The section type can be used to group slots of the same type together. For example, all virtual 
stub dispatch slots may be grouped together to simplify resetting of virtual stub dispatch cells into their 
initial state. 

### READYTORUN_IMPORT_SECTIONS::Signatures

This field points to array of RVAs that is parallel with the array of slots. Each RVA points to fixup 
signature that contains the information required to fill the corresponding slot. The signature encoding 
builds upon the encoding used for signatures in ECMA-335. The first element of the signature describes the 
fixup kind, the rest of the signature varies based on the fixup kind.

| ReadyToRunFixupKind                      | Value | Description
|:-----------------------------------------|------:|:-----------
| READYTORUN_FIXUP_ThisObjDictionaryLookup |  0x07 | Generic lookup using `this`; followed by the type signature and by the method signature
| READYTORUN_FIXUP_TypeDictionaryLookup    |  0x08 | Type-based generic lookup for methods on instantiated types; followed by the typespec signature
| READYTORUN_FIXUP_MethodDictionaryLookup  |  0x09 | Generic method lookup; followed by the method spec signature
| READYTORUN_FIXUP_TypeHandle              |  0x10 | Pointer uniquely identifying the type to the runtime, followed by typespec signature (see ECMA-335)
| READYTORUN_FIXUP_MethodHandle            |  0x11 | Pointer uniquely identifying the method to the runtime, followed by method signature (see below)
| READYTORUN_FIXUP_FieldHandle             |  0x12 | Pointer uniquely identifying the field to the runtime, followed by field signature (see below)
| READYTORUN_FIXUP_MethodEntry             |  0x13 | Method entrypoint or call, followed by method signature
| READYTORUN_FIXUP_MethodEntry_DefToken    |  0x14 | Method entrypoint or call, followed by methoddef token (shortcut)
| READYTORUN_FIXUP_MethodEntry_RefToken    |  0x15 | Method entrypoint or call, followed by methodref token (shortcut)
| READYTORUN_FIXUP_VirtualEntry            |  0x16 | Virtual method entrypoint or call, followed by method signature
| READYTORUN_FIXUP_VirtualEntry_DefToken   |  0x17 | Virtual method entrypoint or call, followed by methoddef token (shortcut)
| READYTORUN_FIXUP_VirtualEntry_RefToken   |  0x18 | Virtual method entrypoint or call, followed by methodref token (shortcut)
| READYTORUN_FIXUP_VirtualEntry_Slot       |  0x19 | Virtual method entrypoint or call, followed by typespec signature and slot
| READYTORUN_FIXUP_Helper                  |  0x1A | Helper call, followed by helper call id (see chapter 4 Helper calls)
| READYTORUN_FIXUP_StringHandle            |  0x1B | String handle, followed by metadata string token
| READYTORUN_FIXUP_NewObject               |  0x1C | New object helper, followed by typespec  signature
| READYTORUN_FIXUP_NewArray                |  0x1D | New array helper, followed by typespec signature
| READYTORUN_FIXUP_IsInstanceOf            |  0x1E | isinst helper, followed by typespec signature
| READYTORUN_FIXUP_ChkCast                 |  0x1F | chkcast helper, followed by typespec signature
| READYTORUN_FIXUP_FieldAddress            |  0x20 | Field address, followed by field signature
| READYTORUN_FIXUP_CctorTrigger            |  0x21 | Static constructor trigger, followed by typespec signature
| READYTORUN_FIXUP_StaticBaseNonGC         |  0x22 | Non-GC static base, followed by typespec signature
| READYTORUN_FIXUP_StaticBaseGC            |  0x23 | GC static base, followed by typespec signature
| READYTORUN_FIXUP_ThreadStaticBaseNonGC   |  0x24 | Non-GC thread-local static base, followed by typespec signature
| READYTORUN_FIXUP_ThreadStaticBaseGC      |  0x25 | GC thread-local static base, followed by typespec signature
| READYTORUN_FIXUP_FieldBaseOffset         |  0x26 | Starting offset of fields for given type, followed by typespec signature. Used to address base class fragility.
| READYTORUN_FIXUP_FieldOffset             |  0x27 | Field offset, followed by field signature
| READYTORUN_FIXUP_TypeDictionary          |  0x28 | Hidden dictionary argument for generic code, followed by typespec signature
| READYTORUN_FIXUP_MethodDictionary        |  0x29 | Hidden dictionary argument for generic code, followed by method signature
| READYTORUN_FIXUP_Check_TypeLayout        |  0x2A | Verification of type layout, followed by typespec and expected type layout descriptor
| READYTORUN_FIXUP_Check_FieldOffset       |  0x2B | Verification of field offset, followed by field signature and expected field layout descriptor
| READYTORUN_FIXUP_DelegateCtor            |  0x2C | Delegate constructor, followed by method signature
| READYTORUN_FIXUP_DeclaringTypeHandle     |  0x2D | Dictionary lookup for method declaring type. Followed by the type signature.
| READYTORUN_FIXUP_IndirectPInvokeTarget   |  0x2E | Target of an inlined PInvoke. Followed by method signature.
| READYTORUN_FIXUP_ModuleOverride          |  0x80 | When or-ed to the fixup ID, the fixup byte in the signature is followed by an encoded uint with assemblyref index, either within the MSIL metadata of the master context module for the signature or within the manifest metadata R2R header table (used in cases inlining brings in references to assemblies not seen in the input MSIL).

#### Method Signatures

MethodSpec signatures defined by ECMA-335 are not rich enough to describe method flavors 
referenced by native code. The first element of the method signature are flags. It is followed by method 
token, and additional data determined by the flags.

| ReadyToRunMethodSigFlags                  | Value | Description
|:------------------------------------------|------:|:-----------
| READYTORUN_METHOD_SIG_UnboxingStub        |  0x01 | Unboxing entrypoint of the method.
| READYTORUN_METHOD_SIG_InstantiatingStub   |  0x02 | Instantiating entrypoint of the method does not take hidden dictionary generic argument.
| READYTORUN_METHOD_SIG_MethodInstantiation |  0x04 | Method instantitation. Number of instantiation arguments followed by typespec for each of them appended as additional data.
| READYTORUN_METHOD_SIG_SlotInsteadOfToken  |  0x08 | If set, the token is slot number. Used for multidimensional array methods that do not have metadata token, and also as an optimization for stable interface methods. Cannot be combined with `MemberRefToken`.
| READYTORUN_METHOD_SIG_MemberRefToken      |  0x10 | If set, the token is memberref token. If not set, the token is methoddef token.
| READYTORUN_METHOD_SIG_Constrained         |  0x20 | Constrained type for method resolution. Typespec appended as additional data.
| READYTORUN_METHOD_SIG_OwnerType           |  0x40 | Method type. Typespec appended as additional data.

#### Field Signatures

ECMA-335 does not define field signatures that are rich enough to describe method flavors referenced 
by native code. The first element of the field signature are flags. It is followed by field token, and 
additional data determined by the flags.

| ReadyToRunFieldSigFlags                  | Value | Description
|:-----------------------------------------|------:|:-----------
| READYTORUN_FIELD_SIG_IndexInsteadOfToken |  0x08 | Used as an optimization for stable fields. Cannot be combined with `MemberRefToken`.
| READYTORUN_FIELD_SIG_MemberRefToken      |  0x10 | If set, the token is memberref token. If not set, the token is fielddef token.
| READYTORUN_FIELD_SIG_OwnerType           |  0x40 | Field type. Typespec appended as additional data.

### READYTORUN_IMPORT_SECTIONS::AuxiliaryData

For slots resolved lazily via `READYTORUN_HELPER_DelayLoad_MethodCall` helper, auxiliary data are 
compressed argument maps that allow precise GC stack scanning while the helper is running. The CoreCLR runtime class [`GCRefMapDecoder`](https://github.com/dotnet/coreclr/blob/6b9a3d3a87825b1a34bd8f114c9b181ce75b3b2e/src/inc/gcrefmap.h#L157) is used to parse this information. This data would not be required for runtimes that allow conservative stack scanning.

The auxiliary data table contains the exact same number of GC ref map records as there are method entries in the import section. To accelerate GC ref map lookup, the auxiliary data section starts with a lookup table holding the offset of every 1024-th method in the runtime function table within the linearized GC ref map.

|     Offset in auxiliary data | Size | Content
|-----------------------------:|-----:|:-------
|                            0 |    4 | Offset to GC ref map info for method #0 relative to this byte i.e. 4 * (MethodCount / 1024 + 1)
|                            4 |    4 | Offset to GC ref map info for method #1024
|                            8 |    4 | Offset to GC ref map info for method #2048
|                          ... |      |
| 4 * (MethodCount / 1024 + 1) |  ... | Serialized GC ref map info

The GCRef map is used to encode GC type of arguments for callsites. Logically, it is a sequence `<pos, token>` where `pos` is 
position of the reference in the stack frame and `token` is type of GC reference (one of [`GCREFMAP_XXX`](https://github.com/dotnet/coreclr/blob/6b9a3d3a87825b1a34bd8f114c9b181ce75b3b2e/src/inc/corcompile.h#L633) values):

| CORCOMPILE_GCREFMAP_TOKENS | Value | Stack frame entry interpretation
|:---------------------------|------:|:--------------------------------
| GCREFMAP_SKIP              |     0 | Not a GC-relevant entry
| GCREFMAP_REF               |     1 | GC reference
| GCREFMAP_INTERIOR          |     2 | Pointer to a GC reference
| GCREFMAP_METHOD_PARAM      |     3 | Hidden method instantiation argument to generic method
| GCREFMAP_TYPE_PARAM        |     4 | Hidden type instantiation argument to generic method
| GCREFMAP_VASIG_COOKIE      |     5 | VARARG signature cookie

The position values are calculated in `size_t` aka `IntPtr` units (4 bytes for 32-bit architectures vs. 8 bytes for 64-bit architectures) starting at the first position in the transition frame that may contain GC references. For all architectures except for **arm64** this is the beginning of the array of spilled argument registers. On arm64 it is the offset of the `X8` register used to pass the location to be filled in with the return value by the called method.

* The encoding always starts at the byte boundary. The high order bit of each byte is used to signal end of the encoding stream. The last byte has the high order bit zero. It means that there are 7 useful bits in each byte. 

* "pos" is always encoded as delta from previous pos.

* The basic encoding unit is two bits. Values 0, 1 and 2 are the common constructs (skip single slot, GC reference, interior pointer). Value 3 means that extended encoding follows. 

* The extended information is integer encoded in one or more four bit blocks. The high order bit of the four bit block is used to signal the end.

* For x86, the encoding starts with size of the callee popped stack. The size is encoded using the same mechanism as above (two bit
basic encoding, with extended encoding for large values).

## READYTORUN_SECTION_RUNTIME_FUNCTIONS

This section contains sorted array of `RUNTIME_FUNCTION` entries that describe all functions in the 
image with pointers to their unwind info. The standard Windows xdata/pdata format is used.
ARM format is used for x86 to compensate for lack of x86 unwind info standard.
The unwind info blob is immediately followed by GC info blob. The encoding slightly differs for amd64 which encodes an extra 4-byte representing the end RVA of the unwind info blob.

### RUNTIME_FUNCTION (x86, arm, arm64, size = 8 bytes)

| Offset | Size | Value
|-------:|-----:|:-----
|      0 |    4 | Unwind info start RVA
|      4 |    4 | GC info start RVA

### RUNTIME_FUNCTION (amd64, size = 12 bytes)

| Offset | Size | Value
|-------:|-----:|:-----
|      0 |    4 | Unwind info start RVA
|      4 |    4 | Unwind info end RVA (1 plus RVA of last byte)
|      8 |    4 | GC info start RVA

## READYTORUN_SECTION_METHODDEF_ENTRYPOINTS

This section contains in native format sparse array (see 4 Native Format) that maps methoddef row to 
method entrypoint. Methoddef is used as index into the array. The element of the array is index of the 
method in `READYTORUN_SECTION_RUNTIME_FUNCTIONS`, followed by list of slots that needs to be 
filled before method can be executed executing.

The index of the method is shift left by 1 bit, with the low bit indicating whether the list of slots to fixup 
follows. The list of slots is encoded as follows (same encoding as used by NGen):

``
READYTORUN_IMPORT_SECTIONS absolute index
    absolute slot index 
    slot index delta 
    _
    slot index delta 
    0
READYTORUN_IMPORT_SECTIONS index delta
    absolute slot index 
    slot index delta 
    _ 
    slot delta 
    0
READYTORUN_IMPORT_SECTIONS index delta
    absolute slot index 
    slot index delta 
    _ 
    slot delta 
    0
0
``

The fixup list is a stream of integers encoded as nibbles (1 nibble = 4 bits). 3 bits of a nibble are used to 
store 3 bits of the value, and the top bit indicates if the following nibble contains rest of the value. If the 
top bit in the nibble is set, then the value continues in the next nibble.

The section and slot indices are delta-encoded offsets from that initial absolute index.  Delta-encoded 
means that the i-th value is the sum of values [1..i].

The list is terminated by a 0 (0 is not meaningful as valid delta).

## READYTORUN_SECTION_EXCEPTION_INFO

Exception handling information. This section contains array of 
`READYTORUN_EXCEPTION_LOOKUP_TABLE_ENTRY` sorted by `MethodStart` RVA. `ExceptionInfo` is RVA of 
`READYTORUN_EXCEPTION_CLAUSE` array that described the exception handling information for given 
method.

```C++
struct READYTORUN_EXCEPTION_LOOKUP_TABLE_ENTRY
{
    DWORD MethodStart;
    DWORD ExceptionInfo;
};

struct READYTORUN_EXCEPTION_CLAUSE
{
    CorExceptionFlag    Flags;  
    DWORD               TryStartPC;    
    DWORD               TryEndPC;
    DWORD               HandlerStartPC;  
    DWORD               HandlerEndPC;  
    union {
        mdToken         ClassToken;
        DWORD           FilterOffset;
    };  
};
```

Same encoding is as used by NGen.

## READYTORUN_SECTION_DEBUG_INFO

This section contains information to support debugging: native offset and local variable maps. 

**TODO**: Document the debug info encoding. It is the same encoding as used by NGen. It should not be 
required when debuggers are able to handle debug info stored separately.

## READYTORUN_SECTION_DELAYLOAD_METHODCALL_THUNKS

This section marks region that contains thunks for `READYTORUN_HELPER_DelayLoad_MethodCall` 
helper. It is used by debugger for step-in into lazily resolved calls. It should not be required when 
debuggers are able to handle debug info stored separately.

## READYTORUN_SECTION_AVAILABLE_TYPES

This section contains a native hashtable of all defined & export types within the compilation module. The key is the full type name, the value is the exported type or defined type token row ID left-shifted by one and or-ed with bit 0 defining the token type:

| Bit value | Token type
|----------:|:----------
|         0 | defined type
|         1 | exported type

The version-resilient hashing algorithm used for hashing the type names is implemented in [vm/versionresilienthashcode.cpp](https://github.com/dotnet/coreclr/blob/ec2a74e7649f1c0ecff32ce86724bf3ca80bfd46/src/vm/versionresilienthashcode.cpp#L75).

## READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS

This section contains a native hashtable of all generic method instantiations compiled into the R2R executable. The key is the method instance signature; the appropriate version-resilient hash code calculation is implemented in [vm/versionresilienthashcode.cpp](https://github.com/dotnet/coreclr/blob/ec2a74e7649f1c0ecff32ce86724bf3ca80bfd46/src/vm/versionresilienthashcode.cpp#L128); the value, represented by the `EntryPointWithBlobVertex` class, stores the method index in the runtime function table, the fixups blob and a blob encoding the method signature.

## READYTORUN_SECTION_INLINING_INFO

**TODO**: document inlining info encoding

## READYTORUN_SECTION_PROFILEDATA_INFO

**TODO**: document profile data encoding

## READYTORUN_SECTION_MANIFEST_METADATA

Manifest metadata is an [ECMA-335] metadata blob containing extra reference assemblies within the version bubble introduced by inlining on top of assembly references stored in the input MSIL. As of R2R version 3.1, the metadata is only searched for the AssemblyRef table. This is used to translate module override indices in signatures to the actual reference modules (using either the `READYTORUN_FIXUP_ModuleOverride` bit flag on the signature fixup byte or the `ELEMENT_TYPE_MODULE_ZAPSIG` COR element type).

**Disclaimer:** The manifest metadata is a new feature that hasn't shipped yet; it involves straightforward adaptation of a fragile nGen technology to ReadyToRun images as an expedite means for enabling new functionality (larger version bubble support). The precise details of this encoding are still work in progress and likely to further evolve.

**Note:** It doesn't make sense to store references to assemblies external to the version bubble in the manifest metadata as there's no guarantee that their metadata token values remain constant; thus we cannot encode signatures relative to them.

The module override index translation algorithm is as follows (**ILAR** = *the number of `AssemblyRef` rows in the input MSIL*):

| Module override index (*i*) | Reference assembly
|:----------------------------|:------------------
| *i* = 0                     | Global context - assembly containing the signature
| 1 <= *i* <= **ILAR**        | *i* is the index into the MSIL `AssemblyRef` table
| *i* > **ILAR**              | *i* - **ILAR** - 1 is the zero-based index into the `AssemblyRef` table in the manifest metadata

**Note:** This means that the entry corresponding to *i* = **ILAR** + 1 is actually undefined as it corresponds to the `NULL` entry (ROWID #0) in the manifest metadata AssemblyRef table. The first meaningful index into the manifest metadata, *i* = **ILAR** + 2, corresponding to ROWID #1, is historically filled in by Crossgen with the input assembly info but this shouldn't be depended upon, in fact the input assembly is useless in the manifest metadata as the module override to it can be encoded by using the special index 0.

## READYTORUN_SECTION_ATTRIBUTEPRESENCE

**TODO**: document attribute presence encoding

# Native Format

Native format is set of encoding patterns that allow persisting type system data in a binary format that is 
efficient for runtime access - both in working set and CPU cycles. (Originally designed for and extensively 
used by .NET Native.)

## Integer encoding

Native format uses a variable length encoding scheme for signed and unsigned numbers. The low bits of 
the first byte of the encoding specify the number of following bytes as follows:

* `xxxxxxx0` (i.e. the least significant bit is 0): no more bytes follow. Shift the byte one bit right, and 
  sign or zero extend for signed and unsigned number, respectively.
* `xxxxxx01`: one more byte follows. Build a 16-bit number from the two bytes read (little-endian 
  order), shift it right by 2 bits, then sign or zero extend.
* `xxxxx011`: two more bytes follow. Build a 24-bit number from the three bytes read (little-endian 
  order), shift it right by 3 bits, then sign or zero extend.
* `xxxx0111`: three more bytes follow. Build a 32-bit number from the four bytes read, then sign or 
  zero extend
* `xxxx1111`: four more bytes follow. Discard the first byte, build the signed or unsigned number 
  from the following four bytes (again little-endian order).

**Examples**:
* the unsigned number 12 (`0x0000000c`) would be expressed as the single byte `0x18`.
* The unsigned number 1000 (`0x000003e8`) would be expressed as the two bytes `0xa1, 0x0f`

## Sparse Array

**TODO**: Document native format sparse array 

## Hashtable

**TODO**: Document native format hashtable

# Helper calls

List of helper calls supported by READYTORUN_FIXUP_Helper:

```C++
enum ReadyToRunHelper
{
    READYTORUN_HELPER_Invalid                   = 0x00,

    // Not a real helper - handle to current module passed to delay load helpers.
    READYTORUN_HELPER_Module                    = 0x01,
    READYTORUN_HELPER_GSCookie                  = 0x02,

    //
    // Delay load helpers
    //

    // All delay load helpers use custom calling convention:
    // - scratch register - address of indirection cell. 0 = address is inferred from callsite.
    // - stack - section index, module handle
    READYTORUN_HELPER_DelayLoad_MethodCall      = 0x08,

    READYTORUN_HELPER_DelayLoad_Helper          = 0x10,
    READYTORUN_HELPER_DelayLoad_Helper_Obj      = 0x11,
    READYTORUN_HELPER_DelayLoad_Helper_ObjObj   = 0x12,

    // JIT helpers

    // Exception handling helpers
    READYTORUN_HELPER_Throw                     = 0x20,
    READYTORUN_HELPER_Rethrow                   = 0x21,
    READYTORUN_HELPER_Overflow                  = 0x22,
    READYTORUN_HELPER_RngChkFail                = 0x23,
    READYTORUN_HELPER_FailFast                  = 0x24,
    READYTORUN_HELPER_ThrowNullRef              = 0x25,
    READYTORUN_HELPER_ThrowDivZero              = 0x26,

    // Write barriers
    READYTORUN_HELPER_WriteBarrier              = 0x30,
    READYTORUN_HELPER_CheckedWriteBarrier       = 0x31,
    READYTORUN_HELPER_ByRefWriteBarrier         = 0x32,

    // Array helpers
    READYTORUN_HELPER_Stelem_Ref                = 0x38,
    READYTORUN_HELPER_Ldelema_Ref               = 0x39,

    READYTORUN_HELPER_MemSet                    = 0x40,
    READYTORUN_HELPER_MemCpy                    = 0x41,

    // Get string handle lazily
    READYTORUN_HELPER_GetString                 = 0x50,

    // Used by /Tuning for Profile optimizations
    READYTORUN_HELPER_LogMethodEnter            = 0x51,

    // Reflection helpers
    READYTORUN_HELPER_GetRuntimeTypeHandle      = 0x54,
    READYTORUN_HELPER_GetRuntimeMethodHandle    = 0x55,
    READYTORUN_HELPER_GetRuntimeFieldHandle     = 0x56,

    READYTORUN_HELPER_Box                       = 0x58,
    READYTORUN_HELPER_Box_Nullable              = 0x59,
    READYTORUN_HELPER_Unbox                     = 0x5A,
    READYTORUN_HELPER_Unbox_Nullable            = 0x5B,
    READYTORUN_HELPER_NewMultiDimArr            = 0x5C,
    READYTORUN_HELPER_NewMultiDimArr_NonVarArg  = 0x5D,

    // Helpers used with generic handle lookup cases
    READYTORUN_HELPER_NewObject                 = 0x60,
    READYTORUN_HELPER_NewArray                  = 0x61,
    READYTORUN_HELPER_CheckCastAny              = 0x62,
    READYTORUN_HELPER_CheckInstanceAny          = 0x63,
    READYTORUN_HELPER_GenericGcStaticBase       = 0x64,
    READYTORUN_HELPER_GenericNonGcStaticBase    = 0x65,
    READYTORUN_HELPER_GenericGcTlsBase          = 0x66,
    READYTORUN_HELPER_GenericNonGcTlsBase       = 0x67,
    READYTORUN_HELPER_VirtualFuncPtr            = 0x68,

    // Long mul/div/shift ops
    READYTORUN_HELPER_LMul                      = 0xC0,
    READYTORUN_HELPER_LMulOfv                   = 0xC1,
    READYTORUN_HELPER_ULMulOvf                  = 0xC2,
    READYTORUN_HELPER_LDiv                      = 0xC3,
    READYTORUN_HELPER_LMod                      = 0xC4,
    READYTORUN_HELPER_ULDiv                     = 0xC5,
    READYTORUN_HELPER_ULMod                     = 0xC6,
    READYTORUN_HELPER_LLsh                      = 0xC7,
    READYTORUN_HELPER_LRsh                      = 0xC8,
    READYTORUN_HELPER_LRsz                      = 0xC9,
    READYTORUN_HELPER_Lng2Dbl                   = 0xCA,
    READYTORUN_HELPER_ULng2Dbl                  = 0xCB,

    // 32-bit division helpers
    READYTORUN_HELPER_Div                       = 0xCC,
    READYTORUN_HELPER_Mod                       = 0xCD,
    READYTORUN_HELPER_UDiv                      = 0xCE,
    READYTORUN_HELPER_UMod                      = 0xCF,

    // Floating point conversions
    READYTORUN_HELPER_Dbl2Int                   = 0xD0,
    READYTORUN_HELPER_Dbl2IntOvf                = 0xD1,
    READYTORUN_HELPER_Dbl2Lng                   = 0xD2,
    READYTORUN_HELPER_Dbl2LngOvf                = 0xD3,
    READYTORUN_HELPER_Dbl2UInt                  = 0xD4,
    READYTORUN_HELPER_Dbl2UIntOvf               = 0xD5,
    READYTORUN_HELPER_Dbl2ULng                  = 0xD6,
    READYTORUN_HELPER_Dbl2ULngOvf               = 0xD7,

    // Floating point ops
    READYTORUN_HELPER_DblRem                    = 0xE0,
    READYTORUN_HELPER_FltRem                    = 0xE1,
    READYTORUN_HELPER_DblRound                  = 0xE2,
    READYTORUN_HELPER_FltRound                  = 0xE3,

#ifndef _TARGET_X86_
    // Personality rountines
    READYTORUN_HELPER_PersonalityRoutine        = 0xF0,
    READYTORUN_HELPER_PersonalityRoutineFilterFunclet = 0xF1,
#endif

    //
    // Deprecated/legacy
    //

    // JIT32 x86-specific write barriers
    READYTORUN_HELPER_WriteBarrier_EAX          = 0x100,
    READYTORUN_HELPER_WriteBarrier_EBX          = 0x101,
    READYTORUN_HELPER_WriteBarrier_ECX          = 0x102,
    READYTORUN_HELPER_WriteBarrier_ESI          = 0x103,
    READYTORUN_HELPER_WriteBarrier_EDI          = 0x104,
    READYTORUN_HELPER_WriteBarrier_EBP          = 0x105,
    READYTORUN_HELPER_CheckedWriteBarrier_EAX   = 0x106,
    READYTORUN_HELPER_CheckedWriteBarrier_EBX   = 0x107,
    READYTORUN_HELPER_CheckedWriteBarrier_ECX   = 0x108,
    READYTORUN_HELPER_CheckedWriteBarrier_ESI   = 0x109,
    READYTORUN_HELPER_CheckedWriteBarrier_EDI   = 0x10A,
    READYTORUN_HELPER_CheckedWriteBarrier_EBP   = 0x10B,

    // JIT32 x86-specific exception handling
    READYTORUN_HELPER_EndCatch                  = 0x110,
};
```

# References

[ECMA-335](http://www.ecma-international.org/publications/standards/Ecma-335.htm)
