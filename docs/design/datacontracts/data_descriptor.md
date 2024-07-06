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
human-readable form.  This data may be used for visualization, diagnostics, etc.  These data
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

In the .NET runtime there are two physical descriptors:
* a "baseline" physical data descriptor with a well-known name,
* an in-memory physical data descriptor that resides in the target process' memory

When constructing the logical descriptor, first the baseline physical descriptor is consumed: the
types and values from the baseline are added to the logical descriptor.  Then the types of the
in-memory data descriptor are used to augment the baseline: fields are added or modified, sizes and
offsets are overwritten.  The global values of the in-memory data descriptor are used to augment the
baseline: new globals are added, existing globals are modified by overwriting their types or values.

Rationale: If a type appears in multiple physical descriptors, the later appearances may add more
fields or change the offsets or definite/indefinite sizes of prior definitions.  If a value appears
multiple times, later definitions take precedence.

## Physical JSON descriptor

### Version

This is version 0 of the physical descriptor.

### Summary

A data descriptor may be stored in the "JSON with comments" format.  There are two formats: a
"regular" format and a "compact" format.  The baseline data descriptor may be either regular or
compact.  The in-memory descriptor will typically be compact.

The toplevel dictionary will contain:

* `"version": 0`
* optional `"baseline": "BASELINE_ID"` see below
* `"types": TYPES_DESCRIPTOR` see below
* `"globals": GLOBALS_DESCRIPTOR` see below

Additional toplevel keys may be present. For example, the in-memory data descriptor will contain a
`"contracts"` key (see [contract descriptor](./contract_descriptor.md#Compatible_contracts)) for the
set of compatible contracts.

### Baseline data descriptor identifier

The in-memory descriptor may contain an optional string identifying a well-known baseline
descriptor.  The identifier is an arbitrary string, that could be used, for example to tag a
collection of globals and data structure layouts present in a particular release of a .NET runtime
for a certain architecture (for example `net9.0/coreclr/linux-arm64`).  Global values and data structure
layouts present in the data contract descriptor take precedence over the baseline contract.  This
way variant builds can be specified as a delta over a baseline.  For example, debug builds of
CoreCLR that include additional fields in a `MethodTable` data structure could be based on the same
baseline as Release builds, but with the in-memory data descriptor augmented with new `MethodTable`
fields and additional structure descriptors.

It is not a requirement that the baseline is chosen so that additional "delta" is the smallest
possible size, although for practical purposes that may be desired.

Data descriptors are registered as "well known" by checking them into the main branch of
`dotnet/runtime` in the `docs/design/datacontracts/data/` directory in the JSON format specified
in the [data descriptor spec](./data_descriptor.md#Physical_JSON_Descriptor).  The relative path name (with `/` as the path separator, if any) of the descriptor without
any extension is the identifier.  (for example:
`/docs/design/datacontracts/data/net9.0/coreclr/linux-arm64.json` is the filename for the data
descriptor with identifier `net9.0/coreclr/linux-arm64`)

The baseline descriptors themselves must not have a baseline.

### Types descriptor

**Regular format**:

The types will be in an array, with each type described by a dictionary containing keys:

* `"name": "type name"` the name of each type
* optional `"size": int | "indeterminate"` if omitted the size is indeterminate
* optional `"fields": FIELD_ARRAY` if omitted same as a field array of length zero

Each `FIELD_ARRAY` is an array of dictionaries each containing keys:

* `"name": "field name"` the name of each field
* `"type": "type name"` the name of a primitive type or another type defined in the logical descriptor
* optional `"offset": int | "unknown"` the offset of the field or "unknown". If omitted, same as "unknown".

**Compact format**:

The types will be in a dictionary, with each type name being the key and a `FIELD_DICT` dictionary as a value.

The `FIELD_DICT` will have a field name as a key, or the special name `"!"` as a key.

If a key is `!` the value is an `int` giving the total size of the struct. The key must be omitted
if the size is indeterminate.

If the key is any other string, the value may be one of:

* `[int, "type name"]` giving the type and offset of the field
* `int` giving just the offset of the field with the type left unspecified

Unknown offsets are not supported in the compact format.

Rationale: the compact format is expected to be used for the in-memory data descriptor. In the
common case the field type is known from the baseline descriptor. As a result, a field descriptor
like `"field_name": 36` is the minimum necessary information to be conveyed.  If the field is not
present in the baseline, then `"field_name": [12, "uint16"]` must be used.

**Both formats**:

Note that the logical descriptor does not contain "unknown" offsets: it is expected that the
in-memory data descriptor will augment the baseline with a known offset for all fields in the
baseline.

Rationale: "unknown" offsets may be used to document in the physical JSON descriptor that the
in-memory descriptor is expected to provide the offset of the field.

### Global values

**Regular format**:

The global values will be in an array, with each value described by a dictionary containing keys:

* `"name": "global value name"` the name of the global value
* `"type": "type name"` the type of the global value
* optional `"value": VALUE | [ int ] | "unknown"` the value of the global value, or an offset in an auxiliary array containing the value or "unknown".

The `VALUE` may be a JSON numeric constant integer or a string containing a signed or unsigned
decimal or hex (with prefix `0x` or `0X`) integer constant.  The constant must be within the range
of the type of the global value.

**Compact format**:

The global values will be in a dictionary, with each key being the name of a global and the values being one of:

* `[VALUE | [int], "type name"]` the type and value of a global
* `VALUE | [int]` just the value of a global

As in the regular format, `VALUE` is a numeric constant or a string containing an integer constant.

Note that a two element array is unambiguously "type and value", whereas a one-element array is
unambiguously "indirect value".

**Both formats**

For pointer and nuint globals, the value may be assumed to fit in a 64-bit unsigned integer.  For
nint globals, the value may be assumed to fit in a 64-bit signed integer.

Note that the logical descriptor does not contain "unknown" values: it is expected that the
in-memory data descriptor will augment the baseline with a known offset for all fields in the
baseline.

If the value is given as a single-element array `[ int ]` then the value is stored in an auxiliary
array that is part of the data contract descriptor.  Only in-memory data descriptors may have
indirect values; baseline data descriptors may not have indirect values.

Rationale: This allows tooling to generate the in-memory data descriptor as a single constant
string.  For pointers, the address can be stored at a known offset in an in-proc
array of pointers and the offset written into the constant JSON string.

The indirection array is not part of the data descriptor spec.  It is part of the [contract
descriptor](./contract_descriptor.md#Contract_descriptor).



## Example

This is an example of a baseline descriptor for a 64-bit architecture. Suppose it has the name `"example-64"`

The baseline is given in the "regular" format.

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
    { "name": "FEATURE_COMINTEROP", "type", "uint8", "value": "1"},
    { "name": "s_pThreadStore", "type": "pointer" } // no baseline value
  ]
}
```

The following is an example of an in-memory descriptor that references the above baseline. The in-memory descriptor is in the "compact" format:

```jsonc
{
  "version": "0",
  "baseline": "example-64",
  "types":
  {
    "Thread": { "ThreadId": 32, "ThreadState": 0, "Next": 128 },
    "ThreadStore": { "ThreadCount": 32, "ThreadList": 8 }
  },
  "globals":
  {
    "FEATURE_COMINTEROP": 0,
    "s_pThreadStore": [ 0 ] // indirect from aux data offset 0
  }
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
| FEATURE_COMINTEROP  | uint8   | 0          |
| FEATURE_EH_FUNCLETS | uint8   | 0          |
| s_pThreadStore      | pointer | 0x0100ffe0 |

The `FEATURE_EH_FUNCLETS` global's value comes from the baseline - not the in-memory data
descriptor.  By contrast, `FEATURE_COMINTEROP` comes from the in-memory data descriptor - with the
value embedded directly in the json since it is known at build time and does not vary.  Finally the
value of the pointer `s_pThreadStore` comes from the auxiliary vector's offset 0 since it is an
execution-time value that is only known to the running process.
