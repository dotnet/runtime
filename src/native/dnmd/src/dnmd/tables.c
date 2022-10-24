#include "internal.h"

// Computed values from II.24.2.6
static mdtable_id_t const TypeDefOrRef[] = { mdtid_TypeDef, mdtid_TypeRef, mdtid_TypeSpec };
static mdtable_id_t const HasConstant[] = { mdtid_Field, mdtid_Param, mdtid_Property };
static mdtable_id_t const HasCustomAttribute[] = {
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

coded_index_entry const coded_index_map[13] =
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
};

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
static mdtcol_t compute_coded_index(uint32_t const* row_counts, md_coded_idx_t coded_map_idx)
{
    assert(coded_map_idx < ARRAYSIZE(coded_index_map));
    assert(coded_map_idx <= ExtractCodedIndex(mdtc_cimask) && "Coded index map index bit encoding exceeded");

    coded_index_entry const* entry = &coded_index_map[coded_map_idx];

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
    return InsertCodedIndex(coded_map_idx) | mdtc_idx_coded | (m < max_rows_2b ? mdtc_b2 : mdtc_b4);
}

static mdtcol_t compute_table_index(uint32_t const* row_counts, mdtable_id_t id)
{
    assert(row_counts != NULL && (mdtid_First <= id && id <= mdtid_Last));
    return InsertTable(id) | (row_counts[id] < (1 << 16) ? mdtc_b2 : mdtc_b4) | mdtc_idx_table;
}

bool initialize_table_details(
    uint32_t const* all_table_row_counts,
    uint8_t heap_sizes,
    mdtable_id_t id,
    bool is_sorted,
    mdtable_t* table)
{
    assert(all_table_row_counts != NULL && (mdtid_First <= id && id <= mdtid_Last) && table != NULL);
    assert(all_table_row_counts[id] != 0 && "Unable to initialize a table with a row count of 0.");
    if (all_table_row_counts[id] == 0)
        return false;

    mdtcol_t const string_index = mdtc_idx_heap | mdtc_hstring | (heap_sizes & 0x1 ? mdtc_b4 : mdtc_b2);
    mdtcol_t const guid_index = mdtc_idx_heap | mdtc_hguid | (heap_sizes & 0x2 ? mdtc_b4 : mdtc_b2);
    mdtcol_t const blob_index = mdtc_idx_heap | mdtc_hblob | (heap_sizes & 0x4 ? mdtc_b4 : mdtc_b2);

    table->row_count = all_table_row_counts[id];
    table->is_sorted = is_sorted;
    table->table_id = (uint8_t)id;

#define CODED_INDEX_ARGS(x) all_table_row_counts, x
#define TABLE_INDEX_ARGS(x) all_table_row_counts, x
    switch (id)
    {
    case mdtid_Module: // II.22.30
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = string_index;
        table->column_details[2] = guid_index;
        table->column_details[3] = guid_index;
        table->column_details[4] = guid_index;
        table->column_count = 5;
        break;
    case mdtid_TypeRef: // II.22.38
        table->column_details[0] = compute_coded_index(CODED_INDEX_ARGS(mdci_ResolutionScope));
        table->column_details[1] = string_index;
        table->column_details[2] = string_index;
        table->column_count = 3;
        break;
    case mdtid_TypeDef: // II.22.37
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = string_index;
        table->column_details[2] = string_index;
        table->column_details[3] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        table->column_details[4] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Field));
        table->column_details[5] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        table->column_count = 6;
        break;
    case mdtid_Field: // II.22.15
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = string_index;
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_MethodDef: // II.22.26
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b2;
        table->column_details[2] = mdtc_constant | mdtc_b2;
        table->column_details[3] = string_index;
        table->column_details[4] = blob_index;
        table->column_details[5] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Param));
        table->column_count = 6;
        break;
    case mdtid_Param: // II.22.33
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = mdtc_constant | mdtc_b2;
        table->column_details[2] = string_index;
        table->column_count = 3;
        break;
    case mdtid_InterfaceImpl: // II.22.23
        table->column_details[0] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        table->column_count = 2;
        break;
    case mdtid_MemberRef: // II.22.25
        table->column_details[0] = compute_coded_index(CODED_INDEX_ARGS(mdci_MemberRefParent));
        table->column_details[1] = string_index;
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_Constant: // II.22.9
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasConstant));
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_CustomAttribute: // II.22.10
        table->column_details[0] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasCustomAttribute));
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_CustomAttributeType));
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_FieldMarshal: // II.22.17
        table->column_details[0] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasFieldMarshall));
        table->column_details[1] = blob_index;
        table->column_count = 2;
        break;
    case mdtid_DeclSecurity: // II.22.11
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasDeclSecurity));
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_ClassLayout: // II.22.8
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_count = 3;
        break;
    case mdtid_FieldLayout: // II.22.16
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Field));
        table->column_count = 2;
        break;
    case mdtid_StandAloneSig: // II.22.36
        table->column_details[0] = blob_index;
        table->column_count = 1;
        break;
    case mdtid_EventMap: // II.22.12
        table->column_details[0] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Event));
        table->column_count = 2;
        break;
    case mdtid_Event:// II.22.13
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = string_index;
        table->column_details[2] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        table->column_count = 3;
        break;
    case mdtid_PropertyMap: // II.22.35
        table->column_details[0] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Property));
        table->column_count = 2;
        break;
    case mdtid_Property: // II.22.34
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = string_index;
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_MethodSemantics: // II.22.28
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_MethodDef));
        table->column_details[2] = compute_coded_index(CODED_INDEX_ARGS(mdci_HasSemantics));
        table->column_count = 3;
        break;
    case mdtid_MethodImpl: // II.22.27
        table->column_details[0] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_MethodDefOrRef));
        table->column_details[2] = compute_coded_index(CODED_INDEX_ARGS(mdci_MethodDefOrRef));
        table->column_count = 3;
        break;
    case mdtid_ModuleRef: // II.22.31
        table->column_details[0] = string_index;
        table->column_count = 1;
        break;
    case mdtid_TypeSpec: // II.22.39
        table->column_details[0] = blob_index;
        table->column_count = 1;
        break;
    case mdtid_ImplMap: // II.22.22
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_MemberForwarded));
        table->column_details[2] = string_index;
        table->column_details[3] = compute_table_index(TABLE_INDEX_ARGS(mdtid_ModuleRef));
        table->column_count = 4;
        break;
    case mdtid_FieldRva: // II.22.18
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_Field));
        table->column_count = 2;
        break;
    case mdtid_Assembly: // II.22.2
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b2;
        table->column_details[2] = mdtc_constant | mdtc_b2;
        table->column_details[3] = mdtc_constant | mdtc_b2;
        table->column_details[4] = mdtc_constant | mdtc_b2;
        table->column_details[5] = mdtc_constant | mdtc_b4;
        table->column_details[6] = blob_index;
        table->column_details[7] = string_index;
        table->column_details[8] = string_index;
        table->column_count = 9;
        break;
    case mdtid_AssemblyProcessor: // II.22.3
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_count = 1;
        break;
    case mdtid_AssemblyOS: // II.22.4
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = mdtc_constant | mdtc_b4;
        table->column_count = 3;
        break;
    case mdtid_AssemblyRef: // II.22.5
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = mdtc_constant | mdtc_b2;
        table->column_details[2] = mdtc_constant | mdtc_b2;
        table->column_details[3] = mdtc_constant | mdtc_b2;
        table->column_details[4] = mdtc_constant | mdtc_b4;
        table->column_details[5] = blob_index;
        table->column_details[6] = string_index;
        table->column_details[7] = string_index;
        table->column_details[8] = blob_index;
        table->column_count = 9;
        break;
    case mdtid_AssemblyRefProcessor: // II.22.7
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_AssemblyRef));
        table->column_count = 2;
        break;
    case mdtid_AssemblyRefOS: // II.22.6
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = mdtc_constant | mdtc_b4;
        table->column_details[3] = compute_table_index(TABLE_INDEX_ARGS(mdtid_AssemblyRef));
        table->column_count = 4;
        break;
    case mdtid_File: // II.22.19
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = string_index;
        table->column_details[2] = blob_index;
        table->column_count = 3;
        break;
    case mdtid_ExportedType: // II.22.14
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = string_index;
        table->column_details[3] = string_index;
        table->column_details[4] = compute_coded_index(CODED_INDEX_ARGS(mdci_Implementation));
        table->column_count = 5;
        break;
    case mdtid_ManifestResource: // II.22.24
        table->column_details[0] = mdtc_constant | mdtc_b4;
        table->column_details[1] = mdtc_constant | mdtc_b4;
        table->column_details[2] = string_index;
        table->column_details[3] = compute_coded_index(CODED_INDEX_ARGS(mdci_Implementation));
        table->column_count = 4;
        break;
    case mdtid_NestedClass: // II.22.32
        table->column_details[0] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_details[1] = compute_table_index(TABLE_INDEX_ARGS(mdtid_TypeDef));
        table->column_count = 2;
        break;
    case mdtid_GenericParam: // II.22.20
        table->column_details[0] = mdtc_constant | mdtc_b2;
        table->column_details[1] = mdtc_constant | mdtc_b2;
        table->column_details[2] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeOrMethodDef));
        table->column_details[3] = string_index;
        table->column_count = 4;
        break;
    case mdtid_MethodSpec: // II.22.29
        table->column_details[0] = compute_coded_index(CODED_INDEX_ARGS(mdci_MethodDefOrRef));
        table->column_details[1] = blob_index;
        table->column_count = 2;
        break;
    case mdtid_GenericParamConstraint: // II.22.21
        table->column_details[0] = compute_table_index(TABLE_INDEX_ARGS(mdtid_GenericParam));
        table->column_details[1] = compute_coded_index(CODED_INDEX_ARGS(mdci_TypeDefOrRef));
        table->column_count = 2;
        break;
    default:
        assert(!"Unknown metadata table ID");
        return false;
    }
#undef TABLE_INDEX_ARGS
#undef CODED_INDEX_ARGS

    assert(table->column_count != 0);
    table->row_size_bytes = compute_row_offsets_size(table->column_details, table->column_count);
    return true;
}

bool consume_table_rows(mdtable_t* table, uint8_t const** data, size_t* data_len)
{
    assert(table != NULL && data != NULL && data_len != NULL);
    assert(table->row_size_bytes != 0 && "Table with row byte length of 0 is unexpected");
    if (table->row_count == 0)
        return true;

    uint8_t const* rows = *data;
    size_t rows_len = table->row_size_bytes * table->row_count;
    if (!advance_stream(data, data_len, rows_len))
        return false;

    table->data.ptr = rows;
    table->data.size = rows_len;
    return true;
}
