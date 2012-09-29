/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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
#ifndef __MONO_SGEN_MEMORY_GOVERNOR_H__
#define __MONO_SGEN_MEMORY_GOVERNOR_H__

/* Heap limits */
void sgen_memgov_init (glong max_heap, glong soft_limit, gboolean debug_allowance, double min_allowance_ratio, double save_target) MONO_INTERNAL;
void sgen_memgov_release_space (mword size, int space) MONO_INTERNAL;
gboolean sgen_memgov_try_alloc_space (mword size, int space) MONO_INTERNAL;

/* GC trigger heuristics */
void sgen_memgov_minor_collection_start (void) MONO_INTERNAL;
void sgen_memgov_minor_collection_end (void) MONO_INTERNAL;

void sgen_memgov_major_collection_start (void) MONO_INTERNAL;
void sgen_memgov_major_collection_end (void) MONO_INTERNAL;

void sgen_memgov_collection_start (int generation) MONO_INTERNAL;
void sgen_memgov_collection_end (int generation, GGTimingInfo* info, int info_count) MONO_INTERNAL;

void sgen_register_major_sections_alloced (int num_sections) MONO_INTERNAL;
mword sgen_get_minor_collection_allowance (void) MONO_INTERNAL;
gboolean sgen_need_major_collection (mword space_needed) MONO_INTERNAL;


typedef enum {
	SGEN_ALLOC_INTERNAL = 0,
	SGEN_ALLOC_HEAP = 1,
	SGEN_ALLOC_ACTIVATE = 2
} SgenAllocFlags;

/* OS memory allocation */
void* sgen_alloc_os_memory (size_t size, SgenAllocFlags flags, const char *assert_description) MONO_INTERNAL;
void* sgen_alloc_os_memory_aligned (size_t size, mword alignment, SgenAllocFlags flags, const char *assert_description) MONO_INTERNAL;
void sgen_free_os_memory (void *addr, size_t size, SgenAllocFlags flags) MONO_INTERNAL;

/* Error handling */
void sgen_assert_memory_alloc (void *ptr, const char *assert_description) MONO_INTERNAL;

#endif

