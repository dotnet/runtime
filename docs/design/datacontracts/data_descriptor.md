# Data Descriptors

The [data contract](datacontracts_design.md) specification for .NET depends on each target .NET
runtime describing a subset of its platform- and build-specific data structures to diagnostic
tooling.  The information is given meaning by algorithmic contracts that describe how the low-level
layout of the memory of a .NET process corresponds to high-level abstract data structures that
represent the conceptual state of a .NET process.

In this document we give a logical description of a data descriptor together with two physical
manifestations.

The first physical format is used to publish well-known data descriptors in the `dotnet/runtime`
repository.  It is supposed to be machine- and human-readable.  This format is not meant to be
particularly concise and may be used for visualization, diagnostics, etc.  Typically data
descriptors in this form may be written by hand or with the aid of tooling.

The second physical format is used to embed a data descriptor blob within a particularly instance of
a target runtime.  It is meant to be machine-readable while minimizing the total space needed to
store it.  It is primarily meant to be read and written by tooling.

## Logical descriptor

Each logical descriptor exists within an implied /target architecture/ consisiting of:
* target architecture endianness (little endian or big endian)
* target architecture pointer size (4 bytes or 8 bytes)

The following /primitive types/ are assumed: int8, uint8, int16, uint16, int32, uint32, int64,
uint64, nint, nuint, pointer.  The multi-byte types are in the target architecture
endianness.  The types `nint`, `nuint` and `pointer` have target architecture pointer size.

The data descriptor consists of:
* the data descriptor specification version
* a collection of type structure descriptors
* a collection of global value descriptors.

## Data descriptor specification version

This is the version of the physical data descriptor.

## Types

The types (both primitive types and structures described by structure descriptors) are classified as
having either determinate or indeterminate size.  Determinate sizes may be used for pointer
arithmetic.  Types with indeterminate size may not be.  Note that some sizes may be determinate, but
/target specific/.  For example pointer types have a fixed size that varies by architecture.

## Structure descriptors

Each structure descriptor consists of:
* a name
* an optional size in bytes
* a collection of field descriptors

If the size is not given, the type has indeterminate size.  The size may also be given explicitly as
"indeterminate" to emphasize that the type has indeterminate size.

The collection of field descriptors may be empty.  In that case the type is opaque.  The primitive
types may be thought of as opaque (for example: on ARM64 `nuint` is an opaque 8 byte type, `int64`
is another opaque 8 byte type. `string` is an opaque type of indeterminate size).

Type names must be globally unique within a single logical descriptor.

### Field descriptors

Each field descriptor consists of:
* a name
* an offset in bytes from the beginning of the struct

The name of a field descriptor must be unique within the definition of a structure.

Two or more fields may have the same offsets or imply that the underlying fields overlap.  The field
offsets need not be aligned using any sort of target-specific alignment rules.

Each field's type may refer to one of the primitive types or to any other type defined in the logical descriptor.

If a structure descriptor contains at least one field of indeterminate size, the whole structure
must have indeterminate size.  Tooling is not required to, but may, signal a warning if a descriptor
has a determinate size and contains indeterminate size fields.

It is expected that tooling will signal a warning if a field specifies a type that does not appear
in the logical descriptor.

## Global value descriptors

Each global value descriptor consists of:
* a name
* a type
* a value

The name of each global value must be unique within the logical descriptor.

The type must be one of the determinate-size primitive types.

The value must be an integral constant within the range of its type.  Signed values use the target's
natural encoding.  Pointer values need not be aligned and need not point to addressable target
memory.


## Physical descriptors

The physical descriptors are meant to describe /subsets/ of a logical descriptor and to compose.
Each physical descriptor can name an ordered sequence of zero or more "baseline" descriptor which is then
considered to comprise a piece of the overall logical descriptor.

Starting from a single physical descriptor, the "baseline" relationship forms a directed graph.  It
is an error for the graph to contain a cycle. The baseline relationship may form a DAG (that is: two
or more nodes may refer to the same baseline).

When constructing the logical descriptor, the DAG is traversed in a post-order traversal with each
node visited at most once with baselines of a particular node visited from first to last.

To form the logical descriptor the types are added in traversal order with later appearances
augmenting earlier ones (fields are added or modified, sizes and offsets are overwritten).  The
global values are added in traversal order with later appearances overwriting previous ones.

Rationale: if a baseline is included more than once, only the first inclusion counts.  If a type
appears in multiple physical descriptors, the later appearances may add more fields or change the
offsets or definite/indefinite sizes of prior definitions.  If a value appears multipel times, later
definitions take precedence.

**FIXME** do we really want a DAG? Are we ok with a linked list?

## Physical JSON descriptor

### Version

This is version 0 of the physical descriptor

### Summary

A data descriptor may be stored in the "JSON with comments" format.

The toplevel dictionary will contain:

* `"version": 0`
* `"baseline": "FREEFORM STRING"` or `baseline: ["FREEFORM STRING"", ...]`
* `"types": TYPE_ARRAY` see below
* `"globals": VALUE_ARRAY` see below

### Types

The types will be in an array, with each type described by a dictionary containining keys:

* `"name": "type name"` the name of each type
* optional `"size": int | "indeterminate"` if omitted the size is indeterminate
* optional `"fields": FIELD_ARRAY` if omitted same as a field array of length zero

Each `FIELD_ARRAY` is an array of dictionaries each containing keys:

* `"name": "field name"` the name of each field
* `"type": "type name"` the name of a primitive type or another type defined in the same /logical/ descriptor
* optional `"offset": int | "unknown"` the offset of the field or "unknown". If omitted, same as "unknown".

Note that the logical descriptor does not contain "unknown" offsets.

Rationale: "unknown" offsets may be used to document in the physical JSON descriptor that another
physical descriptor in the "baseline" graph is expected to provide the offset of the field.

### Global values

The global values will be in an array, with each value described by a dictionary containing keys:

* `"name": "global value name"` the name of the global value
* `"type": "type name"` the type of the global value
* optional `"value": VALUE | "unknown"` the value of the global value or "unknown". If omitted, same as "unknown".

Note that the logical descriptor does not contain "unknown" values.

The `VALUE` may be a JSON numeric constant integer or a string containing a signed or unsigned
decimal or hex (with prefix `0x` or `0X`) integer constant.  The constant must be within the range
of the type of the global value.

For pointer and nuint globals, the value may be assumed to fit in a 64-bit unsigned integer.  For
nint globals, the value may be assumed to fit in a 64-bit signed integer.

If the value is specified as "unknown" another physical descriptor in the "baseline" graph is
expected to provide the value.

## Physical binary blob descriptor

### Version

This is version 0 of the physical binary blob format.

### Design requirements

The design of the physical binary blob descriptor is constrained by the following requirements:
* The binary blob should be easy to process by examining an object file on disk - even if the object
  file is for a foreign architecture/OS.  It should be possible to read the binary blob purely by
  looking at the bytes.  Tooling should be able to analyze the blob without having to understand
  relocation entries, dwarf debug info, symbols etc.
* It should be possible to produce the blob using the native C/C++/NativeAOT compiler for a given
  target/architecture.  In particular for a runtime written in C, the binary blob should be
  constructible using C idioms.  If the C compiler needs to pad or align the data, the blob format
  should provide a way to iterate the blob contents without having to know anything abotu the target
  platform ABI or C compiler conventions.

This leads to the following overall strategy for the design:
* The physical blob is "self-contained": using pointers would mean that the encoding of the blob
  would have relocations applied to it, which would preclude reading the blob out of of an object
  file without understanding the object file format.
* The physical blob must be "self-describing": If the C compiler adds padding or alignment, the blob
  descriptor must contain information for how to skip the pading/alignment data.
* The physical blob must be constructible using "lowest common denominator" target toolchain
  tooling - the C preprocessor.  That doesn't mean that tooling _must_ use the C preprocessor to
  generate the blob, but the format must not exceed the capabilities of the C preprocessor.

### Summary

The binary blob format for a physical descriptor is expected to be stored in the memory space of a
target .NET runtime and is thus likely to be the final descriptor in the "baseline" graph.

It is likely that the physical descriptor will be a part of a build-time constant in the disk image
of a .NET runtime, and the format is designed to be compact and specifiable as a (likely
machine-generated) compile-time constant in a suitable source language.

The data descriptor forms one part of an overall physical data contract descriptor in a targret .NET
runtime and as such this format does not specify a "magic number", or "well known symbol", or
another means of identifying the blob within a target process.  Additionally the version of the
binary blob data descriptor is expected to be stored within the larger enclosing data contract
descriptor and is not included here.

### Blob

Multi-byte values are in the target platform endianness.

The format is:

```c
struct BinaryBlobDataDescriptor
{
    struct Directory {
        uint32_t TypesStart;
        uint32_t FieldPoolStart;
        
        uint32_t GlobalValuesStart;
        uint32_t NamesStart;
        
        uint32_t TypeCount;
        uint32_t FieldPoolCount;
        
        uint32_t NamesPoolCount;
        
        uint8_t TypeSpecSize;
        uint8_t FieldSpecSize;
        uint8_t GlobalSpecSize;
        uint8_t Reserved0;
    }
    uint32_t BaselineName;
    TypeSpec[TypeCount] Types;
    FieldSpec[FieldPoolCount] FieldPool;
    GlobalSpec[GlobalsCount] GlobalValues;
    uint8_t[NamesPoolCount] NamesPool;
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

struct GlobalSpec
{
    uint32_t Name;
    uint32_t TypeName;
    uint64_t Value;
};
```

The blob begins with a directory that gives the relative offsets of the `Types`, `FieldPool`,
`GlobalValues` and `Names` fields of the blob.  The number of elements of each of the arrays is
next. This is followed by the sizes of the `TypeSpec`, `FieldSpec` and `GlobalSpec` structs.

Rationale: If a `BinaryBlobDataDescriptor` is created via C macros, we want to embed the `offsetof`
and `sizeof` of the components of the blob into the blob itself without having to account for any
padding that the C compiler may introduce to enfore alignment.  Additionally the `Directory` tries
to follow a common C alignment rule (we don't want padding introduced in the directory itself):
N-byte members are aligned to start on N-byte boundaries.

The baseline is specified as an offset into the names pool.

The types are given as an array of `TypeSpec` elements.  Each one contains an offset into the
`NamesPool` giving the name of the type, An offset into the fields pool indicating the first
specified field of the type, and the size of the type in bytes or 0 if it is indeterminate.

The fields pool is given as a sequence of `FieldSpec` elements.  The fields for each type are given
in a contiguious subsequence and are terminated by a marker `FieldSpec` with a `Name` offset of 0.
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

And example C header describing some data types is given in [sample.data.h](./sample.data.h). And
example series of C macro preprocessor definitions that produces a constant blob `Blob` is given in
[sample.blob.c](./sample.blob.c)
