// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_ALLOCATOR_H__
#define __DN_ALLOCATOR_H__

#include "dn-utils.h"
#include <stdlib.h>
#include <memory.h>

#ifdef __cplusplus
extern "C" {
#endif

#define DN_ALLOCATOR_MEM_ALIGN8 8
#define DN_ALLOCATOR_MEM_ALIGN16 16
#define DN_ALLOCATOR_MAX_ALIGNMENT DN_ALLOCATOR_MEM_ALIGN16
#define DN_ALLOCATOR_ALIGN_SIZE(size,align) (((size) + align - 1) & ~(align - 1))
#define DN_ALLOCATOR_ALIGN_PTR_TO(ptr,align) (void *)((((ptrdiff_t)(ptr)) + (ptrdiff_t)(align - 1)) & (~((ptrdiff_t)(align - 1))))

typedef struct _dn_allocator_vtable_t dn_allocator_vtable_t;
typedef struct _dn_allocator_t dn_allocator_t;
typedef struct _dn_allocator_fixed_t dn_allocator_fixed_t;
typedef struct _dn_allocator_fixed_data_t dn_allocator_fixed_data_t;
typedef struct _dn_allocator_fixed_or_malloc_t dn_allocator_fixed_or_malloc_t;
typedef struct _dn_allocator_fixed_data_t dn_allocator_fixed_or_malloc_data_t;

struct _dn_allocator_vtable_t {
	void *(*_alloc)(dn_allocator_t *, size_t);
	void *(*_realloc)(dn_allocator_t *, void *, size_t);
	void (*_free)(dn_allocator_t *, void *);
};

struct _dn_allocator_t {
	dn_allocator_vtable_t *_vtable;
};

struct _dn_allocator_fixed_data_t {
	void *_begin;
	void *_end;
	void *_ptr;
};

struct _dn_allocator_fixed_t {
	dn_allocator_vtable_t *_vtable;
	dn_allocator_fixed_data_t _data;
};

struct _dn_allocator_fixed_or_malloc_t {
	dn_allocator_vtable_t *_vtable;
	dn_allocator_fixed_or_malloc_data_t _data;
};

static inline void *
dn_allocator_alloc (
	dn_allocator_t *allocator,
	size_t size)
{
	return allocator ?
		allocator->_vtable->_alloc (allocator, size) :
		malloc (size);
}

static inline void *
dn_allocator_realloc (
	dn_allocator_t *allocator,
	void *block,
	size_t size)
{
	return allocator ?
		allocator->_vtable->_realloc (allocator, block, size) :
		realloc (block, size);
}

static inline void
dn_allocator_free (
	dn_allocator_t *allocator,
	void *block)
{
	allocator ?
		allocator->_vtable->_free (allocator, block) :
		free (block);
}

dn_allocator_fixed_t *
dn_allocator_fixed_init (
	dn_allocator_fixed_t *allocator,
	void *block,
	size_t size);

dn_allocator_fixed_t *
dn_allocator_fixed_reset (dn_allocator_fixed_t *allocator);

dn_allocator_fixed_or_malloc_t *
dn_allocator_fixed_or_malloc_init (
	dn_allocator_fixed_or_malloc_t *allocator,
	void *block,
	size_t size);

dn_allocator_fixed_or_malloc_t *
dn_allocator_fixed_or_malloc_reset (dn_allocator_fixed_or_malloc_t *allocator);

#define DN_ALLOCATOR_FIXED_OR_MALLOC(var_name, buffer_size) \
	uint8_t __buffer_##var_name [buffer_size]; \
	dn_allocator_fixed_or_malloc_t var_name; \
	dn_allocator_fixed_or_malloc_init (&var_name, __buffer_##var_name, buffer_size);

#define DN_DEFAULT_ALLOCATOR NULL
#define DN_DEFAULT_LOCAL_ALLOCATOR(var_name, buffer_size) DN_ALLOCATOR_FIXED_OR_MALLOC (var_name, buffer_size)

#ifdef __cplusplus
} // extern "C"
#endif

#endif /* __DN_ALLOCATOR_H__ */
