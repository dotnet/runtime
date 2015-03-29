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
#include <mono/metadata/sgen-bridge.h>
#include <mono/metadata/sgen-protocol.h>

/* The method used to clear the nursery */
/* Clearing at nursery collections is the safest, but has bad interactions with caches.
 * Clearing at TLAB creation is much faster, but more complex and it might expose hard
 * to find bugs.
 */
typedef enum {
	CLEAR_AT_GC,
	CLEAR_AT_TLAB_CREATION,
	CLEAR_AT_TLAB_CREATION_DEBUG
} NurseryClearPolicy;

NurseryClearPolicy sgen_get_nursery_clear_policy (void);

#define SGEN_TV_DECLARE(name) gint64 name
#define SGEN_TV_GETTIME(tv) tv = mono_100ns_ticks ()
#define SGEN_TV_ELAPSED(start,end) (int)((end-start))

#if !defined(__MACH__) && !MONO_MACH_ARCH_SUPPORTED && defined(HAVE_PTHREAD_KILL)
#define SGEN_POSIX_STW 1
#endif

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
	gboolean skip, suspend_done;
	volatile int in_critical_region;

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

#ifdef SGEN_POSIX_STW
	/* This is -1 until the first suspend. */
	int signal;
	/* FIXME: kill this, we only use signals on systems that have rt-posix, which doesn't have issues with duplicates. */
	unsigned int stop_count; /* to catch duplicate signals. */
#endif

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
	size_t pin_queue_first_entry;
	size_t pin_queue_last_entry;
	size_t num_scan_start;
};

/*
 * Recursion is not allowed for the thread lock.
 */
#define LOCK_DECLARE(name) mono_mutex_t name
/* if changing LOCK_INIT to something that isn't idempotent, look at
   its use in mono_gc_base_init in sgen-gc.c */
#define LOCK_INIT(name)	mono_mutex_init (&(name))
#define LOCK_GC do {						\
		MONO_PREPARE_BLOCKING	\
		mono_mutex_lock (&gc_mutex);			\
		MONO_GC_LOCKED ();				\
		MONO_FINISH_BLOCKING	\
	} while (0)
#define UNLOCK_GC do { sgen_gc_unlock (); } while (0)

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
extern guint64 stat_objects_alloced_degraded;
extern guint64 stat_bytes_alloced_degraded;
extern guint64 stat_copy_object_called_major;
extern guint64 stat_objects_copied_major;
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

extern int gc_debug_level;
extern FILE* gc_debug_file;

extern int current_collection_generation;

extern unsigned int sgen_global_stop_count;

extern gboolean bridge_processing_in_progress;
extern MonoGCBridgeCallbacks bridge_callbacks;

extern int num_ready_finalizers;

#define SGEN_ALLOC_ALIGN		8
#define SGEN_ALLOC_ALIGN_BITS	3

/* s must be non-negative */
#define SGEN_CAN_ALIGN_UP(s)		((s) <= SIZE_MAX - (SGEN_ALLOC_ALIGN - 1))
#define SGEN_ALIGN_UP(s)		(((s)+(SGEN_ALLOC_ALIGN-1)) & ~(SGEN_ALLOC_ALIGN-1))

#if SIZEOF_VOID_P == 4
#define ONE_P 1
#else
#define ONE_P 1ll
#endif

/*
 * The link pointer is hidden by negating each bit.  We use the lowest
 * bit of the link (before negation) to store whether it needs
 * resurrection tracking.
 */
#define HIDE_POINTER(p,t)	((gpointer)(~((size_t)(p)|((t)?1:0))))
#define REVEAL_POINTER(p)	((gpointer)((~(size_t)(p))&~3L))

#define SGEN_PTR_IN_NURSERY(p,bits,start,end)	(((mword)(p) & ~((1 << (bits)) - 1)) == (mword)(start))

#ifdef USER_CONFIG

/* good sizes are 512KB-1MB: larger ones increase a lot memzeroing time */
#define DEFAULT_NURSERY_SIZE (sgen_nursery_size)
extern size_t sgen_nursery_size;
/* The number of trailing 0 bits in DEFAULT_NURSERY_SIZE */
#define DEFAULT_NURSERY_BITS (sgen_nursery_bits)
extern int sgen_nursery_bits;

#else

#define DEFAULT_NURSERY_SIZE (4*1024*1024)
#define DEFAULT_NURSERY_BITS 22

#endif

extern char *sgen_nursery_start;
extern char *sgen_nursery_end;

static inline MONO_ALWAYS_INLINE gboolean
sgen_ptr_in_nursery (void *p)
{
	return SGEN_PTR_IN_NURSERY ((p), DEFAULT_NURSERY_BITS, sgen_nursery_start, sgen_nursery_end);
}

static inline MONO_ALWAYS_INLINE char*
sgen_get_nursery_start (void)
{
	return sgen_nursery_start;
}

static inline MONO_ALWAYS_INLINE char*
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

/*
 * We use the lowest three bits in the vtable pointer of objects to tag whether they're
 * forwarded, pinned, and/or cemented.  These are the valid states:
 *
 * | State            | bits |
 * |------------------+------+
 * | default          |  000 |
 * | forwarded        |  001 |
 * | pinned           |  010 |
 * | pinned, cemented |  110 |
 *
 * We store them in the vtable slot because the bits are used in the sync block for other
 * purposes: if we merge them and alloc the sync blocks aligned to 8 bytes, we can change
 * this and use bit 3 in the syncblock (with the lower two bits both set for forwarded, that
 * would be an invalid combination for the monitor and hash code).
 */

#include "sgen-tagged-pointer.h"

#define SGEN_VTABLE_BITS_MASK	SGEN_TAGGED_POINTER_MASK

#define SGEN_POINTER_IS_TAGGED_FORWARDED(p)	SGEN_POINTER_IS_TAGGED_1((p))
#define SGEN_POINTER_TAG_FORWARDED(p)		SGEN_POINTER_TAG_1((p))

#define SGEN_POINTER_IS_TAGGED_PINNED(p)	SGEN_POINTER_IS_TAGGED_2((p))
#define SGEN_POINTER_TAG_PINNED(p)		SGEN_POINTER_TAG_2((p))

#define SGEN_POINTER_IS_TAGGED_CEMENTED(p)	SGEN_POINTER_IS_TAGGED_4((p))
#define SGEN_POINTER_TAG_CEMENTED(p)		SGEN_POINTER_TAG_4((p))

#define SGEN_POINTER_UNTAG_VTABLE(p)		SGEN_POINTER_UNTAG_ALL((p))

/* returns NULL if not forwarded, or the forwarded address */
#define SGEN_VTABLE_IS_FORWARDED(vtable) (SGEN_POINTER_IS_TAGGED_FORWARDED ((vtable)) ? SGEN_POINTER_UNTAG_VTABLE ((vtable)) : NULL)
#define SGEN_OBJECT_IS_FORWARDED(obj) (SGEN_VTABLE_IS_FORWARDED (((mword*)(obj))[0]))

#define SGEN_VTABLE_IS_PINNED(vtable) SGEN_POINTER_IS_TAGGED_PINNED ((vtable))
#define SGEN_OBJECT_IS_PINNED(obj) (SGEN_VTABLE_IS_PINNED (((mword*)(obj))[0]))

#define SGEN_OBJECT_IS_CEMENTED(obj) (SGEN_POINTER_IS_TAGGED_CEMENTED (((mword*)(obj))[0]))

/* set the forwarded address fw_addr for object obj */
#define SGEN_FORWARD_OBJECT(obj,fw_addr) do {				\
		*(void**)(obj) = SGEN_POINTER_TAG_FORWARDED ((fw_addr));	\
	} while (0)
#define SGEN_PIN_OBJECT(obj) do {	\
		*(void**)(obj) = SGEN_POINTER_TAG_PINNED (*(void**)(obj)); \
	} while (0)
#define SGEN_CEMENT_OBJECT(obj) do {	\
		*(void**)(obj) = SGEN_POINTER_TAG_CEMENTED (*(void**)(obj)); \
	} while (0)
/* Unpins and uncements */
#define SGEN_UNPIN_OBJECT(obj) do {	\
		*(void**)(obj) = SGEN_POINTER_UNTAG_VTABLE (*(void**)(obj)); \
	} while (0)

/*
 * Since we set bits in the vtable, use the macro to load it from the pointer to
 * an object that is potentially pinned.
 */
#define SGEN_LOAD_VTABLE(addr)	SGEN_POINTER_UNTAG_ALL (*(void**)(addr))

/*
List of what each bit on of the vtable gc bits means. 
*/
enum {
	SGEN_GC_BIT_BRIDGE_OBJECT = 1,
	SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT = 2,
	SGEN_GC_BIT_FINALIZER_AWARE = 4,
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

int sgen_thread_handshake (BOOL suspend);
gboolean sgen_suspend_thread (SgenThreadInfo *info);
gboolean sgen_resume_thread (SgenThreadInfo *info);
void sgen_wait_for_suspend_ack (int count);
void sgen_os_init (void);

gboolean sgen_is_worker_thread (MonoNativeThreadId thread);

void sgen_update_heap_boundaries (mword low, mword high);

void sgen_scan_area_with_callback (char *start, char *end, IterateObjectCallbackFunc callback, void *data, gboolean allow_flags);
void sgen_check_section_scan_starts (GCMemSection *section);

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
	INTERNAL_MEM_OLD_BRIDGE_HASH_TABLE,
	INTERNAL_MEM_OLD_BRIDGE_HASH_TABLE_ENTRY,
	INTERNAL_MEM_BRIDGE_HASH_TABLE,
	INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY,
	INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE,
	INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE_ENTRY,
	INTERNAL_MEM_TARJAN_BRIDGE_HASH_TABLE,
	INTERNAL_MEM_TARJAN_BRIDGE_HASH_TABLE_ENTRY,
	INTERNAL_MEM_TARJAN_OBJ_BUCKET,
	INTERNAL_MEM_BRIDGE_DEBUG,
	INTERNAL_MEM_JOB_QUEUE_ENTRY,
	INTERNAL_MEM_TOGGLEREF_DATA,
	INTERNAL_MEM_CARDTABLE_MOD_UNION,
	INTERNAL_MEM_BINARY_PROTOCOL,
	INTERNAL_MEM_TEMPORARY,
	INTERNAL_MEM_MAX
};

enum {
	GENERATION_NURSERY,
	GENERATION_OLD,
	GENERATION_MAX
};

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
#define BINARY_PROTOCOL_ARG(x)	,x
#else
#define BINARY_PROTOCOL_ARG(x)
#endif

void sgen_init_internal_allocator (void);

typedef struct _ObjectList ObjectList;
struct _ObjectList {
	MonoObject *obj;
	ObjectList *next;
};

typedef void (*CopyOrMarkObjectFunc) (void**, SgenGrayQueue*);
typedef void (*ScanObjectFunc) (char *obj, mword desc, SgenGrayQueue*);
typedef void (*ScanVTypeFunc) (char*, mword desc, SgenGrayQueue* BINARY_PROTOCOL_ARG (size_t size));

typedef struct
{
	ScanObjectFunc scan_func;
	CopyOrMarkObjectFunc copy_func;
	SgenGrayQueue *queue;
} ScanCopyContext;

void sgen_report_internal_mem_usage (void);
void sgen_dump_internal_mem_usage (FILE *heap_dump_file);
void sgen_dump_section (GCMemSection *section, const char *type);
void sgen_dump_occupied (char *start, char *end, char *section_start);

void sgen_register_moved_object (void *obj, void *destination);

void sgen_register_fixed_internal_mem_type (int type, size_t size);

void* sgen_alloc_internal (int type);
void sgen_free_internal (void *addr, int type);

void* sgen_alloc_internal_dynamic (size_t size, int type, gboolean assert_on_failure);
void sgen_free_internal_dynamic (void *addr, size_t size, int type);

void sgen_pin_stats_register_object (char *obj, size_t size);
void sgen_pin_stats_register_global_remset (char *obj);
void sgen_pin_stats_print_class_stats (void);

void sgen_sort_addresses (void **array, size_t size);
void sgen_add_to_global_remset (gpointer ptr, gpointer obj);

int sgen_get_current_collection_generation (void);
gboolean sgen_collection_is_concurrent (void);
gboolean sgen_concurrent_collection_in_progress (void);

typedef struct {
	CopyOrMarkObjectFunc copy_or_mark_object;
	ScanObjectFunc scan_object;
	ScanVTypeFunc scan_vtype;
	/*FIXME add allocation function? */
} SgenObjectOperations;

SgenObjectOperations *sgen_get_current_object_ops (void);

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

void sgen_fragment_allocator_add (SgenFragmentAllocator *allocator, char *start, char *end);
void sgen_fragment_allocator_release (SgenFragmentAllocator *allocator);
void* sgen_fragment_allocator_serial_alloc (SgenFragmentAllocator *allocator, size_t size);
void* sgen_fragment_allocator_par_alloc (SgenFragmentAllocator *allocator, size_t size);
void* sgen_fragment_allocator_serial_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size);
void* sgen_fragment_allocator_par_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size);
SgenFragment* sgen_fragment_allocator_alloc (void);
void sgen_clear_allocator_fragments (SgenFragmentAllocator *allocator);
void sgen_clear_range (char *start, char *end);


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

extern char *sgen_space_bitmap;
extern size_t sgen_space_bitmap_size;

static inline gboolean
sgen_nursery_is_to_space (char *object)
{
	size_t idx = (object - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	size_t byte = idx >> 3;
	size_t bit = idx & 0x7;

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

	SgenObjectOperations serial_ops;

	void (*prepare_to_space) (char *to_space_bitmap, size_t space_bitmap_size);
	void (*clear_fragments) (void);
	SgenFragment* (*build_fragments_get_exclude_head) (void);
	void (*build_fragments_release_exclude_head) (void);
	void (*build_fragments_finish) (SgenFragmentAllocator *allocator);
	void (*init_nursery) (SgenFragmentAllocator *allocator, char *start, char *end);

	gboolean (*handle_gc_param) (const char *opt); /* Optional */
	void (*print_gc_param_usage) (void); /* Optional */
} SgenMinorCollector;

extern SgenMinorCollector sgen_minor_collector;

void sgen_simple_nursery_init (SgenMinorCollector *collector);
void sgen_split_nursery_init (SgenMinorCollector *collector);

/* Updating references */

#ifdef SGEN_CHECK_UPDATE_REFERENCE
static inline void
sgen_update_reference (void **p, void *o, gboolean allow_null)
{
	if (!allow_null)
		SGEN_ASSERT (0, o, "Cannot update a reference with a NULL pointer");
	SGEN_ASSERT (0, !sgen_is_worker_thread (mono_native_thread_id_get ()), "Can't update a reference in the worker thread");
	*p = o;
}

#define SGEN_UPDATE_REFERENCE_ALLOW_NULL(p,o)	sgen_update_reference ((void**)(p), (void*)(o), TRUE)
#define SGEN_UPDATE_REFERENCE(p,o)		sgen_update_reference ((void**)(p), (void*)(o), FALSE)
#else
#define SGEN_UPDATE_REFERENCE_ALLOW_NULL(p,o)	(*(void**)(p) = (void*)(o))
#define SGEN_UPDATE_REFERENCE(p,o)		SGEN_UPDATE_REFERENCE_ALLOW_NULL ((p), (o))
#endif

/* Major collector */

typedef void (*sgen_cardtable_block_callback) (mword start, mword size);
void sgen_major_collector_iterate_live_block_ranges (sgen_cardtable_block_callback callback);

typedef enum {
	ITERATE_OBJECTS_SWEEP = 1,
	ITERATE_OBJECTS_NON_PINNED = 2,
	ITERATE_OBJECTS_PINNED = 4,
	ITERATE_OBJECTS_ALL = ITERATE_OBJECTS_NON_PINNED | ITERATE_OBJECTS_PINNED,
	ITERATE_OBJECTS_SWEEP_NON_PINNED = ITERATE_OBJECTS_SWEEP | ITERATE_OBJECTS_NON_PINNED,
	ITERATE_OBJECTS_SWEEP_PINNED = ITERATE_OBJECTS_SWEEP | ITERATE_OBJECTS_PINNED,
	ITERATE_OBJECTS_SWEEP_ALL = ITERATE_OBJECTS_SWEEP | ITERATE_OBJECTS_NON_PINNED | ITERATE_OBJECTS_PINNED
} IterateObjectsFlags;

typedef struct
{
	size_t num_scanned_objects;
	size_t num_unique_scanned_objects;
} ScannedObjectCounts;

typedef struct _SgenMajorCollector SgenMajorCollector;
struct _SgenMajorCollector {
	size_t section_size;
	gboolean is_concurrent;
	gboolean supports_cardtable;
	gboolean sweeps_lazily;

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

	void* (*alloc_object) (MonoVTable *vtable, size_t size, gboolean has_references);
	void (*free_pinned_object) (char *obj, size_t size);
	void (*iterate_objects) (IterateObjectsFlags flags, IterateObjectCallbackFunc callback, void *data);
	void (*free_non_pinned_object) (char *obj, size_t size);
	void (*pin_objects) (SgenGrayQueue *queue);
	void (*pin_major_object) (char *obj, SgenGrayQueue *queue);
	void (*scan_card_table) (gboolean mod_union, SgenGrayQueue *queue);
	void (*iterate_live_block_ranges) (sgen_cardtable_block_callback callback);
	void (*update_cardtable_mod_union) (void);
	void (*init_to_space) (void);
	void (*sweep) (void);
	gboolean (*have_finished_sweeping) (void);
	void (*free_swept_blocks) (void);
	void (*check_scan_starts) (void);
	void (*dump_heap) (FILE *heap_dump_file);
	gint64 (*get_used_size) (void);
	void (*start_nursery_collection) (void);
	void (*finish_nursery_collection) (void);
	void (*start_major_collection) (void);
	void (*finish_major_collection) (ScannedObjectCounts *counts);
	gboolean (*drain_gray_stack) (ScanCopyContext ctx);
	gboolean (*ptr_is_in_non_pinned_space) (char *ptr, char **start);
	gboolean (*obj_is_from_pinned_alloc) (char *obj);
	void (*report_pinned_memory_usage) (void);
	size_t (*get_num_major_sections) (void);
	gboolean (*handle_gc_param) (const char *opt);
	void (*print_gc_param_usage) (void);
	gboolean (*is_worker_thread) (MonoNativeThreadId thread);
	void (*post_param_init) (SgenMajorCollector *collector);
	void* (*alloc_worker_data) (void);
	void (*init_worker_thread) (void *data);
	void (*reset_worker_data) (void *data);
	gboolean (*is_valid_object) (char *object);
	MonoVTable* (*describe_pointer) (char *pointer);
	guint8* (*get_cardtable_mod_union_for_object) (char *object);
	long long (*get_and_reset_num_major_objects_marked) (void);
	void (*count_cards) (long long *num_total_cards, long long *num_marked_cards);
};

extern SgenMajorCollector major_collector;

void sgen_marksweep_init (SgenMajorCollector *collector);
void sgen_marksweep_fixed_init (SgenMajorCollector *collector);
void sgen_marksweep_par_init (SgenMajorCollector *collector);
void sgen_marksweep_fixed_par_init (SgenMajorCollector *collector);
void sgen_marksweep_conc_init (SgenMajorCollector *collector);
SgenMajorCollector* sgen_get_major_collector (void);


typedef struct _SgenRememberedSet {
	void (*wbarrier_set_field) (MonoObject *obj, gpointer field_ptr, MonoObject* value);
	void (*wbarrier_set_arrayref) (MonoArray *arr, gpointer slot_ptr, MonoObject* value);
	void (*wbarrier_arrayref_copy) (gpointer dest_ptr, gpointer src_ptr, int count);
	void (*wbarrier_value_copy) (gpointer dest, gpointer src, int count, MonoClass *klass);
	void (*wbarrier_object_copy) (MonoObject* obj, MonoObject *src);
	void (*wbarrier_generic_nostore) (gpointer ptr);
	void (*record_pointer) (gpointer ptr);

	void (*scan_remsets) (SgenGrayQueue *queue);

	void (*clear_cards) (void);

	void (*finish_minor_collection) (void);
	gboolean (*find_address) (char *addr);
	gboolean (*find_address_with_cards) (char *cards_start, guint8 *cards, char *addr);
} SgenRememberedSet;

SgenRememberedSet *sgen_get_remset (void);

static mword /*__attribute__((noinline)) not sure if this hint is a good idea*/
slow_object_get_size (MonoVTable *vtable, MonoObject* o)
{
	MonoClass *klass = vtable->klass;

	/*
	 * We depend on mono_string_length_fast and
	 * mono_array_length_fast not using the object's vtable.
	 */
	if (klass == mono_defaults.string_class) {
		return G_STRUCT_OFFSET (MonoString, chars) + 2 * mono_string_length_fast ((MonoString*) o) + 2;
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

static inline mword
sgen_vtable_get_descriptor (MonoVTable *vtable)
{
	return (mword)vtable->gc_descr;
}

static inline mword
sgen_obj_get_descriptor (char *obj)
{
	MonoVTable *vtable = ((MonoObject*)obj)->vtable;
	SGEN_ASSERT (9, !SGEN_POINTER_IS_TAGGED_ANY (vtable), "Object can't be tagged");
	return sgen_vtable_get_descriptor (vtable);
}

static inline mword
sgen_obj_get_descriptor_safe (char *obj)
{
	MonoVTable *vtable = (MonoVTable*)SGEN_LOAD_VTABLE (obj);
	return sgen_vtable_get_descriptor (vtable);
}

/*
 * This function can be called on an object whose first word, the
 * vtable field, is not intact.  This is necessary for the parallel
 * collector.
 */
static MONO_NEVER_INLINE mword
sgen_par_object_get_size (MonoVTable *vtable, MonoObject* o)
{
	mword descr = (mword)vtable->gc_descr;
	mword type = descr & DESC_TYPE_MASK;

	if (type == DESC_TYPE_RUN_LENGTH || type == DESC_TYPE_SMALL_PTRFREE) {
		mword size = descr & 0xfff8;
		SGEN_ASSERT (9, size >= sizeof (MonoObject), "Run length object size to small");
		return size;
	} else if (descr == SGEN_DESC_STRING) {
		return G_STRUCT_OFFSET (MonoString, chars) + 2 * mono_string_length_fast ((MonoString*) o) + 2;
	} else if (type == DESC_TYPE_VECTOR) {
		int element_size = ((descr) >> VECTOR_ELSIZE_SHIFT) & MAX_ELEMENT_SIZE;
		MonoArray *array = (MonoArray*)o;
		size_t size = sizeof (MonoArray) + element_size * mono_array_length_fast (array);

		/*
		 * Non-vector arrays with a single dimension whose lower bound is zero are
		 * allocated without bounds.
		 */
		if ((descr & VECTOR_KIND_ARRAY) && array->bounds) {
			size += sizeof (mono_array_size_t) - 1;
			size &= ~(sizeof (mono_array_size_t) - 1);
			size += sizeof (MonoArrayBounds) * vtable->klass->rank;
		}
		return size;
	}

	return slow_object_get_size (vtable, o);
}

static inline mword
sgen_safe_object_get_size (MonoObject *obj)
{
       char *forwarded;

       if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj)))
               obj = (MonoObject*)forwarded;

       return sgen_par_object_get_size ((MonoVTable*)SGEN_LOAD_VTABLE (obj), obj);
}

/*
 * This variant guarantees to return the exact size of the object
 * before alignment. Needed for canary support.
 */
static inline guint
sgen_safe_object_get_size_unaligned (MonoObject *obj)
{
       char *forwarded;

       if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
               obj = (MonoObject*)forwarded;
       }

       return slow_object_get_size ((MonoVTable*)SGEN_LOAD_VTABLE (obj), obj);
}

const char* sgen_safe_name (void* obj);

gboolean sgen_object_is_live (void *obj);

void  sgen_init_fin_weak_hash (void);

gboolean sgen_need_bridge_processing (void);
void sgen_bridge_reset_data (void);
void sgen_bridge_processing_stw_step (void);
void sgen_bridge_processing_finish (int generation);
void sgen_register_test_bridge_callbacks (const char *bridge_class_name);
gboolean sgen_is_bridge_object (MonoObject *obj);
MonoGCBridgeObjectKind sgen_bridge_class_kind (MonoClass *klass);
void sgen_mark_bridge_object (MonoObject *obj);
void sgen_bridge_register_finalized_object (MonoObject *object);
void sgen_bridge_describe_pointer (MonoObject *object);

void sgen_mark_togglerefs (char *start, char *end, ScanCopyContext ctx);
void sgen_clear_togglerefs (char *start, char *end, ScanCopyContext ctx);

void sgen_process_togglerefs (void);
void sgen_register_test_toggleref_callback (void);

gboolean sgen_is_bridge_object (MonoObject *obj);
void sgen_mark_bridge_object (MonoObject *obj);

gboolean sgen_bridge_handle_gc_debug (const char *opt);
void sgen_bridge_print_gc_debug_usage (void);

typedef struct {
	void (*reset_data) (void);
	void (*processing_stw_step) (void);
	void (*processing_build_callback_data) (int generation);
	void (*processing_after_callback) (int generation);
	MonoGCBridgeObjectKind (*class_kind) (MonoClass *class);
	void (*register_finalized_object) (MonoObject *object);
	void (*describe_pointer) (MonoObject *object);
	void (*enable_accounting) (void);
	void (*set_dump_prefix) (const char *prefix);

	/*
	 * These are set by processing_build_callback_data().
	 */
	int num_sccs;
	MonoGCBridgeSCC **api_sccs;

	int num_xrefs;
	MonoGCBridgeXRef *api_xrefs;
} SgenBridgeProcessor;

void sgen_old_bridge_init (SgenBridgeProcessor *collector);
void sgen_new_bridge_init (SgenBridgeProcessor *collector);
void sgen_tarjan_bridge_init (SgenBridgeProcessor *collector);
void sgen_set_bridge_implementation (const char *name);
void sgen_bridge_set_dump_prefix (const char *prefix);

gboolean sgen_compare_bridge_processor_results (SgenBridgeProcessor *a, SgenBridgeProcessor *b);

typedef mono_bool (*WeakLinkAlivePredicateFunc) (MonoObject*, void*);

void sgen_null_links_with_predicate (int generation, WeakLinkAlivePredicateFunc predicate, void *data);

gboolean sgen_gc_is_object_ready_for_finalization (void *object);
void sgen_gc_lock (void);
void sgen_gc_unlock (void);
void sgen_gc_event_moves (void);

void sgen_queue_finalization_entry (MonoObject *obj);
const char* sgen_generation_name (int generation);

void sgen_collect_bridge_objects (int generation, ScanCopyContext ctx);
void sgen_finalize_in_range (int generation, ScanCopyContext ctx);
void sgen_null_link_in_range (int generation, gboolean before_finalization, ScanCopyContext ctx);
void sgen_null_links_for_domain (MonoDomain *domain, int generation);
void sgen_remove_finalizers_for_domain (MonoDomain *domain, int generation);
void sgen_process_fin_stage_entries (void);
void sgen_process_dislink_stage_entries (void);
void sgen_register_disappearing_link (MonoObject *obj, void **link, gboolean track, gboolean in_gc);

gboolean sgen_drain_gray_stack (int max_objs, ScanCopyContext ctx);

enum {
	SPACE_NURSERY,
	SPACE_MAJOR,
	SPACE_LOS
};

void sgen_pin_object (void *object, SgenGrayQueue *queue);
void sgen_set_pinned_from_failed_allocation (mword objsize);

void sgen_ensure_free_space (size_t size);
void sgen_perform_collection (size_t requested_size, int generation_to_collect, const char *reason, gboolean wait_to_finish);
gboolean sgen_has_critical_method (void);
gboolean sgen_is_critical_method (MonoMethod *method);

/* STW */

typedef struct {
	int generation;
	const char *reason;
	gboolean is_overflow;
	SGEN_TV_DECLARE (total_time);
	SGEN_TV_DECLARE (stw_time);
	SGEN_TV_DECLARE (bridge_time);
} GGTimingInfo;

int sgen_stop_world (int generation);
int sgen_restart_world (int generation, GGTimingInfo *timing);
void sgen_init_stw (void);

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

extern LOSObject *los_object_list;
extern mword los_memory_usage;

void sgen_los_free_object (LOSObject *obj);
void* sgen_los_alloc_large_inner (MonoVTable *vtable, size_t size);
void sgen_los_sweep (void);
gboolean sgen_ptr_is_in_los (char *ptr, char **start);
void sgen_los_iterate_objects (IterateObjectCallbackFunc cb, void *user_data);
void sgen_los_iterate_live_block_ranges (sgen_cardtable_block_callback callback);
void sgen_los_scan_card_table (gboolean mod_union, SgenGrayQueue *queue);
void sgen_los_update_cardtable_mod_union (void);
void sgen_los_count_cards (long long *num_total_cards, long long *num_marked_cards);
void sgen_major_collector_scan_card_table (SgenGrayQueue *queue);
gboolean sgen_los_is_valid_object (char *object);
gboolean mono_sgen_los_describe_pointer (char *ptr);
LOSObject* sgen_los_header_for_object (char *data);
mword sgen_los_object_size (LOSObject *obj);
void sgen_los_pin_object (char *obj);
void sgen_los_unpin_object (char *obj);
gboolean sgen_los_object_is_pinned (char *obj);


/* nursery allocator */

void sgen_clear_nursery_fragments (void);
void sgen_nursery_allocator_prepare_for_pinning (void);
void sgen_nursery_allocator_set_nursery_bounds (char *nursery_start, char *nursery_end);
mword sgen_build_nursery_fragments (GCMemSection *nursery_section, SgenGrayQueue *unpin_queue);
void sgen_init_nursery_allocator (void);
void sgen_nursery_allocator_init_heavy_stats (void);
void sgen_alloc_init_heavy_stats (void);
char* sgen_nursery_alloc_get_upper_alloc_bound (void);
void* sgen_nursery_alloc (size_t size);
void* sgen_nursery_alloc_range (size_t size, size_t min_size, size_t *out_alloc_size);
MonoVTable* sgen_get_array_fill_vtable (void);
gboolean sgen_can_alloc_size (size_t size);
void sgen_nursery_retire_region (void *address, ptrdiff_t size);

void sgen_nursery_alloc_prepare_for_minor (void);
void sgen_nursery_alloc_prepare_for_major (void);

char* sgen_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references);

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

#define EMIT_TLS_ACCESS_NEXT_ADDR(mb)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), TLS_KEY_SGEN_TLAB_NEXT_ADDR);		\
	} while (0)

#define EMIT_TLS_ACCESS_TEMP_END(mb)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), TLS_KEY_SGEN_TLAB_TEMP_END);		\
	} while (0)

#else

#if defined(__APPLE__) || defined (HOST_WIN32)
#define EMIT_TLS_ACCESS_NEXT_ADDR(mb)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), TLS_KEY_SGEN_THREAD_INFO);	\
	mono_mb_emit_icon ((mb), MONO_STRUCT_OFFSET (SgenThreadInfo, tlab_next_addr));	\
	mono_mb_emit_byte ((mb), CEE_ADD);		\
	mono_mb_emit_byte ((mb), CEE_LDIND_I);		\
	} while (0)

#define EMIT_TLS_ACCESS_TEMP_END(mb)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), TLS_KEY_SGEN_THREAD_INFO);	\
	mono_mb_emit_icon ((mb), MONO_STRUCT_OFFSET (SgenThreadInfo, tlab_temp_end));	\
	mono_mb_emit_byte ((mb), CEE_ADD);		\
	mono_mb_emit_byte ((mb), CEE_LDIND_I);		\
	} while (0)

#else
#define EMIT_TLS_ACCESS_NEXT_ADDR(mb)	do { g_error ("sgen is not supported when using --with-tls=pthread.\n"); } while (0)
#define EMIT_TLS_ACCESS_TEMP_END(mb)	do { g_error ("sgen is not supported when using --with-tls=pthread.\n"); } while (0)
#endif

#endif

/* Other globals */

extern GCMemSection *nursery_section;
extern guint32 collect_before_allocs;
extern guint32 verify_before_allocs;
extern gboolean has_per_allocation_action;
extern size_t degraded_mode;
extern int default_nursery_size;
extern guint32 tlab_size;
extern NurseryClearPolicy nursery_clear_policy;
extern gboolean sgen_try_free_some_memory;

extern LOCK_DECLARE (gc_mutex);

extern int do_pin_stats;

/* Nursery helpers. */

static inline void
sgen_set_nursery_scan_start (char *p)
{
	size_t idx = (p - (char*)nursery_section->data) / SGEN_SCAN_START_SIZE;
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
void sgen_check_whole_heap_stw (void);
void sgen_check_objref (char *obj);
void sgen_check_heap_marked (gboolean nursery_must_be_pinned);
void sgen_check_nursery_objects_pinned (gboolean pinned);
void sgen_scan_for_registered_roots_in_domain (MonoDomain *domain, int root_type);
void sgen_check_for_xdomain_refs (void);
char* sgen_find_object_for_ptr (char *ptr);

void mono_gc_scan_for_specific_ref (MonoObject *key, gboolean precise);

/* Write barrier support */

/*
 * This causes the compile to extend the liveness of 'v' till the call to dummy_use
 */
static inline void
sgen_dummy_use (gpointer v) {
#if defined(__GNUC__)
	__asm__ volatile ("" : "=r"(v) : "r"(v));
#elif defined(_MSC_VER)
	static volatile gpointer ptr;
	ptr = v;
#else
#error "Implement sgen_dummy_use for your compiler"
#endif
}

/* Environment variable parsing */

#define MONO_GC_PARAMS_NAME	"MONO_GC_PARAMS"
#define MONO_GC_DEBUG_NAME	"MONO_GC_DEBUG"

gboolean sgen_parse_environment_string_extract_number (const char *str, size_t *out);
void sgen_env_var_error (const char *env_var, const char *fallback, const char *description_format, ...);

/* Utilities */

void sgen_qsort (void *base, size_t nel, size_t width, int (*compar) (const void*, const void*));
gint64 sgen_timestamp (void);

/*
 * Canary (guard word) support
 * Notes:
 * - CANARY_SIZE must be multiple of word size in bytes
 * - Canary space is not included on checks against SGEN_MAX_SMALL_OBJ_SIZE
 */
 
gboolean nursery_canaries_enabled (void);

#define CANARY_SIZE 8
#define CANARY_STRING  "koupepia"

#define CANARIFY_SIZE(size) if (nursery_canaries_enabled ()) {	\
			size = size + CANARY_SIZE;	\
		}

#define CANARIFY_ALLOC(addr,size) if (nursery_canaries_enabled ()) {	\
				memcpy ((char*) (addr) + (size), CANARY_STRING, CANARY_SIZE);	\
			}

#define CANARY_VALID(addr) (strncmp ((char*) (addr), CANARY_STRING, CANARY_SIZE) == 0)

#define CHECK_CANARY_FOR_OBJECT(addr) if (nursery_canaries_enabled ()) {	\
				char* canary_ptr = (char*) (addr) + sgen_safe_object_get_size_unaligned ((MonoObject *) (addr));	\
				if (!CANARY_VALID(canary_ptr)) {	\
					char canary_copy[CANARY_SIZE +1];	\
					strncpy (canary_copy, canary_ptr, CANARY_SIZE);	\
					canary_copy[CANARY_SIZE] = 0;	\
					g_error ("CORRUPT CANARY:\naddr->%p\ntype->%s\nexcepted->'%s'\nfound->'%s'\n", (char*) addr, ((MonoObject*)addr)->vtable->klass->name, CANARY_STRING, canary_copy);	\
				} }
				 
#endif /* HAVE_SGEN_GC */

#endif /* __MONO_SGENGC_H__ */
