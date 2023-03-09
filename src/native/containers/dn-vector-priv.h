// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_VECTOR_PRIV_H__
#define __DN_VECTOR_PRIV_H__

#include "dn-utils.h"
#include "dn-allocator.h"
#include "dn-vector-types.h"

bool
_dn_vector_ensure_capacity (
	dn_vector_t *vector,
	uint32_t capacity,
	bool calc_capacity);

bool
_dn_vector_insert_range (
	dn_vector_it_t *position,
	const uint8_t *elements,
	uint32_t element_count);

bool
_dn_vector_append_range (
	dn_vector_t *vector,
	const uint8_t *elements,
	uint32_t element_count);

bool
_dn_vector_erase (
	dn_vector_it_t *position,
	dn_vector_dispose_func_t dispose_func);

bool
_dn_vector_erase_fast (
	dn_vector_it_t *position,
	dn_vector_dispose_func_t dispose_func);

dn_vector_it_t
_dn_vector_custom_find (
	dn_vector_t *vector,
	const uint8_t *value,
	dn_vector_equal_func_t equal_func);

static inline dn_vector_result_t
_dn_vector_insert_range_adapter (
	dn_vector_it_t position,
	const uint8_t *elements,
	uint32_t element_count)
{
	dn_vector_result_t result;

	if (position.it == position._internal._vector->size)
		result.result = _dn_vector_append_range (position._internal._vector, elements, element_count);
	else
		result.result = _dn_vector_insert_range (&position, elements, element_count);

	result.it = position;
	return result;
}

static inline dn_vector_result_t
_dn_vector_erase_adapter (
	dn_vector_it_t position,
	dn_vector_dispose_func_t dispose_func)
{
	dn_vector_result_t result;
	result.result = _dn_vector_erase (&position, dispose_func);
	result.it = position;
	return result;
}

static inline dn_vector_result_t
_dn_vector_erase_fast_adapter (
	dn_vector_it_t position,
	dn_vector_dispose_func_t dispose_func)
{
	dn_vector_result_t result;
	result.result = _dn_vector_erase_fast (&position, dispose_func);
	result.it = position;
	return result;
}

static inline void
_dn_vector_find_adapter (
	dn_vector_t *vector,
	const uint8_t *data,
	dn_vector_equal_func_t equal_func,
	dn_vector_it_t *found)
{
	DN_ASSERT (found);
	*found = _dn_vector_custom_find (vector, data, equal_func);
}

#endif /* __DN_VECTOR_PRIV_H__ */
