#include "internal.h"

#include <stdio.h>

// mdlib magic number for context
#define MDLIB_MAGIC_NUMBER 0x3d71b

// Defined in II.24.2.1
#define METADATA_SIG 0x424A5342

bool md_create_handle(void* data, size_t data_len, mdhandle_t* handle)
{
    if (data == NULL || handle == NULL)
        return false;

    uint8_t* const base = data;
    uint8_t* curr = data;
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

    mdcxt_t* pcxt = (mdcxt_t*)malloc(sizeof(mdcxt_t));
    if (pcxt == NULL)
        return false;

    memcpy(pcxt, &cxt, sizeof(cxt));
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
    free(cxt->data.ptr);
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
//#define PRINTF(...) printf(__VA_ARGS__);
#define PRINTF(...) ;
#define IF_FALSE_REPORT_RETURN(exp) if (!exp) { printf("Failure in row %u (0x%x), column %u (0x%x)\n", i, i, j, j); return false; }

    printf("Table 0x%x rows: %u\n", table->table_id, table->row_count);

    char const* str;
    GUID guid;
    uint8_t const* blob;
    uint32_t blob_len;
    uint32_t constant;
    mdToken tk;

#ifdef DEBUG_TABLE_COLUMN_LOOKUP
    uint32_t const embedded_tid = table->table_id << 8;
#define IDX(x) (embedded_tid | x)
#else
#define IDX(x) x
#endif

    // Create a cursor to the first row.
    mdcursor_t cursor = create_cursor(table, 1);

    for (uint32_t i = 0; i < table->row_count; ++i)
    {
        for (uint32_t j = 0; j < table->column_count; ++j)
        {
            if (table->column_details[j] & mdtc_hstring)
            {
                IF_FALSE_REPORT_RETURN(md_get_column_value_as_utf8(cursor, IDX(j), &str));
                PRINTF("'%s' ", str);
            }
            else if (table->column_details[j] & mdtc_hguid)
            {
                IF_FALSE_REPORT_RETURN(md_get_column_value_as_guid(cursor, IDX(j), &guid));
                PRINTF("{%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x} ",
                    guid.Data1, guid.Data2, guid.Data3,
                    guid.Data4[0], guid.Data4[1],
                    guid.Data4[2], guid.Data4[3],
                    guid.Data4[4], guid.Data4[5],
                    guid.Data4[6], guid.Data4[7]);
            }
            else if (table->column_details[j] & mdtc_hblob)
            {
                IF_FALSE_REPORT_RETURN(md_get_column_value_as_blob(cursor, IDX(j), &blob, &blob_len));
                PRINTF("0x%p [len: %u] ", blob, blob_len);
            }
            else if (table->column_details[j] & (mdtc_idx_table | mdtc_idx_coded))
            {
                IF_FALSE_REPORT_RETURN(md_get_column_value_as_token(cursor, IDX(j), &tk));
                PRINTF("0x%08x (mdToken) ", tk);
            }
            else if (table->column_details[j] & mdtc_constant)
            {
                IF_FALSE_REPORT_RETURN(md_get_column_value_as_constant(cursor, IDX(j), &constant));
                PRINTF("0x%08x ", constant);
            }
        }
        PRINTF("\n");
        if (!md_cursor_next(&cursor) && i != (table->row_count - 1))
            return false;
    }
    PRINTF("\n");
#undef IF_FALSE_REPORT_RETURN
#undef PRINTF

    return true;
}

bool md_dump_tables(mdhandle_t handle)
{
    mdcxt_t* cxt = extract_mdcxt(handle);
    if (cxt == NULL)
        return false;

    for (uint32_t i = 0; i < MDTABLE_MAX_COUNT; ++i)
    {
        if (!dump_table_rows(&cxt->tables[i]))
        {
            printf("Failure in table '%u'\n", i);
            return false;
        }
    }

    return true;
}

mdcxt_t* extract_mdcxt(mdhandle_t md)
{
    mdcxt_t* cxt = (mdcxt_t*)md;
    if (!cxt || cxt->magic != MDLIB_MAGIC_NUMBER)
        return NULL;
    return cxt;
}
