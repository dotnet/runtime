#include "internal.h"

static bool append_heaps_from_delta(mdcxt_t* cxt, mdcxt_t* delta)
{
    assert(delta != NULL);

    if (!append_heap(cxt, delta, mdtc_hstring))
        return false;

    if (!append_heap(cxt, delta, mdtc_hguid))
        return false;

    if (!append_heap(cxt, delta, mdtc_hblob))
        return false;

    if (!append_heap(cxt, delta, mdtc_hus))
        return false;

    return true;
}

typedef enum
{
    dops_Default = 0,
    dops_MethodCreate,
    dops_FieldCreate,
    dops_ParamCreate,
    dops_PropertyCreate,
    dops_EventCreate,
} delta_ops_t;

#define NO_TOKENS_IN_GROUP (uint32_t)(-1)

// Tokens in the EncMap and EncLog tables may have the high bit set to indicate that they aren't true token references.
// Deltas produced by CoreCLR, CLR, and ILASM will have this bit set. Roslyn does not utilize this bit.
// We'll strip this high bit if it is set since we don't need it.
#define RemoveRecordBit(x) (x & 0x7fffffff)

typedef struct map_table_group__
{
    mdcursor_t start;
    uint32_t count;
} map_table_group_t;

typedef struct enc_token_map__
{
    map_table_group_t map_cur_by_table[MDTABLE_MAX_COUNT];
} enc_token_map_t;

static bool initialize_token_map(mdtable_t* map, enc_token_map_t* token_map)
{
    assert(map != NULL);
    assert(token_map != NULL);
    assert(map->table_id == mdtid_ENCMap);
    for (uint32_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        token_map->map_cur_by_table[i].count = NO_TOKENS_IN_GROUP;
    }

    // If we don't have any entries in the map table, then we don't have any remapped tokens.
    // The initialization we've already done by this point is sufficient.
    if (map->row_count == 0)
        return true;

    // The EncMap table is grouped by token type and sorted by the order of the rows in the tables in the delta.
    mdcursor_t map_cur = create_cursor(map, 1);

    mdtable_id_t previous_table_id = mdtid_Unused;
    for (uint32_t i = 0; i < map->row_count; (void)md_cursor_next(&map_cur), ++i)
    {
        mdToken tk;
        if (!md_get_column_value_as_constant(map_cur, mdtENCMap_Token, &tk))
            return false;

        mdtable_id_t table_id = ExtractTokenType(RemoveRecordBit(tk));

        if (table_id < mdtid_First || table_id >= mdtid_End)
            return false;

        if (token_map->map_cur_by_table[table_id].count == NO_TOKENS_IN_GROUP)
        {
            token_map->map_cur_by_table[table_id].start = map_cur;
            token_map->map_cur_by_table[table_id].count = 1;
        }
        else if (previous_table_id != table_id)
        {
            // If the set of remapped tokens for this table has already been started, then the previous token
            // must be from the same table as the current token (tokens are grouped by table).
            return false;
        }
        else
        {
            token_map->map_cur_by_table[table_id].count++;
        }

        previous_table_id = table_id;
    }

    return true;
}

static bool resolve_token(enc_token_map_t* token_map, mdToken referenced_token, mdhandle_t delta_image, mdcursor_t* row_in_delta)
{
    mdtable_id_t type = ExtractTokenType(referenced_token);

    if (type < mdtid_First || type >= mdtid_End)
        return false;

    uint32_t rid = RidFromToken(referenced_token);

    if (rid == 0)
        return false;

    // If we don't have any EncMap entries for this table,
    // then the token in the EncLog is the token we need to look up in the delta image to get the delta info to apply.
    if (token_map->map_cur_by_table[type].count == NO_TOKENS_IN_GROUP)
    {
        return md_token_to_cursor(delta_image, referenced_token, row_in_delta);
    }

    mdcursor_t map_record = token_map->map_cur_by_table[type].start;
    for (uint32_t i = 0; i < token_map->map_cur_by_table[type].count; md_cursor_next(&map_record), i++)
    {
        mdToken mappedToken;
        if (!md_get_column_value_as_constant(map_record, mdtENCMap_Token, &mappedToken))
            return false;

        assert((mdtable_id_t)ExtractTokenType(RemoveRecordBit(mappedToken)) == type);
        if (RidFromToken(mappedToken) == rid)
        {
            return md_token_to_cursor(delta_image, TokenFromRid(i + 1, CreateTokenType(type)), row_in_delta);
        }
    }

    // If we have a set of remapped tokens for a table,
    // we will remap all tokens in the EncLog.
    return false;
}

static bool add_list_target_row(mdcursor_t parent, col_index_t list_col)
{
    mdcursor_t new_child_record;
    if (!md_add_new_row_to_list(parent, list_col, &new_child_record))
        return false;

    // We've added enough information to the new record to make it valid for sorting purposes.
    // Commit the row add. We'll fill in the rest of the data in the next record in the EncLog.
    md_commit_row_add(new_child_record);

    return true;
}

static bool process_log(mdcxt_t* cxt, mdcxt_t* delta)
{
    // The EncMap table is grouped by token type and sorted by the order of the rows in the tables in the delta.
    mdtable_t* map = &delta->tables[mdtid_ENCMap];
    enc_token_map_t token_map;
    if (!initialize_token_map(map, &token_map))
        return false;

    mdtable_t* log = &delta->tables[mdtid_ENCLog];
    mdcursor_t log_cur = create_cursor(log, 1);
    delta_ops_t last_op = dops_Default;
    for (uint32_t i = 0; i < log->row_count; (void)md_cursor_next(&log_cur), ++i)
    {
        mdToken tk;
        uint32_t op;
        if (!md_get_column_value_as_constant(log_cur, mdtENCLog_Token, &tk)
            || !md_get_column_value_as_constant(log_cur, mdtENCLog_Op, &op))
        {
            return false;
        }

        tk = RemoveRecordBit(tk);

        switch ((delta_ops_t)op)
        {
        case dops_MethodCreate:
        {
            if (ExtractTokenType(tk) != mdtid_TypeDef)
                return false;

            // By the time we're adding a member to a list, the parent should already be in the image.
            mdcursor_t parent;
            if (!md_token_to_cursor(cxt, tk, &parent))
                return false;

            if (!add_list_target_row(parent, mdtTypeDef_MethodList))
                return false;
            break;
        }
        case dops_FieldCreate:
        {
            if (ExtractTokenType(tk) != mdtid_TypeDef)
                return false;

            // By the time we're adding a member to a list, the parent should already be in the image.
            mdcursor_t parent;
            if (!md_token_to_cursor(cxt, tk, &parent))
                return false;

            if (!add_list_target_row(parent, mdtTypeDef_FieldList))
                return false;
            break;
        }
        case dops_ParamCreate:
        {
            if (ExtractTokenType(tk) != mdtid_MethodDef)
                return false;

            // By the time we're adding a member to a list, the parent should already be in the image.
            mdcursor_t parent;
            if (!md_token_to_cursor(cxt, tk, &parent))
                return false;

            // We don't use md_add_new_row_to_sorted_list here because we don't know the value of the Sequence column
            // until we process the next record in the EncLog.
            // We'll re-sort the list after we process the next entry in the EncLog.
            if (!add_list_target_row(parent, mdtMethodDef_ParamList))
                return false;
            break;
        }
        case dops_PropertyCreate:
        {
            if (ExtractTokenType(tk) != mdtid_PropertyMap)
                return false;

            // By the time we're adding a member to a list, the parent should already be in the image.
            mdcursor_t parent;
            if (!md_token_to_cursor(cxt, tk, &parent))
                return false;

            if (!add_list_target_row(parent, mdtPropertyMap_PropertyList))
                return false;
            break;
        }
        case dops_EventCreate:
        {
            if (ExtractTokenType(tk) != mdtid_EventMap)
                return false;

            // By the time we're adding a member to a list, the parent should already be in the image.
            mdcursor_t parent;
            if (!md_token_to_cursor(cxt, tk, &parent))
                return false;

            if (!add_list_target_row(parent, mdtEventMap_EventList))
                return false;
            break;
        }
        case dops_Default:
        {
            mdtable_id_t table_id = ExtractTokenType(tk);

            if (table_id < mdtid_First || table_id >= mdtid_End)
                return false;

            uint32_t rid = RidFromToken(tk);

            if (rid == 0)
                return false;

            // Resolve the token in the delta image that has the data that we need to copy to the base image.
            mdcursor_t delta_record;
            if (!resolve_token(&token_map, tk, delta, &delta_record))
                return false;

            // Try resolving the original token to determine what row we're editing.
            // We'll try to look up the row in the base image.
            // If we fail to resolve the original token, then we aren't editing an existing row,
            // but instead creating a new row.
            mdcursor_t record_to_edit;
            bool edit_record = md_token_to_cursor(cxt, tk, &record_to_edit);

            // We can only add rows directly to the end of the table.
            // TODO: In the future, we could be smarter
            // and try to insert a row in the middle of a table to preserve sorting.
            // For some tables that aren't referred to by other tables, such as CustomAttribute,
            // we could get much better performance by preserving the sorted behavior.
            // If the runtime doesn't have any dependency on tokens being stable for these tables,
            // this optimization may reduce the need for maintaining a manual sorting above the metadata layer.
            if (!edit_record)
            {
                mdtable_t* table = &cxt->tables[table_id];

                // If we're adding a row to a table, then the row we're adding must be the next row in the table.
                // If it's not, then we're missing some row-add operations that should have happened previously.
                // The ENC Log is invalid.
                if (table->row_count != (rid - 1))
                    return false;

                if (!md_append_row(cxt, table_id, &record_to_edit))
                    return false;
            }

            if (!copy_cursor(record_to_edit, delta_record))
                return false;

            if (!edit_record)
                md_commit_row_add(record_to_edit);

            if (last_op == dops_ParamCreate)
            {
                // If the last operation we did was a "create parameter" operation,
                // then we need to ensure that the ParamList is sorted by Sequence.
                // This ordering is not guaranteed by EnC delta producers,
                // so we need to enforce it ourselves during delta application.
                mdcursor_t parent;
                bool success = md_find_cursor_of_range_element(record_to_edit, &parent);
                assert(success);
                (void)success;

                if (!sort_list_by_column(parent, mdtMethodDef_ParamList, mdtParam_Sequence))
                   return false;
            }
            // TODO: Write to the ENC Log in cxt to record the change.
            break;
        }
        default:
            assert(!"Unknown delta operation");
            return false;
        }

        last_op = (delta_ops_t)op;
    }

    return true;
}

bool merge_in_delta(mdcxt_t* cxt, mdcxt_t* delta)
{
    assert(cxt != NULL);
    assert(delta != NULL && (delta->context_flags & mdc_minimal_delta));

    // Validate metadata versions
    if (cxt->major_ver != delta->major_ver
        || cxt->minor_ver != delta->minor_ver)
    {
        return false;
    }

    mdcursor_t base_module = create_cursor(&cxt->tables[mdtid_Module], 1);
    mdcursor_t delta_module = create_cursor(&delta->tables[mdtid_Module], 1);

    mdguid_t base_mvid;
    if (!md_get_column_value_as_guid(base_module, mdtModule_Mvid, &base_mvid))
        return false;

    mdguid_t delta_mvid;
    if (!md_get_column_value_as_guid(delta_module, mdtModule_Mvid, &delta_mvid))
        return false;

    // MVIDs must match between base and delta images.
    if (memcmp(&base_mvid, &delta_mvid, sizeof(mdguid_t)) != 0)
        return false;

    // The EncBaseId of the delta must equal the EncId of the base image.
    // This ensures that we are applying deltas in order.
    mdguid_t enc_id;
    mdguid_t delta_enc_base_id;
    if (!md_get_column_value_as_guid(base_module, mdtModule_EncId, &enc_id)
        || !md_get_column_value_as_guid(delta_module, mdtModule_EncBaseId, &delta_enc_base_id)
        || memcmp(&enc_id, &delta_enc_base_id, sizeof(mdguid_t)) != 0)
    {
        return false;
    }

    // Merge heaps
    if (!append_heaps_from_delta(cxt, delta))
    {
        return false;
    }

    // Process delta log
    if (!process_log(cxt, delta))
    {
        return false;
    }

    // Now that we've applied the delta,
    // update our Enc IDd to match the delta's ID in preparation for the next delta.
    // We don't want to manipulate the heap sizes, so we'll pull the heap offset directly from the delta and use that
    // in the base image.
    uint32_t new_enc_base_id_offset;
    if (!get_column_value_as_heap_offset(delta_module, mdtModule_EncId, &new_enc_base_id_offset))
        return false;
    if (!set_column_value_as_heap_offset(base_module, mdtModule_EncId, new_enc_base_id_offset))
        return false;

    return true;
}
