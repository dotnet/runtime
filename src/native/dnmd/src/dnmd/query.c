#include "internal.h"

#define CreateTokenType(tk) (mdToken)(((uint32_t)tk << 24) & 0xff000000)
#define ExtractTokenType(tk) ((tk >> 24) & 0xff)

#define CreateTokenIndex(i) (mdToken)(i & 0x00ffffff)
#define ExtractTokenIndex(i) (uint32_t)(i & 0x00ffffff)

static mdtable_t* CursorTable(mdcursor_t* c)
{
    assert(c != NULL);
    return (mdtable_t*)c->_reserved1;
}

static uint32_t CursorRow(mdcursor_t* c)
{
    assert(c != NULL);
    return ExtractTokenIndex(c->_reserved2);
}

static bool CursorNull(mdcursor_t* c)
{
    return CursorRow(c) == 0;
}

static bool CursorEnd(mdcursor_t* c)
{
    return (CursorTable(c)->row_count + 1) == CursorRow(c);
}

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
    if (0 > table_id || table_id >= MDTABLE_MAX_COUNT)
        return NULL;
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
    assert(c != NULL && delta != 0);

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

    if (delta == 0)
        return true;

    return cursor_move_no_checks(c, delta);
}

bool md_cursor_next(mdcursor_t* c)
{
    return md_cursor_move(c, 1);
}

bool md_token_to_cursor(mdhandle_t handle, mdToken tk, mdcursor_t* c)
{
    if (c == NULL)
        return false;

    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    mdtable_t* table = type_to_table(cxt, ExtractTokenType(tk));
    if (table == NULL)
        return false;

    // Indices into tables begin at 1 - see II.22.
    uint32_t row = ExtractTokenIndex(tk);
    if (row == 0 || row > table->row_count)
        return false;

    *c = create_cursor(table, row);
    return true;
}

bool md_cursor_to_token(mdcursor_t c, mdToken* tk)
{
    if (tk == NULL)
        return false;

    mdtable_t* table = CursorTable(&c);
    if (table == NULL)
    {
        *tk = mdTokenNil;
        return true;
    }

    mdToken row = CreateTokenIndex(CursorRow(&c));
    if (row > table->row_count)
        return false;

    *tk = CreateTokenType(table->table_id) | row;
    return true;
}

int32_t md_walk_user_string_heap(mdhandle_t handle, mduserstringcursor_t* cursor, uint32_t out_length, mduserstring_t* strings, uint32_t* offsets)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return -1;

    if (cursor == NULL)
        return -1;

    uint32_t offset = (uint32_t)*cursor;
    size_t next_offset;
    uint32_t read_in = 0;
    for (uint32_t i = 0; i < out_length; ++i)
    {
        if (!try_get_user_string(cxt, offset, &strings[read_in], &next_offset))
            break;
        offsets[i] = offset;
        read_in++;
        offset = (uint32_t)next_offset;
    }

    *cursor = offset;
    return read_in;
}

typedef struct _query_cxt_t
{
    mdtable_t* table;
    mdtcol_t col_details;
    uint8_t const* data;
    uint8_t const* end;
    size_t data_len;
    uint32_t next_row_stride;
} query_cxt_t;

static uint32_t col_to_index(col_index_t col_idx, mdtable_t* table)
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
    return idx;
}

static bool create_query_context(mdcursor_t* cursor, col_index_t col_idx, uint32_t row_count, query_cxt_t* qcxt)
{
    assert(qcxt != NULL);
    mdtable_t* table = CursorTable(cursor);
    if (table == NULL)
        return false;

    uint32_t idx = col_to_index(col_idx, table);
    assert(idx < MDTABLE_MAX_COLUMN_COUNT);
    if (idx >= table->column_count)
        return false;

    mdtcol_t cd = table->column_details[idx];

    uint32_t offset = ExtractOffset(cd);
    uint32_t row = CursorRow(cursor);
    if (row == 0 || row > table->row_count)
        return false;

    // Metadata row indexing is 1-based.
    row--;
    qcxt->table = table;
    qcxt->col_details = cd;

    // Compute the offset into the first row.
    qcxt->data = table->data.ptr + (row * table->row_size_bytes) + offset;

    // Compute the beginning of the row after the last row.
    qcxt->end = table->data.ptr + ((row + row_count) * table->row_size_bytes);

    // Limit the data read to the width of the column
    uint32_t const data_len = (cd & mdtc_b2) ? 2 : 4;
    qcxt->data_len = data_len;

    // Compute the next row stride. Take the total length and substract
    // the data length for the column to get at the next row's column.
    qcxt->next_row_stride = table->row_size_bytes - data_len;
    return true;
}

static bool read_column_data(query_cxt_t* qcxt, uint32_t* data)
{
    assert(qcxt != NULL && data != NULL);
    *data = 0;
    return (qcxt->col_details & mdtc_b2)
        ? read_u16(&qcxt->data, &qcxt->data_len, (uint16_t*)data)
        : read_u32(&qcxt->data, &qcxt->data_len, data);
}

static bool next_row(query_cxt_t* qcxt)
{
    assert(qcxt != NULL);
    qcxt->data += qcxt->next_row_stride;
    return qcxt->data < qcxt->end;
}

static int32_t get_column_value_as_token_or_cursor(mdcursor_t* c, uint32_t col_idx, uint32_t out_length, mdToken* tk, mdcursor_t* cursor)
{
    assert(c != NULL && out_length != 0 && (tk != NULL || cursor != NULL));

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't an index column, then fail.
    if (!(qcxt.col_details & (mdtc_idx_table | mdtc_idx_coded)))
        return -1;

    size_t ci_idx;
    coded_index_entry const* ci_entry;
    uint32_t code_mask;

    uint32_t table_row;
    mdtable_id_t table_id;

    uint32_t raw;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &raw))
            return -1;

        if (qcxt.col_details & mdtc_idx_table)
        {
            assert(ExtractTable(qcxt.col_details) != 0);

            // The raw value is the row index into the table that
            // is embedded in the column details.
            table_row = CreateTokenIndex(raw);
            table_id = ExtractTable(qcxt.col_details);
        }
        else
        {
            assert(qcxt.col_details & mdtc_idx_coded);

            // Use the embedded index into the coded index map.
            ci_idx = ExtractCodedIndex(qcxt.col_details);
            if (ci_idx >= ARRAY_SIZE(coded_index_map))
                return -1;
            ci_entry = &coded_index_map[ci_idx];

            // Create a mask to extract the index into the entry.
            code_mask = (1 << ci_entry->bit_encoding_size) - 1;
            if ((raw & code_mask) >= ci_entry->lookup_len)
                return -1;
            table_id = ci_entry->lookup[raw & code_mask];

            // Remove the encoded lookup index.
            table_row = raw >> ci_entry->bit_encoding_size;
        }

        if (0 > table_id || table_id >= MDTABLE_MAX_COUNT)
            return -1;

        mdtable_t* table;
        if (tk != NULL)
        {
            tk[read_in] = CreateTokenType(table_id) | CreateTokenIndex(table_row);
        }
        else
        {
            // Returning a cursor means pointing directly into a table
            // so we must validate the cursor is valid prior to creation.
            table = &qcxt.table->cxt->tables[table_id];

            // Indices into tables begin at 1 - see II.22.
            // However, tables can contain a row ID of 0 to
            // indicate "none" or point 1 past the end.
            if (table_row > table->row_count + 1)
                return -1;

            assert(cursor != NULL);
            cursor[read_in] = create_cursor(table, table_row);
        }
        read_in++;
    } while (next_row(&qcxt));

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
            if (!md_get_column_value_as_cursor(nextMaybe, col_idx, 1, &end))
                return false;

            // The next row is a null cursor, which means we need to
            // check the next row in the current table.
            if (CursorNull(&end))
                continue;
            *count = CursorRow(&end) - CursorRow(cursor);
        }
        break;
    }
    return true;
}

int32_t md_get_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint32_t* constant)
{
    if (out_length == 0)
        return 0;
    assert(constant != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(&c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't an constant column, then fail.
    if (!(qcxt.col_details & mdtc_constant))
        return -1;

    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &constant[read_in]))
            return -1;
        read_in++;
    } while (next_row(&qcxt));

    return read_in;
}

int32_t md_get_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, uint32_t out_length, char const** str)
{
    if (out_length == 0)
        return 0;
    assert(str != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(&c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't a #String column, then fail.
    if (!(qcxt.col_details & mdtc_hstring))
        return -1;

    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &offset))
            return -1;
        if (!try_get_string(qcxt.table->cxt, offset, &str[read_in]))
            return -1;
        read_in++;
    } while (next_row(&qcxt));

    return read_in;
}

int32_t md_get_column_value_as_wchar(mdcursor_t c, col_index_t col_idx, uint32_t out_length, mduserstring_t* strings)
{
    if (out_length == 0)
        return 0;
    assert(strings != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(&c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't a #US column, then fail.
    if (!(qcxt.col_details & mdtc_hus))
        return -1;

    size_t unused;
    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &offset))
            return -1;
        if (!try_get_user_string(qcxt.table->cxt, offset, &strings[read_in], &unused))
            return -1;
        read_in++;
    } while (next_row(&qcxt));

    return read_in;
}

int32_t md_get_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint32_t out_length, uint8_t const** blob, uint32_t* blob_len)
{
    if (out_length == 0)
        return 0;
    assert(blob != NULL && blob_len != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(&c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't a #Blob column, then fail.
    if (!(qcxt.col_details & mdtc_hblob))
        return -1;

    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &offset))
            return -1;
        if (!try_get_blob(qcxt.table->cxt, offset, &blob[read_in], &blob_len[read_in]))
            return -1;
        read_in++;
    } while (next_row(&qcxt));

    return read_in;
}

int32_t md_get_column_value_as_guid(mdcursor_t c, col_index_t col_idx, uint32_t out_length, GUID* guid)
{
    if (out_length == 0)
        return 0;
    assert(guid != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(&c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't a #GUID column, then fail.
    if (!(qcxt.col_details & mdtc_hguid))
        return -1;

    uint32_t idx;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &idx))
            return -1;
        if (!try_get_guid(qcxt.table->cxt, idx, &guid[read_in]))
            return -1;
        read_in++;
    } while (next_row(&qcxt));

    return read_in;
}

typedef int32_t(*md_bcompare_t)(void const* key, void const* row, void*);

// Since MSVC doesn't have a C11 compatible bsearch_s, defining one below.
// Ideally we would use the one in the standard.
static void const* md_bsearch(
    void const* key,
    void const* base,
    rsize_t count,
    rsize_t element_size,
    md_bcompare_t cmp,
    void* cxt)
{
    assert(key != NULL && base != NULL);
    while (count > 0)
    {
        void const* row = (uint8_t const*)base + (element_size * (count / 2));
        int32_t res = cmp(key, row, cxt);
        if (res == 0)
            return row;

        if (count == 1)
        {
            break;
        }
        else if (res < 0)
        {
            count /= 2;
        }
        else
        {
            base = row;
            count -= count / 2;
        }
    }
    return NULL;
}

static void const* md_lsearch(
    void const* key,
    void const* base,
    rsize_t count,
    rsize_t element_size,
    md_bcompare_t cmp,
    void* cxt)
{
    assert(key != NULL && base != NULL);
    void const* row = base;
    for (rsize_t i = 0; i < count; ++i)
    {
        int32_t res = cmp(key, row, cxt);
        if (res == 0)
            return row;

        // Onto the next row.
        row = (uint8_t const*)row + element_size;
    }
    return NULL;
}

typedef struct _find_cxt_t
{
    uint32_t col_offset;
    uint32_t data_len;
} find_cxt_t;

static bool create_find_context(mdtable_t* table, col_index_t col_idx, find_cxt_t* fcxt)
{
    assert(table != NULL && fcxt != NULL);

    uint32_t idx = col_to_index(col_idx, table);
    assert(idx < MDTABLE_MAX_COLUMN_COUNT);
    if (idx >= table->column_count)
        return false;

    mdtcol_t cd = table->column_details[idx];
    fcxt->col_offset = ExtractOffset(cd);
    fcxt->data_len = (cd & mdtc_b2) ? 2 : 4;
    return true;
}

static int32_t col_compare(void const* key, void const* row, void* cxt)
{
    assert(key != NULL && row != NULL && cxt != NULL);

    find_cxt_t* fcxt = (find_cxt_t*)cxt;
    uint8_t const* col_data = (uint8_t const*)row + fcxt->col_offset;

    uint32_t const lhs = *(uint32_t*)key;
    uint32_t rhs = 0;
    size_t col_len = fcxt->data_len;
    bool success = (col_len == 2)
        ? read_u16(&col_data, &col_len, (uint16_t*)&rhs)
        : read_u32(&col_data, &col_len, &rhs);
    assert(success && col_len == 0);

    return (lhs == rhs) ? 0
        : (lhs < rhs) ? -1
        : 1;
}

static void const* cursor_to_row_bytes(mdcursor_t* c)
{
    assert(c != NULL && (CursorRow(c) > 0));
    // Indices into tables begin at 1 - see II.22.
    return &CursorTable(c)->data.ptr[(CursorRow(c) - 1) * CursorTable(c)->row_size_bytes];
}

bool md_find_row_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* cursor)
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

    // Compute the starting row.
    void const* starting_row = cursor_to_row_bytes(&begin);
    // Add +1 for inclusive count - use binary search if sorted, otherwise linear.
    void const* row_maybe = (table->is_sorted)
        ? md_bsearch(&value, starting_row, (table->row_count - first_row) + 1, table->row_size_bytes, col_compare, &fcxt)
        : md_lsearch(&value, starting_row, (table->row_count - first_row) + 1, table->row_size_bytes, col_compare, &fcxt);
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

bool md_find_range_from_cursor(mdcursor_t begin, col_index_t idx, uint32_t value, mdcursor_t* start, uint32_t* count)
{
    // Look for any instance of the value.
    mdcursor_t found;
    if (!md_find_row_from_cursor(begin, idx, value, &found))
        return false;

    // If the table isn't sorted, then a range isn't possible.
    mdtable_t* table = CursorTable(&begin);
    if (!table->is_sorted)
        return false;

    int32_t res;
    find_cxt_t fcxt;
    // This was already created and validated when the row was found.
    // We assume the data is still valid.
    (void)create_find_context(table, idx, &fcxt);

    // A valid value was found, so we are at least within the range.
    // Now find the extrema.
    *start = found;
    while (cursor_move_no_checks(start, -1))
    {
        // Since we are moving backwards in a sorted column,
        // the value should match or be greater.
        res = col_compare(&value, cursor_to_row_bytes(start), &fcxt);
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
        res = col_compare(&value, cursor_to_row_bytes(&end), &fcxt);
        assert(res <= 0);
        if (res < 0)
            break;
    }

    // Compute the row delta
    *count = CursorRow(&end) - CursorRow(start);
    return true;
}
