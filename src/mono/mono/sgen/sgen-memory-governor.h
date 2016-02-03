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
void sgen_memgov_init (size_t max_heap, size_t soft_limit, gboolean debug_allowance, double min_allowance_ratio, double save_target);
void sgen_memgov_release_space (mword size, int space);
gboolean sgen_memgov_try_alloc_space (mword size, int space);

/* GC trigger heuristics */
void sgen_memgov_minor_collection_start (void);
void sgen_memgov_minor_collection_end (void);

void sgen_memgov_major_pre_sweep (void);
void sgen_memgov_major_collection_start (void);
void sgen_memgov_major_collection_end (gboolean forced);

void sgen_memgov_collection_start (int generation);
void sgen_memgov_collection_end (int generation, GGTimingInfo* info, int info_count);

gboolean sgen_need_major_collection (mword space_needed);


typedef enum {
	SGEN_ALLOC_INTERNAL = 0,
	SGEN_ALLOC_HEAP = 1,
	SGEN_ALLOC_ACTIVATE = 2
} SgenAllocFlags;

/* OS memory allocation */
void* sgen_alloc_os_memory (size_t size, SgenAllocFlags flags, const char *assert_description);
void* sgen_alloc_os_memory_aligned (size_t size, mword alignment, SgenAllocFlags flags, const char *assert_description);
void sgen_free_os_memory (void *addr, size_t size, SgenAllocFlags flags);

/* Error handling */
void sgen_assert_memory_alloc (void *ptr, size_t requested_size, const char *assert_description);

#endif

