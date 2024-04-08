#include "common.h"

#include <stdint.h>
#include <stddef.h>


#include "threads.h"

// begin blob definition

extern "C"
{

struct TypeSpec
{
    uint32_t Name;
    uint32_t Fields;
    uint16_t Size; // note: C++ fragile no designated initializers - Size must come after Name and Fields
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

#define CONCAT(token1,token2) token1 ## token2
#define CONCAT4(token1, token2, token3, token4) token1 ## token2 ## token3 ## token4

#define MAKE_TYPELEN_NAME(tyname) CONCAT(cdac_string_pool_typename__, tyname)
#define MAKE_FIELDLEN_NAME(tyname,membername) CONCAT4(cdac_string_pool_membername__, tyname, __, membername)
#define MAKE_FIELDTYPELEN_NAME(tyname,membername) CONCAT4(cdac_string_pool_membertypename__, tyname, __, membername)
#define MAKE_GLOBALLEN_NAME(globalname) CONCAT(cdac_string_pool_globalname__, globalname)
#define MAKE_GLOBALTYPELEN_NAME(globalname) CONCAT(cdac_string_pool_globaltypename__, globalname)

// define a struct where the size of each field is the length of some string.  we will use offsetof to get
// the offset of each struct element, which will be equal to the offset of the beginning of that string in the
// string pool.
struct CDacStringPoolSizes
{
    char cdac_string_pool_nil; // make the first real string start at offset 1
#define DECL_LEN(membername,len) char membername[(len)];
#define CDAC_BASELINE(name) DECL_LEN(cdac_string_pool_baseline_, (sizeof(name)))
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name) DECL_LEN(MAKE_TYPELEN_NAME(name), sizeof(#name))
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset) DECL_LEN(MAKE_FIELDLEN_NAME(tyname,membername), sizeof(#membername)) \
    DECL_LEN(MAKE_FIELDTYPELEN_NAME(tyname,membername), sizeof(#membertyname))
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value) DECL_LEN(MAKE_GLOBALLEN_NAME(name), sizeof(#name))
#define CDAC_GLOBAL(name,tyname,value) DECL_LEN(MAKE_GLOBALLEN_NAME(name), sizeof(#name)) \
    DECL_LEN(MAKE_GLOBALTYPELEN_NAME(name), sizeof(#tyname))
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
    char cdac_string_pool_trailing_nil;
};

#define GET_TYPE_NAME(name) offsetof(struct CDacStringPoolSizes, MAKE_TYPELEN_NAME(name))
#define GET_FIELD_NAME(tyname,membername) offsetof(struct CDacStringPoolSizes, MAKE_FIELDLEN_NAME(tyname,membername))
#define GET_FIELDTYPE_NAME(tyname,membername) offsetof(struct CDacStringPoolSizes, MAKE_FIELDTYPELEN_NAME(tyname,membername))
#define GET_GLOBAL_NAME(globalname) offsetof(struct CDacStringPoolSizes, MAKE_GLOBALLEN_NAME(globalname))
#define GET_GLOBALTYPE_NAME(globalname) offsetof(struct CDacStringPoolSizes, MAKE_GLOBALTYPELEN_NAME(globalname))

// count the types
enum
{
    CDacBlobTypesCount =
#define CDAC_BASELINE(name) 0
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name) + 1
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
    ,
};

// count the field pool size.
// there's 1 placeholder element at the start, and 1 endmarker after each type
enum
{
    CDacBlobFieldPoolCount =
#define CDAC_BASELINE(name) 1
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset) + 1
#define CDAC_TYPE_END(name) + 1
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
    ,
};

// count the literal globals
enum
{
    CDacBlobGlobalLiteralsCount =
#define CDAC_BASELINE(name) 0
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value) + 1
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
    ,
};

// count the aux vector globals
enum
{
    CDacBlobGlobalPointersCount =
#define CDAC_BASELINE(name) 0
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value) + 1
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
    ,
};


#define MAKE_TYPEFIELDS_TYNAME(tyname) CONCAT(CDacFieldPoolTypeStart__, tyname)

// index of each run of fields.
// we make a struct containing one 1-byte field for each field in the run, and then take the offset of the
// struct to get the index of the run of fields.
// this looks like
//
// struct CDacFieldPoolSizes {
//   char cdac_field_pool_start_placeholder__;
//   struct CDacFieldPoolTypeStart__MethodTable {
//     char cdac_field_pool_member__MethodTable__GCHandle;
//     char cdac_field_pool_member__MethodTable_endmarker;
//   } CDacFieldPoolTypeStart__MethodTable;
//   ...
// };
//
// so that offsetof(struct CDacFieldPoolSizes, CDacFieldPoolTypeStart__MethodTable) will give the offset of the
// method table field descriptors in the run of fields
struct CDacFieldPoolSizes
{
#define DECL_LEN(membername) char membername;
#define CDAC_BASELINE(name) DECL_LEN(cdac_field_pool_start_placeholder__)
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name) struct MAKE_TYPEFIELDS_TYNAME(name) {
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset) DECL_LEN(CONCAT4(cdac_field_pool_member__, tyname, __, membername))
#define CDAC_TYPE_END(name) DECL_LEN(CONCAT4(cdac_field_pool_member__, tyname, _, endmarker)) \
    } MAKE_TYPEFIELDS_TYNAME(name);
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
#undef DECL_LEN
};

#define GET_TYPE_FIELDS(tyname) offsetof(struct CDacFieldPoolSizes, MAKE_TYPEFIELDS_TYNAME(tyname))

// index of each global pointer
//
// struct CDacGlobalPointerIndex
// {
//    char placeholder;
//    char firstGlobalPointerName;
//    char secondGlobalPointerName;
//    ...
//}
//
//  offsetof (CDACGlobalPointerIndex, NAME) returns the index of the global
struct CDacGlobalPointerIndex
{
#define DECL_LEN(membername) char membername;
#define CDAC_BASELINE(name) DECL_LEN(cdac_global_pointer_index_start_placeholder__)
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value) DECL_LEN(CONCAT(cdac_global_pointer_index__, name))
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
};

#define GET_GLOBAL_POINTER_INDEX(name) offsetof(struct CDacGlobalPointerIndex, CONCAT(cdac_global_pointer_index__, name))

struct BinaryBlobDataDescriptor
{
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

struct MagicAndBlob {
    uint64_t magic;
    struct BinaryBlobDataDescriptor Blob;
};

DLLEXPORT size_t FooFaFa = 0x12345678;

// C-style designated initializers are a C++20 feature.  Have to use plain old aggregate initialization instead.

DLLEXPORT
struct MagicAndBlob BlobDataDescriptor = {
    /*.magic = */ 0x00424F4C42434144ull,// "DACBLOB",
    /*.Blob =*/ {
        /*.Directory =*/ {
            /*.BaselineStart = */ offsetof(struct BinaryBlobDataDescriptor, BaselineName),
            /* .TypesStart = */ offsetof(struct BinaryBlobDataDescriptor, Types),
            /* .FieldPoolStart = */ offsetof(struct BinaryBlobDataDescriptor, FieldPool),
            /* .GlobalLiteralValuesStart = */ offsetof(struct BinaryBlobDataDescriptor, GlobalLiteralValues),
            /* .GlobalPointersStart = */ offsetof(struct BinaryBlobDataDescriptor, GlobalPointerValues),
            /* .NamesStart = */ offsetof(struct BinaryBlobDataDescriptor, NamesPool),
            /* .TypeCount = */ CDacBlobTypesCount,
            /* .FieldPoolCount = */ CDacBlobFieldPoolCount,
            /* .GlobalLiteralValuesCount = */ CDacBlobGlobalLiteralsCount,
            /* .GlobalPointerValuesCount = */ CDacBlobGlobalPointersCount,
            /* .NamesPoolCount = */ sizeof(struct CDacStringPoolSizes),
            /* .TypeSpecSize = */ sizeof(struct TypeSpec),
            /* .FieldSpecSize = */ sizeof(struct FieldSpec),
            /* .GlobalLiteralSpecSize = */ sizeof(struct GlobalLiteralSpec),
            /* .GlobalPointerSpecSize = */ sizeof(struct GlobalPointerSpec),
        },
        /* .BaselineName = */ offsetof(struct CDacStringPoolSizes, cdac_string_pool_baseline_),

        /* .Types = */ {
#define CDAC_BASELINE(name)
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name) { \
    /* .Name = */ GET_TYPE_NAME(name), \
    /* .Fields = */ GET_TYPE_FIELDS(name),
#define CDAC_TYPE_INDETERMINATE(name) /*.Size = */ 0,
#define CDAC_TYPE_SIZE(size) /* .Size = */ size,
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name) },
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
        },

        /* .FieldPool = */ {
#define CDAC_BASELINE(name) {0,},
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset) { \
    /* .Name = */ GET_FIELD_NAME(tyname,membername), \
    /* .TypeName = */ GET_FIELDTYPE_NAME(tyname,membername), \
    /* .FieldOffset = */ offset, \
},
#define CDAC_TYPE_END(name) { 0, },
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
        },

        /* .GlobalLiteralValues = */ {
#define CDAC_BASELINE(name)
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value)
#define CDAC_GLOBAL(name,tyname,value) { /*.Name = */ GET_GLOBAL_NAME(name), /* .TypeName = */ GET_GLOBALTYPE_NAME(name), /* .Value = */ value },
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
        },

        /* .GlobalPointerValues = */ {
#define CDAC_BASELINE(name)
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value) { /* .Name = */ GET_GLOBAL_NAME(name), /* .AuxIndex = */ GET_GLOBAL_POINTER_INDEX(name) },
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
        },

        /* .NamesPool = */ ("\0" // starts with a nul
#define CDAC_BASELINE(name) name "\0"
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name) #name "\0"
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset) #membername "\0" #membertyname "\0"
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value) #name "\0"
#define CDAC_GLOBAL(name,tyname,value) #name "\0" #tyname "\0"
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
                  ),

        /* .EndMagic = */ { 0x01, 0x02, 0x03, 0x04 },
    }
};

// end blob definition

} // extern "C"
