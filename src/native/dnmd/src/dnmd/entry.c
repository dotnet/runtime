#include "internal.h"

#include <stdio.h>
#include <inttypes.h>

// mdlib magic number for context
#define MDLIB_MAGIC_NUMBER 0x3d71b

// Defined in II.24.2.1
#define METADATA_SIG 0x424A5342

static mdcxt_t* allocate_full_context(mdcxt_t* cxt)
{
    // The intent here is to call the allocator once.
    // Therefore we compute the full size and then call
    // malloc a single time. The following needs to be
    // done:
    //  1. Compute total amount of needed memory:
    //     - sizeof(mdcxt_t)
    //     - table count
    //     - column count for each table
    //  2. Copy supplied mdcxt_t to the newly allocated one
    //  3. Determine table array offset
    //  4. Set table pointer in mdcxt_t
    //  5. Determine column details array offsets
    //  6. Set column details array in each table
    //  7. Return the newly allocated context

    uint32_t table_col_sizes[MDTABLE_MAX_COUNT];
    uint32_t total_col_size = 0;
    for (mdtable_id_t id = mdtid_First; id < mdtid_End; ++id)
    {
        table_col_sizes[id] = sizeof(mdtcol_t) * get_table_column_count(id);
        total_col_size += table_col_sizes[id];
    }

    // Ensure all sections of the allocation are pointer aligned.
    size_t cxt_mem = align_to(sizeof(mdcxt_t), sizeof(void*));
    size_t tables_mem = MDTABLE_MAX_COUNT * align_to(sizeof(mdtable_t), sizeof(void*));
    size_t col_mem = align_to(total_col_size, sizeof(void*));

    size_t total_mem = cxt_mem + tables_mem + col_mem;
    uint8_t* mem = (uint8_t*)malloc(total_mem);
    if (mem == NULL)
        return NULL;

    // Copy passed in state
    mdcxt_t* pcxt = (mdcxt_t*)mem;
    mem += cxt_mem;
    memcpy(pcxt, cxt, sizeof(*cxt));
    assert(pcxt->tables == NULL);
    assert(pcxt->mem == NULL);

    // Zero out the remaining memory
    memset(mem, 0, total_mem - cxt_mem);

    // Update the tables pointer to offset in allocation
    pcxt->tables = (mdtable_t*)mem;
    mem += tables_mem;

    // Update each table's column array
    for (mdtable_id_t id = mdtid_First; id < mdtid_End; ++id)
    {
        pcxt->tables[id].column_details = (mdtcol_t*)mem;
        uint32_t size = table_col_sizes[id];
        mem += size;
    }

    assert(mem <= (uint8_t*)(pcxt + total_mem));
    return pcxt;
}

bool md_create_handle(void const* data, size_t data_len, mdhandle_t* handle)
{
    if (data == NULL || handle == NULL)
        return false;

    uint8_t const* const base = data;
    uint8_t const* curr = data;
    size_t curr_len = data_len;

    // Validate the metadata root is the minimally valid before creating a handle.
    uint32_t sig;
    uint32_t ver_buf_count;
    uint16_t stream_count;

    mdcxt_t cxt;
    memset(&cxt, 0, sizeof(cxt));

    // Consume header defined in II.24.2.1
    if (!read_u32(&curr, &curr_len, &sig) || sig != METADATA_SIG)
        return false;

    if (!read_u16(&curr, &curr_len, &cxt.major_ver)
        || !read_u16(&curr, &curr_len, &cxt.minor_ver)
        || !advance_stream(&curr, &curr_len, 4)
        || !read_u32(&curr, &curr_len, &ver_buf_count))
    {
        return false;
    }

    // The version count is aligned to 4-bytes
    ver_buf_count = align_to(ver_buf_count, 4);
    if (ver_buf_count > curr_len)
        return false;

    // Confirm terminator and consume the version/aligned length
    cxt.version = (char const*)curr;
    if (ver_buf_count == 0
        || cxt.version[ver_buf_count - 1] != '\0'
        || !advance_stream(&curr, &curr_len, ver_buf_count))
    {
        return false;
    }

    if (!read_u16(&curr, &curr_len, &cxt.flags)
        || !read_u16(&curr, &curr_len, &stream_count))
    {
        return false;
    }

    // Iterate over the discovered streams
    uint32_t offset;
    uint32_t stream_size;
    uint8_t* name_end;
    size_t name_len;
    bool tables_heap_uncompressed = false;
    for (size_t i = 0; i < stream_count; ++i)
    {
        if (!read_u32(&curr, &curr_len, &offset)
            || !read_u32(&curr, &curr_len, &stream_size))
        {
            return false;
        }

        // Verify the offset is valid for our data size
        if (offset > data_len)
            return false;

        // Verify the stream size can fit into available size
        if (stream_size > data_len - offset)
            return false;

        // Find the terminating null.
        name_end = memchr(curr, 0, curr_len);
        if (name_end == NULL)
            return false;

        name_len = name_end - curr;
        if (strncmp((char const*)curr, "#~", name_len) == 0)
        {
            cxt.tables_heap.ptr = base + offset;
            cxt.tables_heap.size = stream_size;
            tables_heap_uncompressed = false;
        }
        // The #- stream is used for images that may have the *Ptr indirection tables.
        // The indirection tables, as well as the #- stream, are not documented in the ECMA spec.
        else if (strncmp((char const*)curr, "#-", name_len) == 0)
        {
            cxt.tables_heap.ptr = base + offset;
            cxt.tables_heap.size = stream_size;
            tables_heap_uncompressed = true;
        }
        // The #JTD stream is a marker that the image is a minimal EnC delta, as compared to an image
        // with the EnC data included. This stream is not documented in the ECMA spec.
        else if (strncmp((char const*)curr, "#JTD", name_len) == 0)
        {
            // The content of the stream is ignored.
            cxt.context_flags |= mdc_minimal_delta;
        }
        else if (strncmp((char const*)curr, "#Strings", name_len) == 0)
        {
            cxt.strings_heap.ptr = base + offset;
            cxt.strings_heap.size = stream_size;

            // Compute the precise size of the string heap by walking back over the trailing null padding.
            // There may be up to three extra '\0' characters appended for padding.
            // ENC minimal delta images require the precise size of the base image string heap to be known,
            // so we trim the trailing padding.
            uint8_t const* p = cxt.strings_heap.ptr + cxt.strings_heap.size - 1;
            while (cxt.strings_heap.size >= 2 && p[0] == 0 && p[-1] == 0)
            {
                p--;
                cxt.strings_heap.size--;
            }
        }
        else if (strncmp((char const*)curr, "#Blob", name_len) == 0)
        {
            cxt.blob_heap.ptr = base + offset;
            cxt.blob_heap.size = stream_size;
        }
        else if (strncmp((char const*)curr, "#US", name_len) == 0)
        {
            cxt.user_string_heap.ptr = base + offset;
            cxt.user_string_heap.size = stream_size;
        }
        else if (strncmp((char const*)curr, "#GUID", name_len) == 0)
        {
            cxt.guid_heap.ptr = base + offset;
            cxt.guid_heap.size = stream_size;
        }
#ifdef DNMD_PORTABLE_PDB
        else if (strncmp((char const*)curr, "#Pdb", name_len) == 0)
        {
            cxt.pdb.ptr = base + offset;
            cxt.pdb.size = stream_size;
        }
#endif // DNMD_PORTABLE_PDB
        else
        {
            assert(!"Unknown stream");
            return false;
        }

        // Align the string length to 4 bytes.
        if (!advance_stream(&curr, &curr_len, align_to((uint32_t)(name_len + 1), 4)))
            return false;
    }

    // When the #JTD stream is present, the #- stream must be
    // the stream that contains the metadata tables.
    if ((bool)(cxt.context_flags & mdc_minimal_delta) && !tables_heap_uncompressed)
        return false;

    // Header initialization is complete.
    cxt.magic = MDLIB_MAGIC_NUMBER;
    cxt.raw_metadata.ptr = data;
    cxt.raw_metadata.size = data_len;

    // Allocate and initialize a context
    mdcxt_t* pcxt = allocate_full_context(&cxt);
    if (pcxt == NULL)
        return false;

#ifndef NDEBUG
    memset(&cxt, 0xcc, sizeof(cxt));
#endif // NDEBUG

    // Initialize the tables in the new context.
    if (!initialize_tables(pcxt))
    {
        free(pcxt);
        return false;
    }

    // Move the constructed context to the allocated one.
    *handle = pcxt;
    return true;
}

// Initialize the minimal set of tables required for a valid metadata image.
// Every image must have a row in the Module table
// for module identity information
// and a row in the TypeDef table for the global type.
static bool initialize_minimal_table_rows(mdcxt_t* cxt)
{
    // Add the Module row for module identity
    mdcursor_t module_cursor;
    if (!md_append_row(cxt, mdtid_Module, &module_cursor))
        return false;
    
    // Set the Generation to 0
    uint32_t generation = 0;
    if (1 != md_set_column_value_as_constant(module_cursor, mdtModule_Generation, 1, &generation))
        return false;
    
    // Use the 0 index to specify the NULL guid as the guids for the image.
    uint32_t guid_heap_offset = 0;
    if (1 != set_column_value_as_heap_offset(module_cursor, mdtModule_Mvid, 1, &guid_heap_offset)
        || 1 != set_column_value_as_heap_offset(module_cursor, mdtModule_EncBaseId, 1, &guid_heap_offset)
        || 1 != set_column_value_as_heap_offset(module_cursor, mdtModule_EncId, 1, &guid_heap_offset))
    {
        return false;
    }

    char const* name = "";
    if (1 != md_set_column_value_as_utf8(module_cursor, mdtModule_Name, 1, &name))
        return false;
    
    // Mark that we're done adding the Module row.
    md_commit_row_add(module_cursor);

    // Add a row for the global <Module> type.
    mdcursor_t global_type_cursor;
    if (!md_append_row(cxt, mdtid_TypeDef, &global_type_cursor))
        return false;

    uint32_t flags = 0;
    if (1 != md_set_column_value_as_constant(global_type_cursor, mdtTypeDef_Flags, 1, &flags))
        return false;
    
    char const* global_type_name = "<Module>"; // Defined in ECMA-335 II.10.8
    if (1 != md_set_column_value_as_utf8(global_type_cursor, mdtTypeDef_TypeName, 1, &global_type_name))
        return false;
    
    char const* namespace = "";
    if (1 != md_set_column_value_as_utf8(global_type_cursor, mdtTypeDef_TypeNamespace, 1, &namespace))
        return false;
    
    mdToken nil_typedef = CreateTokenType(mdtid_TypeDef);
    if (1 != md_set_column_value_as_token(global_type_cursor, mdtTypeDef_Extends, 1, &nil_typedef))
        return false;

    // Mark that we're done adding the TypeDef row.
    md_commit_row_add(global_type_cursor);

    return true;
}

mdhandle_t md_create_new_handle()
{
    mdcxt_t cxt;

    memset(&cxt, 0, sizeof(mdcxt_t));
    cxt.magic = MDLIB_MAGIC_NUMBER;
    cxt.context_flags = mdc_none;
    cxt.major_ver = 1;
    cxt.minor_ver = 1;
    cxt.flags = 0;
    cxt.version = "v4.0.30319";
    cxt.editor = NULL;
    cxt.mem = NULL;

    // Allocate and initialize a full context
    // with the correctly-sized trailing memory.
    mdcxt_t* pcxt = allocate_full_context(&cxt);
    if (pcxt == NULL)
        return NULL;
    
    if (!initialize_minimal_table_rows(pcxt))
    {
        free(pcxt);
        return NULL;
    }

    return pcxt;
}

#ifdef DNMD_PORTABLE_PDB
mdhandle_t md_create_new_pdb_handle()
{
    mdcxt_t cxt;

    memset(&cxt, 0, sizeof(mdcxt_t));
    cxt.magic = MDLIB_MAGIC_NUMBER;
    cxt.context_flags = mdc_none;
    cxt.major_ver = 1;
    cxt.minor_ver = 1;
    cxt.flags = 0;
    cxt.version = "PDB v1.0";
    cxt.editor = NULL;
    cxt.mem = NULL;

    // Allocate and initialize a full context
    // with the correctly-sized trailing memory.
    mdcxt_t* pcxt = allocate_full_context(&cxt);
    if (pcxt == NULL)
        return NULL;

    return pcxt;
}
#endif // DNMD_PORTABLE_PDB

bool md_apply_delta(mdhandle_t handle, mdhandle_t delta_handle)
{
    mdcxt_t* base = extract_mdcxt(handle);
    if (base == NULL)
        return false;

    mdcxt_t* delta = extract_mdcxt(delta_handle);
    if (delta == NULL)
        return false;

    // Verify the supplied delta is actually a delta file
    bool result = false;
    if (delta->context_flags & mdc_minimal_delta)
        result = merge_in_delta(base, delta);

    return result;
}

typedef struct mdmem__
{
    struct mdmem__* next;
    size_t size;
    uint8_t data[];
} mdmem_t;

void md_destroy_handle(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return;

    mdmem_t* tmp;
    mdmem_t* curr = cxt->mem;
    while(curr != NULL)
    {
        tmp = curr->next;
        free(curr);
        curr = tmp;
    }

    free(cxt);
}

bool md_validate(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    return validate_guid_heap(cxt)
        && validate_strings_heap(cxt)
        && validate_user_string_heap(cxt)
        && validate_blob_heap(cxt)
        && validate_tables(cxt);
}

static bool dump_table_rows(mdtable_t* table)
{
    if (table->row_count == 0)
    {
        printf("Empty table\n");
    }
    else
    {
        printf("Table %u (0x%x) rows: %u\n", table->table_id, table->table_id, table->row_count);
    }

    char const* str;
    mdguid_t guid;
    uint8_t const* blob;
    uint32_t blob_len;
    uint32_t constant;
    mduserstring_t user_string;
    mdToken tk;

#ifdef DEBUG_TABLE_COLUMN_LOOKUP
    uint16_t const embedded_tid = ((uint16_t)table->table_id) << 8;
#define IDX(x) (embedded_tid | x)
#else
#define IDX(x) x
#endif

    // Create a cursor to the first row.
    mdcursor_t cursor = create_cursor(table, 1);

    // The maximum known column count is 9, so hard coding the array to that.
    bool to_get[] = { true, true, true, true, true, true, true, true, true };
    assert(table->column_count <= ARRAY_SIZE(to_get));
    uint32_t raw_values[ARRAY_SIZE(to_get)];

#define IF_NOT_ONE_REPORT_RAW(exp) if (1 != (exp)) { printf("Invalid (%u) [%#x]|", j, raw_values[j]); continue; }
#define IF_INVALID_BLOB_REPORT_RAW(parse_fn, handle_or_cursor, blob_type, result_buf, result_buf_len) \
    { \
    result_buf = NULL; \
    md_blob_parse_result_t result = parse_fn(handle_or_cursor, blob, blob_len, result_buf, &result_buf_len); \
    if (result == mdbpr_InvalidBlob) { printf("Invalid PDB Blob (" blob_type ") Offset: %zu (len: %u) [%#x]|", (blob - table->cxt->blob_heap.ptr), blob_len, raw_values[j]); continue; } \
    assert(result == mdbpr_InsufficientBuffer); \
    result_buf = malloc(result_buf_len); \
    if (result_buf == NULL) { printf("Ran out of memory when parsing PDB blob.\n"); return false; } \
    result = parse_fn(handle_or_cursor, blob, blob_len, result_buf, &result_buf_len); \
    if (result == mdbpr_InvalidBlob) { printf("Invalid PDB Blob (" blob_type ") Offset: %zu (len: %u) [%#x]|", (blob - table->cxt->blob_heap.ptr), blob_len, raw_values[j]); free(result_buf); continue; } \
    assert(result == mdbpr_Success); \
    }

    for (uint32_t i = 0; i < table->row_count; ++i)
    {
        if (!md_get_column_values_raw(cursor, table->column_count, to_get, raw_values))
        {
            printf("Failure to retrieve raw column values. Table is corrupted.\n");
            return false;
        }

        printf("%4u|", i);
        for (uint8_t j = 0; j < table->column_count; ++j)
        {
            if (table->column_details[j] & mdtc_hstring)
            {
                IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_utf8(cursor, IDX(j), 1, &str));
                printf("'%s' [%#x]|", str, raw_values[j]);
            }
            else if (table->column_details[j] & mdtc_hguid)
            {
                IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_guid(cursor, IDX(j), 1, &guid));
                printf("{%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x} [%#x]|",
                    guid.data1, guid.data2, guid.data3,
                    guid.data4[0], guid.data4[1],
                    guid.data4[2], guid.data4[3],
                    guid.data4[4], guid.data4[5],
                    guid.data4[6], guid.data4[7], raw_values[j]);
            }
            else if (table->column_details[j] & mdtc_hblob)
            {
                col_index_t col = IDX(j);
#ifdef DNMD_PORTABLE_PDB
                if (table->table_id == mdtid_Document && col == mdtDocument_Name)
                {
                    IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_blob(cursor, col, 1, &blob, &blob_len));
                    
                    char* document_name;
                    size_t name_len;
                    IF_INVALID_BLOB_REPORT_RAW(md_parse_document_name, table->cxt, "DocumentName", document_name, name_len);
                    printf("DocumentName: '%s' [%#x]|", document_name, raw_values[j]);
                    free(document_name);
                    continue;
                }
                else if (table->table_id == mdtid_MethodDebugInformation && col == mdtMethodDebugInformation_SequencePoints)
                {
                    IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_blob(cursor, col, 1, &blob, &blob_len));
                    
                    if (blob_len == 0)
                    {
                        printf("Empty SequencePoints: Offset: %zu (len: %u) [%#x]|", (blob - table->cxt->blob_heap.ptr), blob_len, raw_values[j]);
                        continue;
                    }

                    md_sequence_points_t* sequence_points;
                    size_t sequence_points_len;
                    IF_INVALID_BLOB_REPORT_RAW(md_parse_sequence_points, cursor, "SequencePoints", sequence_points, sequence_points_len);
                    printf("SequencePoints: LocalSignature 0x%08x (mdToken) ", sequence_points->signature);
                    mdToken document_tok;
                    md_cursor_to_token(sequence_points->document, &document_tok);
                    printf("Document 0x%08x (mdToken) ", document_tok);
                    printf("{ ");
                    bool first = true;
                    for (uint32_t k = 0; k < sequence_points->record_count; ++k)
                    {
                        if (!first)
                        {
                            printf(", ");
                        }
                        first = false;
                        if (sequence_points->records[k].kind == mdsp_DocumentRecord)
                        {
                            printf("document-record: ");
                            md_cursor_to_token(sequence_points->records[k].document.document, &document_tok);
                            printf("0x%08x (mdToken)", document_tok);
                        }
                        else if (sequence_points->records[k].kind == mdsp_HiddenSequencePointRecord)
                        {
                            printf("hidden-sequence-point-record: %u", sequence_points->records[k].hidden_sequence_point.rolling_il_offset);
                        }
                        else if (sequence_points->records[k].kind == mdsp_SequencePointRecord)
                        {
                            printf("sequence-point-record: (%u, %u, %" PRId64 ", %" PRId64 ", %" PRId64 ")",
                                sequence_points->records[k].sequence_point.rolling_il_offset,
                                sequence_points->records[k].sequence_point.delta_lines,
                                sequence_points->records[k].sequence_point.delta_columns,
                                sequence_points->records[k].sequence_point.rolling_start_line,
                                sequence_points->records[k].sequence_point.rolling_start_column);
                        }
                        else
                        {
                            assert(!"Invalid sequence point record kind.");
                        }
                    }
                    
                    printf(" } [%#x]|", raw_values[j]);

                    free(sequence_points);
                    continue;
                }
                else if (table->table_id == mdtid_LocalConstant && col == mdtLocalConstant_Signature)
                {
                    IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_blob(cursor, col, 1, &blob, &blob_len));
                    md_local_constant_sig_t* local_constant_sig;
                    size_t local_constant_sig_len;
                    IF_INVALID_BLOB_REPORT_RAW(md_parse_local_constant_sig, table->cxt, "LocalConstantSig", local_constant_sig, local_constant_sig_len);
                    printf("LocalConstantSig: ");
                    for (uint32_t k = 0; k < local_constant_sig->custom_modifier_count; ++k)
                    {
                        printf("%s(0x%08x) ", local_constant_sig->custom_modifiers[k].required ? "modreq" : "modopt", local_constant_sig->custom_modifiers[k].type);
                    }

                    if (local_constant_sig->constant_kind == mdck_PrimitiveConstant)
                    {
                        printf("Primitive: 0x%02x ", local_constant_sig->primitive.type_code);
                    }
                    else if (local_constant_sig->constant_kind == mdck_EnumConstant)
                    {
                        printf("Enum: 0x%02x{0x%08x (mdToken)} ", local_constant_sig->enum_constant.type_code, local_constant_sig->enum_constant.enum_type);
                    }
                    else if (local_constant_sig->constant_kind == mdck_GeneralConstant)
                    {
                        printf("General: 0x%02x{0x%08x (mdToken)} ", local_constant_sig->general.kind, local_constant_sig->general.type);
                    }
                    else
                    {
                        assert(!"Invalid constant kind.");
                    }
                    printf("Value Offset: %zu (len: %zu) [%#x]|", local_constant_sig->value_blob - table->cxt->blob_heap.ptr, local_constant_sig->value_len, raw_values[j]);
                    
                    free(local_constant_sig);
                    continue;
                }
                else if (table->table_id == mdtid_ImportScope && col == mdtImportScope_Imports)
                {
                    IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_blob(cursor, col, 1, &blob, &blob_len));
                    
                    if (blob_len == 0)
                    {
                        printf("Empty Imports: Offset: %zu (len: %u) [%#x]|", (blob - table->cxt->blob_heap.ptr), blob_len, raw_values[j]);
                        continue;
                    }

                    md_imports_t* imports;
                    size_t imports_len;
                    IF_INVALID_BLOB_REPORT_RAW(md_parse_imports, table->cxt, "Imports", imports, imports_len);
                    printf("{ ");
                    bool first = true;
                    for (uint32_t k = 0; k < imports->count; ++k)
                    {
                        if (!first)
                        {
                            printf(", ");
                        }
                        first = false;
                        switch (imports->imports[k].kind)
                        {
                        case mdidk_ImportNamespace:
                            printf("ns('%.*s')", imports->imports[k].target_namespace_len, imports->imports[k].target_namespace);
                            break;
                        case mdidk_ImportAssemblyNamespace:
                            printf("ns('%.*s' in 0x%08x (mdToken))", imports->imports[k].target_namespace_len, imports->imports[k].target_namespace, imports->imports[k].assembly);
                            break;
                        case mdidk_ImportType:
                            printf("type(0x%08x (mdToken))", imports->imports[k].target_type);
                            break;
                        case mdidk_ImportXmlNamespace:
                            printf("xml-alias('%.*s' for '%.*s')", imports->imports[k].alias_len, imports->imports[k].alias, imports->imports[k].target_namespace_len, imports->imports[k].target_namespace);
                            break;
                        case mdidk_ImportAssemblyReferenceAlias:
                            printf("import-alias('%.*s')", imports->imports[k].alias_len, imports->imports[k].alias);
                            break;
                        case mdidk_AliasAssemblyReference:
                            printf("alias('%.*s' for 0x%08x (mdToken))", imports->imports[k].alias_len, imports->imports[k].alias, imports->imports[k].assembly);
                            break;
                        case mdidk_AliasNamespace:
                            printf("alias('%.*s' for '%.*s')", imports->imports[k].alias_len, imports->imports[k].alias, imports->imports[k].target_namespace_len, imports->imports[k].target_namespace);
                            break;
                        case mdidk_AliasAssemblyNamespace:
                            printf("alias('%.*s' for '%.*s' in 0x%08x (mdToken))", imports->imports[k].alias_len, imports->imports[k].alias, imports->imports[k].target_namespace_len, imports->imports[k].target_namespace, imports->imports[k].assembly);
                            break;
                        case mdidk_AliasType:
                            printf("alias('%.*s' for  0x%08x (mdToken))", imports->imports[k].alias_len, imports->imports[k].alias, imports->imports[k].target_type);
                            break;
                        default:
                            assert(!"Invalid import kind.");
                            break;
                        }
                    }
                    
                    printf(" } [%#x]|", raw_values[j]);

                    free(imports);
                    continue;
                }
#endif
                IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_blob(cursor, col, 1, &blob, &blob_len));
                printf("Offset: %zu (len: %u) [%#x]|", (blob - table->cxt->blob_heap.ptr), blob_len, raw_values[j]);
            }
            else if (table->column_details[j] & mdtc_hus)
            {
                IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_userstring(cursor, IDX(j), 1, &user_string));
                printf("UTF-16 string (%u bytes) [%#x]|", user_string.str_bytes, raw_values[j]);
            }
            else if (table->column_details[j] & (mdtc_idx_table | mdtc_idx_coded))
            {
                IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_token(cursor, IDX(j), 1, &tk));
                printf("0x%08x (mdToken) [%#x]|", tk, raw_values[j]);
            }
            else
            {
                assert(table->column_details[j] & mdtc_constant);
                IF_NOT_ONE_REPORT_RAW(md_get_column_value_as_constant(cursor, IDX(j), 1, &constant));
                printf("0x%08x [%#x]|", constant, raw_values[j]);
            }
        }
        printf("\n");
        if (!md_cursor_next(&cursor) && i != (table->row_count - 1))
            return false;
    }
    printf("\n");
#undef IF_NOT_ONE_REPORT_RAW
#undef IF_INVALID_BLOB_REPORT_RAW

    return true;
}

bool md_dump_tables(mdhandle_t handle, int32_t table_id)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    for (int32_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        // Check if the user supplied a table to check
        if (table_id > -1)
        {
            if (i < table_id) // Less than, skip.
                continue;
            if (i > table_id) // Greater than, done.
                break;
            assert(i == table_id);
        }

        if (!dump_table_rows(&cxt->tables[i]))
        {
            printf("Failure in table '%u'\n", i);
            return false;
        }
    }

    return true;
}

char const* md_get_version_string(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return NULL;
    return cxt->version;
}

mdcxt_t* extract_mdcxt(mdhandle_t md)
{
    mdcxt_t* cxt = (mdcxt_t*)md;
    if (!cxt || cxt->magic != MDLIB_MAGIC_NUMBER)
        return NULL;
    return cxt;
}

void* alloc_mdmem(mdcxt_t* cxt, size_t length)
{
    assert(cxt != NULL);
    mdmem_t* m = (mdmem_t*)malloc(sizeof(mdmem_t) + length);
    if (m != NULL)
    {
        m->next = cxt->mem;
        m->size = length;
        cxt->mem = m;
        return m->data;
    }
    return NULL;
}

void free_mdmem(mdcxt_t* cxt, void* mem)
{
    assert(cxt != NULL);
    if (mem == NULL)
        return;

    // We need to get back to the mdmem_t header from the start of the block.
    mdmem_t* m = (mdmem_t*)((char*)mem - offsetof(mdmem_t, data));

    // Remove m from the chain of tracked memory.
    if (cxt->mem == m)
    {
        cxt->mem = m->next;
    }
    else
    {
        for (mdmem_t* p = cxt->mem; p != NULL; p = p->next)
        {
            if (p->next == m)
            {
                p->next = m->next;
                break;
            }
        }
    }

    // Now that we aren't tracking the memory, free it.
    free(m);
}

static size_t get_stream_header_and_contents_size(char const* heap_name, size_t heap_size)
{
    assert(heap_name != NULL);
    // II.24.2.2 Stream header
    size_t const base_stream_header_size =
        sizeof(uint32_t) // Offset
        + sizeof(uint32_t) // Size
        // Name is variable length and calculated below
    ;

    // Add the size of the stream header
    // II.24.2.2 Stream name is padded to a 4-byte boundary
    size_t save_size = base_stream_header_size;
    save_size += align_to((uint32_t)strlen(heap_name) + 1, 4);
    // Add the size of the stream itself.
    // It's not placed directly after the header in the image,
    // but we might as well account for it here while we're checking
    // the heap's existence.
    save_size += heap_size;

    return save_size;
}

static size_t get_table_stream_size(mdcxt_t* cxt)
{
    // II.24.2.6 #~ stream
    size_t const table_stream_header_size =        
        + sizeof(uint32_t) // Reserved
        + sizeof(uint8_t) // MajorVersion
        + sizeof(uint8_t) // MinorVersion
        + sizeof(uint8_t) // HeapSizes
        + sizeof(uint8_t) // Reserved
        + sizeof(uint64_t) // Valid tables
        + sizeof(uint64_t) // Sorted tables
        // Rows and Tables entries are both variable length and calculated below
    ;
    
    size_t save_size = table_stream_header_size;
    
    for (uint8_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        if (cxt->tables[i].cxt != NULL && cxt->tables[i].row_count != 0)
        {
            save_size += sizeof(uint32_t); // Row count
            save_size += cxt->tables[i].data.size; // Table data
        }
    }
    
    return save_size;
}

static size_t get_image_size(mdcxt_t* cxt)
{    
    if (cxt->editor == NULL)
        return cxt->raw_metadata.size;
    
    // II.24.2.1 Metadata Root size
    size_t const image_header_size =
        sizeof(uint32_t) // Signature
        + sizeof(uint16_t) // MajorVersion
        + sizeof(uint16_t) // MinorVersion
        + sizeof(uint32_t) // Reserved
        + sizeof(uint32_t) // Length (of version string)
        + align_to((uint32_t)strlen(cxt->version) + 1, 4) // Version String
        + sizeof(uint16_t) // Flags
        + sizeof(uint16_t) // Streams (number of streams)
    ;
    
    size_t save_size = image_header_size;

    if (cxt->blob_heap.size != 0)
        save_size += get_stream_header_and_contents_size("#Blob", cxt->blob_heap.size);
    if (cxt->guid_heap.size != 0)
        save_size += get_stream_header_and_contents_size("#GUID", cxt->guid_heap.size);
    if (cxt->strings_heap.size != 0)
        save_size += get_stream_header_and_contents_size("#Strings", align_to((uint32_t)cxt->strings_heap.size, 4));
    if (cxt->user_string_heap.size != 0)
        save_size += get_stream_header_and_contents_size("#US", cxt->user_string_heap.size);

    if (cxt->context_flags & mdc_minimal_delta)
        save_size += get_stream_header_and_contents_size("#JTD", 0);
    
    // All names of the tables stream are the same length,
    // so pick the one in the standard.
    save_size += get_stream_header_and_contents_size("#~", get_table_stream_size(cxt));

    return save_size;
}

// II.24.2.2 Stream header
static bool write_stream_header(char const* name, size_t size, mddata_t* offset_space, uint8_t** buffer, size_t* buffer_len)
{
    assert(offset_space != NULL);
    size_t name_len = strlen(name);
    size_t name_buf_len = align_to((uint32_t)name_len + 1, 4);

    offset_space->ptr = *buffer;
    offset_space->size = 4;

    if (!advance_output_stream(buffer, buffer_len, 4) // Offset
        || !write_u32(buffer, buffer_len, (uint32_t)size)) // Size
    {
        return false;
    }

    if (*buffer_len < name_buf_len)
        return false;

    // Name
    memcpy(*buffer, name, name_len + 1);
    advance_output_stream(buffer, buffer_len, name_len + 1);
    // Pad the name to a 4-byte boundary.
    advance_output_stream(buffer, buffer_len, name_buf_len - name_len - 1);

    return true;
}

bool md_write_to_buffer(mdhandle_t handle, uint8_t* buffer, size_t* len)
{
    if (len == NULL)
        return false;

    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    size_t image_size = get_image_size(cxt);
    size_t const full_buffer_len = *len;

    // Handle the case where no edits have occurred.
    // This operation is basically a "copy to new buffer".
    if (cxt->editor == NULL)
    {
        if (buffer == NULL || full_buffer_len < cxt->raw_metadata.size)
        {
            *len = cxt->raw_metadata.size;
            return false;
        }
        memcpy(buffer, cxt->raw_metadata.ptr, cxt->raw_metadata.size);
        return true;
    }
    
    if (buffer == NULL || full_buffer_len < image_size)
    {
        *len = image_size;
        return false;
    }

    uint8_t* const buffer_start = buffer;
    size_t remaining_buffer_len = full_buffer_len;
    if (!write_u32(&buffer, &remaining_buffer_len, METADATA_SIG)
        || !write_u16(&buffer, &remaining_buffer_len, cxt->major_ver)
        || !write_u16(&buffer, &remaining_buffer_len, cxt->minor_ver)
        || !write_u32(&buffer, &remaining_buffer_len, 0))
    {
        return false;
    }
    
    size_t version_str_len = strlen(cxt->version);
    uint32_t version_buf_len = align_to((uint32_t)version_str_len + 1, 4);

    if (!write_u32(&buffer, &remaining_buffer_len, (uint32_t)version_buf_len))
        return false;
    
    if (remaining_buffer_len < version_buf_len)
        return false;
    
    memcpy(buffer, cxt->version, version_str_len + 1);
    // Pad the version string to a 4-byte boundary.
    memset(buffer + version_str_len + 1, 0, version_buf_len - version_str_len - 1);
    advance_output_stream(&buffer, &remaining_buffer_len, version_buf_len);

    if (!write_u16(&buffer, &remaining_buffer_len, cxt->flags))
        return false;
    
    uint16_t stream_count = 0;
    if (cxt->blob_heap.size != 0)
        stream_count++;
    if (cxt->guid_heap.size != 0)
        stream_count++;
    if (cxt->strings_heap.size != 0)
        stream_count++;
    if (cxt->user_string_heap.size != 0)
        stream_count++;

    char const* tables_stream_name = "#~";

    if (cxt->context_flags & mdc_minimal_delta)
    {
        tables_stream_name = "#-";
        stream_count++;
    }

    uint64_t valid_tables = 0;
    uint64_t sorted_tables = 0;
    for (uint8_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        if (cxt->tables[i].cxt != NULL && cxt->tables[i].row_count != 0)
        {
            // We don't support saving if we are in the process of adding a new row.
            if (cxt->tables[i].is_adding_new_row)
                return false;

            valid_tables |= (1ULL << i);
            if (cxt->tables[i].is_sorted)
                sorted_tables |= (1ULL << i);
            
            // Indirect tables only exist in images that use the uncompresed stream.
            if (table_is_indirect_table((mdtable_id_t)i))
                tables_stream_name = "#-";
        }
    }

    // The tables stream is always included.
    stream_count++;
    
    if (!write_u16(&buffer, &remaining_buffer_len, stream_count))
        return false;

    mddata_t blob_heap_offset_space = { 0 };
    mddata_t strings_heap_offset_space = { 0 };
    mddata_t guid_heap_offset_space = { 0 };
    mddata_t user_string_heap_offset_space = { 0 };
    mddata_t tables_heap_offset_space = { 0 };
#ifdef DNMD_PORTABLE_PDB
    mddata_t pdb_offset_space = { 0 };
#endif

    // Write the stream headers.
    if (cxt->context_flags & mdc_minimal_delta)
    {
        mddata_t offset_space;
        if (!write_stream_header("#JTD", 0, &offset_space, &buffer, &remaining_buffer_len))
            return false;
        
        // Set the stream offset to the location of the stream header.
        // There's no content in this stream, but the offset must be valid.
        write_u32(&offset_space.ptr, &offset_space.size, (uint32_t)((uint8_t*)offset_space.ptr - buffer_start));
    }

    if (cxt->strings_heap.size != 0)
    {
        // The strings heap should be aligned to 4 bytes.
        if (!write_stream_header("#Strings", align_to((uint32_t)cxt->strings_heap.size, 4), &strings_heap_offset_space, &buffer, &remaining_buffer_len))
            return false;
    }

    if (cxt->blob_heap.size != 0)
    {
        if (!write_stream_header("#Blob", cxt->blob_heap.size, &blob_heap_offset_space, &buffer, &remaining_buffer_len))
            return false;
    }

    if (cxt->guid_heap.size != 0)
    {
        if (!write_stream_header("#GUID", cxt->guid_heap.size, &guid_heap_offset_space, &buffer, &remaining_buffer_len))
            return false;
    }

    if (cxt->user_string_heap.size != 0)
    {
        if (!write_stream_header("#US", cxt->user_string_heap.size, &user_string_heap_offset_space, &buffer, &remaining_buffer_len))
            return false;
    }

#ifdef DNMD_PORTABLE_PDB
    if (cxt->pdb.size != 0)
    {
        if (!write_stream_header("#Pdb", cxt->pdb.size, &pdb_offset_space, &buffer, &remaining_buffer_len))
            return false;
    }
#endif // DNMD_PORTABLE_PDB

    size_t table_stream_size = get_table_stream_size(cxt);

    if (table_stream_size > UINT32_MAX)
        return false;

    if (!write_stream_header(tables_stream_name, (uint32_t)table_stream_size, &tables_heap_offset_space, &buffer, &remaining_buffer_len))
        return false;

    // Write the stream data
    if (cxt->strings_heap.size != 0)
    {
        assert(strings_heap_offset_space.ptr != NULL && strings_heap_offset_space.size == 4);
        write_u32(&strings_heap_offset_space.ptr, &strings_heap_offset_space.size, (uint32_t)(buffer - buffer_start));
        uint32_t string_heap_size = align_to((uint32_t)cxt->strings_heap.size, 4);
        if (remaining_buffer_len < string_heap_size)
            return false;
        memcpy(buffer, cxt->strings_heap.ptr, cxt->strings_heap.size);
        memset((uint8_t*)buffer + cxt->strings_heap.size, 0, string_heap_size - cxt->strings_heap.size);
        advance_output_stream(&buffer, &remaining_buffer_len, string_heap_size);
    }

    if (cxt->blob_heap.size != 0)
    {
        assert(blob_heap_offset_space.ptr != NULL && blob_heap_offset_space.size == 4);
        write_u32(&blob_heap_offset_space.ptr, &blob_heap_offset_space.size, (uint32_t)(buffer - buffer_start));
        if (remaining_buffer_len < cxt->blob_heap.size)
            return false;
        memcpy(buffer, cxt->blob_heap.ptr, cxt->blob_heap.size);
        advance_output_stream(&buffer, &remaining_buffer_len, cxt->blob_heap.size);
    }

    if (cxt->guid_heap.size != 0)
    {
        assert(guid_heap_offset_space.ptr != NULL && guid_heap_offset_space.size == 4);
        write_u32(&guid_heap_offset_space.ptr, &guid_heap_offset_space.size, (uint32_t)(buffer - buffer_start));
        if (remaining_buffer_len < cxt->guid_heap.size)
            return false;
        memcpy(buffer, cxt->guid_heap.ptr, cxt->guid_heap.size);
        advance_output_stream(&buffer, &remaining_buffer_len, cxt->guid_heap.size);
    }

    if (cxt->user_string_heap.size != 0)
    {
        assert(user_string_heap_offset_space.ptr != NULL && user_string_heap_offset_space.size == 4);
        write_u32(&user_string_heap_offset_space.ptr, &user_string_heap_offset_space.size, (uint32_t)(buffer - buffer_start));
        if (remaining_buffer_len < cxt->user_string_heap.size)
            return false;
        memcpy(buffer, cxt->user_string_heap.ptr, cxt->user_string_heap.size);
        advance_output_stream(&buffer, &remaining_buffer_len, cxt->user_string_heap.size);
    }

#ifdef DNMD_PORTABLE_PDB
    if (cxt->pdb.size != 0)
    {
        assert(pdb_offset_space.ptr != NULL && pdb_offset_space.size == 4);
        write_u32(&pdb_offset_space.ptr, &pdb_offset_space.size, (uint32_t)(buffer - buffer_start));
        if (remaining_buffer_len < cxt->pdb.size)
            return false;
        memcpy(buffer, cxt->pdb.ptr, cxt->pdb.size);
        advance_output_stream(&buffer, &remaining_buffer_len, cxt->pdb.size);
    }
#endif // DNMD_PORTABLE_PDB

    if (remaining_buffer_len < table_stream_size)
        return false;

    // Always write the table stream header. This is required for a valid image.
    assert(tables_heap_offset_space.ptr != NULL && tables_heap_offset_space.size == 4);
    write_u32(&tables_heap_offset_space.ptr, &tables_heap_offset_space.size, (uint32_t)(buffer - buffer_start));
    if (!write_u32(&buffer, &remaining_buffer_len, 0) // Reserved
        || !write_u8(&buffer, &remaining_buffer_len, 2) // MajorVersion
        || !write_u8(&buffer, &remaining_buffer_len, 0) // MinorVersion
        || !write_u8(&buffer, &remaining_buffer_len, (uint8_t)(cxt->context_flags & mdc_image_flags & ~mdc_extra_data)) // HeapOffsetSizes, excluding the extra data flag as we don't save it to write out.
        || !write_u8(&buffer, &remaining_buffer_len, 1) // Reserved
        || !write_u64(&buffer, &remaining_buffer_len, valid_tables)
        || !write_u64(&buffer, &remaining_buffer_len, sorted_tables))
    {
        return false;
    }

    if (valid_tables != 0)
    {
        for (uint8_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
        {
            if (valid_tables & (1ULL << i))
            {
                if (!write_u32(&buffer, &remaining_buffer_len, cxt->tables[i].row_count))
                    return false;
            }
        }

        for (uint8_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
        {
            if (valid_tables & (1ULL << i))
            {
                assert (remaining_buffer_len >= cxt->tables[i].data.size);
                memcpy(buffer, cxt->tables[i].data.ptr, cxt->tables[i].data.size);
                advance_output_stream(&buffer, &remaining_buffer_len, cxt->tables[i].data.size);
            }
        }
    }

    assert(full_buffer_len - remaining_buffer_len == image_size);
    return true;
}
