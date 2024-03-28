// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

#ifndef DN_SIMDHASH_T
#error Expected DN_SIMDHASH_T definition
#endif

#ifndef DN_SIMDHASH_KEY_T
#error Expected DN_SIMDHASH_KEY_T definition
#endif

#ifndef DN_SIMDHASH_VALUE_T
#error Expected DN_SIMDHASH_VALUE_T definition
#endif

#ifndef DN_SIMDHASH_KEY_HASHER
#error Expected DN_SIMDHASH_KEY_HASHER definition
#endif

#ifndef DN_SIMDHASH_KEY_COMPARER
#error Expected DN_SIMDHASH_KEY_COMPARER definition
#endif

#ifndef DN_SIMDHASH_BUCKET_CAPACITY
#error Expected DN_SIMDHASH_BUCKET_CAPACITY definition
#endif

#define DN_SIMDHASH_BUCKET_T (DN_SIMDHASH_T ## _bucket)
#define DN_SIMDHASH_T_VTABLE (DN_SIMDHASH_T ## _vtable)
#define DN_SIMDHASH_SCAN_BUCKET_INTERNAL (DN_SIMDHASH_T ## _scan_bucket_internal)
#define DN_SIMDHASH_FIND_VALUE_INTERNAL (DN_SIMDHASH_T ## _find_value_internal)
#define DN_SIMDHASH_TRY_INSERT_INTERNAL (DN_SIMDHASH_T ## _try_insert_internal)
#define DN_SIMDHASH_REHASH_INTERNAL (DN_SIMDHASH_T ## _rehash_internal)

typedef struct {
    dn_simdhash_suffixes suffixes;
    DN_SIMDHASH_KEY_T keys[DN_SIMDHASH_BUCKET_CAPACITY];
} DN_SIMDHASH_BUCKET_T;

static uint32_t
DN_SIMDHASH_COMPUTE_HASH_INTERNAL (DN_SIMDHASH_KEY_T *key_ptr)
{
    return DN_SIMDHASH_KEY_HASHER(*key_ptr);
}

static int
DN_SIMDHASH_SCAN_BUCKET_INTERNAL (dn_simdhash_t *hash, DN_SIMDHASH_BUCKET_T *bucket, DN_SIMDHASH_KEY_T needle, dn_simdhash_suffixes search_vector)
{
    dn_simdhash_suffixes suffixes = bucket->suffixes;
    int index = dn_simdhash_find_first_matching_suffix (search_vector, suffixes);
    DN_SIMDHASH_KEY_T *key = &bucket->keys[index];

    for (int count = dn_simdhash_bucket_count (suffixes); index < count; index++, key++) {
        if (DN_SIMDHASH_KEY_COMPARER (needle, *key))
            return index;
    }

    return -1;
}

static DN_SIMDHASH_VALUE_T *
DN_SIMDHASH_FIND_VALUE_INTERNAL (dn_simdhash_t *hash, DN_SIMDHASH_KEY_T *key_ptr, uint32_t key_hash)
{
    uint8_t suffix = dn_simdhash_select_suffix(key_hash);
    uint32_t bucket_index = dn_simdhash_select_bucket_index(hash->buffers, key_hash);
    DN_SIMDHASH_BUCKET_T *bucket_address = (DN_SIMDHASH_BUCKET_T *)dn_simdhash_address_of_bucket(hash->meta, hash->buffers, bucket_index);
    DN_SIMDHASH_KEY_T key = *key_ptr;
    dn_simdhash_suffixes search_vector = dn_simdhash_build_search_vector(suffix);

    for (uint32_t c = hash->buffers.buckets_length; bucket_index < c; bucket_index++, bucket_address++) {
        int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL (hash, bucket_address, key, search_vector);
        if (index_in_bucket >= 0) {
            uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + index_in_bucket;
            return &((DN_SIMDHASH_VALUE_T *)hash->buffers.values)[value_slot_index];
        }

        if (!dn_simdhash_bucket_is_cascaded(bucket_address->suffixes))
            return NULL;
    }

    return NULL;
}

// Does not update hash->count, that's your job.
static dn_simdhash_insert_result
DN_SIMDHASH_TRY_INSERT_INTERNAL (dn_simdhash_t *hash, DN_SIMDHASH_KEY_T *key_ptr, DN_SIMDHASH_VALUE_T *value_ptr, uint32_t key_hash, uint8_t ensure_not_present)
{
    // HACK: Early out. Better to grow without scanning here.
    if (hash->count >= hash->buffers.values_length)
        return DN_SIMDHASH_INSERT_NEED_TO_GROW;

    // TODO: Optimize this to do a single scan that either locates an existing item or chooses
    //  a slot for the new item
    if (ensure_not_present)
        if (DN_SIMDHASH_FIND_VALUE_INTERNAL(hash, key_ptr, key_hash))
            return DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT;

    uint8_t suffix = dn_simdhash_select_suffix(key_hash);
    uint32_t bucket_index = dn_simdhash_select_bucket_index(hash->buffers, key_hash);
    DN_SIMDHASH_BUCKET_T *bucket_address = (DN_SIMDHASH_BUCKET_T *)dn_simdhash_address_of_bucket(hash->meta, hash->buffers, bucket_index);

    for (uint32_t c = hash->buffers.buckets_length; bucket_index < c; bucket_index++, bucket_address++) {
        uint32_t new_index = dn_simdhash_bucket_count (bucket_address->suffixes);
        if (new_index < DN_SIMDHASH_BUCKET_CAPACITY) {
            // We found a bucket with space, so claim the first free slot
            dn_simdhash_bucket_set_count (bucket_address->suffixes, new_index + 1);
            dn_simdhash_bucket_set_suffix (bucket_address->suffixes, new_index, suffix);
            bucket_address->keys[new_index] = *key_ptr;
            uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + new_index;
            ((DN_SIMDHASH_VALUE_T *)hash->buffers.values)[value_slot_index] = *value_ptr;
            return DN_SIMDHASH_INSERT_OK;
        }

        // The current bucket is full, so set the cascade flag and try the next bucket.
        dn_simdhash_bucket_set_cascaded (bucket_address->suffixes, 1);
    }

    // If we got here, we had so many hash collisions that we hit the last bucket without finding
    //  a spot for our new item. It's best to just grow and rehash the whole table now.
    return DN_SIMDHASH_INSERT_NEED_TO_GROW;
}

static void
DN_SIMDHASH_REHASH_INTERNAL (dn_simdhash_t *hash, dn_simdhash_buffers_t old_buffers)
{
    DN_SIMDHASH_BUCKET_T *bucket_address = old_buffers.buckets;
    for (
        uint32_t i = 0, bc = old_buffers.buckets_length, value_slot_base = 0;
        i < bc; i++, bucket_address++, value_slot_base += DN_SIMDHASH_BUCKET_CAPACITY
    ) {
        uint32_t c = dn_simdhash_bucket_count(bucket_address->suffixes);
        for (uint32_t j = 0; j < c; j++) {
            DN_SIMDHASH_KEY_T key = bucket_address->keys[j];
            uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
            // FIXME: If there are too many collisions, this could theoretically fail
            // But I'm not sure it's possible in practice, since we just grew the table -
            //  we should have double the previous number of buckets and the items should
            //  be spread out better
            DN_ASSERT(DN_SIMDHASH_TRY_INSERT_INTERNAL(
                hash, key,
                ((DN_SIMDHASH_VALUE_T *)old_buffers.values)[value_slot_base + j],
                key_hash, 0
            ) == DN_SIMDHASH_INSERT_OK);
        }
    }

    dn_simdhash_free_buffers (old_buffers);
}

dn_simdhash_vtable_t DN_SIMDHASH_T_VTABLE = {
    DN_SIMDHASH_FIND_VALUE_INTERNAL,
    DN_SIMDHASH_TRY_INSERT_INTERNAL,
    DN_SIMDHASH_REHASH_INTERNAL,
    DN_SIMDHASH_COMPUTE_HASH_INTERNAL,
};
