#include "dnmd.h"
#include "internal.h"

typedef struct mdtable_editor__
{
    mddata_t data; // If non-null, points to allocated data for the table.
    mdtable_t* table; // The read-only table that corresponds to this editor.
} mdtable_editor_t;

typedef struct md_heap_editor__
{
    mddata_t heap; // If non-null, points to allocated data for the heap.
    mdstream_t* stream; // The read-only stream that corresponds to this editor.
} md_heap_editor_t;

typedef struct mdeditor__
{
    mdcxt_t* cxt; // Non-null is indication of complete initialization

    // Metadata heaps - II.24.2.2
    md_heap_editor_t strings_heap;
    md_heap_editor_t guid_heap;
    md_heap_editor_t blob_heap;
    md_heap_editor_t user_string_heap;
    md_heap_editor_t pdb_heap;

    // Metadata tables - II.22
    mdtable_editor_t* tables;
} mdeditor_t;

static mdeditor_t* get_editor(mdcxt_t* cxt)
{
    if (cxt->editor != NULL)
        return cxt->editor;

    assert(cxt->editor == NULL);
    // If we haven't edited yet, initialize the table editor.
    size_t editor_mem = align_to(sizeof(mdeditor_t), sizeof(void*));
    size_t table_editor_mem = MDTABLE_MAX_COUNT * align_to(sizeof(mdtable_editor_t), sizeof(void*));

    size_t total_mem = editor_mem + table_editor_mem;
    uint8_t* mem = (uint8_t*)alloc_mdmem(cxt, total_mem);
    if (mem == NULL)
        return NULL;

    // Zero out the memory
    memset(mem, 0, total_mem);

    // Configure the editor object
    mdeditor_t* editor = (mdeditor_t*)mem;
    // Point the read-only view of the heaps at the image heaps.
    editor->strings_heap.stream = &cxt->strings_heap;
    editor->guid_heap.stream = &cxt->guid_heap;
    editor->blob_heap.stream = &cxt->blob_heap;
    editor->user_string_heap.stream = &cxt->user_string_heap;
#ifdef DNMD_PORTABLE_PDB
    editor->pdb_heap.stream = &cxt->pdb;
#endif // DNMD_PORTABLE_PDB

    mem += editor_mem;
    editor->tables = (mdtable_editor_t*)mem;

    // Update each table editor to point to its table view.
    for (mdtable_id_t id = mdtid_First; id < mdtid_End; ++id)
    {
        editor->tables[id].table = &cxt->tables[id];
    }

    // Connect the editor and context.
    editor->cxt = cxt;
    cxt->editor = editor;
    return editor;
}

bool create_and_fill_indirect_table(mdcxt_t* cxt, mdtable_id_t original_table, mdtable_id_t indirect_table)
{
    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return false;

    // We should only call into here if we don't already have an indirection table.
    mdtable_t* target_table = editor->tables[indirect_table].table;
    assert(target_table->cxt == NULL);

    if (!initialize_new_table_details(cxt, indirect_table, target_table))
        return false;

    target_table->cxt = editor->cxt;
    // Assert that the indirection table has exactly one column that points back at the original table.
    // The width can be either a short or wide column.
    assert(target_table->column_count == 1 && (target_table->column_details[0] & ~mdtc_widthmask) == (InsertTable(original_table) | mdtc_idx_table));

    // If we're allocating an indirection table, then we're about to add new rows to the original table.
    // Allocate more space than we need for the rows we're copying over to be able to handle adding new rows.
    size_t allocation_space = target_table->row_size_bytes * editor->tables[original_table].table->row_count * 2;
    void* mem = alloc_mdmem(editor->cxt, allocation_space);
    if (mem == NULL)
        return false;

    editor->tables[indirect_table].data.ptr = mem;
    editor->tables[indirect_table].data.size = allocation_space;
    target_table->data.ptr = editor->tables[indirect_table].data.ptr;
    target_table->data.size = target_table->row_size_bytes * editor->tables[original_table].table->row_count;

    // The indirection table will initially have each row pointing to the matching row in the original table.
    uint8_t* table_data = editor->tables[indirect_table].data.ptr;
    size_t table_len = editor->tables[indirect_table].data.size;
    for (uint32_t i = 0; i < editor->tables[original_table].table->row_count; i++)
    {
        if (target_table->column_details[0] & mdtc_b2)
        {
            assert(i + 1 <= UINT16_MAX);
            write_u16(&table_data, &table_len, (uint16_t)i + 1);
        }
        else
            write_u32(&table_data, &table_len, i + 1);
    }

    target_table->row_count = editor->tables[original_table].table->row_count;
    target_table->cxt = editor->cxt;
    return true;
}

uint8_t* get_writable_table_data(mdtable_t* table, bool make_writable)
{
    mdeditor_t* editor = get_editor(table->cxt);
    if (editor == NULL)
        return NULL;

    mddata_t* table_data = &editor->tables[table->table_id].data;

    if (table_data->ptr == NULL && make_writable)
    {
        // If we're trying to get writable data for a table that has not been edited,
        // then we need to allocate space for it and copy the contents for editing.
        // TODO: Should we allocate more space than the table currently uses to ensure
        // immediate table growth doesn't require a realloc?
        void* mem = alloc_mdmem(table->cxt, table->data.size);
        if (mem == NULL)
            return NULL;
        table_data->ptr = mem;
        if (table_data->ptr == NULL)
            return NULL;
        table_data->size = table->data.size;
        memcpy(table_data->ptr, table->data.ptr, table->data.size);
        table->data.ptr = table_data->ptr;
    }

    return table_data->ptr;
}

// Copy a row from one table to another.
// The rows must have an identical number of columns and the columns must have the same definition other than column width.
// This function does not ensure that the destination table is still sorted after the copy, so this should only be used in cases
// where a table will keep the same sort order after the copy (such as resizing a table).
static bool copy_row(uint8_t** dest, size_t* dest_len, mdtcol_t const* dest_cols, uint8_t const** src, size_t* src_len, mdtcol_t const* src_cols, uint8_t num_cols)
{
    for (uint8_t col_index = 0; col_index < num_cols; col_index++)
    {
        // The source and destination column details can only differ by storage width.
        assert((src_cols[col_index] & ~mdtc_widthmask) == (dest_cols[col_index] & ~mdtc_widthmask));

        uint32_t data = 0;

        if (src_cols[col_index] & mdtc_b2)
        {
            if (!read_u16(src, src_len, (uint16_t*)&data))
                return false;
        }
        else
        {
            if (!read_u32(src, src_len, &data))
                return false;
        }

        if (dest_cols[col_index] & mdtc_b2)
        {
            if (!write_u16(dest, dest_len, (uint16_t)data))
                return false;
        }
        else
        {
            if (!write_u32(dest, dest_len, data))
                return false;
        }
    }

    return true;
}

static bool set_column_size_for_max_row_count(mdeditor_t* editor, mdtable_t* table, mdtable_id_t updated_table, mdtcol_t updated_heap, uint32_t new_max_row_count)
{
    assert(table->column_count <= MDTABLE_MAX_COLUMN_COUNT);
    assert((mdtid_First <= updated_table && updated_table <= mdtid_End) || (updated_table == mdtid_Unused && (ExtractHeapType(updated_heap) != 0)));
    mdtcol_t new_column_details[MDTABLE_MAX_COLUMN_COUNT];

    uint32_t initial_row_count;
    if (updated_table != mdtid_Unused)
    {
        initial_row_count = editor->tables[updated_table].table->row_count;
    }
    else if (updated_heap == mdtc_hguid)
    {
        mdstream_t* stream = get_heap_by_id(table->cxt, updated_heap);
        initial_row_count = (uint32_t)(stream->size / sizeof(mdguid_t));
    }
    else
    {
        initial_row_count = (uint32_t)get_heap_by_id(table->cxt, updated_heap)->size;
        // If we are resizing a heap, we'll ensure that the flag on the context for large heaps is consistent.
        // This makes saving easier by requiring minimal reprocessing of the heaps at save time.
        mdcxt_flag_t large_heap_flag = get_large_heap_flag(updated_heap);
        if (large_heap_flag != 0)
        {
            if ((editor->cxt->context_flags & large_heap_flag) == large_heap_flag
                && new_max_row_count <= UINT16_MAX)
            {
                editor->cxt->context_flags &= ~large_heap_flag;
            }
            else if ((editor->cxt->context_flags & large_heap_flag) == 0
                && new_max_row_count > UINT16_MAX)
            {
                editor->cxt->context_flags |= large_heap_flag;
            }
        }
    }

    for (uint8_t col_index = 0; col_index < table->column_count; col_index++)
    {
        mdtcol_t col_details = table->column_details[col_index];
        new_column_details[col_index] = col_details;

        uint32_t initial_max_column_value, new_max_column_value;
        if ((col_details & mdtc_idx_table) == mdtc_idx_table && ExtractTable(col_details) == updated_table)
        {
            initial_max_column_value = initial_row_count;
            new_max_column_value = new_max_row_count;
        }
        else if ((col_details & mdtc_idx_coded) == mdtc_idx_coded && updated_table != mdtid_Unused && is_coded_index_target(col_details, updated_table))
        {
            bool composed = compose_coded_index(TokenFromRid(initial_row_count, CreateTokenType(updated_table)), col_details, &initial_max_column_value);
            assert(composed);
            (void)composed;
            composed = compose_coded_index(TokenFromRid(new_max_row_count, CreateTokenType(updated_table)), col_details, &new_max_column_value);
            assert(composed);
            (void)composed;
        }
        else if ((col_details & (mdtc_idx_heap)) == mdtc_idx_heap && ExtractHeapType(col_details) == updated_heap)
        {
            initial_max_column_value = initial_row_count;
            new_max_column_value = new_max_row_count;
        }
        else
        {
            continue;
        }

        if ((col_details & mdtc_b2) && new_max_column_value > UINT16_MAX)
        {
            new_column_details[col_index] = (col_details & ~mdtc_b2) | mdtc_b4;
        }
        else if ((col_details & mdtc_b4) && new_max_column_value <= UINT16_MAX)
        {
            new_column_details[col_index] = (col_details & ~mdtc_b4) | mdtc_b2;
        }
    }

    // We want to make sure that we can store as many rows as the current table can in our new allocation.
    size_t table_data_size = editor->tables[table->table_id].data.ptr != NULL ? editor->tables[table->table_id].data.size : table->data.size;
    size_t max_original_rows_in_size = table_data_size / table->row_size_bytes;

    uint8_t new_row_size = 0;
    for (uint8_t col_index = 0; col_index < table->column_count; col_index++)
    {
        new_row_size += (new_column_details[col_index] & mdtc_b2) == mdtc_b2 ? 2 : 4;
    }

    // If the row size is the same, then we didn't have to change any column sizes.
    // We are either only expanding or reducing, so we can't end up at the same size with changed column sizes.
    if (new_row_size == table->row_size_bytes)
        return true;

    size_t new_allocation_size = max_original_rows_in_size * new_row_size;

    void* mem = alloc_mdmem(editor->cxt, new_allocation_size);
    if (mem == NULL)
        return false;
    uint8_t* new_data_blob = mem;

    // Go through all of the columns of each row and copy them to the new memory for the table
    // in their correct size.
    uint8_t const* table_data = table->data.ptr;
    size_t table_data_length = table->data.size;
    uint8_t* new_table_data = new_data_blob;
    size_t new_table_data_length = new_allocation_size;
    for (uint32_t i = 0; i < table->row_count; i++)
    {
        if (!copy_row(&new_table_data, &new_table_data_length, new_column_details, &table_data, &table_data_length, table->column_details, table->column_count))
            return false;
    }

    // Update the public view of the table to have the new schema and point to the new data.
    table->row_size_bytes = new_row_size;
    table->data.ptr = new_data_blob;
    table->data.size = (size_t)table->row_count * table->row_size_bytes;
    memcpy(table->column_details, new_column_details, sizeof(mdtcol_t) * table->column_count);

    // Update the table's corresponding editor to point to the newly-allocated memory,
    // and free the previous allocation if necessary.
    if (editor->tables[table->table_id].data.ptr != NULL)
        free_mdmem(editor->cxt, editor->tables[table->table_id].data.ptr);

    editor->tables[table->table_id].data.ptr = new_data_blob;
    editor->tables[table->table_id].data.size = new_allocation_size;

    return true;
}

static bool update_table_references_for_shifted_rows(mdeditor_t* editor, mdtable_id_t updated_table, uint32_t changed_row_start, int64_t shift)
{
    assert(updated_table != mdtid_Unused);
    // Make sure we aren't shifting into negative row ids or shifting above the max row id. That isn't legal.
    assert(changed_row_start + shift > 0 && changed_row_start + shift < 0x00ffffff);
    for (mdtable_id_t table_id = mdtid_First; table_id < mdtid_End; table_id++)
    {
        mdtable_t* table = &editor->cxt->tables[table_id];
        if (table->cxt == NULL) // This table is not used in the current image
            continue;

        // Update all columns in the table that can refer to the updated table
        // to be the correct width for the updated table's new size.
        if (!set_column_size_for_max_row_count(editor, table, updated_table, mdtc_none, (uint32_t)(table->row_count + shift)))
            return false;

        for (uint8_t i = 0; i < table->column_count; i++)
        {
            mdtcol_t col_details = table->column_details[i];
            if (((col_details & mdtc_idx_table) == mdtc_idx_table && ExtractTable(col_details) == updated_table)
                || ((col_details & mdtc_idx_coded) == mdtc_idx_coded && is_coded_index_target(col_details, updated_table)))
            {
                // We've found a column that will need updating.
                mdcursor_t c = create_cursor(table, 1);
                update_shifted_row_references(&c, table->row_count, i, updated_table, changed_row_start, (uint32_t)(changed_row_start + shift));
            }
        }
    }
    return true;
}

static bool allocate_more_editable_space(mdcxt_t* cxt, mddata_t* editable_data, mdcdata_t* data, size_t minimum_size)
{
    size_t new_size = minimum_size > data->size * 2 ? minimum_size : data->size * 2;
    void* new_ptr;
    if (editable_data->ptr != NULL)
    {
        void* mem = alloc_mdmem(cxt, new_size);
        if (mem == NULL)
            return false;
        memcpy(mem, data->ptr, data->size);
        new_ptr = mem;
        free_mdmem(cxt, editable_data->ptr);
    }
    else
    {
        void* mem = alloc_mdmem(cxt, new_size);
        if (mem == NULL)
            return false;
        new_ptr = mem;
        memcpy(new_ptr, data->ptr, data->size);
    }

    if (new_ptr == NULL)
        return false;
    editable_data->ptr = new_ptr;
    editable_data->size = new_size;
    data->ptr = new_ptr;
    // We don't update data->size as the space has been allocated, but it is not in use in the image yet.
    return true;
}

bool allocate_new_table(mdcxt_t* cxt, mdtable_id_t table_id)
{
    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return false;

    mdtable_t* table = &cxt->tables[table_id];
    // We should not be allocating for a newly-initialized table.
    assert(table->cxt == NULL);

    if (!initialize_new_table_details(cxt, table_id, table))
        return false;

    table->cxt = cxt;
    // Allocate some memory for the table.
    // The number of rows in this allocation is arbitrary.
    // It may be interesting to change the default depending on the target table.
    size_t initial_allocation_size = table->row_size_bytes * 20;
    // The initial table has a size 0 as it has no rows.
    table->data.size = 0;
    uint8_t* table_data = alloc_mdmem(cxt, initial_allocation_size);
    if (table_data == NULL)
    {
        table->cxt = NULL;
        return false;
    }
    table->data.ptr = cxt->editor->tables[table_id].data.ptr = table_data;
    cxt->editor->tables[table_id].data.size = initial_allocation_size;
    return true;
}

bool insert_row_into_table(mdcxt_t* cxt, mdtable_id_t table_id, uint32_t row_index, mdcursor_t* new_row)
{
    assert(row_index != 0); // Row indexes are 1-based.
    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return false;

    mdtable_editor_t* target_table_editor = &editor->tables[table_id];
    assert(target_table_editor->table->cxt != NULL); // The table should exist in the image before a row is added to it.

    // We do not support adding multiple rows to a table at once. One row must be fully added before another is added.
    if (target_table_editor->table->is_adding_new_row)
        return false;

    // We can either insert a row in the middle of a table or directly after the end of the table.
    if (target_table_editor->table->row_count < (row_index - 1))
        return false;

    // If we are out of space in our table, then we need to allocate a new table buffer.
    if (target_table_editor->data.ptr == NULL || target_table_editor->data.size < target_table_editor->table->row_size_bytes * (size_t)(target_table_editor->table->row_count + 1))
    {
        if (!allocate_more_editable_space(editor->cxt, &target_table_editor->data, &target_table_editor->table->data, (target_table_editor->table->row_count + 1) * target_table_editor->table->row_size_bytes))
            return false;
    }

    size_t next_row_start_offset = target_table_editor->table->row_size_bytes * (size_t)(row_index - 1);
    size_t last_row_end_offset = target_table_editor->table->row_size_bytes * (size_t)target_table_editor->table->row_count;

    if (next_row_start_offset < last_row_end_offset)
    {
        // If we're inserting a row in the middle of the table, then we need to move the rows after it down.
        memmove(
            target_table_editor->data.ptr + next_row_start_offset + target_table_editor->table->row_size_bytes,
            target_table_editor->data.ptr + next_row_start_offset,
            last_row_end_offset - next_row_start_offset);

    }
    // Clear the new row.
    memset(target_table_editor->data.ptr + next_row_start_offset, 0, target_table_editor->table->row_size_bytes);

    // Update table references
    // We may have columns that are pointing to the row just after the end of the table, so we need to do this in all cases,
    // not just the "in the middle" case.
    if (!update_table_references_for_shifted_rows(editor, table_id, row_index, 1))
        return false;

    target_table_editor->table->data.size += target_table_editor->table->row_size_bytes;
    target_table_editor->table->row_count++;
    target_table_editor->table->is_adding_new_row = true;

    *new_row = create_cursor(target_table_editor->table, row_index);
    return true;
}

#ifdef DNMD_PORTABLE_PDB
bool update_referenced_type_system_table_row_count(mdcxt_t* cxt, mdtable_id_t updated_table, uint32_t new_max_row_count)
{
    assert(updated_table < mdtid_FirstPdb);

    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return false;

    md_pdb_t pdb;
    if (!try_get_pdb(cxt, &pdb))
        return false;

    if (pdb.type_system_table_rows[updated_table] >= new_max_row_count)
        return true;

    pdb.type_system_table_rows[updated_table] = new_max_row_count;

    for (mdtable_id_t table_id = mdtid_FirstPdb; table_id < mdtid_End; ++table_id)
    {
        mdtable_t* table = &editor->cxt->tables[table_id];
        if (table->cxt == NULL) // This table is not used in the current image
            continue;

        // Update all columns in the table that can refer to the updated table
        // to be the correct width for the updated table's new size.
        if (!set_column_size_for_max_row_count(editor, table, updated_table, mdtc_none, new_max_row_count))
            return false;
    }
    
    size_t pdb_heap_size = cxt->pdb.size;
    if (!(pdb.referenced_type_system_tables & (1ULL << updated_table)))
    {
        // If we haven't referenced this type system table yet, then we need to allocate space for the row count
        // and mark that we're referencing it now.
        pdb_heap_size += sizeof(uint32_t);
        pdb.referenced_type_system_tables |= (1ULL << updated_table);
    }

    if (editor->pdb_heap.heap.ptr == NULL || pdb_heap_size < editor->pdb_heap.heap.size)
    {
        // If we don't have space for the new row count or we haven't edited the PDB heap yet, then we need to allocate more space.
        if (!allocate_more_editable_space(editor->cxt, &editor->pdb_heap.heap, editor->pdb_heap.stream, pdb_heap_size))
            return false;
    }

    uint8_t* pdb_heap_data = editor->pdb_heap.heap.ptr;
    size_t pdb_heap_data_length = editor->pdb_heap.heap.size;
    // We can skip over the PDB ID and the entrypoint token.
    if (!advance_output_stream(&pdb_heap_data, &pdb_heap_data_length, ARRAY_SIZE(pdb.pdb_id) + sizeof(mdToken)))
        return false;
    
    // Write the bitset of referenced type system tables.
    if (!write_u64(&pdb_heap_data, &pdb_heap_data_length, pdb.referenced_type_system_tables))
        return false;
    
    // Now write the row counts for each referenced type system table.
    size_t n = count_set_bits(pdb.referenced_type_system_tables);
    uint8_t const* pdb_end = pdb_heap_data + (n * sizeof(uint32_t));

    // Read in all row data defined by the references bits.
    for (size_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        if (pdb.referenced_type_system_tables & (1ULL << i))
        {
            // Read in the row count for referenced tables
            if (!write_u32(&pdb_heap_data, &pdb_heap_data_length, pdb.type_system_table_rows[i]))
                return false;
        }
    }

    // Validate we wrote the row counts properly
    if (pdb_heap_data != pdb_end)
        return false;
    return true;
}
#endif // DNMD_PORTABLE_PDB

static md_heap_editor_t* get_heap_editor_by_id(mdeditor_t* editor, mdtcol_t heap_id)
{
    switch (heap_id)
    {
        case mdtc_hblob:
            return &editor->blob_heap;
        case mdtc_hguid:
            return &editor->guid_heap;
        case mdtc_hstring:
            return &editor->strings_heap;
        case mdtc_hus:
            return &editor->user_string_heap;
        default:
            return NULL;
    }
}

static bool reserve_heap_space(mdeditor_t* editor, uint32_t space_size, mdtcol_t heap_id, bool preserve_offsets, uint32_t* heap_offset)
{
    md_heap_editor_t* heap_editor = get_heap_editor_by_id(editor, heap_id);
    if (heap_editor == NULL)
        return false;

    if (heap_editor->stream->ptr == NULL)
    {
        // Set the default heap size based on likely reasonable sizes for the heaps.
        // In most images, there won't be more than three guids, so we can start with a small heap in that case.
        size_t const initial_heap_size = heap_id == mdtc_hguid ? sizeof(mdguid_t) * 3 : 0x100;
        void* mem = alloc_mdmem(editor->cxt, initial_heap_size);
        if (mem == NULL)
            return false;

        heap_editor->stream->ptr = mem;
        heap_editor->heap.ptr = mem;
        heap_editor->heap.size = initial_heap_size;

        // The first character in the strings heap must be the '\0' - II.24.2.3
        // The first character in the user_string and blob heaps must be the 0 - II.24.2.4
        // The guid heap doesn't start with a 0 byte, but it must be emitted in sizeof(mdguid_t)-based chuncks.
        // If we are preserving offsets, then we don't initialize the heap, as we will be copying an existing heap that has already been validated and
        // we must have the exact same offsets as the existing heap to avoid breaking heap references.
        if (heap_id != mdtc_hguid && !preserve_offsets)
        {
            heap_editor->heap.ptr[0] = 0;
            heap_editor->stream->size = heap_id == mdtc_hguid ? 0 : 1;
        }
    }

    *heap_offset = (uint32_t)heap_editor->stream->size;

    if (*heap_offset > UINT32_MAX - space_size)
    {
        // The max heap size is 2^32-1, so we don't have space left to allocate.
        return false;
    }


    uint32_t new_heap_size = *heap_offset + space_size;
    if (new_heap_size > heap_editor->heap.size)
    {
        if (!allocate_more_editable_space(editor->cxt, &heap_editor->heap, heap_editor->stream, new_heap_size))
            return false;
    }

    // Update heap references in case the additional used space crosses the boundary for index sizes.
    uint32_t index_scale = (heap_id == mdtc_hguid ? sizeof(mdguid_t) : 1);
    assert(heap_editor->stream->size % index_scale == 0);
    for (mdtable_id_t i = mdtid_First; i < mdtid_End; i++)
    {
        mdtable_t* table = &editor->cxt->tables[i];
        if (table->cxt == NULL) // This table is not used in the current image
            continue;

        // Update all columns in the table that can refer to the updated heap
        // to be the correct width for the updated heap's new size.
        if (!set_column_size_for_max_row_count(editor, table, mdtid_Unused, heap_id, new_heap_size / index_scale))
            return false;
    }

    // Now that the new heap size can be referenced, let's update the heap size.
    heap_editor->stream->size += space_size;

    return true;
}

uint32_t add_to_string_heap(mdcxt_t* cxt, char const* str)
{
    // II.24.2.3 - When the #String heap is present, the first entry is always the empty string (i.e., \0). 
    // II.24.2.2 -  Streams need not be there if they are empty.
    // We can avoid allocating the heap if the only entry is the empty string.
    // Columns that point to the string heap can be 0 if there is no #String heap.
    // In that case, they represent the empty string.
    if (str[0] == '\0')
        return 0;

    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return 0;

    // TODO: Deduplicate heap
    uint32_t str_len = (uint32_t)strlen(str);
    uint32_t heap_offset;
    if (!reserve_heap_space(editor, str_len + 1, mdtc_hstring, false, &heap_offset))
    {
        return 0;
    }
    memcpy((uint8_t*)editor->strings_heap.heap.ptr + heap_offset, str, str_len);
    ((uint8_t*)editor->strings_heap.heap.ptr)[heap_offset + str_len] = '\0';
    return heap_offset;
}

uint32_t add_to_blob_heap(mdcxt_t* cxt, uint8_t const* data, uint32_t length)
{
    // II.24.2.4 - When the #Blob heap is present, the first entry is always the empty blob. 
    // II.24.2.2 -  Streams need not be there if they are empty.
    // We can avoid allocating the heap if the only entry is the empty blob.
    // Columns that point to the blob heap can be 0 if there is no #Blob heap.
    // In that case, they represent the empty blob.
    if (length == 0)
        return 0;

    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return 0;

    // TODO: Deduplicate heap
    uint8_t compressed_length[4];
    size_t compressed_length_size = ARRAY_SIZE(compressed_length);
    if (!compress_u32(length, compressed_length, &compressed_length_size))
        return 0;

    uint32_t heap_slot_size = length + (uint32_t)compressed_length_size;

    uint32_t heap_offset;
    if (!reserve_heap_space(editor, heap_slot_size, mdtc_hblob, false, &heap_offset))
    {
        return 0;
    }

    memcpy(editor->blob_heap.heap.ptr + heap_offset, compressed_length, compressed_length_size);
    memcpy(editor->blob_heap.heap.ptr + heap_offset + compressed_length_size, data, length);
    return heap_offset;
}

uint32_t add_to_user_string_heap(mdcxt_t* cxt, char16_t const* str)
{
    uint32_t str_len;
    uint8_t has_special_char = 0;
    for (str_len = 0; str[str_len] != (char16_t)0; str_len++)
    {
        char16_t c = str[str_len];
        // II.24.2.4
        // There is an additional terminal byte which holds a 1 or 0.
        // The 1 signifies Unicode characters that require handling beyond
        // that normally provided for 8-bit encoding sets.
        //  This final byte holds the value 1 if and only if any UTF16 character
        // within the string has any bit set in its top byte,
        // or its low byte is any of the following: 0x01-0x08, 0x0E–0x1F, 0x27, 0x2D, 0x7F.
        // Otherwise, it holds 0.
        if ((c & 0x80) != 0)
        {
            has_special_char = 1;
        }
        else if (c >= 0x1 && c <= 0x8)
        {
            has_special_char = 1;
        }
        else if (c >= 0xe && c <= 0x1f)
        {
            has_special_char = 1;
        }
        else if (c == 0x27)
        {
            has_special_char = 1;
        }
        else if (c == 0x2d)
        {
            has_special_char = 1;
        }
        else if (c == 0x7f)
        {
            has_special_char = 1;
        }
    }

    // II.24.2.4 - When the #US heap is present, the first entry is always the empty blob. 
    // II.24.2.2 -  Streams need not be there if they are empty.
    // We can avoid allocating the heap if the only entry is the empty blob.
    // Indices into the #US heap can be 0 if there is no #US heap.
    // In that case, they represent the empty userstring blob.
    if (str_len == 0)
        return 0;

    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return 0;

    // TODO: Deduplicate heap
    
    // II.24.2.4
    // Strings in the #US (user string) heap are encoded using 16-bit Unicode encodings.
    // The count on each string is the number of bytes (not characters) in the string.
    // Furthermore, there is an additional terminal byte (so all byte counts are odd, not even).
    size_t us_blob_bytes = str_len * sizeof(char16_t) + 1;
    
    // The string is too long to represent in the heap.
    if (us_blob_bytes > INT32_MAX)
        return 0;
    uint8_t compressed_length[sizeof(uint32_t)];
    size_t compressed_length_size = ARRAY_SIZE(compressed_length);
    if (!compress_u32((uint32_t)us_blob_bytes, compressed_length, &compressed_length_size))
        return 0;

    uint32_t heap_slot_size = (uint32_t)us_blob_bytes + (uint32_t)compressed_length_size;
    uint32_t heap_offset;
    if (!reserve_heap_space(editor, heap_slot_size, mdtc_hus, false, &heap_offset))
    {
        return 0;
    }

    // Copy the compressed blob length into the heap.
    memcpy(editor->user_string_heap.heap.ptr + heap_offset, compressed_length, compressed_length_size);
    // Copy the UTF-16-encoded user string into the heap.
    memcpy(editor->user_string_heap.heap.ptr + heap_offset + compressed_length_size, str, us_blob_bytes - 1);

    // Set the trailing byte.
    editor->user_string_heap.heap.ptr[heap_offset + compressed_length_size + us_blob_bytes - 1] = has_special_char;
    return heap_offset;
}

mdguid_t const empty_guid = { 0 };

uint32_t add_to_guid_heap(mdcxt_t* cxt, mdguid_t guid)
{
    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return 0;
    // TODO: Deduplicate heap
    if (memcmp(&guid, &empty_guid, sizeof(mdguid_t)) == 0)
        return 0;

    uint32_t heap_offset;
    if (!reserve_heap_space(editor, sizeof(mdguid_t), mdtc_hguid, false, &heap_offset))
    {
        return 0;
    }

    memcpy(editor->guid_heap.heap.ptr + heap_offset, &guid, sizeof(mdguid_t));
    // II.22 -  The Guid heap is an array of GUIDs, each 16 bytes wide.  Its 
    //    first element is numbered 1, its second 2, and so on. 
    // So, we need to make the offset 1-based and at the scale of the GUID size.
    return (heap_offset / sizeof(mdguid_t)) + 1;
}

bool append_heap(mdcxt_t* cxt, mdcxt_t* delta, mdtcol_t heap_id)
{
    bool is_minimal_delta = (delta->context_flags & mdc_minimal_delta) == mdc_minimal_delta;
    mdeditor_t* editor = get_editor(cxt);
    if (editor == NULL)
        return false;

    md_heap_editor_t* heap_editor = get_heap_editor_by_id(editor, heap_id);
    mdstream_t* delta_heap = get_heap_by_id(delta, heap_id);
    if (delta_heap->size == 0)
        return true;

    size_t copy_offset;
    size_t delta_size;
    if (is_minimal_delta && heap_id != mdtc_hguid)
    {
        // If the delta image is a minimal delta and the heap is not the GUID heap, we do a full copy from the delta image.
        copy_offset = 0;
        delta_size = delta_heap->size;
    }
    else
    {
        // Otherwise, we only do a partial copy from the stream starting at the end of the existing heap.
        copy_offset = heap_editor->stream->size;
        delta_size = delta_heap->size - copy_offset;
    }

    if (delta_size > UINT32_MAX)
    {
        return false;
    }

    uint32_t heap_offset;
    if (!reserve_heap_space(editor, (uint32_t)delta_size, heap_id, true, &heap_offset))
    {
        return false;
    }

    memcpy(heap_editor->heap.ptr + heap_offset, delta_heap->ptr + copy_offset, delta_size);

    return true;
}

mduserstringcursor_t md_add_userstring_to_heap(mdhandle_t handle, char16_t const* userstring)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return 0;

    return add_to_user_string_heap(cxt, userstring);
}
