#include "internal.h"

bool create_access_context(mdcursor_t* cursor, col_index_t col_idx, bool make_writable, access_cxt_t* acxt)
{
    mdtable_t* table = CursorTable(cursor);
    if (table == NULL)
        return false;

    uint32_t row = CursorRow(cursor);
    if (row == 0 || row > table->row_count)
        return false;

    uint8_t idx = col_to_index(col_idx, table);
    assert(idx < table->column_count);

    // Metadata row indexing is 1-based.
    row--;

    mdtcol_t col = table->column_details[idx];

    uint32_t offset_to_table_data = row * table->row_size_bytes + ExtractOffset(col);
#ifndef NDEBUG
    size_t len = (col & mdtc_b2) ? 2 : 4;
    assert(offset_to_table_data + len <= table->data.size);
#endif

    acxt->table = table;
    acxt->data = table->data.ptr + offset_to_table_data;
    acxt->col_details = col;
    if (make_writable)
    {
        acxt->writable_data = get_writable_table_data(table, make_writable) + offset_to_table_data;
    }
    else
    {
        acxt->writable_data = NULL;
    }
    return true;
}

bool read_column_data(access_cxt_t* acxt, uint32_t* data)
{
    assert(acxt != NULL && acxt->data != NULL && data != NULL);

    uint8_t const* table_data = acxt->data;

    if ((acxt->col_details & mdtc_b4) == mdtc_b4)
    {
        size_t len = 4;
        return read_u32(&table_data, &len, data);
    }
    else
    {
        size_t len = 2;
        uint16_t value;
        if (!read_u16(&table_data, &len, &value))
            return false;

        *data = value;
        return true;
    }
}

bool write_column_data(access_cxt_t* acxt, uint32_t data)
{
    assert(acxt != NULL && acxt->writable_data != NULL);
    uint8_t* table_data = acxt->writable_data;
    if ((acxt->col_details & mdtc_b4) == mdtc_b4)
    {
        size_t len = 4;
        return write_u32(&table_data, &len, data);
    }
    else
    {
        size_t len = 2;
        return write_u16(&table_data, &len, (uint16_t)data);
    }
}

bool create_bulk_access_context(mdcursor_t* cursor, col_index_t col_idx, uint32_t row_count, bulk_access_cxt_t* acxt)
{
    assert(acxt != NULL);
    mdtable_t* table = CursorTable(cursor);
    if (table == NULL)
        return false;

    uint32_t row = CursorRow(cursor);
    if (row == 0 || row > table->row_count)
        return false;

    uint8_t idx = col_to_index(col_idx, table);
    assert(idx < table->column_count);

    // Metadata row indexing is 1-based.
    row--;
    acxt->table = table;
    acxt->col_details = table->column_details[idx];

    // Compute the offset into the first row.
    uint32_t offset = ExtractOffset(acxt->col_details);

    acxt->start = acxt->data = table->data.ptr + (row * table->row_size_bytes) + offset;

    // Compute the beginning of the row after the last valid row.
    uint32_t last_row = row + row_count;
    if (last_row > table->row_count)
        last_row = table->row_count;
    acxt->end = table->data.ptr + (last_row * table->row_size_bytes);

    // Limit the data read to the width of the column
    acxt->data_len_col = (acxt->col_details & mdtc_b2) ? 2 : 4;
    acxt->data_len = acxt->data_len_col;

    // Compute the next row stride. Take the total length and substract
    // the data length for the column to get at the next row's column.
    acxt->next_row_stride = table->row_size_bytes - acxt->data_len_col;
    return true;
}

bool read_column_data_and_advance(bulk_access_cxt_t* acxt, uint32_t* data)
{
    assert(acxt != NULL && data != NULL);
    *data = 0;

    if ((acxt->col_details & mdtc_b4) == mdtc_b4)
    {
        return read_u32(&acxt->data, &acxt->data_len, data);
    }
    else
    {
        uint16_t value;
        if (!read_u16(&acxt->data, &acxt->data_len, &value))
            return false;

        *data = value;
        return true;
    }
}

bool next_row(bulk_access_cxt_t* acxt)
{
    assert(acxt != NULL);
    // We will only traverse correctly if we've already read the column in this row.
    assert(acxt->data_len == 0);
    acxt->data += acxt->next_row_stride;

    // Restore the data length of the column data.
    acxt->data_len = acxt->data_len_col;
    return acxt->data < acxt->end;
}
