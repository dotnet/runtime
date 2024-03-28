// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

#if defined(__clang__) || defined (__GNUC__) // vector intrinsics

#if defined(__wasm_simd128__)
#include <wasm_simd128.h>
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
#include <emmintrin.h>
#elif defined(__ARM_NEON)
#include <arm_neon.h>
#elif defined(__wasm)
#warning Building dn_simdhash for WASM without -msimd128! Performance will be terrible!
#else
#warning Unsupported architecture for dn_simdhash! Performance will be terrible!
#endif

// FIXME: How can we inline this without polluting every consumer with intrinsic includes?
int
dn_simdhash_find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack)
{
#if defined(__wasm_simd128__)
	dn_simdhash_suffixes match_vector;
	match_vector.vec = wasm_i8x16_eq(needle.vec, haystack.vec);
	return __builtin_ctz(wasm_i8x16_bitmask(match_vector.vec));
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
	dn_simdhash_suffixes match_vector;
	// Completely untested.
	match_vector.vec = _mm_cmpeq_epi8(needle.vec, haystack.vec);
	return __builtin_ctz(_mm_movemask_epi8(match_vector.vec));
#elif defined(__ARM_NEON)
	dn_simdhash_suffixes match_vector;
	// Completely untested.
	static const dn_simdhash_suffixes byte_mask = {
		1, 2, 4, 8, 16, 32, 64, 128, 1, 2, 4, 8, 16, 32, 64, 128
	};
	union {
		uint8_t b[4];
		uint32_t u;
	} msb;
	match_vector.vec = vceqq_u8(needle.vec, haystack.vec);
	dn_simdhash_suffixes masked;
	masked.vec = vandq_u8(match_vector.vec, byte_mask.vec);
	_msb.b[0] = vaddv_u8(vget_low_u8(masked.vec));
	_msb.b[1] = vaddv_u8(vget_high_u8(masked.vec));
	return __builtin_ctz(_msb.u);
#else
	// Completely untested.
	for (int i = 0; i < dn_simdhash_bucket_count(haystack); i++)
		if (needle.values[i] == haystack.values[i])
			return i;

	return 32;
#endif
}

#else // __clang__ || __GNUC__

// FIXME: How can we inline this without polluting every consumer with intrinsic includes?
int
dn_simdhash_find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack)
{
	// FIXME: Do this using intrinsics on MSVC. Seems complicated.

	// Completely untested.
	for (int i = 0; i < dn_simdhash_bucket_count(haystack); i++)
		if (needle.values[i] == haystack.values[i])
			return i;

	return 32;
}

#endif // __clang__ || __GNUC__

dn_simdhash_t *
dn_simdhash_new_internal (dn_simdhash_meta_t meta, dn_simdhash_vtable_t vtable, uint32_t capacity, dn_allocator_t *allocator)
{
	dn_simdhash_t *result = (dn_simdhash_t *)dn_allocator_alloc(allocator, sizeof(dn_simdhash_t));
	memset(result, 0, sizeof(dn_simdhash_t));

	assert((meta.bucket_capacity > 1) && (meta.bucket_capacity <= DN_SIMDHASH_MAX_BUCKET_CAPACITY));
	assert(meta.key_size > 0);
	assert(meta.bucket_size_bytes >= (DN_SIMDHASH_VECTOR_WIDTH + (meta.bucket_capacity * meta.key_size)));
	result->meta = meta;
	result->vtable = vtable;
	result->buffers.allocator = allocator;

	// FIXME: Why does clang insist old_buffers is unused here?
	/*
	dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(result, capacity);
	assert(old_buffers.buckets == NULL);
	assert(old_buffers.values == NULL);
	*/

	capacity = capacity * DN_SIMDHASH_SIZING_PERCENTAGE / 100;
	dn_simdhash_ensure_capacity_internal(result, capacity);

	return result;
}

void
dn_simdhash_free (dn_simdhash_t *hash)
{
	assert(hash);
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
	assert(hash);
	uint32_t bucket_count = (capacity + hash->meta.bucket_capacity - 1) / hash->meta.bucket_capacity;
	// FIXME: Only apply this when capacity == 0?
	if (bucket_count < DN_SIMDHASH_MIN_BUCKET_COUNT)
		bucket_count = DN_SIMDHASH_MIN_BUCKET_COUNT;
	// Bucket count must be a power of two (this enables more efficient hashcode -> bucket mapping)
	bucket_count = dn_simdhash_next_power_of_two(bucket_count);
	uint32_t value_count = bucket_count * hash->meta.bucket_capacity;

	dn_simdhash_buffers_t result = { 0, };
	if (bucket_count <= hash->buffers.buckets_length) {
		assert(value_count <= hash->buffers.values_length);
		return result;
	}

	/*
	printf (
		"growing from %d bucket(s) to %d bucket(s) for requested capacity %d (actual capacity %d)\n",
		hash->buffers.buckets_length, bucket_count,
		capacity, value_count
	);
	*/
	// Store old buffers so caller can rehash and then free them
	result = hash->buffers;

	hash->grow_at_count = value_count * 100 / DN_SIMDHASH_SIZING_PERCENTAGE;
	hash->buffers.buckets_length = bucket_count;
	hash->buffers.values_length = value_count;
	// FIXME: 16-byte aligned allocation
	hash->buffers.buckets = dn_allocator_alloc(hash->buffers.allocator, bucket_count * hash->meta.bucket_size_bytes);
	hash->buffers.values = dn_allocator_alloc(hash->buffers.allocator, value_count * hash->meta.value_size);

	return result;
}

void
dn_simdhash_clear (dn_simdhash_t *hash)
{
	assert(hash);
	hash->count = 0;
	memset(hash->buffers.buckets, 0, hash->buffers.buckets_length * hash->meta.bucket_size_bytes);
	// Clearing the values is technically optional, so we could skip this for performance
	memset(hash->buffers.values, 0, hash->buffers.values_length * hash->meta.value_size);
}

uint32_t
dn_simdhash_capacity (dn_simdhash_t *hash)
{
	assert(hash);
	return hash->buffers.buckets_length * hash->meta.bucket_capacity;
}

uint32_t
dn_simdhash_count (dn_simdhash_t *hash)
{
	assert(hash);
	return hash->count;
}

void
dn_simdhash_ensure_capacity (dn_simdhash_t *hash, uint32_t capacity)
{
	capacity = capacity * DN_SIMDHASH_SIZING_PERCENTAGE / 100;
	dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(hash, capacity);
	if (old_buffers.buckets) {
		hash->vtable.rehash(hash, old_buffers);
		dn_simdhash_free_buffers(old_buffers);
	}
}

static DN_FORCEINLINE(void *)
deref_raw (void * src)
{
	return *((void**)src);
}

void
dn_simdhash_foreach (dn_simdhash_t *hash, dn_simdhash_foreach_func func, void *user_data)
{
	assert(hash);
	dn_simdhash_meta_t meta = hash->meta;

	uint8_t *bucket = hash->buffers.buckets,
		*values = hash->buffers.values;
	uint32_t values_step = meta.value_size * meta.bucket_capacity;

	for (
		uint32_t i = 0; i < hash->buffers.buckets_length;
		i++, bucket += meta.bucket_size_bytes, values += values_step
	) {
		uint32_t count = dn_simdhash_bucket_count(*(dn_simdhash_suffixes *)bucket);
		uint8_t *keys = bucket + sizeof(dn_simdhash_suffixes);
		if (meta.key_is_pointer && meta.value_is_pointer) {
			for (uint32_t j = 0; j < count; j++)
				func(deref_raw(keys + (j * meta.key_size)), deref_raw(values + (j * meta.value_size)), user_data);
		} else if (meta.key_is_pointer) {
			for (uint32_t j = 0; j < count; j++)
				func(deref_raw(keys + (j * meta.key_size)), values + (j * meta.value_size), user_data);
		} else if (meta.value_is_pointer) {
			for (uint32_t j = 0; j < count; j++)
				func(keys + (j * meta.key_size), deref_raw(values + (j * meta.value_size)), user_data);
		} else {
			for (uint32_t j = 0; j < count; j++)
				func(keys + (j * meta.key_size), values + (j * meta.value_size), user_data);
		}
	}
}
