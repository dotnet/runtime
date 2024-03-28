// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_H__
#error Include dn-simdhash.h first
// HACK: for better language server parsing
#include "dn-simdhash.h"
#endif

#ifndef DN_SIMDHASH_T
#error Expected DN_SIMDHASH_T definition
#endif

#ifndef DN_SIMDHASH_KEY_T
#error Expected DN_SIMDHASH_KEY_T definition
#endif

#ifndef DN_SIMDHASH_KEY_IS_POINTER
#error Expected DN_SIMDHASH_KEY_IS_POINTER to be 0 or 1
#endif

#ifndef DN_SIMDHASH_VALUE_T
#error Expected DN_SIMDHASH_VALUE_T definition
#endif

#ifndef DN_SIMDHASH_VALUE_IS_POINTER
#error Expected DN_SIMDHASH_VALUE_IS_POINTER to be 0 or 1
#endif

#ifndef DN_SIMDHASH_KEY_HASHER
#error Expected DN_SIMDHASH_KEY_HASHER definition
#endif

#ifndef DN_SIMDHASH_KEY_COMPARER
#error Expected DN_SIMDHASH_KEY_COMPARER definition
#endif

#ifndef DN_SIMDHASH_BUCKET_CAPACITY
#define DN_SIMDHASH_BUCKET_CAPACITY DN_SIMDHASH_DEFAULT_BUCKET_CAPACITY
#endif

#if DN_SIMDHASH_KEY_IS_POINTER
#define KEY_REF DN_SIMDHASH_KEY_T
#define REF_KEY(K) K
#define DEREF_KEY(P) P
static_assert(sizeof(DN_SIMDHASH_KEY_T) == sizeof(void *), "You said your key is a pointer, but it's not!");
#else
#define KEY_REF DN_SIMDHASH_KEY_T*
#define REF_KEY(K) &K
#define DEREF_KEY(P) *P
#endif

#if DN_SIMDHASH_VALUE_IS_POINTER
#define VALUE_REF DN_SIMDHASH_VALUE_T
#define REF_VALUE(V) V
#define DEREF_VALUE(P) P
static_assert(sizeof(DN_SIMDHASH_VALUE_T) == sizeof(void *), "You said your value is a pointer, but it's not!");
#else
#define VALUE_REF DN_SIMDHASH_VALUE_T*
#define REF_VALUE(V) &V
#define DEREF_VALUE(P) *P
#endif

// We generate unique names for each specialization so that they will be easy to distinguish
//  when debugging, profiling, or disassembling. Otherwise they would have linker-assigned names
#define DN_SIMDHASH_BUCKET_T DN_SIMDHASH_T ## _bucket
#define DN_SIMDHASH_T_VTABLE DN_SIMDHASH_T ## _vtable
#define DN_SIMDHASH_T_META DN_SIMDHASH_T ## _meta
#define DN_SIMDHASH_SCAN_BUCKET_INTERNAL DN_SIMDHASH_T ## _scan_bucket_internal
#define DN_SIMDHASH_FIND_VALUE_INTERNAL DN_SIMDHASH_T ## _find_value_internal
#define DN_SIMDHASH_TRY_INSERT_INTERNAL DN_SIMDHASH_T ## _try_insert_internal
#define DN_SIMDHASH_REHASH_INTERNAL DN_SIMDHASH_T ## _rehash_internal
#define DN_SIMDHASH_COMPUTE_HASH_INTERNAL DN_SIMDHASH_T ## _compute_hash_internal
#define DN_SIMDHASH_NEW DN_SIMDHASH_T ## _new

static_assert (DN_SIMDHASH_BUCKET_CAPACITY < DN_SIMDHASH_MAX_BUCKET_CAPACITY, "Maximum bucket capacity exceeded");
static_assert (DN_SIMDHASH_BUCKET_CAPACITY > 1, "Bucket capacity too low");

// We set bucket_size_bytes to sizeof() this struct so that we can let the compiler
//  generate the most optimal code possible when we're manipulating pointers to it -
//  that is, it can do mul-by-constant instead of mul-by-(hash->meta.etc)
typedef struct DN_SIMDHASH_BUCKET_T {
    dn_simdhash_suffixes suffixes;
    DN_SIMDHASH_KEY_T keys[DN_SIMDHASH_BUCKET_CAPACITY];
} DN_SIMDHASH_BUCKET_T;

static uint32_t
DN_SIMDHASH_COMPUTE_HASH_INTERNAL (KEY_REF key_ptr)
{
    return DN_SIMDHASH_KEY_HASHER(DEREF_KEY(key_ptr));
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
DN_SIMDHASH_FIND_VALUE_INTERNAL (dn_simdhash_t *hash, KEY_REF key_ptr, uint32_t key_hash)
{
    uint8_t suffix = dn_simdhash_select_suffix(key_hash);
    uint32_t bucket_index = dn_simdhash_select_bucket_index(hash->buffers, key_hash);
    DN_SIMDHASH_BUCKET_T *bucket_address = (DN_SIMDHASH_BUCKET_T *)dn_simdhash_address_of_bucket(hash->meta, hash->buffers, bucket_index);
    DN_SIMDHASH_KEY_T key = DEREF_KEY(key_ptr);
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

static dn_simdhash_insert_result
DN_SIMDHASH_TRY_INSERT_INTERNAL (dn_simdhash_t *hash, KEY_REF key_ptr, VALUE_REF value_ptr, uint32_t key_hash, uint8_t ensure_not_present)
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
            bucket_address->keys[new_index] = DEREF_KEY(key_ptr);
            uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + new_index;
            ((DN_SIMDHASH_VALUE_T *)hash->buffers.values)[value_slot_index] = DEREF_VALUE(value_ptr);
            return DN_SIMDHASH_INSERT_OK;
        }

        // The current bucket is full, so set the cascade flag and try the next bucket.
        dn_simdhash_bucket_set_cascaded (bucket_address->suffixes, 1);
    }

    // If we got here, we had so many hash collisions that we hit the last bucket without finding
    //  a spot for our new item. It's best to just grow and rehash the whole table now.
    // TODO: Wrap around to the first bucket, like S.C.G.Dictionary does? I don't like it, but it
    //  would reduce memory usage for the worst case scenario.
    return DN_SIMDHASH_INSERT_NEED_TO_GROW;
}

static void
DN_SIMDHASH_REHASH_INTERNAL (dn_simdhash_t *hash, dn_simdhash_buffers_t old_buffers)
{
    DN_SIMDHASH_BUCKET_T *bucket_address = (void *)old_buffers.buckets;
    for (
        uint32_t i = 0, bc = old_buffers.buckets_length, value_slot_base = 0;
        i < bc; i++, bucket_address++, value_slot_base += DN_SIMDHASH_BUCKET_CAPACITY
    ) {
        uint32_t c = dn_simdhash_bucket_count(bucket_address->suffixes);
        for (uint32_t j = 0; j < c; j++) {
            KEY_REF key = REF_KEY(bucket_address->keys[j]);
            uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DEREF_KEY(key));
            // FIXME: If there are too many collisions, this could theoretically fail
            // But I'm not sure it's possible in practice, since we just grew the table -
            //  we should have double the previous number of buckets and the items should
            //  be spread out better
            dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(
                hash, key,
                REF_VALUE(
                    ((DN_SIMDHASH_VALUE_T *)old_buffers.values)[value_slot_base + j]
                ),
                key_hash, 0
            );
            // FIXME: Why doesn't assert(ok) work here? Clang says it's unused
            if (ok != DN_SIMDHASH_INSERT_OK)
                assert(0);
        }
    }

    dn_simdhash_free_buffers (old_buffers);
}

// We expose these tables instead of making them static, just in case you want to use
//  them directly for some reason

// TODO: Store this by-reference instead of inline in the hash?
dn_simdhash_vtable_t DN_SIMDHASH_T_VTABLE = {
    // HACK: Cast these fn pointers to (void *), because their signatures
    //  aren't exact matches, but they're compatible.
    (void *)DN_SIMDHASH_FIND_VALUE_INTERNAL,
    (void *)DN_SIMDHASH_TRY_INSERT_INTERNAL,
    (void *)DN_SIMDHASH_REHASH_INTERNAL,
    (void *)DN_SIMDHASH_COMPUTE_HASH_INTERNAL,
};

// While we've inlined these constants into the specialized code generated above,
//  the generic code in dn-simdhash.c needs them, so we put them in this meta header
//  that lives inside every hash instance. (TODO: Store it by-reference?)
dn_simdhash_meta_t DN_SIMDHASH_T_META = {
    DN_SIMDHASH_BUCKET_CAPACITY,
    sizeof(DN_SIMDHASH_BUCKET_T),
    sizeof(DN_SIMDHASH_KEY_T),
    sizeof(DN_SIMDHASH_VALUE_T),
};

// HACK: We don't expect the caller to pre-declare this in the specialization file
dn_simdhash_t *
DN_SIMDHASH_NEW (uint32_t capacity, dn_allocator_t *allocator);

dn_simdhash_t *
DN_SIMDHASH_NEW (uint32_t capacity, dn_allocator_t *allocator)
{
    return dn_simdhash_new_internal(DN_SIMDHASH_T_META, DN_SIMDHASH_T_VTABLE, capacity, allocator);
}
