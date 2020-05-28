/**
 * \file
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGEN_MEMORY_GOVERNOR_H__
#define __MONO_SGEN_MEMORY_GOVERNOR_H__

/* Heap limits */
void sgen_memgov_init (size_t max_heap, size_t soft_limit, gboolean debug_allowance, double min_allowance_ratio, double save_target);
void sgen_memgov_release_space (mword size, int space);
gboolean sgen_memgov_try_alloc_space (mword size, int space);

/* GC trigger heuristics */
void sgen_memgov_minor_collection_start (void);
void sgen_memgov_minor_collection_end (const char *reason, gboolean is_overflow);

void sgen_memgov_major_pre_sweep (void);
void sgen_memgov_major_post_sweep (mword used_slots_size);
void sgen_memgov_major_collection_start (gboolean concurrent, const char *reason);
void sgen_memgov_major_collection_end (gboolean forced, gboolean concurrent, const char *reason, gboolean is_overflow);

void sgen_memgov_collection_start (int generation);
void sgen_memgov_collection_end (int generation, gint64 stw);

gboolean sgen_need_major_collection (mword space_needed, gboolean *forced);


typedef enum {
	SGEN_ALLOC_INTERNAL = 0,
	SGEN_ALLOC_HEAP = 1,
	SGEN_ALLOC_ACTIVATE = 2
} SgenAllocFlags;

typedef enum {
	SGEN_LOG_NURSERY,
	SGEN_LOG_MAJOR_SERIAL,
	SGEN_LOG_MAJOR_CONC_START,
	SGEN_LOG_MAJOR_CONC_FINISH,
	SGEN_LOG_MAJOR_SWEEP_FINISH
} SgenLogType;

typedef struct {
	SgenLogType type;
	const char *reason;
	gboolean is_overflow;
	gint64 time;
	mword promoted_size;
	mword major_size;
	mword major_size_in_use;
	mword los_size;
	mword los_size_in_use;
} SgenLogEntry;

/* OS memory allocation */
void* sgen_alloc_os_memory (size_t size, SgenAllocFlags flags, const char *assert_description, MonoMemAccountType type);
void* sgen_alloc_os_memory_aligned (size_t size, mword alignment, SgenAllocFlags flags, const char *assert_description, MonoMemAccountType type);
void sgen_free_os_memory (void *addr, size_t size, SgenAllocFlags flags, MonoMemAccountType type);

/* Error handling */
void sgen_assert_memory_alloc (void *ptr, size_t requested_size, const char *assert_description);

#endif

