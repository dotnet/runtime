## On-disk target object binary blob descriptor

### Summary

This is an internal implemetnation detail allowing tooling to read target architecture structure sizes and offsets without understanding target architecture object formats.

### Design requirements

The design of the physical binary blob descriptor is constrained by the following requirements:
* The binary blob should be easy to process by examining an object file on disk - even if the object
  file is for a foreign architecture/OS.  It should be possible to read the binary blob purely by
  looking at the bytes.  Tooling should be able to analyze the blob without having to understand
  relocation entries, dwarf debug info, symbols etc.
* It should be possible to produce the blob using the native C/C++/NativeAOT compiler for a given
  target/architecture.  In particular for a runtime written in C, the binary blob should be
  constructible using C idioms.  If the C compiler needs to pad or align the data, the blob format
  should provide a way to iterate the blob contents without having to know anything about the target
  platform ABI or C compiler conventions.
* It should be possible to create separate subsets of the physical descriptor (in the target runtime
  object format) using separate toolchains (for example: in NativeAOT some of the struct layouts may
  be described by the NativeAOT compiler, while some might be described by the C/C++ toolchain) and
  to run a build host (not target architecture) tool to read and compose them into a single physical
  binary blob before embedding it into the final NativeAOT runtime binary.

This leads to the following overall strategy for the design:
* The physical blob is "self-contained": indirections are encoded as offsets from the beginning of
  the blob (or other base offsets), whereas using pointers would mean that the encoding of the blob
  would have relocations applied to it, which would preclude reading the blob out of of an object
  file without understanding the object file format.
* The physical blob must be "self-describing": If the C compiler adds padding or alignment, the blob
  descriptor must contain information for how to skip the padding/alignment data.
* The physical blob must be constructible using "lowest common denominator" target toolchain
  tooling - the C preprocessor.  That doesn't mean that tooling _must_ use the C preprocessor to
  generate the blob, but the format must not exceed the capabilities of the C preprocessor.


### Blob

Multi-byte values are in the target platform endianness.

The blob's job is to encode descriptions of the .NET runtime's implementation types and their fields,
as well as globals.

When encoding strings, we create a "string pool" in the data blob: a massive string literal
that concatentates all the names that we might need, separated by `"\0"` nul characters.  To encode a name into another data structure, we write the offset of the name from the beginning of the string pool.  We reserve the offset 0 to designate empty or invalid names.

When encoding the fields of a type, we create a "field pool" in the data blob: a collection of field
descriptors delimited by an "empty field descriptor" (a field descriptor of a name index of 0).  All
the fields for a single type are encoded as a contiguous run from a given field pool index until the next empty field descriptor.

We're interested in encoding the following kinds of information:

```c
// A type:
// We encode a data contract name and a collection of fields, and the size of the type.
struct TypeSpec
{
    uint32_t Name;
    uint32_t Fields;
    uint16_t Size;
};

// A field:
// We encode the field name, the type (or an empty name) and the offset of the field in the native
// struct. The size of the field is not part of the data descriptor.
struct FieldSpec
{
    uint32_t Name;
    uint32_t TypeName;
    uint16_t FieldOffset;
};

// A literal global value such as a constant, some flags bitmap, or the value of a preprocessor define:
// we record the name, an optional type name, and a value as an unsigned 64-bit value
struct GlobalLiteralSpec
{
    uint32_t Name;
    uint32_t TypeName;
    uint64_t Value;
};

// A global pointer value such as the addrress of some important datastructure:
// We record the name and the index of the global in the auxiliarly "pointer data" global which
// is compiled into the .NET runtime and contains the addresses of all the globals that are referenced
// from the data descriptor.
struct GlobalPointerSpec
{
    uint32_t Name;
    uint32_t PointerDataIndex;
};
```

The main data we want to emit to the object file is an instance of the following structure:

```c
// The main payload of the object file.
struct BinaryBlobDataDescriptor
{
    // A directory giving the offsets of all the other content,
    // the number of types, fields, global literals and pointers, and
    // the sizes of the "Spec" structs, above, in order to account for any padding added
    // by the C/C++ compiler.
    struct Directory {
        uint32_t FlagsAndBaselineStart;
        uint32_t TypesStart;

        uint32_t FieldPoolStart;
        uint32_t GlobalLiteralValuesStart;

        uint32_t GlobalPointersStart;
        uint32_t NamesStart;

        uint32_t TypeCount;
        uint32_t FieldPoolCount;

        uint32_t GlobalLiteralValuesCount;
        uint32_t GlobalPointerValuesCount;

        uint32_t NamesPoolCount;

        uint8_t TypeSpecSize;
        uint8_t FieldSpecSize;
        uint8_t GlobalLiteralSpecSize;
        uint8_t GlobalPointerSpecSize;
    } Directory;
    // Platform flags (primarily pointer size)
    uint32_t PlatformFlags;
    // a well-known name of the baseline data descriptor. the current descriptor
    // records changes from this baseline.
    uint32_t BaselineName;
    // an array of type specs
    struct TypeSpec Types[CDacBlobTypesCount];
    // all of the field specs - contiguous runs are all owned by the same type
    struct FieldSpec FieldPool[CDacBlobFieldPoolCount];
    // an array of literal globals
    struct GlobalLiteralSpec GlobalLiteralValues[CDacBlobGlobalLiteralsCount];
    // an array of pointer globals
    struct GlobalPointerSpec GlobalPointerValues[CDacBlobGlobalPointersCount];
    // all of the names that might be referenced from elsewhere in BinaryBlobDataDescriptor,
    // delimited by "\0"
    uint8_t NamesPool[sizeof(struct CDacStringPoolSizes)];
    // an end magic value to validate that the name pool is of the expected length
    uint8_t EndMagic[4]; // the bytes 0x01 0x02 0x03 0x04
};
```

Finally, the value that we write to the object file has this form:

```c
struct MagicAndBlob {
    // the magic value that we look for in the object file
    // 0x00424F4C42434144ull - in little endian this is "DACBLOB\0"
    uint64_t magic;
    // the blob payload, described above
    struct BinaryBlobDataDescriptor Blob;
};
```

The `BinaryBlobDataDescriptor` begins with a directory that gives the relative offsets of the `PlatformFlags`, `Types`, `FieldPool`,
`GlobalLiteralValues`, `GlobalPointerValues` and `Names` fields of the blob.  The number of elements of each of the arrays is
next. This is followed by the sizes of the spec structs.

Rationale: If a `BinaryBlobDataDescriptor` is created via C macros, we want to embed the `offsetof`
and `sizeof` of the components of the blob into the blob itself without having to account for any
padding that the C compiler may introduce to enforce alignment.  Additionally the `Directory` tries
to follow a common C alignment rule (we don't want padding introduced in the directory itself):
N-byte members are aligned to start on N-byte boundaries.

The baseline is specified as an offset into the names pool.

The types are given as an array of `TypeSpec` elements.  Each one contains an offset into the
`NamesPool` giving the name of the type, An offset into the fields pool indicating the first
specified field of the type, and the size of the type in bytes or 0 if it is indeterminate.

The fields pool is given as a sequence of `FieldSpec` elements.  The fields for each type are given
in a contiguous subsequence and are terminated by a marker `FieldSpec` with a `Name` offset of 0.
(Thus if a type has an empty sequence of fields it just points to a marker field spec directly.)
For each field there is a name that gives an offset in the name pool and an offset indicating the
field's offset.

The global constants are given as a sequence of `GlobalLiteralSpec` elements.  Each global has a
name, type and a value.  Globals that are the addresses in target memory, are in `GlobalPointerSpec`
elements. Each pointer element has a name and an index in a separately compiled pointer structure
that is linked into runtime .  See
[contract-descriptor.md](/docs/design/datacontracts/contract-descriptor.md)

The `NamesPool` is a single sequence of utf-8 bytes comprising the concatenation of all the type
field and global names including a terminating nul byte for each name.  The same name may occur
multiple times.  The names could be referenced by multiple type or multiple fields. (That is, a
clever blob emitter may pool strings).  The first name in the name pool is the empty string (with
its nul byte).

Rationale: we want to reserve the offset 0 as a marker.

Names are referenced by giving their offset from the beginning of the `NamesPool`.  Each name
extends until the first nul byte encountered at or past the beginning of the name.


## Example

An example C header describing some data types is given in [sample.data.h](./sample/sample.data.h). And
example series of C macro preprocessor definitions that produces a constant blob `Blob` is given in
[sample.blob.c](./sample/sample.blob.c)
