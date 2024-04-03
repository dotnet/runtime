// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <config.h>
#include "dn-simdhash.h"

#include "dn-simdhash-utils.h"

typedef unsigned int   guint;
typedef int32_t        gboolean;
typedef void *         gpointer;
typedef const void *   gconstpointer;

typedef void     (*GDestroyNotify) (gpointer data);
typedef guint    (*GHashFunc)      (gconstpointer key);
typedef gboolean (*GEqualFunc)     (gconstpointer a, gconstpointer b);

typedef struct dn_simdhash_ght_data {
	GHashFunc hash_func;
	GEqualFunc key_equal_func;
	GDestroyNotify key_destroy_func;
	GDestroyNotify value_destroy_func;
} dn_simdhash_ght_data;

static inline uint32_t
dn_simdhash_ght_hash (dn_simdhash_ght_data data, gconstpointer key)
{
	GHashFunc hash_func = data.hash_func;
	if (hash_func)
		return (uint32_t)hash_func(key);
	else
		// FIXME: Seed
		return MurmurHash3_32_ptr(key, 0);
}

static inline gboolean
dn_simdhash_ght_equals (dn_simdhash_ght_data data, gconstpointer lhs, gconstpointer rhs)
{
	GEqualFunc equal_func = data.key_equal_func;
	if (equal_func)
		return equal_func(lhs, rhs);
	else
		return lhs == rhs;
}

static inline void
dn_simdhash_ght_removed (dn_simdhash_ght_data data, gconstpointer key, gpointer value)
{
	GDestroyNotify key_destroy_func = data.key_destroy_func,
		value_destroy_func = data.value_destroy_func;
	if (key_destroy_func)
		key_destroy_func((gpointer)key);
	if (value_destroy_func)
		value_destroy_func((gpointer)value);
}

static inline void
dn_simdhash_ght_replaced (dn_simdhash_ght_data data, gconstpointer key, gpointer old_value, gpointer new_value)
{
	if (old_value == new_value)
		return;

	GDestroyNotify value_destroy_func = data.value_destroy_func;
	if (value_destroy_func)
		value_destroy_func((gpointer)old_value);
}

#define DN_SIMDHASH_T dn_simdhash_ght
#define DN_SIMDHASH_KEY_T gconstpointer
#define DN_SIMDHASH_VALUE_T gpointer
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
#include "dn-simdhash-ght-compatible.h"

dn_simdhash_ght_t *
dn_simdhash_ght_new (
	GHashFunc hash_func, GEqualFunc key_equal_func,
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
	GHashFunc hash_func, GEqualFunc key_equal_func,
	GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func,
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
