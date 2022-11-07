#ifndef _SRC_INC_DNMD_H_
#define _SRC_INC_DNMD_H_

#ifdef __cplusplus
extern "C" {
#endif

typedef void* mdhandle_t;

// Create a metadata handle that can be used to parse the supplied metadata.
//
// The supplied data is expected to be unmoved and available until all
// handles created with the data have been destroyed.
bool md_create_handle(void const* data, size_t data_len, mdhandle_t* handle);

// Destroy the metadata handle and free all associated memory.
void md_destroy_handle(mdhandle_t handle);

// Validate the metadata associated with the handle.
bool md_validate(mdhandle_t handle);

// Write all tables to stdout.
// Set table_id to '-1' to print out all tables.
bool md_dump_tables(mdhandle_t handle, int32_t table_id);

//
// All tables possible in ECMA-335
//
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

    mdtid_Last,
    mdtid_First = mdtid_Module,
} mdtable_id_t;

// Table cursor definition
typedef struct _mdcursor_t
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

// Walk the #US heap. The initial value should be set to 0 or
// a valid offset into the #US heap - see RidFromToken in corhdr.h.
typedef intptr_t mduserstringcursor_t;

typedef struct _mduserstring_t
{
    WCHAR const* str;
    uint32_t str_bytes;
    uint8_t final_byte;
} mduserstring_t;
int32_t md_walk_user_string_heap(mdhandle_t handle, mduserstringcursor_t* cursor, uint32_t out_length, mduserstring_t* strings, uint32_t* offsets);

// Define to help debug table indexing
//#define DEBUG_TABLE_COLUMN_LOOKUP

// The MDTABLE_COLUMN macro constructs a table/column ID enumeration.
//
// An example (release build):
//  MDTABLE_COLUMN(Assembly, HashAlgId     , 0) => mdtAssembly_HashAlgId = 0
//
#if defined(DEBUG_TABLE_COLUMN_LOOKUP) && !defined(MDTABLES_BUILD)
#define MDTABLE_COLUMN(table, col, value) mdt ## table ## _ ## col = ((mdtid_ ## table << 8) | (value))
#else
#define MDTABLE_COLUMN(table, col, value) mdt ## table ## _ ## col = (value)
#endif // DEBUG_TABLE_COLUMN_LOOKUP && !MDTABLES_BUILD

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

    MDTABLE_COLUMN(TypeRef, ResolutionScope, 0),
    MDTABLE_COLUMN(TypeRef, TypeName, 1),
    MDTABLE_COLUMN(TypeRef, TypeNamespace, 2),

    MDTABLE_COLUMN(TypeDef, Flags, 0),
    MDTABLE_COLUMN(TypeDef, TypeName, 1),
    MDTABLE_COLUMN(TypeDef, TypeNamespace, 2),
    MDTABLE_COLUMN(TypeDef, Extends, 3),
    MDTABLE_COLUMN(TypeDef, FieldList, 4),
    MDTABLE_COLUMN(TypeDef, MethodList, 5),

    MDTABLE_COLUMN(Field, Flags, 0),
    MDTABLE_COLUMN(Field, Name, 1),
    MDTABLE_COLUMN(Field, Signature, 2),

    MDTABLE_COLUMN(MethodDef, Rva, 0),
    MDTABLE_COLUMN(MethodDef, ImplFlags, 1),
    MDTABLE_COLUMN(MethodDef, Flags, 2),
    MDTABLE_COLUMN(MethodDef, Name, 3),
    MDTABLE_COLUMN(MethodDef, Signature, 4),
    MDTABLE_COLUMN(MethodDef, ParamList, 5),

    MDTABLE_COLUMN(Param, Flags, 0),
    MDTABLE_COLUMN(Param, Sequence, 1),
    MDTABLE_COLUMN(Param, Name, 2),

    MDTABLE_COLUMN(InterfaceImpl, Class, 0),
    MDTABLE_COLUMN(InterfaceImpl, Interface, 1),

    MDTABLE_COLUMN(MemberRef, Class, 0),
    MDTABLE_COLUMN(MemberRef, Name, 1),
    MDTABLE_COLUMN(MemberRef, Signature, 2),

    MDTABLE_COLUMN(Constant, Type, 0),
    MDTABLE_COLUMN(Constant, Parent, 1),
    MDTABLE_COLUMN(Constant, Value, 2),

    MDTABLE_COLUMN(CustomAttribute, Parent, 0),
    MDTABLE_COLUMN(CustomAttribute, Type, 1),
    MDTABLE_COLUMN(CustomAttribute, Value, 2),

    MDTABLE_COLUMN(FieldMarshal, Parent, 0),
    MDTABLE_COLUMN(FieldMarshal, NativeType, 1),

    MDTABLE_COLUMN(DeclSecurity, Action, 0),
    MDTABLE_COLUMN(DeclSecurity, Parent, 1),
    MDTABLE_COLUMN(DeclSecurity, PermissionSet, 2),

    MDTABLE_COLUMN(ClassLayout, PackingSize, 0),
    MDTABLE_COLUMN(ClassLayout, ClassSize, 1),
    MDTABLE_COLUMN(ClassLayout, Parent, 2),

    MDTABLE_COLUMN(FieldLayout, Offset, 0),
    MDTABLE_COLUMN(FieldLayout, Field, 1),

    MDTABLE_COLUMN(StandAloneSig, Signature, 0),

    MDTABLE_COLUMN(EventMap, Parent, 0),
    MDTABLE_COLUMN(EventMap, EventList, 1),

    MDTABLE_COLUMN(Event, EventFlags, 0),
    MDTABLE_COLUMN(Event, Name, 1),
    MDTABLE_COLUMN(Event, EventType, 2),

    MDTABLE_COLUMN(PropertyMap, Parent, 0),
    MDTABLE_COLUMN(PropertyMap, PropertyList, 1),

    MDTABLE_COLUMN(Property, Flags, 0),
    MDTABLE_COLUMN(Property, Name, 1),
    MDTABLE_COLUMN(Property, Type, 2),

    MDTABLE_COLUMN(MethodSemantics, Semantics, 0),
    MDTABLE_COLUMN(MethodSemantics, Method, 1),
    MDTABLE_COLUMN(MethodSemantics, Association, 2),

    MDTABLE_COLUMN(MethodImpl, Class, 0),
    MDTABLE_COLUMN(MethodImpl, MethodBody, 1),
    MDTABLE_COLUMN(MethodImpl, MethodDeclaration, 2),

    MDTABLE_COLUMN(ModuleRef, Name, 0),

    MDTABLE_COLUMN(TypeSpec, Signature, 0),

    MDTABLE_COLUMN(ImplMap, MappingFlags, 0),
    MDTABLE_COLUMN(ImplMap, MemberForwarded, 1),
    MDTABLE_COLUMN(ImplMap, ImportName, 2),
    MDTABLE_COLUMN(ImplMap, ImportScope, 3),

    MDTABLE_COLUMN(FieldRva, Rva, 0),
    MDTABLE_COLUMN(FieldRva, Field, 1),

    MDTABLE_COLUMN(Assembly, HashAlgId, 0),
    MDTABLE_COLUMN(Assembly, MajorVersion, 1),
    MDTABLE_COLUMN(Assembly, MinorVersion, 2),
    MDTABLE_COLUMN(Assembly, BuildNumber, 3),
    MDTABLE_COLUMN(Assembly, RevisionNumber, 4),
    MDTABLE_COLUMN(Assembly, Flags, 5),
    MDTABLE_COLUMN(Assembly, PublicKey, 6),
    MDTABLE_COLUMN(Assembly, Name, 7),
    MDTABLE_COLUMN(Assembly, Culture, 8),

    MDTABLE_COLUMN(AssemblyRef, MajorVersion, 0),
    MDTABLE_COLUMN(AssemblyRef, MinorVersion, 1),
    MDTABLE_COLUMN(AssemblyRef, BuildNumber, 2),
    MDTABLE_COLUMN(AssemblyRef, RevisionNumber, 3),
    MDTABLE_COLUMN(AssemblyRef, Flags, 4),
    MDTABLE_COLUMN(AssemblyRef, PublicKeyOrToken, 5),
    MDTABLE_COLUMN(AssemblyRef, Name, 6),
    MDTABLE_COLUMN(AssemblyRef, Culture, 7),
    MDTABLE_COLUMN(AssemblyRef, HashValue, 8),

    MDTABLE_COLUMN(File, Flags, 0),
    MDTABLE_COLUMN(File, Name, 1),
    MDTABLE_COLUMN(File, HashValue, 2),

    MDTABLE_COLUMN(ExportedType, Flags, 0),
    MDTABLE_COLUMN(ExportedType, TypeDefId, 1),
    MDTABLE_COLUMN(ExportedType, TypeName, 2),
    MDTABLE_COLUMN(ExportedType, TypeNamespace, 3),
    MDTABLE_COLUMN(ExportedType, Implementation, 4),

    MDTABLE_COLUMN(ManifestResource, Offset, 0),
    MDTABLE_COLUMN(ManifestResource, Flags, 1),
    MDTABLE_COLUMN(ManifestResource, Name, 2),
    MDTABLE_COLUMN(ManifestResource, Implementation, 3),

    MDTABLE_COLUMN(NestedClass, NestedClass, 0),
    MDTABLE_COLUMN(NestedClass, EnclosingClass, 1),

    MDTABLE_COLUMN(GenericParam, Number, 0),
    MDTABLE_COLUMN(GenericParam, Flags, 1),
    MDTABLE_COLUMN(GenericParam, Owner, 2),
    MDTABLE_COLUMN(GenericParam, Name, 3),

    MDTABLE_COLUMN(MethodSpec, Method, 0),
    MDTABLE_COLUMN(MethodSpec, Instantiation, 1),

    MDTABLE_COLUMN(GenericParamConstraint, Owner, 0),
    MDTABLE_COLUMN(GenericParamConstraint, Constraint, 1),

#ifdef DNMD_PORTABLE_PDB
    // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
    MDTABLE_COLUMN(Document, Name, 0),
    MDTABLE_COLUMN(Document, HashAlgorithm, 1),
    MDTABLE_COLUMN(Document, Hash, 2),
    MDTABLE_COLUMN(Document, Language, 3),

    MDTABLE_COLUMN(MethodDebugInformation, Document, 0),
    MDTABLE_COLUMN(MethodDebugInformation, SequencePoints, 1),

    MDTABLE_COLUMN(LocalScope, Method, 0),
    MDTABLE_COLUMN(LocalScope, ImportScope, 1),
    MDTABLE_COLUMN(LocalScope, VariableList, 2),
    MDTABLE_COLUMN(LocalScope, ConstantList, 3),
    MDTABLE_COLUMN(LocalScope, StartOffset, 4),
    MDTABLE_COLUMN(LocalScope, Length, 5),

    MDTABLE_COLUMN(LocalVariable, Attributes, 0),
    MDTABLE_COLUMN(LocalVariable, Index, 1),
    MDTABLE_COLUMN(LocalVariable, Name, 2),

    MDTABLE_COLUMN(LocalConstant, Name, 0),
    MDTABLE_COLUMN(LocalConstant, Signature, 1),

    MDTABLE_COLUMN(ImportScope, Parent, 0),
    MDTABLE_COLUMN(ImportScope, Imports, 1),

    MDTABLE_COLUMN(StateMachineMethod, MoveNextMethod, 0),
    MDTABLE_COLUMN(StateMachineMethod, KickoffMethod, 1),

    MDTABLE_COLUMN(CustomDebugInformation, Parent, 0),
    MDTABLE_COLUMN(CustomDebugInformation, Kind, 1),
    MDTABLE_COLUMN(CustomDebugInformation, Value, 2),
#endif // DNMD_PORTABLE_PDB

} col_index_t;

// Query row's column values
int32_t md_get_column_value_as_token(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdToken* tk);
// The returned number represents the number of valid cursor(s) for indexing.
int32_t md_get_column_value_as_cursor(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdcursor_t* cursor);
// Resolve the column to a cursor and a range based on the "run" pattern in tables.
// The run continues to the smaller of:
//   * the last row of the target table
//   * the next run in the target table, found by inspecting the column value of the next row in the current table.
bool md_get_column_value_as_range(mdcursor_t c, col_index_t col_idx, mdcursor_t* cursor, uint32_t* count);
int32_t md_get_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint32_t* constant);
int32_t md_get_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, uint32_t out_length, char const** str);
int32_t md_get_column_value_as_wchar(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mduserstring_t* strings);
int32_t md_get_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint8_t const** blob, uint32_t* blob_len);
int32_t md_get_column_value_as_guid(mdcursor_t c,col_index_t col_idx, uint32_t out_length, GUID* guid);

// Find a row or range of rows where the supplied column has the expected value.
bool md_find_row_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* cursor);
bool md_find_range_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* start, uint32_t* count);

#ifdef __cplusplus
}
#endif

#endif // _SRC_INC_DNMD_H_
