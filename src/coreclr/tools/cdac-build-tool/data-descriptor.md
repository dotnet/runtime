## On-disk target object binary blob descriptor

### Summary

This is an internal implemetnation detail allowing tooling to read target architecture structure sizes and offsets without understaing target architecture object formats.

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
* The physical blob must be round-trippable: it should be possible to extract the blob from an
  object file and write it back out as C source code that compiles back to a logically equivalent
  blob.

### Summary

The binary blob format for a physical descriptor is expected to be stored in the memory space of a
target .NET runtime and is thus likely to be the final descriptor in the "baseline" graph.

It is likely that the physical descriptor will be a part of a build-time constant in the disk image
of a .NET runtime, and the format is designed to be compact and specifiable as a (likely
machine-generated) compile-time constant in a suitable source language.

The data descriptor forms one part of an overall physical data contract descriptor in a target .NET
runtime and as such this format does not specify a "magic number", or "well known symbol", or
another means of identifying the blob within a target process.  Additionally the version of the
binary blob data descriptor is expected to be stored within the larger enclosing data contract
descriptor and is not included here.

### Blob

Multi-byte values are in the target platform endianness.

The format is:

```c

struct TypeSpec;
struct FieldSpec;
struct GlobalLiteralSpec;
struct GlobalPointerSpec;

struct BinaryBlobDataDescriptor
{
    char Magic[8];
    struct Directory {
        uint32_t BaselineStart;
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
    uint32_t BaselineName;
    struct TypeSpec Types[CDacBlobTypesCount];
    struct FieldSpec FieldPool[CDacBlobFieldPoolCount];
    struct GlobalLiteralSpec GlobalLiteralValues[CDacBlobGlobalLiteralsCount];
    struct GlobalPointerSpec GlobalPointerValues[CDacBlobGlobalPointersCount];
    uint8_t NamesPool[sizeof(struct CDacStringPoolSizes)];
    uint8_t EndMagic[4];
};

struct TypeSpec
{
    uint32_t Name;
    uint32_t Fields;
    uint16_t Size;
};

struct FieldSpec
{
    uint32_t Name;
    uint32_t TypeName;
    uint16_t FieldOffset;
};

struct GlobalLiteralSpec
{
    uint32_t Name;
    uint32_t TypeName;
    uint64_t Value;
};

struct GlobalPointerSpec
{
    uint32_t Name;
    uint32_t AuxIndex;
};
```

where the magic value is `"DACBLOB"` and `EndMagic` is `{0x01, 0x02, 0x03, 0x04}`

The blob begins with a directory that gives the relative offsets of the `Types`, `FieldPool`,
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
field's offset.  The field type is not given.

Rationale: it is expected that the types of the fields were provided by a "baseline" data descriptor.

The globals are gives as a sequence of `GlobalSpec` elements.  Each global has a name and a value.
The types of the globals are not given.

Rationale: it is expected that the types of the global values were provided by a "baseline" data descriptor.

The `NamesPool` is a single sequence of utf-8 bytes comprising the concatenation of all the type
field and global names including a terminating nul byte for each name.  The same name may occur
multiple times.  The names will be referenced by multiple type or multiple fields. (That is, a
clever blob emitter may pool strings).  The first name in the name pool is the empty string (with
its nul byte).

Rationale: we want to reserve the offset 0 as a marker.

Names are referenced by giving their offset from the beginning of the `NamesPool`.  Each name
extends until the first nul byte encountered at or past the beginning of the name.


## Example

An example C header describing some data types is given in [sample.data.h](./sample/sample.data.h). And
example series of C macro preprocessor definitions that produces a constant blob `Blob` is given in
[sample.blob.c](./sample/sample.blob.c)
