// Never permit debugging the column lookup logic.
// Communicate this request by defining the following macro.
#define MDTABLES_BUILD
#include "internal.h"

// Computed values from II.24.2.6
static mdtable_id_t const TypeDefOrRef[] = { mdtid_TypeDef, mdtid_TypeRef, mdtid_TypeSpec };
static mdtable_id_t const HasConstant[] = { mdtid_Field, mdtid_Param, mdtid_Property };
static mdtable_id_t const HasCustomAttribute[] =
{
    mdtid_MethodDef,
    mdtid_Field,
    mdtid_TypeRef,
    mdtid_TypeDef,
    mdtid_Param,
    mdtid_InterfaceImpl,
    mdtid_MemberRef,
    mdtid_Module,
    mdtid_DeclSecurity,
    mdtid_Property,
    mdtid_Event,
    mdtid_StandAloneSig,
    mdtid_ModuleRef,
    mdtid_TypeSpec,
    mdtid_Assembly,
    mdtid_AssemblyRef,
    mdtid_File,
    mdtid_ExportedType,
    mdtid_ManifestResource,
    mdtid_GenericParam,
    mdtid_GenericParamConstraint,
    mdtid_MethodSpec
};
static mdtable_id_t const HasFieldMarshall[] = { mdtid_Field, mdtid_Param };
static mdtable_id_t const HasDeclSecurity[] = { mdtid_TypeDef, mdtid_MethodDef, mdtid_Assembly };
static mdtable_id_t const MemberRefParent[] = { mdtid_TypeDef, mdtid_TypeRef, mdtid_ModuleRef, mdtid_MethodDef, mdtid_TypeSpec };
static mdtable_id_t const HasSemantics[] = { mdtid_Event, mdtid_Property };
static mdtable_id_t const MethodDefOrRef[] = { mdtid_MethodDef, mdtid_MemberRef };
static mdtable_id_t const MemberForwarded[] = { mdtid_Field, mdtid_MethodDef };
static mdtable_id_t const Implementation[] = { mdtid_File, mdtid_AssemblyRef, mdtid_ExportedType };
static mdtable_id_t const CustomAttributeType[] = { mdtid_Unused, mdtid_Unused, mdtid_MethodDef, mdtid_MemberRef, mdtid_Unused };
static mdtable_id_t const ResolutionScope[] = { mdtid_Module, mdtid_ModuleRef, mdtid_AssemblyRef, mdtid_TypeRef };
static mdtable_id_t const TypeOrMethodDef[] = { mdtid_TypeDef, mdtid_MethodDef };

#ifdef DNMD_PORTABLE_PDB
static mdtable_id_t const HasCustomDebugInformation[] =
{
    mdtid_MethodDef,
    mdtid_Field,
    mdtid_TypeRef,
    mdtid_TypeDef,
    mdtid_Param,
    mdtid_InterfaceImpl,
    mdtid_MemberRef,
    mdtid_Module,
    mdtid_DeclSecurity,
    mdtid_Property,
    mdtid_Event,
    mdtid_StandAloneSig,
    mdtid_ModuleRef,
    mdtid_TypeSpec,
    mdtid_Assembly,
    mdtid_AssemblyRef,
    mdtid_File,
    mdtid_ExportedType,
    mdtid_ManifestResource,
    mdtid_GenericParam,
    mdtid_GenericParamConstraint,
    mdtid_MethodSpec,
    mdtid_Document,
    mdtid_LocalScope,
    mdtid_LocalVariable,
    mdtid_LocalConstant,
    mdtid_ImportScope,
};
#endif // DNMD_PORTABLE_PDB

typedef struct
{
    // Coded index lookup
    mdtable_id_t const* lookup;
    // Coded index lookup length
    uint8_t const lookup_len;
    // Number of bits needed to encode lookup index
    uint8_t const bit_encoding_size;
} coded_index_entry_t;

static coded_index_entry_t const coded_index_map[] =
{
    { TypeDefOrRef, ARRAY_SIZE(TypeDefOrRef), 2},
    { HasConstant, ARRAY_SIZE(HasConstant), 2},
    { HasCustomAttribute, ARRAY_SIZE(HasCustomAttribute), 5},
    { HasFieldMarshall, ARRAY_SIZE(HasFieldMarshall), 1},
    { HasDeclSecurity, ARRAY_SIZE(HasDeclSecurity), 2 },
    { MemberRefParent, ARRAY_SIZE(MemberRefParent), 3 },
    { HasSemantics, ARRAY_SIZE(HasSemantics), 1 },
    { MethodDefOrRef, ARRAY_SIZE(MethodDefOrRef), 1 },
    { MemberForwarded, ARRAY_SIZE(MemberForwarded), 1 },
    { Implementation, ARRAY_SIZE(Implementation), 2 },
    { CustomAttributeType, ARRAY_SIZE(CustomAttributeType), 3 },
    { ResolutionScope, ARRAY_SIZE(ResolutionScope), 2 },
    { TypeOrMethodDef, ARRAY_SIZE(TypeOrMethodDef), 1 },
#ifdef DNMD_PORTABLE_PDB
    { HasCustomDebugInformation, ARRAY_SIZE(HasCustomDebugInformation), 5 },
#endif // DNMD_PORTABLE_PDB
};

bool compose_coded_index(mdToken tk, mdtcol_t col_details, uint32_t* coded_index)
{
    // See II.24.2.6
    assert(coded_index != NULL);

    // Use the embedded index into the coded index map.
    size_t ci_idx = ExtractCodedIndex(col_details);
    assert(ci_idx < ARRAY_SIZE(coded_index_map));
    coded_index_entry_t const* ci_entry = &coded_index_map[ci_idx];

    // Verify the supplied table type is valid for encoding.
    mdtable_id_t tgt_table = ExtractTokenType(tk);
    uint32_t row;
    for (uint8_t i = 0; i < ci_entry->lookup_len; ++i)
    {
        // If the table is valid, construct the coded index.
        if (ci_entry->lookup[i] == tgt_table)
        {
            row = RidFromToken(tk);
            *coded_index = (row << ci_entry->bit_encoding_size) | (uint32_t)i;
            return true;
        }
    }
    return false;
}

bool decompose_coded_index(uint32_t cidx, mdtcol_t col_details, mdtable_id_t* table_id, uint32_t* table_row)
{
    // See II.24.2.6
    assert(table_id != NULL && table_row != NULL);

    // Use the embedded index into the coded index map.
    size_t ci_idx = ExtractCodedIndex(col_details);
    assert(ci_idx < ARRAY_SIZE(coded_index_map));
    coded_index_entry_t const* ci_entry = &coded_index_map[ci_idx];

    // Create a mask to extract the index into the entry.
    uint32_t code_mask = (1 << ci_entry->bit_encoding_size) - 1;
    if ((cidx & code_mask) >= ci_entry->lookup_len)
        return false;
    *table_id = ci_entry->lookup[cidx & code_mask];

    // Remove the encoded lookup index.
    *table_row = cidx >> ci_entry->bit_encoding_size;
    return true;
}

bool is_coded_index_target(mdtcol_t col_details, mdtable_id_t table)
{
    assert(table != mdtid_Unused);

    size_t ci_idx = ExtractCodedIndex(col_details);
    assert(ci_idx < ARRAY_SIZE(coded_index_map));
    coded_index_entry_t const* ci_entry = &coded_index_map[ci_idx];
    for (uint8_t i = 0; i < ci_entry->lookup_len; ++i)
    {
        // If the table is valid, construct the coded index.
        if (ci_entry->lookup[i] == table)
        {
            return true;
        }
    }
    return false;
}

// Look up table for column counts by table ID
static uint8_t const table_column_counts[] =
{
    mdtModule_ColCount,
    mdtTypeRef_ColCount,
    mdtTypeDef_ColCount,
    mdtFieldPtr_ColCount,
    mdtField_ColCount,
    mdtMethodPtr_ColCount,
    mdtMethodDef_ColCount,
    mdtParamPtr_ColCount,
    mdtParam_ColCount,
    mdtInterfaceImpl_ColCount,
    mdtMemberRef_ColCount,
    mdtConstant_ColCount,
    mdtCustomAttribute_ColCount,
    mdtFieldMarshal_ColCount,
    mdtDeclSecurity_ColCount,
    mdtClassLayout_ColCount,
    mdtFieldLayout_ColCount,
    mdtStandAloneSig_ColCount,
    mdtEventMap_ColCount,
    mdtEventPtr_ColCount,
    mdtEvent_ColCount,
    mdtPropertyMap_ColCount,
    mdtPropertyPtr_ColCount,
    mdtProperty_ColCount,
    mdtMethodSemantics_ColCount,
    mdtMethodImpl_ColCount,
    mdtModuleRef_ColCount,
    mdtTypeSpec_ColCount,
    mdtImplMap_ColCount,
    mdtFieldRva_ColCount,
    2, // ENCLog
    1, // ENCMap
    mdtAssembly_ColCount,
    1, // AssemblyProcessor
    3, // AssemblyOS
    mdtAssemblyRef_ColCount,
    2, // AssemblyRefProcessor,
    4, // AssemblyRefOS,
    mdtFile_ColCount,
    mdtExportedType_ColCount,
    mdtManifestResource_ColCount,
    mdtNestedClass_ColCount,
    mdtGenericParam_ColCount,
    mdtMethodSpec_ColCount,
    mdtGenericParamConstraint_ColCount,
#ifdef DNMD_PORTABLE_PDB
    0, // Reserved
    0, // Reserved
    0, // Reserved
    // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
    mdtDocument_ColCount,
    mdtMethodDebugInformation_ColCount,
    mdtLocalScope_ColCount,
    mdtLocalVariable_ColCount,
    mdtLocalConstant_ColCount,
    mdtImportScope_ColCount,
    mdtStateMachineMethod_ColCount,
    mdtCustomDebugInformation_ColCount,
#endif // DNMD_PORTABLE_PDB
};
static_assert(ARRAY_SIZE(table_column_counts) == MDTABLE_MAX_COUNT, "Table column count must match max count");

uint8_t get_table_column_count(mdtable_id_t id)
{
    assert(mdtid_First <= id && id < mdtid_End);
    return table_column_counts[id];
}

// II.22 Metadata logical format tables
// DNMD implements the augments to the metadata logical format in the ECMA-335 spec located at https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md
static md_key_info_t const keys_ClassLayout[] = { { mdtClassLayout_Parent, false } };
static md_key_info_t const keys_Constant[] = { { mdtConstant_Parent, false } };
static md_key_info_t const keys_CustomAttribute[] = { { mdtCustomAttribute_Parent, false } };
static md_key_info_t const keys_DeclSecurity[] = { { mdtDeclSecurity_Parent, false } };
static md_key_info_t const keys_FieldLayout[] = { { mdtFieldLayout_Field, false } };
static md_key_info_t const keys_FieldMarshal[] = { { mdtFieldMarshal_Parent, false } };
static md_key_info_t const keys_FieldRva[] = { { mdtFieldRva_Field, false } };
static md_key_info_t const keys_GenericParam[] = { { mdtGenericParam_Owner, false }, { mdtGenericParam_Number, false } };
static md_key_info_t const keys_GenericParamConstraint[] = { { mdtGenericParamConstraint_Owner, false } };
static md_key_info_t const keys_ImplMap[] = { { mdtImplMap_MemberForwarded, false } };
static md_key_info_t const keys_InterfaceImpl[] = { { mdtInterfaceImpl_Class, false } };
static md_key_info_t const keys_MethodImpl[] = { { mdtMethodImpl_Class, false } };
static md_key_info_t const keys_MethodSemantics[] = { { mdtMethodSemantics_Association, false } };
static md_key_info_t const keys_NestedClass[] = { { mdtNestedClass_NestedClass, false } };

#ifdef DNMD_PORTABLE_PDB
// https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
static md_key_info_t const keys_LocalScope[] = { { mdtLocalScope_Method, false }, { mdtLocalScope_StartOffset, false }, { mdtLocalScope_Length, true } };
static md_key_info_t const keys_StateMachineMethod[] = { { mdtStateMachineMethod_MoveNextMethod, false } };
static md_key_info_t const keys_CustomDebugInformation[] = { { mdtCustomDebugInformation_Parent, false } };
#endif

typedef struct
{
    md_key_info_t const* keys;
    uint8_t key_count;
} keys_info_t;

static keys_info_t const table_keys[] =
{
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { keys_InterfaceImpl, ARRAY_SIZE(keys_InterfaceImpl) },
    { NULL, 0 },
    { keys_Constant, ARRAY_SIZE(keys_Constant) },
    { keys_CustomAttribute, ARRAY_SIZE(keys_CustomAttribute) },
    { keys_FieldMarshal, ARRAY_SIZE(keys_FieldMarshal) },
    { keys_DeclSecurity, ARRAY_SIZE(keys_DeclSecurity) },
    { keys_ClassLayout, ARRAY_SIZE(keys_ClassLayout) },
    { keys_FieldLayout, ARRAY_SIZE(keys_FieldLayout) },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { keys_MethodSemantics, ARRAY_SIZE(keys_MethodSemantics) },
    { keys_MethodImpl, ARRAY_SIZE(keys_MethodImpl) },
    { NULL, 0 },
    { NULL, 0 },
    { keys_ImplMap, ARRAY_SIZE(keys_ImplMap) },
    { keys_FieldRva, ARRAY_SIZE(keys_FieldRva) },
    { NULL, 0 }, // ENCLog
    { NULL, 0 }, // ENCMap
    { NULL, 0 },
    { NULL, 0 }, // AssemblyProcessor
    { NULL, 0 }, // AssemblyOS
    { NULL, 0 },
    { NULL, 0 }, // AssemblyRefProcessor,
    { NULL, 0 }, // AssemblyRefOS,
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { keys_NestedClass, ARRAY_SIZE(keys_NestedClass) },
    { keys_GenericParam, ARRAY_SIZE(keys_GenericParam) },
    { NULL, 0 },
    { keys_GenericParamConstraint, ARRAY_SIZE(keys_GenericParamConstraint) },
#ifdef DNMD_PORTABLE_PDB
    { NULL, 0 }, // Reserved
    { NULL, 0 }, // Reserved
    { NULL, 0 }, // Reserved
    // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
    { NULL, 0 },
    { NULL, 0 },
    { keys_LocalScope, ARRAY_SIZE(keys_LocalScope) },
    { NULL, 0 },
    { NULL, 0 },
    { NULL, 0 },
    { keys_StateMachineMethod, ARRAY_SIZE(keys_StateMachineMethod) },
    { keys_CustomDebugInformation, ARRAY_SIZE(keys_CustomDebugInformation) },
#endif // DNMD_PORTABLE_PDB
};

// II.22 Metadata logical format tables
// Primary and secondary key info for tables
uint8_t get_table_keys(mdtable_id_t id, md_key_info_t const** keys)
{
    assert(mdtid_First <= id && id < mdtid_End);
    *keys = table_keys[id].keys;
    return table_keys[id].key_count;
}

// Compute the row size and embed the offset for
// each column in the column details.
static uint32_t compute_row_offsets_size(mdtcol_t* col, size_t col_len)
{
    uint32_t c = 0;
    for (size_t i = 0; i < col_len; i++)
    {
        assert((mdtc_b2 | mdtc_b4) & col[i]);
        col[i] |= InsertOffset(c);
        c += ((col[i] & mdtc_b2) ? 2 : 4);
    }
    return c;
}

// II.24.2.6
static mdtcol_t compute_coded_index(bool is_minimal_delta, uint32_t const* row_counts, md_coded_idx_t coded_map_idx)
{
    assert(coded_map_idx < ARRAY_SIZE(coded_index_map));
    assert(coded_map_idx <= ExtractCodedIndex(mdtc_cimask) && "Coded index map index bit encoding exceeded");

    coded_index_entry_t const* entry = &coded_index_map[coded_map_idx];

    uint32_t rc;
    uint32_t m = 0;
    for (uint8_t i = 0; i < entry->lookup_len; ++i)
    {
        if (entry->lookup[i] != mdtid_Unused)
        {
            rc = row_counts[entry->lookup[i]];
            if (m < rc)
                m = rc;
        }
    }
    uint32_t max_rows_2b = (uint32_t)1 << (16 - entry->bit_encoding_size);
    return InsertCodedIndex(coded_map_idx) | mdtc_idx_coded | (m < max_rows_2b && !is_minimal_delta ? mdtc_b2 : mdtc_b4);
}

static mdtcol_t compute_table_index(bool is_minimal_delta, uint32_t const* row_counts, mdtable_id_t id)
{
    assert(row_counts != NULL && (mdtid_First <= id && id < mdtid_End));
    return InsertTable(id) | (row_counts[id] < (1 << 16) && !is_minimal_delta ? mdtc_b2 : mdtc_b4) | mdtc_idx_table;
}

static mdtable_id_t get_target_table(uint32_t const* all_table_row_counts, mdtable_id_t direct_table, mdtable_id_t indirect_table)
{
    assert(all_table_row_counts != NULL);
    assert(mdtid_First <= direct_table && direct_table < mdtid_End);
    assert(mdtid_First <= indirect_table && indirect_table < mdtid_End);
    return all_table_row_counts[indirect_table] != 0 ? indirect_table : direct_table;
}

bool initialize_table_details(
    uint32_t const* all_table_row_counts,
    mdcxt_flag_t context_flags,
    mdtable_id_t id,
    bool is_sorted,
    mdtable_t* table)
{
    assert(all_table_row_counts != NULL && (mdtid_First <= id && id < mdtid_End) && table != NULL);
    assert(all_table_row_counts[id] != 0 && "Unable to initialize a table with a row count of 0.");
    if (all_table_row_counts[id] == 0)
        return false;

    mdtcol_t const string_index = mdtc_idx_heap | mdtc_hstring | (context_flags & mdc_large_string_heap ? mdtc_b4 : mdtc_b2);
    mdtcol_t const guid_index = mdtc_idx_heap | mdtc_hguid | (context_flags & mdc_large_guid_heap ? mdtc_b4 : mdtc_b2);
    mdtcol_t const blob_index = mdtc_idx_heap | mdtc_hblob | (context_flags & mdc_large_blob_heap ? mdtc_b4 : mdtc_b2);

    bool is_minimal_delta = (context_flags & mdc_minimal_delta) == mdc_minimal_delta;

    table->row_count = all_table_row_counts[id];
    table->is_sorted = is_sorted;
    table->table_id = (uint8_t)id;

#define CODED_INDEX_ARGS(x) is_minimal_delta, all_table_row_counts, (x)
#define TABLE_INDEX_ARGS(x) is_minimal_delta, all_table_row_counts, (x)
    switch (id)
    {
    case mdtid_Module: // II.22.30
        table->column_details[mdtModule_Generation] = mdtc_constant | mdtc_b2;
        table->column_details[mdtModule_Name] = string_index;
        table->column_details[mdtModule_Mvid] = guid_index;
        table->column_details[mdtModule_EncId] = guid_index;
        table->column_details[mdtModule_EncBaseId] = guid_index;
        assert(mdtModule_ColCount == get_table_column_count(id));
        break;
    case mdtid_TypeRef: // II.22.38
        table->column_details[mdtTypeRef_ResolutionScope] = compute_coded_index(CODED_INDEX_ARGS(mdci_ResolutionScope));
        table->column_details[mdtTypeRef_TypeName] = string_index;
        table->column_details[mdtTypeRef_TypeNamespace] = string_index;
        assert(mdtTypeRef_ColCount == get_table_column_count(id));
        break;
    case mdtid_TypeDef: // II.22.37
        table->column_details[mdtTypeDef_Flags] = mdtc_constant | mdtc_b4;
        table->column_details[mdtTypeDef_TypeName] = string_index;
        table->column_details[mdtTypeDef_TypeNamespace] = string_index;
        table->column_details[mdtTypeDef_Extends] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        table->column_details[mdtTypeDef_FieldList] = compute_table_index(TABLE_INDEX_ARGS(get_target_table(all_table_row_counts, mdtid_Field, mdtid_FieldPtr)));
        table->column_details[mdtTypeDef_MethodList] = compute_table_index(TABLE_INDEX_ARGS(get_target_table(all_table_row_counts, mdtid_MethodDef, mdtid_MethodPtr)));
        assert(mdtTypeDef_ColCount == get_table_column_count(id));
        break;
    case mdtid_FieldPtr: // Not in ECMA
        table->column_details[mdtFieldPtr_Field] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Field));
        assert(mdtFieldPtr_ColCount == get_table_column_count(id));
        break;
    case mdtid_Field: // II.22.15
        table->column_details[mdtField_Flags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtField_Name] = string_index;
        table->column_details[mdtField_Signature] = blob_index;
        assert(mdtField_ColCount == get_table_column_count(id));
        break;
    case mdtid_MethodPtr: // Not in ECMA
        table->column_details[mdtMethodPtr_Method] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        assert(mdtMethodPtr_ColCount == get_table_column_count(id));
        break;
    case mdtid_MethodDef: // II.22.26
        table->column_details[mdtMethodDef_Rva] = mdtc_constant | mdtc_b4;
        table->column_details[mdtMethodDef_ImplFlags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtMethodDef_Flags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtMethodDef_Name] = string_index;
        table->column_details[mdtMethodDef_Signature] = blob_index;
        table->column_details[mdtMethodDef_ParamList] = compute_table_index(TABLE_INDEX_ARGS(get_target_table(all_table_row_counts, mdtid_Param, mdtid_ParamPtr)));
        assert(mdtMethodDef_ColCount == get_table_column_count(id));
        break;
    case mdtid_ParamPtr: // Not in ECMA
        table->column_details[mdtParamPtr_Param] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Param));
        assert(mdtParamPtr_ColCount == get_table_column_count(id));
        break;
    case mdtid_Param: // II.22.33
        table->column_details[mdtParam_Flags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtParam_Sequence] = mdtc_constant | mdtc_b2;
        table->column_details[mdtParam_Name] = string_index;
        assert(mdtParam_ColCount == get_table_column_count(id));
        break;
    case mdtid_InterfaceImpl: // II.22.23
        table->column_details[mdtInterfaceImpl_Class] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[mdtInterfaceImpl_Interface] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        assert(mdtInterfaceImpl_ColCount == get_table_column_count(id));
        break;
    case mdtid_MemberRef: // II.22.25
        table->column_details[mdtMemberRef_Class] = compute_coded_index(CODED_INDEX_ARGS(mdci_MemberRefParent));
        table->column_details[mdtMemberRef_Name] = string_index;
        table->column_details[mdtMemberRef_Signature] = blob_index;
        assert(mdtMemberRef_ColCount == get_table_column_count(id));
        break;
    case mdtid_Constant: // II.22.9
        table->column_details[mdtConstant_Type] = mdtc_constant | mdtc_b2;
        table->column_details[mdtConstant_Parent] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasConstant));
        table->column_details[mdtConstant_Value] = blob_index;
        assert(mdtConstant_ColCount == get_table_column_count(id));
        break;
    case mdtid_CustomAttribute: // II.22.10
        table->column_details[mdtCustomAttribute_Parent] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasCustomAttribute));
        table->column_details[mdtCustomAttribute_Type] = compute_coded_index(CODED_INDEX_ARGS(mdci_CustomAttributeType));
        table->column_details[mdtCustomAttribute_Value] = blob_index;
        assert(mdtCustomAttribute_ColCount == get_table_column_count(id));
        break;
    case mdtid_FieldMarshal: // II.22.17
        table->column_details[mdtFieldMarshal_Parent] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasFieldMarshall));
        table->column_details[mdtFieldMarshal_NativeType] = blob_index;
        assert(mdtFieldMarshal_ColCount == get_table_column_count(id));
        break;
    case mdtid_DeclSecurity: // II.22.11
        table->column_details[mdtDeclSecurity_Action] = mdtc_constant | mdtc_b2;
        table->column_details[mdtDeclSecurity_Parent] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasDeclSecurity));
        table->column_details[mdtDeclSecurity_PermissionSet] = blob_index;
        assert(mdtDeclSecurity_ColCount == get_table_column_count(id));
        break;
    case mdtid_ClassLayout: // II.22.8
        table->column_details[mdtClassLayout_PackingSize] = mdtc_constant | mdtc_b2;
        table->column_details[mdtClassLayout_ClassSize] = mdtc_constant | mdtc_b4;
        table->column_details[mdtClassLayout_Parent] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        assert(mdtClassLayout_ColCount == get_table_column_count(id));
        break;
    case mdtid_FieldLayout: // II.22.16
        table->column_details[mdtFieldLayout_Offset] = mdtc_constant | mdtc_b4;
        table->column_details[mdtFieldLayout_Field] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Field));
        assert(mdtFieldLayout_ColCount == get_table_column_count(id));
        break;
    case mdtid_StandAloneSig: // II.22.36
        table->column_details[mdtStandAloneSig_Signature] = blob_index;
        assert(mdtStandAloneSig_ColCount == get_table_column_count(id));
        break;
    case mdtid_EventMap: // II.22.12
        table->column_details[mdtEventMap_Parent] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[mdtEventMap_EventList] = compute_table_index(TABLE_INDEX_ARGS(get_target_table(all_table_row_counts, mdtid_Event, mdtid_EventPtr)));
        assert(mdtEventMap_ColCount == get_table_column_count(id));
        break;
    case mdtid_EventPtr: // Not in ECMA
        table->column_details[mdtEventPtr_Event] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Event));
        assert(mdtEventPtr_ColCount == get_table_column_count(id));
        break;
    case mdtid_Event:// II.22.13
        table->column_details[mdtEvent_EventFlags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtEvent_Name] = string_index;
        table->column_details[mdtEvent_EventType] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        assert(mdtEvent_ColCount == get_table_column_count(id));
        break;
    case mdtid_PropertyMap: // II.22.35
        table->column_details[mdtPropertyMap_Parent] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[mdtPropertyMap_PropertyList] = compute_table_index(TABLE_INDEX_ARGS(get_target_table(all_table_row_counts, mdtid_Property, mdtid_PropertyPtr)));
        assert(mdtPropertyMap_ColCount == get_table_column_count(id));
        break;
    case mdtid_PropertyPtr: // Not in ECMA
        table->column_details[mdtPropertyPtr_Property] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Property));
        assert(mdtPropertyPtr_ColCount == get_table_column_count(id));
        break;
    case mdtid_Property: // II.22.34
        table->column_details[mdtProperty_Flags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtProperty_Name] = string_index;
        table->column_details[mdtProperty_Type] = blob_index;
        assert(mdtProperty_ColCount == get_table_column_count(id));
        break;
    case mdtid_MethodSemantics: // II.22.28
        table->column_details[mdtMethodSemantics_Semantics] = mdtc_constant | mdtc_b2;
        table->column_details[mdtMethodSemantics_Method] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        table->column_details[mdtMethodSemantics_Association] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasSemantics));
        assert(mdtMethodSemantics_ColCount == get_table_column_count(id));
        break;
    case mdtid_MethodImpl: // II.22.27
        table->column_details[mdtMethodImpl_Class] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[mdtMethodImpl_MethodBody] = compute_coded_index(CODED_INDEX_ARGS(mdci_MethodDefOrRef));
        table->column_details[mdtMethodImpl_MethodDeclaration] = compute_coded_index(CODED_INDEX_ARGS(mdci_MethodDefOrRef));
        assert(mdtMethodImpl_ColCount == get_table_column_count(id));
        break;
    case mdtid_ModuleRef: // II.22.31
        table->column_details[mdtModuleRef_Name] = string_index;
        assert(mdtModuleRef_ColCount == get_table_column_count(id));
        break;
    case mdtid_TypeSpec: // II.22.39
        table->column_details[mdtTypeSpec_Signature] = blob_index;
        assert(mdtTypeSpec_ColCount == get_table_column_count(id));
        break;
    case mdtid_ImplMap: // II.22.22
        table->column_details[mdtImplMap_MappingFlags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtImplMap_MemberForwarded] = compute_coded_index(CODED_INDEX_ARGS(mdci_MemberForwarded));
        table->column_details[mdtImplMap_ImportName] = string_index;
        table->column_details[mdtImplMap_ImportScope] = compute_table_index(TABLE_INDEX_ARGS(mdtid_ModuleRef));
        assert(mdtImplMap_ColCount == get_table_column_count(id));
        break;
    case mdtid_FieldRva: // II.22.18
        table->column_details[mdtFieldRva_Rva] = mdtc_constant | mdtc_b4;
        table->column_details[mdtFieldRva_Field] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Field));
        assert(mdtFieldRva_ColCount == get_table_column_count(id));
        break;
    case mdtid_ENCLog:
        table->column_details[mdtENCLog_Token] = mdtc_constant | mdtc_b4;
        table->column_details[mdtENCLog_Op] = mdtc_constant | mdtc_b4;
        assert(mdtENCLog_ColCount == get_table_column_count(id));
        break;
    case mdtid_ENCMap:
        table->column_details[mdtENCMap_Token] = mdtc_constant | mdtc_b4;
        assert(mdtENCMap_ColCount == get_table_column_count(id));
        break;
    case mdtid_Assembly: // II.22.2
        table->column_details[mdtAssembly_HashAlgId] = mdtc_constant | mdtc_b4;
        table->column_details[mdtAssembly_MajorVersion] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssembly_MinorVersion] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssembly_BuildNumber] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssembly_RevisionNumber] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssembly_Flags] = mdtc_constant | mdtc_b4;
        table->column_details[mdtAssembly_PublicKey] = blob_index;
        table->column_details[mdtAssembly_Name] = string_index;
        table->column_details[mdtAssembly_Culture] = string_index;
        assert(mdtAssembly_ColCount == get_table_column_count(id));
        break;
    case mdtid_AssemblyProcessor: // II.22.3
        table->column_details[0] = mdtc_constant | mdtc_b4;
        assert(1 == get_table_column_count(id));
        break;
    case mdtid_AssemblyOS: // II.22.4
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = mdtc_constant | mdtc_b4;
        assert(3 == get_table_column_count(id));
        break;
    case mdtid_AssemblyRef: // II.22.5
        table->column_details[mdtAssemblyRef_MajorVersion] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssemblyRef_MinorVersion] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssemblyRef_BuildNumber] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssemblyRef_RevisionNumber] = mdtc_constant | mdtc_b2;
        table->column_details[mdtAssemblyRef_Flags] = mdtc_constant | mdtc_b4;
        table->column_details[mdtAssemblyRef_PublicKeyOrToken] = blob_index;
        table->column_details[mdtAssemblyRef_Name] = string_index;
        table->column_details[mdtAssemblyRef_Culture] = string_index;
        table->column_details[mdtAssemblyRef_HashValue] = blob_index;
        assert(mdtAssemblyRef_ColCount == get_table_column_count(id));
        break;
    case mdtid_AssemblyRefProcessor: // II.22.7
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_AssemblyRef));
        assert(2 == get_table_column_count(id));
        break;
    case mdtid_AssemblyRefOS: // II.22.6
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = mdtc_constant | mdtc_b4;
        table->column_details[3] = compute_table_index(TABLE_INDEX_ARGS(mdtid_AssemblyRef));
        assert(4 == get_table_column_count(id));
        break;
    case mdtid_File: // II.22.19
        table->column_details[mdtFile_Flags] = mdtc_constant | mdtc_b4;
        table->column_details[mdtFile_Name] = string_index;
        table->column_details[mdtFile_HashValue] = blob_index;
        assert(mdtFile_ColCount == get_table_column_count(id));
        break;
    case mdtid_ExportedType: // II.22.14
        table->column_details[mdtExportedType_Flags] = mdtc_constant | mdtc_b4;
        table->column_details[mdtExportedType_TypeDefId] = mdtc_constant | mdtc_b4;
        table->column_details[mdtExportedType_TypeName] = string_index;
        table->column_details[mdtExportedType_TypeNamespace] = string_index;
        table->column_details[mdtExportedType_Implementation] = compute_coded_index(CODED_INDEX_ARGS(mdci_Implementation));
        assert(mdtExportedType_ColCount == get_table_column_count(id));
        break;
    case mdtid_ManifestResource: // II.22.24
        table->column_details[mdtManifestResource_Offset] = mdtc_constant | mdtc_b4;
        table->column_details[mdtManifestResource_Flags] = mdtc_constant | mdtc_b4;
        table->column_details[mdtManifestResource_Name] = string_index;
        table->column_details[mdtManifestResource_Implementation] = compute_coded_index(CODED_INDEX_ARGS(mdci_Implementation));
        assert(mdtManifestResource_ColCount == get_table_column_count(id));
        break;
    case mdtid_NestedClass: // II.22.32
        table->column_details[mdtNestedClass_NestedClass] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[mdtNestedClass_EnclosingClass] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        assert(mdtNestedClass_ColCount == get_table_column_count(id));
        break;
    case mdtid_GenericParam: // II.22.20
        table->column_details[mdtGenericParam_Number] = mdtc_constant | mdtc_b2;
        table->column_details[mdtGenericParam_Flags] = mdtc_constant | mdtc_b2;
        table->column_details[mdtGenericParam_Owner] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeOrMethodDef));
        table->column_details[mdtGenericParam_Name] = string_index;
        assert(mdtGenericParam_ColCount == get_table_column_count(id));
        break;
    case mdtid_MethodSpec: // II.22.29
        table->column_details[mdtMethodSpec_Method] = compute_coded_index(CODED_INDEX_ARGS(mdci_MethodDefOrRef));
        table->column_details[mdtMethodSpec_Instantiation] = blob_index;
        assert(mdtMethodSpec_ColCount == get_table_column_count(id));
        break;
    case mdtid_GenericParamConstraint: // II.22.21
        table->column_details[mdtGenericParamConstraint_Owner] = compute_table_index(TABLE_INDEX_ARGS(mdtid_GenericParam));
        table->column_details[mdtGenericParamConstraint_Constraint] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        assert(mdtGenericParamConstraint_ColCount == get_table_column_count(id));
        break;

#ifdef DNMD_PORTABLE_PDB
    // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
    case mdtid_Document:
        table->column_details[mdtDocument_Name] = blob_index;
        table->column_details[mdtDocument_HashAlgorithm] = guid_index;
        table->column_details[mdtDocument_Hash] = blob_index;
        table->column_details[mdtDocument_Language] = guid_index;
        assert(mdtDocument_ColCount == get_table_column_count(id));
        break;
    case mdtid_MethodDebugInformation:
        table->column_details[mdtMethodDebugInformation_Document] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Document));
        table->column_details[mdtMethodDebugInformation_SequencePoints] = blob_index;
        assert(mdtMethodDebugInformation_ColCount == get_table_column_count(id));
        break;
    case mdtid_LocalScope:
        table->column_details[mdtLocalScope_Method] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        table->column_details[mdtLocalScope_ImportScope] = compute_table_index(TABLE_INDEX_ARGS(mdtid_ImportScope));
        table->column_details[mdtLocalScope_VariableList] = compute_table_index(TABLE_INDEX_ARGS(mdtid_LocalVariable));
        table->column_details[mdtLocalScope_ConstantList] = compute_table_index(TABLE_INDEX_ARGS(mdtid_LocalConstant));
        table->column_details[mdtLocalScope_StartOffset] = mdtc_constant | mdtc_b4;
        table->column_details[mdtLocalScope_Length] = mdtc_constant | mdtc_b4;
        assert(mdtLocalScope_ColCount == get_table_column_count(id));
        break;
    case mdtid_LocalVariable:
        table->column_details[mdtLocalVariable_Attributes] = mdtc_constant | mdtc_b2;
        table->column_details[mdtLocalVariable_Index] = mdtc_constant | mdtc_b2;
        table->column_details[mdtLocalVariable_Name] = string_index;
        assert(mdtLocalVariable_ColCount == get_table_column_count(id));
        break;
    case mdtid_LocalConstant:
        table->column_details[mdtLocalConstant_Name] = string_index;
        table->column_details[mdtLocalConstant_Signature] = blob_index;
        assert(mdtLocalConstant_ColCount == get_table_column_count(id));
        break;
    case mdtid_ImportScope:
        table->column_details[mdtImportScope_Parent] = compute_table_index(TABLE_INDEX_ARGS(mdtid_ImportScope));
        table->column_details[mdtImportScope_Imports] = blob_index;
        assert(mdtImportScope_ColCount == get_table_column_count(id));
        break;
    case mdtid_StateMachineMethod:
        table->column_details[mdtStateMachineMethod_MoveNextMethod] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        table->column_details[mdtStateMachineMethod_KickoffMethod] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        assert(mdtStateMachineMethod_ColCount == get_table_column_count(id));
        break;
    case mdtid_CustomDebugInformation:
        table->column_details[mdtCustomDebugInformation_Parent] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasCustomDebugInformation));
        table->column_details[mdtCustomDebugInformation_Kind] = guid_index;
        table->column_details[mdtCustomDebugInformation_Value] = blob_index;
        assert(mdtCustomDebugInformation_ColCount == get_table_column_count(id));
        break;
#endif // DNMD_PORTABLE_PDB

    default:
        assert(!"Unknown metadata table ID");
        return false;
    }
#undef TABLE_INDEX_ARGS
#undef CODED_INDEX_ARGS

    // Set the column count
    table->column_count = get_table_column_count(id);
    assert(table->column_count != 0);
    uint32_t size_bytes = compute_row_offsets_size(table->column_details, table->column_count);
    assert(size_bytes <= UINT8_MAX);
    table->row_size_bytes = (uint8_t)size_bytes;
    return true;
}

bool initialize_new_table_details(
    mdcxt_t* cxt,
    mdtable_id_t id,
    mdtable_t* table
)
{
    assert(table->cxt == NULL);
    // Use the real table row counts to ensure that when saving, we can
    // directly write out table memory without any required post-processing.
    uint32_t table_row_counts[MDTABLE_MAX_COUNT];
    for (size_t i = 0; i < MDTABLE_MAX_COUNT; i++)
    {
        table_row_counts[i] = cxt->tables[i].row_count;
    }

    // Set the new table's row count temporarily to 1 to ensure that we initialize the table.
    table_row_counts[id] = 1;

    // We'll treat any new table that has keys as sorted.
    // We only want to do this for tables with keys as tables without keys
    // never use the is_sorted bit.
    md_key_info_t const* keys;
    uint8_t key_count = get_table_keys(id, &keys);
    bool has_keys = key_count != 0;

    if (!initialize_table_details(
        table_row_counts,
        cxt->context_flags,
        id,
        has_keys,
        table))
        return false;

    table->row_count = 0;
    return true;
}

bool consume_table_rows(mdtable_t* table, uint8_t const** data, size_t* data_len)
{
    assert(table != NULL && data != NULL && data_len != NULL);
    assert(table->row_size_bytes != 0 && "Table with row byte length of 0 is unexpected");
    if (table->row_count == 0)
        return true;

    uint8_t const* rows = *data;
    size_t rows_len = table->row_size_bytes * (size_t)table->row_count;
    if (!advance_stream(data, data_len, rows_len))
        return false;

    table->data.ptr = rows;
    table->data.size = rows_len;
    return true;
}

bool table_is_indirect_table(mdtable_id_t table_id)
{
    switch (table_id)
    {
        case mdtid_FieldPtr:
        case mdtid_MethodPtr:
        case mdtid_ParamPtr:
        case mdtid_EventPtr:
        case mdtid_PropertyPtr:
            return true;
        default:
            return false;
    }
}

mdtable_id_t get_corresponding_indirection_table(mdtable_id_t table_id)
{
    switch (table_id)
    {
    case mdtid_Field:
        return mdtid_FieldPtr;
    case mdtid_MethodDef:
        return mdtid_MethodPtr;
    case mdtid_Param:
        return mdtid_ParamPtr;
    case mdtid_Event:
        return mdtid_EventPtr;
    case mdtid_Property:
        return mdtid_PropertyPtr;
    default:
        return mdtid_Unused;
    }
}