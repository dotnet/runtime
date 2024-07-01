// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef NO_CONFIG_H
#include <dn-config.h>
#endif
#include "dn-simdhash.h"

#include "dn-simdhash-utils.h"

typedef struct dn_simdhash_str_key {
	const char *text;
	// We keep a precomputed hash to speed up rehashing and scans.
	uint32_t hash;
#if SIZEOF_VOID_P == 8
	// HACK: Perfect cache alignment isn't possible for a 12-byte struct, so pad it to 16 bytes
	uint32_t padding;
#endif
} dn_simdhash_str_key;

static inline int32_t
dn_simdhash_str_equal (dn_simdhash_str_key v1, dn_simdhash_str_key v2)
{
	if (v1.text == v2.text)
		return 1;
	return strcmp(v1.text, v2.text) == 0;
}

static inline uint32_t
dn_simdhash_str_hash (dn_simdhash_str_key v1)
{
	return v1.hash;
}

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T dn_simdhash_str_key
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER(hash, key) dn_simdhash_str_hash(key)
#define DN_SIMDHASH_KEY_EQUALS(hash, lhs, rhs) dn_simdhash_str_equal(lhs, rhs)
#define DN_SIMDHASH_ACCESSOR_SUFFIX _raw

// perfect cache alignment. 32-bit ptrs: 8-byte keys. 64-bit: 16-byte keys.
#if SIZEOF_VOID_P == 8
#define DN_SIMDHASH_BUCKET_CAPACITY 11
#else
#define DN_SIMDHASH_BUCKET_CAPACITY 12
#endif

#include "dn-simdhash-specialization.h"
#include "dn-simdhash-string-ptr.h"

static dn_simdhash_str_key
dn_simdhash_make_str_key (const char *text)
{
	dn_simdhash_str_key result = { 0, };
	if (text) {
		// FIXME: Select a good seed.
		result.hash = MurmurHash3_32_streaming((uint8_t *)text, 0);
		result.text = text;
	}
	return result;
}

uint8_t
dn_simdhash_string_ptr_try_add (dn_simdhash_string_ptr_t *hash, const char *key, void *value)
{
	return dn_simdhash_string_ptr_try_add_raw(hash, dn_simdhash_make_str_key(key), value);
}

uint8_t
dn_simdhash_string_ptr_try_get_value (dn_simdhash_string_ptr_t *hash, const char *key, void **result)
{
	return dn_simdhash_string_ptr_try_get_value_raw(hash, dn_simdhash_make_str_key(key), result);
}

uint8_t
dn_simdhash_string_ptr_try_remove (dn_simdhash_string_ptr_t *hash, const char *key)
{
	return dn_simdhash_string_ptr_try_remove_raw(hash, dn_simdhash_make_str_key(key));
}

// FIXME: Find a way to make this easier to define
void
dn_simdhash_string_ptr_foreach (dn_simdhash_string_ptr_t *hash, dn_simdhash_string_ptr_foreach_func func, void *user_data)
{
	assert(hash);
	assert(func);

	dn_simdhash_buffers_t buffers = hash->buffers;
	BEGIN_SCAN_PAIRS(buffers, key_address, value_address)
		func(key_address->text, *value_address, user_data);
	END_SCAN_PAIRS(buffers, key_address, value_address)
}
