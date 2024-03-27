# Data Descriptors

The [data contract](datacontracts_design.md) specification for .NET depends on each target .NET
runtime describing a subset of its platform- and build-specific data structures to diagnostic
tooling.  The information is given meaning by algorithmic contracts that describe how the low-level
layout of the memory of a .NET process corresponds to high-level abstract data structures that
represent the conceptual state of a .NET process.

In this document we give a logical description of a data descriptor together with a physical
manifestation.

The physical format is used for two purposes:

1. To publish well-known data descriptors in the `dotnet/runtime` repository in a machine- and
human-readable form.  This datamay be used for visualization, diagnostics, etc.  These data
descriptors may be written by hand or with the aid of tooling.

2. To embed a data descriptor blob within a particular instance of a target runtime.  The data
descriptor blob will be discovered by diagnostic tooling from the memory of a target process.

## Logical descriptor

Each logical descriptor exists within an implied *target architecture* consisting of:
* target architecture endianness (little endian or big endian)
* target architecture pointer size (4 bytes or 8 bytes)

The following *primitive types* are assumed: int8, uint8, int16, uint16, int32, uint32, int64,
uint64, nint, nuint, pointer.  The multi-byte types are in the target architecture
endianness.  The types `nint`, `nuint` and `pointer` have target architecture pointer size.

The data descriptor consists of:
* a collection of type structure descriptors
* a collection of global value descriptors

## Types

The types (both primitive types and structures described by structure descriptors) are classified as
having either determinate or indeterminate size.  Types with a determinate size may be used for
pointer arithmetic, whereas types with an indeterminate size may not be.  Note that some sizes may
be determinate, but *target specific*.  For example pointer types have a fixed size that varies by
architecture.

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
* a type
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

The physical descriptors are meant to describe *subsets* of a logical descriptor and to compose.

In typical usage we expect to have two physical descriptors that are combined to form the logical descriptor for a target runtime:
* a "baseline" physical descriptor with a well-known name,
* a "binary blob" physical descriptor that is part of the target runtime process' memory

When constructing the logical descriptor, first the baseline physical descriptor is consumed: the
types and values from the baseline are added to the logical descriptor.  Then the types of the
binary blob are used to augment the baseline: fields are added or modified, sizes and offsets are
overwritten.  The global values of the binary blob are used to augment the baseline: new globals are
added, existing globals are modified by overwriting their types or values.

Rationale: If a type appears in multiple physical descriptors, the later appearances may add more
fields or change the offsets or definite/indefinite sizes of prior definitions.  If a value appears
multiple times, later definitions take precedence.

## Physical JSON descriptor

### Version

This is version 0 of the physical descriptor

### Summary

A data descriptor may be stored in the "JSON with comments" format.

The toplevel dictionary will contain:

* `"version": 0`
* `"types": TYPE_ARRAY` see below
* `"globals": VALUE_ARRAY` see below

### Types

The types will be in an array, with each type described by a dictionary containing keys:

* `"name": "type name"` the name of each type
* optional `"size": int | "indeterminate"` if omitted the size is indeterminate
* optional `"fields": FIELD_ARRAY` if omitted same as a field array of length zero

Each `FIELD_ARRAY` is an array of dictionaries each containing keys:

* `"name": "field name"` the name of each field
* `"type": "type name"` the name of a primitive type or another type defined in the logical descriptor
* optional `"offset": int | "unknown"` the offset of the field or "unknown". If omitted, same as "unknown".

Note that the logical descriptor does not contain "unknown" offsets: it is expected that the binary
blob will augment the baseline with a known offset for all fields in the baseline.

Rationale: "unknown" offsets may be used to document in the physical JSON descriptor that the binary
blob descriptor is expected to provide the offset of the field.

### Global values

The global values will be in an array, with each value described by a dictionary containing keys:

* `"name": "global value name"` the name of the global value
* `"type": "type name"` the type of the global value
* optional `"value": VALUE | { "indirect": int } | "unknown"` the value of the global value, or an offset in an auxiliary array containing the value or "unknown".

Note that the logical descriptor does not contain "unknown" values: it is expected that the binary
blob will augment the baseline with a known offset for all fields in the baseline.

The `VALUE` may be a JSON numeric constant integer or a string containing a signed or unsigned
decimal or hex (with prefix `0x` or `0X`) integer constant.  The constant must be within the range
of the type of the global value.

For pointer and nuint globals, the value may be assumed to fit in a 64-bit unsigned integer.  For
nint globals, the value may be assumed to fit in a 64-bit signed integer.

If the value is given as `{"indirect": int}` then the value is stored in an auxiliary array that is
part of the data contrat descriptor.  Only in-memory data descriptors may have indirect values; baseline data descriptors may not have indirect values.

Rationale: This allows tooling to generate the in-memory data descriptor as a single constant
string.  For pointers, the address can be stored at a known offset in an in-proc
array of pointers and the offset written into the constant json string.

The indirection array is not part of the data descriptor spec.  It is expected that the data
contract descriptor will include it. (The data contract descriptor must contain: the data
descriptor, the set of compatible algorithmic contracts, the aux array of globals).

## Example

This is an example of a baseline descriptor for a 64-bit architecture. Suppose it has the name `"example-64"`

```jsonc
{
  "version": 0,
  "types": [
    {
      "name": "GCHandle",
      "size": 8,
      "fields": [
        { "name": "Value", "type": "pointer", "offset": 0 }
      ]
    },
    {
      "name": "Thread",
      "size": "indeterminate",
      "fields": [
        { "name": "ThreadId", "type": "uint32", "offset": "unknown" },
        { "name": "Next", "type": "pointer" }, // offset "unknown" is implied
        { "name": "ThreadState", "type": "uint32" }
      ]
    },
    {
      "name": "ThreadStore",
      "fields": [
        { "name": "ThreadCount", "type": "int32" },
        { "name": "ThreadList", "type": "pointer" }
      ]
    }
  ],
  "globals": [
    { "name": "FEATURE_EH_FUNCLETS", "type": "uint8", "value": "0" }, // baseline defaults value to 0
    { "name": "s_pThreadStore", "type": "pointer" } // no baseline value
  ]
}
```

The following is an example of an in-memory descriptor that references the above baseline:

```jsonc
{
  "version": "0",
  "baseline": "example-64",
  "types": [
    {
      "name": "Thread",
      "fields": [
        { "name": "ThreadId", "offset": 32 },
        { "name": "ThreadState", "offset": 0 },
        { "name": "Next", "offset": 128 }
      ]
    },
    {
      "name": "ThreadStore",
      "fields": [
        { "name": "ThreadCount", "offset": 32 }
        { "name": "ThreadList", "offset": 8 }
      ]
    }
  ],
  "globals": [
    { "name": "s_pThreadStore", "value": { "indirect": 0 } }
  ]
}
```

If the indirect values table has the values `0x0100ffe0` in offset 0, then a possible logical descriptor with the above physical descriptors will have the following types:

| Type        | Size          | Field Name  | Field Type | Field Offset |
| ----------- | ------------- | ----------- | ---------- | ------------ |
| GCHandle    | 8             | Value       | pointer    | 0            |
| Thread      | indeterminate | ThreadState | uint32     | 0            |
|             |               | ThreadId    | uint32     | 32           |
|             |               | Next        | pointer    | 128          |
| ThreadStore | indeterminate | ThreadList  | pointer    | 8            |
|             |               | ThreadCount | int32      | 32           |


And the globals will be:

| Name                | Type    | Value      |
| ------------------- | ------- | ---------- |
| FEATURE_EH_FUNCLETS | uint8   | 0          |
| s_pThreadStore      | pointer | 0x0100ffe0 |
