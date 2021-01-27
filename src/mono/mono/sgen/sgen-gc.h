/**
 * \file
 * Simple generational GC.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

/* pthread impl */
#include "config.h"

#ifdef HAVE_SGEN_GC

#include <mono/utils/mono-forward-internal.h>
#undef THREAD_INFO_TYPE
#define THREAD_INFO_TYPE SgenThreadInfo

#include <glib.h>
#include <stdio.h>
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#include <stdint.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/atomic.h"
#include "mono/utils/mono-os-mutex.h"
#include "mono/utils/mono-coop-mutex.h"
#include "mono/utils/ward.h"
#include "mono/sgen/sgen-conf.h"
#include "mono/sgen/sgen-hash-table.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/gc-internal-agnostic.h"
#include "mono/sgen/sgen-thread-pool.h"

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

#if !defined(__MACH__) && !MONO_MACH_ARCH_SUPPORTED && defined(HAVE_PTHREAD_KILL)
#define SGEN_POSIX_STW 1
#endif

/*
 * The nursery section uses this struct.
 */
typedef struct _GCMemSection GCMemSection;
struct _GCMemSection {
	char *data;
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
#define LOCK_GC do { sgen_gc_lock (); } while (0)
#define UNLOCK_GC do { sgen_gc_unlock (); } while (0)

extern MonoCoopMutex sgen_interruption_mutex;

#define LOCK_INTERRUPTION mono_coop_mutex_lock (&sgen_interruption_mutex)
#define UNLOCK_INTERRUPTION mono_coop_mutex_unlock (&sgen_interruption_mutex)

/* FIXME: Use mono_atomic_add_i32 & mono_atomic_add_i64 to reduce the CAS cost. */
#define SGEN_CAS	mono_atomic_cas_i32
#define SGEN_CAS_PTR	mono_atomic_cas_ptr
#define SGEN_ATOMIC_ADD(x,i)	do {					\
		int __old_x;						\
		do {							\
			__old_x = (x);					\
		} while (mono_atomic_cas_i32 (&(x), __old_x + (i), __old_x) != __old_x); \
	} while (0)
#define SGEN_ATOMIC_ADD_P(x,i) do { \
		size_t __old_x;                                            \
		do {                                                    \
			__old_x = (x);                                  \
		} while (mono_atomic_cas_ptr ((void**)&(x), (void*)(__old_x + (i)), (void*)__old_x) != (void*)__old_x); \
	} while (0)

#ifndef MONO_ATOMIC_USES_LOCK
#define SGEN_ATOMIC_ADD_I64(x,i) do { \
		mono_atomic_add_i64 ((volatile gint64 *)&x, i); \
	} while (0)
#else
#define SGEN_ATOMIC_ADD_I64(x,i) do { \
		gint64 __old_x; \
		do { \
			__old_x = (x); \
		} while (mono_sgen_atomic_cas_i64 ((volatile gint64 *)&(x), __old_x + (i), __old_x) != __old_x); \
	} while (0)
#endif /* BROKEN_64BIT_ATOMICS_INTRINSIC */

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

#ifdef HAVE_LOCALTIME_R
# define LOG_TIMESTAMP  \
	do {	\
		time_t t;									\
		struct tm tod;									\
		time(&t);									\
		localtime_r(&t, &tod);								\
		strftime(logTime, sizeof(logTime), MONO_STRFTIME_F " " MONO_STRFTIME_T, &tod);	\
	} while (0)
#else
# define LOG_TIMESTAMP  \
	do {	\
		time_t t;									\
		struct tm *tod;									\
		time(&t);									\
		tod = localtime(&t);								\
		strftime(logTime, sizeof(logTime), MONO_STRFTIME_F " " MONO_STRFTIME_T, tod);	\
	} while (0)
#endif

#define SGEN_LOG(level, format, ...) do {      \
	if (G_UNLIKELY ((level) <= SGEN_MAX_DEBUG_LEVEL && (level) <= sgen_gc_debug_level)) {	\
		char logTime[80];								\
		LOG_TIMESTAMP;									\
		mono_gc_printf (sgen_gc_debug_file, "%s " format "\n", logTime, ##__VA_ARGS__);	\
} } while (0)

#define SGEN_COND_LOG(level, cond, format, ...) do {	\
	if (G_UNLIKELY ((level) <= SGEN_MAX_DEBUG_LEVEL && (level) <= sgen_gc_debug_level)) {		\
		if (cond) {										\
			char logTime[80];								\
			LOG_TIMESTAMP;									\
			mono_gc_printf (sgen_gc_debug_file, "%s " format "\n", logTime, ##__VA_ARGS__);	\
		}											\
} } while (0)

extern int sgen_gc_debug_level;
extern FILE* sgen_gc_debug_file;

extern int sgen_current_collection_generation;

extern unsigned int sgen_global_stop_count;

#define SGEN_ALIGN_UP_TO(val,align)	(((val) + (align - 1)) & ~(align - 1))
#define SGEN_ALIGN_DOWN_TO(val,align)	((val) & ~(align - 1))

#define SGEN_ALLOC_ALIGN		8
#define SGEN_ALLOC_ALIGN_BITS	3

/* s must be non-negative */
#define SGEN_CAN_ALIGN_UP(s)		((s) <= SIZE_MAX - (SGEN_ALLOC_ALIGN - 1))
#define SGEN_ALIGN_UP(s)		SGEN_ALIGN_UP_TO(s, SGEN_ALLOC_ALIGN)
#define SGEN_ALIGN_DOWN(s)		SGEN_ALIGN_DOWN_TO(s, SGEN_ALLOC_ALIGN)

#if SIZEOF_VOID_P == 4
#define ONE_P 1
#else
#define ONE_P 1ll
#endif

static inline guint
sgen_aligned_addr_hash (gconstpointer ptr)
{
	return GPOINTER_TO_UINT (ptr) >> 3;
}

#define SGEN_PTR_IN_NURSERY(p,bits,start,end)	(((mword)(p) & ~(((mword)1 << (bits)) - 1)) == (mword)(start))

extern size_t sgen_nursery_size;
extern size_t sgen_nursery_max_size;
extern int sgen_nursery_bits;

extern char *sgen_nursery_start;
extern char *sgen_nursery_end;

static inline MONO_ALWAYS_INLINE gboolean
sgen_ptr_in_nursery (void *p)
{
	return SGEN_PTR_IN_NURSERY ((p), sgen_nursery_bits, sgen_nursery_start, sgen_nursery_end);
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
#define SGEN_VTABLE_IS_FORWARDED(vtable) ((GCObject *)(SGEN_POINTER_IS_TAGGED_FORWARDED ((vtable)) ? SGEN_POINTER_UNTAG_VTABLE ((vtable)) : NULL))
#define SGEN_OBJECT_IS_FORWARDED(obj) ((GCObject *)SGEN_VTABLE_IS_FORWARDED (((mword*)(obj))[0]))

#define SGEN_VTABLE_IS_PINNED(vtable) SGEN_POINTER_IS_TAGGED_PINNED ((vtable))
#define SGEN_OBJECT_IS_PINNED(obj) (SGEN_VTABLE_IS_PINNED (((mword*)(obj))[0]))

#define SGEN_OBJECT_IS_CEMENTED(obj) (SGEN_POINTER_IS_TAGGED_CEMENTED (((mword*)(obj))[0]))

/* set the forwarded address fw_addr for object obj */
#define SGEN_FORWARD_OBJECT(obj,fw_addr) do {				\
		*(void**)(obj) = SGEN_POINTER_TAG_FORWARDED ((fw_addr));	\
	} while (0)
#define SGEN_FORWARD_OBJECT_PAR(obj,fw_addr,final_fw_addr) do {			\
		gpointer old_vtable_word = *(gpointer*)obj;			\
		gpointer new_vtable_word;					\
		final_fw_addr = SGEN_VTABLE_IS_FORWARDED (old_vtable_word);	\
		if (final_fw_addr)						\
			break;							\
		new_vtable_word = SGEN_POINTER_TAG_FORWARDED ((fw_addr));	\
		old_vtable_word = mono_atomic_cas_ptr ((gpointer*)obj, new_vtable_word, old_vtable_word); \
		final_fw_addr = SGEN_VTABLE_IS_FORWARDED (old_vtable_word);	\
		if (!final_fw_addr)						\
			final_fw_addr = (fw_addr);				\
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
#define SGEN_LOAD_VTABLE(obj)		((GCVTable)(SGEN_POINTER_UNTAG_ALL (SGEN_LOAD_VTABLE_UNCHECKED ((GCObject *)(obj)))))

/*
List of what each bit on of the vtable gc bits means. 
*/
enum {
	// When the Java bridge has determined an object is "bridged", it uses these two bits to cache that information.
	SGEN_GC_BIT_BRIDGE_OBJECT = 1,
	SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT = 2,
	SGEN_GC_BIT_FINALIZER_AWARE = 4,
};

void sgen_gc_init (void)
    MONO_PERMIT (need (sgen_lock_gc));

void sgen_os_init (void);

void sgen_update_heap_boundaries (mword low, mword high);

void sgen_check_section_scan_starts (GCMemSection *section);

void sgen_conservatively_pin_objects_from (void **start, void **end, void *start_nursery, void *end_nursery, int pin_type);

gboolean sgen_gc_initialized (void);

/* Keep in sync with description_for_type() in sgen-internal.c! */
enum {
	INTERNAL_MEM_PIN_QUEUE,
	INTERNAL_MEM_FRAGMENT,
	INTERNAL_MEM_SECTION,
	INTERNAL_MEM_SCAN_STARTS,
	INTERNAL_MEM_FIN_TABLE,
	INTERNAL_MEM_FINALIZE_ENTRY,
	INTERNAL_MEM_FINALIZE_READY,
	INTERNAL_MEM_DISLINK_TABLE,
	INTERNAL_MEM_DISLINK,
	INTERNAL_MEM_ROOTS_TABLE,
	INTERNAL_MEM_ROOT_RECORD,
	INTERNAL_MEM_STATISTICS,
	INTERNAL_MEM_STAT_PINNED_CLASS,
	INTERNAL_MEM_STAT_REMSET_CLASS,
	INTERNAL_MEM_STAT_GCHANDLE_CLASS,
	INTERNAL_MEM_GRAY_QUEUE,
	INTERNAL_MEM_MS_TABLES,
	INTERNAL_MEM_MS_BLOCK_INFO,
	INTERNAL_MEM_MS_BLOCK_INFO_SORT,
	INTERNAL_MEM_WORKER_DATA,
	INTERNAL_MEM_THREAD_POOL_JOB,
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
	INTERNAL_MEM_TOGGLEREF_DATA,
	INTERNAL_MEM_CARDTABLE_MOD_UNION,
	INTERNAL_MEM_BINARY_PROTOCOL,
	INTERNAL_MEM_TEMPORARY,
	INTERNAL_MEM_LOG_ENTRY,
	INTERNAL_MEM_COMPLEX_DESCRIPTORS,
	INTERNAL_MEM_FIRST_CLIENT
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

#define SGEN_DEFINE_OBJECT_VTABLE
#ifdef SGEN_CLIENT_HEADER
#include SGEN_CLIENT_HEADER
#else
#include "metadata/sgen-client-mono.h"
#endif
#undef SGEN_DEFINE_OBJECT_VTABLE

#include "mono/sgen/sgen-descriptor.h"
#include "mono/sgen/sgen-gray.h"

/* the runtime can register areas of memory as roots: we keep two lists of roots,
 * a pinned root set for conservatively scanned roots and a normal one for
 * precisely scanned roots (currently implemented as a single list).
 */
typedef struct _RootRecord RootRecord;
struct _RootRecord {
	char *end_root;
	SgenDescriptor root_desc;
	int source;
	const char *msg;
};

enum {
	ROOT_TYPE_NORMAL = 0, /* "normal" roots */
	ROOT_TYPE_PINNED = 1, /* roots without a GC descriptor */
	ROOT_TYPE_WBARRIER = 2, /* roots with a write barrier */
	ROOT_TYPE_NUM
};

extern SgenHashTable sgen_roots_hash [ROOT_TYPE_NUM];

int sgen_register_root (char *start, size_t size, SgenDescriptor descr, int root_type, MonoGCRootSource source, void *key, const char *msg)
	MONO_PERMIT (need (sgen_lock_gc));
void sgen_deregister_root (char* addr)
	MONO_PERMIT (need (sgen_lock_gc));

typedef void (*IterateObjectCallbackFunc) (GCObject*, size_t, void*);
typedef gboolean (*IterateObjectResultCallbackFunc) (GCObject*, size_t, void*);

void sgen_scan_area_with_callback (char *start, char *end, IterateObjectCallbackFunc callback, void *data, gboolean allow_flags, gboolean fail_on_canaries);

/* eventually share with MonoThread? */
/*
 * This structure extends the MonoThreadInfo structure.
 */
struct _SgenThreadInfo {
	SgenClientThreadInfo client_info;

	char *tlab_start;
	char *tlab_next;
	char *tlab_temp_end;
	char *tlab_real_end;

	/* Total bytes allocated by this thread in its lifetime so far. */
	gint64 total_bytes_allocated;
};

gboolean sgen_is_worker_thread (MonoNativeThreadId thread);

typedef void (*CopyOrMarkObjectFunc) (GCObject**, SgenGrayQueue*);
typedef void (*ScanObjectFunc) (GCObject *obj, SgenDescriptor desc, SgenGrayQueue*);
typedef void (*ScanVTypeFunc) (GCObject *full_object, char *start, SgenDescriptor desc, SgenGrayQueue* BINARY_PROTOCOL_ARG (size_t size));
typedef void (*ScanPtrFieldFunc) (GCObject *obj, GCObject **ptr, SgenGrayQueue* queue);
typedef gboolean (*DrainGrayStackFunc) (SgenGrayQueue *queue);

typedef struct {
	CopyOrMarkObjectFunc copy_or_mark_object;
	ScanObjectFunc scan_object;
	ScanVTypeFunc scan_vtype;
	ScanPtrFieldFunc scan_ptr_field;
	/* Drain stack optimized for the above functions */
	DrainGrayStackFunc drain_gray_stack;
	/*FIXME add allocation function? */
} SgenObjectOperations;

typedef struct
{
	SgenObjectOperations *ops;
	SgenGrayQueue *queue;
} ScanCopyContext;

// error C4576: a parenthesized type followed by an initializer list is a non-standard explicit type conversion
// An inline function or constructor would work here too.
#ifdef _MSC_VER
#define MONO_MSC_WARNING_SUPPRESS(warn, body) __pragma (warning (suppress:warn)) body
#else
#define MONO_MSC_WARNING_SUPPRESS(warn, body) body
#endif
#define CONTEXT_FROM_OBJECT_OPERATIONS(ops, queue) MONO_MSC_WARNING_SUPPRESS (4576, ((ScanCopyContext) { (ops), (queue) }))

void sgen_report_internal_mem_usage (void);
void sgen_dump_internal_mem_usage (FILE *heap_dump_file);
void sgen_dump_section (GCMemSection *section, const char *type);
void sgen_dump_occupied (char *start, char *end, char *section_start);

void sgen_register_fixed_internal_mem_type (int type, size_t size);

void* sgen_alloc_internal (int type);
void sgen_free_internal (void *addr, int type);

void* sgen_alloc_internal_dynamic (size_t size, int type, gboolean assert_on_failure);
void sgen_free_internal_dynamic (void *addr, size_t size, int type);

#ifndef DISABLE_SGEN_DEBUG_HELPERS
void sgen_pin_stats_enable (void);
void sgen_pin_stats_register_object (GCObject *obj, int generation);
void sgen_pin_stats_register_global_remset (GCObject *obj);
void sgen_pin_stats_report (void);
#else
static inline void sgen_pin_stats_enable (void) { }
static inline void sgen_pin_stats_register_object (GCObject *obj, int generation) { }
static inline void sgen_pin_stats_register_global_remset (GCObject *obj) { }
static inline void sgen_pin_stats_report (void) { }
#endif

#ifndef DISABLE_SGEN_DEBUG_HELPERS
void sgen_gchandle_stats_enable (void);
void sgen_gchandle_stats_report (void);
#else
static inline void sgen_gchandle_stats_enable (void) { }
static inline void sgen_gchandle_stats_report (void) { }
#endif

void sgen_sort_addresses (void **array, size_t size);
void sgen_add_to_global_remset (gpointer ptr, GCObject *obj);

int sgen_get_current_collection_generation (void);
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
gboolean sgen_collection_is_concurrent (void);
gboolean sgen_get_concurrent_collection_in_progress (void);
#else
#define sgen_collection_is_concurrent() FALSE
#define sgen_get_concurrent_collection_in_progress() FALSE
#endif

void sgen_set_bytes_allocated_attached (guint64 bytes);
void sgen_increment_bytes_allocated_detached (guint64 bytes);

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
sgen_nursery_is_to_space (void *object)
{
	size_t idx = ((char*)object - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	size_t byte = idx >> 3;
	size_t bit = idx & 0x7;

	SGEN_ASSERT (4, sgen_ptr_in_nursery (object), "object %p is not in nursery [%p - %p]", object, sgen_get_nursery_start (), sgen_get_nursery_end ());
	SGEN_ASSERT (4, byte < sgen_space_bitmap_size, "byte index %" G_GSIZE_FORMAT "d out of range (%" G_GSIZE_FORMAT "d)", byte, sgen_space_bitmap_size);

	return (sgen_space_bitmap [byte] & (1 << bit)) != 0;
}

static inline gboolean
sgen_nursery_is_from_space (void *object)
{
	return !sgen_nursery_is_to_space (object);
}

static inline gboolean
sgen_nursery_is_object_alive (GCObject *obj)
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
	gboolean is_parallel;

	GCObject* (*alloc_for_promotion) (GCVTable vtable, GCObject *obj, size_t objsize, gboolean has_references);
	GCObject* (*alloc_for_promotion_par) (GCVTable vtable, GCObject *obj, size_t objsize, gboolean has_references);

	SgenObjectOperations serial_ops;
	SgenObjectOperations serial_ops_with_concurrent_major;
	SgenObjectOperations parallel_ops;
	SgenObjectOperations parallel_ops_with_concurrent_major;

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

void sgen_simple_nursery_init (SgenMinorCollector *collector, gboolean parallel);
void sgen_split_nursery_init (SgenMinorCollector *collector);

/* Updating references */

#ifdef SGEN_CHECK_UPDATE_REFERENCE
gboolean sgen_thread_pool_is_thread_pool_thread (MonoNativeThreadId some_thread);

static inline void
sgen_update_reference (GCObject **p, GCObject *o, gboolean allow_null)
{
	if (!allow_null)
		SGEN_ASSERT (0, o, "Cannot update a reference with a NULL pointer");
	SGEN_ASSERT (0, !sgen_workers_is_worker_thread (mono_native_thread_id_get ()), "Can't update a reference in the worker thread");
	*p = o;
}

#define SGEN_UPDATE_REFERENCE_ALLOW_NULL(p,o)	sgen_update_reference ((GCObject**)(p), (GCObject*)(o), TRUE)
#define SGEN_UPDATE_REFERENCE(p,o)		sgen_update_reference ((GCObject**)(p), (GCObject*)(o), FALSE)
#else
#define SGEN_UPDATE_REFERENCE_ALLOW_NULL(p,o)	(*(GCObject**)(p) = (GCObject*)(o))
#define SGEN_UPDATE_REFERENCE(p,o)		SGEN_UPDATE_REFERENCE_ALLOW_NULL ((p), (o))
#endif

/* Major collector */

typedef void (*sgen_cardtable_block_callback) (mword start, mword size);
void sgen_major_collector_iterate_live_block_ranges (sgen_cardtable_block_callback callback);
void sgen_major_collector_iterate_block_ranges (sgen_cardtable_block_callback callback);

void sgen_iterate_all_block_ranges (sgen_cardtable_block_callback callback, gboolean is_parallel);

typedef enum {
	ITERATE_OBJECTS_SWEEP = 1,
	ITERATE_OBJECTS_NON_PINNED = 2,
	ITERATE_OBJECTS_PINNED = 4,
	ITERATE_OBJECTS_SWEEP_NON_PINNED = ITERATE_OBJECTS_SWEEP | ITERATE_OBJECTS_NON_PINNED,
	ITERATE_OBJECTS_SWEEP_PINNED = ITERATE_OBJECTS_SWEEP | ITERATE_OBJECTS_PINNED,
	ITERATE_OBJECTS_SWEEP_ALL = ITERATE_OBJECTS_SWEEP | ITERATE_OBJECTS_NON_PINNED | ITERATE_OBJECTS_PINNED
} IterateObjectsFlags;

typedef struct
{
	size_t num_scanned_objects;
	size_t num_unique_scanned_objects;
} ScannedObjectCounts;

typedef enum {
	CARDTABLE_SCAN_GLOBAL = 0,
	CARDTABLE_SCAN_MOD_UNION = 1,
	CARDTABLE_SCAN_MOD_UNION_PRECLEAN = CARDTABLE_SCAN_MOD_UNION | 2,
} CardTableScanType;

typedef struct _SgenMajorCollector SgenMajorCollector;
struct _SgenMajorCollector {
	size_t section_size;
	gboolean is_concurrent;
	gboolean is_parallel;
	gboolean supports_cardtable;
	gboolean sweeps_lazily;

	void* (*alloc_heap) (mword nursery_size, mword nursery_align);
	gboolean (*is_object_live) (GCObject *obj);
	GCObject* (*alloc_small_pinned_obj) (GCVTable vtable, size_t size, gboolean has_references);
	GCObject* (*alloc_degraded) (GCVTable vtable, size_t size);

	SgenObjectOperations major_ops_serial;
	SgenObjectOperations major_ops_concurrent_start;
	SgenObjectOperations major_ops_concurrent_finish;
	SgenObjectOperations major_ops_conc_par_start;
	SgenObjectOperations major_ops_conc_par_finish;

	GCObject* (*alloc_object) (GCVTable vtable, size_t size, gboolean has_references);
	GCObject* (*alloc_object_par) (GCVTable vtable, size_t size, gboolean has_references);
	void (*free_pinned_object) (GCObject *obj, size_t size);

	/*
	 * This is used for domain unloading, heap walking from the logging profiler, and
	 * debugging.  Can assume the world is stopped.
	 */
	void (*iterate_objects) (IterateObjectsFlags flags, IterateObjectCallbackFunc callback, void *data);

	void (*free_non_pinned_object) (GCObject *obj, size_t size);
	void (*pin_objects) (SgenGrayQueue *queue);
	void (*pin_major_object) (GCObject *obj, SgenGrayQueue *queue);
	void (*scan_card_table) (CardTableScanType scan_type, ScanCopyContext ctx, int job_index, int job_split_count, int block_count);
	void (*iterate_live_block_ranges) (sgen_cardtable_block_callback callback);
	void (*iterate_block_ranges) (sgen_cardtable_block_callback callback);
	void (*iterate_block_ranges_in_parallel) (sgen_cardtable_block_callback callback, int job_index, int job_split_count, int block_count);
	void (*update_cardtable_mod_union) (void);
	void (*init_to_space) (void);
	void (*sweep) (void);
	gboolean (*have_swept) (void);
	void (*finish_sweeping) (void);
	void (*free_swept_blocks) (size_t section_reserve);
	void (*check_scan_starts) (void);
	void (*dump_heap) (FILE *heap_dump_file);
	gint64 (*get_used_size) (void);
	void (*start_nursery_collection) (void);
	void (*finish_nursery_collection) (void);
	void (*start_major_collection) (void);
	void (*finish_major_collection) (ScannedObjectCounts *counts);
	gboolean (*ptr_is_in_non_pinned_space) (char *ptr, char **start);
	gboolean (*ptr_is_from_pinned_alloc) (char *ptr);
	void (*report_pinned_memory_usage) (void);
	size_t (*get_num_major_sections) (void);
	size_t (*get_bytes_survived_last_sweep) (void);
	gboolean (*handle_gc_param) (const char *opt);
	void (*print_gc_param_usage) (void);
	void (*post_param_init) (SgenMajorCollector *collector);
	gboolean (*is_valid_object) (char *ptr);
	GCVTable (*describe_pointer) (char *pointer);
	guint8* (*get_cardtable_mod_union_for_reference) (char *object);
	long long (*get_and_reset_num_major_objects_marked) (void);
	void (*count_cards) (long long *num_total_cards, long long *num_marked_cards);
	void (*init_block_free_lists) (gpointer *list_p);
};

extern SgenMajorCollector sgen_major_collector;

void sgen_marksweep_init (SgenMajorCollector *collector);
void sgen_marksweep_conc_init (SgenMajorCollector *collector);
void sgen_marksweep_conc_par_init (SgenMajorCollector *collector);
SgenMajorCollector* sgen_get_major_collector (void);
SgenMinorCollector* sgen_get_minor_collector (void);


typedef struct _SgenRememberedSet {
	void (*wbarrier_set_field) (GCObject *obj, gpointer field_ptr, GCObject* value);
	void (*wbarrier_arrayref_copy) (gpointer dest_ptr, gconstpointer src_ptr, int count);
	void (*wbarrier_value_copy) (gpointer dest, gconstpointer src, int count, size_t element_size);
	void (*wbarrier_object_copy) (GCObject* obj, GCObject *src);
	void (*wbarrier_generic_nostore) (gpointer ptr);
	void (*record_pointer) (gpointer ptr);
	void (*wbarrier_range_copy) (gpointer dest, gconstpointer src, int count);

	void (*start_scan_remsets) (gboolean is_parallel);

	void (*clear_cards) (void);

	gboolean (*find_address) (char *addr);
	gboolean (*find_address_with_cards) (char *cards_start, guint8 *cards, char *addr);
} SgenRememberedSet;

typedef struct _SgenGCInfo {
	guint64 fragmented_bytes;
	guint64 heap_size_bytes;
	guint64 high_memory_load_threshold_bytes;
	guint64 memory_load_bytes;
	guint64 total_available_memory_bytes;
} SgenGCInfo;

extern SgenGCInfo sgen_gc_info;

SgenRememberedSet *sgen_get_remset (void);

/*
 * These must be kept in sync with object.h.  They're here for using SGen independently of
 * Mono.
 */
void mono_gc_wbarrier_arrayref_copy (gpointer dest_ptr, /*const*/ void* src_ptr, int count);
void mono_gc_wbarrier_generic_nostore (gpointer ptr);
void mono_gc_wbarrier_generic_store (gpointer ptr, GCObject* value);
void mono_gc_wbarrier_generic_store_atomic (gpointer ptr, GCObject *value);

void sgen_wbarrier_range_copy (gpointer _dest, gconstpointer _src, int size);

static inline SgenDescriptor
sgen_obj_get_descriptor (GCObject *obj)
{
	GCVTable vtable = SGEN_LOAD_VTABLE_UNCHECKED (obj);
	SGEN_ASSERT (9, !SGEN_POINTER_IS_TAGGED_ANY (vtable), "Object can't be tagged");
	return sgen_vtable_get_descriptor (vtable);
}

static inline SgenDescriptor
sgen_obj_get_descriptor_safe (GCObject *obj)
{
	GCVTable vtable = SGEN_LOAD_VTABLE (obj);
	return sgen_vtable_get_descriptor (vtable);
}

static mword sgen_client_par_object_get_size (GCVTable vtable, GCObject* o);
static mword sgen_client_slow_object_get_size (GCVTable vtable, GCObject* o);

static inline mword
sgen_safe_object_get_size (GCObject *obj)
{
	GCObject *forwarded;
	GCVTable vtable = SGEN_LOAD_VTABLE_UNCHECKED (obj);

	/*
	 * Once we load the vtable, we must always use it, in case we are in parallel case.
	 * Otherwise the object might get forwarded in the meantime and we would read an
	 * invalid vtable. An object cannot be forwarded for a second time during same GC.
	 */
	if ((forwarded = SGEN_VTABLE_IS_FORWARDED (vtable)))
		return sgen_client_par_object_get_size (SGEN_LOAD_VTABLE (forwarded), obj);
	else
		return sgen_client_par_object_get_size ((GCVTable)SGEN_POINTER_UNTAG_ALL (vtable), obj);
}

static inline gboolean
sgen_safe_object_is_small (GCObject *obj, int type)
{
	if (type <= DESC_TYPE_MAX_SMALL_OBJ)
		return TRUE;
	return SGEN_ALIGN_UP (sgen_safe_object_get_size ((GCObject*)obj)) <= SGEN_MAX_SMALL_OBJ_SIZE;
}

/*
 * This variant guarantees to return the exact size of the object
 * before alignment. Needed for canary support.
 */
static inline guint
sgen_safe_object_get_size_unaligned (GCObject *obj)
{
	GCObject *forwarded;

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
		obj = (GCObject*)forwarded;
	}

	return sgen_client_slow_object_get_size (SGEN_LOAD_VTABLE (obj), obj);
}

#ifdef SGEN_CLIENT_HEADER
#include SGEN_CLIENT_HEADER
#else
#include "metadata/sgen-client-mono.h"
#endif

gboolean sgen_object_is_live (GCObject *obj);

void  sgen_init_fin_weak_hash (void);

void sgen_register_obj_with_weak_fields (GCObject *obj);

/* FIXME: move the toggleref stuff out of here */
void sgen_mark_togglerefs (char *start, char *end, ScanCopyContext ctx);
void sgen_clear_togglerefs (char *start, char *end, ScanCopyContext ctx);

#ifndef DISABLE_SGEN_TOGGLEREF
void sgen_process_togglerefs (void);
void sgen_register_test_toggleref_callback (void);
#else
static inline void sgen_process_togglerefs (void) { }
static inline void sgen_register_test_toggleref_callback (void) { }
#endif


void sgen_mark_bridge_object (GCObject *obj)
	MONO_PERMIT (need (sgen_gc_locked));
void sgen_collect_bridge_objects (int generation, ScanCopyContext ctx)
	MONO_PERMIT (need (sgen_gc_locked));

typedef gboolean (*SgenObjectPredicateFunc) (GCObject *obj, void *user_data);

void sgen_null_links_if (SgenObjectPredicateFunc predicate, void *data, int generation, gboolean track)
	MONO_PERMIT (need (sgen_gc_locked, sgen_world_stopped));

gboolean sgen_gc_is_object_ready_for_finalization (GCObject *object);
void sgen_gc_lock (void) MONO_PERMIT (use (sgen_lock_gc), grant (sgen_gc_locked), revoke (sgen_lock_gc));
void sgen_gc_unlock (void) MONO_PERMIT (use (sgen_gc_locked), revoke (sgen_gc_locked), grant (sgen_lock_gc));

void sgen_queue_finalization_entry (GCObject *obj);
const char* sgen_generation_name (int generation);

void sgen_finalize_in_range (int generation, ScanCopyContext ctx)
	MONO_PERMIT (need (sgen_gc_locked));
void sgen_null_link_in_range (int generation, ScanCopyContext ctx, gboolean track)
	MONO_PERMIT (need (sgen_gc_locked, sgen_world_stopped));
void sgen_process_fin_stage_entries (void)
	MONO_PERMIT (need (sgen_gc_locked));
gboolean sgen_have_pending_finalizers (void);

typedef void (*SGenFinalizationProc)(gpointer, gpointer); // same as MonoFinalizationProc, GC_finalization_proc

void sgen_object_register_for_finalization (GCObject *obj, SGenFinalizationProc user_data)
	MONO_PERMIT (need (sgen_lock_gc));

void sgen_finalize_if (SgenObjectPredicateFunc predicate, void *user_data)
	MONO_PERMIT (need (sgen_lock_gc));
void sgen_remove_finalizers_if (SgenObjectPredicateFunc predicate, void *user_data, int generation);
void sgen_set_suspend_finalizers (void);

void sgen_wbroots_iterate_live_block_ranges (sgen_cardtable_block_callback cb);
void sgen_wbroots_scan_card_table (ScanCopyContext ctx);

void sgen_register_disappearing_link (GCObject *obj, void **link, gboolean track, gboolean in_gc);

GCObject* sgen_weak_link_get (void **link_addr);

gboolean sgen_drain_gray_stack (ScanCopyContext ctx);

enum {
	SPACE_NURSERY,
	SPACE_MAJOR,
	SPACE_LOS
};

void sgen_pin_object (GCObject *object, SgenGrayQueue *queue);
void sgen_set_pinned_from_failed_allocation (mword objsize);

void sgen_ensure_free_space (size_t size, int generation)
	MONO_PERMIT (need (sgen_gc_locked, sgen_stop_world));
void sgen_gc_collect (int generation)
	MONO_PERMIT (need (sgen_lock_gc, sgen_stop_world));
void sgen_perform_collection (size_t requested_size, int generation_to_collect, const char *reason, gboolean wait_to_finish, gboolean stw)
	MONO_PERMIT (need (sgen_gc_locked, sgen_stop_world));

int sgen_gc_collection_count (int generation);
/* FIXME: what exactly does this return? */
size_t sgen_gc_get_used_size (void)
	MONO_PERMIT (need (sgen_lock_gc));
size_t sgen_gc_get_total_heap_allocation (void);

/* STW */

void sgen_stop_world (int generation, gboolean serial_collection)
	MONO_PERMIT (need (sgen_gc_locked), use (sgen_stop_world), grant (sgen_world_stopped), revoke (sgen_stop_world));
void sgen_restart_world (int generation, gboolean serial_collection)
	MONO_PERMIT (need (sgen_gc_locked), use (sgen_world_stopped), revoke (sgen_world_stopped), grant (sgen_stop_world));
gboolean sgen_is_world_stopped (void);

gboolean sgen_set_allow_synchronous_major (gboolean flag);

/* LOS */

typedef struct _LOSObject LOSObject;
struct _LOSObject {
	mword size; /* this is the object size, lowest bit used for pin/mark */
	guint8 * volatile cardtable_mod_union; /* only used by the concurrent collector */
	GCObject data [MONO_ZERO_LEN_ARRAY];
};

extern mword sgen_los_memory_usage;
extern mword sgen_los_memory_usage_total;

void sgen_los_free_object (LOSObject *obj);
void* sgen_los_alloc_large_inner (GCVTable vtable, size_t size)
	MONO_PERMIT (need (sgen_gc_locked, sgen_stop_world));
void sgen_los_sweep (void);
gboolean sgen_ptr_is_in_los (char *ptr, char **start);
void sgen_los_iterate_objects (IterateObjectCallbackFunc cb, void *user_data);
void sgen_los_iterate_objects_free (IterateObjectResultCallbackFunc cb, void *user_data);
void sgen_los_iterate_live_block_ranges (sgen_cardtable_block_callback callback);
void sgen_los_iterate_live_block_range_jobs (sgen_cardtable_block_callback callback, int job_index, int job_split_count);
void sgen_los_scan_card_table (CardTableScanType scan_type, ScanCopyContext ctx, int job_index, int job_split_count);
void sgen_los_update_cardtable_mod_union (void);
void sgen_los_count_cards (long long *num_total_cards, long long *num_marked_cards);
gboolean sgen_los_is_valid_object (char *object);
gboolean mono_sgen_los_describe_pointer (char *ptr);
LOSObject* sgen_los_header_for_object (GCObject *data);
mword sgen_los_object_size (LOSObject *obj);
void sgen_los_pin_object (GCObject *obj);
void sgen_los_pin_objects (SgenGrayQueue *gray_queue, gboolean finish_concurrent_mode);
gboolean sgen_los_pin_object_par (GCObject *obj);
gboolean sgen_los_object_is_pinned (GCObject *obj);
void sgen_los_mark_mod_union_card (GCObject *mono_obj, void **ptr);


/* nursery allocator */

void sgen_clear_nursery_fragments (void);
void sgen_nursery_allocator_prepare_for_pinning (void);
void sgen_nursery_allocator_set_nursery_bounds (char *nursery_start, size_t min_size, size_t max_size);
void sgen_resize_nursery (gboolean need_shrink);
mword sgen_build_nursery_fragments (GCMemSection *nursery_section);
void sgen_init_nursery_allocator (void);
void sgen_nursery_allocator_init_heavy_stats (void);
void sgen_init_allocator (void);
void* sgen_nursery_alloc (size_t size);
void* sgen_nursery_alloc_range (size_t size, size_t min_size, size_t *out_alloc_size);
gboolean sgen_can_alloc_size (size_t size);
void sgen_nursery_retire_region (void *address, ptrdiff_t size);

void sgen_nursery_alloc_prepare_for_minor (void);
void sgen_nursery_alloc_prepare_for_major (void);

GCObject* sgen_alloc_for_promotion (GCObject *obj, size_t objsize, gboolean has_references);

GCObject* sgen_alloc_obj_nolock (GCVTable vtable, size_t size)
	MONO_PERMIT (need (sgen_gc_locked, sgen_stop_world));
GCObject* sgen_try_alloc_obj_nolock (GCVTable vtable, size_t size);

/* Threads */

void* sgen_thread_attach (SgenThreadInfo* info);
void sgen_thread_detach_with_lock (SgenThreadInfo *p);

/* Finalization/ephemeron support */

static inline gboolean
sgen_major_is_object_alive (GCObject *object)
{
	mword objsize;

	/* Oldgen objects can be pinned and forwarded too */
	if (SGEN_OBJECT_IS_PINNED (object) || SGEN_OBJECT_IS_FORWARDED (object))
		return TRUE;

	/*
	 * FIXME: major_collector.is_object_live() also calculates the
	 * size.  Avoid the double calculation.
	 */
	objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size (object));
	if (objsize > SGEN_MAX_SMALL_OBJ_SIZE)
		return sgen_los_object_is_pinned (object);

	return sgen_major_collector.is_object_live (object);
}


/*
 * If the object has been forwarded it means it's still referenced from a root. 
 * If it is pinned it's still alive as well.
 * A LOS object is only alive if we have pinned it.
 * Return TRUE if @obj is ready to be finalized.
 */
static inline gboolean
sgen_is_object_alive (GCObject *object)
{
	if (sgen_ptr_in_nursery (object))
		return sgen_nursery_is_object_alive (object);

	return sgen_major_is_object_alive (object);
}


/*
 * This function returns true if @object is either alive or it belongs to the old gen
 * and we're currently doing a minor collection.
 */
static inline int
sgen_is_object_alive_for_current_gen (GCObject *object)
{
	if (sgen_ptr_in_nursery (object))
		return sgen_nursery_is_object_alive (object);

	if (sgen_current_collection_generation == GENERATION_NURSERY)
		return TRUE;

	return sgen_major_is_object_alive (object);
}

int sgen_gc_invoke_finalizers (void)
	MONO_PERMIT (need (sgen_lock_gc));

/* GC handles */

void sgen_init_gchandles (void);

void sgen_null_links_if (SgenObjectPredicateFunc predicate, void *data, int generation, gboolean track)
	MONO_PERMIT (need (sgen_gc_locked));

typedef gpointer (*SgenGCHandleIterateCallback) (gpointer hidden, GCHandleType handle_type, int max_generation, gpointer user);

guint32 sgen_gchandle_new (GCObject *obj, gboolean pinned);
guint32 sgen_gchandle_new_weakref (GCObject *obj, gboolean track_resurrection);
void sgen_gchandle_iterate (GCHandleType handle_type, int max_generation, SgenGCHandleIterateCallback callback, gpointer user)
	MONO_PERMIT (need (sgen_world_stopped));
void sgen_gchandle_set_target (guint32 gchandle, GCObject *obj);
void sgen_mark_normal_gc_handles (void *addr, SgenUserMarkFunc mark_func, void *gc_data)
	MONO_PERMIT (need (sgen_world_stopped));
void sgen_gc_handles_report_roots (SgenUserReportRootFunc mark_func, void *gc_data)
	MONO_PERMIT (need (sgen_world_stopped));
gpointer sgen_gchandle_get_metadata (guint32 gchandle);
GCObject *sgen_gchandle_get_target (guint32 gchandle);
void sgen_gchandle_free (guint32 gchandle);

/* Other globals */

extern GCMemSection *sgen_nursery_section;
extern guint32 sgen_collect_before_allocs;
extern guint32 sgen_verify_before_allocs;
extern gboolean sgen_has_per_allocation_action;
extern size_t sgen_degraded_mode;
extern int default_nursery_size;
extern guint32 sgen_tlab_size;
extern NurseryClearPolicy sgen_nursery_clear_policy;
extern gboolean sgen_try_free_some_memory;
extern mword sgen_total_promoted_size;
extern mword sgen_total_allocated_major;
extern volatile gboolean sgen_suspend_finalizers;
extern MonoCoopMutex sgen_gc_mutex;
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
extern volatile gboolean sgen_concurrent_collection_in_progress;
#else
static const gboolean sgen_concurrent_collection_in_progress = FALSE;
#endif

/* Nursery helpers. */

static inline void
sgen_set_nursery_scan_start (char *p)
{
	size_t idx = (p - (char*)sgen_nursery_section->data) / SGEN_SCAN_START_SIZE;
	char *old = sgen_nursery_section->scan_starts [idx];
	if (!old || old > p)
		sgen_nursery_section->scan_starts [idx] = p;
}


/* Object Allocation */

typedef enum {
	ATYPE_NORMAL,
	ATYPE_VECTOR,
	ATYPE_SMALL,
	ATYPE_STRING,
	ATYPE_NUM
} SgenAllocatorType;

void sgen_clear_tlabs (void);
void sgen_update_allocation_count (void)
	MONO_PERMIT (need (sgen_world_stopped));
guint64 sgen_get_total_allocated_bytes (MonoBoolean precise)
	MONO_PERMIT (need (sgen_lock_gc, sgen_stop_world));

GCObject* sgen_alloc_obj (GCVTable vtable, size_t size)
	MONO_PERMIT (need (sgen_lock_gc, sgen_stop_world));
GCObject* sgen_alloc_obj_pinned (GCVTable vtable, size_t size)
	MONO_PERMIT (need (sgen_lock_gc, sgen_stop_world));
GCObject* sgen_alloc_obj_mature (GCVTable vtable, size_t size)
	MONO_PERMIT (need (sgen_lock_gc, sgen_stop_world));

/* Debug support */

void sgen_check_remset_consistency (void);
void sgen_check_mod_union_consistency (void);
void sgen_check_major_refs (void);
void sgen_check_whole_heap (gboolean allow_missing_pinning);
void sgen_check_whole_heap_stw (void)
	MONO_PERMIT (need (sgen_gc_locked, sgen_stop_world));
void sgen_check_objref (char *obj);
void sgen_check_heap_marked (gboolean nursery_must_be_pinned);
void sgen_check_nursery_objects_untag (void);
void sgen_check_for_xdomain_refs (void);
GCObject* sgen_find_object_for_ptr (char *ptr);

void mono_gc_scan_for_specific_ref (GCObject *key, gboolean precise);

void sgen_debug_enable_heap_dump (const char *filename);
void sgen_debug_dump_heap (const char *type, int num, const char *reason);

void sgen_debug_verify_nursery (gboolean do_dump_nursery_content);
void sgen_debug_check_nursery_is_clean (void);

/* Environment variable parsing */

#define MONO_GC_PARAMS_NAME	"MONO_GC_PARAMS"
#define MONO_GC_DEBUG_NAME	"MONO_GC_DEBUG"

void sgen_env_var_error (const char *env_var, const char *fallback, const char *description_format, ...);

/* Utilities */

void sgen_qsort (void *const array, const size_t count, const size_t element_size, int (*compare) (const void*, const void*));
gint64 sgen_timestamp (void);

/*
 * Canary (guard word) support
 * Notes:
 * - CANARY_SIZE must be multiple of word size in bytes
 * - Canary space is not included on checks against SGEN_MAX_SMALL_OBJ_SIZE
 */
 
gboolean sgen_nursery_canaries_enabled (void);

#define CANARY_SIZE 8
#define CANARY_STRING  "koupepia"

#define CANARIFY_SIZE(size) if (sgen_nursery_canaries_enabled ()) {	\
			size = size + CANARY_SIZE;	\
		}

#define CANARIFY_ALLOC(addr,size) if (sgen_nursery_canaries_enabled ()) {	\
				memcpy ((char*) (addr) + (size), CANARY_STRING, CANARY_SIZE);	\
			}

#define CANARY_VALID(addr) (strncmp ((char*) (addr), CANARY_STRING, CANARY_SIZE) == 0)

void
sgen_check_canary_for_object (gpointer addr);

guint64 sgen_get_precise_allocation_count (void);

#define CHECK_CANARY_FOR_OBJECT(addr, ignored) \
	(sgen_nursery_canaries_enabled () ? sgen_check_canary_for_object (addr) : (void)0)

/*
 * This causes the compile to extend the liveness of 'v' till the call to dummy_use
 */
static inline void
sgen_dummy_use (gpointer v)
{
#if defined(_MSC_VER) || defined(HOST_WASM)
	static volatile gpointer ptr;
	ptr = v;
#elif defined(__GNUC__)
	__asm__ volatile ("" : "=r"(v) : "r"(v));
#else
#error "Implement sgen_dummy_use for your compiler"
#endif
}

#endif /* HAVE_SGEN_GC */

#endif /* __MONO_SGENGC_H__ */
