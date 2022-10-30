#include "internal.h"

#define CreateTokenType(tk) (mdToken)(((uint32_t)tk << 24) & 0xff000000)
#define ExtractTokenType(tk) ((tk >> 24) & 0xff)

#define CreateTokenIndex(i) (mdToken)(i & 0x00ffffff)
#define ExtractTokenIndex(i) (uint32_t)(i & 0x00ffffff)

static mdtable_t* CursorTable(mdcursor_t* c)
{
    return (mdtable_t*)c->_reserved1;
}

static uint32_t CursorRow(mdcursor_t* c)
{
    return ExtractTokenIndex(c->_reserved2);
}

mdcursor_t create_cursor(mdtable_t* table, uint32_t row)
{
    assert(table != NULL && row != 0);
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

bool md_create_cursor(mdhandle_t handle, mdtable_id_t table_id, mdcursor_t* cursor, int32_t* count)
{
    if (count == NULL)
        return false;

    // Set the token to the first row.
    // If the table is empty, the call will return false.
    if (!md_token_to_cursor(handle, (CreateTokenType(table_id) | 1), cursor))
        return false;

    *count = CursorRow(cursor);
    return true;
}

bool md_cursor_move(mdcursor_t* c, int32_t delta)
{
    if (c == NULL)
        return false;

    mdtable_t* table = CursorTable(c);
    if (table == NULL)
        return false;

    uint32_t row = CursorRow(c);
    row += delta;
    // Indices into tables begin at 1 - see II.22.
    if (row == 0 || row > table->row_count)
        return false;

    *c = create_cursor(table, row);
    return true;
}

bool md_cursor_next(mdcursor_t* c)
{
    return md_cursor_move(c, 1);
}

bool md_row_distance(mdcursor_t begin, mdcursor_t end, int32_t* distance)
{
    mdtable_t* b_table = CursorTable(&begin);
    mdtable_t* e_table = CursorTable(&end);

    // Cursors must point to same table and be non-null.
    if (b_table != e_table || b_table == NULL)
        return false;

    if (distance == NULL)
        return false;

    // Compute the relative distance between the two indices
    uint32_t e_row = CursorRow(&end);
    uint32_t b_row = CursorRow(&begin);
    if (b_row < e_row)
    {
        *distance = (int32_t)(e_row - b_row);
    }
    else
    {
        // Compute distance, then negate
        *distance = -(int32_t)(b_row - e_row);
    }

    return true;
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

typedef struct _query_cxt_t
{
    mdtable_t* table;
    mdtcol_t col_details;
    uint8_t const* data;
    uint8_t const* end;
    size_t data_len;
    uint32_t next_row_stride;
} query_cxt_t;

static bool create_query_context(mdcursor_t* cursor, col_index_t col_idx, uint32_t row_count, query_cxt_t* qcxt)
{
    assert(qcxt != NULL);
    mdtable_t* table = CursorTable(cursor);
    if (table == NULL)
        return false;

    uint32_t idx = (uint32_t)col_idx;
#ifdef DEBUG_TABLE_COLUMN_LOOKUP
    mdtable_id_t tgt_table_id = col_idx >> 8;
    if (tgt_table_id != table->table_id)
    {
        assert(!"Unexpected table/column indexing");
        return false;
    }
    idx = (col_idx & 0xff);
#endif

    assert(idx < MDTABLE_MAX_COLUMN_COUNT);
    if (idx >= table->column_count)
        return false;

    mdtcol_t cd = table->column_details[idx];

    uint32_t offset = ExtractOffset(cd);
    // Metadata row indexing is 1-based, minus 1.
    uint32_t row = CursorRow(cursor) - 1;
    assert(row < table->row_count);

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
            if (table_row == 0 || table_row > table->row_count)
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

int32_t md_get_column_value_as_wchar(mdcursor_t c, col_index_t col_idx, uint32_t out_length, WCHAR const** str, uint32_t* str_chars, uint8_t* final_byte)
{
    if (out_length == 0)
        return 0;
    assert(str != NULL && str_chars != NULL && final_byte != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(&c, col_idx, out_length, &qcxt))
        return -1;

    // If this isn't a #US column, then fail.
    if (!(qcxt.col_details & mdtc_hus))
        return -1;

    uint32_t offset;
    int32_t read_in = 0;
    do
    {
        if (!read_column_data(&qcxt, &offset))
            return -1;
        if (!try_get_user_string(qcxt.table->cxt, offset, &str[read_in], &str_chars[read_in], &final_byte[read_in]))
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
