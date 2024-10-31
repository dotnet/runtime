//
// File containing functions that we templatize using macros
//

#ifndef SEARCH_COMPARE
#error Must define SEARCH_COMPARE() macro
#endif // SEARCH_COMPARE

#ifndef SEARCH_FUNC_NAME
#error Must define SEARCH_FUNC_NAME(name) macro
#endif // SEARCH_FUNC_NAME

// Since MSVC doesn't have a C11 compatible bsearch_s, defining one below.
// Ideally we would use the one in the standard so the signature is designed
// to match what should eventually exist.
static void const* SEARCH_FUNC_NAME(md_bsearch)(
    void const* key,
    void const* base,
    rsize_t count,
    rsize_t element_size,
    void* cxt)
{
    assert(key != NULL && base != NULL);
    while (count > 0)
    {
        void const* row = (uint8_t const*)base + (element_size * (count / 2));
        int32_t res = SEARCH_COMPARE(key, row, cxt);
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

static void const* SEARCH_FUNC_NAME(md_lsearch)(
    void const* key,
    void const* base,
    rsize_t count,
    rsize_t element_size,
    void* cxt)
{
    assert(key != NULL && base != NULL);
    void const* row = base;
    for (rsize_t i = 0; i < count; ++i)
    {
        int32_t res = SEARCH_COMPARE(key, row, cxt);
        if (res == 0)
            return row;

        // Onto the next row.
        row = (uint8_t const*)row + element_size;
    }
    return NULL;
}

// Modeled after C11's bsearch_s. This API performs a binary search
// and instead of returning NULL if the value isn't found, the last
// compare result and row is returned.
static int32_t SEARCH_FUNC_NAME(mdtable_bsearch_closest)(
    void const* key,
    mdtable_t* table,
    find_cxt_t* fcxt,
    uint32_t* found_row)
{
    assert(table != NULL && found_row != NULL);
    void const* base = table->data.ptr;
    rsize_t count = table->row_count;
    rsize_t element_size = table->row_size_bytes;

    int32_t res = 0;
    void const* row = base;
    while (count > 0)
    {
        row = (uint8_t const*)base + (element_size * (count / 2));
        res = SEARCH_COMPARE(key, row, fcxt);
        if (res == 0 || count == 1)
            break;

        if (res < 0)
        {
            count /= 2;
        }
        else
        {
            base = row;
            count -= count / 2;
        }
    }

    // Compute the found row.
    // Indices into tables begin at 1 - see II.22.
    *found_row = (uint32_t)(((intptr_t)row - (intptr_t)table->data.ptr) / element_size) + 1;
    return res;
}

#undef SEARCH_COMPARE
#undef SEARCH_FUNC_NAME
