# cDAC Build Tool

## Summary

The purpose of `cdac-build-tool` is to generate a `.c` file that contains a JSON cDAC contract descriptor.

It works by processing one or more object files containing data descriptors and zero or more text
files that specify contracts.

## Running

```console
% cdac-build-tool compose [-v] -o contract-descriptor.c -c contracts.txt data-descriptor.o
```

## Specifying data descriptors

The sample in the `sample` dir uses the following syntax (see [sample/sample.data.h](sample/sample.data.h)) to specify the data descriptor:

```c
CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(ManagedThread)
CDAC_TYPE_INDETERMINATE(ManagedThread)
CDAC_TYPE_FIELD(ManagedThread, GCHandle, GCHandle, offsetof(ManagedThread,m_gcHandle))
CDAC_TYPE_FIELD(ManagedThread, pointer, Next, offsetof(ManagedThread,m_next))
CDAC_TYPE_END(ManagedThread)

CDAC_TYPE_BEGIN(GCHandle)
CDAC_TYPE_SIZE(sizeof(intptr_t))
CDAC_TYPE_END(GCHandle)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()
// FIXME: wasm32 doesn't like uint64_t cast from uintptr_t at compile-time
// The right thing to do is to not do pointers using this mechanism since they need to go into
// auxdata anyway.
CDAC_GLOBAL_POINTER(ManagedThreadStore, &g_managedThreadStore)
#if FEATURE_EH_FUNCLETS
CDAC_GLOBAL(FeatureEHFunclets, uint8, 1)
#else
CDAC_GLOBAL(FeatureEHFunclets, uint8, 0)
#endif
CDAC_GLOBAL(SomeMagicNumber, uint32, 42)
CDAC_GLOBALS_END()
```

**TODO**: finish documenting this

The file is included multiple times with the macros variously defined in order to generate the
data descriptor blob.

## Implementation Details

The tool works by scraping the input object file for a special magic value followed by a binary blob
in a particular format.  The tool takes advantage of the fact that most compilers emit a C `const struct D globalData = { INITIALIZER }` as an exact sequence of bytes as long as `D` is composed of only integral types, and fixed-size arrays and structs of integral types, and `INITIALIZER` is a constant initializer.

The tool expects to find the following structure in the file:

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
