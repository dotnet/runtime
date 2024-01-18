#ifndef _SRC_INC_DNMD_H_
#define _SRC_INC_DNMD_H_

#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>
// MacOS doesn't have uchar.h
#if defined(__has_include)
#if __has_include(<uchar.h>)
#include <uchar.h>
#elif !defined(__cplusplus)
// When uchar.h isn't available and we're in C, define char16_t as per the C standard.
typedef uint_least16_t char16_t;
#endif

#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef uint32_t mdToken;

typedef struct mdguid__
{
    uint32_t data1;
    uint16_t data2;
    uint16_t data3;
    uint8_t  data4[8];
} mdguid_t;

typedef void* mdhandle_t;

// Create a metadata handle that can be used to parse and modify the supplied metadata.
//
// The supplied data is expected to be unmoved and available until all
// handles created with the data have been destroyed.
// If modifications are made, the data will not be updated in place.
bool md_create_handle(void const* data, size_t data_len, mdhandle_t* handle);

// Create a new metadata handle for a new image.
// Returns a handle for the new image, or NULL if the handle could not be created.
// The image will always be in the v1.1 ECMA-355 metadata format,
// use the "v4.0.30319" version string,
// and have an MVID of all zeros.
mdhandle_t md_create_new_handle();

#ifdef DNMD_PORTABLE_PDB
// Create a new metadata handle for a new Portable PDB image.
// Returns a handle for the new image, or NULL if the handle could not be created.
// The image will always be in the v1.1 metadata format
// and use the "PDB v1.0" version string.
mdhandle_t md_create_new_pdb_handle();
#endif // DNMD_PORTABLE_PDB

// Apply delta data to the current metadata.
bool md_apply_delta(mdhandle_t handle, void const* data, size_t data_len);

// Destroy the metadata handle and free all associated memory.
void md_destroy_handle(mdhandle_t handle);

// Validate the metadata associated with the handle.
bool md_validate(mdhandle_t handle);

// Write all tables to stdout.
// Set table_id to '-1' to print out all tables.
bool md_dump_tables(mdhandle_t handle, int32_t table_id);

char const* md_get_version_string(mdhandle_t handle);

//
// All tables possible in ECMA-335
//
typedef enum
{
    mdtid_Unused = -1,
    mdtid_Module = 0x0,
    mdtid_TypeRef = 0x01,
    mdtid_TypeDef = 0x02,
    mdtid_FieldPtr = 0x03,
    mdtid_Field = 0x04,
    mdtid_MethodPtr = 0x05,
    mdtid_MethodDef = 0x06,
    mdtid_ParamPtr = 0x07,
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
    mdtid_EventPtr = 0x13,
    mdtid_Event = 0x14,
    mdtid_PropertyMap = 0x15,
    mdtid_PropertyPtr = 0x16,
    mdtid_Property = 0x17,
    mdtid_MethodSemantics = 0x18,
    mdtid_MethodImpl = 0x19,
    mdtid_ModuleRef = 0x1a,
    mdtid_TypeSpec = 0x1b,
    mdtid_ImplMap = 0x1c,
    mdtid_FieldRva = 0x1d,
    mdtid_ENCLog = 0x1e,
    mdtid_ENCMap = 0x1f,
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

#ifdef DNMD_PORTABLE_PDB
    // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
    mdtid_Document = 0x30,
    mdtid_MethodDebugInformation = 0x31,
    mdtid_LocalScope = 0x32,
    mdtid_LocalVariable = 0x33,
    mdtid_LocalConstant = 0x34,
    mdtid_ImportScope = 0x35,
    mdtid_StateMachineMethod = 0x36,
    mdtid_CustomDebugInformation = 0x37,
#endif // DNMD_PORTABLE_PDB

    mdtid_End,
    mdtid_First = mdtid_Module,
#ifdef DNMD_PORTABLE_PDB
    mdtid_FirstPdb = mdtid_Document,
#endif // DNMD_PORTABLE_PDB
} mdtable_id_t;

// Table cursor definition
typedef struct mdcursor__
{
    intptr_t _reserved1;
    intptr_t _reserved2;
} mdcursor_t;

// Create a cursor to the first row in a table.
bool md_create_cursor(mdhandle_t handle, mdtable_id_t table_id, mdcursor_t* cursor, uint32_t* count);

// Move the cursor +/- number of rows.
bool md_cursor_move(mdcursor_t* c, int32_t delta);

// Move to the next row.
bool md_cursor_next(mdcursor_t* c);

// Convert between a token and location in metadata tables.
bool md_token_to_cursor(mdhandle_t handle, mdToken tk, mdcursor_t* c);
bool md_cursor_to_token(mdcursor_t c, mdToken* tk);
mdhandle_t md_extract_handle_from_cursor(mdcursor_t c);

// Walk the #US heap. The initial value should be set to 0 or
// a valid offset into the #US heap - see RidFromToken in corhdr.h.
typedef intptr_t mduserstringcursor_t;

typedef struct mduserstring__
{
    char16_t const* str;
    uint32_t str_bytes;
    uint8_t final_byte;
} mduserstring_t;
bool md_walk_user_string_heap(mdhandle_t handle, mduserstringcursor_t* cursor, mduserstring_t* str, uint32_t* offset);

// Define to help debug table indexing
//#define DEBUG_TABLE_COLUMN_LOOKUP

// The MDTABLE_COLUMN macro constructs a table/column ID enumeration.
//
// An example (release build):
//  MDTABLE_COLUMN(Assembly, HashAlgId, 0) => mdtAssembly_HashAlgId = 0
//
#if defined(DEBUG_TABLE_COLUMN_LOOKUP) && !defined(MDTABLES_BUILD)
#define MDTABLE_COLUMN(table, col, value) mdt ## table ## _ ## col = ((mdtid_ ## table << 8) | (value))
#else
#define MDTABLE_COLUMN(table, col, value) mdt ## table ## _ ## col = (value)
#endif // DEBUG_TABLE_COLUMN_LOOKUP && !MDTABLES_BUILD

#define MDTABLE_COLUMN_COUNT(table, value) mdt ## table ## _ ## ColCount = (value)

//
// Column indexes for tables
//
typedef enum
{
    MDTABLE_COLUMN(Module, Generation, 0),
    MDTABLE_COLUMN(Module, Name, 1),
    MDTABLE_COLUMN(Module, Mvid, 2),
    MDTABLE_COLUMN(Module, EncId, 3),
    MDTABLE_COLUMN(Module, EncBaseId, 4),
    MDTABLE_COLUMN_COUNT(Module, 5),

    MDTABLE_COLUMN(TypeRef, ResolutionScope, 0),
    MDTABLE_COLUMN(TypeRef, TypeName, 1),
    MDTABLE_COLUMN(TypeRef, TypeNamespace, 2),
    MDTABLE_COLUMN_COUNT(TypeRef, 3),

    MDTABLE_COLUMN(TypeDef, Flags, 0),
    MDTABLE_COLUMN(TypeDef, TypeName, 1),
    MDTABLE_COLUMN(TypeDef, TypeNamespace, 2),
    MDTABLE_COLUMN(TypeDef, Extends, 3),
    MDTABLE_COLUMN(TypeDef, FieldList, 4),
    MDTABLE_COLUMN(TypeDef, MethodList, 5),
    MDTABLE_COLUMN_COUNT(TypeDef, 6),

    MDTABLE_COLUMN(FieldPtr, Field, 0),
    MDTABLE_COLUMN_COUNT(FieldPtr, 1),

    MDTABLE_COLUMN(Field, Flags, 0),
    MDTABLE_COLUMN(Field, Name, 1),
    MDTABLE_COLUMN(Field, Signature, 2),
    MDTABLE_COLUMN_COUNT(Field, 3),

    MDTABLE_COLUMN(MethodPtr, Method, 0),
    MDTABLE_COLUMN_COUNT(MethodPtr, 1),

    MDTABLE_COLUMN(MethodDef, Rva, 0),
    MDTABLE_COLUMN(MethodDef, ImplFlags, 1),
    MDTABLE_COLUMN(MethodDef, Flags, 2),
    MDTABLE_COLUMN(MethodDef, Name, 3),
    MDTABLE_COLUMN(MethodDef, Signature, 4),
    MDTABLE_COLUMN(MethodDef, ParamList, 5),
    MDTABLE_COLUMN_COUNT(MethodDef, 6),

    MDTABLE_COLUMN(ParamPtr, Param, 0),
    MDTABLE_COLUMN_COUNT(ParamPtr, 1),

    MDTABLE_COLUMN(Param, Flags, 0),
    MDTABLE_COLUMN(Param, Sequence, 1),
    MDTABLE_COLUMN(Param, Name, 2),
    MDTABLE_COLUMN_COUNT(Param, 3),

    MDTABLE_COLUMN(InterfaceImpl, Class, 0),
    MDTABLE_COLUMN(InterfaceImpl, Interface, 1),
    MDTABLE_COLUMN_COUNT(InterfaceImpl, 2),

    MDTABLE_COLUMN(MemberRef, Class, 0),
    MDTABLE_COLUMN(MemberRef, Name, 1),
    MDTABLE_COLUMN(MemberRef, Signature, 2),
    MDTABLE_COLUMN_COUNT(MemberRef, 3),

    MDTABLE_COLUMN(Constant, Type, 0),
    MDTABLE_COLUMN(Constant, Parent, 1),
    MDTABLE_COLUMN(Constant, Value, 2),
    MDTABLE_COLUMN_COUNT(Constant, 3),

    MDTABLE_COLUMN(CustomAttribute, Parent, 0),
    MDTABLE_COLUMN(CustomAttribute, Type, 1),
    MDTABLE_COLUMN(CustomAttribute, Value, 2),
    MDTABLE_COLUMN_COUNT(CustomAttribute, 3),

    MDTABLE_COLUMN(FieldMarshal, Parent, 0),
    MDTABLE_COLUMN(FieldMarshal, NativeType, 1),
    MDTABLE_COLUMN_COUNT(FieldMarshal, 2),

    MDTABLE_COLUMN(DeclSecurity, Action, 0),
    MDTABLE_COLUMN(DeclSecurity, Parent, 1),
    MDTABLE_COLUMN(DeclSecurity, PermissionSet, 2),
    MDTABLE_COLUMN_COUNT(DeclSecurity, 3),

    MDTABLE_COLUMN(ClassLayout, PackingSize, 0),
    MDTABLE_COLUMN(ClassLayout, ClassSize, 1),
    MDTABLE_COLUMN(ClassLayout, Parent, 2),
    MDTABLE_COLUMN_COUNT(ClassLayout, 3),

    MDTABLE_COLUMN(FieldLayout, Offset, 0),
    MDTABLE_COLUMN(FieldLayout, Field, 1),
    MDTABLE_COLUMN_COUNT(FieldLayout, 2),

    MDTABLE_COLUMN(StandAloneSig, Signature, 0),
    MDTABLE_COLUMN_COUNT(StandAloneSig, 1),

    MDTABLE_COLUMN(EventMap, Parent, 0),
    MDTABLE_COLUMN(EventMap, EventList, 1),
    MDTABLE_COLUMN_COUNT(EventMap, 2),

    MDTABLE_COLUMN(EventPtr, Event, 0),
    MDTABLE_COLUMN_COUNT(EventPtr, 1),

    MDTABLE_COLUMN(Event, EventFlags, 0),
    MDTABLE_COLUMN(Event, Name, 1),
    MDTABLE_COLUMN(Event, EventType, 2),
    MDTABLE_COLUMN_COUNT(Event, 3),

    MDTABLE_COLUMN(PropertyMap, Parent, 0),
    MDTABLE_COLUMN(PropertyMap, PropertyList, 1),
    MDTABLE_COLUMN_COUNT(PropertyMap, 2),

    MDTABLE_COLUMN(PropertyPtr, Property, 0),
    MDTABLE_COLUMN_COUNT(PropertyPtr, 1),

    MDTABLE_COLUMN(Property, Flags, 0),
    MDTABLE_COLUMN(Property, Name, 1),
    MDTABLE_COLUMN(Property, Type, 2),
    MDTABLE_COLUMN_COUNT(Property, 3),

    MDTABLE_COLUMN(MethodSemantics, Semantics, 0),
    MDTABLE_COLUMN(MethodSemantics, Method, 1),
    MDTABLE_COLUMN(MethodSemantics, Association, 2),
    MDTABLE_COLUMN_COUNT(MethodSemantics, 3),

    MDTABLE_COLUMN(MethodImpl, Class, 0),
    MDTABLE_COLUMN(MethodImpl, MethodBody, 1),
    MDTABLE_COLUMN(MethodImpl, MethodDeclaration, 2),
    MDTABLE_COLUMN_COUNT(MethodImpl, 3),

    MDTABLE_COLUMN(ModuleRef, Name, 0),
    MDTABLE_COLUMN_COUNT(ModuleRef, 1),

    MDTABLE_COLUMN(TypeSpec, Signature, 0),
    MDTABLE_COLUMN_COUNT(TypeSpec, 1),

    MDTABLE_COLUMN(ImplMap, MappingFlags, 0),
    MDTABLE_COLUMN(ImplMap, MemberForwarded, 1),
    MDTABLE_COLUMN(ImplMap, ImportName, 2),
    MDTABLE_COLUMN(ImplMap, ImportScope, 3),
    MDTABLE_COLUMN_COUNT(ImplMap, 4),

    MDTABLE_COLUMN(FieldRva, Rva, 0),
    MDTABLE_COLUMN(FieldRva, Field, 1),
    MDTABLE_COLUMN_COUNT(FieldRva, 2),

    MDTABLE_COLUMN(ENCLog, Token, 0),
    MDTABLE_COLUMN(ENCLog, Op, 1),
    MDTABLE_COLUMN_COUNT(ENCLog, 2),

    MDTABLE_COLUMN(ENCMap, Token, 0),
    MDTABLE_COLUMN_COUNT(ENCMap, 1),

    MDTABLE_COLUMN(Assembly, HashAlgId, 0),
    MDTABLE_COLUMN(Assembly, MajorVersion, 1),
    MDTABLE_COLUMN(Assembly, MinorVersion, 2),
    MDTABLE_COLUMN(Assembly, BuildNumber, 3),
    MDTABLE_COLUMN(Assembly, RevisionNumber, 4),
    MDTABLE_COLUMN(Assembly, Flags, 5),
    MDTABLE_COLUMN(Assembly, PublicKey, 6),
    MDTABLE_COLUMN(Assembly, Name, 7),
    MDTABLE_COLUMN(Assembly, Culture, 8),
    MDTABLE_COLUMN_COUNT(Assembly, 9),

    MDTABLE_COLUMN(AssemblyRef, MajorVersion, 0),
    MDTABLE_COLUMN(AssemblyRef, MinorVersion, 1),
    MDTABLE_COLUMN(AssemblyRef, BuildNumber, 2),
    MDTABLE_COLUMN(AssemblyRef, RevisionNumber, 3),
    MDTABLE_COLUMN(AssemblyRef, Flags, 4),
    MDTABLE_COLUMN(AssemblyRef, PublicKeyOrToken, 5),
    MDTABLE_COLUMN(AssemblyRef, Name, 6),
    MDTABLE_COLUMN(AssemblyRef, Culture, 7),
    MDTABLE_COLUMN(AssemblyRef, HashValue, 8),
    MDTABLE_COLUMN_COUNT(AssemblyRef, 9),

    MDTABLE_COLUMN(File, Flags, 0),
    MDTABLE_COLUMN(File, Name, 1),
    MDTABLE_COLUMN(File, HashValue, 2),
    MDTABLE_COLUMN_COUNT(File, 3),

    MDTABLE_COLUMN(ExportedType, Flags, 0),
    MDTABLE_COLUMN(ExportedType, TypeDefId, 1),
    MDTABLE_COLUMN(ExportedType, TypeName, 2),
    MDTABLE_COLUMN(ExportedType, TypeNamespace, 3),
    MDTABLE_COLUMN(ExportedType, Implementation, 4),
    MDTABLE_COLUMN_COUNT(ExportedType, 5),

    MDTABLE_COLUMN(ManifestResource, Offset, 0),
    MDTABLE_COLUMN(ManifestResource, Flags, 1),
    MDTABLE_COLUMN(ManifestResource, Name, 2),
    MDTABLE_COLUMN(ManifestResource, Implementation, 3),
    MDTABLE_COLUMN_COUNT(ManifestResource, 4),

    MDTABLE_COLUMN(NestedClass, NestedClass, 0),
    MDTABLE_COLUMN(NestedClass, EnclosingClass, 1),
    MDTABLE_COLUMN_COUNT(NestedClass, 2),

    MDTABLE_COLUMN(GenericParam, Number, 0),
    MDTABLE_COLUMN(GenericParam, Flags, 1),
    MDTABLE_COLUMN(GenericParam, Owner, 2),
    MDTABLE_COLUMN(GenericParam, Name, 3),
    MDTABLE_COLUMN_COUNT(GenericParam, 4),

    MDTABLE_COLUMN(MethodSpec, Method, 0),
    MDTABLE_COLUMN(MethodSpec, Instantiation, 1),
    MDTABLE_COLUMN_COUNT(MethodSpec, 2),

    MDTABLE_COLUMN(GenericParamConstraint, Owner, 0),
    MDTABLE_COLUMN(GenericParamConstraint, Constraint, 1),
    MDTABLE_COLUMN_COUNT(GenericParamConstraint, 2),

#ifdef DNMD_PORTABLE_PDB
    // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
    MDTABLE_COLUMN(Document, Name, 0),
    MDTABLE_COLUMN(Document, HashAlgorithm, 1),
    MDTABLE_COLUMN(Document, Hash, 2),
    MDTABLE_COLUMN(Document, Language, 3),
    MDTABLE_COLUMN_COUNT(Document, 4),

    MDTABLE_COLUMN(MethodDebugInformation, Document, 0),
    MDTABLE_COLUMN(MethodDebugInformation, SequencePoints, 1),
    MDTABLE_COLUMN_COUNT(MethodDebugInformation, 2),

    MDTABLE_COLUMN(LocalScope, Method, 0),
    MDTABLE_COLUMN(LocalScope, ImportScope, 1),
    MDTABLE_COLUMN(LocalScope, VariableList, 2),
    MDTABLE_COLUMN(LocalScope, ConstantList, 3),
    MDTABLE_COLUMN(LocalScope, StartOffset, 4),
    MDTABLE_COLUMN(LocalScope, Length, 5),
    MDTABLE_COLUMN_COUNT(LocalScope, 6),

    MDTABLE_COLUMN(LocalVariable, Attributes, 0),
    MDTABLE_COLUMN(LocalVariable, Index, 1),
    MDTABLE_COLUMN(LocalVariable, Name, 2),
    MDTABLE_COLUMN_COUNT(LocalVariable, 3),

    MDTABLE_COLUMN(LocalConstant, Name, 0),
    MDTABLE_COLUMN(LocalConstant, Signature, 1),
    MDTABLE_COLUMN_COUNT(LocalConstant, 2),

    MDTABLE_COLUMN(ImportScope, Parent, 0),
    MDTABLE_COLUMN(ImportScope, Imports, 1),
    MDTABLE_COLUMN_COUNT(ImportScope, 2),

    MDTABLE_COLUMN(StateMachineMethod, MoveNextMethod, 0),
    MDTABLE_COLUMN(StateMachineMethod, KickoffMethod, 1),
    MDTABLE_COLUMN_COUNT(StateMachineMethod, 2),

    MDTABLE_COLUMN(CustomDebugInformation, Parent, 0),
    MDTABLE_COLUMN(CustomDebugInformation, Kind, 1),
    MDTABLE_COLUMN(CustomDebugInformation, Value, 2),
    MDTABLE_COLUMN_COUNT(CustomDebugInformation, 3),
#endif // DNMD_PORTABLE_PDB

} col_index_t;

// Query row's column values
// The returned number represents the number of valid cursor(s) for indexing.
int32_t md_get_column_value_as_token(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdToken* tk);
int32_t md_get_column_value_as_cursor(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdcursor_t* cursor);
// Resolve the column to a cursor and a range based on the run/list pattern in tables.
// The run continues to the smaller of:
//   * the last row of the target table
//   * the next run in the target table, found by inspecting the column value of the next row in the current table.
// See md_find_token_of_range_element() for mapping elements in the other direction.
bool md_get_column_value_as_range(mdcursor_t c, col_index_t col_idx, mdcursor_t* cursor, uint32_t* count);
int32_t md_get_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint32_t* constant);
int32_t md_get_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, uint32_t out_length, char const** str);
int32_t md_get_column_value_as_userstring(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mduserstring_t* strings);
int32_t md_get_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint8_t const** blob, uint32_t* blob_len);
int32_t md_get_column_value_as_guid(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdguid_t* guid);

// Return the raw column values for the row. Unlike the md_get_column_value_as_* APIs, the returned values
// are in their raw form.
// Callers should indicate ('true') using the 'values_to_get' collection which columns are desired.
// Corresponding entries in 'values_raw' will only be set if a 'true' value is set in 'values_to_get'.
// Note this API was not designed in a performance critical manner and should only be used if necessary.
// The APIs that retrieve specific columns in their respective formatted forms have been designed for performance
// and should be preferred whenever possible.
bool md_get_column_values_raw(mdcursor_t c, uint32_t values_length, bool* values_to_get, uint32_t* values_raw);

// Find a row or range of rows where the supplied column has the expected value.
// These APIs assume the value to look for is the value in the table, typically record IDs (RID)
// for tokens. An exception is made for coded indices, which are cumbersome to compute.
// If the queried column contains a coded index value, the value will be validated and
// transformed to its coded form for comparison.
bool md_find_row_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* cursor);

typedef enum
{
    MD_RANGE_FOUND = 0,
    MD_RANGE_NOT_FOUND = 1,
    MD_RANGE_NOT_SUPPORTED = 2,
} md_range_result_t;

md_range_result_t md_find_range_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* start, uint32_t* count);

// Given a value into a supported table, find the associated parent token.
//  - mdtid_Field
//  - mdtid_MethodDef
//  - mdtid_Param
//  - mdtid_Event
//  - mdtid_Property
// See md_get_column_value_as_range() for getting the complete range.
bool md_find_token_of_range_element(mdcursor_t element, mdToken* tk);
bool md_find_cursor_of_range_element(mdcursor_t element, mdcursor_t* cursor);

// Given a cursor, resolve any indirections to the final cursor or return the original cursor if it does not point to an indirection table.
// Returns true if the cursor was not an indirect cursor or if the indirection was resolved, or false if the cursor pointed to an invalid indirection table entry.
bool md_resolve_indirect_cursor(mdcursor_t c, mdcursor_t* target);

// Set row's column values
// The returned number represents the number of rows updated.
int32_t md_set_column_value_as_token(mdcursor_t c, col_index_t col, uint32_t in_length, mdToken const* tk);
int32_t md_set_column_value_as_cursor(mdcursor_t c, col_index_t col, uint32_t in_length, mdcursor_t const* cursor);
int32_t md_set_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t in_length, uint32_t const* constant);
int32_t md_set_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, uint32_t in_length, char const* const* str);
int32_t md_set_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint32_t in_length, uint8_t const* const* blob, uint32_t const* blob_len);
int32_t md_set_column_value_as_guid(mdcursor_t c, col_index_t col_idx, uint32_t in_length, mdguid_t const* guid);
int32_t md_set_column_value_as_userstring(mdcursor_t c, col_index_t col_idx, uint32_t in_length, char16_t const* const* userstring);

// Create a new row logically before the row specified by the cursor.
// If the given row is in a table that is a target of a list column, this function will return false.
// Only md_add_row_to_list can be used to add rows to a table that is a target of a list column.
// The table is treated as unsorted until md_commit_row_add is called after all columns have been set on the new row.
bool md_insert_row_before(mdcursor_t row, mdcursor_t* new_row);

// Create a new row after the row specified by the cursor.
// If the given row is in a table that is a target of a list column, this function will return false.
// Only md_add_row_to_list can be used to add rows to a table that is a target of a list column.
// The table is treated as unsorted until md_commit_row_add is called after all columns have been set on the new row.
bool md_insert_row_after(mdcursor_t row, mdcursor_t* new_row);

// Create a new row at the end of the specified table.
// If the given row is in a table that is a target of a list column, this function will return false.
// Only md_add_row_to_list can be used to add rows to a table that is a target of a list column.
// The table is treated as unsorted until md_commit_row_add is called after all columns have been set on the new row.
bool md_append_row(mdhandle_t handle, mdtable_id_t table_id, mdcursor_t* new_row);

// Creates a new row in the list for the given cursor specified by the given column.
// This method accounts for any indirection tables that may need to be created or maintained to ensure that
// the structure of the list is maintained without moving tokens.
// The table that new_child_row points to is treated as unsorted until md_commit_row_add is called after all columns have been set on the new row.
bool md_add_new_row_to_list(mdcursor_t list_owner, col_index_t list_col, mdcursor_t* new_row);

// Creates a new row in the list for the given cursor specified by the given column such that the values of the sort_order_col are maintained in ascending order.
// This method assumes that the list is currently sorted by the sort_order_col column.
// This method accounts for any indirection tables that may need to be created or maintained to ensure that
// the structure of the list is maintained without moving tokens.
// The table that new_row points to is treated as unsorted until md_commit_row_add is called after all columns have been set on the new row.
// The new_row row will also have the sort_order_col column initialized to sort_col_value.
bool md_add_new_row_to_sorted_list(mdcursor_t list_owner, col_index_t list_col, col_index_t sort_order_col, uint32_t sort_col_value, mdcursor_t* new_row);

// Finish the process of adding a row to the cursor's table.
void md_commit_row_add(mdcursor_t row);

// Add a user string to the #US heap.
mduserstringcursor_t md_add_userstring_to_heap(mdhandle_t handle, char16_t const* userstring);

// Write the metadata represented by the handle to the supplied buffer.
// The metadata is always written with the v2.0 table schema.
bool md_write_to_buffer(mdhandle_t handle, uint8_t* buffer, size_t* len);
#ifdef __cplusplus
}
#endif

#endif // _SRC_INC_DNMD_H_
