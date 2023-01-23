#include "internal.h"

#include <stdio.h>

// mdlib magic number for context
#define MDLIB_MAGIC_NUMBER 0x3d71b

// Defined in II.24.2.1
#define METADATA_SIG 0x424A5342

static mdcxt_t* allocate_full_context(mdcxt_t* cxt)
{
    // The intent here is to call the allocator once.
    // Therefore we compute the full size and then call
    // malloc a single time. The following needs to be
    // done:
    //  1. Compute total amount of needed memory:
    //     - sizeof(mdcxt_t)
    //     - table count
    //     - column count for each table
    //  2. Copy supplied mdcxt_t to the newly allocated one
    //  3. Determine table array offset
    //  4. Set table pointer in mdcxt_t
    //  5. Determine column details array offsets
    //  6. Set column details array in each table
    //  7. Return the newly allocated context

    uint32_t table_col_sizes[MDTABLE_MAX_COUNT];
    uint32_t total_col_size = 0;
    for (mdtable_id_t id = mdtid_First; id < mdtid_End; ++id)
    {
        table_col_sizes[id] = sizeof(mdtcol_t) * get_table_column_count(id);
        total_col_size += table_col_sizes[id];
    }

    // Ensure all sections of the allocation are pointer aligned.
    size_t cxt_mem = align_to(sizeof(mdcxt_t), sizeof(void*));
    size_t tables_mem = MDTABLE_MAX_COUNT * align_to(sizeof(mdtable_t), sizeof(void*));
    size_t col_mem = align_to(total_col_size, sizeof(void*));

    size_t total_mem = cxt_mem + tables_mem + col_mem;
    uint8_t* mem = (uint8_t*)malloc(total_mem);
    if (mem == NULL)
        return NULL;

    // Copy passed in state
    mdcxt_t* pcxt = (mdcxt_t*)mem;
    mem += cxt_mem;
    memcpy(pcxt, cxt, sizeof(*cxt));
    assert(pcxt->tables == NULL);

    // Zero out the remaining memory
    memset(mem, 0, total_mem - cxt_mem);

    // Update the tables pointer to offset in allocation
    pcxt->tables = (mdtable_t*)mem;
    mem += tables_mem;

    // Update each table's column array
    for (mdtable_id_t id = mdtid_First; id < mdtid_End; ++id)
    {
        pcxt->tables[id].column_details = (mdtcol_t*)mem;
        uint32_t size = table_col_sizes[id];
        mem += size;
    }

    assert(mem <= (uint8_t*)(pcxt + total_mem));
    return pcxt;
}

bool md_create_handle(void const* data, size_t data_len, mdhandle_t* handle)
{
    if (data == NULL || handle == NULL)
        return false;

    uint8_t const* const base = data;
    uint8_t const* curr = data;
    size_t curr_len = data_len;

    // Validate the metadata root is the minimally valid before creating a handle.
    uint32_t sig;
    uint32_t ver_buf_count;
    uint16_t stream_count;

    mdcxt_t cxt;
    memset(&cxt, 0, sizeof(cxt));

    // Consume header defined in II.24.2.1
    if (!read_u32(&curr, &curr_len, &sig) || sig != METADATA_SIG)
        return false;

    if (!read_u16(&curr, &curr_len, &cxt.major_ver)
        || !read_u16(&curr, &curr_len, &cxt.minor_ver)
        || !advance_stream(&curr, &curr_len, 4)
        || !read_u32(&curr, &curr_len, &ver_buf_count))
    {
        return false;
    }

    // The version count is aligned to 4-bytes
    ver_buf_count = align_to(ver_buf_count, 4);

    // Confirm terminator and consume the version/aligned length
    cxt.version = (char const*)curr;
    if (ver_buf_count == 0
        || cxt.version[ver_buf_count - 1] != '\0'
        || !advance_stream(&curr, &curr_len, ver_buf_count))
    {
        return false;
    }

    if (!read_u16(&curr, &curr_len, &cxt.flags)
        || !read_u16(&curr, &curr_len, &stream_count))
    {
        return false;
    }

    // Iterate over the discovered streams
    uint32_t offset;
    uint32_t stream_size;
    uint8_t* name_end;
    size_t name_len;
    for (size_t i = 0; i < stream_count; ++i)
    {
        if (!read_u32(&curr, &curr_len, &offset)
            || !read_u32(&curr, &curr_len, &stream_size))
        {
            return false;
        }

        // Find the terminating null.
        name_end = memchr(curr, 0, curr_len);
        if (name_end == NULL)
            return false;

        name_len = name_end - curr;
        if (strncmp((char const*)curr, "#~", name_len) == 0)
        {
            cxt.tables_heap.ptr = base + offset;
            cxt.tables_heap.size = stream_size;
        }
        else if (strncmp((char const*)curr, "#Strings", name_len) == 0)
        {
            cxt.strings_heap.ptr = base + offset;
            cxt.strings_heap.size = stream_size;
        }
        else if (strncmp((char const*)curr, "#Blob", name_len) == 0)
        {
            cxt.blob_heap.ptr = base + offset;
            cxt.blob_heap.size = stream_size;
        }
        else if (strncmp((char const*)curr, "#US", name_len) == 0)
        {
            cxt.user_string_heap.ptr = base + offset;
            cxt.user_string_heap.size = stream_size;
        }
        else if (strncmp((char const*)curr, "#GUID", name_len) == 0)
        {
            cxt.guid_heap.ptr = base + offset;
            cxt.guid_heap.size = stream_size;
        }
#ifdef DNMD_PORTABLE_PDB
        else if (strncmp((char const*)curr, "#Pdb", name_len) == 0)
        {
            cxt.pdb.ptr = base + offset;
            cxt.pdb.size = stream_size;
        }
#endif // DNMD_PORTABLE_PDB
        else
        {
            assert(!"Unknown stream");
            return false;
        }

        // Align the string length to 4 bytes.
        if (!advance_stream(&curr, &curr_len, align_to((uint32_t)(name_len + 1), 4)))
            return false;
    }

    // Header initialization is complete.
    cxt.magic = MDLIB_MAGIC_NUMBER;
    cxt.data.ptr = data;
    cxt.data.size = data_len;
    // Allocate and initialize a context

    mdcxt_t* pcxt = allocate_full_context(&cxt);
    if (pcxt == NULL)
        return false;

#ifdef DEBUG
    memset(&cxt, 0xcc, sizeof(cxt));
#endif //DEBUG

    // Initialize the tables in the new context.
    if (!initialize_tables(pcxt))
    {
        free(pcxt);
        return false;
    }

    // Move the constructed context to the allocated one.
    *handle = pcxt;
    return true;
}

void md_destroy_handle(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return;
    free(cxt);
}

bool md_validate(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    return validate_guid_heap(cxt)
        && validate_strings_heap(cxt)
        && validate_user_string_heap(cxt)
        && validate_blob_heap(cxt)
        && validate_tables(cxt);
}

static bool dump_table_rows(mdtable_t* table)
{
#define IF_NOT_ONE_REPORT_RETURN(exp) if (1 != (exp)) { printf("Failure in row %u (0x%x), column %u (0x%x)\n", i, i, j, j); return false; }

    if (table->row_count == 0)
    {
        printf("Empty table\n");
    }
    else
    {
        printf("Table %u (0x%x) rows: %u\n", table->table_id, table->table_id, table->row_count);
    }

    char const* str;
    GUID guid;
    uint8_t const* blob;
    uint32_t blob_len;
    uint32_t constant;
    mduserstring_t user_string;
    mdToken tk;

#ifdef DEBUG_TABLE_COLUMN_LOOKUP
    uint16_t const embedded_tid = ((uint16_t)table->table_id) << 8;
#define IDX(x) (embedded_tid | x)
#else
#define IDX(x) x
#endif

    // Create a cursor to the first row.
    mdcursor_t cursor = create_cursor(table, 1);

    for (uint32_t i = 0; i < table->row_count; ++i)
    {
        printf("|");
        for (uint8_t j = 0; j < table->column_count; ++j)
        {
            if (table->column_details[j] & mdtc_hstring)
            {
                IF_NOT_ONE_REPORT_RETURN(md_get_column_value_as_utf8(cursor, IDX(j), 1, &str));
                printf("'%s'|", str);
            }
            else if (table->column_details[j] & mdtc_hguid)
            {
                IF_NOT_ONE_REPORT_RETURN(md_get_column_value_as_guid(cursor, IDX(j), 1, &guid));
                printf("{%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x}|",
                    guid.Data1, guid.Data2, guid.Data3,
                    guid.Data4[0], guid.Data4[1],
                    guid.Data4[2], guid.Data4[3],
                    guid.Data4[4], guid.Data4[5],
                    guid.Data4[6], guid.Data4[7]);
            }
            else if (table->column_details[j] & mdtc_hblob)
            {
                IF_NOT_ONE_REPORT_RETURN(md_get_column_value_as_blob(cursor, IDX(j), 1, &blob, &blob_len));
                printf("Offset: %zu (len: %u)|", (blob - table->cxt->blob_heap.ptr), blob_len);
            }
            else if (table->column_details[j] & mdtc_hus)
            {
                IF_NOT_ONE_REPORT_RETURN(md_get_column_value_as_userstring(cursor, IDX(j), 1, &user_string));
                printf("UTF-16 string (%u bytes)|", user_string.str_bytes);
            }
            else if (table->column_details[j] & (mdtc_idx_table | mdtc_idx_coded))
            {
                IF_NOT_ONE_REPORT_RETURN(md_get_column_value_as_token(cursor, IDX(j), 1, &tk));
                printf("0x%08x (mdToken)|", tk);
            }
            else
            {
                assert(table->column_details[j] & mdtc_constant);
                IF_NOT_ONE_REPORT_RETURN(md_get_column_value_as_constant(cursor, IDX(j), 1, &constant));
                printf("0x%08x|", constant);
            }
        }
        printf("\n");
        if (!md_cursor_next(&cursor) && i != (table->row_count - 1))
            return false;
    }
    printf("\n");
#undef IF_NOT_ONE_REPORT_RETURN

    return true;
}

bool md_dump_tables(mdhandle_t handle, int32_t table_id)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    for (int32_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        // Check if the user supplied a table to check
        if (table_id > -1)
        {
            if (i < table_id) // Less than, skip.
                continue;
            if (i > table_id) // Greater than, done.
                break;
            assert(i == table_id);
        }

        if (!dump_table_rows(&cxt->tables[i]))
        {
            printf("Failure in table '%u'\n", i);
            return false;
        }
    }

    return true;
}

char const* md_get_version_string(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return NULL;
    return cxt->version;
}

mdcxt_t* extract_mdcxt(mdhandle_t md)
{
    mdcxt_t* cxt = (mdcxt_t*)md;
    if (!cxt || cxt->magic != MDLIB_MAGIC_NUMBER)
        return NULL;
    return cxt;
}
