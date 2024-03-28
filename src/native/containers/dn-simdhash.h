// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_H__
#define __DN_SIMDHASH_H__

#include "dn-utils.h"
#include "dn-allocator.h"

// We reserve the last two bytes of each suffix vector to store data
#define DN_SIMDHASH_MAX_BUCKET_CAPACITY 14
// The ideal capacity depends on the size of your keys. For 4-byte keys, it is 12.
#define DN_SIMDHASH_DEFAULT_BUCKET_CAPACITY 12
// We use the last two bytes specifically to store item count and cascade flag
#define DN_SIMDHASH_COUNT_SLOT (DN_SIMDHASH_MAX_BUCKET_CAPACITY)
// The cascade flag indicates that an item overflowed from this bucket into the next one
#define DN_SIMDHASH_CASCADED_SLOT (DN_SIMDHASH_MAX_BUCKET_CAPACITY + 1)
// We always use 16-byte-wide vectors (I've tested this, 32-byte vectors are slower)
#define DN_SIMDHASH_VECTOR_WIDTH 16
// We need to make sure suffixes are never zero. A bad hash is more likely to collide
//  at the top bit than at the bottom.
#define DN_SIMDHASH_SUFFIX_SALT 0b10000000
// Set a minimum number of buckets when created, regardless of requested capacity
#define DN_SIMDHASH_MIN_BUCKET_COUNT 4
// User-specified capacity values will be increased to this percentage in order
//  to maintain an ideal load factor. FIXME: 140 isn't right
#define DN_SIMDHASH_SIZING_PERCENTAGE 140

#ifdef _MSC_VER
#define DN_FORCEINLINE(RET_TYPE) __forceinline RET_TYPE
#else
#define DN_FORCEINLINE(RET_TYPE) inline RET_TYPE __attribute__((always_inline))
#endif

#if defined(__clang__) || defined (__GNUC__) // vector intrinsics
typedef uint8_t dn_u8x16 __attribute__ ((vector_size (DN_SIMDHASH_VECTOR_WIDTH)));
typedef int8_t dn_i8x16 __attribute__ ((vector_size (DN_SIMDHASH_VECTOR_WIDTH)));

// extract/replace lane opcodes require constant indices on some target architectures,
//  and in some cases it is profitable to do a single-byte memory load/store instead of
//  a full vector load/store, so we expose both layouts as a union
typedef union {
    dn_u8x16 vec;
    uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

static DN_FORCEINLINE(uint32_t)
dn_simdhash_next_power_of_two (uint32_t value) {
    if (value < 2)
        return 2;
    return 1u << (32 - __builtin_clz (value - 1));
}

DN_FORCEINLINE(dn_simdhash_suffixes)
dn_simdhash_build_search_vector (uint8_t needle)
{
    // this produces a splat and then .const, .and in wasm, and the other architectures are fine too
    dn_u8x16 needles = {
        needle, needle, needle, needle, needle, needle, needle, needle,
        needle, needle, needle, needle, needle, needle, needle, needle
    };
    dn_u8x16 mask = {
        ~0, ~0, ~0, ~0, ~0, ~0, ~0, ~0,
        ~0, ~0, ~0, ~0, ~0, ~0, 0, 0
    };
    dn_simdhash_suffixes result;
    result.vec = needles & mask;
    return result;
}

#else // __clang__ || __GNUC__

typedef struct {
    uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

static uint32_t
dn_simdhash_next_power_of_two (uint32_t value) {
    if (value < 2)
        return 2;
    value--;
    value |= value >> 1;
    value |= value >> 2;
    value |= value >> 4;
    value |= value >> 8;
    value |= value >> 16;
    value++;
    return value;
}

DN_FORCEINLINE(dn_simdhash_suffixes)
dn_simdhash_build_search_vector (uint8_t needle)
{
    dn_simdhash_suffixes result;
    for (int i = 0; i < DN_SIMDHASH_VECTOR_WIDTH; i++)
        result.values[i] = (i >= DN_SIMDHASH_MAX_BUCKET_CAPACITY) ? 0 : needle;
    return result;
}

#endif // __clang__ || __GNUC__

typedef struct dn_simdhash_buffers_t {
    // sizes of current allocations in items (not bytes)
    // so values_length should == (buckets_length * bucket_capacity)
    uint32_t buckets_length, values_length;
    uint8_t *buckets;
    uint8_t *values;
    dn_allocator_t *allocator;
} dn_simdhash_buffers_t;

typedef struct dn_simdhash_t dn_simdhash_t;

// The address of a key to insert into the hashtable.
// Unless DN_SIMDHASH_KEY_IS_POINTER, this will be de-referenced.
typedef const void * dn_simdhash_key_ref;
// The address of a value to insert into the hashtable.
// Unless DN_SIMDHASH_VALUE_IS_POINTER, this will be de-referenced.
typedef const void * dn_simdhash_value_ref;

typedef struct dn_simdhash_meta_t {
    // type metadata for generic implementation
    uint32_t bucket_capacity, bucket_size_bytes, key_size, value_size;
    uint8_t key_is_pointer, value_is_pointer;
} dn_simdhash_meta_t;

typedef enum dn_simdhash_insert_result {
    DN_SIMDHASH_INSERT_OK,
    DN_SIMDHASH_INSERT_NEED_TO_GROW,
    DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT,
} dn_simdhash_insert_result;

typedef struct dn_simdhash_vtable_t {
    // Does not free old_buffers, that's your job.
    void (*rehash) (dn_simdhash_t *hash, dn_simdhash_buffers_t old_buffers);
} dn_simdhash_vtable_t;

typedef struct dn_simdhash_t {
    // internal state
    uint32_t count;
    dn_simdhash_buffers_t buffers;
    dn_simdhash_vtable_t vtable;
    dn_simdhash_meta_t meta;
} dn_simdhash_t;

// These helpers use .values instead of .vec to avoid generating unnecessary
//  vector loads/stores. Operations that touch these values may not need vectorization,
//  so it's ideal to just do single-byte memory accesses instead.
static DN_FORCEINLINE(uint8_t)
dn_simdhash_bucket_count (dn_simdhash_suffixes bucket)
{
    return bucket.values[DN_SIMDHASH_COUNT_SLOT];
}

static DN_FORCEINLINE(uint8_t)
dn_simdhash_bucket_is_cascaded (dn_simdhash_suffixes bucket)
{
    return bucket.values[DN_SIMDHASH_CASCADED_SLOT];
}

#define dn_simdhash_bucket_set_suffix(suffixes, slot, value) \
    (suffixes).values[(slot)] = (value)

#define dn_simdhash_bucket_set_count(suffixes, value) \
    (suffixes).values[DN_SIMDHASH_COUNT_SLOT] = (value)

#define dn_simdhash_bucket_set_cascaded(suffixes, value) \
    (suffixes).values[DN_SIMDHASH_CASCADED_SLOT] = (value)

static DN_FORCEINLINE(uint8_t)
dn_simdhash_select_suffix (uint32_t key_hash)
{
    // Extract top 8 bits, then trash the highest one.
    // The lowest bits of the hash are used to select the bucket index.
    return (key_hash >> 24) | DN_SIMDHASH_SUFFIX_SALT;
}

static DN_FORCEINLINE(uint32_t)
dn_simdhash_select_bucket_index (dn_simdhash_buffers_t buffers, uint32_t key_hash)
{
    // This relies on bucket count being a power of two.
    return key_hash & (buffers.buckets_length - 1);
}


// Scans a single bucket for a key with a suffix matching the provided search vector, and
//  returns the index of the first match, if any.
// If there is no match, the result will be out of range (typically -1 or 32).
int
dn_simdhash_find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack);

// Creates a simdhash with the provided configuration metadata, vtable, size, and allocator.
// Be sure you know what you're doing.
dn_simdhash_t *
dn_simdhash_new_internal (dn_simdhash_meta_t meta, dn_simdhash_vtable_t vtable, uint32_t capacity, dn_allocator_t *allocator);

// Frees a simdhash and its associated buffers.
void
dn_simdhash_free (dn_simdhash_t *hash);

// Frees a set of simdhash buffers (returned by ensure_capacity_internal).
void
dn_simdhash_free_buffers (dn_simdhash_buffers_t buffers);

// If a resize happens, this will allocate new buffers and return the old ones.
// It is your responsibility to rehash and then free the old buffers.
dn_simdhash_buffers_t
dn_simdhash_ensure_capacity_internal (dn_simdhash_t *hash, uint32_t capacity);

// Erases the contents of the table, but does not shrink it.
void
dn_simdhash_clear (dn_simdhash_t *hash);

// Returns the actual number of items the table can currently hold.
// It may grow automatically before reaching that point if there are hash collisions.
uint32_t
dn_simdhash_capacity (dn_simdhash_t *hash);

// Returns the number of items currently stored in the table.
uint32_t
dn_simdhash_count (dn_simdhash_t *hash);

// Automatically resizes the table if it is too small to hold the requested number
//  of items. Will not shrink the table if it is already bigger.
void
dn_simdhash_ensure_capacity (dn_simdhash_t *hash, uint32_t capacity);

typedef void (*dn_simdhash_foreach_func) (dn_simdhash_key_ref key, dn_simdhash_value_ref value, void* user_data);

// Iterates over all the key/value pairs in the table and passes each one to your provided
//  callback, along with the user_data pointer you provide.
void
dn_simdhash_foreach (dn_simdhash_t *hash, dn_simdhash_foreach_func func, void *user_data);

#endif // __DN_SIMDHASH_H__
