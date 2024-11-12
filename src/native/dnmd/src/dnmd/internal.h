#ifndef _SRC_DNMD_INTERNAL_H_
#define _SRC_DNMD_INTERNAL_H_

#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include <assert.h>
#include <string.h>
#include <corhdr.h>
#include <dnmd.h>
#ifdef DNMD_PORTABLE_PDB
#include <dnmd_pdb.h>
#endif

// Implementations for missing bounds checking APIs.
// See https://en.cppreference.com/w/c/error#Bounds_checking
#if !defined(__STDC_LIB_EXT1__) && !defined(BUILD_WINDOWS)
typedef size_t rsize_t;
#endif // !__STDC_LIB_EXT1__

#define ARRAY_SIZE(a) (sizeof(a) / sizeof(*a))

#ifndef NDEBUG
#define ASSERT_ASSUME(x) assert(x)
#elif defined(_MSC_VER)
#define ASSERT_ASSUME(x) __assume(x)
#elif defined(__clang__)
#define ASSERT_ASSUME(x) __builtin_assume(x)
#elif defined(__GNUC__)
#define ASSERT_ASSUME(x) do { if (!(x)) __builtin_unreachable(); } while (0)
#else
#define ASSERT_ASSUME(x) (void)(x)
#endif

// Mutable data
typedef struct mddata__
{
    uint8_t* ptr;
    size_t size;
} mddata_t;

// Const data
typedef struct mdcdata__
{
    uint8_t const* ptr;
    size_t size;
} mdcdata_t;

// II.24.2.6 - 64 is the maximum value
#define MDTABLE_MAX_COUNT ((size_t)mdtid_End)
static_assert(MDTABLE_MAX_COUNT <= 64, "Specification sets max table count to 64");

#define MDTABLE_MAX_COLUMN_COUNT 9

// Macros for computing token types.
#define CreateTokenType(tk) (mdToken)(((uint32_t)tk << 24) & 0xff000000)
#define ExtractTokenType(tk) ((tk >> 24) & 0xff)

// Flags and masks used to embed column details for
// interpreting table rows.
typedef enum
{
    mdtc_none       = 0x00000000,

    // If the value should be taken as-is or used to index more
    mdtc_constant   = 0x00000001,
    mdtc_idx_heap   = 0x00000002, // Index into a heap - see flags below.
    mdtc_idx_table  = 0x00000004, // Index into a table - see mask below.
    mdtc_idx_coded  = 0x00000008, // Coded index - see II.24.2.6.
    // Value category mask
    mdtc_categorymask = 0x0000000f,

    // Size of the constant or index
    mdtc_b2         = 0x00000010, // 2-bytes
    mdtc_b4         = 0x00000020, // 4-bytes
    // Width of column flags
    mdtc_widthmask  = 0x00000030,

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
    mdtc_hmask      = 0xf0000000, // Mask for storing heap type
} mdtcol_t;

// Flags and masks for context details
typedef enum
{
    mdc_none              = 0x0000,
    mdc_large_string_heap = 0x0001,
    mdc_large_guid_heap   = 0x0002,
    mdc_large_blob_heap   = 0x0004,
    mdc_extra_data        = 0x0040,
    mdc_image_flags       = 0xffff,
    mdc_minimal_delta     = 0x00010000,
} mdcxt_flag_t;

// Macros used to insert/extract the column offset.
#define InsertOffset(o) ((o << 8) & mdtc_comask)
#define ExtractOffset(o) ((o & mdtc_comask) >> 8)

// Macros used to insert/extract the table for indexing.
#define InsertTable(c) ((c << 16) & mdtc_timask)
#define ExtractTable(c) ((c & mdtc_timask) >> 16)

// Macros used to insert/extract the coded index map index
#define InsertCodedIndex(s) ((s << 24) & mdtc_cimask)
#define ExtractCodedIndex(s) ((s & mdtc_cimask) >> 24)

// Macros used to insert/extract the heap type
#define InsertHeapType(h) ((h) & mdtc_hmask)
#define ExtractHeapType(h) ((h) & mdtc_hmask)

// Forward declare.
struct mdcxt__;

typedef struct mdtable__
{
    mdcdata_t data;
    uint32_t row_count;
    uint8_t row_size_bytes;
    uint8_t column_count;
    bool is_sorted : 1;
    bool is_adding_new_row : 1;
    uint8_t table_id;
    struct mdcxt__* cxt; // Non-null is indication of complete initialization
    mdtcol_t* column_details;
} mdtable_t;

typedef mdcdata_t mdstream_t;

typedef struct mdmem__ mdmem_t;

typedef struct mdeditor__ mdeditor_t;

typedef struct mdcxt__
{
    uint32_t magic; // mdlib magic
    mdcdata_t raw_metadata; // metadata raw bytes
    mdeditor_t* editor; // metadata editor
    mdcxt_flag_t context_flags;

    // Metadata root details - II.24.2.1
    uint16_t major_ver;
    uint16_t minor_ver;
    uint16_t flags;
    char const* version;

    // Metadata heaps - II.24.2.2
    mdstream_t strings_heap;
    mdstream_t guid_heap;
    mdstream_t blob_heap;
    mdstream_t user_string_heap;
    mdstream_t tables_heap;
#ifdef DNMD_PORTABLE_PDB
    mdstream_t pdb;
#endif // DNMD_PORTABLE_PDB

    // Metadata tables - II.22
    mdtable_t* tables;

    // Additional memory used for dynamic operations
    mdmem_t* mem;
} mdcxt_t;

// Extract a context from the mdhandle_t.
mdcxt_t* extract_mdcxt(mdhandle_t md);

// Allocate and free tracked memory.
void* alloc_mdmem(mdcxt_t* cxt, size_t length);
void free_mdmem(mdcxt_t* cxt, void* mem);

// Merge the supplied delta into the context.
bool merge_in_delta(mdcxt_t* cxt, mdcxt_t* delta);

//
// Streams
//

mdstream_t* get_heap_by_id(mdcxt_t* cxt, mdtcol_t heap_id);
mdcxt_flag_t get_large_heap_flag(mdtcol_t heap_id);

// Strings heap, #Strings - II.24.2.3
bool try_get_string(mdcxt_t* cxt, size_t offset, char const** str);
bool validate_strings_heap(mdcxt_t* cxt);
uint32_t add_to_string_heap(mdcxt_t* cxt, char const* str);

// User strings heap, #US - II.24.2.4
bool try_get_user_string(mdcxt_t* cxt, size_t offset, mduserstring_t* str, size_t* next_offset);
bool validate_user_string_heap(mdcxt_t* cxt);
uint32_t add_to_user_string_heap(mdcxt_t* cxt, char16_t const* str);

// Blob heap, #Blob - II.24.2.4
bool try_get_blob(mdcxt_t* cxt, size_t offset, uint8_t const** blob, uint32_t* blob_len);
bool validate_blob_heap(mdcxt_t* cxt);
uint32_t add_to_blob_heap(mdcxt_t* cxt, uint8_t const* data, uint32_t length);

// GUID heap, #GUID - II.24.2.5
bool try_get_guid(mdcxt_t* cxt, size_t idx, mdguid_t* guid);
bool validate_guid_heap(mdcxt_t* cxt);
uint32_t add_to_guid_heap(mdcxt_t* cxt, mdguid_t guid);

// Table heap, #~ - II.24.2.6
// Note: This can only be done after all streams have been read in.
bool initialize_tables(mdcxt_t* cxt);
bool validate_tables(mdcxt_t* cxt);

// PDB heap, #Pdb - https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#pdb-stream
typedef struct md_pdb__
{
    uint8_t pdb_id[20];
    mdToken entry_point;
    uint64_t referenced_type_system_tables;
    uint32_t type_system_table_rows[MDTABLE_MAX_COUNT];
} md_pdb_t;

// Interpret in the PDB data stream
// The md_pdb_t will be fully initialized if "true" is returned.
bool try_get_pdb(mdcxt_t* cxt, md_pdb_t* pdb);

//
// Tables
//

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
    mdci_TypeOrMethodDef,
#ifdef DNMD_PORTABLE_PDB
    mdci_HasCustomDebugInformation,
#endif // DNMD_PORTABLE_PDB
    mdci_Count
} md_coded_idx_t;

// Manipulators for coded indices - II.24.2.6
bool compose_coded_index(mdToken tk, mdtcol_t col_details, uint32_t* coded_index);
bool decompose_coded_index(uint32_t cidx, mdtcol_t col_details, mdtable_id_t* table_id, uint32_t* table_row);
bool is_coded_index_target(mdtcol_t col_details, mdtable_id_t table);

// Get the column count for a table.
uint8_t get_table_column_count(mdtable_id_t id);

// II.22 Metadata logical format tables
// Sort key info for tables

typedef struct md_key_info__
{
    uint8_t index;
    bool descending;
} md_key_info_t;

uint8_t get_table_keys(mdtable_id_t id, md_key_info_t const** keys);

// Initialize the supplied table details
bool initialize_table_details(
    uint32_t const* all_table_row_counts,
    mdcxt_flag_t context_flags,
    mdtable_id_t id,
    bool is_sorted,
    mdtable_t* table);

// Given the current table, consume the data stream assuming it contains the rows
bool consume_table_rows(mdtable_t* table, uint8_t const** data, size_t* data_len);

// Get whether or not the column in the table points into an indirect table
bool table_is_indirect_table(mdtable_id_t table_id);
// Get the indirection table for a given table
mdtable_id_t get_corresponding_indirection_table(mdtable_id_t table_id);

// Cursor manipulation


// Internal function used to create a cursor.
// Limited validation is done for the arguments.
mdcursor_t create_cursor(mdtable_t* table, uint32_t row);

// We declare these functions as static so they can be included in each translation unit.
// Some units may not use them, so we ignore the unused function warning.
#ifdef __GNUC__
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wunused-function"
#endif
static mdtable_t* CursorTable(mdcursor_t* c)
{
    assert(c != NULL);
    return (mdtable_t*)c->_reserved1;
}

static uint32_t CursorRow(mdcursor_t* c)
{
    assert(c != NULL);
    return RidFromToken(c->_reserved2);
}

static bool CursorNull(mdcursor_t* c)
{
    return CursorRow(c) == 0;
}

static bool CursorEnd(mdcursor_t* c)
{
    return (CursorTable(c)->row_count + 1) == CursorRow(c);
}

static uint8_t col_to_index(col_index_t col_idx, mdtable_t const* table)
{
    assert(table != NULL);
    uint32_t idx = (uint32_t)col_idx;
#ifdef DEBUG_TABLE_COLUMN_LOOKUP
    mdtable_id_t tgt_table_id = col_idx >> 8;
    if (tgt_table_id != table->table_id)
    {
        assert(!"Unexpected table/column indexing");
        return false;
    }
    idx = (col_idx & 0xff);
#else
    (void)table;
#endif
    return (uint8_t)idx;
}

static col_index_t index_to_col(uint8_t idx, mdtable_id_t table_id)
{
#ifdef DEBUG_TABLE_COLUMN_LOOKUP
    return (col_index_t)((table_id << 8) | idx);
#else
    (void)table_id;
    return (col_index_t)idx;
#endif
}
#ifdef __GNUC__
#pragma GCC diagnostic pop
#endif

// Copy data from a cursor to one row to a cursor to another row.
bool copy_cursor(mdcursor_t dest, mdcursor_t src);

// Single column access
typedef struct access_cxt__
{
    mdtable_t* table;
    mdtcol_t col_details;
    uint8_t const* data;
    uint8_t* writable_data;
} access_cxt_t;

bool create_access_context(mdcursor_t* cursor, col_index_t col_idx, bool make_writable, access_cxt_t* rcxt);
bool write_column_data(access_cxt_t* acxt, uint32_t data);
bool read_column_data(access_cxt_t* acxt, uint32_t* data);

// Raw bulk table access
typedef struct bulk_access_cxt__
{
    mdtable_t* table;
    mdtcol_t col_details;
    uint8_t const* start;
    uint8_t const* data;
    uint8_t const* end;
    size_t data_len;
    uint32_t data_len_col;
    uint32_t next_row_stride;
} bulk_access_cxt_t;

bool create_bulk_access_context(mdcursor_t* cursor, col_index_t col_idx, uint32_t row_count, bulk_access_cxt_t* acxt);
bool read_column_data_and_advance(bulk_access_cxt_t* acxt, uint32_t* data);
bool next_row(bulk_access_cxt_t* acxt);

// Internal functions used to read/write columns with minimal validation.
bool get_column_value_as_heap_offset(mdcursor_t c, col_index_t col_idx, uint32_t* offset);
bool set_column_value_as_heap_offset(mdcursor_t c, col_index_t col_idx, uint32_t offset);

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

bool advance_output_stream(uint8_t** data, size_t* data_len, size_t b);

bool write_u8(uint8_t** data, size_t* data_len, uint8_t o);
bool write_i8(uint8_t** data, size_t* data_len, int8_t o);
bool write_u16(uint8_t** data, size_t* data_len, uint16_t o);
bool write_i16(uint8_t** data, size_t* data_len, int16_t o);
bool write_u32(uint8_t** data, size_t* data_len, uint32_t o);
bool write_i32(uint8_t** data, size_t* data_len, int32_t o);
bool write_u64(uint8_t** data, size_t* data_len, uint64_t o);
bool write_i64(uint8_t** data, size_t* data_len, int64_t o);

// II.23.2
bool decompress_u32(uint8_t const** data, size_t* data_len, uint32_t* o);
bool decompress_i32(uint8_t const** data, size_t* data_len, int32_t* o);
// compressed_len is an in/out parameter. If compress_u32 returns true, then
// compressed_len is set to the number of bytes written to compressed.
bool compress_u32(uint32_t data, uint8_t* compressed, size_t* compressed_len);

// Editing
bool create_and_fill_indirect_table(mdcxt_t* cxt, mdtable_id_t original_table, mdtable_id_t indirect_table);
bool allocate_new_table(mdcxt_t* cxt, mdtable_id_t table_id);
uint8_t* get_writable_table_data(mdtable_t* table, bool make_writable);
bool initialize_new_table_details(mdcxt_t* cxt, mdtable_id_t id, mdtable_t* table);
int32_t update_shifted_row_references(mdcursor_t* c, uint32_t count, uint8_t col_index, mdtable_id_t updated_table, uint32_t original_starting_table_index, uint32_t new_starting_table_index);
bool insert_row_into_table(mdcxt_t* cxt, mdtable_id_t table_id, uint32_t row_index, mdcursor_t* new_row);
#ifdef DNMD_PORTABLE_PDB
bool update_referenced_type_system_table_row_count(mdcxt_t* cxt, mdtable_id_t updated_table, uint32_t new_max_row_count);
#endif // DNMD_PORTABLE_PDB

// Sort a row list (like FieldList, MethodList, ParamList, etc.) by the values specified in the given constant column on the target table.
bool sort_list_by_column(mdcursor_t parent, col_index_t list_col, col_index_t col);

// Add the heap with the specified id from the delta image to the cxt image.
bool append_heap(mdcxt_t* cxt, mdcxt_t* delta, mdtcol_t heap_id);

extern mdguid_t const empty_guid;

#endif // _SRC_DNMD_INTERNAL_H_
