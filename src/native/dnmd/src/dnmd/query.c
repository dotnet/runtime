#include "internal.h"

mdcursor_t create_cursor(mdtable_t* table, uint32_t row)
{
    assert(table != NULL && row <= (table->row_count + 1));
    mdcursor_t c;
    c._reserved1 = (intptr_t)table;
    c._reserved2 = row;
    return c;
}

static mdtable_t* type_to_table(mdcxt_t* cxt, mdtable_id_t table_id)
{
    assert(cxt != NULL);
    assert(0 <= table_id && table_id < MDTABLE_MAX_COUNT);
    return &cxt->tables[table_id];
}

bool md_create_cursor(mdhandle_t handle, mdtable_id_t table_id, mdcursor_t* cursor, uint32_t* count)
{
    if (count == NULL)
        return false;

    // Set the token to the first row.
    // If the table is empty, the call will return false.
    if (!md_token_to_cursor(handle, (CreateTokenType(table_id) | 1), cursor))
        return false;

    *count = CursorTable(cursor)->row_count;
    return true;
}

static bool cursor_move_no_checks(mdcursor_t* c, int32_t delta)
{
    assert(c != NULL);

    mdtable_t* table = CursorTable(c);
    uint32_t row = CursorRow(c);
    row += delta;
    // Indices into tables begin at 1 - see II.22.
    // They can also point to index n+1, which
    // indicates the end.
    if (row == 0 || row > (table->row_count + 1))
        return false;

    *c = create_cursor(table, row);
    return true;
}

bool md_cursor_move(mdcursor_t* c, int32_t delta)
{
    if (c == NULL || CursorTable(c) == NULL)
        return false;
    return cursor_move_no_checks(c, delta);
}

bool md_cursor_next(mdcursor_t* c)
{
    return md_cursor_move(c, 1);
}

bool md_token_to_cursor(mdhandle_t handle, mdToken tk, mdcursor_t* c)
{
    assert(c != NULL);

    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    mdtable_id_t table_id = ExtractTokenType(tk);
    if (table_id >= MDTABLE_MAX_COUNT)
        return false;

    mdtable_t* table = type_to_table(cxt, table_id);

    // Indices into tables begin at 1 - see II.22.
    uint32_t row = RidFromToken(tk);
    if (row == 0 || row > table->row_count)
        return false;

    *c = create_cursor(table, row);
    return true;
}

bool md_cursor_to_token(mdcursor_t c, mdToken* tk)
{
    assert(tk != NULL);

    mdtable_t* table = CursorTable(&c);
    if (table == NULL)
    {
        *tk = mdTokenNil;
        return true;
    }

    mdToken row = RidFromToken(CursorRow(&c));
    *tk = CreateTokenType(table->table_id) | row;
    // We'll allow getting a token for a cursor just passed the end of the table.
    // These tokens are used in some scenarios to represent an empty list.
    return (row <= table->row_count + 1);
}

mdhandle_t md_extract_handle_from_cursor(mdcursor_t c)
{
    mdtable_t* table = CursorTable(&c);
    if (table == NULL)
        return NULL;
    return table->cxt;
}

bool md_walk_user_string_heap(mdhandle_t handle, mduserstringcursor_t* cursor, mduserstring_t* str, uint32_t* offset)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return -1;

    assert(cursor != NULL);
    *offset = (uint32_t)*cursor;
    size_t next_offset;
    if (!try_get_user_string(cxt, *offset, str, &next_offset))
        return false;

    *cursor = (mduserstringcursor_t)next_offset;
    return true;
}

static int32_t get_column_value_as_token_or_cursor(mdcursor_t* c, uint32_t col_idx, uint32_t out_length, mdToken* tk, mdcursor_t* cursor)
{
    assert(c != NULL && out_length != 0 && (tk != NULL || cursor != NULL));

    access_cxt_t acxt;
    if (!create_access_context(c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't an index column, then fail.
    if (!(acxt.col_details & (mdtc_idx_table | mdtc_idx_coded)))
        return -1;

    uint32_t table_row;
    mdtable_id_t table_id;

    uint32_t raw;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &raw))
            return -1;

        if (acxt.col_details & mdtc_idx_table)
        {
            // The raw value is the row index into the table that
            // is embedded in the column details.
            table_row = RidFromToken(raw);
            table_id = ExtractTable(acxt.col_details);
        }
        else
        {
            assert(acxt.col_details & mdtc_idx_coded);
            if (!decompose_coded_index(raw, acxt.col_details, &table_id, &table_row))
                return -1;
        }

        if (0 > table_id || table_id >= MDTABLE_MAX_COUNT)
            return -1;

        mdtable_t* table;
        if (tk != NULL)
        {
            tk[read_in] = CreateTokenType(table_id) | table_row;
        }
        else
        {
            // Returning a cursor means pointing directly into a table
            // so we must validate the cursor is valid prior to creation.
            table = type_to_table(acxt.table->cxt, table_id);

            // Indices into tables begin at 1 - see II.22.
            // However, tables can contain a row ID of 0 to
            // indicate "none" or point 1 past the end.
            if (table_row > table->row_count + 1)
                return -1;

            // Sometimes we can get an index into a table of 0 or 1 past the end
            // of a table that does not exist. In that case, our table object here
            // will be completely uninitialized. Set the table id so we can do operations
            // that need a table id, like creating the table or getting a token.
            if (table->table_id == 0 && table_id != 0)
            {
                assert(table_row == 0 || table_row == 1);
                table->table_id = table_id;
            }

            assert(cursor != NULL);
            cursor[read_in] = create_cursor(table, table_row);
        }
        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

int32_t md_get_column_value_as_token(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdToken* tk)
{
    if (out_length == 0)
        return 0;
    assert(tk != NULL);
    return get_column_value_as_token_or_cursor(&c, col_idx, out_length, tk, NULL);
}

int32_t md_get_column_value_as_cursor(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdcursor_t* cursor)
{
    if (out_length == 0)
        return 0;
    assert(cursor != NULL);
    return get_column_value_as_token_or_cursor(&c, col_idx, out_length, NULL, cursor);
}

// Forward declaration
//#define DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT
#ifdef DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT
static bool _validate_md_find_token_of_range_element(mdcursor_t expected, mdcursor_t begin, uint32_t count);
#endif // DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT

bool md_get_column_value_as_range(mdcursor_t c, col_index_t col_idx, mdcursor_t* cursor, uint32_t* count)
{
    assert(cursor != NULL);
    if (1 != get_column_value_as_token_or_cursor(&c, col_idx, 1, NULL, cursor))
        return false;

    // Check if the cursor is null or the end of the table
    if (CursorNull(cursor) || CursorEnd(cursor))
    {
        *count = 0;
        return true;
    }

    mdcursor_t nextMaybe = c;
    // Loop here as we may have a bunch of null cursors in the column.
    for (;;)
    {
        // The cursor into the current table remains valid,
        // we can safely move it at least one beyond the last.
        (void)cursor_move_no_checks(&nextMaybe, 1);

        // Check if we are at the end of the current table. If so,
        // ECMA states we use the remaining rows in the target table.
        if (CursorEnd(&nextMaybe))
        {
            // Add +1 for inclusive count.
            *count = CursorTable(cursor)->row_count - CursorRow(cursor) + 1;
        }
        else
        {
            // Examine the current table's next row value to find the
            // extrema of the target table range.
            mdcursor_t end;
            if (1 != md_get_column_value_as_cursor(nextMaybe, col_idx, 1, &end))
                return false;

            // The next row is a null cursor, which means we need to
            // check the next row in the current table.
            if (CursorNull(&end))
                continue;
            *count = CursorRow(&end) - CursorRow(cursor);
        }
        break;
    }

    // Use the results of this function to validate md_find_token_of_range_element()
#ifdef DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT
    (void)_validate_md_find_token_of_range_element(c, *cursor, *count);
#endif // DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT

    return true;
}

int32_t md_get_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint32_t* constant)
{
    if (out_length == 0)
        return 0;
    assert(constant != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(acxt.col_details & mdtc_constant))
        return -1;

    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &constant[read_in]))
            return -1;
        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

// Set a column value as an existing offset into a heap.
int32_t get_column_value_as_heap_offset(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint32_t* offset)
{
    if (out_length == 0)
        return 0;
    assert(offset != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't a heap index column, then fail.
    if (!(acxt.col_details & mdtc_idx_heap))
        return -1;

    mdstream_t const* heap = get_heap_by_id(acxt.table->cxt, ExtractHeapType(acxt.col_details));
    if (heap == NULL)
        return -1;

#ifdef DEBUG_COLUMN_SORTING
    validate_column_not_sorted(acxt.table, col_idx);
#endif

    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &offset[read_in]))
            return -1;

        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

int32_t md_get_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, uint32_t out_length, char const** str)
{
    if (out_length == 0)
        return 0;
    assert(str != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't a #String column, then fail.
    if (!(acxt.col_details & mdtc_hstring))
        return -1;

    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &offset))
            return -1;
        if (!try_get_string(acxt.table->cxt, offset, &str[read_in]))
            return -1;
        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

int32_t md_get_column_value_as_userstring(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mduserstring_t* strings)
{
    if (out_length == 0)
        return 0;
    assert(strings != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't a #US column, then fail.
    if (!(acxt.col_details & mdtc_hus))
        return -1;

    size_t unused;
    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &offset))
            return -1;
        if (!try_get_user_string(acxt.table->cxt, offset, &strings[read_in], &unused))
            return -1;
        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

int32_t md_get_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint8_t const** blob, uint32_t* blob_len)
{
    if (out_length == 0)
        return 0;
    assert(blob != NULL && blob_len != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't a #Blob column, then fail.
    if (!(acxt.col_details & mdtc_hblob))
        return -1;

    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &offset))
            return -1;
        if (!try_get_blob(acxt.table->cxt, offset, &blob[read_in], &blob_len[read_in]))
            return -1;
        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

int32_t md_get_column_value_as_guid(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mdguid_t* guid)
{
    if (out_length == 0)
        return 0;
    assert(guid != NULL);

    access_cxt_t acxt;
    if (!create_access_context(&c, col_idx, out_length, false, &acxt))
        return -1;

    // If this isn't a #GUID column, then fail.
    if (!(acxt.col_details & mdtc_hguid))
        return -1;

    uint32_t idx;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&acxt, &idx))
            return -1;
        if (!try_get_guid(acxt.table->cxt, idx, &guid[read_in]))
            return -1;
        read_in++;
    } while (out_length > 1 && next_row(&acxt));

    return read_in;
}

bool md_get_column_values_raw(mdcursor_t c, uint32_t values_length, bool* values_to_get, uint32_t* values_raw)
{
    if (values_length > 0
        && (values_to_get == NULL
            || values_raw == NULL))
    {
        return false;
    }

    access_cxt_t acxt;
    for (uint32_t i = 0; i < values_length; ++i)
    {
        if (!values_to_get[i])
            continue;

        // Create access context for the next column value
        if (!create_access_context(&c, i, 1, false, &acxt))
            return false;

        if (!read_column_data(&acxt, &values_raw[i]))
            return false;
    }

    return true;
}

typedef struct find_cxt__
{
    uint32_t col_offset;
    uint32_t data_len;
    mdtcol_t col_details;
} find_cxt_t;

static bool create_find_context(mdtable_t* table, col_index_t col_idx, find_cxt_t* fcxt)
{
    assert(table != NULL && fcxt != NULL);

    uint8_t idx = col_to_index(col_idx, table);
    assert(idx < MDTABLE_MAX_COLUMN_COUNT);
    if (idx >= table->column_count)
        return false;

    mdtcol_t cd = table->column_details[idx];
    fcxt->col_offset = ExtractOffset(cd);
    fcxt->data_len = (cd & mdtc_b2) ? 2 : 4;
    fcxt->col_details = cd;
    return true;
}

static int32_t col_compare_2bytes(void const* key, void const* row, void* cxt)
{
    assert(key != NULL && row != NULL && cxt != NULL);

    find_cxt_t* fcxt = (find_cxt_t*)cxt;
    uint8_t const* col_data = (uint8_t const*)row + fcxt->col_offset;

    uint16_t const lhs = *(uint16_t*)key;
    uint16_t rhs = 0;
    size_t col_len = fcxt->data_len;
    assert(col_len == 2);
    bool success = read_u16(&col_data, &col_len, &rhs);
    assert(success && col_len == 0);
    (void)success;

    return (lhs == rhs) ? 0
        : (lhs < rhs) ? -1
        : 1;
}

static int32_t col_compare_4bytes(void const* key, void const* row, void* cxt)
{
    assert(key != NULL && row != NULL && cxt != NULL);

    find_cxt_t* fcxt = (find_cxt_t*)cxt;
    uint8_t const* col_data = (uint8_t const*)row + fcxt->col_offset;

    uint32_t const lhs = *(uint32_t*)key;
    uint32_t rhs = 0;
    size_t col_len = fcxt->data_len;
    assert(col_len == 4);
    bool success = read_u32(&col_data, &col_len, &rhs);
    assert(success && col_len == 0);
    (void)success;

    return (lhs == rhs) ? 0
        : (lhs < rhs) ? -1
        : 1;
}

typedef int32_t(*md_bcompare_t)(void const* key, void const* row, void*);

// Define all 2 and 4 byte search functions
#define SEARCH_COMPARE(...) col_compare_2bytes(__VA_ARGS__)
#define SEARCH_FUNC_NAME(n) n ## _2bytes
#include "search.template.h"

#define SEARCH_COMPARE(...) col_compare_4bytes(__VA_ARGS__)
#define SEARCH_FUNC_NAME(n) n ## _4bytes
#include "search.template.h"

static void const* cursor_to_row_bytes(mdcursor_t* c)
{
    assert(c != NULL && (CursorRow(c) > 0));
    // Indices into tables begin at 1 - see II.22.
    return &CursorTable(c)->data.ptr[(CursorRow(c) - 1) * CursorTable(c)->row_size_bytes];
}

static bool find_row_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t* value, mdcursor_t* cursor)
{
    mdtable_t* table = CursorTable(&begin);
    if (table == NULL || cursor == NULL)
        return false;

    uint32_t first_row = CursorRow(&begin);
    // Indices into tables begin at 1 - see II.22.
    if (first_row == 0 || first_row > table->row_count)
        return false;

    find_cxt_t fcxt;
    if (!create_find_context(table, idx, &fcxt))
        return false;

    // If the value is for a coded index, update the value.
    if (fcxt.col_details & mdtc_idx_coded)
    {
        if (!compose_coded_index(*value, fcxt.col_details, value))
            return false;
    }

    // Compute the starting row.
    void const* starting_row = cursor_to_row_bytes(&begin);
    // Add +1 for inclusive count - use binary search if sorted, otherwise linear.
    void const* row_maybe = (table->is_sorted && !table->is_adding_new_row)
        ? ((fcxt.data_len == 2)
            ? md_bsearch_2bytes(value, starting_row, (table->row_count - first_row) + 1, table->row_size_bytes, &fcxt)
            : md_bsearch_4bytes(value, starting_row, (table->row_count - first_row) + 1, table->row_size_bytes, &fcxt))
        : ((fcxt.data_len == 2)
            ? md_lsearch_2bytes(value, starting_row, (table->row_count - first_row) + 1, table->row_size_bytes, &fcxt)
            : md_lsearch_4bytes(value, starting_row, (table->row_count - first_row) + 1, table->row_size_bytes, &fcxt));
    if (row_maybe == NULL)
        return false;

    // Compute the found row.
    // Indices into tables begin at 1 - see II.22.
    assert(starting_row <= row_maybe);
    uint32_t row = (uint32_t)(((intptr_t)row_maybe - (intptr_t)starting_row) / table->row_size_bytes) + 1;
    if (row > table->row_count)
        return false;

    *cursor = create_cursor(table, row);
    return true;
}

bool md_find_row_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* cursor)
{
    return find_row_from_cursor(begin, idx, &value, cursor);
}

md_range_result_t md_find_range_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* start, uint32_t* count)
{
    // If the table isn't sorted, then a range isn't possible.
    mdtable_t* table = CursorTable(&begin);

    if (!table->is_sorted || table->is_adding_new_row)
        return MD_RANGE_NOT_SUPPORTED;

    md_key_info_t const* keys;
    uint8_t keys_count = get_table_keys(table->table_id, &keys);
    if (keys_count == 0)
        return MD_RANGE_NOT_SUPPORTED;

    if (keys[0].index != col_to_index(idx, table))
        return MD_RANGE_NOT_SUPPORTED;

    // Currently all tables have ascending primary keys.
    // The algorithm below only works with ascending keys.
    assert(!keys[0].descending);

    // Look for any instance of the value.
    mdcursor_t found;
    if (!find_row_from_cursor(begin, idx, &value, &found))
        return MD_RANGE_NOT_FOUND;

    int32_t res;
    find_cxt_t fcxt;
    // This was already created and validated when the row was found.
    // We assume the data is still valid.
    (void)create_find_context(table, idx, &fcxt);
    md_bcompare_t cmp_func = fcxt.data_len == 2 ? col_compare_2bytes : col_compare_4bytes;

    // A valid value was found, so we are at least within the range.
    // Now find the extrema.
    *start = found;
    while (cursor_move_no_checks(start, -1))
    {
        // Since we are moving backwards in a sorted column,
        // the value should match or be greater.
        res = cmp_func(&value, cursor_to_row_bytes(start), &fcxt);
        assert(res >= 0);
        if (res > 0)
        {
            // Move forward to the start.
            (void)cursor_move_no_checks(start, 1);
            break;
        }
    }

    mdcursor_t end = found;
    while (cursor_move_no_checks(&end, 1) && !CursorEnd(&end))
    {
        // Since we are moving forwards in a sorted column,
        // the value should match or be less.
        res = cmp_func(&value, cursor_to_row_bytes(&end), &fcxt);
        assert(res <= 0);
        if (res < 0)
            break;
    }

    // Compute the row delta
    *count = CursorRow(&end) - CursorRow(start);
    return MD_RANGE_FOUND;
}

#ifdef DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT
// This function is used to validate the mapping logic between
// md_find_token_of_range_element() and md_get_column_value_as_range().
static bool _validate_md_find_token_of_range_element(mdcursor_t expected, mdcursor_t begin, uint32_t count)
{
#define IF_FALSE_RETURN(exp) { if (!(exp)) assert(false && #exp); return false; }
    mdToken expected_tk = 0;

    // The expected token is often just where the cursor presently points.
    // The Event and Property tables need to be queryed for the expected value.
    switch (CursorTable(&begin)->table_id)
    {
    case mdtid_Field:
    case mdtid_MethodDef:
    case mdtid_Param:
#ifdef DNMD_PORTABLE_PDB
    case mdtid_LocalVariable:
    case mdtid_LocalConstant:
#endif // DNMD_PORTABLE_PDB
        IF_FALSE_RETURN(md_cursor_to_token(expected, &expected_tk));
        break;
    case mdtid_Event:
        IF_FALSE_RETURN(md_get_column_value_as_token(expected, mdtEventMap_Parent, 1, &expected_tk));
        break;
    case mdtid_Property:
        IF_FALSE_RETURN(md_get_column_value_as_token(expected, mdtPropertyMap_Parent, 1, &expected_tk));
        break;
    default:
        IF_FALSE_RETURN(!"Invalid table ID");
        break;
    }

    mdToken actual;
    mdcursor_t curr = begin;
    for (uint32_t i = 0; i < count; ++i)
    {
        IF_FALSE_RETURN(md_find_token_of_range_element(curr, &actual));
        IF_FALSE_RETURN(expected_tk == actual);
        IF_FALSE_RETURN(md_cursor_next(&curr));
    }
#undef IF_FALSE_RETURN

    return true;
}
#endif // DNMD_DEBUG_FIND_TOKEN_OF_RANGE_ELEMENT

static bool find_range_element(mdcursor_t element, mdcursor_t* tgt_cursor)
{
    assert(tgt_cursor != NULL);
    mdtable_t* table = CursorTable(&element);
    if (table == NULL)
        return false;

    uint32_t row = CursorRow(&element);
    mdtable_id_t tgt_table_id;
    col_index_t tgt_col;
    switch (table->table_id)
    {
    case mdtid_Field:
        tgt_table_id = mdtid_TypeDef;
        tgt_col = mdtTypeDef_FieldList;
        break;
    case mdtid_MethodDef:
        tgt_table_id = mdtid_TypeDef;
        tgt_col = mdtTypeDef_MethodList;
        break;
    case mdtid_Param:
        tgt_table_id = mdtid_MethodDef;
        tgt_col = mdtMethodDef_ParamList;
        break;
    case mdtid_Event:
        tgt_table_id = mdtid_EventMap;
        tgt_col = mdtEventMap_EventList;
        break;
    case mdtid_Property:
        tgt_table_id = mdtid_PropertyMap;
        tgt_col = mdtPropertyMap_PropertyList;
        break;
#ifdef DNMD_PORTABLE_PDB
    case mdtid_LocalVariable:
        tgt_table_id = mdtid_LocalScope;
        tgt_col = mdtLocalScope_VariableList;
        break;
    case mdtid_LocalConstant:
        tgt_table_id = mdtid_LocalScope;
        tgt_col = mdtLocalScope_ConstantList;
        break;
#endif // DNMD_PORTABLE_PDB
    default:
        return false;
    }

    mdtable_t* tgt_table = type_to_table(table->cxt, tgt_table_id);

    uint8_t col_index = col_to_index(tgt_col, tgt_table);

    assert((tgt_table->column_details[col_index] & mdtc_idx_table) == mdtc_idx_table);

    // If the column in the target table is not pointing to the starting table,
    // then it is pointing to the corresponding indirection table.
    // We need to find the element in the indirection table that points to the cursor
    // and then find the element in the target table that points to the indirection table.
    if (table_is_indirect_table(ExtractTable(tgt_table->column_details[col_index])))
    {
        mdtable_id_t indir_table_id = ExtractTable(tgt_table->column_details[col_index]);
        col_index_t indir_col = index_to_col(0, indir_table_id);
        mdcursor_t indir_table_cursor;
        uint32_t indir_table_row_count;
        if (!md_create_cursor(table->cxt, indir_table_id, &indir_table_cursor, &indir_table_row_count))
            return false;

        mdcursor_t indir_row;
        if (!find_row_from_cursor(indir_table_cursor, indir_col, &row, &indir_row))
            return false;

        // Now that we've found the indirection cell, we can look in the target table for the
        // element that contains the indirection cell in its range.
        row = CursorRow(&indir_row);
    }

    find_cxt_t fcxt;
    if (!create_find_context(tgt_table, tgt_col, &fcxt))
        return false;

    uint32_t found_row;
    int32_t last_cmp = (fcxt.data_len == 2)
        ? mdtable_bsearch_closest_2bytes(&row, tgt_table, &fcxt, &found_row)
        : mdtable_bsearch_closest_4bytes(&row, tgt_table, &fcxt, &found_row);

    // The three result cases are handled as follows.
    // If last < 0, then the cursor is greater than the value so we must move back one.
    // If last == 0, then the cursor matches the value. This could be the first
    //    instance of the value in a run of rows. We are only interested in the
    //    last row with this value.
    // If last > 0, then the cursor is less than the value and begins the list, use it.
    mdcursor_t pos;
    mdcursor_t tmp;
    mdToken tmp_tk;
    if (last_cmp < 0)
    {
        pos = create_cursor(tgt_table, found_row - 1);
    }
    else if (last_cmp == 0)
    {
        tmp = create_cursor(tgt_table, found_row);
        tmp_tk = row;
        do
        {
            pos = tmp;
            if (!cursor_move_no_checks(&tmp, 1)
                || 1 != md_get_column_value_as_token(tmp, tgt_col, 1, &tmp_tk))
            {
                break;
            }
        }
        while (RidFromToken(tmp_tk) == row);
    }
    else
    {
        pos = create_cursor(tgt_table, found_row);
    }

    switch (table->table_id)
    {
    case mdtid_Field:
    case mdtid_MethodDef:
    case mdtid_Param:
#ifdef DNMD_PORTABLE_PDB
    case mdtid_LocalVariable:
    case mdtid_LocalConstant:
#endif // DNMD_PORTABLE_PDB
        *tgt_cursor = pos;
        return true;
    case mdtid_Event:
        return md_get_column_value_as_cursor(pos, mdtEventMap_Parent, 1, tgt_cursor);
    case mdtid_Property:
        return md_get_column_value_as_cursor(pos, mdtPropertyMap_Parent, 1, tgt_cursor);
    default:
        assert(!"Invalid table ID");
        return false;
    }
}

bool md_find_token_of_range_element(mdcursor_t element, mdToken* tk)
{
    if (tk == NULL)
        return false;
    mdcursor_t cursor;
    if (!find_range_element(element, &cursor))
        return false;
    return md_cursor_to_token(cursor, tk);
}

bool md_find_cursor_of_range_element(mdcursor_t element, mdcursor_t* cursor)
{
    if (cursor == NULL)
        return false;
    return find_range_element(element, cursor);
}

bool md_resolve_indirect_cursor(mdcursor_t c, mdcursor_t* target)
{
    mdtable_t* table = CursorTable(&c);
    if (table == NULL)
        return false;

    if (!table_is_indirect_table(table->table_id))
    {
        // If the table isn't an indirect table,
        // we don't need to resolve an indirection from the cursor.
        // In this case, the original cursor is the target cursor.
        *target = c;
        return true;
    }
    col_index_t col_idx = index_to_col(0, table->table_id);

    // If the cursor points to the end of the indirection table (just after the last row),
    // then we'll manually resolve it to the end of the target table.
    if (CursorEnd(&c))
    {
        mdtable_id_t tgt_table_id = ExtractTable(table->column_details[col_idx]);
        mdtable_t* tgt_table = type_to_table(table->cxt, tgt_table_id);
        *target = create_cursor(tgt_table, tgt_table->row_count + 1);
        return true;
    }

    return 1 == md_get_column_value_as_cursor(c, col_idx, 1, target);
}
