// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_UMAP_H__
#define __DN_UMAP_H__

#include "dn-utils.h"
#include "dn-allocator.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef uint32_t (DN_CALLBACK_CALLTYPE *dn_umap_hash_func_t) (const void *key);
typedef bool (DN_CALLBACK_CALLTYPE *dn_umap_equal_func_t) (const void *a, const void *b);
typedef void (DN_CALLBACK_CALLTYPE *dn_umap_key_dispose_func_t) (void *key);
typedef void (DN_CALLBACK_CALLTYPE *dn_umap_value_dispose_func_t) (void *value);
typedef void (DN_CALLBACK_CALLTYPE *dn_umap_key_value_func_t) (void *key, void *value, void *user_data);

typedef struct _dn_umap_node_t dn_umap_node_t;
struct _dn_umap_node_t {
	void *key;
	void *value;
	dn_umap_node_t *next;
};

typedef struct _dn_umap_t dn_umap_t;
struct _dn_umap_t {
	struct {
		dn_umap_node_t **_buckets;
		dn_umap_hash_func_t _hash_func;
		dn_umap_equal_func_t _key_equal_func;
		dn_umap_key_dispose_func_t _key_dispose_func;
		dn_umap_value_dispose_func_t _value_dispose_func;
		dn_allocator_t *_allocator;
		uint32_t _bucket_count;
		uint32_t _node_count;
		uint32_t _threshold;
		uint32_t _last_rehash;
	} _internal;
};

typedef struct _dn_umap_it_t dn_umap_it_t;
struct _dn_umap_it_t
{
	struct {
		dn_umap_t *_map;
		dn_umap_node_t *_node;
		uint32_t _index;
	} _internal;
};

typedef struct _dn_umap_result_t dn_umap_result_t;
struct _dn_umap_result_t {
	dn_umap_it_t it;
	bool result;
};

typedef struct _dn_umap_custom_params_t dn_umap_custom_alloc_params_t;
typedef struct _dn_umap_custom_params_t dn_umap_custom_init_params_t;
struct _dn_umap_custom_params_t {
	dn_allocator_t *allocator;
	dn_umap_hash_func_t hash_func;
	dn_umap_equal_func_t equal_func;
	dn_umap_key_dispose_func_t key_dispose_func;
	dn_umap_value_dispose_func_t value_dispose_func;
};

dn_umap_it_t
dn_umap_begin (dn_umap_t *map);

static inline dn_umap_it_t
dn_umap_end (dn_umap_t *map)
{
	DN_ASSERT (map);
	dn_umap_it_t it = { { map, NULL, 0 } };
	return it;
}

void
dn_umap_it_advance (
	dn_umap_it_t *it,
	uint32_t n);

static inline dn_umap_it_t
dn_umap_it_next (dn_umap_it_t it)
{
	dn_umap_it_advance (&it, 1);
	return it;
}

static inline void *
dn_umap_it_key (dn_umap_it_t it)
{
	DN_ASSERT (it._internal._node);
	return it._internal._node->key;
}

#define dn_umap_it_key_t(it, type) \
	(type)(uintptr_t)(dn_umap_it_key (it))

static inline void *
dn_umap_it_value (dn_umap_it_t it)
{
	DN_ASSERT (it._internal._node);
	return it._internal._node->value;
}

#define dn_umap_it_value_t(it, type) \
	(type)(uintptr_t)(dn_umap_it_value ((it)))

static inline bool
dn_umap_it_begin (dn_umap_it_t it)
{
	dn_umap_it_t begin = dn_umap_begin (it._internal._map);
	return (begin._internal._node == it._internal._node && begin._internal._index == it._internal._index);
}

static inline bool
dn_umap_it_end (dn_umap_it_t it)
{
	return !(it._internal._node);
}

#define DN_UMAP_FOREACH_BEGIN(key_type, key_name, value_type, value_name, map) do { \
	key_type key_name; \
	value_type value_name; \
	for (dn_umap_it_t __it##key_name = dn_umap_begin (map); !dn_umap_it_end (__it##key_name); __it##key_name = dn_umap_it_next (__it##key_name)) { \
		key_name = ((key_type)(uintptr_t)dn_umap_it_key (__it##key_name)); \
		value_name = ((value_type)(uintptr_t)dn_umap_it_value (__it##key_name));

#define DN_UMAP_FOREACH_KEY_BEGIN(key_type, key_name, map) do { \
	key_type key_name; \
	for (dn_umap_it_t __it##key_name = dn_umap_begin (map); !dn_umap_it_end (__it##key_name); __it##key_name = dn_umap_it_next (__it##key_name)) { \
		key_name = ((key_type)(uintptr_t)dn_umap_it_key (__it##key_name));

#define DN_UMAP_FOREACH_END \
		} \
	} while (0)

dn_umap_t *
dn_umap_custom_alloc (const dn_umap_custom_alloc_params_t *params);

static inline dn_umap_t *
dn_umap_alloc (void)
{
	return dn_umap_custom_alloc (NULL);
}

void
dn_umap_free (dn_umap_t *map);

bool
dn_umap_custom_init (
	dn_umap_t *map,
	const dn_umap_custom_init_params_t *params);

static inline bool
dn_umap_init (dn_umap_t *map)
{
	return dn_umap_custom_init (map, NULL);
}

void
dn_umap_dispose (dn_umap_t *map);

static inline bool
dn_umap_empty (const dn_umap_t *map)
{
	DN_ASSERT (map);
	return map->_internal._node_count == 0;
}

static inline uint32_t
dn_umap_size (const dn_umap_t *map)
{
	DN_ASSERT (map);
	return map->_internal._node_count;
}

static inline uint32_t
dn_umap_max_size (const dn_umap_t *map)
{
	DN_UNREFERENCED_PARAMETER (map);
	return UINT32_MAX;
}

void
dn_umap_clear (dn_umap_t *map);

dn_umap_result_t
dn_umap_insert (
	dn_umap_t *map,
	void *key,
	void *value);

dn_umap_result_t
dn_umap_insert_or_assign (
	dn_umap_t *map,
	void *key,
	void *value);

dn_umap_it_t
dn_umap_erase (dn_umap_it_t position);

uint32_t
dn_umap_erase_key (
	dn_umap_t *map,
	const void *key);

bool
dn_umap_extract_key (
	dn_umap_t *map,
	const void *key,
	void **out_key,
	void **out_value);

dn_umap_it_t
dn_umap_custom_find (
	dn_umap_t *map,
	const void *key,
	dn_umap_equal_func_t equal_func);

static inline dn_umap_it_t
dn_umap_find (
	dn_umap_t *map,
	const void *key)
{
	return dn_umap_custom_find (map, key, NULL);
}

static inline bool
dn_umap_contains (
	dn_umap_t *map,
	const void *key)
{
	dn_umap_it_t it = dn_umap_find (map, key);
	return !dn_umap_it_end (it);
}

void
dn_umap_for_each (
	dn_umap_t *map,
	dn_umap_key_value_func_t for_each_func,
	void *user_data);

void
dn_umap_rehash (
	dn_umap_t *map,
	uint32_t count);

void
dn_umap_reserve (
	dn_umap_t *map,
	uint32_t count);

bool
DN_CALLBACK_CALLTYPE
dn_direct_equal (const void *v1, const void *v2);

uint32_t
DN_CALLBACK_CALLTYPE
dn_direct_hash (const void *v1);

bool
DN_CALLBACK_CALLTYPE
dn_int_equal (const void *v1, const void *v2);

uint32_t
DN_CALLBACK_CALLTYPE
dn_int_hash (const void *v1);

bool
DN_CALLBACK_CALLTYPE
dn_str_equal (const void *v1, const void *v2);

uint32_t
DN_CALLBACK_CALLTYPE
dn_str_hash (const void *v1);

#ifdef __cplusplus
} // extern "C"
#endif

#endif /* __DN_UMAP_H__ */
