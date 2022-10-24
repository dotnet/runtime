#ifndef _SRC_DNMD_INTERNAL_H_
#define _SRC_DNMD_INTERNAL_H_

#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include <assert.h>
#include <string.h>

#ifndef NDEBUG
#define DEBUG
#endif

#include <dnmd.h>

#define ARRAY_SIZE(a) (sizeof(a) / sizeof(*a))

// Mutable data
typedef struct _mddata_t
{
    uint8_t* ptr;
    size_t size;
} mddata_t;

// Const data
typedef struct _mdcdata_t
{
    uint8_t const* ptr;
    size_t size;
} mdcdata_t;

// II.24.2.6
#define MDTABLE_MAX_COUNT 64

#define MDTABLE_MAX_COLUMN_COUNT 9

// Flags and mases used to embed column details for
// interpreting table rows.
typedef enum
{
    mdtc_none       = 0x00000000,

    // If the value should be taken as-is or used to index more
    mdtc_constant   = 0x00000001,
    mdtc_idx_heap   = 0x00000002, // Index into a heap - see flags below.
    mdtc_idx_table  = 0x00000004, // Index into a table - see mask below.
    mdtc_idx_coded  = 0x00000008, // Coded index - see II.24.2.6.

    // Size of the constant or index
    mdtc_b2         = 0x00000010, // 2-bytes
    mdtc_b4         = 0x00000020, // 4-bytes
    //mdtc_unused1  = 0x00000040,
    //mdtc_unused2  = 0x00000080,

    // Column byte offset into row
    mdtc_comask     = 0x0000ff00, // Mask for storing column offset

    // Table values
    mdtc_timask     = 0x00ff0000, // Mask for storing table index

    // Coded index
    mdtc_cimask     = 0x0f000000, // Mask for storing coded index map index

    // Heap flags
    mdtc_hguid      = 0x10000000, // #GUID
    mdtc_hstring    = 0x20000000, // #Strings
    mdtc_hus        = 0x40000000, // #US
    mdtc_hblob      = 0x80000000, // #Blob
} mdtcol_t;

// Macros used to insert/extract the column offset.
#define InsertOffset(o) ((o << 8) & mdtc_comask)
#define ExtractOffset(o) ((o & mdtc_comask) >> 8)

// Macros used to insert/extract the table for indexing.
#define InsertTable(c) ((c << 16) & mdtc_timask)
#define ExtractTable(c) ((c & mdtc_timask) >> 16)

// Macros used to insert/extract the coded index map index
#define InsertCodedIndex(s) ((s << 24) & mdtc_cimask)
#define ExtractCodedIndex(s) ((s & mdtc_cimask) >> 24)

// Foward declare.
struct _mdcxt_t;

typedef struct _mdtable_t
{
    mdcdata_t data;
    uint32_t row_count;
    uint32_t row_size_bytes;
    uint32_t column_count;
    bool is_sorted;
    uint8_t table_id;
    struct _mdcxt_t* cxt; // Non-null is indication of complete initialization
    mdtcol_t column_details[MDTABLE_MAX_COLUMN_COUNT];
} mdtable_t;

typedef mddata_t mdstream_t;

typedef struct _mdcxt_t
{
    uint32_t magic; // mdlib magic
    mddata_t data; // metadata raw bytes

    // Metadata root details - II.24.2.1
    uint16_t major_ver;
    uint16_t minor_ver;
    char const* version;
    uint16_t flags;

    // Metadata heaps - II.24.2.2
    mdstream_t strings_heap;
    mdstream_t guid_heap;
    mdstream_t blob_heap;
    mdstream_t user_string_heap;
    mdstream_t tables_heap;

    // Metadata tables - II.22
    uint8_t heap_sizes; // 1 = "#Strings", 2 = "#GUID", 4 = "#Blob"
    mdtable_t tables[MDTABLE_MAX_COUNT];
} mdcxt_t;

// Extract a context from the mdhandle_t.
mdcxt_t* extract_mdcxt(mdhandle_t md);

//
// Streams
//

// Strings heap, #Strings - II.24.2.3
bool try_get_string(mdcxt_t* cxt, size_t offset, char const** str);
bool validate_strings_heap(mdcxt_t* cxt);

// User strings heap, #US - II.24.2.4
bool try_get_user_string(mdcxt_t* cxt, size_t offset, WCHAR const** str, uint32_t* str_wchars, uint8_t* final_byte);
bool validate_user_string_heap(mdcxt_t* cxt);

// Blob heap, #Blob - II.24.2.4
bool try_get_blob(mdcxt_t* cxt, size_t offset, uint8_t const** blob, uint32_t* blob_len);
bool validate_blob_heap(mdcxt_t* cxt);

// GUID heap, #GUID - II.24.2.5
bool try_get_guid(mdcxt_t* cxt, size_t idx, GUID* guid);
bool validate_guid_heap(mdcxt_t* cxt);

// Table heap, #~ - II.24.2.6
// Note: This can only be done after all streams have been read in.
bool initialize_tables(mdcxt_t* cxt);
bool validate_tables(mdcxt_t* cxt);

//
// Tables
//

// II.22 all tables
typedef enum
{
    mdtid_Unused = -1,
    mdtid_Module = 0x0,
    mdtid_TypeRef = 0x01,
    mdtid_TypeDef = 0x02,

    mdtid_Field = 0x04,

    mdtid_MethodDef = 0x06,

    mdtid_Param = 0x08,
    mdtid_InterfaceImpl = 0x09,
    mdtid_MemberRef = 0x0a,
    mdtid_Constant = 0x0b,
    mdtid_CustomAttribute = 0x0c,
    mdtid_FieldMarshal = 0x0d,
    mdtid_DeclSecurity = 0x0e,
    mdtid_ClassLayout = 0x0f,
    mdtid_FieldLayout = 0x10,
    mdtid_StandAloneSig = 0x11,
    mdtid_EventMap = 0x12,

    mdtid_Event = 0x14,
    mdtid_PropertyMap = 0x15,

    mdtid_Property = 0x17,
    mdtid_MethodSemantics = 0x18,
    mdtid_MethodImpl = 0x19,
    mdtid_ModuleRef = 0x1a,
    mdtid_TypeSpec = 0x1b,
    mdtid_ImplMap = 0x1c,
    mdtid_FieldRva = 0x1d,

    mdtid_Assembly = 0x20,
    mdtid_AssemblyProcessor = 0x21,
    mdtid_AssemblyOS = 0x22,
    mdtid_AssemblyRef = 0x23,
    mdtid_AssemblyRefProcessor = 0x24,
    mdtid_AssemblyRefOS = 0x25,
    mdtid_File = 0x26,
    mdtid_ExportedType = 0x27,
    mdtid_ManifestResource = 0x28,
    mdtid_NestedClass = 0x29,
    mdtid_GenericParam = 0x2a,
    mdtid_MethodSpec = 0x2b,
    mdtid_GenericParamConstraint = 0x2c,

    mdtid_First = mdtid_Module,
    mdtid_Last = mdtid_GenericParamConstraint
} mdtable_id_t;

static_assert(mdtid_Last < MDTABLE_MAX_COUNT, "Last ID should be less than max count");

// Coded index collections - II.24.2.6
typedef enum
{
    mdci_TypeDefOrRef,
    mdci_HasConstant,
    mdci_HasCustomAttribute,
    mdci_HasFieldMarshall,
    mdci_HasDeclSecurity,
    mdci_MemberRefParent,
    mdci_HasSemantics,
    mdci_MethodDefOrRef,
    mdci_MemberForwarded,
    mdci_Implementation,
    mdci_CustomAttributeType,
    mdci_ResolutionScope,
    mdci_TypeOrMethodDef
} md_coded_idx_t;

typedef struct _coded_index_entry
{
    // Coded index lookup
    mdtable_id_t const* lookup;
    // Coded index lookup length
    uint8_t const lookup_len;
    // Number of bits needed to encode lookup index
    uint8_t const bit_encoding_size;
} coded_index_entry;

extern coded_index_entry const coded_index_map[13];

// Initialize the supplied table details
bool initialize_table_details(
    uint32_t const* all_table_row_counts,
    uint8_t heap_sizes,
    mdtable_id_t id,
    bool is_sorted,
    mdtable_t* table);

// Given the current table, consume the data stream assuming it contains the rows
bool consume_table_rows(mdtable_t* table, uint8_t const** data, size_t* data_len);

// Internal function used to create a cursor.
// Limited validation is done for the arguments.
mdcursor_t create_cursor(mdtable_t* table, uint32_t row);

//
// Manipulation of bits
//

uint32_t align_to(uint32_t val, uint32_t align);
size_t count_set_bits(uint64_t val);

//
// Byte streams
//

bool advance_stream(uint8_t const** data, size_t* data_len, size_t b);

bool read_u8(uint8_t const** data, size_t* data_len, uint8_t* o);
bool read_i8(uint8_t const** data, size_t* data_len, int8_t* o);
bool read_u16(uint8_t const** data, size_t* data_len, uint16_t* o);
bool read_i16(uint8_t const** data, size_t* data_len, int16_t* o);
bool read_u32(uint8_t const** data, size_t* data_len, uint32_t* o);
bool read_i32(uint8_t const** data, size_t* data_len, int32_t* o);
bool read_u64(uint8_t const** data, size_t* data_len, uint64_t* o);
bool read_i64(uint8_t const** data, size_t* data_len, int64_t* o);

// II.23.2
bool decompress_u32(uint8_t const** data, size_t* data_len, uint32_t* o);

#endif // _SRC_DNMD_INTERNAL_H_
