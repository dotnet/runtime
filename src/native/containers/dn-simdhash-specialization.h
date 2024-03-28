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
#error Expected DN_SIMDHASH_KEY_HASHER definition with signature: uint32_t (KEY_T key)
#endif

#ifndef DN_SIMDHASH_KEY_EQUALS
#error Expected DN_SIMDHASH_KEY_EQUALS definition with signature: int (KEY_T lhs, KEY_T rhs) that returns 1 for match
#endif

#ifndef DN_SIMDHASH_BUCKET_CAPACITY
#define DN_SIMDHASH_BUCKET_CAPACITY DN_SIMDHASH_DEFAULT_BUCKET_CAPACITY
#endif

// For keys or values of non-pointer-sized types (for example, an int64 key on a 32-bit platform),
//  we want to store the whole thing inside the hash so that it's not necessary to do tons of mallocs
//  and frees when managing the contents of the table.
// For keys or values of pointer-sized types, we can just let the user pass in arbitrary pointer-
//  sized blobs of data and store those pointers directly instead.
// Ultimately this is all just to allow you to easily store 'const char *' in this thing.

#if DN_SIMDHASH_KEY_IS_POINTER
static_assert(sizeof(DN_SIMDHASH_KEY_T) == sizeof(void *), "You said your key is a pointer, but it's not!");
#else
#endif

#if DN_SIMDHASH_VALUE_IS_POINTER
static_assert(sizeof(DN_SIMDHASH_VALUE_T) == sizeof(void *), "You said your value is a pointer, but it's not!");
#else
#endif

#include "dn-simdhash-specialization-declarations.h"

static_assert (DN_SIMDHASH_BUCKET_CAPACITY < DN_SIMDHASH_MAX_BUCKET_CAPACITY, "Maximum bucket capacity exceeded");
static_assert (DN_SIMDHASH_BUCKET_CAPACITY > 1, "Bucket capacity too low");

// We set bucket_size_bytes to sizeof() this struct so that we can let the compiler
//  generate the most optimal code possible when we're manipulating pointers to it -
//  that is, it can do mul-by-constant instead of mul-by-(hash->meta.etc)
typedef struct bucket_t {
	dn_simdhash_suffixes suffixes;
	DN_SIMDHASH_KEY_T keys[DN_SIMDHASH_BUCKET_CAPACITY];
} bucket_t;

static DN_FORCEINLINE(bucket_t *)
address_of_bucket (dn_simdhash_buffers_t buffers, uint32_t bucket_index)
{
	return &((bucket_t *)buffers.buckets)[bucket_index];
}

static DN_FORCEINLINE(DN_SIMDHASH_VALUE_T *)
address_of_value (dn_simdhash_buffers_t buffers, uint32_t value_slot_index)
{
	return &((DN_SIMDHASH_VALUE_T *)buffers.values)[value_slot_index];
}


#if defined(__clang__) || defined (__GNUC__) // use vector intrinsics

#if defined(__wasm_simd128__)
#include <wasm_simd128.h>
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
#include <emmintrin.h>
#elif defined(__ARM_NEON)
#include <arm_neon.h>
#elif defined(__wasm)
#pragma message("WARNING: Building dn_simdhash for WASM without -msimd128! Performance will be terrible!")
#else
#pragma message("WARNING: Unsupported architecture for dn_simdhash! Performance will be terrible!")
#endif

static DN_FORCEINLINE(int)
ctz (uint32_t value)
{
	// __builtin_ctz is undefined for 0
	if (value == 0)
		return 32;
	return __builtin_ctz(value);
}

static DN_FORCEINLINE(int)
find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack)
{
	// FIXME: This code is worse because according to gcc, ctz/clz are undefined for 0.
#if defined(__wasm_simd128__)
	dn_simdhash_suffixes match_vector;
	match_vector.vec = wasm_i8x16_eq(needle.vec, haystack.vec);
	return ctz(wasm_i8x16_bitmask(match_vector.vec));
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
	dn_simdhash_suffixes match_vector;
	match_vector.vec = _mm_cmpeq_epi8(needle.vec, haystack.vec);
	return ctz(_mm_movemask_epi8(match_vector.vec));
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
	msb.b[0] = vaddv_u8(vget_low_u8(masked.vec));
	msb.b[1] = vaddv_u8(vget_high_u8(masked.vec));
	return ctz(msb.u);
#else
	for (uint32_t i = 0, c = dn_simdhash_bucket_count(haystack); i < c; i++)
		if (needle.values[i] == haystack.values[i])
			return i;

	return 32;
#endif
}

#else // __clang__ || __GNUC__

#pragma message("WARNING: Building dn_simdhash for MSVC without SIMD intrinsics! Performance will be terrible!")

int
find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack)
{
	// FIXME: Do this using intrinsics on MSVC. Seems complicated since there's no __builtin_ctz.
	for (uint32_t i = 0, c = dn_simdhash_bucket_count(haystack); i < c; i++)
		if (needle.values[i] == haystack.values[i])
			return i;

	return 32;
}

#endif // __clang__ || __GNUC__


// This is split out into a helper so we can eventually reuse it for more efficient add/remove
static DN_FORCEINLINE(int)
DN_SIMDHASH_SCAN_BUCKET_INTERNAL(DN_SIMDHASH_T) (bucket_t *bucket, DN_SIMDHASH_KEY_T needle, dn_simdhash_suffixes search_vector)
{
	dn_simdhash_suffixes suffixes = bucket->suffixes;
	int index = find_first_matching_suffix (search_vector, suffixes);
	// FIXME: This shouldn't be necessary.
	if (index > DN_SIMDHASH_BUCKET_CAPACITY)
		return -1;
	DN_SIMDHASH_KEY_T *key = &bucket->keys[index];

	for (int count = dn_simdhash_bucket_count (suffixes); index < count; index++, key++) {
		if (DN_SIMDHASH_KEY_EQUALS (needle, *key))
			return index;
	}

	return -1;
}

// Helper macro so that we can optimize and change scan logic more easily

#define BEGIN_SCAN_BUCKETS(initial_index, bucket_index, bucket_address) \
	{ \
		uint32_t bucket_index = initial_index; \
		bucket_t *bucket_address; \
		do { \
			bucket_address = address_of_bucket(buffers, bucket_index);

#define END_SCAN_BUCKETS(initial_index, bucket_index, bucket_address) \
			bucket_index++; \
			/* Wrap around if we hit the last bucket. */ \
			if (bucket_index >= buffers.buckets_length) \
				bucket_index = 0; \
		} while (bucket_index != initial_index); \
	}

static DN_SIMDHASH_VALUE_T *
DN_SIMDHASH_FIND_VALUE_INTERNAL(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(buffers, key_hash);
	dn_simdhash_suffixes search_vector = dn_simdhash_build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(DN_SIMDHASH_T)(bucket_address, key, search_vector);
		if (index_in_bucket >= 0) {
			uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + index_in_bucket;
			return address_of_value(buffers, value_slot_index);
		}

		if (!dn_simdhash_bucket_is_cascaded(bucket_address->suffixes))
			return NULL;
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)

	return NULL;
}

static dn_simdhash_insert_result
DN_SIMDHASH_TRY_INSERT_INTERNAL(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T value, uint8_t ensure_not_present)
{
	// HACK: Early out. Better to grow without scanning here.
	// We're comparing with the computed grow_at_count threshold to maintain an appropriate load factor
	if (hash->count >= hash->grow_at_count) {
		// printf ("hash->count %d >= hash->grow_at_count %d\n", hash->count, hash->grow_at_count);
		return DN_SIMDHASH_INSERT_NEED_TO_GROW;
	}

	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(hash->buffers, key_hash);
	dn_simdhash_suffixes search_vector = dn_simdhash_build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		// If necessary, check the current bucket for the key
		if (ensure_not_present) {
			int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(DN_SIMDHASH_T)(bucket_address, key, search_vector);
			if (index_in_bucket >= 0)
				return DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT;
		}

		// The current bucket doesn't contain the key, or duplicate checks are disabled (for rehashing),
		//  so attempt to insert into the bucket
		uint8_t new_index = dn_simdhash_bucket_count (bucket_address->suffixes);
		if (new_index < DN_SIMDHASH_BUCKET_CAPACITY) {
			// We found a bucket with space, so claim the first free slot
			dn_simdhash_bucket_set_count (bucket_address->suffixes, new_index + 1);
			dn_simdhash_bucket_set_suffix (bucket_address->suffixes, new_index, suffix);
			bucket_address->keys[new_index] = key;
			uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + new_index;
			*address_of_value(buffers, value_slot_index) = value;
			// printf("Inserted [%zd, %zd] in bucket %d at index %d\n", key, value, bucket_index, new_index);
			return DN_SIMDHASH_INSERT_OK;
		}

		// The current bucket is full, so set the cascade flag and try the next bucket.
		dn_simdhash_bucket_set_cascaded (bucket_address->suffixes, 1);
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)

	// If we got here, we had so many hash collisions that we hit the last bucket without finding
	//  a spot for our new item. It's best to just grow and rehash the whole table now.
	// TODO: Wrap around to the first bucket, like S.C.G.Dictionary does? I don't like it, but it
	//  would reduce memory usage for the worst case scenario.
	// printf("Scanned from bucket %d without finding space, growing\n", first_bucket_index);
	return DN_SIMDHASH_INSERT_NEED_TO_GROW;
}

static void
DN_SIMDHASH_REHASH_INTERNAL(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, dn_simdhash_buffers_t old_buffers)
{
	bucket_t *bucket_address = address_of_bucket(old_buffers, 0);
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
			dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(DN_SIMDHASH_T)(
				hash, key, key_hash,
				*address_of_value(old_buffers, value_slot_base + j),
				0
			);
			// FIXME: Why doesn't assert(ok) work here? Clang says it's unused
			if (ok != DN_SIMDHASH_INSERT_OK)
				assert(0);
		}
	}
}

// We expose these tables instead of making them static, just in case you want to use
//  them directly for some reason

// TODO: Store this by-reference instead of inline in the hash?
dn_simdhash_vtable_t DN_SIMDHASH_T_VTABLE(DN_SIMDHASH_T) = {
	DN_SIMDHASH_REHASH_INTERNAL(DN_SIMDHASH_T),
};

// While we've inlined these constants into the specialized code generated above,
//  the generic code in dn-simdhash.c needs them, so we put them in this meta header
//  that lives inside every hash instance. (TODO: Store it by-reference?)
dn_simdhash_meta_t DN_SIMDHASH_T_META(DN_SIMDHASH_T) = {
	DN_SIMDHASH_BUCKET_CAPACITY,
	sizeof(bucket_t),
	sizeof(DN_SIMDHASH_KEY_T),
	sizeof(DN_SIMDHASH_VALUE_T),
	DN_SIMDHASH_KEY_IS_POINTER,
	DN_SIMDHASH_VALUE_IS_POINTER
};

DN_SIMDHASH_T_PTR(DN_SIMDHASH_T)
DN_SIMDHASH_NEW(DN_SIMDHASH_T) (uint32_t capacity, dn_allocator_t *allocator)
{
	return dn_simdhash_new_internal(DN_SIMDHASH_T_META(DN_SIMDHASH_T), DN_SIMDHASH_T_VTABLE(DN_SIMDHASH_T), capacity, allocator);
}

uint8_t
DN_SIMDHASH_TRY_ADD(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T value)
{
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
	return DN_SIMDHASH_TRY_ADD_WITH_HASH(DN_SIMDHASH_T)(hash, key, key_hash, value);
}

uint8_t
DN_SIMDHASH_TRY_ADD_WITH_HASH(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T value)
{
	assert(hash);
	while (true) {
		dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(DN_SIMDHASH_T)(hash, key, key_hash, value, 1);

		switch (ok) {
			case DN_SIMDHASH_INSERT_OK:
				hash->count++;
				return 1;
			case DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT:
				return 0;
			case DN_SIMDHASH_INSERT_NEED_TO_GROW: {
				// We may have already grown once and still not had enough space, due to collisions
				//  so we want to ensure we increase the *capacity* beyond its current value, not
				//  ensure a capacity of count + 1
				// We use ensure_capacity_internal because the public one applies the sizing percentage
				dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(hash, dn_simdhash_capacity(hash) + 1);
				if (old_buffers.buckets) {
					DN_SIMDHASH_REHASH_INTERNAL(DN_SIMDHASH_T)(hash, old_buffers);
					dn_simdhash_free_buffers(old_buffers);
				}
				continue;
			}
			default:
				assert(0);
				return 0;
		}
	}
}

uint8_t
DN_SIMDHASH_TRY_GET_VALUE(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T *result)
{
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
	return DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH(DN_SIMDHASH_T)(hash, key, key_hash, result);
}

uint8_t
DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T *result)
{
	assert(hash);
	DN_SIMDHASH_VALUE_T *value_ptr = DN_SIMDHASH_FIND_VALUE_INTERNAL(DN_SIMDHASH_T)(hash, key, key_hash);
	if (!value_ptr)
		return 0;
	if (result)
		*result = *value_ptr;
	return 1;
}

uint8_t
DN_SIMDHASH_TRY_REMOVE(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key)
{
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
	return DN_SIMDHASH_TRY_REMOVE_WITH_HASH(DN_SIMDHASH_T)(hash, key, key_hash);
}

uint8_t
DN_SIMDHASH_TRY_REMOVE_WITH_HASH(DN_SIMDHASH_T) (DN_SIMDHASH_T_PTR(DN_SIMDHASH_T) hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
	assert(hash);

	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(buffers, key_hash);
	dn_simdhash_suffixes search_vector = dn_simdhash_build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(DN_SIMDHASH_T)(bucket_address, key, search_vector);
		if (index_in_bucket >= 0) {
			// We found the item. Replace it with the last item in the bucket, then erase
			//  the last item in the bucket. This ensures sequential scans still work.
			uint8_t bucket_count = dn_simdhash_bucket_count(bucket_address->suffixes),
				replacement_index_in_bucket = bucket_count - 1;
			uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + index_in_bucket,
				replacement_value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + replacement_index_in_bucket;

			hash->count--;

			// Update count first
			dn_simdhash_bucket_set_count(bucket_address->suffixes, bucket_count - 1);
			// Rotate replacement suffix from the end of the bucket to here
			dn_simdhash_bucket_set_suffix(
				bucket_address->suffixes, index_in_bucket,
				bucket_address->suffixes.values[replacement_index_in_bucket]
			);
			// Zero replacement suffix's old slot so it won't produce false positives in scans
			dn_simdhash_bucket_set_suffix(
				bucket_address->suffixes, replacement_index_in_bucket, 0
			);
			// Rotate replacement value from the end of the bucket to here
			*address_of_value(buffers, value_slot_index) = *address_of_value(buffers, replacement_value_slot_index);
			// Rotate replacement key from the end of the bucket to here
			bucket_address->keys[index_in_bucket] = bucket_address->keys[replacement_index_in_bucket];
			// Erase replacement key/value's old slots
			// TODO: Skip these for performance?
			memset(&bucket_address->keys[replacement_index_in_bucket], 0, sizeof(DN_SIMDHASH_KEY_T));
			memset(address_of_value(buffers, replacement_value_slot_index), 0, sizeof(DN_SIMDHASH_VALUE_T));

			return 1;
		}

		if (!dn_simdhash_bucket_is_cascaded(bucket_address->suffixes))
			return 0;
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)

	return 0;
}
