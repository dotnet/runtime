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

// If specified, we pass instance data to the handlers by-value, otherwise we
//  pass the pointer to the hash itself by-value. This is enough to allow clang
//  to hoist the load of the instance data out of the key scan loop, though it
//  won't hoist it all the way out of the bucket scan loop.
#ifndef DN_SIMDHASH_INSTANCE_DATA_T
#define DN_SIMDHASH_GET_DATA(hash) (hash)
#define DN_SIMDHASH_INSTANCE_DATA_T DN_SIMDHASH_T_PTR
#else // DN_SIMDHASH_INSTANCE_DATA_T
#define DN_SIMDHASH_GET_DATA(hash) dn_simdhash_instance_data(DN_SIMDHASH_INSTANCE_DATA_T, hash)
#endif // DN_SIMDHASH_INSTANCE_DATA_T

#ifndef DN_SIMDHASH_KEY_HASHER
#error Expected DN_SIMDHASH_KEY_HASHER definition with signature: uint32_t (DN_SIMDHASH_INSTANCE_DATA_T data, KEY_T key)
#endif

#ifndef DN_SIMDHASH_KEY_EQUALS
#error Expected DN_SIMDHASH_KEY_EQUALS definition with signature: int (DN_SIMDHASH_INSTANCE_DATA_T data, KEY_T lhs, KEY_T rhs) that returns 1 for match
#endif

#ifndef DN_SIMDHASH_ON_REPLACE
#define DN_SIMDHASH_HAS_REPLACE_HANDLER 0
#define DN_SIMDHASH_ON_REPLACE(data, key, old_value, new_value)
#else // DN_SIMDHASH_ON_REPLACE
#define DN_SIMDHASH_HAS_REPLACE_HANDLER 1
#ifndef DN_SIMDHASH_ON_REMOVE
#error Expected DN_SIMDHASH_ON_REMOVE(data, key, value) to be defined.
#endif
#endif // DN_SIMDHASH_ON_REPLACE

#ifndef DN_SIMDHASH_ON_REMOVE
#define DN_SIMDHASH_HAS_REMOVE_HANDLER 0
#define DN_SIMDHASH_ON_REMOVE(data, key, value)
#else // DN_SIMDHASH_ON_REMOVE
#define DN_SIMDHASH_HAS_REMOVE_HANDLER 1
#ifndef DN_SIMDHASH_ON_REPLACE
#error Expected DN_SIMDHASH_ON_REPLACE(data, key, old_value, new_value) to be defined.
#endif
#endif // DN_SIMDHASH_ON_REMOVE

#ifndef DN_SIMDHASH_BUCKET_CAPACITY
// TODO: Find some way to automatically select an ideal bucket capacity based on key size.
// Some sort of trick using _Generic?
#define DN_SIMDHASH_BUCKET_CAPACITY DN_SIMDHASH_DEFAULT_BUCKET_CAPACITY
#endif

#include "dn-simdhash-specialization-declarations.h"

static_assert(DN_SIMDHASH_BUCKET_CAPACITY <= DN_SIMDHASH_MAX_BUCKET_CAPACITY, "Maximum bucket capacity exceeded");
static_assert(DN_SIMDHASH_BUCKET_CAPACITY > 1, "Bucket capacity too low");

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
#if defined(__clang__) || defined(__GNUC__)
__attribute__((__aligned__(DN_SIMDHASH_VECTOR_WIDTH))) bucket_t;
#else
bucket_t;
#endif

static_assert((sizeof (bucket_t) % DN_SIMDHASH_VECTOR_WIDTH) == 0, "Bucket size is not vector aligned");


// While we've inlined these constants into the specialized code we're generating,
//  the generic code in dn-simdhash.c needs them, so we put them in this meta header
//  that lives inside every hash instance. (TODO: Store it by-reference?)
dn_simdhash_meta_t DN_SIMDHASH_T_META = {
	DN_SIMDHASH_BUCKET_CAPACITY,
	sizeof(bucket_t),
	sizeof(DN_SIMDHASH_KEY_T),
	sizeof(DN_SIMDHASH_VALUE_T),
	sizeof(DN_SIMDHASH_INSTANCE_DATA_T),
};


static DN_FORCEINLINE(uint8_t)
check_self (DN_SIMDHASH_T_PTR self)
{
	// Verifies both that the self-ptr is non-null and that the meta pointer matches
	//  what it should be. This detects passing the wrong kind of simdhash_t pointer
	//  to one of the APIs, since C doesn't have fully type-safe pointers.
	uint8_t ok = self && (self->meta == &DN_SIMDHASH_T_META);
	assert(ok);
	return ok;
}


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
DN_SIMDHASH_SCAN_BUCKET_INTERNAL (DN_SIMDHASH_T_PTR hash, bucket_t *bucket, DN_SIMDHASH_KEY_T needle, dn_simdhash_suffixes search_vector)
{
	uint32_t count = dn_simdhash_bucket_count(bucket->suffixes),
#if DN_SIMDHASH_USE_SCALAR_FALLBACK
		// HACK: This allows the creation of the search_vector in our caller to be optimized out,
		//  and allows the search to compare each lane against a single scalar instead of having to
		//  compare two search vectors lane-by-lane.
		index = find_first_matching_suffix_scalar_1(search_vector.values[0], bucket->suffixes.values, count);
#else
		index = find_first_matching_suffix(search_vector, bucket->suffixes, count);
#endif
	DN_SIMDHASH_KEY_T *key = &bucket->keys[index];

	for (; index < count; index++, key++) {
		// FIXME: Could be profitable to manually hoist the data load outside of the loop,
		//  if not out of SCAN_BUCKET_INTERNAL entirely. Clang appears to do LICM on it.
		if (DN_SIMDHASH_KEY_EQUALS(DN_SIMDHASH_GET_DATA(hash), needle, *key))
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

#define BEGIN_SCAN_PAIRS(buffers, key_address, value_address) \
	bucket_t *scan_bucket_address = address_of_bucket(buffers, 0); \
	for ( \
		uint32_t scan_i = 0, scan_bc = buffers.buckets_length, scan_value_slot_base = 0; \
		scan_i < scan_bc; scan_i++, scan_bucket_address++, scan_value_slot_base += DN_SIMDHASH_BUCKET_CAPACITY \
	) { \
		uint32_t scan_c = dn_simdhash_bucket_count(scan_bucket_address->suffixes); \
		for (uint32_t scan_j = 0; scan_j < scan_c; scan_j++) { \
			DN_SIMDHASH_KEY_T *key_address = &scan_bucket_address->keys[scan_j]; \
			DN_SIMDHASH_VALUE_T *value_address = address_of_value(buffers, scan_value_slot_base + scan_j);

#define END_SCAN_PAIRS(buffers, key_address, value_address) \
		} \
	}

// FIXME: inline? might improve performance for bucket overflow, but would
//  increase code size, and maybe blow out icache. clang seems to inline it anyway.
static void
adjust_cascaded_counts (dn_simdhash_buffers_t buffers, uint32_t first_bucket_index, uint32_t last_bucket_index, uint8_t increase)
{
	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		if (bucket_index == last_bucket_index)
			break;

		uint8_t cascaded_count = dn_simdhash_bucket_cascaded_count(bucket_address->suffixes);
		if (cascaded_count < 255) {
			if (increase)
				dn_simdhash_bucket_set_cascaded_count(bucket_address->suffixes, cascaded_count + 1);
			else if (cascaded_count < 0)
				assert(0);
			else
				dn_simdhash_bucket_set_cascaded_count(bucket_address->suffixes, cascaded_count - 1);
		}
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
}

static DN_SIMDHASH_VALUE_T *
DN_SIMDHASH_FIND_VALUE_INTERNAL (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(buffers, key_hash);
	dn_simdhash_suffixes search_vector = build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(hash, bucket_address, key, search_vector);
		if (index_in_bucket >= 0) {
			uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + index_in_bucket;
			return address_of_value(buffers, value_slot_index);
		}

		if (!dn_simdhash_bucket_cascaded_count(bucket_address->suffixes))
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
			int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(hash, bucket_address, key, search_vector);
			if (index_in_bucket >= 0)
				return DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT;
		}

		// The current bucket doesn't contain the key, or duplicate checks are disabled (for rehashing),
		//  so attempt to insert into the bucket
		uint8_t new_index = dn_simdhash_bucket_count(bucket_address->suffixes);
		if (new_index < DN_SIMDHASH_BUCKET_CAPACITY) {
			// Calculate value slot index early so that we don't stall waiting for it later
			uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + new_index;
			// We found a bucket with space, so claim the first free slot
			dn_simdhash_bucket_set_count(bucket_address->suffixes, new_index + 1);
			dn_simdhash_bucket_set_suffix(bucket_address->suffixes, new_index, suffix);
			bucket_address->keys[new_index] = key;
			*address_of_value(buffers, value_slot_index) = value;
			// printf("Inserted [%zd, %zd] in bucket %d at index %d\n", key, value, bucket_index, new_index);
			// If we cascaded out of our original target bucket, scan through our probe path
			//  and increase the cascade counters. We have to wait until now to do that, because
			//  during the process of getting here we may end up finding a duplicate, which would
			//  leave the cascade counters in a corrupted state
			adjust_cascaded_counts(buffers, first_bucket_index, bucket_index, 1);
			return DN_SIMDHASH_INSERT_OK;
		}

		// The current bucket is full, so try the next bucket.
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)

	return DN_SIMDHASH_INSERT_NEED_TO_GROW;
}

static void
DN_SIMDHASH_REHASH_INTERNAL (DN_SIMDHASH_T_PTR hash, dn_simdhash_buffers_t old_buffers)
{
	BEGIN_SCAN_PAIRS(old_buffers, key_address, value_address)
		uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), *key_address);
		// This theoretically can't fail, since we just grew the container and we
		//  wrap around to the beginning when there's a collision in the last bucket.
		dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(
			hash, *key_address, key_hash,
			*value_address,
			0
		);
		// FIXME: Why doesn't assert(ok) work here? Clang says it's unused
		if (ok != DN_SIMDHASH_INSERT_OK)
			assert(0);
	END_SCAN_PAIRS(old_buffers, key_address, value_address)
}

#if DN_SIMDHASH_HAS_REMOVE_HANDLER
static void
DN_SIMDHASH_DESTROY_ALL (DN_SIMDHASH_T_PTR hash)
{
	dn_simdhash_buffers_t buffers = hash->buffers;
	BEGIN_SCAN_PAIRS(buffers, key_address, value_address)
		DN_SIMDHASH_ON_REMOVE(DN_SIMDHASH_GET_DATA(hash), *key_address, *value_address);
	END_SCAN_PAIRS(buffers, key_address, value_address)
}
#endif


// TODO: Store this by-reference instead of inline in the hash?
dn_simdhash_vtable_t DN_SIMDHASH_T_VTABLE = {
	DN_SIMDHASH_REHASH_INTERNAL,
#if DN_SIMDHASH_HAS_REMOVE_HANDLER
	DN_SIMDHASH_DESTROY_ALL,
#else
	NULL,
#endif
};


#ifndef DN_SIMDHASH_NO_DEFAULT_NEW
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

	return dn_simdhash_new_internal(&DN_SIMDHASH_T_META, DN_SIMDHASH_T_VTABLE, capacity, allocator);
}
#endif

uint8_t
DN_SIMDHASH_TRY_ADD (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T value)
{
	check_self(hash);

	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), key);
	return DN_SIMDHASH_TRY_ADD_WITH_HASH(hash, key, key_hash, value);
}

uint8_t
DN_SIMDHASH_TRY_ADD_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T value)
{
	check_self(hash);

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
	check_self(hash);

	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), key);
	return DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH(hash, key, key_hash, result);
}

uint8_t
DN_SIMDHASH_TRY_GET_VALUE_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T *result)
{
	check_self(hash);

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
	check_self(hash);

	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), key);
	return DN_SIMDHASH_TRY_REMOVE_WITH_HASH(hash, key, key_hash);
}

uint8_t
DN_SIMDHASH_TRY_REMOVE_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash)
{
	check_self(hash);

	dn_simdhash_buffers_t buffers = hash->buffers;
	uint8_t suffix = dn_simdhash_select_suffix(key_hash);
	uint32_t first_bucket_index = dn_simdhash_select_bucket_index(buffers, key_hash);
	dn_simdhash_suffixes search_vector = build_search_vector(suffix);

	BEGIN_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)
		int index_in_bucket = DN_SIMDHASH_SCAN_BUCKET_INTERNAL(hash, bucket_address, key, search_vector);
		if (index_in_bucket >= 0) {
			// We found the item. Replace it with the last item in the bucket, then erase
			//  the last item in the bucket. This ensures sequential scans still work.
			uint8_t bucket_count = dn_simdhash_bucket_count(bucket_address->suffixes),
				replacement_index_in_bucket = bucket_count - 1;
			uint32_t value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + index_in_bucket,
				replacement_value_slot_index = (bucket_index * DN_SIMDHASH_BUCKET_CAPACITY) + replacement_index_in_bucket;

			DN_SIMDHASH_VALUE_T *value_address = address_of_value(buffers, value_slot_index);
			DN_SIMDHASH_VALUE_T *replacement_address = address_of_value(buffers, replacement_value_slot_index);
			DN_SIMDHASH_KEY_T *key_address = &bucket_address->keys[index_in_bucket];
			DN_SIMDHASH_KEY_T *replacement_key_address = &bucket_address->keys[replacement_index_in_bucket];

#if DN_SIMDHASH_HAS_REMOVE_HANDLER
			// Store for later, so we can run the callback after we're done removing the item
			DN_SIMDHASH_VALUE_T value = *value_address;
			// The key used for lookup may not be the key that was actually stored inside us,
			//  so make sure we store the one that was inside and destroy that one
			DN_SIMDHASH_KEY_T actual_key = *key_address;
#endif

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
			*value_address = *replacement_address;
			// Rotate replacement key from the end of the bucket to here
			*key_address = *replacement_key_address;
			// Erase replacement key/value's old slots
			// TODO: Skip these for performance?
			memset(replacement_key_address, 0, sizeof(DN_SIMDHASH_KEY_T));
			memset(replacement_address, 0, sizeof(DN_SIMDHASH_VALUE_T));

			// If this item cascaded out of its original target bucket, we need
			//  to go through all the buckets we visited on the way here and reduce
			//  their cascade counters (if possible), to maintain better scan performance.
			if (bucket_index != first_bucket_index)
				adjust_cascaded_counts(buffers, first_bucket_index, bucket_index, 0);

#if DN_SIMDHASH_HAS_REMOVE_HANDLER
			// We've finished removing the item, so we're in a consistent state and can notify
			DN_SIMDHASH_ON_REMOVE(DN_SIMDHASH_GET_DATA(hash), actual_key, value);
#endif

			return 1;
		}

		if (!dn_simdhash_bucket_cascaded_count(bucket_address->suffixes))
			return 0;
	END_SCAN_BUCKETS(first_bucket_index, bucket_index, bucket_address)

	return 0;
}

uint8_t
DN_SIMDHASH_TRY_REPLACE (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, DN_SIMDHASH_VALUE_T new_value)
{
	check_self(hash);

	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), key);
	return DN_SIMDHASH_TRY_REPLACE_WITH_HASH(hash, key, key_hash, new_value);
}

uint8_t
DN_SIMDHASH_TRY_REPLACE_WITH_HASH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_KEY_T key, uint32_t key_hash, DN_SIMDHASH_VALUE_T new_value)
{
	check_self(hash);

	assert(hash);
	DN_SIMDHASH_VALUE_T *value_ptr = DN_SIMDHASH_FIND_VALUE_INTERNAL(hash, key, key_hash);
	if (!value_ptr)
		return 0;
#if DN_SIMDHASH_HAS_REPLACE_HANDLER
	DN_SIMDHASH_VALUE_T old_value = *value_ptr;
#endif
	*value_ptr = new_value;
#if DN_SIMDHASH_HAS_REPLACE_HANDLER
	DN_SIMDHASH_ON_REPLACE(DN_SIMDHASH_GET_DATA(hash), key, old_value, new_value);
#endif
	return 1;
}

void
DN_SIMDHASH_FOREACH (DN_SIMDHASH_T_PTR hash, DN_SIMDHASH_FOREACH_FUNC func, void *user_data)
{
	check_self(hash);
	assert(func);

	dn_simdhash_buffers_t buffers = hash->buffers;
	BEGIN_SCAN_PAIRS(buffers, key_address, value_address)
		func(*key_address, *value_address, user_data);
	END_SCAN_PAIRS(buffers, key_address, value_address)
}
