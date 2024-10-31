#include "internal.h"

bool try_get_string(mdcxt_t* cxt, size_t offset, char const** str)
{
    assert(cxt != NULL && str != NULL);

    mdstream_t* h = &cxt->strings_heap;

    // II.24.2.3 - When the #String heap is present, the first entry is always the empty string (i.e., \0). 
    // II.24.2.2 -  Streams need not be there if they are empty.
    // If the offset into the heap is 0, we can treat that as a "null" index into the heap and return
    // the empty string.
    if (h->size == 0 && offset == 0)
    {
        *str = "\0"; // II.24.2.3 'The first character must be the '\0' character.
        return true;
    }

    if (h->size <= offset)
        return false;

    *str = (char const*)(h->ptr + offset);
    return true;
}

bool validate_strings_heap(mdcxt_t* cxt)
{
    assert(cxt != NULL);

    mdstream_t* h = &cxt->strings_heap;
    if (h->size == 0)
        return true;

    // The first character must be the '\0' - II.24.2.3
    if (*(char const*)h->ptr != '\0')
        return false;

    return true;
}

bool try_get_user_string(mdcxt_t* cxt, size_t offset, mduserstring_t* str, size_t* next_offset)
{
    assert(cxt != NULL && str != NULL && next_offset != NULL);
    mdstream_t* h = &cxt->user_string_heap;
    if (h->size <= offset)
        return false;

    uint8_t const* begin = (uint8_t const*)(h->ptr + offset);

    size_t data_len = h->size - offset;
    uint32_t byte_count;
    if (!decompress_u32(&begin, &data_len, &byte_count))
        return false;

    if (byte_count == 0)
    {
        memset(str, 0, sizeof(*str));
    }
    else
    {
        // II.24.2.4
        // The count on each string is the number of bytes in the string.
        // There is an additional terminal byte which holds a 1 or 0.
        // The 1 signifies Unicode characters that require handling beyond
        // that normally provided for 8-bit encoding sets.
        str->str = (char16_t const*)begin;
        str->str_bytes = byte_count;
        str->final_byte = begin[byte_count - 1];
    }

    *next_offset = &begin[byte_count] - h->ptr;
    return true;
}

bool validate_user_string_heap(mdcxt_t* cxt)
{
    assert(cxt != NULL);

    mdstream_t* h = &cxt->user_string_heap;
    if (h->size == 0)
        return true;

    // The first element must be the 0 - II.24.2.4
    if (*h->ptr != 0)
        return false;

    return true;
}

bool try_get_blob(mdcxt_t* cxt, size_t offset, uint8_t const** blob, uint32_t* blob_len)
{
    assert(cxt != NULL && blob != NULL && blob_len != NULL);

    mdstream_t* h = &cxt->blob_heap;

    if (h->size == 0 && offset == 0)
    {
        // The first element must be the 0 - II.24.2.4
        *blob = h->ptr;
        *blob_len = 0;
        return true;
    }

    if (h->size <= offset)
        return false;

    uint8_t const* ptr = (uint8_t const*)(h->ptr + offset);

    size_t data_len = h->size - offset;
    uint32_t byte_count;
    if (!decompress_u32(&ptr, &data_len, &byte_count))
        return false;

    *blob = ptr;
    *blob_len = byte_count;
    return true;
}

bool validate_blob_heap(mdcxt_t* cxt)
{
    assert(cxt != NULL);

    mdstream_t* h = &cxt->blob_heap;
    if (h->size == 0)
        return true;

    // The first element must be the 0 - II.24.2.4
    if (*h->ptr != 0)
        return false;

    return true;
}

bool try_get_guid(mdcxt_t* cxt, size_t idx, mdguid_t* guid)
{
    assert(cxt != NULL && guid != NULL);

    mdstream_t* h = &cxt->guid_heap;
    size_t count = h->size / sizeof(mdguid_t);

    if (count < idx)
        return false;

    // The guid heap starts from an index of 1 - see II.22.
    // However, since there are many zero indices, we permit
    // the value and return the "null" guid.
    if (idx == 0)
    {
        memset(guid, 0, sizeof(*guid));
        return true;
    }

    mdguid_t* guids = (mdguid_t*)h->ptr;
    *guid = guids[idx - 1];
    return true;
}

bool validate_guid_heap(mdcxt_t* cxt)
{
    assert(cxt != NULL);

    mdstream_t* h = &cxt->guid_heap;
    if (h->size % sizeof(mdguid_t) != 0)
        return false;

    return true;
}

bool initialize_tables(mdcxt_t* cxt)
{
    assert(cxt != NULL);

    mdstream_t* h = &cxt->tables_heap;
    if (h->size == 0)
        return false;

    uint8_t const* curr = h->ptr;
    size_t curr_len = h->size;

    uint8_t maj;
    uint8_t min;
    uint8_t heap_sizes;
    uint8_t reserved;
    if (!advance_stream(&curr, &curr_len, 4)
        || !read_u8(&curr, &curr_len, &maj)
        || !read_u8(&curr, &curr_len, &min)
        || !read_u8(&curr, &curr_len, &heap_sizes)
        || !read_u8(&curr, &curr_len, &reserved))
    {
        return false;
    }

    // The bottom byte of the context flags is the heap sizes
    // flags from the image.
    // We shouldn't have any of these bits set from our other
    // initialization logic.
    assert((cxt->context_flags & mdc_image_flags) == 0);
    cxt->context_flags |= heap_sizes;

    uint64_t valid_tables;
    uint64_t sorted_tables;
    if (!read_u64(&curr, &curr_len, &valid_tables)
        || !read_u64(&curr, &curr_len, &sorted_tables))
    {
        return false;
    }

    size_t n = count_set_bits(valid_tables);
    uint8_t const* table_begin = curr + (n * sizeof(uint32_t));

    // We need to collect table row counts first.
    // This is required because we need row counts to compute
    // row size when a "coded index" is used - see II.24.2.6.
    uint64_t valid = valid_tables;
    uint32_t row_counts[MDTABLE_MAX_COUNT];
    for (size_t i = 0; i < ARRAY_SIZE(row_counts); ++i)
    {
        if (valid & 1)
        {
            // Read in the row count for the table
            if (!read_u32(&curr, &curr_len, &row_counts[i]))
                return false;
        }
        else
        {
            row_counts[i] = 0;
        }
        valid = valid >> 1;
    }

    // There is an extra flag that is respected by metadata readers but is not defined in the ECMA spec.
    // If the 0x40 bit is set, then there is an extra 4-byte integer after the row counts before the table data.
    // This is refered to as the ExtraData heap size flag in the System.Reflection.Metadata reader.
    if (cxt->context_flags & mdc_extra_data)
    {
        if (!advance_stream(&curr, &curr_len, sizeof(uint32_t)))
            return false;
    }

    // Validate we processed the row counts properly
    if (curr != table_begin)
        return false;

#ifdef DNMD_PORTABLE_PDB
    md_pdb_t pdb;
    if (try_get_pdb(cxt, &pdb))
    {
        // Merge in the PDB reference row counts
        for (size_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
            row_counts[i] += pdb.type_system_table_rows[i];
    }
    else if (cxt->pdb.size != 0)
    {
        return false;
    }
#endif // DNMD_PORTABLE_PDB

    mdtable_t* table;
    valid = valid_tables;
    for (size_t i = 0; valid; ++i)
    {
        // If the table is valid, initialize the table
        if (valid & 1)
        {
            table = &cxt->tables[i];

            // Initialize the table
            if (!initialize_table_details(row_counts, cxt->context_flags, (mdtable_id_t)i, (bool)(sorted_tables & 1), table))
                return false;

            // Consume the data based on the table details
            if (!consume_table_rows(table, &curr, &curr_len))
                return false;

            // Store the context on the table as an indication of fully initialized.
            table->cxt = cxt;
        }
        sorted_tables = sorted_tables >> 1;
        valid = valid >> 1;
    }

    return true;
}

bool validate_tables(mdcxt_t* cxt)
{
    assert(cxt != NULL);
    (void)cxt;
    // [TODO] Reference ECMA-335 and encode table verification.
    // [TODO] Validate that tables marked as sorted are actually sorted.
    // [TODO] Do not allow the *Ptr tables to be present in a compressed table heap.
    return true;
}

bool try_get_pdb(mdcxt_t* cxt, md_pdb_t* pdb)
{
#ifdef DNMD_PORTABLE_PDB
    assert(cxt != NULL && pdb != NULL);

    mdstream_t* h = &cxt->pdb;
    if (h->size == 0)
        return false;

    uint8_t const* curr = h->ptr;
    size_t curr_len = h->size;

    uint8_t const* pdb_id_maybe = curr;
    if (!advance_stream(&curr, &curr_len, ARRAY_SIZE(pdb->pdb_id)))
        return false;

    memcpy(&pdb->pdb_id, pdb_id_maybe, ARRAY_SIZE(pdb->pdb_id));

    uint64_t tables;
    if (!read_u32(&curr, &curr_len, &pdb->entry_point)
        || !read_u64(&curr, &curr_len, &tables))
    {
        return false;
    }

    pdb->referenced_type_system_tables = tables;
    size_t n = count_set_bits(tables);
    uint8_t const* pdb_end = curr + (n * sizeof(uint32_t));

    // Read in all row data defined by the references bits.
    for (size_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        if (tables & 1)
        {
            // Read in the row count for referenced tables
            if (!read_u32(&curr, &curr_len, &pdb->type_system_table_rows[i]))
                return false;
        }
        else
        {
            pdb->type_system_table_rows[i] = 0;
        }
        tables = tables >> 1;
    }

    // Validate we processed the row counts properly

    if (curr != pdb_end)
        return false;
    return true;
#else
    (void)cxt;
    (void)pdb;
    return false;
#endif // !DNMD_PORTABLE_PDB
}

mdstream_t* get_heap_by_id(mdcxt_t* cxt, mdtcol_t heap_id)
{
    assert(cxt != NULL);
    switch (heap_id)
    {
        case mdtc_hblob:
            return &cxt->blob_heap;
        case mdtc_hguid:
            return &cxt->guid_heap;
        case mdtc_hstring:
            return &cxt->strings_heap;
        case mdtc_hus:
            return &cxt->user_string_heap;
        default:
            return NULL;
    }
}

mdcxt_flag_t get_large_heap_flag(mdtcol_t heap_id)
{
    switch (heap_id)
    {
    case mdtc_hblob:
        return mdc_large_blob_heap;
    case mdtc_hguid:
        return mdc_large_guid_heap;
    case mdtc_hstring:
        return mdc_large_string_heap;
    default:
        return 0;
    }
}