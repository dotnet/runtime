// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>

#include <data_stream.h>

#ifdef HOST_WINDOWS
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

#define _Atomic(t) t
static void* atomic_load(_Atomic(void*)* ptr)
{
    void* p = *ptr;
#ifdef _M_ARM64
    MemoryBarrier();
#endif // _M_ARM64
    return p;
}

static bool atomic_compare_exchange_strong(_Atomic(void*)* obj, void** expected, void* desired)
{
    void* comperand = *expected;
    *expected = InterlockedCompareExchangePointer(obj, desired, comperand);
    return comperand == *expected;
}

static bool atomic_compare_exchange_weak(_Atomic(void*)* obj, void** expected, void* desired)
{
    return atomic_compare_exchange_strong(obj, expected, desired);
}

#define strdup(...) _strdup(__VA_ARGS__)

#else
#include <stdatomic.h>
#endif // !HOST_WINDOWS

#ifndef EXTERN_C
#ifdef __cplusplus
#define EXTERN_C extern "C"
#else
#define EXTERN_C
#endif // __cplusplus
#endif // EXTERN_C

static uint16_t const stream_version = 1;
static uint32_t const endian_magic_value = 0x646e6300;
static uint8_t const big_endian[sizeof(endian_magic_value)] = { 0x64, 0x6e, 0x63, 0 };
static uint8_t const little_endian[sizeof(endian_magic_value)] = { 0, 0x63, 0x6e, 0x64 };

#ifdef __cplusplus
EXTERN_C {
#endif // __cplusplus

typedef struct data_block__
{
    void* begin;
    _Atomic(void*) pos;
    void* end;
    _Atomic(struct data_block__*) prev;
} data_block_t;
static_assert(sizeof(data_block_t) % sizeof(void*) == 0, "Block data allocations assume pointer alignment");

typedef struct data_stream__
{
    _Atomic(data_block_t*) curr;
    size_t block_data_size;
    size_t max_data_size;
    data_stream_context_t* cxt;
} data_stream_t;

typedef struct stream_entry__
{
    uint32_t offset_next;
    uint32_t reserved;
    uint8_t data[];
} stream_entry_t;

#ifdef __cplusplus
}
#endif // __cplusplus

EXTERN_C ds_validate_t dnds_validate(uint32_t m)
{
    if (memcmp(&m, big_endian, sizeof(big_endian)) == 0)
    {
        return dsv_big_endian;
    }
    else if (memcmp(&m, little_endian, sizeof(little_endian)) == 0)
    {
        return dsv_little_endian;
    }
    else
    {
        return dsv_invalid;
    }
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_is_big_endian(data_stream_context_t* cxt)
{
    assert(cxt != NULL && dnds_validate(cxt->magic) != dsv_invalid);
    return memcmp(&cxt->magic, big_endian, sizeof(big_endian)) == 0
        ? true
        : false;
}

// Used to perform a single allocation during initialization.
static uint8_t* initial_allocation(
    size_t base_size,
    size_t stream_count,
    size_t const* stream_byte_lengths,
    size_t* allocated_size)
{
    assert(base_size > 0);
    assert(stream_count > 0);
    assert(stream_byte_lengths != NULL);
    assert(allocated_size != NULL);

    size_t total_streams = 0;
    for (size_t i = 0; i < stream_count; ++i)
    {
        // Stream size must be large enough for an entry.
        if (stream_byte_lengths[i] < sizeof(stream_entry_t))
            return NULL;
        total_streams += stream_byte_lengths[i];
    }

    *allocated_size = base_size + total_streams;
    return (uint8_t*)malloc(*allocated_size);
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_init(
    data_stream_context_t* cxt,
    uint32_t stream_count,
    size_t const* stream_byte_lengths)
{
    if (cxt == NULL || stream_count == 0)
    {
        return false;
    }

    // Initialize basic details
    cxt->magic = endian_magic_value;
    cxt->size = sizeof(*cxt);
    cxt->version = stream_version;

    // Perform a single allocation for initialization.
    size_t total_allocation;

    // [TODO] Arithmetic overflow
    size_t steams_collection_size = (size_t)stream_count * (sizeof(data_stream_t) + sizeof(data_block_t));
    uint8_t* mem = initial_allocation(
        steams_collection_size,
        stream_count,
        stream_byte_lengths,
        &total_allocation);
    if (mem == NULL)
        return false;

    uint8_t const * const end = mem + total_allocation;
    cxt->streams_count = stream_count;
    cxt->streams = (data_stream_t*)mem;

    // Move past the stream collection.
    // The remaining memory is for the blocks and data.
    mem += stream_count * sizeof(data_stream_t);

    // Zero initialize all stream memory.
    memset(mem, 0, total_allocation - steams_collection_size);

    // Now initialize each stream
    for (size_t i = 0; i < stream_count; ++i)
    {
        size_t len = stream_byte_lengths[i];
        assert(len != 0);

        // Initialize the blocks
        data_block_t* blk = (data_block_t*)mem;
        mem += sizeof(data_block_t);
        assert(((intptr_t)mem) % sizeof(void*) == 0);
        blk->begin = mem;
        blk->pos = mem + len;
        blk->end = blk->pos;
        blk->prev = NULL;

        // Increment the memory counter.
        mem += len;

        // Initialize the current stream with the new block
        data_stream_t* curr = &cxt->streams[i];
        curr->curr = blk;
        curr->block_data_size = len;
        assert(len >= sizeof(stream_entry_t));
        curr->max_data_size = len - sizeof(stream_entry_t);
        curr->cxt = cxt;
    }

    assert(mem == end);
    return true;
}

EXTERN_C DATA_STREAM_EXPORT void dnds_destroy(data_stream_context_t* cxt)
{
    assert(dnds_validate(cxt->magic) != dsv_invalid);
    if (cxt->streams != NULL)
    {
        for (uint32_t i = 0; i < cxt->streams_count; ++i)
        {
            // The streams themselves are all allocated
            // in a chunk during initialization. However,
            // any chained streams need to be deallocated
            // before the larger stream data can be freed.
            data_block_t* blk = cxt->streams[i].curr;
            while (blk != NULL)
            {
                data_block_t* tmp = blk->prev;

                // If the current block is the last block (prev == NULL)
                // in the chain, don't delete it since it was allocated
                // with the stream itself.
                if (tmp == NULL)
                    break;

                free(blk);
                blk = tmp;
            }
        }
        free(cxt->streams);
        cxt->streams_count = 0;
        cxt->streams = NULL;
    }
}

static stream_entry_t* alloc_entry(data_block_t* blk, size_t needed)
{
    assert(blk != NULL);
    size_t total_needed = sizeof(stream_entry_t) + needed;
    assert(needed < total_needed);

    size_t avail;
    void* update;
    void* expected = atomic_load(&blk->pos);
    do
    {
        assert((intptr_t)expected >= (intptr_t)blk->begin);
        avail = (intptr_t)expected - (intptr_t)blk->begin;
        if (avail < total_needed)
            return NULL;

        update = (void*)((intptr_t)expected - total_needed);
    }
    while (!atomic_compare_exchange_weak(&blk->pos, &expected, update));

    // Initialize the entry
    memset(update, 0, total_needed);
    stream_entry_t* new_entry = (stream_entry_t*)update;
    new_entry->offset_next = (uint32_t)total_needed;
    return new_entry;
}

static bool expand_stream(data_stream_t* s)
{
    assert(s != NULL);
    size_t len = s->block_data_size;

    // Create new block
    uint8_t* mem = (uint8_t*)malloc(sizeof(data_block_t) + len);
    if (mem == NULL)
        return false;

    // Get memory beyond the data_block_t.
    data_block_t* blk = (data_block_t*)mem;
    mem += sizeof(data_block_t);
    memset(mem, 0, len);

    // Initialize block
    assert(((intptr_t)mem) % sizeof(void*) == 0);
    blk->begin = mem;
    blk->pos = mem + len;
    blk->end = blk->pos;

    // Set the current block as the new previous one.
    data_block_t* expected = atomic_load(&s->curr);
    blk->prev = expected;

    // Attempt to expand the stream. If it fails, we lost the race.
    if (!atomic_compare_exchange_strong(&s->curr, &expected, blk))
        free(blk);

    return true;
}

static stream_entry_t* alloc_stream_entry(data_stream_t* s, size_t needed)
{
    stream_entry_t* e = alloc_entry(atomic_load(&s->curr), needed);
    while (e == NULL)
    {
        if (!expand_stream(s))
            return NULL;
        e = alloc_entry(atomic_load(&s->curr), needed);
    }
    return e;
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_define_type(
    data_stream_context_t* cxt,
    type_details_t const* details,
    size_t total_size,
    size_t offsets_length,
    field_offset_t const* offsets)
{
    if (cxt == NULL
        || cxt->streams_count == 0
        || details == NULL)
    {
        return false;
    }
    assert(details->reserved == 0);
    assert(dnds_validate(cxt->magic) != dsv_invalid);

    data_stream_t* str = &cxt->streams[0];

    // [TODO] Arithmetic overflow
    size_t offsets_bytes = offsets_length * sizeof(field_offset_t);
    size_t needed = sizeof(type_details_t*) + sizeof(total_size) + offsets_bytes;
    stream_entry_t* entry = alloc_stream_entry(str, needed);
    if (entry == NULL)
        return false;

    uint8_t* curr = entry->data;
    memcpy(curr, &details, sizeof(type_details_t*));
    curr += sizeof(type_details_t*);
    memcpy(curr, &total_size, sizeof(total_size));
    curr += sizeof(total_size);
    memcpy(curr, offsets, offsets_bytes);
    curr += offsets_bytes;
    assert(entry->offset_next == (curr - (uint8_t*)entry));
    return true;
}

EXTERN_C DATA_STREAM_EXPORT data_stream_t* dnds_get_stream(
    data_stream_context_t* cxt,
    size_t id)
{
    if (cxt == NULL || id >= cxt->streams_count || id == 0)
    {
        return false;
    }
    assert(dnds_validate(cxt->magic) != dsv_invalid);

    return &cxt->streams[id];
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_record_instance(
    data_stream_t* str,
    uint16_t type,
    void* inst)
{
    if (str == NULL)
        return false;

    stream_entry_t* entry = alloc_stream_entry(str, sizeof(type) + sizeof(inst));
    if (entry == NULL)
        return false;

    uint8_t* curr = entry->data;
    memcpy(curr, &type, sizeof(type));
    curr += sizeof(type);
    memcpy(curr, &inst, sizeof(inst));
    curr += sizeof(inst);
    assert(entry->offset_next == (curr - (uint8_t*)entry));
    return true;
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_record_blob(
    data_stream_t* str,
    uint16_t type,
    uint16_t size,
    void* inst)
{
    if (str == NULL)
        return false;

    size_t needed_size = sizeof(type) + sizeof(size) + (size_t)size;
    if (needed_size > str->max_data_size)
        return false;

    stream_entry_t* entry = alloc_stream_entry(str, needed_size);
    if (entry == NULL)
        return false;

    uint8_t* curr = entry->data;
    memcpy(curr, &type, sizeof(type));
    curr += sizeof(type);
    memcpy(curr, &size, sizeof(size));
    curr += sizeof(size);
    memcpy(curr, inst, size);
    curr += size;
    assert(entry->offset_next == (curr - (uint8_t*)entry));
    return true;
}

static bool read_local(memory_reader_t* r, intptr_t m, size_t* i, void** ptr)
{
    *ptr = (void*)m;
    return true;
}

static void free_local(memory_reader_t* r, size_t len, void* ptr)
{
    // nop
}

static memory_reader_t g_memory_reader_local = { read_local, free_local };

static bool read_in_block_data(memory_reader_t* reader, data_block_t* block, size_t* data_read, void** data, data_block_t** prev_block)
{
    bool result = false;
    size_t remote_block_read = sizeof(data_block_t);
    data_block_t* remote_block = NULL;
    if (!reader->read_ptr(reader, (intptr_t)block, &remote_block_read, (void**)&remote_block))
        goto cleanup;

    *data_read = (intptr_t)remote_block->end - (intptr_t)remote_block->pos;
    if (!reader->read_ptr(reader, (intptr_t)remote_block->pos, data_read, data))
        goto cleanup;

    *prev_block = remote_block->prev;
    result = true;
cleanup:
    if (remote_block) reader->free_ptr(reader, remote_block_read, remote_block);
    return result;
}

static bool read_in_details(memory_reader_t* reader, type_details_t* details, type_details_t* local_details)
{
    assert(reader != NULL);
    assert(details != NULL);
    assert(local_details != NULL);

    char* name = NULL;
    size_t name_read = 0;

    bool result = false;
    type_details_t* remote_details = NULL;
    size_t remote_details_read = sizeof(*remote_details);
    if (!reader->read_ptr(reader, (intptr_t)details, &remote_details_read, (void**)&remote_details))
        goto cleanup;

    name_read = remote_details->name_len;
    if (!reader->read_ptr(reader, (intptr_t)remote_details->name, &name_read, (void**)&name))
        goto cleanup;

    *local_details = *remote_details;
    local_details->name = strdup(name);
    if (local_details->name == NULL)
        goto cleanup;

    result = true;
cleanup:
    if (remote_details) reader->free_ptr(reader, remote_details_read, remote_details);
    if (name) reader->free_ptr(reader, name_read, name);
    return result;
}

static bool enum_type(
    on_next_type on_next,
    void* user_defined,
    memory_reader_t* reader,
    stream_entry_t* curr,
    stream_entry_t* end)
{
    assert(on_next != NULL);
    assert(reader != NULL);
    assert(curr <= end);

    // Iterate over entries
    intptr_t mem_pos;
    stream_entry_t* next;
    while (curr < end)
    {
        // Compute the next entry
        assert(curr->offset_next != 0);
        next = (stream_entry_t*)(((uint8_t*)curr) + curr->offset_next);

        // We compute the memory position based on the start of data.
        mem_pos = (intptr_t)curr->data;

        type_details_t* details;
        memcpy(&details, (void*)mem_pos, sizeof(type_details_t*));
        mem_pos += sizeof(type_details_t*);

        type_details_t local_details;
        if (!read_in_details(reader, details, &local_details))
            return false;

        size_t total_size;
        memcpy(&total_size, (void*)mem_pos, sizeof(total_size));
        mem_pos += sizeof(total_size);

        size_t offsets_length = ((intptr_t)next - mem_pos) / sizeof(field_offset_t);
        bool cont = on_next(&local_details, total_size, offsets_length, (field_offset_t*)mem_pos, user_defined);
        free((void*)local_details.name);
        if (!cont)
            break;

        curr = next;
    }
    return true;
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_enum_type(
    data_stream_context_t* cxt,
    on_next_type on_next,
    void* user_defined,
    memory_reader_t* reader)
{
    if (cxt == NULL
        || cxt->streams_count == 0
        || on_next == NULL)
    {
        return false;
    }
    assert(dnds_validate(cxt->magic) != dsv_invalid);

    if (reader == NULL)
        reader = &g_memory_reader_local;

    bool result = false;

    size_t data_read = 0;
    void* data = NULL;

    // Read in the streams collection
    size_t streams_read = (size_t)cxt->streams_count * sizeof(data_stream_t);
    data_stream_t* streams = NULL;
    if (!reader->read_ptr(reader, (intptr_t)cxt->streams, &streams_read, (void**)&streams))
        goto cleanup;

    // The first stream is the types' stream.
    data_stream_t* types_stream = &streams[0];

    // [TODO] Read all blocks in the stream
    data_block_t* types_block = types_stream->curr;
    if (!read_in_block_data(reader, types_block, &data_read, &data, &types_block))
        goto cleanup;

    stream_entry_t* entry = (stream_entry_t*)data;
    stream_entry_t* end = (stream_entry_t*)((intptr_t)data + data_read);
    if (!enum_type(on_next, user_defined, reader, entry, end))
        goto cleanup;

    result = true;
cleanup:
    if (streams) reader->free_ptr(reader, streams_read, streams);
    if (data) reader->free_ptr(reader, data_read, data);
    return result;
}

static bool enum_blobs(
    on_next_blob on_next,
    void* user_defined,
    memory_reader_t* reader,
    stream_entry_t* curr,
    stream_entry_t* end)
{
    assert(on_next != NULL);
    assert(reader != NULL);
    assert(curr <= end);

    // Iterate over entries
    intptr_t mem_pos;
    stream_entry_t* next;
    while (curr < end)
    {
        // Compute the next entry
        assert(curr->offset_next != 0);
        next = (stream_entry_t*)(((uint8_t*)curr) + curr->offset_next);
        mem_pos = (intptr_t)curr->data;

        uint16_t type;
        memcpy(&type, (void*)mem_pos, sizeof(type));
        mem_pos += sizeof(type);

        uint16_t size;
        memcpy(&size, (void*)mem_pos, sizeof(size));
        mem_pos += sizeof(size);

        void* inst = (void*)mem_pos;
        mem_pos += size;

        bool cont = on_next(type, (uint16_t)size, inst, user_defined);
        if (!cont)
            break;

        curr = next;
    }
    return true;
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_enum_blobs(
    data_stream_context_t* cxt,
    on_next_blob on_next,
    void* user_defined,
    memory_reader_t* reader)
{
    if (cxt == NULL
        || cxt->streams_count <= 1
        || on_next == NULL)
    {
        return false;
    }
    assert(dnds_validate(cxt->magic) != dsv_invalid);

    if (reader == NULL)
        reader = &g_memory_reader_local;

    bool result = false;

    size_t data_read = 0;
    void* data = NULL;

    // Read in the streams collection
    size_t streams_read = (size_t)cxt->streams_count * sizeof(data_stream_t);
    data_stream_t* streams = NULL;
    if (!reader->read_ptr(reader, (intptr_t)cxt->streams, &streams_read, (void**)&streams))
        goto cleanup;

    // [TODO] Enumerate all streams
    data_stream_t* str = &streams[1];

    // Read the stream's blocks
    // [TODO] Read all blocks in the stream
    data_block_t* block = str->curr;
    if (!read_in_block_data(reader, block, &data_read, &data, &block))
        goto cleanup;

    stream_entry_t* entry = (stream_entry_t*)data;
    stream_entry_t* end = (stream_entry_t*)((intptr_t)data + data_read);
    if (!enum_blobs(on_next, user_defined, reader, entry, end))
        goto cleanup;

    result = true;
cleanup:
    if (streams) reader->free_ptr(reader, streams_read, streams);
    if (data) reader->free_ptr(reader, data_read, data);
    return result;
}

static bool enum_instances(
    on_next_instance on_next,
    void* user_defined,
    memory_reader_t* reader,
    stream_entry_t* curr,
    stream_entry_t* end)
{
    assert(on_next != NULL);
    assert(reader != NULL);
    assert(curr <= end);

    // Iterate over entries
    intptr_t mem_pos;
    stream_entry_t* next;
    while (curr < end)
    {
        // Compute the next entry
        assert(curr->offset_next != 0);
        next = (stream_entry_t*)(((uint8_t*)curr) + curr->offset_next);
        mem_pos = (intptr_t)curr->data;

        uint16_t type;
        memcpy(&type, (void*)mem_pos, sizeof(type));
        mem_pos += sizeof(type);

        intptr_t inst;
        memcpy(&inst, (void*)mem_pos, sizeof(inst));
        mem_pos += sizeof(inst);

        bool cont = on_next(type, inst, user_defined);
        if (!cont)
            break;

        curr = next;
    }
    return true;
}

EXTERN_C DATA_STREAM_EXPORT bool dnds_enum_instances(
    data_stream_context_t* cxt,
    on_next_instance on_next,
    void* user_defined,
    memory_reader_t* reader)
{
    if (cxt == NULL
        || cxt->streams_count <= 2
        || on_next == NULL)
    {
        return false;
    }
    assert(dnds_validate(cxt->magic) != dsv_invalid);

    if (reader == NULL)
        reader = &g_memory_reader_local;

    bool result = false;

    size_t data_read = 0;
    void* data = NULL;

    // Read in the streams collection
    size_t streams_read = (size_t)cxt->streams_count * sizeof(data_stream_t);
    data_stream_t* streams = NULL;
    if (!reader->read_ptr(reader, (intptr_t)cxt->streams, &streams_read, (void**)&streams))
        goto cleanup;

    // [TODO] Enumerate all streams
    data_stream_t* str = &streams[2];

    // Read the stream's blocks
    // [TODO] Read all blocks in the stream
    data_block_t* block = str->curr;
    if (!read_in_block_data(reader, block, &data_read, &data, &block))
        goto cleanup;

    stream_entry_t* entry = (stream_entry_t*)data;
    stream_entry_t* end = (stream_entry_t*)((intptr_t)data + data_read);
    if (!enum_instances(on_next, user_defined, reader, entry, end))
        goto cleanup;

    result = true;
cleanup:
    if (streams) reader->free_ptr(reader, streams_read, streams);
    if (data) reader->free_ptr(reader, data_read, data);
    return result;
}
