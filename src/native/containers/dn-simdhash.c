// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

#if defined(__clang__) || defined (__GNUC__) // vector intrinsics

#if defined(__wasm)
#include <wasm_simd128.h>
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
#include <emmintrin.h>
#elif defined(__ARM_NEON)
#include <arm_neon.h>
#else
#error Unsupported architecture for dn_simdhash
#endif

dn_simdhash_suffixes
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

int
dn_simdhash_find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack)
{
    dn_simdhash_suffixes match_vector;
    uint32_t msb;

#if defined(__wasm)
    match_vector.vec = wasm_i8x16_eq(needle.vec, haystack.vec);
    msb = wasm_i8x16_bitmask(match_vector.vec);
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
    // Completely untested.
    match_vector.vec = _mm_cmpeq_epi8(needle.vec, haystack.vec);
    msb = _mm_movemask_epi8(match_vector.vec);
#elif defined(__ARM_NEON)
    // Completely untested.
	static const dn_simdhash_suffixes byte_mask = {
        1, 2, 4, 8, 16, 32, 64, 128, 1, 2, 4, 8, 16, 32, 64, 128
    };
    union {
        uint8_t b[4];
        uint32_t u;
    } _msb;
    match_vector.vec = vceqq_u8(needle.vec, haystack.vec);
	dn_simdhash_suffixes masked;
    masked.vec = vandq_u8(match_vector.vec, byte_mask.vec);
	_msb.b[0] = vaddv_u8(vget_low_u8(masked.vec));
    _msb.b[1] = vaddv_u8(vget_high_u8(masked.vec));
    msb = _msb.u;
#else
    // Completely untested.
    for (int i = 0; i < dn_simdhash_bucket_count(haystack); i++)
        if (needle.values[i] == haystack.values[i])
            return i;

    return 32;
#endif

    int first_match_index = __builtin_ctz(msb);
    return first_match_index;
}

#else // __clang__ || __GNUC__

#error Unsupported compiler for dn_simdhash

#endif // __clang__ || __GNUC__

dn_simdhash_t *
dn_simdhash_new_internal (uint32_t bucket_capacity, uint32_t key_size, uint32_t value_size, uint32_t capacity, dn_allocator_t *allocator)
{
    dn_simdhash_t *result = dn_allocator_alloc(allocator, sizeof(dn_simdhash_t));
    memset(result, 0, sizeof(dn_simdhash_t));

    DN_ASSERT((bucket_capacity > 1) && (bucket_capacity <= DN_SIMDHASH_MAX_BUCKET_CAPACITY));
    result->bucket_size_bytes = DN_SIMDHASH_VECTOR_WIDTH + (bucket_capacity * key_size);
    result->bucket_capacity = bucket_capacity;
    result->key_size = key_size;
    result->value_size = value_size;
    result->buffers.allocator = allocator;

    dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(result, capacity);
    DN_ASSERT(old_buffers.buckets == NULL);
    DN_ASSERT(old_buffers.values == NULL);

    return result;
}

void
dn_simdhash_free (dn_simdhash_t *hash)
{
    DN_ASSERT(hash);
    dn_simdhash_buffers_t buffers = hash->buffers;
    memset(hash, 0, sizeof(dn_simdhash_t));
    dn_simdhash_free_buffers(buffers);
}

void
dn_simdhash_free_buffers (dn_simdhash_buffers_t buffers)
{
    if (buffers.buckets)
        dn_allocator_free(buffers.allocator, buffers.buckets);
    if (buffers.values)
        dn_allocator_free(buffers.allocator, buffers.values);
}

dn_simdhash_buffers_t
dn_simdhash_ensure_capacity_internal (dn_simdhash_t *hash, uint32_t capacity)
{
    DN_ASSERT(hash);
    uint32_t bucket_count = (capacity + hash->bucket_capacity - 1) / hash->bucket_capacity;
    // Bucket count must be a power of two (this enables more efficient hashcode -> bucket mapping)
    bucket_count = dn_simdhash_next_power_of_two(bucket_count);
    uint32_t value_count = bucket_count * hash->bucket_capacity;

    if (bucket_count <= hash->buffers.buckets_length) {
        DN_ASSERT(value_count <= hash->buffers.values_length);
        return;
    }

    // Store old buffers so caller can rehash and then free them
    dn_simdhash_buffers_t result = hash->buffers;

    hash->buffers.buckets_length = bucket_count;
    hash->buffers.values_length = value_count;
    hash->buffers.buckets = dn_allocator_alloc(hash->buffers.allocator, bucket_count * hash->bucket_size_bytes);
    hash->buffers.values = dn_allocator_alloc(hash->buffers.allocator, value_count * hash->value_size);

    return result;
}
