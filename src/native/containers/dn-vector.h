// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_VECTOR_H__
#define __DN_VECTOR_H__

#include "dn-utils.h"
#include "dn-allocator.h"
#include "dn-vector-types.h"
#include "dn-vector-priv.h"

static inline dn_vector_it_t
dn_vector_begin (dn_vector_t *vector)
{
	DN_ASSERT (vector);
	dn_vector_it_t it = { 0, { vector } };
	return it;
}

static inline dn_vector_it_t
dn_vector_end (dn_vector_t *vector)
{
	DN_ASSERT (vector);
	dn_vector_it_t it = { vector->size, { vector } };
	return it;
}

static inline void
dn_vector_it_advance (
	dn_vector_it_t *it,
	ptrdiff_t n)
{
	DN_ASSERT (it && ((it->it + n) < UINT32_MAX));
	it->it = it->it + (int32_t)n;
}

static inline dn_vector_it_t
dn_vector_it_next (dn_vector_it_t it)
{
	DN_ASSERT ((it.it + 1) < UINT32_MAX);
	it.it++;
	return it;
}

static inline dn_vector_it_t
dn_vector_it_prev (dn_vector_it_t it)
{
	DN_ASSERT (it.it > 0);
	it.it--;
	return it;
}

static inline dn_vector_it_t
dn_vector_it_next_n (
	dn_vector_it_t it,
	uint32_t n)
{
	DN_ASSERT ((it.it + n) < UINT32_MAX);
	it.it += n;
	return it;
}

static inline dn_vector_it_t
dn_vector_it_prev_n (
	dn_vector_it_t it,
	uint32_t n)
{
	DN_ASSERT (it.it >= n);
	it.it -= n;
	return it;
}

static inline uint8_t *
dn_vector_it_data (dn_vector_it_t it)
{
	DN_ASSERT (it._internal._vector && it._internal._vector->data);
	return it._internal._vector->data + (it._internal._vector->_internal._element_size * it.it);
}

#define dn_vector_it_data_t(it, type) \
	(type *)dn_vector_it_data ((it))

static inline bool
dn_vector_it_begin (dn_vector_it_t it)
{
	DN_ASSERT (it._internal._vector);
	return it.it == 0;
}

static inline bool
dn_vector_it_end (dn_vector_it_t it)
{
	DN_ASSERT (it._internal._vector);
	return it.it == it._internal._vector->size;
}

#define DN_VECTOR_FOREACH_BEGIN(var_type, var_name, vector) do { \
	var_type var_name; \
	DN_ASSERT (sizeof (var_type) == (vector)->_internal._element_size); \
	for (uint32_t __i_ ## var_name = 0; __i_ ## var_name < (vector)->size; ++__i_ ## var_name) { \
		var_name = *dn_vector_index_t (vector, var_type, __i_##var_name);

#define DN_VECTOR_FOREACH_RBEGIN(var_type, var_name, vector) do { \
	var_type var_name; \
	DN_ASSERT (sizeof (var_type) == (vector)->_internal._element_size); \
	for (uint32_t __i_ ## var_name = (vector)->size; __i_ ## var_name > 0; --__i_ ## var_name) { \
		var_name = *dn_vector_index_t (vector, var_type, __i_ ## var_name - 1);

#define DN_VECTOR_FOREACH_END \
		} \
	} while (0)

dn_vector_t *
dn_vector_custom_alloc (
	const dn_vector_custom_alloc_params_t *params,
	uint32_t element_size);

#define dn_vector_custom_alloc_t(params, element_type) \
	dn_vector_custom_alloc (params, sizeof (element_type))

static inline dn_vector_t *
dn_vector_alloc (uint32_t element_size)
{
	return dn_vector_custom_alloc (NULL, element_size);
}

#define dn_vector_alloc_t(element_type) \
	dn_vector_alloc (sizeof (element_type))

void
dn_vector_custom_free (
	dn_vector_t *vector,
	dn_vector_dispose_func_t dispose_func);

static inline void
dn_vector_free (dn_vector_t *vector)
{
	dn_vector_custom_free (vector, NULL);
}

bool
dn_vector_custom_init (
	dn_vector_t *vector,
	const dn_vector_custom_alloc_params_t *params,
	uint32_t element_size);

#define dn_vector_custom_init_t(vector, params, element_type) \
	dn_vector_custom_init ((vector), (params), sizeof (element_type))

static inline bool
dn_vector_init (
	dn_vector_t *vector,
	uint32_t element_size)
{
	return dn_vector_custom_init (vector, NULL, element_size);
}

#define dn_vector_init_t(vector, element_type) \
	dn_vector_init (vector, sizeof (element_type))

void
dn_vector_custom_dispose (
	dn_vector_t *vector,
	dn_vector_dispose_func_t dispose_func);

static inline void
dn_vector_dispose (dn_vector_t *vector)
{
	dn_vector_custom_dispose (vector, NULL);
}

static inline uint8_t *
dn_vector_index (dn_vector_t *vector, uint32_t size, uint32_t index)
{
	DN_ASSERT (vector && index < vector ->size);
	return ((uint8_t *)(vector->data) + (size * index));
}

#define dn_vector_index_t(vector, type, index) \
	((type*)(dn_vector_index ((vector), sizeof (type), (index))))

static inline uint8_t *
dn_vector_at (dn_vector_t *vector, uint32_t size, uint32_t index)
{
	DN_ASSERT (vector);
	if (index >= vector->size)
		return NULL;
	return dn_vector_index (vector, size, index);
}

#define dn_vector_at_t(vector, type, index) \
	((type *)dn_vector_at(vector, sizeof (type), index))

#define dn_vector_front_t(vector, type) \
	dn_vector_index_t(vector, type, 0)

#define dn_vector_back_t(vector, type) \
	dn_vector_index_t(vector, type, (vector)->size != 0 ? (vector)->size - 1 : 0)

static inline uint8_t *
dn_vector_data (dn_vector_t *vector)
{
	DN_ASSERT (vector);
	return vector->data;
}

#define dn_vector_data_t(vector,type) \
	((type *)dn_vector_data (vector))

static inline bool
dn_vector_empty (const dn_vector_t *vector)
{
	DN_ASSERT (vector);
	return vector->size == 0;
}

static inline uint32_t
dn_vector_size (const dn_vector_t *vector)
{
	DN_ASSERT (vector);
	return vector->size;
}

static inline uint32_t
dn_vector_max_size (const dn_vector_t *vector)
{
	DN_UNREFERENCED_PARAMETER (vector);
	return UINT32_MAX;
}

bool
dn_vector_reserve (
	dn_vector_t *vector,
	uint32_t capacity);

uint32_t
dn_vector_capacity (const dn_vector_t *vector);

#define dn_vector_insert(position, element) \
	_dn_vector_insert_range_adapter ((position), (const uint8_t *)&(element), 1)

#define dn_vector_insert_range(position, elements, element_count) \
	_dn_vector_insert_range_adapter ((position), (const uint8_t *)(elements), (element_count))

#define dn_vector_custom_erase(position, dispose_func) \
	_dn_vector_erase_adapter ((position), (dispose_func))

#define dn_vector_erase(position) \
	_dn_vector_erase_adapter ((position), NULL)

#define dn_vector_custom_erase_fast(position, dispose_func) \
	_dn_vector_erase_fast_adapter ((position), (dispose_func))

#define dn_vector_erase_fast(position) \
	_dn_vector_erase_fast_adapter ((position), NULL)

bool
dn_vector_custom_resize (
	dn_vector_t *vector,
	uint32_t size,
	dn_vector_dispose_func_t dispose_func);

static inline bool
dn_vector_resize (
	dn_vector_t *vector,
	uint32_t size)
{
	return dn_vector_custom_resize (vector, size, NULL);
}

static inline void
dn_vector_custom_clear (
	dn_vector_t *vector,
	dn_vector_dispose_func_t dispose_func)
{
	dn_vector_custom_resize (vector, 0, dispose_func);
}

static inline void
dn_vector_clear (dn_vector_t *vector)
{
	dn_vector_resize (vector, 0);
}

#define dn_vector_push_back(vector, element) \
	_dn_vector_append_range ((vector), (const uint8_t *)&(element), 1)

void
dn_vector_custom_pop_back (
	dn_vector_t* vector,
	dn_vector_dispose_func_t dispose_func);

static inline void
dn_vector_pop_back (dn_vector_t *vector)
{
	dn_vector_custom_pop_back (vector, NULL);
}

void
dn_vector_for_each (
	const dn_vector_t *vector,
	dn_vector_for_each_func_t for_each_func,
	void *user_data);

void
dn_vector_sort (
	dn_vector_t *vector,
	dn_vector_compare_func_t compare_func);

static inline dn_vector_it_t
dn_vector_custom_find (
	dn_vector_t *vector,
	const uint8_t *value,
	dn_vector_equal_func_t equal_func)
{
	return _dn_vector_custom_find (vector, value, equal_func);
}

static inline dn_vector_it_t
dn_vector_find (
	dn_vector_t *vector,
	const uint8_t *value,
	dn_vector_equal_func_t equal_func)
{
	return dn_vector_custom_find (vector, value, equal_func);
}

#endif /* __DN_VECTOR_H__ */
