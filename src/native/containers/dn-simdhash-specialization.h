// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef __DN_SIMDHASH_SPECIALIZATION_H__
#error Specialization header already included
#else
#define __DN_SIMDHASH_SPECIALIZATION_H__
#endif

#include "dn-simdhash.h"
#include "dn-simdhash-arch.h"

#ifndef DN_SIMDHASH_T
#error Expected DN_SIMDHASH_T definition i.e. dn_simdhash_string_ptr
#endif

#ifndef DN_SIMDHASH_KEY_T
#error Expected DN_SIMDHASH_KEY_T definition i.e. const char *
#endif

#ifndef DN_SIMDHASH_VALUE_T
#error Expected DN_SIMDHASH_VALUE_T definition i.e. int
#endif

#ifndef DN_SIMDHASH_KEY_HASHER
#error Expected DN_SIMDHASH_KEY_HASHER definition with signature: uint32_t (KEY_T key)
#endif

#ifndef DN_SIMDHASH_KEY_EQUALS
#error Expected DN_SIMDHASH_KEY_EQUALS definition with signature: int (KEY_T lhs, KEY_T rhs) that returns 1 for match
#endif

#ifndef DN_SIMDHASH_BUCKET_CAPACITY
// TODO: Find some way to automatically select an ideal bucket capacity based on key size.
// Some sort of trick using _Generic?
#define DN_SIMDHASH_BUCKET_CAPACITY DN_SIMDHASH_DEFAULT_BUCKET_CAPACITY
#endif

#include "dn-simdhash-specialization-declarations.h"

static_assert (DN_SIMDHASH_BUCKET_CAPACITY <= DN_SIMDHASH_MAX_BUCKET_CAPACITY, "Maximum bucket capacity exceeded");
static_assert (DN_SIMDHASH_BUCKET_CAPACITY > 1, "Bucket capacity too low");

// We set bucket_size_bytes to sizeof() this struct so that we can let the compiler
//  generate the most optimal code possible when we're manipulating pointers to it -
//  that is, it can do mul-by-constant instead of mul-by-(hash->meta.etc)
// We use memcpy to do an unaligned load when reading dn_simdhash_suffixes, but it's
//  still ideal to align instances of bucket_t to match the vector width, so that
//  loads are less likely to span two cache lines.
#ifdef _MSC_VER
typedef struct __declspec(align(DN_SIMDHASH_VECTOR_WIDTH)) bucket_t {
#else
typedef struct bucket_t {
#endif
	dn_simdhash_suffixes suffixes;
	DN_SIMDHASH_KEY_T keys[DN_SIMDHASH_BUCKET_CAPACITY];
}
#if defined(__clang__) || defined (__GNUC__)
__attribute__((__aligned__(DN_SIMDHASH_VECTOR_WIDTH))) bucket_t;
#else
bucket_t;
#endif

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

// This helper is used to locate the first matching key in a given bucket, so that add
//  operations don't potentially have to scan the whole table twice when hashes collide
static DN_FORCEINLINE(int)
DN_SIMDHASH_SCAN_BUCKET_INTERNAL (bucket_t *bucket, DN_SIMDHASH_KEY_T needle, dn_simdhash_suffixes search_vector)
{
	uint32_t count = dn_simdhash_bucket_count(bucket->suffixes),
		index = find_first_matching_suffix(search_vector, bucket->suffixes);
	DN_SIMDHASH_KEY_T *key = &bucket->keys[index];

	for (; index < count; index++, key++) {
		if (DN_SIMDHASH_KEY_EQUALS(needle, *key))
			return index;
	}

	return -1;
}

// Helper macros so that we can optimize and change scan logic more easily
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
		/* if bucket_index == initial_index, we reached our starting point */ \
		} while (bucket_index != initial_index); \
	}

static DN_SIMDHASH_VALUE_T *
DN_SIMDHASH_FIND_VALUE_INTERNAL (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(buffers, key_hash);
	dn_simdhash_suffixes search_vector = build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(bucket_address, key, search_vector);
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
DN_SIMDHASH_TRY_INSERT_INTERNAL (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T value, uint8_t ensure_not_present)
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
	dn_simdhash_suffixes search_vector = build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		// If necessary, check the current bucket for the key
		if (ensure_not_present) {
			int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(bucket_address, key, search_vector);
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

	return DN_SIMDHASH_INSERT_NEED_TO_GROW;
}

static void
DN_SIMDHASH_REHASH_INTERNAL (DN_SIMDHASH_T_PTR hash, dn_simdhash_buffers_t old_buffers)
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
			// This theoretically can't fail, since we just grew the container and we
			//  wrap around to the beginning when there's a collision in the last bucket.
			dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(
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
dn_simdhash_vtable_t DN_SIMDHASH_T_VTABLE = {
	DN_SIMDHASH_REHASH_INTERNAL,
};

// While we've inlined these constants into the specialized code we're generating,
//  the generic code in dn-simdhash.c needs them, so we put them in this meta header
//  that lives inside every hash instance. (TODO: Store it by-reference?)
dn_simdhash_meta_t DN_SIMDHASH_T_META = {
	DN_SIMDHASH_BUCKET_CAPACITY,
	sizeof(bucket_t),
	sizeof(DN_SIMDHASH_KEY_T),
	sizeof(DN_SIMDHASH_VALUE_T),
};

DN_SIMDHASH_T_PTR
DN_SIMDHASH_NEW (uint32_t capacity, dn_allocator_t *allocator)
{
	// If this isn't satisfied, the generic code will allocate incorrectly sized buffers
	// HACK: Use static_assert because for some reason assert produces unused variable warnings only on CI
	struct silence_nuisance_msvc_warning { bucket_t a, b; };
	static_assert(
		sizeof(struct silence_nuisance_msvc_warning) == (sizeof(bucket_t) * 2),
		"Inconsistent spacing/sizing for bucket_t"
	);

	return dn_simdhash_new_internal(DN_SIMDHASH_T_META, DN_SIMDHASH_T_VTABLE, capacity, allocator);
}

uint8_t
DN_SIMDHASH_TRY_ADD (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T value)
{
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
	return DN_SIMDHASH_TRY_ADD_WITH_HASH(hash, key, key_hash, value);
}

uint8_t
DN_SIMDHASH_TRY_ADD_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T value)
{
	assert(hash);
	dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(hash, key, key_hash, value, 1);
	if (ok == DN_SIMDHASH_INSERT_NEED_TO_GROW) {
		dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(hash, dn_simdhash_capacity(hash) + 1);
		if (old_buffers.buckets) {
			DN_SIMDHASH_REHASH_INTERNAL(hash, old_buffers);
			dn_simdhash_free_buffers(old_buffers);
		}
		ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(hash, key, key_hash, value, 1);
	}

	switch (ok) {
		case DN_SIMDHASH_INSERT_OK:
			hash->count++;
			return 1;
		case DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT:
			return 0;
		case DN_SIMDHASH_INSERT_NEED_TO_GROW:
			// We should always have enough space after growing once.
		default:
			assert(0);
			return 0;
	}
}

uint8_t
DN_SIMDHASH_TRY_GET_VALUE (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T *result)
{
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
	return DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH(hash, key, key_hash, result);
}

uint8_t
DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T *result)
{
	assert(hash);
	DN_SIMDHASH_VALUE_T *value_ptr = DN_SIMDHASH_FIND_VALUE_INTERNAL(hash, key, key_hash);
	if (!value_ptr)
		return 0;
	if (result)
		*result = *value_ptr;
	return 1;
}

uint8_t
DN_SIMDHASH_TRY_REMOVE (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key)
{
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(key);
	return DN_SIMDHASH_TRY_REMOVE_WITH_HASH(hash, key, key_hash);
}

uint8_t
DN_SIMDHASH_TRY_REMOVE_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
	assert(hash);

	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(buffers, key_hash);
	dn_simdhash_suffixes search_vector = build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(bucket_address, key, search_vector);
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

			// FIXME: If we cascaded into this bucket from another bucket, the
			//  origin bucket's cascaded flag will stay set forever. We could fix this
			//  by turning the cascaded flag into some sort of a counter and then
			//  scanning backwards to decrement the counter(s).

			return 1;
		}

		if (!dn_simdhash_bucket_is_cascaded(bucket_address->suffixes))
			return 0;
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)

	return 0;
}

void
DN_SIMDHASH_FOREACH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_FOREACH_FUNC func, void *user_data)
{
	assert(hash);
	assert(func);

	dn_simdhash_buffers_t buffers = hash->buffers;
	bucket_t *bucket_address = address_of_bucket(buffers, 0);
	for (
		uint32_t i = 0, bc = buffers.buckets_length, value_slot_base = 0;
		i < bc; i++, bucket_address++, value_slot_base += DN_SIMDHASH_BUCKET_CAPACITY
	) {
		uint32_t c = dn_simdhash_bucket_count(bucket_address->suffixes);
		for (uint32_t j = 0; j < c; j++)
			func(bucket_address->keys[j], *address_of_value(buffers, value_slot_base + j), user_data);
	}
}
