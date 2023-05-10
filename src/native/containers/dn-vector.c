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

#include "dn-vector.h"

#define INITIAL_CAPACITY 64
#define CALC_NEW_CAPACITY(capacity) ((capacity + (capacity >> 1) + 63) & ~63)

#define element_offset(p,i) \
	((p)->data + (i) * (p)->_internal._element_size)

#define element_length(p,i) \
	((i) * (p)->_internal._element_size)

#define check_attribute(vector, value) ((vector->_internal._attributes & (uint32_t)value) == value)

bool
_dn_vector_ensure_capacity (
	dn_vector_t *vector,
	uint32_t capacity,
	bool calc_capacity)
{
	if (capacity != 0 && capacity <= (uint64_t)(vector->_internal._capacity))
		return true;

	uint64_t new_capacity = calc_capacity ? CALC_NEW_CAPACITY (capacity) : capacity;

	if (DN_UNLIKELY (new_capacity > (uint64_t)(UINT32_MAX)))
		return false;

	size_t realloc_size;
	if (DN_UNLIKELY (!dn_safe_size_t_multiply (element_length (vector, 1), (size_t)new_capacity, &realloc_size)))
		return false;

	uint8_t *data = (uint8_t *)dn_allocator_realloc (vector->_internal._allocator, vector->data, realloc_size);

	if (DN_UNLIKELY (!data && realloc_size != 0))
		return false;

	vector->data = data;

	if (vector->data && check_attribute (vector, DN_VECTOR_ATTRIBUTE_MEMORY_INIT)) {
		// Checks already verified that element_offset won't overflow.
		// new_capacity > vector capacity, so new_capacity - vector capacity won't underflow.
		// dn_safe_size_t_multiply already verified element_length won't overflow.
		memset (element_offset (vector, vector->_internal._capacity), 0, element_length (vector, (uint32_t)new_capacity - vector->_internal._capacity));
	}

	// Overflow already checked.
	vector->_internal._capacity = (uint32_t)new_capacity;

	return vector->data != NULL;
}

bool
_dn_vector_insert_range (
	dn_vector_it_t *position,
	const uint8_t *elements,
	uint32_t element_count)
{
	DN_ASSERT (elements && element_count != 0);

	dn_vector_t *vector = position->_internal._vector;

	uint64_t new_capacity = (uint64_t)vector->size + (uint64_t)element_count;
	if (DN_UNLIKELY (new_capacity > (uint64_t)(vector->_internal._capacity))) {
		if (DN_UNLIKELY (!_dn_vector_ensure_capacity (vector, (uint32_t)new_capacity, true)))
			return false;
	}

	uint64_t insert_offset = (uint64_t)position->it + (uint64_t)element_count;
	uint64_t size_to_move = (uint64_t)vector->size - (uint64_t)position->it;
	if (DN_UNLIKELY (insert_offset > new_capacity || size_to_move > vector->size))
		return false;

	/* first move the existing elements out of the way */
	/* element_offset won't overflow since insert_offset and position is smaller than new_capacity already checked in ensure_capacity */
	/* element_length won't overflow since size_to_move is smaller than new_capacity already checked in ensure_capacity */
	/* element_length won't underflow since size_to_move is already verfied to be smaller or equal to vector->size */
	memmove (element_offset (vector, (uint32_t)insert_offset), element_offset (vector, position->it), element_length (vector, (uint32_t)size_to_move));

	/* then copy the new elements into the array */
	/* element_offset won't overflow since position is smaller than new_capacity already checked in encure_capacity */
	/* element_length won't overflow since element_count is included in new_capacity already checked in encure_capacity */
	memmove (element_offset (vector, position->it), elements, element_length (vector, element_count));

	// Overflow already checked.
	vector->size += element_count;

	position->it = (uint32_t)insert_offset;

	return true;
}

bool
_dn_vector_append_range (
	dn_vector_t *vector,
	const uint8_t *elements,
	uint32_t element_count)
{
	DN_ASSERT (vector && elements && element_count != 0);

	uint64_t new_capacity = (uint64_t)vector->size + (uint64_t)element_count;
	if (DN_UNLIKELY (new_capacity > (uint64_t)(vector->_internal._capacity))) {
		if (DN_UNLIKELY (!_dn_vector_ensure_capacity (vector, (uint32_t)new_capacity, true)))
			return false;
	}

	/* ensure_capacity already verified element_offset and element_length won't overflow. */
	memmove (element_offset (vector, vector->size), elements, element_length (vector, element_count));

	// Overflowed already checked.
	vector->size += element_count;

	return true;
}

bool
_dn_vector_erase (
	dn_vector_it_t *position,
	dn_vector_dispose_func_t dispose_func)
{
	DN_ASSERT (position && !dn_vector_it_end (*position));

	dn_vector_t *vector = position->_internal._vector;

	DN_ASSERT (vector && vector->size != 0);

	uint64_t insert_offset = (uint64_t)position->it + 1;
	int64_t size_to_move = (int64_t)vector->size - (int64_t)position->it - 1;
	if (DN_UNLIKELY (insert_offset > vector->_internal._capacity || size_to_move < 0))
		return false;

	if (dispose_func)
		dispose_func (element_offset (vector, position->it));

	/* element_offset won't overflow since insert_offset and position is smaller than current capacity */
	/* element_length won't overflow since size_to_move is smaller than current capacity */
	/* element_length won't underflow since size_to_move is already verfied to be 0 or larger */
	memmove (element_offset (vector, position->it), element_offset (vector, (uint32_t)insert_offset), element_length (vector, (uint32_t)size_to_move));

	vector->size --;

	if (check_attribute (vector, DN_VECTOR_ATTRIBUTE_MEMORY_INIT))
		memset (element_offset(vector, vector->size), 0, element_length (vector, 1));

	return true;
}

bool
_dn_vector_erase_fast (
	dn_vector_it_t *position,
	dn_vector_dispose_func_t dispose_func)
{
	DN_ASSERT (position && !dn_vector_it_end (*position));

	dn_vector_t *vector = position->_internal._vector;

	DN_ASSERT (vector && vector->size != 0);

	if (dispose_func)
		dispose_func (element_offset (vector, position->it));

	vector->size --;

	/* element_offset won't overflow since position is smaller than current capacity */
	/* element_offset won't overflow since vector->size - 1 is smaller than current capacity */
	/* vector->size - 1 won't underflow since vector->size > 0 */
	memmove (element_offset (vector, position->it), element_offset (vector, vector->size), element_length (vector, 1));

	if (check_attribute (vector, DN_VECTOR_ATTRIBUTE_MEMORY_INIT))
		memset (element_offset(vector, vector->size), 0, element_length (vector, 1));

	return true;
}

dn_vector_it_t
_dn_vector_custom_find (
	dn_vector_t *vector,
	const uint8_t *value,
	dn_vector_equal_func_t equal_func)
{
	DN_ASSERT (vector);

	dn_vector_it_t found = dn_vector_end (vector);
	for (uint32_t i = 0; i < vector->size; i++) {
		if ((equal_func && equal_func (element_offset (vector, i), value)) || (!equal_func && !memcmp (element_offset (vector, i), value, element_length (vector, 1)))) {
			found.it = i;
			break;
		}
	}

	return found;
}

dn_vector_t *
dn_vector_custom_alloc (
	const dn_vector_custom_alloc_params_t *params,
	uint32_t element_size)
{
	dn_allocator_t *allocator = params ? params->allocator : DN_DEFAULT_ALLOCATOR;

	dn_vector_t *vector = (dn_vector_t *)dn_allocator_alloc (allocator, sizeof (dn_vector_t));
	if (!dn_vector_custom_init (vector, params, element_size)) {
		dn_allocator_free (allocator, vector);
		return NULL;
	}

	return vector;
}

bool
dn_vector_custom_init (
	dn_vector_t *vector,
	const dn_vector_custom_alloc_params_t *params,
	uint32_t element_size)
{
	uint32_t capacity = INITIAL_CAPACITY;

	if (DN_UNLIKELY (!vector))
		return false;

	DN_ASSERT (element_size != 0);

	memset (vector, 0, sizeof(dn_vector_t));

	vector->_internal._element_size = element_size;

	if (params) {
		vector->_internal._allocator = params->allocator;
		vector->_internal._attributes = params->attributes;
		if (params->capacity != 0)
			capacity = params->capacity;
	}

	if (DN_UNLIKELY (!_dn_vector_ensure_capacity (vector, capacity, false))) {
		dn_vector_dispose (vector);
		return false;
	}

	return true;
}

void
dn_vector_custom_free (
	dn_vector_t *vector,
	dn_vector_dispose_func_t dispose_func)
{
	dn_vector_custom_dispose (vector, dispose_func);
	dn_allocator_free (vector->_internal._allocator, vector);
}

void
dn_vector_custom_dispose (
	dn_vector_t *vector,
	dn_vector_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY (!vector))
		return;

	if (dispose_func) {
		for(uint32_t i = 0; i < vector->size; i++)
			dispose_func ((element_offset (vector, i)));
	}

	dn_allocator_free (vector->_internal._allocator, vector->data);
}

bool
dn_vector_reserve (
	dn_vector_t *vector,
	uint32_t capacity)
{
	DN_ASSERT (vector);
	return _dn_vector_ensure_capacity (vector, capacity, true);
}

uint32_t
dn_vector_capacity (const dn_vector_t *vector)
{
	DN_ASSERT (vector);
	return vector->_internal._capacity;
}

bool
dn_vector_custom_resize (
	dn_vector_t *vector,
	uint32_t size,
	dn_vector_dispose_func_t dispose_func)
{
	DN_ASSERT (vector);

	if (size == vector->_internal._capacity)
		return true;

	if (size > vector->_internal._capacity)
		if (DN_UNLIKELY (!_dn_vector_ensure_capacity (vector, size, true)))
			return false;
	
	if (size < vector->size) {
		if (dispose_func) {
			for (uint32_t i = size; i < vector->size; i++)
				dispose_func (element_offset (vector, i));
		}

		if (check_attribute (vector, DN_VECTOR_ATTRIBUTE_MEMORY_INIT))
			memset (element_offset(vector, size), 0, element_length (vector, vector->size - size));

	}

	vector->size = size;
	return true;
}

void
dn_vector_custom_pop_back (
	dn_vector_t* vector,
	dn_vector_dispose_func_t dispose_func)
{
	DN_ASSERT (vector && vector->size != 0);

	vector->size --;

	if (dispose_func)
		dispose_func (element_offset (vector, vector->size));

	if (check_attribute (vector, DN_VECTOR_ATTRIBUTE_MEMORY_INIT))
		memset (element_offset (vector, vector->size), 0, vector->_internal._element_size);
}

void
dn_vector_for_each (
	const dn_vector_t *vector,
	dn_vector_for_each_func_t for_each_func,
	void *user_data)
{
	DN_ASSERT (vector && for_each_func);

	for(uint32_t i = 0; i < vector->size; i++)
		for_each_func ((void *)(element_offset (vector, i)), user_data);
}

void
dn_vector_sort (
	dn_vector_t *vector,
	dn_vector_compare_func_t compare_func)
{
	DN_ASSERT (vector);

	if (DN_UNLIKELY (vector->size < 2))
		return;

	qsort ((void *)vector->data, vector->size, element_length (vector, 1), (int (DN_CALLBACK_CALLTYPE *)(const void *, const void *))compare_func);
}
