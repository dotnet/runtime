// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "dn-umap.h"
#include <minipal/utils.h>
#include <string.h>
#include <math.h>

static void * KEYMARKER_REMOVED = &KEYMARKER_REMOVED;

static const uint32_t prime_tbl [] = {
	11, 19, 37, 73, 109, 163, 251, 367, 557, 823, 1237,
	1861, 2777, 4177, 6247, 9371, 14057, 21089, 31627,
	47431, 71143, 106721, 160073, 240101, 360163,
	540217, 810343, 1215497, 1823231, 2734867, 4102283,
	6153409, 9230113, 13845163
};

static bool
umap_test_prime (uint32_t x)
{
	if ((x & 1) != 0) {
		uint32_t n;
		for (n = 3; n < (uint32_t)sqrt (x); n += 2) {
			if ((x % n) == 0)
				return false;
		}
		return true;
	}
	return (x == 2);
}

static uint32_t
umap_calc_prime (uint32_t x)
{
	uint32_t i;

	for (i = (x & (~1))-1; i < UINT32_MAX - 2; i += 2) {
		if (umap_test_prime (i))
			return i;
	}
	return x;
}

static uint32_t
umap_spaced_primes_closest (uint32_t x)
{
	for (size_t i = 0; i < ARRAY_SIZE (prime_tbl); i++) {
		if (x <= prime_tbl [i])
			return prime_tbl [i];
	}
	return umap_calc_prime (x);
}

#ifdef DN_UMAP_SANITY_CHECK
#include <stdio.h>
static void
umap_dump (dn_umap_t *map)
{
	for (uint32_t i = 0; i < map->_internal._bucket_count; i++) {
		for (dn_umap_node_t *node = map->_internal._buckets [i]; node; node = node->next){
			uint32_t hashcode = map->_internal._hash_func (node->key);
			uint32_t bucket = (hashcode) % map->_internal._bucket_count;
			printf ("key %p hash %x on bucket %d correct bucket %d bucket count %d\n", node->key, hashcode, i, bucket, map->_internal._bucket_count);
		}
	}
}

static void
umap_sanity_check (dn_umap_t *map)
{
		for (uint32_t i = 0; i < map->_internal._bucket_count; i++) {
		for (dn_umap_node_t *node = map->_internal._buckets [i]; node; node = node->next){
			uint32_t hashcode = map->_internal._hash_func (node->key);
			uint32_t bucket = (hashcode) % map->_internal._bucket_count;
			if (bucket != i) {
				printf ("Key %p (bucket %d) on invalid bucket %d (hashcode %x) (bucket count %d)", node->key, bucket, i, hashcode, map->_internal._bucket_count);
				abort();
			}
		}
	}
}
#else

#define umap_dump(map) do {}while(0)
#define umap_sanity_check(map) do {}while(0)

#endif

static void
umap_do_rehash (
	dn_umap_t *map,
	uint32_t new_bucket_count)
{
	dn_umap_node_t **buckets = map->_internal._buckets;
	uint32_t current_bucket_count = map->_internal._bucket_count;

	map->_internal._buckets = (dn_umap_node_t **)dn_allocator_alloc (map->_internal._allocator, sizeof (dn_umap_node_t *) * new_bucket_count);
	if (!map->_internal._buckets)
		return;

	memset (map->_internal._buckets, 0, sizeof (dn_umap_node_t *) * new_bucket_count);

	map->_internal._last_rehash = map->_internal._bucket_count;
	map->_internal._bucket_count = new_bucket_count;

	for (uint32_t i = 0; i < current_bucket_count; i++){
		dn_umap_node_t *node, *next_node;
		for (node = buckets [i]; node; node = next_node){
			uint32_t hashcode = (map->_internal._hash_func (node->key)) % map->_internal._bucket_count;
			next_node = node->next;

			node->next = map->_internal._buckets [hashcode];
			map->_internal._buckets [hashcode] = node;
		}
	}

	dn_allocator_free (map->_internal._allocator, buckets);
}

static void
umap_rehash (dn_umap_t *map)
{
	uint32_t diff;
	if (map->_internal._last_rehash > map->_internal._node_count)
		diff = map->_internal._last_rehash - map->_internal._node_count;
	else
		diff = map->_internal._node_count - map->_internal._last_rehash;

	if (!(diff * 0.75 > map->_internal._bucket_count * 2))
		return;

	umap_do_rehash (map, umap_spaced_primes_closest (map->_internal._node_count));
	umap_sanity_check (map);
}

static void
umap_dispose_node (
	dn_umap_t *map,
	dn_umap_node_t *node)
{
	if (DN_UNLIKELY (!map))
		return;

	if (map->_internal._key_dispose_func)
		map->_internal._key_dispose_func (node->key);

	if (map->_internal._value_dispose_func)
		map->_internal._value_dispose_func (node->value);
}

static void
umap_free_node (
	dn_umap_t *map,
	dn_umap_node_t *node)
{
	umap_dispose_node (map, node);
	dn_allocator_free (map->_internal._allocator, node);
}

static void
umap_erase_node (
	dn_umap_t *map,
	uint32_t bucket,
	dn_umap_node_t *node,
	dn_umap_node_t *prev_node)
{
	umap_dispose_node (map, node);

	if (!prev_node)
		map->_internal._buckets [bucket] = node->next;
	else
		prev_node->next = node->next;

	dn_allocator_free (map->_internal._allocator, node);

	map->_internal._node_count --;

	umap_sanity_check (map);
}

static inline void
umap_insert_set_result (
	dn_umap_result_t *insert_result,
	dn_umap_t *map,
	dn_umap_node_t *node,
	uint32_t index,
	bool result)
{
	insert_result->it._internal._map = map;
	insert_result->it._internal._node = node;
	insert_result->it._internal._index = index;
	insert_result->result = result;
	return;
}

static void
umap_insert (
	dn_umap_t *map,
	void *key,
	void *value,
	bool assign,
	dn_umap_result_t *result)
{
	umap_sanity_check (map);

	if (map->_internal._node_count == dn_umap_max_size (map)) {
		umap_insert_set_result (result, map, NULL, 0, false);
		return;
	}

	umap_rehash (map);

	dn_umap_equal_func_t equal_func = map->_internal._key_equal_func;
	uint32_t hashcode = (map->_internal._hash_func (key)) % map->_internal._bucket_count;

	for (dn_umap_node_t *node = map->_internal._buckets [hashcode]; node; node = node->next) {
		if (equal_func (node->key, key)) {
			if (assign) {
				if (map->_internal._value_dispose_func)
					map->_internal._value_dispose_func (node->value);

				node->value = value;

				umap_sanity_check (map);

				umap_insert_set_result (result, map, node, hashcode, true);
				return;
			} else {
				umap_insert_set_result (result, map, node, hashcode, false);
				return;
			}
		}
	}

	dn_umap_node_t *node = (dn_umap_node_t *)dn_allocator_alloc (map->_internal._allocator, sizeof (dn_umap_node_t));
	if (node) {
		node->key = key;
		node->value = value;
		node->next = map->_internal._buckets [hashcode];
		map->_internal._buckets [hashcode] = node;
		map->_internal._node_count ++;

		umap_sanity_check (map);

		umap_insert_set_result (result, map, node, hashcode, true);
		return;
	}

	umap_insert_set_result (result, map, NULL, 0, false);
	return;
}

static bool
umap_it_next (dn_umap_it_t *it)
{
	if (DN_UNLIKELY(dn_umap_it_end (*it)))
		return false;

	dn_umap_t *map = it->_internal._map;

	DN_ASSERT (map);

	if (!it->_internal._node->next) {
		while (true) {
			it->_internal._index ++;
			if (it->_internal._index >= map->_internal._bucket_count) {
				*it = dn_umap_end (it->_internal._map);
				return false;
			}
			if (map->_internal._buckets [it->_internal._index])
				break;
		}
		it->_internal._node = map->_internal._buckets [it->_internal._index];
	} else {
		it->_internal._node = it->_internal._node->next;
	}

	return it->_internal._node;
}

dn_umap_it_t
dn_umap_begin (dn_umap_t *map)
{
	DN_ASSERT (map);

	uint32_t index = 0;

	while (true) {
		if (index >= map->_internal._bucket_count)
			return dn_umap_end (map);

		if (map->_internal._buckets [index])
			break;

		index ++;
	}

	dn_umap_it_t it = { map, map->_internal._buckets [index], index };
	return it;
}

void
dn_umap_it_advance (
	dn_umap_it_t *it,
	uint32_t n)
{
	while (n && umap_it_next (it))
		n--;
}

dn_umap_t *
dn_umap_custom_alloc (const dn_umap_custom_alloc_params_t *params)
{
	dn_allocator_t *allocator = params ? params->allocator : DN_DEFAULT_ALLOCATOR;

	dn_umap_t *map = (dn_umap_t *)dn_allocator_alloc (allocator, sizeof (dn_umap_t));
	if (!dn_umap_custom_init (map, params)) {
		dn_allocator_free (allocator, map);
		return NULL;
	}

	return map;
}

bool
dn_umap_custom_init (
	dn_umap_t *map,
	const dn_umap_custom_alloc_params_t *params)
{
	if (DN_UNLIKELY (!map))
		return false;

	dn_allocator_t *allocator = params ? params->allocator : DN_DEFAULT_ALLOCATOR;

	memset (map, 0, sizeof(dn_umap_t));

	map->_internal._allocator = allocator;

	map->_internal._bucket_count = umap_spaced_primes_closest (1);
	
	map->_internal._last_rehash = map->_internal._bucket_count;

	if (params) {
		map->_internal._hash_func = params->hash_func ? params->hash_func : dn_direct_hash;
		map->_internal._key_equal_func = params->equal_func ? params->equal_func : dn_direct_equal;
		map->_internal._key_dispose_func = params->key_dispose_func;
		map->_internal._value_dispose_func = params->value_dispose_func;
	} else {
		map->_internal._hash_func = dn_direct_hash;
		map->_internal._key_equal_func = dn_direct_equal;
	}

	map->_internal._buckets = (dn_umap_node_t **)dn_allocator_alloc (allocator, sizeof (dn_umap_node_t *) * map->_internal._bucket_count);
	if (map->_internal._buckets)
		memset (map->_internal._buckets, 0, sizeof (dn_umap_node_t *) * map->_internal._bucket_count);

	return map->_internal._buckets;
}

void
dn_umap_free (dn_umap_t *map)
{
	if (DN_UNLIKELY(!map))
		return;

	dn_umap_dispose (map);
	dn_allocator_free (map->_internal._allocator, map);
}

void
dn_umap_dispose (dn_umap_t *map)
{
	if (DN_UNLIKELY(!map))
		return;

	for (uint32_t i = 0; i < map->_internal._bucket_count; i++) {
		dn_umap_node_t *node, *next_node;
		for (node = map->_internal._buckets [i]; node; node = next_node){
			next_node = node->next;
			umap_free_node (map, node);
		}
	}
	dn_allocator_free (map->_internal._allocator, map->_internal._buckets);
}

void
dn_umap_clear (dn_umap_t *map)
{
	DN_ASSERT (map);

	for (uint32_t i = 0; i < map->_internal._bucket_count; i++) {
		dn_umap_node_t *node, *next_node;
		for (node = map->_internal._buckets [i]; node; node = next_node){
			next_node = node->next;
			umap_free_node (map, node);
		}
		map->_internal._buckets [i] = NULL;
	}

	map->_internal._node_count = 0;
}

dn_umap_result_t
dn_umap_insert (
	dn_umap_t *map,
	void *key,
	void *value)
{
	DN_ASSERT (map);

	dn_umap_result_t result;
	umap_insert (map, key, value, false, &result);
	return result;
}

dn_umap_result_t
dn_umap_insert_or_assign (
	dn_umap_t *map,
	void *key,
	void *value)
{
	DN_ASSERT (map);

	dn_umap_result_t result;
	umap_insert (map, key, value, true, &result);
	return result;
}

dn_umap_it_t
dn_umap_erase (dn_umap_it_t position)
{
	if (dn_umap_it_end (position))
		return position;

	DN_ASSERT (position._internal._map);

	dn_umap_it_t result = dn_umap_it_next (position);
	dn_umap_erase_key (position._internal._map, position._internal._node->key);

	return result;
}

uint32_t
dn_umap_erase_key (
	dn_umap_t *map,
	const void *key)
{
	DN_ASSERT (map);

	umap_sanity_check (map);

	dn_umap_equal_func_t equal_func = map->_internal._key_equal_func;
	uint32_t hashcode = (map->_internal._hash_func (key)) % map->_internal._bucket_count;

	dn_umap_node_t *prev_node = NULL;
	for (dn_umap_node_t *node = map->_internal._buckets [hashcode]; node; node = node->next){
		if (equal_func (node->key, key)) {
			umap_erase_node (map, hashcode, node, prev_node);
			return 1;
		}
		prev_node = node;
	}

	umap_sanity_check (map);
	return 0;
}

bool
dn_umap_extract_key (
	dn_umap_t *map,
	const void *key,
	void **out_key,
	void **out_value)
{
	DN_ASSERT (map);

	dn_umap_equal_func_t equal_func = map->_internal._key_equal_func;
	uint32_t hashcode = (map->_internal._hash_func (key)) % map->_internal._bucket_count;

	dn_umap_node_t *prev_node = NULL;
	for (dn_umap_node_t *node = map->_internal._buckets [hashcode]; node; node = node->next){
		if (equal_func (node->key, key)) {
			if (!prev_node)
				map->_internal._buckets [hashcode] = node->next;
			else
				prev_node->next = node->next;

			if (out_key)
				*out_key = node->key;
			if (out_value)
				*out_value = node->value;

			dn_allocator_free (map->_internal._allocator, node);
			map->_internal._node_count --;

			umap_sanity_check (map);
			return true;
		}
		prev_node = node;
	}

	umap_sanity_check (map);
	return false;
}

dn_umap_it_t
dn_umap_custom_find (
	dn_umap_t *map,
	const void *key,
	dn_umap_equal_func_t equal_func)
{
	DN_ASSERT (map);

	if (!equal_func)
		equal_func = map->_internal._key_equal_func;

	uint32_t hashcode = (map->_internal._hash_func (key)) % map->_internal._bucket_count;

	for (dn_umap_node_t *node = map->_internal._buckets [hashcode]; node; node = node->next) {
		if (equal_func (node->key, key)) {
			dn_umap_it_t found = { map, node, hashcode };
			return found;
		}
	}

	return dn_umap_end (map);
}

void
dn_umap_for_each (
	dn_umap_t *map,
	dn_umap_key_value_func_t for_each_func,
	void *user_data)
{
	DN_ASSERT (map && for_each_func);

	DN_UMAP_FOREACH_BEGIN (void *, key, void *, value, map) {
		for_each_func (key, value, user_data);
	} DN_UMAP_FOREACH_END;
}

void
dn_umap_rehash (
	dn_umap_t *map,
	uint32_t count)
{
	DN_ASSERT (map);

	if (count < map->_internal._node_count)
		count = map->_internal._node_count;

	umap_do_rehash (map, count);
}

void
dn_umap_reserve (
	dn_umap_t *map,
	uint32_t count)
{
	DN_ASSERT (map);

	umap_do_rehash (map, count);
}

bool
DN_CALLBACK_CALLTYPE
dn_direct_equal (const void *v1, const void *v2)
{
	return v1 == v2;
}

uint32_t
DN_CALLBACK_CALLTYPE
dn_direct_hash (const void *v1)
{
	return ((uint32_t)(size_t)(v1));
}

bool
DN_CALLBACK_CALLTYPE
dn_int_equal (const void *v1, const void *v2)
{
	return *(int32_t *)v1 == *(int32_t *)v2;
}

uint32_t
DN_CALLBACK_CALLTYPE
dn_int_hash (const void *v1)
{
	return *(uint32_t *)v1;
}

bool
DN_CALLBACK_CALLTYPE
dn_str_equal (const void *v1, const void *v2)
{
	return v1 == v2 || strcmp ((const char*)v1, (const char*)v2) == 0;
}

uint32_t
DN_CALLBACK_CALLTYPE
dn_str_hash (const void *v1)
{
	uint32_t hash = 0;
	char *p = (char *) v1;

	while (*p++)
		hash = (hash << 5) - (hash + *p);

	return hash;
}
