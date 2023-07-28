#include "internal.h"

static bool merge_into_base(mdcxt_t* cxt, mdstream_t* base, mdstream_t* delta, size_t delta_offset)
{
    assert(cxt != NULL);
    assert(base != NULL);
    assert(delta != NULL && delta->size > 0);

    // Check if we are only copying a portion of the delta.
    size_t delta_size = delta->size;
    if (delta_offset != 0)
    {
        if (delta->size < delta_offset)
            return false;
        delta_size -= delta_offset;
    }

    // Check for overflow
    if (delta_size > (SIZE_MAX - base->size))
        return false;

    size_t new_len = base->size + delta_size;
    mdmem_t* mem = alloc_mdmem(cxt, new_len);
    if (mem == NULL)
        return false;

    // Copy over base and delta
    memcpy(mem->data, base->ptr, base->size);
    memcpy(mem->data + base->size, delta->ptr + delta_offset, delta_size);

    // Update base with new memory allocation
    base->ptr = mem->data;
    base->size = mem->size;
    return true;
}

static bool merge_string_heap(mdcxt_t* cxt, mdcxt_t* delta)
{
    if (delta->strings_heap.size == 0)
        return true;

    mdstream_t* base = &cxt->strings_heap;
    mdstream_t* str = &delta->strings_heap;
    return merge_into_base(cxt, base, str, 0);
}

static bool merge_guid_heap(mdcxt_t* cxt, mdcxt_t* delta)
{
    if (delta->guid_heap.size == 0)
        return true;

    mdstream_t* base = &cxt->guid_heap;
    mdstream_t* guid = &delta->guid_heap;

    // The delta's GUID heap is never fully copied.
    // Rather we merge only the newer portion of the heap.
    return merge_into_base(cxt, base, guid, base->size);
}

static bool merge_blob_heap(mdcxt_t* cxt, mdcxt_t* delta)
{
    if (delta->blob_heap.size == 0)
        return true;

    mdstream_t* base = &cxt->blob_heap;
    mdstream_t* blob = &delta->blob_heap;
    return merge_into_base(cxt, base, blob, 0);
}

static bool merge_us_heap(mdcxt_t* cxt, mdcxt_t* delta)
{
    if (delta->user_string_heap.size == 0)
        return true;

    mdstream_t* base = &cxt->user_string_heap;
    mdstream_t* blob = &delta->user_string_heap;
    return merge_into_base(cxt, base, blob, 0);
}

typedef enum
{
    dops_Default = 0,
    dops_MethodCreate,
    dops_FieldCreate,
    dops_ParamCreate,
    dops_PropertyCreate,
    dops_EventCreate,
} delta_ops_t;

static bool process_log(mdcxt_t* cxt, mdcxt_t* delta)
{
    (void)cxt;
    mdtable_t* log = &delta->tables[mdtid_ENCLog];
    //mdtable_t* map = &delta->tables[mdtid_ENCMap];
    mdcursor_t cur = create_cursor(log, 1);
    mdToken tk;
    uint32_t op;
    for (uint32_t i = 0; i < log->row_count; (void)md_cursor_next(&cur), ++i)
    {
        if (1 != md_get_column_value_as_constant(cur, mdtENCLog_Token, 1, &tk)
            || 1 != md_get_column_value_as_constant(cur, mdtENCLog_Op, 1, &op))
        {
            return false;
        }

        switch ((delta_ops_t)op)
        {
        case dops_MethodCreate:
        case dops_ParamCreate:
        case dops_FieldCreate:
        case dops_PropertyCreate:
        case dops_EventCreate:
        case dops_Default:
            assert(!"Not implemented delta operation");
            break;
        default:
            assert(!"Unknown delta operation");
            return false;
        }
    }

    return false;
}

bool merge_in_delta(mdcxt_t* cxt, mdcxt_t* delta)
{
    assert(cxt != NULL);
    assert(delta != NULL && (delta->context_flags & mdc_minimal_delta));

    // Validate metadata versions
    if (cxt->major_ver != delta->major_ver
        || cxt->minor_ver != delta->minor_ver)
    {
        return false;
    }

    // Merge heaps
    if (!merge_string_heap(cxt, delta)
        || !merge_guid_heap(cxt, delta)
        || !merge_blob_heap(cxt, delta)
        || !merge_us_heap(cxt, delta))
    {
        return false;
    }

    // Process delta log
    if (!process_log(cxt, delta))
    {
        return false;
    }

    return true;
}