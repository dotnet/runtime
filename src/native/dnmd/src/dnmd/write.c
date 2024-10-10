#include "internal.h"

static bool is_row_sorted_with_next_row(md_key_info_t const* keys, uint8_t count_keys, mdtable_id_t table_id, mdcursor_t row, mdcursor_t next_row)
{
    // We have a previous row, let's validate that it's sorted.
    for (uint8_t i = 0; i < count_keys; i++)
    {
        col_index_t key_col = index_to_col(keys[i].index, table_id);

        access_cxt_t row_acxt;
        if (!create_access_context(&row, key_col, 1, false, &row_acxt))
            return false;

        // Key columns can only be constant, index into a table, or a coded token index.
        // Heap offset columns cannot be keys.
        assert(row_acxt.col_details & (mdtc_constant | mdtc_idx_table | mdtc_idx_coded));

        access_cxt_t next_acxt;
        if (!create_access_context(&next_row, key_col, 1, false, &next_acxt))
            return false;

        uint32_t row_value;
        if (!read_column_data(&row_acxt, &row_value))
            return false;

        uint32_t next_value;
        if (!read_column_data(&next_acxt, &next_value))
            return false;

        bool column_sorted = keys[i].descending ? (row_value >= next_value) : (row_value <= next_value);
        if (!column_sorted)
            return false;
    }
    return true;
}

static int32_t set_column_value_as_token_or_cursor(mdcursor_t c, uint32_t col_idx, mdToken const* tk, mdcursor_t const* cursor, uint32_t in_length)
{
    assert(in_length != 0 && (tk != NULL || cursor != NULL));

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If we can't write on the underlying table, then fail.
    if (acxt.writable_data == NULL)
        return -1;

    // If this isn't an index column, then fail.
    if (!(acxt.col_details & (mdtc_idx_table | mdtc_idx_coded)))
        return -1;

    uint8_t key_count = 0;
    uint8_t key_idx = UINT8_MAX;
    md_key_info_t const* keys = NULL;
    // If we're editing already-existing rows, then we need to validate that we stay sorted.
    // If we're in the middle of a row-add operation, we'll wait until the add is complete to validate.
    if (acxt.table->is_sorted && !acxt.table->is_adding_new_row)
    {
        // If the table is sorted, then we need to validate that we stay sorted.
        // We will not check here if a table goes from unsorted to sorted as that would require
        // significantly more work to validate and is not a correctness issue.
        key_count = get_table_keys(acxt.table->table_id, &keys);
        for (uint8_t i = 0; i < key_count; i++)
        {
            if (keys[i].index == col_to_index(col_idx, acxt.table))
            {
                key_idx = i;
                break;
            }
        }
    }

    int32_t written = 0;
    do
    {
        mdToken token;
        if (tk != NULL)
        {
            token = tk[written];
        }
        else
        {
            if (!md_cursor_to_token(cursor[written], &token))
                return -1;
        }

#ifdef DNMD_PORTABLE_PDB
        {
            uint32_t table_row = RidFromToken(token);
            mdtable_id_t table_id = ExtractTokenType(token);
            if (table_id < mdtid_FirstPdb)
            {
                if (!update_referenced_type_system_table_row_count(acxt.table->cxt, table_id, table_row))
                    return -1;
            }
        }
#endif

        uint32_t raw;
        if (acxt.col_details & mdtc_idx_table)
        {
            uint32_t table_row = RidFromToken(token);
            mdtable_id_t table_id = ExtractTokenType(token);
            // The raw value is the row index into the table that
            // is embedded in the column details.
            // Return an error if the provided token does not point to the right table.
            if (ExtractTable(acxt.col_details) != table_id)
                return -1;
            raw = table_row;
        }
        else
        {
            assert(acxt.col_details & mdtc_idx_coded);
            if (!compose_coded_index(token, acxt.col_details, &raw))
                return -1;
        }

        if (!write_column_data(&acxt, raw))
            return -1;

        // If the column we are writing to is a key of a sorted column, then we need to validate that it is sorted correctly.
        // We'll validate against the previous row here and then validate against the next row after we've written all of the columns that we will write.
        if (key_idx != UINT8_MAX)
        {
            assert(keys != NULL && key_idx < key_count);
            mdcursor_t current_row = c;
            bool success = md_cursor_move(&current_row, written);
            assert(success);
            (void)success;
            mdcursor_t prior_row = current_row;
            if (md_cursor_move(&prior_row, -1) && !CursorNull(&prior_row))
            {
                // If we have a prior row, then we need to check if we're sorted with respect to it.
                if (!is_row_sorted_with_next_row(keys, key_count, acxt.table->table_id, prior_row, current_row))
                {
                    // If we're not sorted, then invalidate key_idx to avoid checking if we're sorted for future row writes.
                    // We won't go from unsorted to sorted.
                    acxt.table->is_sorted = false;
                    key_idx = UINT8_MAX;
                }
            }
        }

        written++;
    } while (in_length > 1 && next_row(&acxt));

    // Validate that the last row we wrote is sorted with respect to any following rows.
    if (key_idx != UINT8_MAX)
    {
        assert(keys != NULL && key_idx < key_count);
        mdcursor_t current_row = c;
        bool success = md_cursor_move(&current_row, written);
        assert(success);
        (void)success;
        mdcursor_t next_row = current_row;
        if (md_cursor_move(&next_row, 1) && !CursorEnd(&next_row))
        {
            // If we have a prior row, then we need to check if we're sorted with respect to it.
            if (!is_row_sorted_with_next_row(keys, key_count, acxt.table->table_id, current_row, next_row))
            {
                // If we're not sorted, then invalidate key_idx to avoid checking if we're sorted for future row writes.
                // We won't go from unsorted to sorted.
                acxt.table->is_sorted = false;
                key_idx = UINT8_MAX;
            }
        }
    }

    return written;
}

int32_t md_set_column_value_as_token(mdcursor_t c, col_index_t col, uint32_t in_length, mdToken const* tk)
{
    if (tk == NULL || in_length == 0)
        return -1;
    return set_column_value_as_token_or_cursor(c, col_to_index(col, CursorTable(&c)), tk, NULL, in_length);
}

int32_t md_set_column_value_as_cursor(mdcursor_t c, col_index_t col, uint32_t in_length, mdcursor_t const* cursor)
{
    if (cursor == NULL || in_length == 0)
        return -1;
    return set_column_value_as_token_or_cursor(c, col_to_index(col, CursorTable(&c)), NULL, cursor, in_length);
}

int32_t md_set_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t in_length, uint32_t const* constant)
{
    if (in_length == 0)
        return 0;
    assert(constant != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(acxt.col_details & mdtc_constant))
        return -1;

    uint8_t key_count = 0;
    uint8_t key_idx = UINT8_MAX;
    md_key_info_t const* keys = NULL;
    // If we're editing already-existing rows, then we need to validate that we stay sorted.
    // If we're in the middle of a row-add operation, we'll wait until the add is complete to validate.
    if (acxt.table->is_sorted && !acxt.table->is_adding_new_row)
    {
        // If the table is sorted, then we need to validate that we stay sorted.
        // We will not check here if a table goes from unsorted to sorted as that would require
        // significantly more work to validate and is not a correctness issue.
        key_count = get_table_keys(acxt.table->table_id, &keys);
        for (uint8_t i = 0; i < key_count; i++)
        {
            if (keys[i].index == col_to_index(col_idx, acxt.table))
            {
                key_idx = i;
                break;
            }
        }
    }

    int32_t written = 0;
    do
    {
        if (!write_column_data(&acxt, constant[written]))
            return -1;

        // If the column we are writing to is a key of a sorted column, then we need to validate that it is sorted correctly.
        // We'll validate against the previous row here and then validate against the next row after we've written all of the columns that we will write.
        if (key_idx != UINT8_MAX)
        {
            assert(keys != NULL && key_idx < key_count);
            mdcursor_t current_row = c;
            bool success = md_cursor_move(&current_row, written);
            assert(success);
            (void)success;
            mdcursor_t prior_row = current_row;
            if (md_cursor_move(&prior_row, -1) && !CursorNull(&prior_row))
            {
                // If we have a prior row, then we need to check if we're sorted with respect to it.
                if (!is_row_sorted_with_next_row(keys, key_count, acxt.table->table_id, prior_row, current_row))
                {
                    // If we're not sorted, then invalidate key_idx to avoid checking if we're sorted for future row writes.
                    // We won't go from unsorted to sorted.
                    acxt.table->is_sorted = false;
                    key_idx = UINT8_MAX;
                }
            }
        }

        written++;
    } while (in_length > 1 && next_row(&acxt));

    // Validate that the last row we wrote is sorted with respect to any following rows.
    if (key_idx != UINT8_MAX)
    {
        assert(keys != NULL && key_idx < key_count);
        mdcursor_t current_row = c;
        bool success = md_cursor_move(&current_row, written);
        assert(success);
        (void)success;
        mdcursor_t next_row = current_row;
        if (md_cursor_move(&next_row, 1) && !CursorEnd(&next_row))
        {
            // If we have a prior row, then we need to check if we're sorted with respect to it.
            if (!is_row_sorted_with_next_row(keys, key_count, acxt.table->table_id, current_row, next_row))
            {
                // If we're not sorted, then invalidate key_idx to avoid checking if we're sorted for future row writes.
                // We won't go from unsorted to sorted.
                acxt.table->is_sorted = false;
                key_idx = UINT8_MAX;
            }
        }
    }

    return written;
}

#ifdef DEBUG_COLUMN_SORTING
static void validate_column_is_not_key(mdtable_t const* table, col_index_t col_idx)
{
    md_key_info_t const* keys = NULL;
    uint8_t key_count = get_table_keys(table->table_id, &keys);
    for (uint8_t i = 0; i < key_count; i++)
    {
        if (keys[i].index == col_to_index(col_idx, table))
            assert(!"Sorted columns cannot be heap references");
    }
}
#endif

// Set a column value as an existing offset into a heap.
int32_t set_column_value_as_heap_offset(mdcursor_t c, col_index_t col_idx, uint32_t in_length, uint32_t* offset)
{
    if (in_length == 0)
        return 0;

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If this isn't a heap index column, then fail.
    if (!(acxt.col_details & mdtc_idx_heap))
        return -1;

    mdstream_t const* heap = get_heap_by_id(acxt.table->cxt, ExtractHeapType(acxt.col_details));
    if (heap == NULL)
        return -1;

#ifdef DEBUG_COLUMN_SORTING
    validate_column_is_not_key(acxt.table, col_idx);
#endif

    int32_t written = 0;
    do
    {
        if (!write_column_data(&acxt, offset[written]))
            return -1;
        written++;
    } while (in_length > 1 && next_row(&acxt));

    return written;
}

int32_t md_set_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, uint32_t in_length, char const* const* str)
{
    if (in_length == 0)
        return 0;

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(acxt.col_details & mdtc_hstring))
        return -1;

#ifdef DEBUG_COLUMN_SORTING
    validate_column_is_not_key(acxt.table, col_idx);
#endif

    int32_t written = 0;
    do
    {
        uint32_t heap_offset;
        heap_offset = add_to_string_heap(CursorTable(&c)->cxt, str[written]);

        if (heap_offset == 0 && str[written][0] != '\0')
            return -1;

        if (!write_column_data(&acxt, heap_offset))
            return -1;
        written++;
    } while (in_length > 1 && next_row(&acxt));

    return written;
}

int32_t md_set_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint32_t in_length, uint8_t const* const* blob, uint32_t const* blob_len)
{
    if (in_length == 0)
        return 0;

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(acxt.col_details & mdtc_hblob))
        return -1;

#ifdef DEBUG_COLUMN_SORTING
    validate_column_is_not_key(acxt.table, col_idx);
#endif

    int32_t written = 0;
    do
    {
        uint32_t heap_offset = add_to_blob_heap(CursorTable(&c)->cxt, blob[written], blob_len[written]);

        if (heap_offset == 0 && blob_len[written] != 0)
            return -1;

        if (!write_column_data(&acxt, heap_offset))
            return -1;
        written++;
    } while (in_length > 1 && next_row(&acxt));

    return written;
}

int32_t md_set_column_value_as_guid(mdcursor_t c, col_index_t col_idx, uint32_t in_length, mdguid_t const* guid)
{
    if (in_length == 0)
        return 0;

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(acxt.col_details & mdtc_hguid))
        return -1;

#ifdef DEBUG_COLUMN_SORTING
    validate_column_is_not_key(acxt.table, col_idx);
#endif

    int32_t written = 0;
    do
    {
        uint32_t index = add_to_guid_heap(CursorTable(&c)->cxt, guid[written]);

        if (index == 0 && memcmp(&guid[written], &empty_guid, sizeof(mdguid_t)) != 0)
            return -1;

        if (!write_column_data(&acxt, index))
            return -1;
        written++;
    } while (in_length > 1 && next_row(&acxt));

    return written;
}

int32_t md_set_column_value_as_userstring(mdcursor_t c, col_index_t col_idx, uint32_t in_length, char16_t const* const* userstring)
{
    if (in_length == 0)
        return 0;

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, in_length, true, &acxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(acxt.col_details & mdtc_hblob))
        return -1;

#ifdef DEBUG_COLUMN_SORTING
    validate_column_is_not_key(acxt.table, col_idx);
#endif

    int32_t written = 0;
    do
    {
        uint32_t index = add_to_user_string_heap(CursorTable(&c)->cxt, userstring[written]);

        if (index == 0 && userstring[written][0] != 0)
            return -1;

        if (!write_column_data(&acxt, index))
            return -1;
        written++;
    } while (in_length > 1 && next_row(&acxt));

    return written;
}

int32_t update_shifted_row_references(mdcursor_t* c, uint32_t count, uint8_t col_index, mdtable_id_t updated_table, uint32_t original_starting_table_index, uint32_t new_starting_table_index)
{
    assert(c != NULL);
    col_index_t col = index_to_col(col_index, CursorTable(c)->table_id);

    // If this isn't an table or coded index column, then fail.
    if (!(CursorTable(c)->column_details[col_index] & (mdtc_idx_table | mdtc_idx_coded)))
        return -1;

    int32_t diff = (int32_t)(new_starting_table_index - original_starting_table_index);

    for (uint32_t i = 0; i < count; i++, md_cursor_next(c))
    {
        mdToken tk;
        if (1 != md_get_column_value_as_token(*c, col, 1, &tk))
            return -1;

        if ((mdtable_id_t)ExtractTokenType(tk) == updated_table)
        {
            uint32_t rid = RidFromToken(tk);
            if (rid >= original_starting_table_index)
            {
                rid += diff;
                tk = TokenFromRid(rid, CreateTokenType(updated_table));
                if (1 != md_set_column_value_as_token(*c, col, 1, &tk))
                    return -1;
            }
        }
    }

    return count;
}

static bool col_points_to_list(mdcursor_t* c, col_index_t col_index)
{
    assert(c != NULL);

    switch (CursorTable(c)->table_id)
    {
    case mdtid_TypeDef:
        return col_index == mdtTypeDef_FieldList || col_index == mdtTypeDef_MethodList;
    case mdtid_PropertyMap:
        return col_index == mdtPropertyMap_PropertyList;
    case mdtid_EventMap:
        return col_index == mdtEventMap_EventList;
    case mdtid_MethodDef:
        return col_index == mdtMethodDef_ParamList;
#ifdef DNMD_PORTABLE_PDB
    case mdtid_LocalScope:
        return col_index == mdtLocalScope_VariableList || col_index == mdtLocalScope_ConstantList;
#endif // DNMD_PORTABLE_PDB
    }
    return false;
}

static bool copy_cursor_column(mdcursor_t dest, mdcursor_t src, col_index_t idx)
{
    uint32_t column_value;
    mdtable_t* table = CursorTable(&src);
    mdtable_t* dest_table = CursorTable(&dest);
    switch (table->column_details[idx] & mdtc_categorymask)
    {
    case mdtc_constant:
        if (1 != md_get_column_value_as_constant(src, idx, 1, &column_value))
            return false;
        break;
    case mdtc_idx_coded:
    case mdtc_idx_table:
        if (1 != md_get_column_value_as_token(src, idx, 1, &column_value))
            return false;
        break;
    case mdtc_idx_heap:
        if (1 != get_column_value_as_heap_offset(src, idx, 1, &column_value))
            return false;
        break;
    default:
        assert(!"Unknown category");
        return false;
    }

    switch (dest_table->column_details[idx] & mdtc_categorymask)
    {
    case mdtc_constant:
        if (1 != md_set_column_value_as_constant(dest, idx, 1, &column_value))
            return false;
        break;
    case mdtc_idx_coded:
    case mdtc_idx_table:
        if (1 != md_set_column_value_as_token(dest, idx, 1, &column_value))
            return false;
        break;
    case mdtc_idx_heap:
        if (1 != set_column_value_as_heap_offset(dest, idx, 1, &column_value))
            return false;
        break;
    default:
        assert(!"Unknown category");
        return false;
    }
    return true;
}

static bool set_column_as_end_of_table_cursor(mdcursor_t c, col_index_t col_idx)
{
    mdtable_t* table = CursorTable(&c);
    assert((table->column_details[col_to_index(col_idx, table)] & mdtc_categorymask) == mdtc_idx_table);
    mdtable_id_t target_table = ExtractTable(table->column_details[col_to_index(col_idx, table)]);
    mdcursor_t end_of_table = create_cursor(&table->cxt->tables[target_table], table->cxt->tables[target_table].row_count + 1);

    return md_set_column_value_as_cursor(c, col_idx, 1, &end_of_table);
}

static bool initialize_list_columns(mdcursor_t c)
{
    // Initialize list columns to one-past the end of the target table.
    mdtable_t* table = CursorTable(&c);
    switch (table->table_id)
    {
    case mdtid_TypeDef:
        return set_column_as_end_of_table_cursor(c, mdtTypeDef_FieldList)
            && set_column_as_end_of_table_cursor(c, mdtTypeDef_MethodList);
        break;
    case mdtid_MethodDef:
        return set_column_as_end_of_table_cursor(c, mdtMethodDef_ParamList);
    case mdtid_PropertyMap:
        return set_column_as_end_of_table_cursor(c, mdtPropertyMap_PropertyList);
    case mdtid_EventMap:
        return set_column_as_end_of_table_cursor(c, mdtEventMap_EventList);
    default:
        break;
    }
    return true;
}

static bool insert_row_cursor_relative(mdcursor_t row, int32_t offset, mdcursor_t* new_row)
{
    mdtable_t* table = CursorTable(&row);
    if (table->cxt == NULL) // We can't turn an insert into a "create table" operation.
        return false;

    // We don't allow inserting in the middle of tables that have indirection tables.
    // Inserting into these tables should use md_add_new_row_to_parent_list instead.
    mdtable_id_t indirect_table_maybe = get_corresponding_indirection_table(table->table_id);
    if (indirect_table_maybe != mdtid_Unused)
        return false;

    // We can't insert a row before the first row of a table.
    assert(offset + (int64_t)CursorRow(&row) >= 0);

    uint32_t new_row_index = CursorRow(&row) + offset;

    if (new_row_index > table->row_count + 1)
        return false;

    if (!insert_row_into_table(table->cxt, table->table_id, new_row_index, new_row))
        return false;

    // Now that we have this row, we need to initialize the list columns to the correct values that represent a zero-length list.
    // If we've inserted a row at the end of the table, we'll initalize the columns to the end-of-table cursor.
    // If we've inserted a row in the middle of the table, we'll copy the next row's list column values.
    mdcursor_t next_row = *new_row;
    if (!md_cursor_next(&next_row) || CursorEnd(&next_row))
    {
        return initialize_list_columns(*new_row);
    }

    for (uint8_t i = 0; i < table->column_count; i++)
    {
        col_index_t col = index_to_col(i, table->table_id);
        if (col_points_to_list(&next_row, col))
        {
            if (!copy_cursor_column(*new_row, next_row, col))
                return false;
        }
    }

    return true;
}

bool md_insert_row_before(mdcursor_t row, mdcursor_t* new_row)
{
    // Inserting a row before a given cursor means that the new row will point to the same
    // target as the given cursor.
    return insert_row_cursor_relative(row, 0, new_row);
}

bool md_insert_row_after(mdcursor_t row, mdcursor_t* new_row)
{
    return insert_row_cursor_relative(row, 1, new_row);
}

// Append to the end of the table.
// The table must already exist.
static bool append_row(mdtable_t* table, mdcursor_t* new_row)
{
    assert(table->cxt != NULL);

    if (!insert_row_into_table(table->cxt, table->table_id, table->row_count + 1, new_row))
        return false;

    return initialize_list_columns(*new_row);
}

bool md_append_row(mdhandle_t handle, mdtable_id_t table_id, mdcursor_t* new_row)
{
    // We don't allow directly appending to tables that have indirection tables.
    // Inserting into these tables should use md_add_new_row_to_parent_list instead.
    mdtable_id_t indirect_table_maybe = get_corresponding_indirection_table(table_id);
    if (indirect_table_maybe != mdtid_Unused)
        return false;

    mdcxt_t* cxt = extract_mdcxt(handle);

    if (table_id < mdtid_First || table_id > mdtid_End)
        return false;

    mdtable_t* table = &cxt->tables[table_id];

    if (table->cxt == NULL)
    {
        // We should never be allocating a new indirection table through md_append_row.
        // We should be allocating it in md_add_new_row_to_parent_table when necessary.
        assert(!table_is_indirect_table(table_id));
        if (!allocate_new_table(cxt, table_id))
            return false;
    }

    return append_row(table, new_row);
}

static bool add_new_row_to_list(mdcursor_t list_owner, col_index_t list_col, mdcursor_t row_to_insert_before, mdcursor_t* new_row)
{
    assert(col_points_to_list(&list_owner, list_col));
    // Get the range of rows already in the parent's child list.
    // If we have an indirection table already, we will get back a range in the indirection table here.
    mdcursor_t range;
    uint32_t count;
    if (!md_get_column_value_as_range(list_owner, list_col, &range, &count))
        return false;
    
    // Assert that the insertion location is in our range or points to the first row of the next range.
    // For a zero-length range, row_to_insert_before will be the first row of the next range, so we need to account for that.
    assert(CursorTable(&range) == CursorTable(&row_to_insert_before));
    assert(CursorRow(&range) <= CursorRow(&row_to_insert_before) && CursorRow(&row_to_insert_before) <= CursorRow(&range) + (count == 0 ? 1 : count));

    mdcursor_t target_row;
    // If the range is in an indirection table, we'll normalize our insert to the actual target table.
    if (!md_resolve_indirect_cursor(row_to_insert_before, &target_row))
        return false;

    if (CursorTable(&row_to_insert_before) != CursorTable(&target_row))
    {
        // In this case, we resolved the indirect cursor, so we must have an indirection table.
        // We need to append to the target table and then insert a new row in the requested place into the indirection table.
        if (!append_row(CursorTable(&target_row), new_row))
            return false;

        mdcursor_t new_indirection_row;
        if (!md_insert_row_before(row_to_insert_before, &new_indirection_row))
            return false;

        if (!md_set_column_value_as_cursor(new_indirection_row, index_to_col(0, CursorTable(&row_to_insert_before)->table_id), 1, new_row))
            return false;

        if (count == 0 || CursorRow(&range) == CursorRow(&row_to_insert_before))
        {
            // If our original count was zero, then this is the first element in the list for this parent.
            // If the start of our range is the same as the row we're inserting before, then we're inserting at the start of the list.
            // In both of these cases, we need to update the parent's row column to point to the newly inserted row.
            // Otherwise, this element would be associated with the entry before the parent row.
            if (!md_set_column_value_as_cursor(list_owner, list_col, 1, &new_indirection_row))
                return false;
        }

        md_commit_row_add(new_indirection_row);
        return true;
    }
    else if (CursorEnd(&row_to_insert_before))
    {
        // In this case, we don't have an indirection table
        // and we don't need to create one as we're inserting a row at the end of the table.
        if (!append_row(CursorTable(&row_to_insert_before), new_row))
            return false;

        if (count == 0)
        {
            // If our original count was zero, then this is the first element in the list for this parent.
            // We need to update the parent's row column to point to the newly inserted row.
            // Otherwise, this element would be associated with the entry before the parent row.
            // We also need to traverse all rows before this row that have the current value of this column,
            // otherwise the list will be inconsistent.
            mdcursor_t parent_row = list_owner;
            mdcursor_t current_cursor_value;
            if (1 != md_get_column_value_as_cursor(list_owner, list_col, 1, &current_cursor_value))
               return false;

            while (md_cursor_move(&parent_row, -1))
            {
                mdcursor_t prev_cursor_value;
                if (1 != md_get_column_value_as_cursor(parent_row, list_col, 1, &prev_cursor_value))
                    return false;

                if (CursorRow(&prev_cursor_value) != CursorRow(&current_cursor_value))
                {
                    // We found the last cursor value that doesn't match the current value.
                    // Go back to it.
                    md_cursor_next(&parent_row);
                    break;
                }
            }

            for (; CursorRow(&parent_row) <= CursorRow(&list_owner); md_cursor_next(&parent_row))
            {
                if (1 != md_set_column_value_as_cursor(parent_row, list_col, 1, new_row))
                    return false;
            }
        }
        return true;
    }

    // In this case, we don't have an indirection table.
    // We need to create one since the target column is a list column.
    mdtable_t* target_table = CursorTable(&target_row);
    mdtable_id_t indirect_table = get_corresponding_indirection_table(target_table->table_id);
    assert(indirect_table != mdtid_Unused);

    if (!create_and_fill_indirect_table(target_table->cxt, target_table->table_id, indirect_table))
        return false;

    mdtcol_t* list_col_details = &CursorTable(&list_owner)->column_details[col_to_index(list_col, CursorTable(&list_owner))];
    // Clear the target column of the table index, so that we can set it to the new indirection table.
    *list_col_details = (*list_col_details & ~mdtc_timask) | InsertTable(indirect_table);

    // Now that we have created an indirection table, we can insert the row into it.
    // We need to change our "row to insert before" cursor to point at the indirection table.
    // Because we just created the indirection table, then we know that each row in the target table corresponds to the same row index
    // in the indirection table.
    row_to_insert_before = create_cursor(&target_table->cxt->tables[indirect_table], CursorRow(&range));

    // Now, we can call back into ourselves to do the actual insert.
    return add_new_row_to_list(list_owner, list_col, row_to_insert_before, new_row);
}

bool md_add_new_row_to_list(mdcursor_t list_owner, col_index_t list_col, mdcursor_t* new_row)
{
    if (!col_points_to_list(&list_owner, list_col))
       return false;

    // Get the range of rows already in the parent's child list.
    // If we have an indirection table already, we will get back a range in the indirection table here.
    mdcursor_t existing_range;
    uint32_t count;
    if (!md_get_column_value_as_range(list_owner, list_col, &existing_range, &count))
        return false;

    if (CursorTable(&existing_range)->cxt == NULL)
    {
        // If we don't have a table to add the row to, create one.
        if (!allocate_new_table(CursorTable(&list_owner)->cxt, CursorTable(&existing_range)->table_id))
            return false;

        // Now that we have a table, we recreate the "existing range" cursor as the one-past-the-end cursor
        // This allows us to use the remaining logic unchanged.
        existing_range = create_cursor(CursorTable(&existing_range), 1);
    }

    mdcursor_t row_after_range = existing_range;
    // Move the cursor just past the end of the range. We'll insert a row at the end of the range.
    if (!md_cursor_move(&row_after_range, count))
        return false;
    
    return add_new_row_to_list(list_owner, list_col, row_after_range, new_row);
}

bool md_add_new_row_to_sorted_list(mdcursor_t list_owner, col_index_t list_col, col_index_t sort_order_col, uint32_t sort_col_value, mdcursor_t* new_row)
{
    if (!col_points_to_list(&list_owner, list_col))
       return false;

    // Get the range of rows already in the parent's child list.
    // If we have an indirection table already, we will get back a range in the indirection table here.
    mdcursor_t existing_range;
    uint32_t count;
    if (!md_get_column_value_as_range(list_owner, list_col, &existing_range, &count))
        return false;

    if (CursorTable(&existing_range)->cxt == NULL)
    {
        // If we don't have a table to add the row to, create one.
        if (!allocate_new_table(CursorTable(&list_owner)->cxt, CursorTable(&existing_range)->table_id))
            return false;

        // Now that we have a table, we recreate the "existing range" cursor as the one-past-the-end cursor
        // This allows us to use the remaining logic unchanged.
        existing_range = create_cursor(CursorTable(&existing_range), 1);
    }

    mdcursor_t row_to_insert_before = existing_range;
    // Move the cursor to just past the end of the range. If we don't find a place in the middle that we need to insert the row,
    // we'll insert it here.
    if (!md_cursor_move(&row_to_insert_before, count))
        return false;

    // The existing list isn't empty, so we need to find the correct place to insert the new row.
    if (count > 0)
    {
        // In most cases we will be inserting at the end of the list,
        // so start searching there to make this a little faster.
        mdcursor_t row_to_check = row_to_insert_before;

        // Move our cursor to the last row in the list and move back one more row each iteration.
        // This can't return false as we got to row_to_insert_before by moving forward at least one row.
        for (; md_cursor_move(&row_to_check, -1) && CursorRow(&row_to_check) >= CursorRow(&existing_range);)
        {
            // If the range is in an indirection table, we need to normalize to the target table to
            // get the sort column value.
            mdcursor_t target_row;
            if (!md_resolve_indirect_cursor(row_to_check, &target_row))
                return false;
            
            uint32_t current_sort_col_value;
            if (1 != md_get_column_value_as_constant(target_row, sort_order_col, 1, &current_sort_col_value))
                return false;
            
            if (current_sort_col_value <= sort_col_value)
            {
                // row_to_check is the first row with a sort order less than or equal to the new row.
                // So we want to insert the new row after this row.
                // Set row_to_insert_before to the next row to ensure we insert the new row after this row.
                row_to_insert_before = row_to_check;
                (void)md_cursor_next(&row_to_insert_before); // We got to row_to_insert_before by moving back from an existing row, so there must be a next row.
                break;
            }
        }

        // If we didn't find a row with a sort order less than or equal to the new row, we want to insert the new row at the beginning of the list.
        // If our cursor is pointing at the first row, that means that our existing range starts at the first row.
        if (CursorRow(&row_to_check) == 1 || CursorRow(&row_to_check) < CursorRow(&existing_range))
        {
            // We didn't find a row with a sort order less than or equal to the new row.
            // So we want to insert the new row at the beginning of the list.
            row_to_insert_before = existing_range;
        }
    }

    if (!add_new_row_to_list(list_owner, list_col, row_to_insert_before, new_row))
        return false;
    
    // Now that we've added the new column to the list, set the sort order column to the provided value to
    // ensure the sort is accurate.
    if (1 != md_set_column_value_as_constant(*new_row, sort_order_col, 1, &sort_col_value))
        return false;
    
    return true;
}

bool copy_cursor(mdcursor_t dest, mdcursor_t src)
{
    mdtable_t* table = CursorTable(&src);
    assert(table->column_count == CursorTable(&dest)->column_count);

    for (uint8_t i = 0; i < table->column_count; i++)
    {
        col_index_t col = index_to_col(i, table->table_id);
        // We don't want to copy over columns that point to lists in other tables.
        // These columns have very particular behavior and are handled separately by
        // direct manipulation in the other operations.
        if (col_points_to_list(&src, col))
            continue;

        if (!copy_cursor_column(dest, src, col))
            return false;
    }

    return true;
}

static bool validate_row_sorted_within_table(mdcursor_t row)
{
    mdtable_t* table = CursorTable(&row);
    md_key_info_t const* keys;
    uint8_t count_keys = get_table_keys(table->table_id, &keys);
    assert(count_keys != 0); // We should only ever have a sorted table for a table with keys.

    mdcursor_t prior_row = row;
    if (md_cursor_move(&prior_row, -1) && !CursorNull(&prior_row))
    {
        if (!is_row_sorted_with_next_row(keys, count_keys, table->table_id, prior_row, row))
            return false;
    }
    mdcursor_t next_row = row;
    if (!md_cursor_next(&next_row) && !CursorEnd(&next_row))
    {
        if (!is_row_sorted_with_next_row(keys, count_keys, table->table_id, row, next_row))
            return false;
    }

    return true;
}

void md_commit_row_add(mdcursor_t row)
{
    mdtable_t* table = CursorTable(&row);

    // If this method is called with a zero-initialized cursor,
    // no-op. This helps make the C++ helper md_added_row_t function more easily.
    // This also allows users to call this method in all cases, even if the row-add fails.
    if (table == NULL)
        return;

    assert(table->is_adding_new_row);

    // If the table was previously sorted,
    // validate that the current row is sorted with respect to the prior and following rows.
    if (table->is_sorted)
    {
        table->is_sorted = validate_row_sorted_within_table(row);
    }

    table->is_adding_new_row = false;
}

bool sort_list_by_column(mdcursor_t parent, col_index_t list_col, col_index_t col)
{
    mdcursor_t range;
    uint32_t count;
    bool success = md_get_column_value_as_range(parent, list_col, &range, &count);
    assert(success);
    (void)success;

    // A one element range is always sorted.
    if (count == 1)
        return true;

    void* cursor_order_buffer = malloc((sizeof(mdcursor_t) + sizeof(int32_t)) * count);
    if (cursor_order_buffer == NULL)
        return false;

    mdcursor_t* correct_cursor_order = cursor_order_buffer;
    memset(correct_cursor_order, 0, sizeof(*correct_cursor_order) * count);
    int32_t* correct_cursor_order_ids = (int32_t*)(((mdcursor_t*)cursor_order_buffer) + count);

    bool need_to_update = false;
    mdcursor_t list_item = range;
    int32_t next_index = 0;
    // Gather cursors to all of the rows in the list,
    // and put them in the order specified by the column.
    for (uint32_t i = 0; i < count; i++, md_cursor_next(&list_item))
    {
        mdcursor_t target;
        if (!md_resolve_indirect_cursor(list_item, &target))
        {
            free(cursor_order_buffer);
            return false;
        }

        uint32_t sequence_number;
        if (1 != md_get_column_value_as_constant(target, col, 1, &sequence_number))
        {
            free(cursor_order_buffer);
            assert(!"Failed to read constant column from target cursor");
            return false;
        }

        assert(CursorNull(&correct_cursor_order[next_index]));
        correct_cursor_order[next_index] = target;
        correct_cursor_order_ids[next_index] = sequence_number;

        // Sequence ids need to be in ascending order to be sorted.
        if (next_index > 0
            && correct_cursor_order_ids[next_index - 1] > correct_cursor_order_ids[next_index])
        {
            // Do a simple insertion sort as we go.
            // It's unlikely we'll need to sort,
            // and even when we do, we'll likely be mostly sorted anyway.
            for (uint32_t j = next_index - 1; j >= 0; --j)
            {
                if (correct_cursor_order_ids[j] > correct_cursor_order_ids[j + 1])
                {
                    memmove(&correct_cursor_order[j], &correct_cursor_order[j + 1], sizeof(mdcursor_t) * (next_index - j));
                    memmove(&correct_cursor_order_ids[j], &correct_cursor_order_ids[j + 1], sizeof(int32_t) * (next_index - j));
                    correct_cursor_order[j] = target;
                    correct_cursor_order_ids[j] = sequence_number;
                }
            }
            need_to_update = true;
        }

        ++next_index;
    }

    // If we are already sorted, we're done.
    if (!need_to_update)
    {
        free(cursor_order_buffer);
        return true;
    }

    // If we don't have an indirection table, we need to create one now.
    mdtable_t* table = CursorTable(&range);
    if (!table_is_indirect_table(table->table_id))
    {
        mdtable_id_t indirect_table = get_corresponding_indirection_table(table->table_id);
        assert(indirect_table != mdtid_Unused);

        if (!create_and_fill_indirect_table(table->cxt, table->table_id, indirect_table))
        {
            free(cursor_order_buffer);
            assert(!"Failed to create indirection table");
            return false;
        }

        mdtcol_t* list_col_details = &CursorTable(&parent)->column_details[col_to_index(list_col, table)];
        // Clear the target column of the table index, so that we can set it to the new indirection table.
        *list_col_details = (*list_col_details & ~mdtc_timask) | InsertTable(indirect_table);

        // Now update c to point to the same row in the new indirection table.
        range = create_cursor(&table->cxt->tables[indirect_table], CursorRow(&range));
    }

    col_index_t indirect_col = index_to_col(0, CursorTable(&range)->table_id);

    if (next_index != md_set_column_value_as_cursor(range, indirect_col, (uint32_t)next_index, correct_cursor_order))
    {
        free(cursor_order_buffer);
        return false;
    }

    free(cursor_order_buffer);
    return true;
}