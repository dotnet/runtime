#include "internal.h"

md_blob_parse_result_t md_parse_document_name(mdhandle_t handle, uint8_t const* blob, size_t blob_len, char const* name, size_t* name_len)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return mdbpr_InvalidArgument;

    if (blob == NULL || name_len == NULL)
        return mdbpr_InvalidArgument;

    // Only support one-character ASCII seperators.
    // System.Reflection.Metadata.MetadataReader has the same limitation
    uint8_t separator;
    if (!read_u8(&blob, &blob_len, &separator))
        return mdbpr_InvalidBlob;

    if (separator > 0x7f)
        return mdbpr_InvalidBlob;

    uint8_t* name_current = (uint8_t*)name;
    size_t remaining_name_len = *name_len;
    size_t required_len = 0;
    md_blob_parse_result_t result = mdbpr_Success;
    bool write_separator = false;
    while (blob_len > 0)
    {
        if (write_separator)
        {
            // Add the required space for the separator.
            // If there is space in the buffer, write the separator.
            required_len += 1;
            if (name_current == NULL || remaining_name_len == 0)
            {
                result = mdbpr_InsufficientBuffer;
            }
            else
            {
                write_u8(&name_current, &remaining_name_len, separator);
            }
        }
        write_separator = separator != 0;

        // Get the next part of the path.
        uint32_t part_offset;
        if (!decompress_u32(&blob, &blob_len, &part_offset))
            return mdbpr_InvalidBlob;

        // The part blob is a UTF-8 string that is not null-terminated.
        const uint8_t* part;
        uint32_t part_len;
        if (!try_get_blob(cxt, part_offset, &part, &part_len))
            return mdbpr_InvalidBlob;

        // Add the required space for the part.
        // If there is space in the buffer, write the part.
        required_len += part_len;
        if (name_current == NULL || remaining_name_len < part_len)
        {
            result = mdbpr_InsufficientBuffer;
            continue;
        }
        else
        {
            memcpy(name_current, part, part_len);
            bool success = advance_output_stream(&name_current, &remaining_name_len, part_len);
            assert(success);
            (void)success;
        }
    }

    // Add the null terminator.
    required_len++;
    if (name_current != NULL && remaining_name_len > 0)
        write_u8(&name_current, &remaining_name_len, 0);
    else
        result = mdbpr_InsufficientBuffer;

    *name_len = required_len;
    return result;
}

// We only support up to UINT32_MAX - 1 sequence points per method.
// Technically, the number of supported sequence points in the spec is unbounded.
// However, the PE format that an ECMA-335 blob is commonly wrapped in
// can only support up to 4GB files, so we can't possibly have UINT32_MAX - 1 entries
// in any existing scenario anyway.
static uint32_t get_num_sequence_points(mdcursor_t method_debug_information, uint8_t const* blob, size_t blob_len)
{
    uint32_t num_records = 0;
    uint32_t ignored;
    if (!decompress_u32(&blob, &blob_len, &ignored)) // header LocalSignature
        return UINT32_MAX;

    mdcursor_t document;
    if (1 != md_get_column_value_as_cursor(method_debug_information, mdtMethodDebugInformation_Document, 1, &document))
        return UINT32_MAX;

    if (CursorNull(&document) && !decompress_u32(&blob, &blob_len, &ignored)) // header InitialDocument
        return UINT32_MAX;

    bool first_record = true;
    while (blob_len > 0)
    {
        if (num_records == UINT32_MAX)
            return UINT32_MAX;
        num_records++;
        uint32_t il_offset;
        if (!decompress_u32(&blob, &blob_len, &il_offset)) // ILOffset
            return UINT32_MAX;

        // The first record cannot be a document record
        if (!first_record && il_offset == 0)
        {
            uint32_t document_offset;
            if (!decompress_u32(&blob, &blob_len, &document_offset)) // Document
                return UINT32_MAX;
        }
        else
        {
            // We don't need to check if we need to do an unsigned or signed decompression
            // as we will always read the same number of bytes and we don't care about the values here
            // as we're only calculating the number of records.
            uint32_t delta_lines;
            if (!decompress_u32(&blob, &blob_len, &delta_lines)) // DeltaLines
                return UINT32_MAX;

            uint32_t delta_columns;
            if (!decompress_u32(&blob, &blob_len, &delta_columns)) // DeltaColumns
                return UINT32_MAX;
            if (delta_lines != 0 || delta_columns != 0)
            {
                uint32_t start_line;
                if (!decompress_u32(&blob, &blob_len, &start_line)) // StartLine
                    return UINT32_MAX;
                uint32_t start_column;
                if (!decompress_u32(&blob, &blob_len, &start_column)) // StartColumn
                    return UINT32_MAX;
            }
        }

        first_record = false;
    }

    return num_records;
}

md_blob_parse_result_t md_parse_sequence_points(
    mdcursor_t method_debug_information,
    uint8_t const* blob,
    size_t blob_len,
    md_sequence_points_t* sequence_points,
    size_t* buffer_len)
{
    if (CursorNull(&method_debug_information) || CursorEnd(&method_debug_information))
        return mdbpr_InvalidArgument;

    if (blob == NULL || buffer_len == NULL)
        return mdbpr_InvalidArgument;

    uint32_t num_records = get_num_sequence_points(method_debug_information, blob, blob_len);

    if (num_records == UINT32_MAX)
        return mdbpr_InvalidBlob;

    size_t required_size = sizeof(md_sequence_points_t) + num_records * sizeof(sequence_points->records[0]);
    if (sequence_points == NULL || *buffer_len < required_size)
    {
        *buffer_len = required_size;
        return mdbpr_InsufficientBuffer;
    }

    // header LocalSignature
    if (!decompress_u32(&blob, &blob_len, &sequence_points->signature))
        return mdbpr_InvalidBlob;

    mdcursor_t document;
    if (1 != md_get_column_value_as_cursor(method_debug_information, mdtMethodDebugInformation_Document, 1, &document))
        return mdbpr_InvalidBlob;

    // Create a "null" cursor to default-initialize the document field.
    mdcxt_t* cxt = extract_mdcxt(md_extract_handle_from_cursor(method_debug_information));
    sequence_points->document = create_cursor(&cxt->tables[mdtid_Document], 0);

    // header InitialDocument
    uint32_t document_rid = 0;
    if (CursorNull(&document)
        && !decompress_u32(&blob, &blob_len, &document_rid))
    {
        return mdbpr_InvalidBlob;
    }

    if (document_rid != 0
        && !md_token_to_cursor(cxt, CreateTokenType(mdtid_Document) | document_rid, &sequence_points->document))
    {
        return mdbpr_InvalidBlob;
    }

    bool seen_non_hidden_sequence_point = false;
    for (uint32_t i = 0; blob_len > 0 && i < num_records; ++i)
    {
        uint32_t il_offset;
        if (!decompress_u32(&blob, &blob_len, &il_offset)) // ILOffset
            return mdbpr_InvalidBlob;

        // Check if the method transitioned
        // into a new source file.
        if (i != 0 && il_offset == 0)
        {
            uint32_t document_row_id;
            if (!decompress_u32(&blob, &blob_len, &document_row_id)) // Document
                return mdbpr_InvalidBlob;

            sequence_points->records[i].kind = mdsp_DocumentRecord;
            if (!md_token_to_cursor(cxt, CreateTokenType(mdtid_Document) | document_row_id, &sequence_points->records[i].document.document))
                return mdbpr_InvalidBlob;

            continue;
        }

        uint32_t delta_lines;
        if (!decompress_u32(&blob, &blob_len, &delta_lines)) // DeltaLines
            return mdbpr_InvalidBlob;

        int64_t delta_columns;
        if (delta_lines == 0)
        {
            uint32_t raw_delta_columns;
            if (!decompress_u32(&blob, &blob_len, &raw_delta_columns)) // DeltaColumns
                return mdbpr_InvalidBlob;
            delta_columns = raw_delta_columns;
        }
        else
        {
            int32_t raw_delta_columns;
            if (!decompress_i32(&blob, &blob_len, &raw_delta_columns)) // DeltaColumns
                return mdbpr_InvalidBlob;
            delta_columns = raw_delta_columns;
        }

        // Check for hidden point
        if (delta_lines == 0 && delta_columns == 0)
        {
            sequence_points->records[i].kind = mdsp_HiddenSequencePointRecord;
            sequence_points->records[i].hidden_sequence_point.rolling_il_offset = il_offset;
            continue;
        }

        int64_t start_line;
        int64_t start_column;
        if (!seen_non_hidden_sequence_point)
        {
            seen_non_hidden_sequence_point = true;
            uint32_t start_line_raw;
            if (!decompress_u32(&blob, &blob_len, &start_line_raw)) // StartLine
                return mdbpr_InvalidBlob;
            uint32_t start_column_raw;
            if (!decompress_u32(&blob, &blob_len, &start_column_raw)) // StartColumn
                return mdbpr_InvalidBlob;
            start_line = start_line_raw;
            start_column = start_column_raw;
        }
        else
        {
            // If we've seen a non-hidden sequence point,
            // then the values are compressed signed integers instead of
            // unsigned integers.
            int32_t start_line_raw;
            if (!decompress_i32(&blob, &blob_len, &start_line_raw)) // StartLine
                return mdbpr_InvalidBlob;
            int32_t start_column_raw;
            if (!decompress_i32(&blob, &blob_len, &start_column_raw)) // StartColumn
                return mdbpr_InvalidBlob;
            start_line = start_line_raw;
            start_column = start_column_raw;
        }

        sequence_points->records[i].kind = mdsp_SequencePointRecord;
        sequence_points->records[i].sequence_point.rolling_il_offset = il_offset;
        sequence_points->records[i].sequence_point.delta_lines = delta_lines;
        sequence_points->records[i].sequence_point.delta_columns = delta_columns;
        sequence_points->records[i].sequence_point.rolling_start_line = start_line;
        sequence_points->records[i].sequence_point.rolling_start_column = start_column;
    }

    if (blob_len != 0)
        return mdbpr_InvalidBlob;

    sequence_points->record_count = num_records;
    return mdbpr_Success;
}

md_blob_parse_result_t md_parse_local_constant_sig(mdhandle_t handle, uint8_t const* blob, size_t blob_len, md_local_constant_sig_t* local_constant_sig, size_t* buffer_len)
{
    if (extract_mdcxt(handle) == NULL || blob == NULL || buffer_len == NULL)
        return mdbpr_InvalidArgument;

    // Walk the custom modifiers portion of the signature to calculate the required buffer space.
    uint8_t const* custom_modifiers_blob = blob;
    size_t custom_modifiers_blob_len = blob_len;
    uint32_t num_custom_modifiers = 0;
    for (; custom_modifiers_blob_len > 0; ++num_custom_modifiers)
    {
        uint32_t element_type;
        if (!decompress_u32(&custom_modifiers_blob, &custom_modifiers_blob_len, &element_type))
            return mdbpr_InvalidBlob;

        if (element_type != ELEMENT_TYPE_CMOD_OPT && element_type != ELEMENT_TYPE_CMOD_REQD)
            break;

        uint32_t cindex;
        if (!decompress_u32(&custom_modifiers_blob, &custom_modifiers_blob_len, &cindex))
            return mdbpr_InvalidBlob;
    }

    size_t required_size = sizeof(md_local_constant_sig_t) + num_custom_modifiers * sizeof(local_constant_sig->custom_modifiers[0]);
    if (local_constant_sig == NULL || *buffer_len < required_size)
    {
        *buffer_len = required_size;
        return mdbpr_InsufficientBuffer;
    }

    local_constant_sig->custom_modifier_count = num_custom_modifiers;

    for (uint32_t i = 0; i < num_custom_modifiers; ++i)
    {
        uint32_t element_type;
        if (!decompress_u32(&custom_modifiers_blob, &custom_modifiers_blob_len, &element_type))
            return mdbpr_InvalidBlob;

        if (element_type != ELEMENT_TYPE_CMOD_OPT && element_type != ELEMENT_TYPE_CMOD_REQD)
            break;

        local_constant_sig->custom_modifiers[i].required = element_type == ELEMENT_TYPE_CMOD_REQD;

        uint32_t cindex;
        if (!decompress_u32(&custom_modifiers_blob, &custom_modifiers_blob_len, &cindex))
            return mdbpr_InvalidBlob;

        mdtable_id_t table;
        uint32_t row_id;
        // Technically the spec defines this as a TypeDefOrRefOrSpecEncoded token,
        // but the implementation of the TypeDefOrRef coded index has the same configuration as the
        // TypeDefOrRefOrSpec encoding.
        if (!decompose_coded_index(cindex, mdtc_idx_coded | InsertCodedIndex(mdci_TypeDefOrRef), &table, &row_id))
            return mdbpr_InvalidBlob;

        local_constant_sig->custom_modifiers[i].type = CreateTokenType(table) | row_id;
    }

    uint32_t type_code;
    if (!decompress_u32(&blob, &blob_len, &type_code))
        return mdbpr_InvalidBlob;

    uint32_t constant_type_index;
    mdtable_id_t constant_type_table;
    uint32_t constant_type_row;
    switch (type_code)
    {
        case ELEMENT_TYPE_OBJECT:
            local_constant_sig->constant_kind = mdck_GeneralConstant;
            local_constant_sig->general.kind = mdgc_Object;
            local_constant_sig->general.type = 0;
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            break;
        case ELEMENT_TYPE_VALUETYPE:
            local_constant_sig->constant_kind = mdck_GeneralConstant;
            local_constant_sig->general.kind = mdgc_ValueType;
            if (!decompress_u32(&blob, &blob_len, &constant_type_index))
                return mdbpr_InvalidBlob;
            if (!decompose_coded_index(constant_type_index, mdtc_idx_coded | InsertCodedIndex(mdci_TypeDefOrRef), &constant_type_table, &constant_type_row))
                return mdbpr_InvalidBlob;
            local_constant_sig->general.type = CreateTokenType(constant_type_table) | constant_type_row;
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            break;
        case ELEMENT_TYPE_CLASS:
            local_constant_sig->constant_kind = mdck_GeneralConstant;
            local_constant_sig->general.kind = mdgc_Class;
            if (!decompress_u32(&blob, &blob_len, &constant_type_index))
                return mdbpr_InvalidBlob;
            if (!decompose_coded_index(constant_type_index, mdtc_idx_coded | InsertCodedIndex(mdci_TypeDefOrRef), &constant_type_table, &constant_type_row))
                return mdbpr_InvalidBlob;
            local_constant_sig->general.type = CreateTokenType(constant_type_table) | constant_type_row;
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            break;
        // These constants are never enums, so we don't need to skip the content.
        case ELEMENT_TYPE_R4:
            if (blob_len != 4)
                return mdbpr_InvalidBlob;
            local_constant_sig->constant_kind = mdck_PrimitiveConstant;
            local_constant_sig->primitive.type_code = (uint8_t)type_code;
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            break;
        case ELEMENT_TYPE_R8:
            if (blob_len != 8)
                return mdbpr_InvalidBlob;
            local_constant_sig->constant_kind = mdck_PrimitiveConstant;
            local_constant_sig->primitive.type_code = (uint8_t)type_code;
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            break;
        case ELEMENT_TYPE_STRING:
            local_constant_sig->constant_kind = mdck_PrimitiveConstant;
            local_constant_sig->primitive.type_code = (uint8_t)type_code;
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            break;
        // These constant types might be enums, so we need to check if there's a TypeDefOrRefOrSpecEncoded value
        // after the value to determine if the constant is an enum.
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
            // Save off the value.
            local_constant_sig->value_blob = blob;
            local_constant_sig->value_len = blob_len;
            switch (type_code)
            {
                case ELEMENT_TYPE_BOOLEAN:
                case ELEMENT_TYPE_I1:
                case ELEMENT_TYPE_U1:
                {
                    uint8_t dummy;
                    if (!read_u8(&blob, &blob_len, &dummy))
                        return mdbpr_InvalidBlob;
                    break;
                }
                case ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE_I2:
                case ELEMENT_TYPE_U2:
                {
                    uint16_t dummy;
                    if (!read_u16(&blob, &blob_len, &dummy))
                        return mdbpr_InvalidBlob;
                    break;
                }
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                {
                    uint32_t dummy;
                    if (!read_u32(&blob, &blob_len, &dummy))
                        return mdbpr_InvalidBlob;
                    break;
                }
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                {
                    uint64_t dummy;
                    if (!read_u64(&blob, &blob_len, &dummy))
                        return mdbpr_InvalidBlob;
                    break;
                }
                default:
                    assert(false);
                    return mdbpr_InvalidArgument;
            }

            // Check if there is any remaining blob data.
            if (blob_len == 0)
            {
                local_constant_sig->constant_kind = mdck_PrimitiveConstant;
                local_constant_sig->primitive.type_code = (uint8_t)type_code;
            }
            else
            {
                // If we have data remaining, then we need to read the enum type.
                // In this case, we subtract off the rest of the blob length from the value blob length
                // as it isn't part of the value.
                local_constant_sig->value_len -= blob_len;
                if (!decompress_u32(&blob, &blob_len, &constant_type_index)
                    || !decompose_coded_index(constant_type_index, mdtc_idx_coded | InsertCodedIndex(mdci_TypeDefOrRef), &constant_type_table, &constant_type_row))
                {
                    return mdbpr_InvalidBlob;
                }

                local_constant_sig->constant_kind = mdck_EnumConstant;
                local_constant_sig->enum_constant.type_code = (uint8_t)type_code;
                local_constant_sig->enum_constant.enum_type = CreateTokenType(constant_type_table) | constant_type_row;
            }
            break;
        default:
            assert(false);
            return mdbpr_InvalidArgument;
    }
    return mdbpr_Success;
}

// We only support up to UINT32_MAX - 1 imports per Imports blob.
// Technically, the number of supported imports in the spec is unbounded.
// However, the PE format that an ECMA-335 blob is commonly wrapped in
// can only support up to 4GB files, so we can't possibly have UINT32_MAX - 1 entries
// in any existing scenario anyway.
static uint32_t get_num_imports(uint8_t const* blob, size_t blob_len)
{
    uint32_t num_imports = 0;
    for (;blob_len > 0 && num_imports < UINT32_MAX; ++num_imports)
    {
        uint8_t kind;
        if (!read_u8(&blob, &blob_len, &kind))
            return UINT32_MAX;

        uint32_t raw;
        switch (kind)
        {
            case mdidk_ImportNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-namespace
                    return UINT32_MAX;
                break;
            case mdidk_ImportAssemblyNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-assembly
                    return UINT32_MAX;
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-namespace
                    return UINT32_MAX;
                break;
            case mdidk_ImportType:
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-type
                    return UINT32_MAX;
                break;
            case mdidk_AliasNamespace:
            case mdidk_ImportXmlNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw)) // alias
                    return UINT32_MAX;
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-namespace
                    return UINT32_MAX;
                break;
            case mdidk_ImportAssemblyReferenceAlias:
                if (!decompress_u32(&blob, &blob_len, &raw)) // alias
                    return UINT32_MAX;
                break;
            case mdidk_AliasAssemblyReference:
                if (!decompress_u32(&blob, &blob_len, &raw)) // alias
                    return UINT32_MAX;
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-assembly
                    return UINT32_MAX;
                break;
            case mdidk_AliasAssemblyNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw)) // alias
                    return UINT32_MAX;
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-assembly
                    return UINT32_MAX;
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-namespace
                    return UINT32_MAX;
                break;
            case mdidk_AliasType:
                if (!decompress_u32(&blob, &blob_len, &raw)) // alias
                    return UINT32_MAX;
                if (!decompress_u32(&blob, &blob_len, &raw)) // target-type
                    return UINT32_MAX;
                break;
            default:
                return UINT32_MAX;
        }
    }
    return num_imports;
}

md_blob_parse_result_t md_parse_imports(mdhandle_t handle, uint8_t const* blob, size_t blob_len, md_imports_t* imports, size_t* buffer_len)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL || blob == NULL || buffer_len == NULL)
        return mdbpr_InvalidArgument;

    uint32_t num_imports = get_num_imports(blob, blob_len);
    if (num_imports == UINT32_MAX)
        return mdbpr_InvalidBlob;

    size_t required_size = sizeof(md_imports_t) + num_imports * sizeof(imports->imports[0]);
    if (imports == NULL || *buffer_len < required_size)
    {
        *buffer_len = required_size;
        return mdbpr_InsufficientBuffer;
    }

    imports->count = num_imports;
    for (uint32_t i = 0; i < num_imports; ++i)
    {
        uint8_t kind;
        if (!read_u8(&blob, &blob_len, &kind))
            return mdbpr_InvalidBlob;

        // Zero out this import entry.
        memset(&imports->imports[i], 0, sizeof(imports->imports[i]));

        imports->imports[i].kind = kind;
        uint32_t raw;
        switch (kind)
        {
            case mdidk_ImportNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].target_namespace, &imports->imports[i].target_namespace_len))
                    return mdbpr_InvalidBlob;
                break;
            case mdidk_ImportAssemblyNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                imports->imports[i].assembly = CreateTokenType(mdtid_AssemblyRef) | raw;

                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].target_namespace, &imports->imports[i].target_namespace_len))
                    return mdbpr_InvalidBlob;
                break;
            case mdidk_ImportType:
            {
                mdtable_id_t table;
                uint32_t row_id;
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!decompose_coded_index(raw, mdtc_idx_coded | InsertCodedIndex(mdci_TypeDefOrRef), &table, &row_id))
                    return mdbpr_InvalidBlob;

                imports->imports[i].target_type = CreateTokenType(table) | row_id;
                break;
            }
            case mdidk_AliasNamespace:
            case mdidk_ImportXmlNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].alias, &imports->imports[i].alias_len))
                    return mdbpr_InvalidBlob;

                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].target_namespace, &imports->imports[i].target_namespace_len))
                    return mdbpr_InvalidBlob;
                break;
            case mdidk_ImportAssemblyReferenceAlias:
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].alias, &imports->imports[i].alias_len))
                    return mdbpr_InvalidBlob;
                break;
            case mdidk_AliasAssemblyReference:
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].alias, &imports->imports[i].alias_len))
                    return mdbpr_InvalidBlob;

                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                imports->imports[i].assembly = CreateTokenType(mdtid_AssemblyRef) | raw;
                break;
            case mdidk_AliasAssemblyNamespace:
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].alias, &imports->imports[i].alias_len))
                    return mdbpr_InvalidBlob;

                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                imports->imports[i].assembly = CreateTokenType(mdtid_AssemblyRef) | raw;

                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].target_namespace, &imports->imports[i].target_namespace_len))
                    return mdbpr_InvalidBlob;
                break;
            case mdidk_AliasType:
            {
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!try_get_blob(cxt, raw, (uint8_t const**)&imports->imports[i].alias, &imports->imports[i].alias_len))
                    return mdbpr_InvalidBlob;

                mdtable_id_t table;
                uint32_t row_id;
                if (!decompress_u32(&blob, &blob_len, &raw))
                    return mdbpr_InvalidBlob;

                if (!decompose_coded_index(raw, mdtc_idx_coded | InsertCodedIndex(mdci_TypeDefOrRef), &table, &row_id))
                    return mdbpr_InvalidBlob;

                imports->imports[i].target_type = CreateTokenType(table) | row_id;
                break;
            }
            default:
                return mdbpr_InvalidBlob;
        }
    }
    return mdbpr_Success;
}