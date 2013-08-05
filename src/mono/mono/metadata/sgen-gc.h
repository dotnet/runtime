/*
 * sgen-gc.c: Simple generational GC.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

/* pthread impl */
#include "config.h"

#ifdef HAVE_SGEN_GC

typedef struct _SgenThreadInfo SgenThreadInfo;
#define THREAD_INFO_TYPE SgenThreadInfo

#include <glib.h>
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#include <signal.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-mutex.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/sgen-conf.h>
#include <mono/metadata/sgen-archdep.h>
#include <mono/metadata/sgen-descriptor.h>
#include <mono/metadata/sgen-gray.h>
#include <mono/metadata/sgen-hash-table.h>

/* The method used to clear the nursery */
/* Clearing at nursery collections is the safest, but has bad interactions with caches.
 * Clearing at TLAB creation is much faster, but more complex and it might expose hard
 * to find bugs.
 */
typedef enum {
	CLEAR_AT_GC,
	CLEAR_AT_TLAB_CREATION
} NurseryClearPolicy;

NurseryClearPolicy sgen_get_nursery_clear_policy (void) MONO_INTERNAL;

#define SGEN_TV_DECLARE(name) gint64 name
#define SGEN_TV_GETTIME(tv) tv = mono_100ns_ticks ()
#define SGEN_TV_ELAPSED(start,end) (int)((end-start) / 10)
#define SGEN_TV_ELAPSED_MS(start,end) ((SGEN_TV_ELAPSED((start),(end)) + 500) / 1000)

/* eventually share with MonoThread? */
/*
 * This structure extends the MonoThreadInfo structure.
 */
struct _SgenThreadInfo {
	MonoThreadInfo info;
	/*
	This is set to TRUE when STW fails to suspend a thread, most probably because the
	underlying thread is dead.
	*/
	int skip;
	volatile int in_critical_region;

	/*
	Since threads can be created concurrently during STW, it's possible to reach a stable
	state where we find that the world is stopped but there are registered threads that have
	not been suspended.

	Our hope is that those threads are harmlesly blocked in the GC lock trying to finish registration.

	To handle this scenario we set this field on each thread that have joined the current STW phase.
	The GC should ignore unjoined threads.
	*/
	gboolean joined_stw;

	/*
	This is set to TRUE by STW when it initiates suspension of a thread.
	It's used so async suspend can catch the case where a thread is in the middle of unregistering
	and need to cooperatively suspend itself.
	*/
	gboolean doing_handshake;

	/*
	This is set to TRUE when a thread start to dettach.
	This gives STW the oportunity to ignore a thread that started to
	unregister.
	*/
	gboolean thread_is_dying;

	/*
	This is set the argument of mono_gc_set_skip_thread.

	A thread that knowingly holds no managed state can call this
	function around blocking loops to reduce the GC burden by not
	been scanned.
	*/
	gboolean gc_disabled;
	void *stack_end;
	void *stack_start;
	void *stack_start_limit;
	char **tlab_next_addr;
	char **tlab_start_addr;
	char **tlab_temp_end_addr;
	char **tlab_real_end_addr;
	gpointer runtime_data;

	/* Only used on POSIX platforms */
	int signal;
	/* Ditto */
	/* FIXME: kill this, we only use signals on systems that have rt-posix, which doesn't have issues with duplicates. */
	unsigned int stop_count; /* to catch duplicate signals. */

	gpointer stopped_ip;	/* only valid if the thread is stopped */
	MonoDomain *stopped_domain; /* dsto */

	/*FIXME pretty please finish killing ARCH_NUM_REGS */
#ifdef USE_MONO_CTX
	MonoContext ctx;		/* ditto */
#else
	gpointer regs[ARCH_NUM_REGS];	    /* ditto */
#endif

#ifndef HAVE_KW_THREAD
	char *tlab_start;
	char *tlab_next;
	char *tlab_temp_end;
	char *tlab_real_end;
#endif
};

/*
 * The nursery section uses this struct.
 */
typedef struct _GCMemSection GCMemSection;
struct _GCMemSection {
	char *data;
	mword size;
	/* pointer where more data could be allocated if it fits */
	char *next_data;
	char *end_data;
	/*
	 * scan starts is an array of pointers to objects equally spaced in the allocation area
	 * They let use quickly find pinned objects from pinning pointers.
	 */
	char **scan_starts;
	/* in major collections indexes in the pin_queue for objects that pin this section */
	void **pin_queue_start;
	int pin_queue_num_entries;
	unsigned short num_scan_start;
};

/*
 * Recursion is not allowed for the thread lock.
 */
#define LOCK_DECLARE(name) mono_mutex_t name
/* if changing LOCK_INIT to something that isn't idempotent, look at
   its use in mono_gc_base_init in sgen-gc.c */
#define LOCK_INIT(name)	mono_mutex_init (&(name))
#define LOCK_GC do {						\
		mono_mutex_lock (&gc_mutex);			\
		MONO_GC_LOCKED ();				\
	} while (0)
#define TRYLOCK_GC (mono_mutex_trylock (&gc_mutex) == 0)
#define UNLOCK_GC do {						\
		mono_mutex_unlock (&gc_mutex);			\
		MONO_GC_UNLOCKED ();				\
	} while (0)

extern LOCK_DECLARE (sgen_interruption_mutex);

#define LOCK_INTERRUPTION mono_mutex_lock (&sgen_interruption_mutex)
#define UNLOCK_INTERRUPTION mono_mutex_unlock (&sgen_interruption_mutex)

/* FIXME: Use InterlockedAdd & InterlockedAdd64 to reduce the CAS cost. */
#define SGEN_CAS_PTR	InterlockedCompareExchangePointer
#define SGEN_ATOMIC_ADD(x,i)	do {					\
		int __old_x;						\
		do {							\
			__old_x = (x);					\
		} while (InterlockedCompareExchange (&(x), __old_x + (i), __old_x) != __old_x); \
	} while (0)
#define SGEN_ATOMIC_ADD_P(x,i) do { \
		size_t __old_x;                                            \
		do {                                                    \
			__old_x = (x);                                  \
		} while (InterlockedCompareExchangePointer ((void**)&(x), (void*)(__old_x + (i)), (void*)__old_x) != (void*)__old_x); \
	} while (0)


#ifndef HOST_WIN32
/* we intercept pthread_create calls to know which threads exist */
#define USE_PTHREAD_INTERCEPT 1
#endif

#ifdef HEAVY_STATISTICS
#define HEAVY_STAT(x)	x

extern long long stat_objects_alloced_degraded;
extern long long stat_bytes_alloced_degraded;
extern long long stat_copy_object_called_major;
extern long long stat_objects_copied_major;
#else
#define HEAVY_STAT(x)
#endif

#define SGEN_ASSERT(level, a, ...) do {	\
	if (G_UNLIKELY ((level) <= SGEN_MAX_ASSERT_LEVEL && !(a))) {	\
		g_error (__VA_ARGS__);	\
} } while (0)


#define SGEN_LOG(level, format, ...) do {      \
	if (G_UNLIKELY ((level) <= SGEN_MAX_DEBUG_LEVEL && (level) <= gc_debug_level)) {	\
		mono_gc_printf (gc_debug_file, format, ##__VA_ARGS__);	\
} } while (0)

#define SGEN_COND_LOG(level, cond, format, ...) do {	\
	if (G_UNLIKELY ((level) <= SGEN_MAX_DEBUG_LEVEL && (level) <= gc_debug_level)) {	\
		if (cond)	\
			mono_gc_printf (gc_debug_file, format, ##__VA_ARGS__);	\
} } while (0)

#define SGEN_LOG_DO(level, fun) do {	\
	if (G_UNLIKELY ((level) <= SGEN_MAX_DEBUG_LEVEL && (level) <= gc_debug_level)) {	\
		fun;	\
} } while (0)

extern int gc_debug_level;
extern FILE* gc_debug_file;

extern int current_collection_generation;

extern unsigned int sgen_global_stop_count;

extern gboolean bridge_processing_in_progress;

extern int num_ready_finalizers;

#define SGEN_ALLOC_ALIGN		8
#define SGEN_ALLOC_ALIGN_BITS	3

#define SGEN_ALIGN_UP(s)		(((s)+(SGEN_ALLOC_ALIGN-1)) & ~(SGEN_ALLOC_ALIGN-1))

/*
 * The link pointer is hidden by negating each bit.  We use the lowest
 * bit of the link (before negation) to store whether it needs
 * resurrection tracking.
 */
#define HIDE_POINTER(p,t)	((gpointer)(~((gulong)(p)|((t)?1:0))))
#define REVEAL_POINTER(p)	((gpointer)((~(gulong)(p))&~3L))

#ifdef SGEN_ALIGN_NURSERY
#define SGEN_PTR_IN_NURSERY(p,bits,start,end)	(((mword)(p) & ~((1 << (bits)) - 1)) == (mword)(start))
#else
#define SGEN_PTR_IN_NURSERY(p,bits,start,end)	((char*)(p) >= (start) && (char*)(p) < (end))
#endif

#ifdef USER_CONFIG

/* good sizes are 512KB-1MB: larger ones increase a lot memzeroing time */
#define DEFAULT_NURSERY_SIZE (sgen_nursery_size)
extern int sgen_nursery_size MONO_INTERNAL;
#ifdef SGEN_ALIGN_NURSERY
/* The number of trailing 0 bits in DEFAULT_NURSERY_SIZE */
#define DEFAULT_NURSERY_BITS (sgen_nursery_bits)
extern int sgen_nursery_bits MONO_INTERNAL;
#endif

#else

#define DEFAULT_NURSERY_SIZE (4*1024*1024)
#ifdef SGEN_ALIGN_NURSERY
#define DEFAULT_NURSERY_BITS 22
#endif

#endif

#ifndef SGEN_ALIGN_NURSERY
#define DEFAULT_NURSERY_BITS -1
#endif

extern char *sgen_nursery_start MONO_INTERNAL;
extern char *sgen_nursery_end MONO_INTERNAL;

static inline gboolean
sgen_ptr_in_nursery (void *p)
{
	return SGEN_PTR_IN_NURSERY ((p), DEFAULT_NURSERY_BITS, sgen_nursery_start, sgen_nursery_end);
}

static inline char*
sgen_get_nursery_start (void)
{
	return sgen_nursery_start;
}

static inline char*
sgen_get_nursery_end (void)
{
	return sgen_nursery_end;
}

/* Structure that corresponds to a MonoVTable: desc is a mword so requires
 * no cast from a pointer to an integer
 */
typedef struct {
	MonoClass *klass;
	mword desc;
} GCVTable;

/* these bits are set in the object vtable: we could merge them since an object can be
 * either pinned or forwarded but not both.
 * We store them in the vtable slot because the bits are used in the sync block for
 * other purposes: if we merge them and alloc the sync blocks aligned to 8 bytes, we can change
 * this and use bit 3 in the syncblock (with the lower two bits both set for forwarded, that
 * would be an invalid combination for the monitor and hash code).
 * The values are already shifted.
 * The forwarding address is stored in the sync block.
 */
#define SGEN_FORWARDED_BIT 1
#define SGEN_PINNED_BIT 2
#define SGEN_VTABLE_BITS_MASK 0x3

/* returns NULL if not forwarded, or the forwarded address */
#define SGEN_OBJECT_IS_FORWARDED(obj) (((mword*)(obj))[0] & SGEN_FORWARDED_BIT ? (void*)(((mword*)(obj))[0] & ~SGEN_VTABLE_BITS_MASK) : NULL)
#define SGEN_OBJECT_IS_PINNED(obj) (((mword*)(obj))[0] & SGEN_PINNED_BIT)

/* set the forwarded address fw_addr for object obj */
#define SGEN_FORWARD_OBJECT(obj,fw_addr) do {				\
		((mword*)(obj))[0] = (mword)(fw_addr) | SGEN_FORWARDED_BIT; \
	} while (0)
#define SGEN_PIN_OBJECT(obj) do {	\
		((mword*)(obj))[0] |= SGEN_PINNED_BIT;	\
	} while (0)
#define SGEN_UNPIN_OBJECT(obj) do {	\
		((mword*)(obj))[0] &= ~SGEN_PINNED_BIT;	\
	} while (0)

/*
 * Since we set bits in the vtable, use the macro to load it from the pointer to
 * an object that is potentially pinned.
 */
#define SGEN_LOAD_VTABLE(addr) ((*(mword*)(addr)) & ~SGEN_VTABLE_BITS_MASK)

#if defined(SGEN_GRAY_OBJECT_ENQUEUE) || SGEN_MAX_DEBUG_LEVEL >= 9
#define GRAY_OBJECT_ENQUEUE sgen_gray_object_enqueue
#define GRAY_OBJECT_DEQUEUE(queue,o) ((o) = sgen_gray_object_dequeue ((queue)))
#else
#define GRAY_OBJECT_ENQUEUE(queue,o) do {				\
		if (G_UNLIKELY (!(queue)->first || (queue)->first->end == SGEN_GRAY_QUEUE_SECTION_SIZE)) \
			sgen_gray_object_enqueue ((queue), (o));	\
		else							\
			(queue)->first->objects [(queue)->first->end++] = (o); \
		PREFETCH ((o));						\
	} while (0)
#define GRAY_OBJECT_DEQUEUE(queue,o) do {				\
		if (!(queue)->first)					\
			(o) = NULL;					\
		else if (G_UNLIKELY ((queue)->first->end == 1))		\
			(o) = sgen_gray_object_dequeue ((queue));		\
		else							\
			(o) = (queue)->first->objects [--(queue)->first->end]; \
	} while (0)
#endif

/*
List of what each bit on of the vtable gc bits means. 
*/
enum {
	SGEN_GC_BIT_BRIDGE_OBJECT = 1,
};

/* the runtime can register areas of memory as roots: we keep two lists of roots,
 * a pinned root set for conservatively scanned roots and a normal one for
 * precisely scanned roots (currently implemented as a single list).
 */
typedef struct _RootRecord RootRecord;
struct _RootRecord {
	char *end_root;
	mword root_desc;
};

enum {
	ROOT_TYPE_NORMAL = 0, /* "normal" roots */
	ROOT_TYPE_PINNED = 1, /* roots without a GC descriptor */
	ROOT_TYPE_WBARRIER = 2, /* roots with a write barrier */
	ROOT_TYPE_NUM
};

extern SgenHashTable roots_hash [ROOT_TYPE_NUM];

typedef void (*IterateObjectCallbackFunc) (char*, size_t, void*);

int sgen_thread_handshake (BOOL suspend) MONO_INTERNAL;
gboolean sgen_suspend_thread (SgenThreadInfo *info) MONO_INTERNAL;
gboolean sgen_resume_thread (SgenThreadInfo *info) MONO_INTERNAL;
void sgen_wait_for_suspend_ack (int count) MONO_INTERNAL;
gboolean sgen_park_current_thread_if_doing_handshake (SgenThreadInfo *p) MONO_INTERNAL;
void sgen_os_init (void) MONO_INTERNAL;

gboolean sgen_is_worker_thread (MonoNativeThreadId thread) MONO_INTERNAL;

void sgen_update_heap_boundaries (mword low, mword high) MONO_INTERNAL;

void sgen_scan_area_with_callback (char *start, char *end, IterateObjectCallbackFunc callback, void *data, gboolean allow_flags) MONO_INTERNAL;
void sgen_check_section_scan_starts (GCMemSection *section) MONO_INTERNAL;

/* Keep in sync with description_for_type() in sgen-internal.c! */
enum {
	INTERNAL_MEM_PIN_QUEUE,
	INTERNAL_MEM_FRAGMENT,
	INTERNAL_MEM_SECTION,
	INTERNAL_MEM_SCAN_STARTS,
	INTERNAL_MEM_FIN_TABLE,
	INTERNAL_MEM_FINALIZE_ENTRY,
	INTERNAL_MEM_FINALIZE_READY_ENTRY,
	INTERNAL_MEM_DISLINK_TABLE,
	INTERNAL_MEM_DISLINK,
	INTERNAL_MEM_ROOTS_TABLE,
	INTERNAL_MEM_ROOT_RECORD,
	INTERNAL_MEM_STATISTICS,
	INTERNAL_MEM_STAT_PINNED_CLASS,
	INTERNAL_MEM_STAT_REMSET_CLASS,
	INTERNAL_MEM_GRAY_QUEUE,
	INTERNAL_MEM_MS_TABLES,
	INTERNAL_MEM_MS_BLOCK_INFO,
	INTERNAL_MEM_MS_BLOCK_INFO_SORT,
	INTERNAL_MEM_EPHEMERON_LINK,
	INTERNAL_MEM_WORKER_DATA,
	INTERNAL_MEM_WORKER_JOB_DATA,
	INTERNAL_MEM_BRIDGE_DATA,
	INTERNAL_MEM_BRIDGE_HASH_TABLE,
	INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY,
	INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE,
	INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE_ENTRY,
	INTERNAL_MEM_JOB_QUEUE_ENTRY,
	INTERNAL_MEM_TOGGLEREF_DATA,
	INTERNAL_MEM_CARDTABLE_MOD_UNION,
	INTERNAL_MEM_MAX
};

enum {
	GENERATION_NURSERY,
	GENERATION_OLD,
	GENERATION_MAX
};

#ifdef SGEN_BINARY_PROTOCOL
#define BINARY_PROTOCOL_ARG(x)	,x
#else
#define BINARY_PROTOCOL_ARG(x)
#endif

void sgen_init_internal_allocator (void) MONO_INTERNAL;

typedef struct _ObjectList ObjectList;
struct _ObjectList {
	MonoObject *obj;
	ObjectList *next;
};

typedef void (*CopyOrMarkObjectFunc) (void**, SgenGrayQueue*);
typedef void (*ScanObjectFunc) (char*, SgenGrayQueue*);
typedef void (*ScanVTypeFunc) (char*, mword desc, SgenGrayQueue* BINARY_PROTOCOL_ARG (size_t size));

typedef struct
{
	ScanObjectFunc scan_func;
	CopyOrMarkObjectFunc copy_func;
	SgenGrayQueue *queue;
} ScanCopyContext;

void sgen_report_internal_mem_usage (void) MONO_INTERNAL;
void sgen_dump_internal_mem_usage (FILE *heap_dump_file) MONO_INTERNAL;
void sgen_dump_section (GCMemSection *section, const char *type) MONO_INTERNAL;
void sgen_dump_occupied (char *start, char *end, char *section_start) MONO_INTERNAL;

void sgen_register_moved_object (void *obj, void *destination) MONO_INTERNAL;

void sgen_register_fixed_internal_mem_type (int type, size_t size) MONO_INTERNAL;

void* sgen_alloc_internal (int type) MONO_INTERNAL;
void sgen_free_internal (void *addr, int type) MONO_INTERNAL;

void* sgen_alloc_internal_dynamic (size_t size, int type, gboolean assert_on_failure) MONO_INTERNAL;
void sgen_free_internal_dynamic (void *addr, size_t size, int type) MONO_INTERNAL;

void** sgen_find_optimized_pin_queue_area (void *start, void *end, int *num) MONO_INTERNAL;
void sgen_find_section_pin_queue_start_end (GCMemSection *section) MONO_INTERNAL;
void sgen_pin_objects_in_section (GCMemSection *section, ScanCopyContext ctx) MONO_INTERNAL;

void sgen_pin_stats_register_object (char *obj, size_t size);
void sgen_pin_stats_register_global_remset (char *obj);
void sgen_pin_stats_print_class_stats (void);

void sgen_sort_addresses (void **array, int size) MONO_INTERNAL;
void sgen_add_to_global_remset (gpointer ptr, gpointer obj) MONO_INTERNAL;

int sgen_get_current_collection_generation (void) MONO_INTERNAL;
gboolean sgen_collection_is_parallel (void) MONO_INTERNAL;
gboolean sgen_collection_is_concurrent (void) MONO_INTERNAL;
gboolean sgen_concurrent_collection_in_progress (void) MONO_INTERNAL;

typedef struct {
	CopyOrMarkObjectFunc copy_or_mark_object;
	ScanObjectFunc scan_object;
	ScanVTypeFunc scan_vtype;
	/*FIXME add allocation function? */
} SgenObjectOperations;

SgenObjectOperations *sgen_get_current_object_ops (void) MONO_INTERNAL;

typedef struct _SgenFragment SgenFragment;

struct _SgenFragment {
	SgenFragment *next;
	char *fragment_start;
	char *fragment_next; /* the current soft limit for allocation */
	char *fragment_end;
	SgenFragment *next_in_order; /* We use a different entry for all active fragments so we can avoid SMR. */
};

typedef struct {
	SgenFragment *alloc_head; /* List head to be used when allocating memory. Walk with fragment_next. */
	SgenFragment *region_head; /* List head of the region used by this allocator. Walk with next_in_order. */
} SgenFragmentAllocator;

void sgen_fragment_allocator_add (SgenFragmentAllocator *allocator, char *start, char *end) MONO_INTERNAL;
void sgen_fragment_allocator_release (SgenFragmentAllocator *allocator) MONO_INTERNAL;
void* sgen_fragment_allocator_serial_alloc (SgenFragmentAllocator *allocator, size_t size) MONO_INTERNAL;
void* sgen_fragment_allocator_par_alloc (SgenFragmentAllocator *allocator, size_t size) MONO_INTERNAL;
void* sgen_fragment_allocator_serial_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size) MONO_INTERNAL;
void* sgen_fragment_allocator_par_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size) MONO_INTERNAL;
SgenFragment* sgen_fragment_allocator_alloc (void) MONO_INTERNAL;
void sgen_clear_allocator_fragments (SgenFragmentAllocator *allocator) MONO_INTERNAL;
void sgen_clear_range (char *start, char *end) MONO_INTERNAL;


/*
This is a space/speed compromise as we need to make sure the from/to space check is both O(1)
and only hit cache hot memory. On a 4Mb nursery it requires 1024 bytes, or 3% of your average
L1 cache. On small configs with a 512kb nursery, this goes to 0.4%.

Experimental results on how much space we waste with a 4Mb nursery:

Note that the wastage applies to the half nursery, or 2Mb:

Test 1 (compiling corlib):
9: avg: 3.1k
8: avg: 1.6k

*/
#define SGEN_TO_SPACE_GRANULE_BITS 9
#define SGEN_TO_SPACE_GRANULE_IN_BYTES (1 << SGEN_TO_SPACE_GRANULE_BITS)

extern char *sgen_space_bitmap MONO_INTERNAL;
extern int sgen_space_bitmap_size MONO_INTERNAL;

static inline gboolean
sgen_nursery_is_to_space (char *object)
{
	int idx = (object - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	int byte = idx / 8;
	int bit = idx & 0x7;

	SGEN_ASSERT (4, sgen_ptr_in_nursery (object), "object %p is not in nursery [%p - %p]", object, sgen_get_nursery_start (), sgen_get_nursery_end ());
	SGEN_ASSERT (4, byte < sgen_space_bitmap_size, "byte index %d out of range", byte, sgen_space_bitmap_size);

	return (sgen_space_bitmap [byte] & (1 << bit)) != 0;
}

static inline gboolean
sgen_nursery_is_from_space (char *object)
{
	return !sgen_nursery_is_to_space (object);
}

static inline gboolean
sgen_nursery_is_object_alive (char *obj)
{
	/* FIXME put this asserts under a non default level */
	g_assert (sgen_ptr_in_nursery (obj));

	if (sgen_nursery_is_to_space (obj))
		return TRUE;

	if (SGEN_OBJECT_IS_PINNED (obj) || SGEN_OBJECT_IS_FORWARDED (obj))
		return TRUE;

	return FALSE;
}

typedef struct {
	gboolean is_split;

	char* (*alloc_for_promotion) (MonoVTable *vtable, char *obj, size_t objsize, gboolean has_references);
	char* (*par_alloc_for_promotion) (MonoVTable *vtable, char *obj, size_t objsize, gboolean has_references);

	SgenObjectOperations serial_ops;
	SgenObjectOperations parallel_ops;

	void (*prepare_to_space) (char *to_space_bitmap, int space_bitmap_size);
	void (*clear_fragments) (void);
	SgenFragment* (*build_fragments_get_exclude_head) (void);
	void (*build_fragments_release_exclude_head) (void);
	void (*build_fragments_finish) (SgenFragmentAllocator *allocator);
	void (*init_nursery) (SgenFragmentAllocator *allocator, char *start, char *end);

	gboolean (*handle_gc_param) (const char *opt); /* Optional */
	void (*print_gc_param_usage) (void); /* Optional */
} SgenMinorCollector;

extern SgenMinorCollector sgen_minor_collector;

void sgen_simple_nursery_init (SgenMinorCollector *collector) MONO_INTERNAL;
void sgen_split_nursery_init (SgenMinorCollector *collector) MONO_INTERNAL;

typedef void (*sgen_cardtable_block_callback) (mword start, mword size);
void sgen_major_collector_iterate_live_block_ranges (sgen_cardtable_block_callback callback) MONO_INTERNAL;

typedef struct _SgenMajorCollector SgenMajorCollector;
struct _SgenMajorCollector {
	size_t section_size;
	gboolean is_parallel;
	gboolean is_concurrent;
	gboolean supports_cardtable;
	gboolean sweeps_lazily;

	/*
	 * This is set to TRUE if the sweep for the last major
	 * collection has been completed.
	 */
	gboolean *have_swept;
	/*
	 * This is set to TRUE by the sweep if the next major
	 * collection should be synchronous (for evacuation).  For
	 * non-concurrent collectors, this should be NULL.
	 */
	gboolean *want_synchronous_collection;

	void* (*alloc_heap) (mword nursery_size, mword nursery_align, int nursery_bits);
	gboolean (*is_object_live) (char *obj);
	void* (*alloc_small_pinned_obj) (MonoVTable *vtable, size_t size, gboolean has_references);
	void* (*alloc_degraded) (MonoVTable *vtable, size_t size);

	SgenObjectOperations major_ops;
	SgenObjectOperations major_concurrent_ops;

	void* (*alloc_object) (MonoVTable *vtable, int size, gboolean has_references);
	void* (*par_alloc_object) (MonoVTable *vtable, int size, gboolean has_references);
	void (*free_pinned_object) (char *obj, size_t size);
	void (*iterate_objects) (gboolean non_pinned, gboolean pinned, IterateObjectCallbackFunc callback, void *data);
	void (*free_non_pinned_object) (char *obj, size_t size);
	void (*find_pin_queue_start_ends) (SgenGrayQueue *queue);
	void (*pin_objects) (SgenGrayQueue *queue);
	void (*pin_major_object) (char *obj, SgenGrayQueue *queue);
	void (*scan_card_table) (gboolean mod_union, SgenGrayQueue *queue);
	void (*iterate_live_block_ranges) (sgen_cardtable_block_callback callback);
	void (*update_cardtable_mod_union) (void);
	void (*init_to_space) (void);
	void (*sweep) (void);
	void (*check_scan_starts) (void);
	void (*dump_heap) (FILE *heap_dump_file);
	gint64 (*get_used_size) (void);
	void (*start_nursery_collection) (void);
	void (*finish_nursery_collection) (void);
	void (*start_major_collection) (void);
	void (*finish_major_collection) (void);
	void (*have_computed_minor_collection_allowance) (void);
	gboolean (*ptr_is_in_non_pinned_space) (char *ptr, char **start);
	gboolean (*obj_is_from_pinned_alloc) (char *obj);
	void (*report_pinned_memory_usage) (void);
	int (*get_num_major_sections) (void);
	gboolean (*handle_gc_param) (const char *opt);
	void (*print_gc_param_usage) (void);
	gboolean (*is_worker_thread) (MonoNativeThreadId thread);
	void (*post_param_init) (SgenMajorCollector *collector);
	void* (*alloc_worker_data) (void);
	void (*init_worker_thread) (void *data);
	void (*reset_worker_data) (void *data);
	gboolean (*is_valid_object) (char *object);
	gboolean (*describe_pointer) (char *pointer);
	guint8* (*get_cardtable_mod_union_for_object) (char *object);
	long long (*get_and_reset_num_major_objects_marked) (void);
};

extern SgenMajorCollector major_collector;

void sgen_marksweep_init (SgenMajorCollector *collector) MONO_INTERNAL;
void sgen_marksweep_fixed_init (SgenMajorCollector *collector) MONO_INTERNAL;
void sgen_marksweep_par_init (SgenMajorCollector *collector) MONO_INTERNAL;
void sgen_marksweep_fixed_par_init (SgenMajorCollector *collector) MONO_INTERNAL;
void sgen_marksweep_conc_init (SgenMajorCollector *collector) MONO_INTERNAL;
SgenMajorCollector* sgen_get_major_collector (void) MONO_INTERNAL;


typedef struct {
	void (*wbarrier_set_field) (MonoObject *obj, gpointer field_ptr, MonoObject* value);
	void (*wbarrier_set_arrayref) (MonoArray *arr, gpointer slot_ptr, MonoObject* value);
	void (*wbarrier_arrayref_copy) (gpointer dest_ptr, gpointer src_ptr, int count);
	void (*wbarrier_value_copy) (gpointer dest, gpointer src, int count, MonoClass *klass);
	void (*wbarrier_object_copy) (MonoObject* obj, MonoObject *src);
	void (*wbarrier_generic_nostore) (gpointer ptr);
	void (*record_pointer) (gpointer ptr);

	void (*finish_scan_remsets) (void *start_nursery, void *end_nursery, SgenGrayQueue *queue);

	void (*prepare_for_major_collection) (void);

	void (*finish_minor_collection) (void);
	gboolean (*find_address) (char *addr);
	gboolean (*find_address_with_cards) (char *cards_start, guint8 *cards, char *addr);
} SgenRemeberedSet;

SgenRemeberedSet *sgen_get_remset (void) MONO_INTERNAL;

static guint /*__attribute__((noinline)) not sure if this hint is a good idea*/
slow_object_get_size (MonoVTable *vtable, MonoObject* o)
{
	MonoClass *klass = vtable->klass;

	/*
	 * We depend on mono_string_length_fast and
	 * mono_array_length_fast not using the object's vtable.
	 */
	if (klass == mono_defaults.string_class) {
		return sizeof (MonoString) + 2 * mono_string_length_fast ((MonoString*) o) + 2;
	} else if (klass->rank) {
		MonoArray *array = (MonoArray*)o;
		size_t size = sizeof (MonoArray) + klass->sizes.element_size * mono_array_length_fast (array);
		if (G_UNLIKELY (array->bounds)) {
			size += sizeof (mono_array_size_t) - 1;
			size &= ~(sizeof (mono_array_size_t) - 1);
			size += sizeof (MonoArrayBounds) * klass->rank;
		}
		return size;
	} else {
		/* from a created object: the class must be inited already */
		return klass->instance_size;
	}
}

/*
 * This function can be called on an object whose first word, the
 * vtable field, is not intact.  This is necessary for the parallel
 * collector.
 */
static inline guint
sgen_par_object_get_size (MonoVTable *vtable, MonoObject* o)
{
	mword descr = (mword)vtable->gc_descr;
	mword type = descr & 0x7;

	if (type == DESC_TYPE_RUN_LENGTH || type == DESC_TYPE_SMALL_BITMAP) {
		mword size = descr & 0xfff8;
		if (size == 0) /* This is used to encode a string */
			return sizeof (MonoString) + 2 * mono_string_length_fast ((MonoString*) o) + 2;
		return size;
	} else if (type == DESC_TYPE_VECTOR) {
		int element_size = ((descr) >> VECTOR_ELSIZE_SHIFT) & MAX_ELEMENT_SIZE;
		MonoArray *array = (MonoArray*)o;
		size_t size = sizeof (MonoArray) + element_size * mono_array_length_fast (array);

		if (descr & VECTOR_KIND_ARRAY) {
			size += sizeof (mono_array_size_t) - 1;
			size &= ~(sizeof (mono_array_size_t) - 1);
			size += sizeof (MonoArrayBounds) * vtable->klass->rank;
		}
		return size;
	}

	return slow_object_get_size (vtable, o);
}

static inline guint
sgen_safe_object_get_size (MonoObject *obj)
{
       char *forwarded;

       if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj)))
               obj = (MonoObject*)forwarded;

       return sgen_par_object_get_size ((MonoVTable*)SGEN_LOAD_VTABLE (obj), obj);
}

const char* sgen_safe_name (void* obj) MONO_INTERNAL;

gboolean sgen_object_is_live (void *obj) MONO_INTERNAL;

void  sgen_init_fin_weak_hash (void) MONO_INTERNAL;

gboolean sgen_need_bridge_processing (void) MONO_INTERNAL;
void sgen_bridge_reset_data (void) MONO_INTERNAL;
void sgen_bridge_processing_stw_step (void) MONO_INTERNAL;
void sgen_bridge_processing_finish (int generation) MONO_INTERNAL;
void sgen_register_test_bridge_callbacks (const char *bridge_class_name) MONO_INTERNAL;
gboolean sgen_is_bridge_object (MonoObject *obj) MONO_INTERNAL;
gboolean sgen_is_bridge_class (MonoClass *class) MONO_INTERNAL;
void sgen_mark_bridge_object (MonoObject *obj) MONO_INTERNAL;
void sgen_bridge_register_finalized_object (MonoObject *object) MONO_INTERNAL;

void sgen_scan_togglerefs (char *start, char *end, ScanCopyContext ctx) MONO_INTERNAL;
void sgen_process_togglerefs (void) MONO_INTERNAL;

typedef mono_bool (*WeakLinkAlivePredicateFunc) (MonoObject*, void*);

void sgen_null_links_with_predicate (int generation, WeakLinkAlivePredicateFunc predicate, void *data) MONO_INTERNAL;

gboolean sgen_gc_is_object_ready_for_finalization (void *object) MONO_INTERNAL;
void sgen_gc_lock (void) MONO_INTERNAL;
void sgen_gc_unlock (void) MONO_INTERNAL;
void sgen_gc_event_moves (void) MONO_INTERNAL;

void sgen_queue_finalization_entry (MonoObject *obj) MONO_INTERNAL;
const char* sgen_generation_name (int generation) MONO_INTERNAL;

void sgen_collect_bridge_objects (int generation, ScanCopyContext ctx) MONO_INTERNAL;
void sgen_finalize_in_range (int generation, ScanCopyContext ctx) MONO_INTERNAL;
void sgen_null_link_in_range (int generation, gboolean before_finalization, ScanCopyContext ctx) MONO_INTERNAL;
void sgen_null_links_for_domain (MonoDomain *domain, int generation) MONO_INTERNAL;
void sgen_remove_finalizers_for_domain (MonoDomain *domain, int generation) MONO_INTERNAL;
void sgen_process_fin_stage_entries (void) MONO_INTERNAL;
void sgen_process_dislink_stage_entries (void) MONO_INTERNAL;
void sgen_register_disappearing_link (MonoObject *obj, void **link, gboolean track, gboolean in_gc) MONO_INTERNAL;

gboolean sgen_drain_gray_stack (int max_objs, ScanCopyContext ctx) MONO_INTERNAL;

enum {
	SPACE_NURSERY,
	SPACE_MAJOR,
	SPACE_LOS
};

void sgen_pin_object (void *object, SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_parallel_pin_or_update (void **ptr, void *obj, MonoVTable *vt, SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_set_pinned_from_failed_allocation (mword objsize) MONO_INTERNAL;

void sgen_ensure_free_space (size_t size) MONO_INTERNAL;
void sgen_perform_collection (size_t requested_size, int generation_to_collect, const char *reason, gboolean wait_to_finish) MONO_INTERNAL;
gboolean sgen_has_critical_method (void) MONO_INTERNAL;
gboolean sgen_is_critical_method (MonoMethod *method) MONO_INTERNAL;

/* STW */

typedef struct {
	int generation;
	const char *reason;
	gboolean is_overflow;
	SGEN_TV_DECLARE (total_time);
	SGEN_TV_DECLARE (stw_time);
	SGEN_TV_DECLARE (bridge_time);
} GGTimingInfo;

int sgen_stop_world (int generation) MONO_INTERNAL;
int sgen_restart_world (int generation, GGTimingInfo *timing) MONO_INTERNAL;

/* LOS */

typedef struct _LOSObject LOSObject;
struct _LOSObject {
	LOSObject *next;
	mword size; /* this is the object size, lowest bit used for pin/mark */
	guint8 *cardtable_mod_union; /* only used by the concurrent collector */
#if SIZEOF_VOID_P < 8
	mword dummy;		/* to align object to sizeof (double) */
#endif
	char data [MONO_ZERO_LEN_ARRAY];
};

#define ARRAY_OBJ_INDEX(ptr,array,elem_size) (((char*)(ptr) - ((char*)(array) + G_STRUCT_OFFSET (MonoArray, vector))) / (elem_size))

extern LOSObject *los_object_list;
extern mword los_memory_usage;

void sgen_los_free_object (LOSObject *obj) MONO_INTERNAL;
void* sgen_los_alloc_large_inner (MonoVTable *vtable, size_t size) MONO_INTERNAL;
void sgen_los_sweep (void) MONO_INTERNAL;
gboolean sgen_ptr_is_in_los (char *ptr, char **start) MONO_INTERNAL;
void sgen_los_iterate_objects (IterateObjectCallbackFunc cb, void *user_data) MONO_INTERNAL;
void sgen_los_iterate_live_block_ranges (sgen_cardtable_block_callback callback) MONO_INTERNAL;
void sgen_los_scan_card_table (gboolean mod_union, SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_los_update_cardtable_mod_union (void) MONO_INTERNAL;
void sgen_major_collector_scan_card_table (SgenGrayQueue *queue) MONO_INTERNAL;
gboolean sgen_los_is_valid_object (char *object) MONO_INTERNAL;
gboolean mono_sgen_los_describe_pointer (char *ptr) MONO_INTERNAL;
LOSObject* sgen_los_header_for_object (char *data) MONO_INTERNAL;
mword sgen_los_object_size (LOSObject *obj) MONO_INTERNAL;
void sgen_los_pin_object (char *obj) MONO_INTERNAL;
void sgen_los_unpin_object (char *obj) MONO_INTERNAL;
gboolean sgen_los_object_is_pinned (char *obj) MONO_INTERNAL;


/* nursery allocator */

void sgen_clear_nursery_fragments (void) MONO_INTERNAL;
void sgen_nursery_allocator_prepare_for_pinning (void) MONO_INTERNAL;
void sgen_nursery_allocator_set_nursery_bounds (char *nursery_start, char *nursery_end) MONO_INTERNAL;
mword sgen_build_nursery_fragments (GCMemSection *nursery_section, void **start, int num_entries, SgenGrayQueue *unpin_queue) MONO_INTERNAL;
void sgen_init_nursery_allocator (void) MONO_INTERNAL;
void sgen_nursery_allocator_init_heavy_stats (void) MONO_INTERNAL;
void sgen_alloc_init_heavy_stats (void) MONO_INTERNAL;
char* sgen_nursery_alloc_get_upper_alloc_bound (void) MONO_INTERNAL;
void* sgen_nursery_alloc (size_t size) MONO_INTERNAL;
void* sgen_nursery_alloc_range (size_t size, size_t min_size, size_t *out_alloc_size) MONO_INTERNAL;
MonoVTable* sgen_get_array_fill_vtable (void) MONO_INTERNAL;
gboolean sgen_can_alloc_size (size_t size) MONO_INTERNAL;
void sgen_nursery_retire_region (void *address, ptrdiff_t size) MONO_INTERNAL;

void sgen_nursery_alloc_prepare_for_minor (void) MONO_INTERNAL;
void sgen_nursery_alloc_prepare_for_major (void) MONO_INTERNAL;

char* sgen_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references) MONO_INTERNAL;
char* sgen_par_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references) MONO_INTERNAL;

/* TLS Data */

extern MonoNativeTlsKey thread_info_key;

#ifdef HAVE_KW_THREAD
extern __thread SgenThreadInfo *sgen_thread_info;
extern __thread char *stack_end;
#endif

#ifdef HAVE_KW_THREAD
#define TLAB_ACCESS_INIT
#define IN_CRITICAL_REGION sgen_thread_info->in_critical_region
#else
#define TLAB_ACCESS_INIT	SgenThreadInfo *__thread_info__ = mono_native_tls_get_value (thread_info_key)
#define IN_CRITICAL_REGION (__thread_info__->in_critical_region)
#endif

#ifndef DISABLE_CRITICAL_REGION

#ifdef HAVE_KW_THREAD
#define IN_CRITICAL_REGION sgen_thread_info->in_critical_region
#else
#define IN_CRITICAL_REGION (__thread_info__->in_critical_region)
#endif

/* Enter must be visible before anything is done in the critical region. */
#define ENTER_CRITICAL_REGION do { mono_atomic_store_acquire (&IN_CRITICAL_REGION, 1); } while (0)

/* Exit must make sure all critical regions stores are visible before it signal the end of the region. 
 * We don't need to emit a full barrier since we
 */
#define EXIT_CRITICAL_REGION  do { mono_atomic_store_release (&IN_CRITICAL_REGION, 0); } while (0)

#endif

#ifdef HAVE_KW_THREAD
#define EMIT_TLS_ACCESS(mb,dummy,offset)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), (offset));		\
	} while (0)
#else

/* 
 * CEE_MONO_TLS requires the tls offset, not the key, so the code below only works on darwin,
 * where the two are the same.
 */
#if defined(__APPLE__) || defined (HOST_WIN32)
#define EMIT_TLS_ACCESS(mb,member,dummy)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), thread_info_key);	\
	mono_mb_emit_icon ((mb), G_STRUCT_OFFSET (SgenThreadInfo, member));	\
	mono_mb_emit_byte ((mb), CEE_ADD);		\
	mono_mb_emit_byte ((mb), CEE_LDIND_I);		\
	} while (0)
#else
#define EMIT_TLS_ACCESS(mb,member,dummy)	do { g_error ("sgen is not supported when using --with-tls=pthread.\n"); } while (0)
#endif

#endif

/* Other globals */

extern GCMemSection *nursery_section;
extern int stat_major_gcs;
extern guint32 collect_before_allocs;
extern guint32 verify_before_allocs;
extern gboolean has_per_allocation_action;
extern int degraded_mode;
extern int default_nursery_size;
extern guint32 tlab_size;
extern NurseryClearPolicy nursery_clear_policy;

extern LOCK_DECLARE (gc_mutex);

extern int do_pin_stats;

/* Nursery helpers. */

static inline void
sgen_set_nursery_scan_start (char *p)
{
	int idx = (p - (char*)nursery_section->data) / SGEN_SCAN_START_SIZE;
	char *old = nursery_section->scan_starts [idx];
	if (!old || old > p)
		nursery_section->scan_starts [idx] = p;
}


/* Object Allocation */

typedef enum {
	ATYPE_NORMAL,
	ATYPE_VECTOR,
	ATYPE_SMALL,
	ATYPE_STRING,
	ATYPE_NUM
} SgenAllocatorType;

void sgen_init_tlab_info (SgenThreadInfo* info);
void sgen_clear_tlabs (void);
void sgen_set_use_managed_allocator (gboolean flag);
gboolean sgen_is_managed_allocator (MonoMethod *method);
gboolean sgen_has_managed_allocator (void);

/* Debug support */

void sgen_check_consistency (void);
void sgen_check_mod_union_consistency (void);
void sgen_check_major_refs (void);
void sgen_check_whole_heap (gboolean allow_missing_pinning);
void sgen_check_whole_heap_stw (void) MONO_INTERNAL;
void sgen_check_objref (char *obj);
void sgen_check_major_heap_marked (void) MONO_INTERNAL;
void sgen_check_nursery_objects_pinned (gboolean pinned) MONO_INTERNAL;

/* Write barrier support */

/*
 * This causes the compile to extend the liveness of 'v' till the call to dummy_use
 */
static inline void
sgen_dummy_use (gpointer v) {
#if defined(__GNUC__)
	__asm__ volatile ("" : "=r"(v) : "r"(v));
#elif defined(_MSC_VER)
	__asm {
		mov eax, v;
		and eax, eax;
	};
#else
#error "Implement sgen_dummy_use for your compiler"
#endif
}

/* Environment variable parsing */

#define MONO_GC_PARAMS_NAME	"MONO_GC_PARAMS"
#define MONO_GC_DEBUG_NAME	"MONO_GC_DEBUG"

gboolean sgen_parse_environment_string_extract_number (const char *str, glong *out) MONO_INTERNAL;
void sgen_env_var_error (const char *env_var, const char *fallback, const char *description_format, ...) MONO_INTERNAL;

#endif /* HAVE_SGEN_GC */

#endif /* __MONO_SGENGC_H__ */
