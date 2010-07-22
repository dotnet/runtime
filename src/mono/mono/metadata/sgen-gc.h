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
#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

/* pthread impl */
#include "config.h"
#include <glib.h>
#include <pthread.h>
#include <signal.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/class-internals.h>

/* #define SGEN_PARALLEL_MARK */

/*
 * Turning on heavy statistics will turn off the managed allocator and
 * the managed write barrier.
 */
//#define HEAVY_STATISTICS

#define THREAD_HASH_SIZE 11

#define ARCH_THREAD_TYPE pthread_t
#define ARCH_GET_THREAD pthread_self
#define ARCH_THREAD_EQUALS(a,b) pthread_equal (a, b)

#if SIZEOF_VOID_P == 4
typedef guint32 mword;
#else
typedef guint64 mword;
#endif

/* for use with write barriers */
typedef struct _RememberedSet RememberedSet;
struct _RememberedSet {
	mword *store_next;
	mword *end_set;
	RememberedSet *next;
	mword data [MONO_ZERO_LEN_ARRAY];
};

/* eventually share with MonoThread? */
typedef struct _SgenThreadInfo SgenThreadInfo;

struct _SgenThreadInfo {
	SgenThreadInfo *next;
	ARCH_THREAD_TYPE id;
	unsigned int stop_count; /* to catch duplicate signals */
	int signal;
	int skip;
	volatile int in_critical_region;
	void *stack_end;
	void *stack_start;
	void *stack_start_limit;
	char **tlab_next_addr;
	char **tlab_start_addr;
	char **tlab_temp_end_addr;
	char **tlab_real_end_addr;
	gpointer **store_remset_buffer_addr;
	long *store_remset_buffer_index_addr;
	RememberedSet *remset;
	gpointer runtime_data;
	gpointer stopped_ip;	/* only valid if the thread is stopped */
	MonoDomain *stopped_domain; /* ditto */
	gpointer *stopped_regs;	    /* ditto */
#ifndef HAVE_KW_THREAD
	char *tlab_start;
	char *tlab_next;
	char *tlab_temp_end;
	char *tlab_real_end;
	gpointer *store_remset_buffer;
	long store_remset_buffer_index;
#endif
};

enum {
	MEMORY_ROLE_GEN0,
	MEMORY_ROLE_GEN1,
	MEMORY_ROLE_PINNED,
	MEMORY_ROLE_INTERNAL
};

typedef struct _SgenBlock SgenBlock;
struct _SgenBlock {
	void *next;
	unsigned char role;
};

/* This is also the MAJOR_SECTION_SIZE for the copying major
   collector */
#define SGEN_PINNED_CHUNK_SIZE	(128 * 1024)

#define SGEN_PINNED_CHUNK_FOR_PTR(o)	((SgenBlock*)(((mword)(o)) & ~(SGEN_PINNED_CHUNK_SIZE - 1)))

typedef struct _SgenPinnedChunk SgenPinnedChunk;

#ifdef __APPLE__
const static int suspend_signal_num = SIGXFSZ;
#else
const static int suspend_signal_num = SIGPWR;
#endif
const static int restart_signal_num = SIGXCPU;

/*
 * Recursion is not allowed for the thread lock.
 */
#define LOCK_DECLARE(name) pthread_mutex_t name = PTHREAD_MUTEX_INITIALIZER
#define LOCK_INIT(name)
#define LOCK_GC pthread_mutex_lock (&gc_mutex)
#define UNLOCK_GC pthread_mutex_unlock (&gc_mutex)
#define LOCK_INTERRUPTION pthread_mutex_lock (&interruption_mutex)
#define UNLOCK_INTERRUPTION pthread_mutex_unlock (&interruption_mutex)

#ifdef SGEN_PARALLEL_MARK
#define SGEN_CAS_PTR	InterlockedCompareExchangePointer
#define SGEN_ATOMIC_ADD(x,i)	do {					\
		int __old_x;						\
		do {							\
			__old_x = (x);					\
		} while (InterlockedCompareExchange (&(x), __old_x, __old_x + (i)) != __old_x); \
	} while (0)
#else
#define SGEN_CAS_PTR(p,n,c)	((*(void**)(p) == (void*)(c)) ? (*(void**)(p) = (void*)(n), (void*)(c)) : (*(void**)(p)))
#define SGEN_ATOMIC_ADD(x,i)	((x) += (i))
#endif

/* non-pthread will need to provide their own version of start/stop */
#define USE_SIGNAL_BASED_START_STOP_WORLD 1
/* we intercept pthread_create calls to know which threads exist */
#define USE_PTHREAD_INTERCEPT 1

#ifdef HEAVY_STATISTICS
#define HEAVY_STAT(x)	x
#else
#define HEAVY_STAT(x)
#endif

#define SGEN_ALLOC_ALIGN		8
#define SGEN_ALLOC_ALIGN_BITS	3

#define SGEN_ALIGN_UP(s)		(((s)+(SGEN_ALLOC_ALIGN-1)) & ~(SGEN_ALLOC_ALIGN-1))

typedef void (*IterateObjectCallbackFunc) (char*, size_t, void*);

void* mono_sgen_alloc_os_memory (size_t size, int activate) MONO_INTERNAL;
void* mono_sgen_alloc_os_memory_aligned (mword size, mword alignment, gboolean activate) MONO_INTERNAL;
void mono_sgen_free_os_memory (void *addr, size_t size) MONO_INTERNAL;

int mono_sgen_thread_handshake (int signum) MONO_INTERNAL;
SgenThreadInfo* mono_sgen_thread_info_lookup (ARCH_THREAD_TYPE id) MONO_INTERNAL;
SgenThreadInfo** mono_sgen_get_thread_table (void) MONO_INTERNAL;
void mono_sgen_wait_for_suspend_ack (int count) MONO_INTERNAL;

gboolean mono_sgen_is_worker_thread (pthread_t thread) MONO_INTERNAL;

void mono_sgen_update_heap_boundaries (mword low, mword high) MONO_INTERNAL;

/* Keep in sync with mono_sgen_dump_internal_mem_usage() in dump_heap()! */
enum {
	INTERNAL_MEM_MANAGED,
	INTERNAL_MEM_PIN_QUEUE,
	INTERNAL_MEM_FRAGMENT,
	INTERNAL_MEM_SECTION,
	INTERNAL_MEM_SCAN_STARTS,
	INTERNAL_MEM_FIN_TABLE,
	INTERNAL_MEM_FINALIZE_ENTRY,
	INTERNAL_MEM_DISLINK_TABLE,
	INTERNAL_MEM_DISLINK,
	INTERNAL_MEM_ROOTS_TABLE,
	INTERNAL_MEM_ROOT_RECORD,
	INTERNAL_MEM_STATISTICS,
	INTERNAL_MEM_REMSET,
	INTERNAL_MEM_GRAY_QUEUE,
	INTERNAL_MEM_STORE_REMSET,
	INTERNAL_MEM_MS_TABLES,
	INTERNAL_MEM_MS_BLOCK_INFO,
	INTERNAL_MEM_EPHEMERON_LINK,
	INTERNAL_MEM_MAX
};

#define SGEN_INTERNAL_FREELIST_NUM_SLOTS	30

typedef struct _SgenInternalAllocator SgenInternalAllocator;
struct _SgenInternalAllocator {
	SgenPinnedChunk *chunk_list;
	SgenPinnedChunk *free_lists [SGEN_INTERNAL_FREELIST_NUM_SLOTS];
	long small_internal_mem_bytes [INTERNAL_MEM_MAX];
};

void mono_sgen_init_internal_allocator (void) MONO_INTERNAL;

const char* mono_sgen_internal_mem_type_name (int type) MONO_INTERNAL;
void mono_sgen_report_internal_mem_usage (void) MONO_INTERNAL;
void mono_sgen_report_internal_mem_usage_full (SgenInternalAllocator *alc) MONO_INTERNAL;
void mono_sgen_dump_internal_mem_usage (FILE *heap_dump_file) MONO_INTERNAL;

void mono_sgen_register_fixed_internal_mem_type (int type, size_t size) MONO_INTERNAL;

void* mono_sgen_alloc_internal (int type) MONO_INTERNAL;
void mono_sgen_free_internal (void *addr, int type) MONO_INTERNAL;

void* mono_sgen_alloc_internal_dynamic (size_t size, int type) MONO_INTERNAL;
void mono_sgen_free_internal_dynamic (void *addr, size_t size, int type) MONO_INTERNAL;

void* mono_sgen_alloc_internal_full (SgenInternalAllocator *allocator, size_t size, int type) MONO_INTERNAL;
void mono_sgen_free_internal_full (SgenInternalAllocator *allocator, void *addr, size_t size, int type) MONO_INTERNAL;

void mono_sgen_debug_printf (int level, const char *format, ...) MONO_INTERNAL;

void mono_sgen_internal_scan_objects (SgenInternalAllocator *alc, IterateObjectCallbackFunc callback, void *callback_data) MONO_INTERNAL;
void mono_sgen_internal_scan_pinned_objects (SgenInternalAllocator *alc, IterateObjectCallbackFunc callback, void *callback_data) MONO_INTERNAL;

void** mono_sgen_find_optimized_pin_queue_area (void *start, void *end, int *num) MONO_INTERNAL;

#endif /* __MONO_SGENGC_H__ */
