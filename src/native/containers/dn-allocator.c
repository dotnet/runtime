// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-allocator.h"

static void *
fixed_vtable_alloc (
	dn_allocator_t *allocator,
	size_t size);

static void *
fixed_vtable_realloc (
	dn_allocator_t *allocator,
	void *block,
	size_t size);

static void
fixed_vtable_free (
	dn_allocator_t *allocator,
	void *block);

static void *
fixed_alloc (
	dn_allocator_fixed_data_t *data,
	size_t size);

static void *
fixed_realloc (
	dn_allocator_fixed_data_t *data,
	void *block,
	size_t size);

static void
fixed_free (
	dn_allocator_fixed_data_t *data,
	void *block);

static bool
fixed_owns_ptr (
	dn_allocator_fixed_data_t *data,
	void *ptr);

static void *
fixed_memcpy (
	void *dst,
	void *src,
	size_t size);

static void *
fixed_or_malloc_vtable_alloc (
	dn_allocator_t *allocator,
	size_t size);

static void *
fixed_or_malloc_vtable_realloc (
	dn_allocator_t *allocator,
	void *block,
	size_t size);

static void
fixed_or_malloc_vtable_free (
	dn_allocator_t *allocator,
	void *block);

static void *
fixed_or_malloc_alloc (
	dn_allocator_fixed_or_malloc_data_t *data,
	size_t size);

static void *
fixed_or_malloc_realloc (
	dn_allocator_fixed_or_malloc_data_t *data,
	void *block,
	size_t size);

static void
fixed_or_malloc_free (
	dn_allocator_fixed_or_malloc_data_t *data,
	void *block);

static dn_allocator_vtable_t fixed_vtable = {
	fixed_vtable_alloc,
	fixed_vtable_realloc,
	fixed_vtable_free,
};

static dn_allocator_vtable_t fixed_or_malloc_vtable = {
	fixed_or_malloc_vtable_alloc,
	fixed_or_malloc_vtable_realloc,
	fixed_or_malloc_vtable_free,
};

static void *
fixed_vtable_alloc (
	dn_allocator_t *allocator,
	size_t size)
{
	return fixed_alloc (&((dn_allocator_fixed_t *)allocator)->_data, size);
}

static void *
fixed_vtable_realloc (
	dn_allocator_t *allocator,
	void *block,
	size_t size)
{
	return fixed_realloc (&((dn_allocator_fixed_t *)allocator)->_data, block, size);
}

static void
fixed_vtable_free (
	dn_allocator_t *allocator,
	void *block)
{
	fixed_free (&((dn_allocator_fixed_t *)allocator)->_data, block);
}

static void *
fixed_alloc (
	dn_allocator_fixed_data_t *data,
	size_t size)
{
	void *ptr = data->_ptr;
	void *new_ptr = (uint8_t *)ptr + DN_ALLOCATOR_ALIGN_SIZE (size + DN_ALLOCATOR_MEM_ALIGN8, DN_ALLOCATOR_MEM_ALIGN8);

	// Check if new memory address triggers OOM.
	if (!fixed_owns_ptr (data, new_ptr))
		return NULL;

	data->_ptr = new_ptr;

	*((size_t *)ptr) = size;
	return (uint8_t *)ptr + DN_ALLOCATOR_MEM_ALIGN8;
}

static void *
fixed_realloc (
	dn_allocator_fixed_data_t *data,
	void *block,
	size_t size)
{
	if (block && !fixed_owns_ptr (data, block))
		return NULL;

	void *ptr = data->_ptr;
	void *new_ptr = (uint8_t *)ptr + DN_ALLOCATOR_ALIGN_SIZE (size + DN_ALLOCATOR_MEM_ALIGN8, DN_ALLOCATOR_MEM_ALIGN8);

	// Check if new memory address triggers OOM.
	if (!fixed_owns_ptr (data, new_ptr))
		return NULL;

	data->_ptr = new_ptr;

	if (block)
		fixed_memcpy ((uint8_t *)ptr + DN_ALLOCATOR_MEM_ALIGN8, block, size);

	*((size_t *)ptr) = size;
	return (uint8_t *)ptr + DN_ALLOCATOR_MEM_ALIGN8;
}

static void
fixed_free (
	dn_allocator_fixed_data_t *data,
	void *block)
{
	DN_UNREFERENCED_PARAMETER (data);
	DN_UNREFERENCED_PARAMETER (block);

	// Fixed buffer doesn't support free.
}

static inline bool
fixed_owns_ptr (
	dn_allocator_fixed_data_t *data,
	void *ptr)
{
	return (ptr >= data->_begin && ptr < data->_end);
}

static void *
fixed_memcpy (
	void *dst,
	void *src,
	size_t size)
{
	void *result = NULL;
	if (dst && src) {
		size_t *src_size = (size_t *)((uint8_t *)src - DN_ALLOCATOR_MEM_ALIGN8);
		if (src_size && src_size < (size_t *)src)
			result = memcpy (dst, src, size < *src_size ? size : *src_size);
	}

	return result;
}

static void *
fixed_or_malloc_vtable_alloc (
	dn_allocator_t *allocator,
	size_t size)
{
	return fixed_or_malloc_alloc (&((dn_allocator_fixed_or_malloc_t *)allocator)->_data, size);
}

static void *
fixed_or_malloc_vtable_realloc (
	dn_allocator_t *allocator,
	void *block,
	size_t size)
{
	return fixed_or_malloc_realloc (&((dn_allocator_fixed_or_malloc_t *)allocator)->_data, block, size);
}

static void
fixed_or_malloc_vtable_free (
	dn_allocator_t *allocator,
	void *block)
{
	fixed_or_malloc_free (&((dn_allocator_fixed_or_malloc_t *)allocator)->_data, block);
}

static void *
fixed_or_malloc_alloc (
	dn_allocator_fixed_or_malloc_data_t *data,
	size_t size)
{
	void *result = NULL;

	result = fixed_alloc (data, size);
	if (!result)
		result = malloc (size);

	return result;
}

static void *
fixed_or_malloc_realloc (
	dn_allocator_fixed_or_malloc_data_t *data,
	void *block,
	size_t size)
{
	// Check if ptr is owned by fixed buffer, if not, its own by heap.
	if (block && !fixed_owns_ptr (data, block))
		return realloc (block, size);

	// Try realloc using fixed buffer.
	void *result = fixed_realloc (data, block, size);
	if (!result) {
		// Fixed buffer OOM, fallback to heap.
		result = malloc (size);

		// Copy data from fixed buffer to allocated heap memory.
		if (block && result)
			result = fixed_memcpy (result, block, size);
	}

	return result;
}

static void
fixed_or_malloc_free (
	dn_allocator_fixed_or_malloc_data_t *data,
	void *block)
{
	if (fixed_owns_ptr (data, block))
		fixed_free (data, block);
	else
		free (block);
}

dn_allocator_fixed_t *
dn_allocator_fixed_init (
	dn_allocator_fixed_t *allocator,
	void *block,
	size_t size)
{
	void *begin = DN_ALLOCATOR_ALIGN_PTR_TO (block, DN_ALLOCATOR_MEM_ALIGN8);
	void *end = (uint8_t *)begin + size - ((uint8_t *)begin - (uint8_t *)block);

	if (end < begin)
		return NULL;

	allocator->_data._begin = begin;
	allocator->_data._ptr = begin;
	allocator->_data._end = end;

	allocator->_vtable = &fixed_vtable;

	return allocator;
}

dn_allocator_fixed_t *
dn_allocator_fixed_reset(dn_allocator_fixed_t *allocator)
{
	allocator->_data._ptr = allocator->_data._begin;
	return allocator;
}

dn_allocator_fixed_or_malloc_t *
dn_allocator_fixed_or_malloc_reset(dn_allocator_fixed_or_malloc_t *allocator)
{
	allocator->_data._ptr = allocator->_data._begin;
	return allocator;
}

dn_allocator_fixed_or_malloc_t *
dn_allocator_fixed_or_malloc_init (
	dn_allocator_fixed_or_malloc_t *allocator,
	void *block,
	size_t size)
{
	void *begin = DN_ALLOCATOR_ALIGN_PTR_TO (block, DN_ALLOCATOR_MEM_ALIGN8);
	void *end = (uint8_t *)begin + size - ((uint8_t *)begin - (uint8_t *)block);

	if (end < begin)
		return NULL;

	allocator->_data._begin = begin;
	allocator->_data._ptr = begin;
	allocator->_data._end = end;

	allocator->_vtable = &fixed_or_malloc_vtable;

	return allocator;
}
