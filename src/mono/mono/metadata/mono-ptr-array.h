/**
 * \file
 * GC aware equivalente of g_ptr_array
 *
 * Author:
 *	Rodrigo Kumpera  <rkumpera@novell.com>
 *
 * (C) 2010 Novell, Inc
 */

#ifndef __MONO_PTR_ARRAY_H__
#define __MONO_PTR_ARRAY_H__


#include <glib.h>

#include "mono/metadata/gc-internals.h"

/* This is an implementation of a growable pointer array that avoids doing memory allocations for small sizes.
 * It works by allocating an initial small array on stack and only going to gc tracked memory if needed.
 * The array elements are assumed to be object references.
 */
typedef struct {
	void **data;
	int size;
	int capacity;
	MonoGCRootSource source;
	void *key;
	const char *msg;
} MonoPtrArray;

#define MONO_PTR_ARRAY_MAX_ON_STACK (16)

#define mono_ptr_array_init(ARRAY, INITIAL_SIZE, SOURCE, KEY, MSG) do {\
	(ARRAY).size = 0; \
	(ARRAY).capacity = MAX (INITIAL_SIZE, MONO_PTR_ARRAY_MAX_ON_STACK); \
	(ARRAY).source = SOURCE; \
	(ARRAY).key = KEY; \
	(ARRAY).msg = MSG; \
	(ARRAY).data = INITIAL_SIZE > MONO_PTR_ARRAY_MAX_ON_STACK \
		? (void **)mono_gc_alloc_fixed (sizeof (void*) * INITIAL_SIZE, mono_gc_make_root_descr_all_refs (INITIAL_SIZE), SOURCE, NULL, MSG) \
		: g_newa (void*, MONO_PTR_ARRAY_MAX_ON_STACK); \
} while (0)

#define mono_ptr_array_destroy(ARRAY) do {\
	if ((ARRAY).capacity > MONO_PTR_ARRAY_MAX_ON_STACK) \
		mono_gc_free_fixed ((ARRAY).data); \
} while (0)

#define mono_ptr_array_append(ARRAY, VALUE) do { \
	if ((ARRAY).size >= (ARRAY).capacity) {\
	void **__tmp = (void **)mono_gc_alloc_fixed (sizeof (void*) * (ARRAY).capacity * 2, mono_gc_make_root_descr_all_refs ((ARRAY).capacity * 2), (ARRAY).source, (ARRAY).key, (ARRAY).msg); \
		mono_gc_memmove_aligned ((void *)__tmp, (ARRAY).data, (ARRAY).capacity * sizeof (void*)); \
		if ((ARRAY).capacity > MONO_PTR_ARRAY_MAX_ON_STACK)	\
			mono_gc_free_fixed ((ARRAY).data);	\
		(ARRAY).data = __tmp;	\
		(ARRAY).capacity *= 2;\
	}\
	((ARRAY).data [(ARRAY).size++] = VALUE); \
} while (0)

#define mono_ptr_array_sort(ARRAY, COMPARE_FUNC) do { \
	mono_qsort ((ARRAY).data, (ARRAY).size, sizeof (gpointer), (COMPARE_FUNC)); \
} while (0)

#define mono_ptr_array_set(ARRAY, IDX, VALUE) do { \
	((ARRAY).data [(IDX)] = VALUE); \
} while (0)

#define mono_ptr_array_get(ARRAY, IDX) ((ARRAY).data [(IDX)])

#define mono_ptr_array_size(ARRAY) ((ARRAY).size)

#define mono_ptr_array_reset(ARRAY) do { \
	(ARRAY).size = 0; \
} while (0)

#define mono_ptr_array_clear(ARRAY) do { \
	(ARRAY).size = 0; \
	mono_gc_bzero_aligned ((ARRAY).data, (ARRAY).capacity * sizeof (void*)); \
} while (0)

#endif
