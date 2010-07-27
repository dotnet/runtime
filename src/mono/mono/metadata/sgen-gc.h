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

/*
 * If this is set, the nursery is aligned to an address aligned to its size, ie.
 * a 1MB nursery will be aligned to an address divisible by 1MB. This allows us to
 * speed up ptr_in_nursery () checks which are very frequent. This requires the
 * nursery size to be a compile time constant.
 */
#define SGEN_ALIGN_NURSERY 1

//#define SGEN_BINARY_PROTOCOL

#define SGEN_MAX_DEBUG_LEVEL 2

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

/*
 * The nursery section and the major copying collector's sections use
 * this struct.
 */
typedef struct _GCMemSection GCMemSection;
struct _GCMemSection {
	SgenBlock block;
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
	gboolean is_to_space;
};

#define SGEN_SIZEOF_GC_MEM_SECTION	((sizeof (GCMemSection) + 7) & ~7)

/*
 * to quickly find the head of an object pinned by a conservative
 * address we keep track of the objects allocated for each
 * SGEN_SCAN_START_SIZE memory chunk in the nursery or other memory
 * sections. Larger values have less memory overhead and bigger
 * runtime cost. 4-8 KB are reasonable values.
 */
#define SGEN_SCAN_START_SIZE (4096*2)

/*
 * Objects bigger then this go into the large object space.  This size
 * has a few constraints.  It must fit into the major heap, which in
 * the case of the copying collector means that it must fit into a
 * pinned chunk.  It must also play well with the GC descriptors, some
 * of which (DESC_TYPE_RUN_LENGTH, DESC_TYPE_SMALL_BITMAP) encode the
 * object size.
 */
#define SGEN_MAX_SMALL_OBJ_SIZE 8000

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

#ifdef SGEN_ALIGN_NURSERY
#define SGEN_PTR_IN_NURSERY(p,bits,start,end)	(((mword)(p) & ~((1 << (bits)) - 1)) == (mword)(start))
#else
#define SGEN_PTR_IN_NURSERY(p,bits,start,end)	((char*)(p) >= (start) && (char*)(p) < (end))
#endif

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

/* objects are aligned to 8 bytes boundaries
 * A descriptor is a pointer in MonoVTable, so 32 or 64 bits of size.
 * The low 3 bits define the type of the descriptor. The other bits
 * depend on the type.
 * As a general rule the 13 remaining low bits define the size, either
 * of the whole object or of the elements in the arrays. While for objects
 * the size is already in bytes, for arrays we need to shift, because
 * array elements might be smaller than 8 bytes. In case of arrays, we
 * use two bits to describe what the additional high bits represents,
 * so the default behaviour can handle element sizes less than 2048 bytes.
 * The high 16 bits, if 0 it means the object is pointer-free.
 * This design should make it easy and fast to skip over ptr-free data.
 * The first 4 types should cover >95% of the objects.
 * Note that since the size of objects is limited to 64K, larger objects
 * will be allocated in the large object heap.
 * If we want 4-bytes alignment, we need to put vector and small bitmap
 * inside complex.
 */
enum {
	/*
	 * We don't use 0 so that 0 isn't a valid GC descriptor.  No
	 * deep reason for this other than to be able to identify a
	 * non-inited descriptor for debugging.
	 *
	 * If an object contains no references, its GC descriptor is
	 * always DESC_TYPE_RUN_LENGTH, without a size, no exceptions.
	 * This is so that we can quickly check for that in
	 * copy_object_no_checks(), without having to fetch the
	 * object's class.
	 */
	DESC_TYPE_RUN_LENGTH = 1, /* 15 bits aligned byte size | 1-3 (offset, numptr) bytes tuples */
	DESC_TYPE_SMALL_BITMAP, /* 15 bits aligned byte size | 16-48 bit bitmap */
	DESC_TYPE_COMPLEX,      /* index for bitmap into complex_descriptors */
	DESC_TYPE_VECTOR,       /* 10 bits element size | 1 bit array | 2 bits desc | element desc */
	DESC_TYPE_ARRAY,        /* 10 bits element size | 1 bit array | 2 bits desc | element desc */
	DESC_TYPE_LARGE_BITMAP, /* | 29-61 bitmap bits */
	DESC_TYPE_COMPLEX_ARR,  /* index for bitmap into complex_descriptors */
	/* subtypes for arrays and vectors */
	DESC_TYPE_V_PTRFREE = 0,/* there are no refs: keep first so it has a zero value  */
	DESC_TYPE_V_REFS,       /* all the array elements are refs */
	DESC_TYPE_V_RUN_LEN,    /* elements are run-length encoded as DESC_TYPE_RUN_LENGTH */
	DESC_TYPE_V_BITMAP      /* elements are as the bitmap in DESC_TYPE_SMALL_BITMAP */
};

#define SGEN_VTABLE_HAS_REFERENCES(vt)	(((MonoVTable*)(vt))->gc_descr != (void*)DESC_TYPE_RUN_LENGTH)

#define SGEN_GRAY_QUEUE_SECTION_SIZE	(128 - 3)

/*
 * This is a stack now instead of a queue, so the most recently added items are removed
 * first, improving cache locality, and keeping the stack size manageable.
 */
typedef struct _GrayQueueSection GrayQueueSection;
struct _GrayQueueSection {
	int end;
	GrayQueueSection *next;
	char *objects [SGEN_GRAY_QUEUE_SECTION_SIZE];
};

typedef struct _SgenGrayQueue SgenGrayQueue;
struct _SgenGrayQueue {
	GrayQueueSection *first;
	GrayQueueSection *free_list;
	int balance;
};

#if SGEN_MAX_DEBUG_LEVEL >= 9
#define GRAY_OBJECT_ENQUEUE gray_object_enqueue
#define GRAY_OBJECT_DEQUEUE(queue,o) ((o) = gray_object_dequeue ((queue)))
#else
#define GRAY_OBJECT_ENQUEUE(queue,o) do {				\
		if (G_UNLIKELY (!(queue)->first || (queue)->first->end == SGEN_GRAY_QUEUE_SECTION_SIZE)) \
			mono_sgen_gray_object_enqueue ((queue), (o));	\
		else							\
			(queue)->first->objects [(queue)->first->end++] = (o); \
	} while (0)
#define GRAY_OBJECT_DEQUEUE(queue,o) do {				\
		if (!(queue)->first)					\
			(o) = NULL;					\
		else if (G_UNLIKELY ((queue)->first->end == 1))		\
			(o) = mono_sgen_gray_object_dequeue ((queue));		\
		else							\
			(o) = (queue)->first->objects [--(queue)->first->end]; \
	} while (0)
#endif

void mono_sgen_gray_object_enqueue (SgenGrayQueue *queue, char *obj) MONO_INTERNAL;
char* mono_sgen_gray_object_dequeue (SgenGrayQueue *queue) MONO_INTERNAL;

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

void mono_sgen_register_major_sections_alloced (int num_sections) MONO_INTERNAL;
mword mono_sgen_get_minor_collection_allowance (void) MONO_INTERNAL;

void mono_sgen_scan_area_with_callback (char *start, char *end, IterateObjectCallbackFunc callback, void *data) MONO_INTERNAL;
void mono_sgen_check_section_scan_starts (GCMemSection *section) MONO_INTERNAL;

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
void mono_sgen_dump_section (GCMemSection *section, const char *type) MONO_INTERNAL;
void mono_sgen_dump_occupied (char *start, char *end, char *section_start) MONO_INTERNAL;

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
void mono_sgen_find_section_pin_queue_start_end (GCMemSection *section) MONO_INTERNAL;
void mono_sgen_pin_objects_in_section (GCMemSection *section, SgenGrayQueue *queue) MONO_INTERNAL;

void mono_sgen_pin_stats_register_object (char *obj, size_t size);

void* mono_sgen_copy_object_no_checks (void *obj, SgenGrayQueue *queue) MONO_INTERNAL;
void mono_sgen_par_copy_object_no_checks (char *destination, MonoVTable *vt, void *obj, mword objsize, SgenGrayQueue *queue) MONO_INTERNAL;

/* FIXME: this should be inlined */
guint mono_sgen_par_object_get_size (MonoVTable *vtable, MonoObject* o) MONO_INTERNAL;

#define mono_sgen_safe_object_get_size(o)		mono_sgen_par_object_get_size ((MonoVTable*)SGEN_LOAD_VTABLE ((o)), (o))

typedef struct _SgenMajorCollector SgenMajorCollector;
struct _SgenMajorCollector {
	size_t section_size;

	gboolean (*is_object_live) (char *obj);
	void* (*alloc_small_pinned_obj) (size_t size, gboolean has_references);
	void* (*alloc_degraded) (MonoVTable *vtable, size_t size);
	void (*copy_or_mark_object) (void **obj_slot, SgenGrayQueue *queue); /* FIXME: don't call this indirectly - make a major_scan_object instead */
	void* (*alloc_object) (int size, gboolean has_references); /* FIXME: don't call this indirectly, either */
	void (*free_pinned_object) (char *obj, size_t size);
	void (*iterate_objects) (gboolean non_pinned, gboolean pinned, IterateObjectCallbackFunc callback, void *data);
	void (*free_non_pinned_object) (char *obj, size_t size);
	void (*find_pin_queue_start_ends) (SgenGrayQueue *queue);
	void (*pin_objects) (SgenGrayQueue *queue);
	void (*init_to_space) (void);
	void (*sweep) (void);
	void (*check_scan_starts) (void);
	void (*dump_heap) (FILE *heap_dump_file);
	gint64 (*get_used_size) (void);
	void (*start_nursery_collection) (void);
	void (*finish_nursery_collection) (void);
	void (*finish_major_collection) (void);
	gboolean (*ptr_is_in_non_pinned_space) (char *ptr);
	gboolean (*obj_is_from_pinned_alloc) (char *obj);
	void (*report_pinned_memory_usage) (void);
	int (*get_num_major_sections) (void);
};

void mono_sgen_marksweep_init (SgenMajorCollector *collector, int nursery_bits, char *nursery_start, char *nursery_end) MONO_INTERNAL;
void mono_sgen_copying_init (SgenMajorCollector *collector, int the_nursery_bits, char *the_nursery_start, char *the_nursery_end) MONO_INTERNAL;

#endif /* __MONO_SGENGC_H__ */
