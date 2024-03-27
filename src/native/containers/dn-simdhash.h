// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_H__
#define __DN_SIMDHASH_H__

#include "dn-utils.h"

#define DN_SIMDHASH_MAX_BUCKET_CAPACITY 14
#define DN_SIMDHASH_COUNT_SLOT (DN_SIMDHASH_MAX_BUCKET_CAPACITY)
#define DN_SIMDHASH_CASCADED_SLOT (DN_SIMDHASH_MAX_BUCKET_CAPACITY + 1)
#define DN_SIMDHASH_VECTOR_WIDTH 16

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

static int
dn_simdhash_next_power_of_two (uint32_t value) {
    if (value < 2)
        return 2;
    return 1 << (32 - __builtin_clz (value - 1));
}

#else // __clang__ || __GNUC__

typedef struct {
    uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

static int
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

static inline uint8_t
dn_simdhash_bucket_count (dn_simdhash_suffixes bucket)
{
    return bucket.values[DN_SIMDHASH_COUNT_SLOT];
}

static inline uint8_t
dn_simdhash_bucket_is_cascaded (dn_simdhash_suffixes bucket)
{
    return bucket.values[DN_SIMDHASH_CASCADED_SLOT];
}

dn_simdhash_suffixes
dn_simdhash_build_search_vector (uint8_t needle);

int
dn_simdhash_find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack);

#endif // __DN_SIMDHASH_H__
