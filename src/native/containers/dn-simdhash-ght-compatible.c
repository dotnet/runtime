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

static inline uint32_t
dn_simdhash_ght_hash (dn_simdhash_ght_data data, void * key)
{
	dn_simdhash_ght_hash_func hash_func = data.hash_func;
	if (hash_func)
		return (uint32_t)hash_func(key);
	else
		// FIXME: Seed
		return MurmurHash3_32_ptr(key, 0);
}

static inline int32_t
dn_simdhash_ght_equals (dn_simdhash_ght_data data, void * lhs, void * rhs)
{
	dn_simdhash_ght_equal_func equal_func = data.key_equal_func;
	if (equal_func)
		return equal_func(lhs, rhs);
	else
		return lhs == rhs;
}

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
#define DN_SIMDHASH_KEY_HASHER dn_simdhash_ght_hash
#define DN_SIMDHASH_KEY_EQUALS dn_simdhash_ght_equals
#define DN_SIMDHASH_ON_REMOVE dn_simdhash_ght_removed
#define DN_SIMDHASH_ON_REPLACE dn_simdhash_ght_replaced
#if SIZEOF_VOID_P == 8
#define DN_SIMDHASH_BUCKET_CAPACITY 11
#else
#define DN_SIMDHASH_BUCKET_CAPACITY 12
#endif
#define DN_SIMDHASH_NO_DEFAULT_NEW 1

#include "dn-simdhash-specialization.h"

dn_simdhash_ght_t *
dn_simdhash_ght_new (
	dn_simdhash_ght_hash_func hash_func, dn_simdhash_ght_equal_func key_equal_func,
	uint32_t capacity, dn_allocator_t *allocator
)
{
	dn_simdhash_ght_t *hash = dn_simdhash_new_internal(&DN_SIMDHASH_T_META, DN_SIMDHASH_T_VTABLE, capacity, allocator);
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
