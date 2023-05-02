ReadyToRun File Format
======================

Revisions:
* 1.1 - [Jan Kotas](https://github.com/jkotas) - 2015
* 3.1 - [Tomas Rylek](https://github.com/trylek) - 2019
* 4.1 - [Tomas Rylek](https://github.com/trylek) - 2020
* 5.3 - [Tomas Rylek](https://github.com/trylek) - 2021
* 5.4 - [David Wrighton](https://github.com/davidwrighton) - 2021
* 6.3 - [David Wrighton](https://github.com/davidwrighton) - 2022

# Introduction

This document describes ReadyToRun format 3.1 implemented in CoreCLR as of June 2019 and not yet
implemented proposed extensions 4.1 for the support of composite R2R file format.
**Composite R2R file format** has basically the same structure as the traditional R2R file format
defined in earlier revisions except that the output file represents a larger number of input MSIL
assemblies compiled together as a logical unit.

# PE Headers and CLI Headers

**Single-file ReadyToRun images** conform to CLI file format as described in ECMA-335
with the following customizations:

- The PE file is always platform specific
- CLI Header Flags field has set `COMIMAGE_FLAGS_IL_LIBRARY` (0x00000004) bit set
- CLI Header `ManagedNativeHeader` points to READYTORUN_HEADER

The COR header and ECMA 335 metadata pointed to by the COM descriptor data directory item
in the COFF header represent a full copy of the input IL and MSIL metadata it was generated from.

**Composite R2R files** currently conform to Windows PE executable file format as the
native envelope. Moving forward we plan to gradually add support for platform-native
executable formats (ELF on Linux, MachO on OSX) as the native envelopes. There is a
global CLI / COR header in the file, but it only exists to facilitate pdb generation, and does
not participate in any usages by the CoreCLR runtime. The ReadyToRun header structure is pointed to
by the well-known export symbol `RTR_HEADER` and has the `READYTORUN_FLAG_COMPOSITE` flag set.

Input MSIL metadata and IL streams can be either embedded in the composite R2R file or left
as separate files on disk. In case of embedded MSIL, the "actual" metadata for the individual
component assemblies is accessed via the R2R section `ComponentAssemblies`.

**Standalone MSIL files** used as the source of IL and metadata for composite R2R executables
without MSIL embedding are copied to the output folder next to the composite R2R executable
and are rewritten by the compiler to include a formal ReadyToRun header with forwarding
information pointing to the owner composite R2R executable (section `OwnerCompositeExecutable`).

# Additions to the debug directory

Currently shipping PE envelopes - both single-file and composite - can contain records for additional
debug information in the debug directory. One such entry specific to R2R images is the one for R2R PerfMaps.
The format of the auxiliary file is described [R2R perfmap format](./r2r-perfmap-format.md) and the corresponding
debug directory entry is described in [PE COFF](../../../design/specs/PE-COFF.md#r2r-perfmap-debug-directory-entry-type-21).

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

The structures and accompanying constants are defined in the
[readytorun.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/readytorun.h)
header file.
Basically the entire R2R executable image is addressed through the READYTORUN_HEADER singleton
pointed to by the well-known export RTR_HEADER in the export section of the native executable
envelope.

For single-file R2R executables, there's just one header representing all image sections.
For composite and single exe, the global `READYTORUN_HEADER` includes a section of the type
`ComponentAssemblies` representing the component assemblies comprising the composite
R2R image. This table is parallel to (it used the same indexing as) the table
`READYTORUN_MANIFEST_METADATA`. Each `READYTORUN_SECTION_ASSEMBLIES_ENTRY` record points
to a `READYTORUN_CORE_HEADER` variable-length structure representing sections specific to the
particular assembly.

## READYTORUN_HEADER

```C++
struct READYTORUN_HEADER
{
    DWORD                   Signature;      // READYTORUN_SIGNATURE
    USHORT                  MajorVersion;   // READYTORUN_VERSION_XXX
    USHORT                  MinorVersion;

    READYTORUN_CORE_HEADER  CoreHeader;
}
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

## READYTORUN_CORE_HEADER

```C++
struct READYTORUN_CORE_HEADER
{
    DWORD                   Flags;          // READYTORUN_FLAG_XXX

    DWORD                   NumberOfSections;

    // Array of sections follows. The array entries are sorted by Type
    // READYTORUN_SECTION   Sections[];
};
```

### READYTORUN_CORE_HEADER::Flags

| Flag                                       |      Value | Description
|:-------------------------------------------|-----------:|:-----------
| READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE    | 0x00000001 | Set if the original IL image was platform neutral. The platform neutrality is part of assembly name. This flag can be used to reconstruct the full original assembly name.
| READYTORUN_FLAG_COMPOSITE                  | 0x00000002 | The image represents a composite R2R file resulting from a combined compilation of a larger number of input MSIL assemblies.
| READYTORUN_FLAG_PARTIAL                    | 0x00000004 |
| READYTORUN_FLAG_NONSHARED_PINVOKE_STUBS    | 0x00000008 | PInvoke stubs compiled into image are non-shareable (no secret parameter)
| READYTORUN_FLAG_EMBEDDED_MSIL              | 0x00000010 | Input MSIL is embedded in the R2R image.
| READYTORUN_FLAG_COMPONENT                  | 0x00000020 | This is a component assembly of a composite R2R image
| READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE | 0x00000040 | This R2R module has multiple modules within its version bubble (For versions before version 6.3, all modules are assumed to possibly have this characteristic)
| READYTORUN_FLAG_UNRELATED_R2R_CODE         | 0x00000080 | This R2R module has code in it that would not be naturally encoded into this module

## READYTORUN_SECTION

```C++
struct READYTORUN_SECTION
{
    DWORD                   Type;           // READYTORUN_SECTION_XXX
    IMAGE_DATA_DIRECTORY    Section;
};
```

The `READYTORUN_CORE_HEADER` structure is immediately followed by an array of `READYTORUN_SECTION` records
representing the individual R2R sections. Number of elements in the array is `READYTORUN_HEADER::NumberOfSections`.
Each record contains section type and its location within the binary. The array is sorted by section type
to allow binary searching.

This setup allows adding new or optional section types, and obsoleting existing section types, without
file format breaking changes. The runtime is not required to understand all section types in order to load
and execute the ready to run file.

The following section types are defined and described later in this document:

| ReadyToRunSectionType     | Value | Scope (component assembly / entire image)
|:--------------------------|------:|:-----------
| CompilerIdentifier        |   100 | Image
| ImportSections            |   101 | Image
| RuntimeFunctions          |   102 | Image
| MethodDefEntryPoints      |   103 | Assembly
| ExceptionInfo             |   104 | Assembly
| DebugInfo                 |   105 | Assembly
| DelayLoadMethodCallThunks |   106 | Assembly
| ~~AvailableTypes~~        |   107 | (obsolete - used by an older format)
| AvailableTypes            |   108 | Assembly
| InstanceMethodEntryPoints |   109 | Image
| InliningInfo              |   110 | Assembly (added in V2.1)
| ProfileDataInfo           |   111 | Image (added in V2.2)
| ManifestMetadata          |   112 | Image (added in V2.3)
| AttributePresence         |   113 | Assembly (added in V3.1)
| InliningInfo2             |   114 | Image (added in V4.1)
| ComponentAssemblies       |   115 | Image (added in V4.1)
| OwnerCompositeExecutable  |   116 | Image (added in V4.1)
| PgoInstrumentationData    |   117 | Image (added in V5.2)
| ManifestAssemblyMvids     |   118 | Image (added in V5.3)
| CrossModuleInlineInfo     |   119 | Image (added in V6.3)

## ReadyToRunSectionType.CompilerIdentifier

This section contains zero terminated ASCII string that identifies the compiler used to produce the
image.

**Example**: `CoreCLR 4.6.22727.0 PROJECTK`

## ReadyToRunSectionType.ImportSections

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
| ReadyToRunImportSectionFlags::None     | 0x0000  | None
| ReadyToRunImportSectionFlags::Eager    | 0x0001 | Set if the slots in the section have to be initialized at image load time. It is used to avoid lazy initialization when it cannot be done or when it would have undesirable reliability or performance effects (unexpected failure or GC trigger points, overhead of lazy initialization).
| ReadyToRunImportSectionFlags::PCode    | 0x0004  | Section contains pointers to code


### READYTORUN_IMPORT_SECTIONS::Type

| ReadyToRunImportSectionType                 | Value  | Description
|:--------------------------------------------|-------:|:-----------
| ReadyToRunImportSectionType::Unknown      | 0      | The type of slots in this section is unspecified.
| ReadyToRunImportSectionType::StubDispatch | 2      | The type of slots in this section rely on stubs for dispatch.
| ReadyToRunImportSectionType::StringHandle | 3      | The type of slots in this section hold strings
| ReadyToRunImportSectionType::ILBodyFixups | 7      | The type of slots in this section represent cross module IL bodies

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
| READYTORUN_FIXUP_IndirectPInvokeTarget   |  0x2E | Target (indirect) of an inlined PInvoke. Followed by method signature.
| READYTORUN_FIXUP_PInvokeTarget           |  0x2F | Target of an inlined PInvoke. Followed by method signature.
| READYTORUN_FIXUP_Check_InstructionSetSupport | 0x30 | Specify the instruction sets that must be supported/unsupported to use the R2R code associated with the fixup.
| READYTORUN_FIXUP_Verify_FieldOffset      | 0x31 | Generate a runtime check to ensure that the field offset matches between compile and runtime. Unlike CheckFieldOffset, this will generate a runtime exception on failure instead of silently dropping the method
| READYTORUN_FIXUP_Verify_TypeLayout       | 0x32 | Generate a runtime check to ensure that the field offset matches between compile and runtime. Unlike CheckFieldOffset, this will generate a runtime exception on failure instead of silently dropping the method
| READYTORUN_FIXUP_Check_VirtualFunctionOverride | 0x33 | Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, code will not be used. See [Virtual override signatures](virtual-override-signatures) for details of the signature used.
| READYTORUN_FIXUP_Verify_VirtualFunctionOverride | 0x34 | Generate a runtime check to ensure that virtual function resolution has equivalent behavior at runtime as at compile time. If not equivalent, generate runtime failure. See [Virtual override signatures](virtual-override-signatures) for details of the signature used.
| READYTORUN_FIXUP_Check_IL_Body           |  0x35 | Check to see if an IL method is defined the same at runtime as at compile time. A failed match will cause code not to be used. See[IL Body signatures](il-body-signatures) for details.
| READYTORUN_FIXUP_Verify_IL_Body          |  0x36 | Verify an IL body is defined the same at compile time and runtime. A failed match will cause a hard runtime failure. See[IL Body signatures](il-body-signatures) for details.
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
| READYTORUN_METHOD_SIG_UpdateContext       |  0x80 | If set, update the module which is used to parse tokens before performing any token processing. A uint index into the modules table immediately follows the flags

#### Field Signatures

ECMA-335 does not define field signatures that are rich enough to describe method flavors referenced
by native code. The first element of the field signature are flags. It is followed by field token, and
additional data determined by the flags.

| ReadyToRunFieldSigFlags                  | Value | Description
|:-----------------------------------------|------:|:-----------
| READYTORUN_FIELD_SIG_IndexInsteadOfToken |  0x08 | Used as an optimization for stable fields. Cannot be combined with `MemberRefToken`.
| READYTORUN_FIELD_SIG_MemberRefToken      |  0x10 | If set, the token is memberref token. If not set, the token is fielddef token.
| READYTORUN_FIELD_SIG_OwnerType           |  0x40 | Field type. Typespec appended as additional data.

#### Virtual override signatures

ECMA 335 does not have a natural encoding for describing an overridden method. These signatures are encoded as a ReadyToRunVirtualFunctionOverrideFlags byte, followed by a method signature representing the declaration method, a type signature representing the type which is being devirtualized, and (optionally) a method signature indicating the implementation method.

| ReadyToRunVirtualFunctionOverrideFlags                | Value | Description
|:------------------------------------------------------|------:|:-----------
| READYTORUN_VIRTUAL_OVERRIDE_None                      |  0x00 | No flags are set
| READYTORUN_VIRTUAL_OVERRIDE_VirtualFunctionOverridden  |  0x01 | If set, then the virtual function has an implementation, which is encoded in the optional method implementation signature.

#### IL Body signatures

ECMA 335 does not define a format that can represent the exact implementation of a method by itself. This signature holds all of the IL of the method, the EH table, the locals table, and each token (other than type references) in those tables is replaced with an index into a local stream of signatures. Those signatures are simply verbatim copies of the needed metadata to describe MemberRefs, TypeSpecs, MethodSpecs, StandaloneSignatures and strings. All of that is bundled into a large byte array. In addition, a series of TypeSignatures follows which allow the type references to be resolved, as well as a methodreference to the uninstantiated method. Assuming all of this matches with the data that is present at runtime, the fixup is considered to be satisfied. See ReadyToRunStandaloneMetadata.cs for the exact details of the format.

### READYTORUN_IMPORT_SECTIONS::AuxiliaryData

For slots resolved lazily via `READYTORUN_HELPER_DelayLoad_MethodCall` helper, auxiliary data are
compressed argument maps that allow precise GC stack scanning while the helper is running. The CoreCLR runtime class [`GCRefMapDecoder`](https://github.com/dotnet/runtime/blob/69e114c1abf91241a0eeecf1ecceab4711b8aa62/src/coreclr/inc/gcrefmap.h#L158) is used to parse this information. This data would not be required for runtimes that allow conservative stack scanning.

The auxiliary data table contains the exact same number of GC ref map records as there are method entries in the import section. To accelerate GC ref map lookup, the auxiliary data section starts with a lookup table holding the offset of every 1024-th method in the runtime function table within the linearized GC ref map.

|     Offset in auxiliary data | Size | Content
|-----------------------------:|-----:|:-------
|                            0 |    4 | Offset to GC ref map info for method #0 relative to this byte i.e. 4 * (MethodCount / 1024 + 1)
|                            4 |    4 | Offset to GC ref map info for method #1024
|                            8 |    4 | Offset to GC ref map info for method #2048
|                          ... |      |
| 4 * (MethodCount / 1024 + 1) |  ... | Serialized GC ref map info

The GCRef map is used to encode GC type of arguments for callsites. Logically, it is a sequence `<pos, token>` where `pos` is
position of the reference in the stack frame and `token` is type of GC reference (one of [`GCREFMAP_XXX`](https://github.com/dotnet/runtime/blob/69e114c1abf91241a0eeecf1ecceab4711b8aa62/src/coreclr/inc/corcompile.h#L633) values):

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

## ReadyToRunSectionType.RuntimeFunctions

This section contains sorted array of `RUNTIME_FUNCTION` entries that describe all code blocks in the image with pointers to their unwind info.
Despite the name, these code block might represent a method body, or it could be just a part of it (e.g. a funclet) that requires its own unwind data.
The standard Windows xdata/pdata format is used.
ARM format is used for x86 to compensate for the lack of x86 unwind info standard.
The unwind info blob is immediately followed by the GC info blob. The encoding slightly differs for amd64
which encodes an extra 4-byte representing the end RVA of the unwind info blob.

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

## ReadyToRunSectionType.MethodDefEntryPoints

This section contains a native format sparse array (see 4 Native Format) that maps methoddef rows to
method entrypoints. Methoddef is used as index into the array. The element of the array is index of the
method in `RuntimeFunctions`, followed by list of slots that need to be filled before the method
can start executing.

The index of the method is left-shifted by 1 bit with the low bit indicating whether a list of slots
to fix up follows. The list of slots is encoded as follows (same encoding as used by NGen):

```
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
```

The fixup list is a stream of integers encoded as nibbles (1 nibble = 4 bits). 3 bits of a nibble are used to
store 3 bits of the value, and the top bit indicates if the following nibble contains rest of the value. If the
top bit in the nibble is set, then the value continues in the next nibble.

The section and slot indices are delta-encoded offsets from that initial absolute index.  Delta-encoded
means that the i-th value is the sum of values [1..i].

The list is terminated by a 0 (0 is not meaningful as valid delta).

**Note:** This is a per-assembly section. In single-file R2R files, it is pointed to directly by the
main R2R header; in composite R2R files, each component module has its own entrypoint section pointed to
by the `READYTORUN_SECTION_ASSEMBLIES_ENTRY` core header structure.

## ReadyToRunSectionType.ExceptionInfo

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

## ReadyToRunSectionType.DebugInfo

This section contains information to support debugging: native offset and local variable maps.

**TODO**: Document the debug info encoding. It is the same encoding as used by NGen. It should not be
required when debuggers are able to handle debug info stored separately.

## ReadyToRunSectionType.DelayLoadMethodCallThunks

This section marks region that contains thunks for `READYTORUN_HELPER_DelayLoad_MethodCall`
helper. It is used by debugger for step-in into lazily resolved calls. It should not be required when
debuggers are able to handle debug info stored separately.

## ReadyToRunSectionType.AvailableTypes

This section contains a native hashtable of all defined & export types within the compilation module. The key is the full type name, the value is the exported type or defined type token row ID left-shifted by one and or-ed with bit 0 defining the token type:

| Bit value | Token type
|----------:|:----------
|         0 | defined type
|         1 | exported type

The version-resilient hashing algorithm used for hashing the type names is implemented in
[vm/versionresilienthashcode.cpp](https://github.com/dotnet/runtime/blob/69e114c1abf91241a0eeecf1ecceab4711b8aa62/src/coreclr/vm/versionresilienthashcode.cpp#L74).

**Note:** This is a per-assembly section. In single-file R2R files, it is pointed to directly by the
main R2R header; in composite R2R files, each component module has its own available type section pointed to
by the `READYTORUN_SECTION_ASSEMBLIES_ENTRY` core header structure.

## ReadyToRunSectionType.InstanceMethodEntryPoints

This section contains a native hashtable of all generic method instantiations compiled into
the R2R executable. The key is the method instance signature; the appropriate version-resilient
hash code calculation is implemented in
[vm/versionresilienthashcode.cpp](https://github.com/dotnet/runtime/blob/69e114c1abf91241a0eeecf1ecceab4711b8aa62/src/coreclr/vm/versionresilienthashcode.cpp#L126);
the value, represented by the `EntryPointWithBlobVertex` class, stores the method index in the
runtime function table, the fixups blob and a blob encoding the method signature.

**Note:** In contrast to non-generic method entrypoints, this section is image-wide for
composite R2R images. It represents all generics needed by all assemblies within the composite
executable. As mentioned elsewhere in this document, CoreCLR runtime requires changes to
properly look up methods stored in this section in the composite R2R case.

**Note:** Generic methods and non-generic methods on generic types are encoded into this table
and the runtime is expected to lookup into this table in potentially multiple modules. First the
runtime is expected to lookup into this table for the module which defines the method, then it is
expected to use the "alternate" generics location which is defined as the module which is NOT the
defining module which is the defining module of one of the generic arguments to the method. This
alternate lookup is not currently a deeply nested algorithm. If that lookup fails, then lookup
will proceed to every module which specified `READYTORUN_FLAG_UNRELATED_R2R_CODE` as a flag.

## ReadyToRunSectionType.InliningInfo (v2.1+)

**TODO**: document inlining info encoding

## ReadyToRunSectionType.ProfileDataInfo (v2.2+)

**TODO**: document profile data encoding

## ReadyToRunSectionType.ManifestMetadata (v2.3+ with changes for v6.3+)

Manifest metadata is an [ECMA-335] metadata blob containing extra reference assemblies within
the version bubble introduced by inlining on top of assembly references stored in the input MSIL.
As of R2R version 3.1, the metadata is only used for the AssemblyRef table. This is used to
translate module override indices in signatures to the actual reference modules (using either
the `READYTORUN_FIXUP_ModuleOverride` bit flag on the signature fixup byte or the
`ELEMENT_TYPE_MODULE_ZAPSIG` COR element type).

**Note:** It doesn't make sense to use references to assemblies external to the version bubble
in the manifest metadata via the `READYTORUN_FIXUP_ModuleOverride` or `ELEMENT_TYPE_MODULE_ZAPSIG` concept
as there's no guarantee that their metadata token values remain constant; thus we cannot encode signatures relative to them.
However, as of R2R version 6.3, the native manifest metadata may contain tokens to be further resolved to actual
implementation assemblies.

The module override index translation algorithm is as follows (**ILAR** = *the number of `AssemblyRef` rows in the input MSIL*):

For R2R version 6.2 and below

| Module override index (*i*) | Reference assembly
|:----------------------------|:------------------
| *i* = 0                     | Global context - assembly containing the signature
| 1 <= *i* <= **ILAR**        | *i* is the index into the MSIL `AssemblyRef` table
| *i* > **ILAR**              | *i* - **ILAR** - 1 is the zero-based index into the `AssemblyRef` table in the manifest metadata

**Note:** This means that the entry corresponding to *i* = **ILAR** + 1 is actually undefined as it corresponds to the `NULL` entry (ROWID #0) in the manifest metadata AssemblyRef table. The first meaningful index into the manifest metadata, *i* = **ILAR** + 2, corresponding to ROWID #1, is historically filled in by Crossgen with the input assembly info but this shouldn't be depended upon, in fact the input assembly is useless in the manifest metadata as the module override to it can be encoded by using the special index 0.

For R2R version 6.3 and above
| Module override index (*i*) | Reference assembly
|:----------------------------|:------------------
| *i* = 0                     | Global context - assembly containing the signature
| 1 <= *i* <= **ILAR**        | *i* is the index into the MSIL `AssemblyRef` table
| *i* = **ILAR** + 1          | *i* is the index which refers to the Manifest metadata itself
| *i* > **ILAR** + 1          | *i* - **ILAR** - 2 is the zero-based index into the `AssemblyRef` table in the manifest metadata

In addition, a ModuleRef within the module which refers to `System.Private.CoreLib` may be used to serve as the *ResolutionContext* of a *TypeRef* within the manifest metadata. This will always refer to the module which contains the `System.Object` type.

## ReadyToRunSectionType.AttributePresence (v3.1+)

**TODO**: document attribute presence encoding

**Note:** This is a per-assembly section. In single-file R2R files, it is pointed to directly by the
main R2R header; in composite R2R files, each component module has its own attribute presence
section pointed to by the `READYTORUN_SECTION_ASSEMBLIES_ENTRY` core header structure.

## ReadyToRunSectionType.InliningInfo2 (v4.1+)

The inlining information section captures what methods got inlined into other methods. It consists of a single _Native Format Hashtable_ (described below).

The entries in the hashtable are lists of inliners for each inlinee. One entry in the hashtable corresponds to one inlinee. The hashtable is hashed by hashcode of the module name XORed with inlinee RID.

The entry of the hashtable is a counted sequence of compressed unsigned integers:

* RID of the inlinee shifted left by one bit. If the lowest bit is set, this is an inlinee from a foreign module. The _module override index_ (as defined above) follows as another compressed unsigned integer in that case.
* RIDs of the inliners follow. They are encoded similarly to the way the inlinee is encoded (shifted left with the lowest bit indicating foreign RID). Instead of encoding the RID directly, RID delta (the difference between the previous RID and the current RID) is encoded. This allows better integer compression.

Foreign RIDs are only present if a fragile inlining was allowed at compile time.

**TODO:** It remains to be seen whether `DelayLoadMethodCallThunks` and / or
`InliningInfo` also require changes specific to the composite R2R file format.

## ReadyToRunSectionType.ComponentAssemblies (v4.1+)

This image-wide section is only present in the main R2R header of composite R2R files. It is an
array of the entries `READYTORUN_SECTION_ASSEMBLIES_ENTRY` parallel to the indices in the manifest metadata
AssemblyRef table in the sense that it's a linear table where the row indices correspond to the
equivalent AssemblyRef indices. Just like in the AssemblyRef ECMA 335 table, the indexing is
1-based (the first entry in the table corresponds to index 1).

```C++
struct READYTORUN_SECTION_ASSEMBLIES_ENTRY
{
    IMAGE_DATA_DIRECTORY CorHeader;        // Input MSIL metadata COR header (for composite R2R images with embedded MSIL metadata)
    IMAGE_DATA_DIRECTORY ReadyToRunHeader; // READYTORUN_CORE_HEADER of the assembly in question
};
```

## ReadyToRunSectionType.OwnerCompositeExecutable (v4.1+)

For composite R2R executables with standalone MSIL, the MSIL files are rewritten during compilation
by receiving a formal ReadyToRun header with the appropriate signature and major / minor version
pair; in `Flags`, it has the `READYTORUN_FLAG_COMPONENT` bit set and its section list only contains
the `OwnerCompositeExecutable` section that contains a UTF-8 string encoding the file name of the
composite R2R executable this MSIL belongs to with extension (without path). Runtime uses this
information to locate the composite R2R executable with the compiled native code when loading the MSIL.

## ReadyToRunSectionType.PgoInstrumentationData (v5.2+)

**TODO**: document PGO instrumentation data

## ReadyToRunSectionType.ManifestAssemblyMvids (v5.3+)

This section is a binary array of 16-byte MVID records, one for each assembly in the manifest metadata.
Number of assemblies stored in the manifest metadata is equal to the number of MVID records in the array.
MVID records are used at runtime to verify that the assemblies loaded match those referenced by the
manifest metadata representing the versioning bubble.

## ReadyToRunSectionType.CrossModuleInlineInfo (v6.3+)
The inlining information section captures what methods got inlined into other methods. It consists of a single _Native Format Hashtable_ (described below).

The entries in the hashtable are lists of inliners for each inlinee. One entry in the hashtable corresponds to one inlinee. The hashtable is hashed with the version resilient hashcode of the uninstantiated methoddef inlinee.

The entry of the hashtable is a counted sequence of compressed unsigned integers which begins with an InlineeIndex which combines a 30 bit index with 2 bits of flags which how the sequence of inliners shall be parsed and what table is to be indexed into to find the inlinee.

* InlineeIndex
  * Index with 2 flags field in lowest 2 bits to define the inlinee
    - If (flags & 1) == 0 then index is a MethodDef RID, and if the module is a composite image, a module index of the method follows
    - If (flags & 1) == 1, then index is an index into the ILBody import section
    - If (flags & 2) == 0 then inliner list is:
      - Inliner RID deltas - See definition below
    - if (flags & 2) == 2 then what follows is:
      - count of delta encoded indices into the ILBody import section
      - the sequence of delta encoded indices into the first import section with a type of READYTORUN_IMPORT_SECTION_TYPE_ILBODYFIXUPS
      - Inliner RID deltas - See definition below

* Inliner RID deltas (for multi-module version bubble images specified by the module having the READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE flag set)
  - a sequence of inliner RID deltas with flag in the lowest bit
  - if flag is set, the inliner RID is followed by a module ID
  - otherwise the module is the same as the module of the inlinee method
* Inliner RID deltas (for single module version bubble images)
  - a sequence of inliner RID deltas

This section may be included in addition to a InliningInfo2 section.

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

Conceptually, a native hash table is a header that describe the dimensions of the table, a table that maps hash values of the keys to buckets followed with a list of buckets that store the values. These three things are stored consecutively in the format.

To make look up fast, the number of buckets is always a power of 2. The table is simply a sequence of `(1 + number of buckets)` cells, for the first `(number of buckets)` cells, its stores the offset of the bucket list from the beginning of the whole native hash table. The last cell stores the offset to the end of the buckets.

Each bucket is a sequence of entries. An entry has a hash code and an offset to the object stored. The entries are sorted by hash code.

Physically, the header is a single byte. The most significant six bits is used to store the number of buckets in its base-2 logarithm. The remaining two bits are used for storing the entry size, as explained below:

Because the offsets to the bucket lists are often small numbers, the table cells are variable sized.
It could be either 1 byte, 2 bytes or 4 bytes. The three cases are described with two bits. `00` means it is one byte, `01` means it is two bytes and `10` means it is four bytes.

The remaining data are the entries. The entries has only the least significant byte of the hash code, followed by the offset to the actual object stored in the hash table.

To perform a lookup, one starts with reading the header, computing the hash code, using the number of buckets to determine the number of bits to mask away from the hash code, look it up in the table using the right pointer size, find the bucket list, find the next bucket list (or the end of the table) so that we know where to stop, search the entries in that list and then we will find the object if we have a hit, or we have a miss.

To enumerate all the values, simply walk from the first entry and go all the way to the end of the hash table.

To see this in action, we can take a look at the following example, with these objects placed in the native hash table.

| Object | HashCode |
|:-------|:--------:|
| P      | 0x1231   |
| Q      | 0x1232   |
| R      | 0x1234   |
| S      | 0x1238   |

Suppose we decided to have only two buckets, then only the least significant digit will be used to index the table, the whole hash table will look like this:

| Part    | Offset | Content  | Meaning                                                                                                                                                                                   |
|:--------|:-------|:--------:|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Header  | 0      | 0x04     | This is the header, the least significant bit is `00`, therefore the table cell is just one byte. The most significant six bit represents 1, which means the number of buckets is 2^1 = 2. |
| Table   | 1      | 0x08     | This is the representation of the unsigned integer 4, which correspond to the offset of the bucket correspond to hash code `0`.                                                           |
| Table   | 2      | 0x14     | This is the representation of the unsigned integer 10, which correspond to the offset of the bucket correspond to hash code `1`.                                                          |
| Table   | 3      | 0x18     | This is the representation of the unsigned integer 12, which correspond to the offset of the end of the whole hash table.                                                                 |
| Bucket1 | 4      | 0x32     | This is the least significant byte of the hash code of P                                                                                                                                  |
| Bucket1 | 5      | P        | This should be the offset to the object P                                                                                                                                                 |
| Bucket1 | 6      | 0x34     | This is the least significant byte of the hash code of Q                                                                                                                                  |
| Bucket1 | 7      | Q        | This should be the offset to the object Q                                                                                                                                                 |
| Bucket1 | 8      | 0x38     | This is the least significant byte of the hash code of R                                                                                                                                  |
| Bucket1 | 9      | R        | This should be the offset to the object R                                                                                                                                                 |
| Bucket2 | 10     | 0x31     | This is the least significant byte of the hash code of S                                                                                                                                  |
| Bucket2 | 11     | S        | This should be the offset to the object S                                                                                                                                                 |



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
    READYTORUN_HELPER_IsInstanceOfException     = 0x69,
    READYTORUN_HELPER_NewMaybeFrozenArray       = 0x6A,
    READYTORUN_HELPER_NewMaybeFrozenObject      = 0x6B,

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
    // Personality routines
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
