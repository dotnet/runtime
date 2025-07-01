// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef NO_CONFIG_H
#include <dn-config.h>
#endif
#include "dn-simdhash.h"

#include "dn-simdhash-utils.h"
#include "dn-simdhash-ght-compatible.h"

typedef struct dn_simdhash_ght_data {
	dn_simdhash_ght_hash_func hash_func;
	dn_simdhash_ght_equal_func key_equal_func;
	dn_simdhash_ght_destroy_func key_destroy_func;
	dn_simdhash_ght_destroy_func value_destroy_func;
} dn_simdhash_ght_data;

static inline void
dn_simdhash_ght_removed (dn_simdhash_ght_data data, void * key, void * value)
{
	dn_simdhash_ght_destroy_func key_destroy_func = data.key_destroy_func,
		value_destroy_func = data.value_destroy_func;
	if (key_destroy_func)
		key_destroy_func((void *)key);
	if (value_destroy_func)
		value_destroy_func((void *)value);
}

static inline void
dn_simdhash_ght_replaced (dn_simdhash_ght_data data, void * old_key, void * new_key, void * old_value, void * new_value)
{
	if (old_key != new_key) {
		dn_simdhash_ght_destroy_func key_destroy_func = data.key_destroy_func;
		if (key_destroy_func)
			key_destroy_func((void *)old_key);
	}

	if (old_value != new_value) {
		dn_simdhash_ght_destroy_func value_destroy_func = data.value_destroy_func;
		if (value_destroy_func)
			value_destroy_func((void *)old_value);
	}
}

#define DN_SIMDHASH_T dn_simdhash_ght
#define DN_SIMDHASH_KEY_T void *
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_INSTANCE_DATA_T dn_simdhash_ght_data

#define DN_SIMDHASH_SCAN_DATA_T dn_simdhash_ght_equal_func
#define DN_SIMDHASH_GET_SCAN_DATA(data) data.key_equal_func

#define DN_SIMDHASH_KEY_HASHER(data, key) (uint32_t)data.hash_func(key)
#define DN_SIMDHASH_KEY_EQUALS(scan_data, lhs, rhs) scan_data(lhs, rhs)

#define DN_SIMDHASH_ON_REMOVE dn_simdhash_ght_removed
#define DN_SIMDHASH_ON_REPLACE dn_simdhash_ght_replaced
// perfect cache alignment. 128-byte buckets for 64-bit pointers, 64-byte buckets for 32-bit pointers
#if SIZEOF_VOID_P == 8
#define DN_SIMDHASH_BUCKET_CAPACITY 14
#else
#define DN_SIMDHASH_BUCKET_CAPACITY 12
#endif
#define DN_SIMDHASH_NO_DEFAULT_NEW 1

#include "dn-simdhash-specialization.h"

static unsigned int
dn_simdhash_ght_default_hash (const void * key)
{
    // You might think we should avalanche the key bits but in my testing, it doesn't help.
    // Right now the default hash function is rarely used anyway
    return (unsigned int)(size_t)key;
}

static int32_t
dn_simdhash_ght_default_comparer (const void * a, const void * b)
{
	return a == b;
}

dn_simdhash_ght_t *
dn_simdhash_ght_new (
	dn_simdhash_ght_hash_func hash_func, dn_simdhash_ght_equal_func key_equal_func,
	uint32_t capacity, dn_allocator_t *allocator
)
{
	dn_simdhash_ght_t *hash = dn_simdhash_new_internal(&DN_SIMDHASH_T_META, DN_SIMDHASH_T_VTABLE, capacity, allocator);
	// Most users of dn_simdhash_ght are passing a custom comparer, and always doing an indirect call ends up being faster
	//  than conditionally doing a fast inline check when there's no comparer set. Somewhat counter-intuitive, but true
	//  on both x64 and arm64. Probably due to the smaller code size and reduced branch predictor pressure.
	if (!hash_func)
		hash_func = dn_simdhash_ght_default_hash;
	if (!key_equal_func)
		key_equal_func = dn_simdhash_ght_default_comparer;
	dn_simdhash_instance_data(dn_simdhash_ght_data, hash).hash_func = hash_func;
	dn_simdhash_instance_data(dn_simdhash_ght_data, hash).key_equal_func = key_equal_func;
	return hash;
}

dn_simdhash_ght_t *
dn_simdhash_ght_new_full (
	dn_simdhash_ght_hash_func hash_func, dn_simdhash_ght_equal_func key_equal_func,
	dn_simdhash_ght_destroy_func key_destroy_func, dn_simdhash_ght_destroy_func value_destroy_func,
	uint32_t capacity, dn_allocator_t *allocator
)
{
	dn_simdhash_ght_t *hash = dn_simdhash_new_internal(&DN_SIMDHASH_T_META, DN_SIMDHASH_T_VTABLE, capacity, allocator);
	if (!hash_func)
		hash_func = dn_simdhash_ght_default_hash;
	if (!key_equal_func)
		key_equal_func = dn_simdhash_ght_default_comparer;
	dn_simdhash_instance_data(dn_simdhash_ght_data, hash).hash_func = hash_func;
	dn_simdhash_instance_data(dn_simdhash_ght_data, hash).key_equal_func = key_equal_func;
	dn_simdhash_instance_data(dn_simdhash_ght_data, hash).key_destroy_func = key_destroy_func;
	dn_simdhash_instance_data(dn_simdhash_ght_data, hash).value_destroy_func = value_destroy_func;
	return hash;
}

void
dn_simdhash_ght_insert_replace (
	dn_simdhash_ght_t *hash,
	void * key, void * value,
	int32_t overwrite_key
)
{
	check_self(hash);
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), key);
	dn_simdhash_insert_mode imode = overwrite_key
		? DN_SIMDHASH_INSERT_MODE_OVERWRITE_KEY_AND_VALUE
		: DN_SIMDHASH_INSERT_MODE_OVERWRITE_VALUE;

	dn_simdhash_insert_result ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(hash, key, key_hash, value, imode);
	if (ok == DN_SIMDHASH_INSERT_NEED_TO_GROW) {
		dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(hash, dn_simdhash_capacity(hash) + 1);
		if (old_buffers.buckets) {
			DN_SIMDHASH_REHASH_INTERNAL(hash, old_buffers);
			dn_simdhash_free_buffers(old_buffers);
		}
		ok = DN_SIMDHASH_TRY_INSERT_INTERNAL(hash, key, key_hash, value, imode);
	}

	switch (ok) {
		case DN_SIMDHASH_INSERT_OK_ADDED_NEW:
			hash->count++;
			return;
		case DN_SIMDHASH_INSERT_OK_OVERWROTE_EXISTING:
			return;
		// We should always return one of the first two
		case DN_SIMDHASH_INSERT_KEY_ALREADY_PRESENT:
		case DN_SIMDHASH_INSERT_NEED_TO_GROW:
		default:
			assert(0);
			return;
	}
}

void *
dn_simdhash_ght_get_value_or_default (
	dn_simdhash_ght_t *hash, void * key
)
{
	check_self(hash);
	uint32_t key_hash = DN_SIMDHASH_KEY_HASHER(DN_SIMDHASH_GET_DATA(hash), key);
	DN_SIMDHASH_VALUE_T *value_ptr = DN_SIMDHASH_FIND_VALUE_INTERNAL(hash, key, key_hash);
	if (value_ptr)
		return *value_ptr;
	else
		return NULL;
}
