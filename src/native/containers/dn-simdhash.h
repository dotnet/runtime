// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_H__
#define __DN_SIMDHASH_H__

#include "dn-utils.h"
#include "dn-allocator.h"

// We reserve the last two bytes of each suffix vector to store data
#define DN_SIMDHASH_MAX_BUCKET_CAPACITY 14
// We use the last two bytes specifically to store item count and cascade flag
#define DN_SIMDHASH_COUNT_SLOT (DN_SIMDHASH_MAX_BUCKET_CAPACITY)
// The cascade flag indicates that an item overflowed from this bucket into the next one
#define DN_SIMDHASH_CASCADED_SLOT (DN_SIMDHASH_MAX_BUCKET_CAPACITY + 1)
// We always use 16-byte-wide vectors (I've tested this, 32-byte vectors are slower)
#define DN_SIMDHASH_VECTOR_WIDTH 16
// We need to make sure suffixes are never zero. A bad hash is more likely to collide
//  at the top bit than at the bottom.
#define DN_SIMDHASH_SUFFIX_SALT 0b10000000

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

static inline uint32_t
dn_simdhash_next_power_of_two (uint32_t value) {
    if (value < 2)
        return 2;
    return 1u << (32 - __builtin_clz (value - 1));
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

#endif // __clang__ || __GNUC__

typedef struct {
    // sizes of current allocations in items (not bytes)
    // so values_length should == (buckets_length * bucket_capacity)
    uint32_t buckets_length, values_length;
    void *buckets;
    void *values;
    dn_allocator_t *allocator;
} dn_simdhash_buffers_t;

typedef struct {
    // type metadata for generic implementation
    uint32_t bucket_capacity, bucket_size_bytes, key_size, value_size;
    // internal state
    uint32_t count;
    dn_simdhash_buffers_t buffers;
} dn_simdhash_t;

// These helpers use .values instead of .vec to avoid generating unnecessary
//  vector loads/stores. Operations that touch these values may not need vectorization

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

static DN_FORCEINLINE(uint8_t)
dn_simdhash_bucket_set_count (dn_simdhash_suffixes bucket, uint8_t value)
{
    bucket.values[DN_SIMDHASH_COUNT_SLOT] = value;
}

static DN_FORCEINLINE(uint8_t)
dn_simdhash_bucket_set_cascaded (dn_simdhash_suffixes bucket, uint8_t value)
{
    bucket.values[DN_SIMDHASH_CASCADED_SLOT] = value;
}

dn_simdhash_suffixes
dn_simdhash_build_search_vector (uint8_t needle);

int
dn_simdhash_find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack);

dn_simdhash_t *
dn_simdhash_new_internal (uint32_t bucket_capacity, uint32_t key_size, uint32_t value_size, uint32_t capacity, dn_allocator_t allocator);

void
dn_simdhash_free (dn_simdhash_t *hash);

void
dn_simdhash_free_buffers (dn_simdhash_buffers_t buffers);

// This will allocate new buffers. The old buffers are your responsibility to free.
// You almost certainly want to rehash the table if (result != hash->buffers)
dn_simdhash_buffers_t
dn_simdhash_ensure_capacity_internal (dn_simdhash_t *hash, uint32_t capacity);

#endif // __DN_SIMDHASH_H__
