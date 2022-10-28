#include "internal.h"

#define CreateTokenType(tk) (mdToken)(((uint32_t)tk << 24) & 0xff000000)
#define ExtractTokenType(tk) ((tk >> 24) & 0xff)

#define CreateTokenIndex(i) (mdToken)(i & 0x00ffffff)
#define ExtractTokenIndex(i) (uint32_t)(i & 0x00ffffff)

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

bool md_table_row_count(mdhandle_t handle, mdtable_id_t table_id, uint32_t* count)
{
    if (count == NULL)
        return false;

    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    mdtable_t* table = type_to_table(cxt, table_id);
    if (table == NULL)
        return false;

    *count = table->row_count;
    return true;
}

bool md_create_cursor(mdhandle_t handle, mdtable_id_t table_id, mdcursor_t* cursor)
{
    // Set the token to the first row.
    // If the table is empty, the call will return false.
    return md_token_to_cursor(handle, (CreateTokenType(table_id) | 1), cursor);
}

bool md_cursor_move(mdcursor_t* c, int32_t delta)
{
    if (c == NULL)
        return false;

    mdtable_t* table = (mdtable_t*)c->_reserved1;
    if (table == NULL)
        return false;

    uint32_t row = ExtractTokenIndex(c->_reserved2);
    row += delta;
    if (row == 0 || row > table->row_count)
        return false;

    c->_reserved2 = row;
    return true;
}

bool md_cursor_next(mdcursor_t* c)
{
    return md_cursor_move(c, 1);
}

bool md_row_distance(mdcursor_t begin, mdcursor_t end, int32_t* distance)
{
    mdtable_t* b_table = (mdtable_t*)begin._reserved1;
    mdtable_t* e_table = (mdtable_t*)end._reserved1;

    // Cursors must point to same table and be non-null.
    if (b_table != e_table || b_table == NULL)
        return false;

    if (distance == NULL)
        return false;

    // Compute the relative distance between the two indices
    uint32_t e_row = ExtractTokenIndex(end._reserved2);
    uint32_t b_row = ExtractTokenIndex(begin._reserved2);
    if (b_row < e_row)
    {
        *distance = (int32_t)(ExtractTokenIndex(e_row) - ExtractTokenIndex(b_row));
    }
    else
    {
        // Compute distance, then negate
        *distance = -(int32_t)(ExtractTokenIndex(b_row) - ExtractTokenIndex(e_row));
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

    mdtable_t* table = type_to_table(cxt, TypeFromToken(tk));
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

    mdtable_t* table = (mdtable_t*)c._reserved1;
    if (table == NULL)
    {
        *tk = mdTokenNil;
        return true;
    }

    mdToken row = CreateTokenIndex(c._reserved2);
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
    size_t data_len;
} query_cxt_t;

static bool create_query_context(mdcursor_t c, col_index_t col_idx, query_cxt_t* qcxt)
{
    assert(qcxt != NULL);
    mdtable_t* table = (mdtable_t*)c._reserved1;
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

    assert(idx <= MDTABLE_MAX_COLUMN_COUNT);
    if (idx >= table->column_count)
        return false;

    mdtcol_t cd = table->column_details[idx];

    uint32_t offset = ExtractOffset(cd);
    // Metadata row indexing is 1-based, minus 1.
    uint32_t row = ExtractTokenIndex(c._reserved2) - 1;
    assert(row < table->row_count);

    qcxt->table = table;
    qcxt->col_details = cd;
    qcxt->data = table->data.ptr + (row * table->row_size_bytes) + offset;

    // Limit the data read to the width of the column
    qcxt->data_len = (cd & mdtc_b2) ? 2 : 4;
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

static bool get_column_value_as_token_or_cursor(mdcursor_t c, uint32_t col_idx, mdToken* tk, mdcursor_t* cursor)
{
    assert(tk != NULL || cursor != NULL);

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, &qcxt))
        return false;

    // If this isn't an index column, then fail.
    if (!(qcxt.col_details & (mdtc_idx_table | mdtc_idx_coded)))
        return false;

    uint32_t raw;
    if (!read_column_data(&qcxt, &raw))
        return false;

    size_t ci_idx;
    coded_index_entry const* ci_entry;
    uint32_t code_mask;

    uint32_t table_row;
    mdtable_id_t table_id;
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
            return false;
        ci_entry = &coded_index_map[ci_idx];

        // Create a mask to extract the index into the entry.
        code_mask = (1 << ci_entry->bit_encoding_size) - 1;
        if ((raw & code_mask) >= ci_entry->lookup_len)
            return false;
        table_id = ci_entry->lookup[raw & code_mask];

        // Remove the encoded lookup index.
        table_row = raw >> ci_entry->bit_encoding_size;
    }

    if (0 > table_id || table_id >= MDTABLE_MAX_COUNT)
        return false;

    mdtable_t* table;
    if (tk != NULL)
    {
        *tk = CreateTokenType(table_id) | CreateTokenIndex(table_row);
    }
    else
    {
        // Returning a cursor means pointing directly into a table
        // so we must validate the cursor is valid prior to creation.
        table = &qcxt.table->cxt->tables[table_id];

        // Indices into tables begin at 1 - see II.22.
        if (table_row > table->row_count)
            return false;

        assert(cursor != NULL);
        cursor->_reserved1 = (intptr_t)table;
        cursor->_reserved2 = table_row;
    }

    return true;
}

bool md_get_column_value_as_token(mdcursor_t c, col_index_t col_idx, mdToken* tk)
{
    if (tk == NULL)
        return false;
    return get_column_value_as_token_or_cursor(c, col_idx, tk, NULL);
}

bool md_get_column_value_as_cursor(mdcursor_t c, col_index_t col_idx, mdcursor_t* cursor)
{
    if (cursor == NULL)
        return false;
    return get_column_value_as_token_or_cursor(c, col_idx, NULL, cursor);
}

bool md_get_column_value_as_constant(mdcursor_t c, col_index_t col_idx, uint32_t* constant)
{
    if (constant == NULL)
        return false;

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, &qcxt))
        return false;

    // If this isn't an constant column, then fail.
    if (!(qcxt.col_details & mdtc_constant))
        return false;

    return read_column_data(&qcxt, constant);
}

bool md_get_column_value_as_utf8(mdcursor_t c, col_index_t col_idx, char const** str)
{
    if (str == NULL)
        return false;

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, &qcxt))
        return false;

    // If this isn't a #String column, then fail.
    if (!(qcxt.col_details & mdtc_hstring))
        return false;

    uint32_t offset;
    if (!read_column_data(&qcxt, &offset))
        return false;

    return try_get_string(qcxt.table->cxt, offset, str);
}

bool md_get_column_value_as_wchar(mdcursor_t c, col_index_t col_idx, WCHAR const** str, uint32_t* str_chars, uint8_t* final_byte)
{
    if (str == NULL || str_chars == NULL || final_byte == NULL)
        return false;

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, &qcxt))
        return false;

    // If this isn't a #US column, then fail.
    if (!(qcxt.col_details & mdtc_hus))
        return false;

    uint32_t offset;
    if (!read_column_data(&qcxt, &offset))
        return false;

    return try_get_user_string(qcxt.table->cxt, offset, str, str_chars, final_byte);
}

bool md_get_column_value_as_blob(mdcursor_t c, col_index_t col_idx, uint8_t const** blob, uint32_t* blob_len)
{
    if (blob == NULL || blob_len == NULL)
        return false;

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, &qcxt))
        return false;

    // If this isn't a #Blob column, then fail.
    if (!(qcxt.col_details & mdtc_hblob))
        return false;

    uint32_t offset;
    if (!read_column_data(&qcxt, &offset))
        return false;

    return try_get_blob(qcxt.table->cxt, offset, blob, blob_len);
}

bool md_get_column_value_as_guid(mdcursor_t c, col_index_t col_idx, GUID* guid)
{
    if (guid == NULL)
        return false;

    query_cxt_t qcxt;
    if (!create_query_context(c, col_idx, &qcxt))
        return false;

    // If this isn't a #GUID column, then fail.
    if (!(qcxt.col_details & mdtc_hguid))
        return false;

    uint32_t idx;
    if (!read_column_data(&qcxt, &idx))
        return false;

    return try_get_guid(qcxt.table->cxt, idx, guid);
}
