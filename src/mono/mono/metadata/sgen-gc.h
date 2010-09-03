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
#include <mono/metadata/object-internals.h>

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

#define SGEN_HAVE_CARDTABLE	1
#if SIZEOF_VOID_P == 8
#define SGEN_HAVE_OVERLAPPING_CARDS	1
#endif

#define SGEN_MAX_DEBUG_LEVEL 2

#define THREAD_HASH_SIZE 11

#define GC_BITS_PER_WORD (sizeof (mword) * 8)

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

#if defined(__APPLE__) || defined(__OpenBSD__)
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

#define SGEN_CAS_PTR	InterlockedCompareExchangePointer
#define SGEN_ATOMIC_ADD(x,i)	do {					\
		int __old_x;						\
		do {							\
			__old_x = (x);					\
		} while (InterlockedCompareExchange (&(x), __old_x + (i), __old_x) != __old_x); \
	} while (0)

/* non-pthread will need to provide their own version of start/stop */
#define USE_SIGNAL_BASED_START_STOP_WORLD 1
/* we intercept pthread_create calls to know which threads exist */
#define USE_PTHREAD_INTERCEPT 1

#ifdef HEAVY_STATISTICS
#define HEAVY_STAT(x)	x

extern long long stat_objects_alloced_degraded;
extern long long stat_bytes_alloced_degraded;
extern long long stat_copy_object_called_major;
extern long long stat_objects_copied_major;
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

/*
 * ######################################################################
 * ########  GC descriptors
 * ######################################################################
 * Used to quickly get the info the GC needs about an object: size and
 * where the references are held.
 */
#define OBJECT_HEADER_WORDS (sizeof(MonoObject)/sizeof(gpointer))
#define LOW_TYPE_BITS 3
#define SMALL_BITMAP_SHIFT 16
#define SMALL_BITMAP_SIZE (GC_BITS_PER_WORD - SMALL_BITMAP_SHIFT)
#define VECTOR_INFO_SHIFT 14
#define VECTOR_ELSIZE_SHIFT 3
#define LARGE_BITMAP_SIZE (GC_BITS_PER_WORD - LOW_TYPE_BITS)
#define MAX_ELEMENT_SIZE 0x3ff
#define VECTOR_SUBTYPE_PTRFREE (DESC_TYPE_V_PTRFREE << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_REFS    (DESC_TYPE_V_REFS << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_RUN_LEN (DESC_TYPE_V_RUN_LEN << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_BITMAP  (DESC_TYPE_V_BITMAP << VECTOR_INFO_SHIFT)

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

/* helper macros to scan and traverse objects, macros because we resue them in many functions */
#define OBJ_RUN_LEN_SIZE(size,desc,obj) do { \
		(size) = ((desc) & 0xfff8) >> 1;	\
    } while (0)

#define OBJ_BITMAP_SIZE(size,desc,obj) do { \
		(size) = ((desc) & 0xfff8) >> 1;	\
    } while (0)

//#define PREFETCH(addr) __asm__ __volatile__ ("     prefetchnta     %0": : "m"(*(char *)(addr)))
#define PREFETCH(addr)

/* code using these macros must define a HANDLE_PTR(ptr) macro that does the work */
#define OBJ_RUN_LEN_FOREACH_PTR(desc,obj)	do {	\
		if ((desc) & 0xffff0000) {	\
			/* there are pointers */	\
			void **_objptr_end;	\
			void **_objptr = (void**)(obj);	\
			_objptr += ((desc) >> 16) & 0xff;	\
			_objptr_end = _objptr + (((desc) >> 24) & 0xff);	\
			while (_objptr < _objptr_end) {	\
				HANDLE_PTR (_objptr, (obj));	\
				_objptr++;	\
			}	\
		}	\
	} while (0)

/* a bitmap desc means that there are pointer references or we'd have
 * choosen run-length, instead: add an assert to check.
 */
#define OBJ_BITMAP_FOREACH_PTR(desc,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize _bmap = (desc) >> 16;	\
		_objptr += OBJECT_HEADER_WORDS;	\
		while (_bmap) {	\
			if ((_bmap & 1)) {	\
				HANDLE_PTR (_objptr, (obj));	\
			}	\
			_bmap >>= 1;	\
			++_objptr;	\
		}	\
	} while (0)

#define OBJ_LARGE_BITMAP_FOREACH_PTR(vt,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize _bmap = (vt)->desc >> LOW_TYPE_BITS;	\
		_objptr += OBJECT_HEADER_WORDS;	\
		while (_bmap) {	\
			if ((_bmap & 1)) {	\
				HANDLE_PTR (_objptr, (obj));	\
			}	\
			_bmap >>= 1;	\
			++_objptr;	\
		}	\
	} while (0)

gsize* mono_sgen_get_complex_descriptor (GCVTable *vt) MONO_INTERNAL;

#define OBJ_COMPLEX_FOREACH_PTR(vt,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize *bitmap_data = mono_sgen_get_complex_descriptor ((vt)); \
		int bwords = (*bitmap_data) - 1;	\
		void **start_run = _objptr;	\
		bitmap_data++;	\
		if (0) {	\
			MonoObject *myobj = (MonoObject*)obj;	\
			g_print ("found %d at %p (0x%zx): %s.%s\n", bwords, (obj), (vt)->desc, myobj->vtable->klass->name_space, myobj->vtable->klass->name);	\
		}	\
		while (bwords-- > 0) {	\
			gsize _bmap = *bitmap_data++;	\
			_objptr = start_run;	\
			/*g_print ("bitmap: 0x%x/%d at %p\n", _bmap, bwords, _objptr);*/	\
			while (_bmap) {	\
				if ((_bmap & 1)) {	\
					HANDLE_PTR (_objptr, (obj));	\
				}	\
				_bmap >>= 1;	\
				++_objptr;	\
			}	\
			start_run += GC_BITS_PER_WORD;	\
		}	\
	} while (0)

/* this one is untested */
#define OBJ_COMPLEX_ARR_FOREACH_PTR(vt,obj)	do {	\
		/* there are pointers */	\
		gsize *mbitmap_data = mono_sgen_get_complex_descriptor ((vt)); \
		int mbwords = (*mbitmap_data++) - 1;	\
		int el_size = mono_array_element_size (vt->klass);	\
		char *e_start = (char*)(obj) +  G_STRUCT_OFFSET (MonoArray, vector);	\
		char *e_end = e_start + el_size * mono_array_length_fast ((MonoArray*)(obj));	\
		if (0)							\
                        g_print ("found %d at %p (0x%zx): %s.%s\n", mbwords, (obj), (vt)->desc, vt->klass->name_space, vt->klass->name); \
		while (e_start < e_end) {	\
			void **_objptr = (void**)e_start;	\
			gsize *bitmap_data = mbitmap_data;	\
			unsigned int bwords = mbwords;	\
			while (bwords-- > 0) {	\
				gsize _bmap = *bitmap_data++;	\
				void **start_run = _objptr;	\
				/*g_print ("bitmap: 0x%x\n", _bmap);*/	\
				while (_bmap) {	\
					if ((_bmap & 1)) {	\
						HANDLE_PTR (_objptr, (obj));	\
					}	\
					_bmap >>= 1;	\
					++_objptr;	\
				}	\
				_objptr = start_run + GC_BITS_PER_WORD;	\
			}	\
			e_start += el_size;	\
		}	\
	} while (0)

#define OBJ_VECTOR_FOREACH_PTR(vt,obj)	do {	\
		/* note: 0xffffc000 excludes DESC_TYPE_V_PTRFREE */	\
		if ((vt)->desc & 0xffffc000) {	\
			int el_size = ((vt)->desc >> 3) & MAX_ELEMENT_SIZE;	\
			/* there are pointers */	\
			int etype = (vt)->desc & 0xc000;	\
			if (etype == (DESC_TYPE_V_REFS << 14)) {	\
				void **p = (void**)((char*)(obj) + G_STRUCT_OFFSET (MonoArray, vector));	\
				void **end_refs = (void**)((char*)p + el_size * mono_array_length_fast ((MonoArray*)(obj)));	\
				/* Note: this code can handle also arrays of struct with only references in them */	\
				while (p < end_refs) {	\
					HANDLE_PTR (p, (obj));	\
					++p;	\
				}	\
			} else if (etype == DESC_TYPE_V_RUN_LEN << 14) {	\
				int offset = ((vt)->desc >> 16) & 0xff;	\
				int num_refs = ((vt)->desc >> 24) & 0xff;	\
				char *e_start = (char*)(obj) + G_STRUCT_OFFSET (MonoArray, vector);	\
				char *e_end = e_start + el_size * mono_array_length_fast ((MonoArray*)(obj));	\
				while (e_start < e_end) {	\
					void **p = (void**)e_start;	\
					int i;	\
					p += offset;	\
					for (i = 0; i < num_refs; ++i) {	\
						HANDLE_PTR (p + i, (obj));	\
					}	\
					e_start += el_size;	\
				}	\
			} else if (etype == DESC_TYPE_V_BITMAP << 14) {	\
				char *e_start = (char*)(obj) +  G_STRUCT_OFFSET (MonoArray, vector);	\
				char *e_end = e_start + el_size * mono_array_length_fast ((MonoArray*)(obj));	\
				while (e_start < e_end) {	\
					void **p = (void**)e_start;	\
					gsize _bmap = (vt)->desc >> 16;	\
					/* Note: there is no object header here to skip */	\
					while (_bmap) {	\
						if ((_bmap & 1)) {	\
							HANDLE_PTR (p, (obj));	\
						}	\
						_bmap >>= 1;	\
						++p;	\
					}	\
					e_start += el_size;	\
				}	\
			}	\
		}	\
	} while (0)

typedef struct _SgenInternalAllocator SgenInternalAllocator;

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

typedef void (*GrayQueueAllocPrepareFunc) (SgenGrayQueue*);

struct _SgenGrayQueue {
	SgenInternalAllocator *allocator;
	GrayQueueSection *first;
	GrayQueueSection *free_list;
	int balance;
	GrayQueueAllocPrepareFunc alloc_prepare_func;
	void *alloc_prepare_data;
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
	INTERNAL_MEM_WORKER_DATA,
	INTERNAL_MEM_MAX
};

#define SGEN_INTERNAL_FREELIST_NUM_SLOTS	30

struct _SgenInternalAllocator {
	SgenPinnedChunk *chunk_list;
	SgenPinnedChunk *free_lists [SGEN_INTERNAL_FREELIST_NUM_SLOTS];
	void *delayed_free_lists [SGEN_INTERNAL_FREELIST_NUM_SLOTS];
	long small_internal_mem_bytes [INTERNAL_MEM_MAX];
};

void mono_sgen_init_internal_allocator (void) MONO_INTERNAL;

SgenInternalAllocator* mono_sgen_get_unmanaged_allocator (void) MONO_INTERNAL;

const char* mono_sgen_internal_mem_type_name (int type) MONO_INTERNAL;
void mono_sgen_report_internal_mem_usage (void) MONO_INTERNAL;
void mono_sgen_report_internal_mem_usage_full (SgenInternalAllocator *alc) MONO_INTERNAL;
void mono_sgen_dump_internal_mem_usage (FILE *heap_dump_file) MONO_INTERNAL;
void mono_sgen_dump_section (GCMemSection *section, const char *type) MONO_INTERNAL;
void mono_sgen_dump_occupied (char *start, char *end, char *section_start) MONO_INTERNAL;

void mono_sgen_register_moved_object (void *obj, void *destination) MONO_INTERNAL;

void mono_sgen_register_fixed_internal_mem_type (int type, size_t size) MONO_INTERNAL;

void* mono_sgen_alloc_internal (int type) MONO_INTERNAL;
void mono_sgen_free_internal (void *addr, int type) MONO_INTERNAL;

void* mono_sgen_alloc_internal_dynamic (size_t size, int type) MONO_INTERNAL;
void mono_sgen_free_internal_dynamic (void *addr, size_t size, int type) MONO_INTERNAL;

void* mono_sgen_alloc_internal_fixed (SgenInternalAllocator *allocator, int type) MONO_INTERNAL;
void mono_sgen_free_internal_fixed (SgenInternalAllocator *allocator, void *addr, int type) MONO_INTERNAL;

void* mono_sgen_alloc_internal_full (SgenInternalAllocator *allocator, size_t size, int type) MONO_INTERNAL;
void mono_sgen_free_internal_full (SgenInternalAllocator *allocator, void *addr, size_t size, int type) MONO_INTERNAL;

void mono_sgen_free_internal_delayed (void *addr, int type, SgenInternalAllocator *thread_allocator) MONO_INTERNAL;

void mono_sgen_debug_printf (int level, const char *format, ...) MONO_INTERNAL;

gboolean mono_sgen_parse_environment_string_extract_number (const char *str, glong *out) MONO_INTERNAL;

void mono_sgen_internal_scan_objects (SgenInternalAllocator *alc, IterateObjectCallbackFunc callback, void *callback_data) MONO_INTERNAL;
void mono_sgen_internal_scan_pinned_objects (SgenInternalAllocator *alc, IterateObjectCallbackFunc callback, void *callback_data) MONO_INTERNAL;

void** mono_sgen_find_optimized_pin_queue_area (void *start, void *end, int *num) MONO_INTERNAL;
void mono_sgen_find_section_pin_queue_start_end (GCMemSection *section) MONO_INTERNAL;
void mono_sgen_pin_objects_in_section (GCMemSection *section, SgenGrayQueue *queue) MONO_INTERNAL;

void mono_sgen_pin_stats_register_object (char *obj, size_t size);

void mono_sgen_add_to_global_remset (gpointer ptr) MONO_INTERNAL;

#ifdef SGEN_HAVE_CARDTABLE
void sgen_card_table_reset_region (mword start, mword end) MONO_INTERNAL;
guint8* sgen_card_table_get_card_address (mword address) MONO_INTERNAL;
void* sgen_card_table_align_pointer (void *ptr) MONO_INTERNAL;
void sgen_card_table_mark_address (mword address) MONO_INTERNAL;
void sgen_card_table_mark_range (mword address, mword size) MONO_INTERNAL;
gboolean sgen_card_table_card_begin_scanning (mword address) MONO_INTERNAL;
void sgen_cardtable_scan_object (char *obj, mword obj_size, guint8 *cards, SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_card_table_get_card_data (guint8 *dest, mword address, mword cards) MONO_INTERNAL;
typedef void (*sgen_cardtable_block_callback) (mword start, mword size);

#define CARD_BITS 9
#define CARD_SIZE_IN_BYTES (1 << CARD_BITS)
#endif


typedef struct _SgenMajorCollector SgenMajorCollector;
struct _SgenMajorCollector {
	size_t section_size;
	gboolean is_parallel;
	gboolean supports_cardtable;

	void* (*alloc_heap) (mword nursery_size, mword nursery_align, int nursery_bits);
	gboolean (*is_object_live) (char *obj);
	void* (*alloc_small_pinned_obj) (size_t size, gboolean has_references);
	void* (*alloc_degraded) (MonoVTable *vtable, size_t size);
	void (*copy_or_mark_object) (void **obj_slot, SgenGrayQueue *queue);
	void (*minor_scan_object) (char *start, SgenGrayQueue *queue);
	char* (*minor_scan_vtype) (char *start, mword desc, char* from_start, char* from_end, SgenGrayQueue *queue);
	void (*major_scan_object) (char *start, SgenGrayQueue *queue);
	void (*copy_object) (void **obj_slot, SgenGrayQueue *queue);
	void* (*alloc_object) (int size, gboolean has_references);
	void (*free_pinned_object) (char *obj, size_t size);
	void (*iterate_objects) (gboolean non_pinned, gboolean pinned, IterateObjectCallbackFunc callback, void *data);
	void (*free_non_pinned_object) (char *obj, size_t size);
	void (*find_pin_queue_start_ends) (SgenGrayQueue *queue);
	void (*pin_objects) (SgenGrayQueue *queue);
	void (*scan_card_table) (SgenGrayQueue *queue);
	void (*iterate_live_block_ranges) (sgen_cardtable_block_callback);
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
	gboolean (*handle_gc_param) (const char *opt);
	void (*print_gc_param_usage) (void);
};

void mono_sgen_marksweep_init (SgenMajorCollector *collector) MONO_INTERNAL;
void mono_sgen_marksweep_fixed_init (SgenMajorCollector *collector) MONO_INTERNAL;
void mono_sgen_marksweep_par_init (SgenMajorCollector *collector) MONO_INTERNAL;
void mono_sgen_marksweep_fixed_par_init (SgenMajorCollector *collector) MONO_INTERNAL;
void mono_sgen_copying_init (SgenMajorCollector *collector) MONO_INTERNAL;

/*
 * This function can be called on an object whose first word, the
 * vtable field, is not intact.  This is necessary for the parallel
 * collector.
 */
static inline guint
mono_sgen_par_object_get_size (MonoVTable *vtable, MonoObject* o)
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

#define mono_sgen_safe_object_get_size(o)		mono_sgen_par_object_get_size ((MonoVTable*)SGEN_LOAD_VTABLE ((o)), (o))

#endif /* __MONO_SGENGC_H__ */
