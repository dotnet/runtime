/*
 * sgen-gc.c: Simple generational GC.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *
 * Copyright (C) 2005-2006 Novell, Inc
 *
 * Thread start/stop adapted from Boehm's GC:
 * Copyright (c) 1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1998 by Fergus Henderson.  All rights reserved.
 * Copyright (c) 2000-2004 by Hewlett-Packard Company.  All rights reserved.
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
 * All the rest of the code is LGPL.
 *
 * Important: allocation provides always zeroed memory, having to do
 * a memset after allocation is deadly for performance.
 * Memory usage at startup is currently as follows:
 * 64 KB pinned space
 * 64 KB internal space
 * size of nursery
 * We should provide a small memory config with half the sizes
 *
 * We currently try to make as few mono assumptions as possible:
 * 1) 2-word header with no GC pointers in it (first vtable, second to store the
 *    forwarding ptr)
 * 2) gc descriptor is the second word in the vtable (first word in the class)
 * 3) 8 byte alignment is the minimum and enough (not true for special structures, FIXME)
 * 4) there is a function to get an object's size and the number of
 *    elements in an array.
 * 5) we know the special way bounds are allocated for complex arrays
 *
 * Always try to keep stack usage to a minimum: no recursive behaviour
 * and no large stack allocs.
 *
 * General description.
 * Objects are initially allocated in a nursery using a fast bump-pointer technique.
 * When the nursery is full we start a nursery collection: this is performed with a
 * copying GC.
 * When the old generation is full we start a copying GC of the old generation as well:
 * this will be changed to mark/compact in the future.
 * The things that complicate this description are:
 * *) pinned objects: we can't move them so we need to keep track of them
 * *) no precise info of the thread stacks and registers: we need to be able to
 *    quickly find the objects that may be referenced conservatively and pin them
 *    (this makes the first issues more important)
 * *) large objects are too expensive to be dealt with using copying GC: we handle them
 *    with mark/sweep during major collections
 * *) some objects need to not move even if they are small (interned strings, Type handles):
 *    we use mark/sweep for them, too: they are not allocated in the nursery, but inside
 *    PinnedChunks regions
 */

/*
 * TODO:
 *) change the jit to emit write barrier calls when needed (we
  can have specialized write barriers): done with icalls, still need to
  use some specialized barriers
 *) we could have a function pointer in MonoClass to implement
  customized write barriers for value types
 *) the write barrier code could be isolated in a couple of functions: when a
  thread is stopped if it's inside the barrier it is let go again
  until we stop outside of them (not really needed, see below GC-safe points)
 *) investigate the stuff needed to advance a thread to a GC-safe
  point (single-stepping, read from unmapped memory etc) and implement it
  Not needed yet: since we treat the objects reachable from the stack/regs as
  roots, we store the ptr and exec the write barrier so there is no race.
  We may need this to solve the issue with setting the length of arrays and strings.
  We may need this also for handling precise info on stacks, even simple things
  as having uninitialized data on the stack and having to wait for the prolog
  to zero it. Not an issue for the last frame that we scan conservatively.
  We could always not trust the value in the slots anyway.
 *) make the jit info table lock free
 *) modify the jit to save info about references in stack locations:
  this can be done just for locals as a start, so that at least
  part of the stack is handled precisely.
 *) Make the debug printf stuff thread and signal safe.
 *) test/fix 64 bit issues
 *) test/fix endianess issues
 *) port to non-Linux
 *) add batch moving profile info
 *) add more timing info
 *) there is a possible race when an array or string is created: the vtable is set,
    but the length is set only later so if the GC needs to scan the object in that window,
    it won't get the correct size for the object. The object can't have references and it will
    be pinned, but a free memory fragment may be created that overlaps with it.
    We should change the array max_length field to be at the same offset as the string length:
    this way we can have a single special alloc function for them that sets the length.
    Multi-dim arrays have the same issue for rank == 1 for the bounds data.
 *) implement a card table as the write barrier instead of remembered sets?
 *) some sort of blacklist support?
 *) fin_ready_list is part of the root set, too
 *) consider lowering the large object min size to 16/32KB or so and benchmark
 *) once mark-compact is implemented we could still keep the
    copying collector for the old generation and use it if we think
    it is better (small heaps and no pinning object in the old
    generation)
  *) avoid the memory store from copy_object when not needed.
  *) optimize the write barriers fastpath to happen in managed code
  *) add an option to mmap the whole heap in one chunk: it makes for many
     simplifications in the checks (put the nursery at the top and just use a single
     check for inclusion/exclusion): the issue this has is that on 32 bit systems it's
     not flexible (too much of the address space may be used by default or we can't
     increase the heap as needed) and we'd need a race-free mechanism to return memory
     back to the system (mprotect(PROT_NONE) will still keep the memory allocated if it
     was written to, munmap is needed, but the following mmap may not find the same segment
     free...)
   *) memzero the fragments after restarting the world and optionally a smaller chunk at a time
   *) an additional strategy to realloc/expand the nursery when fully pinned is to start
      allocating objects in the old generation. This means that we can't optimize away write
      barrier calls in ctors (but that is not valid for other reasons, too).
   *) add write barriers to the Clone methods
 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#include <unistd.h>
#include <stdio.h>
#include <string.h>
#include <semaphore.h>
#include <signal.h>
#include <errno.h>
#include <assert.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <sys/time.h>
#include <time.h>
#include <fcntl.h>
#include "metadata/metadata-internals.h"
#include "metadata/class-internals.h"
#include "metadata/gc-internal.h"
#include "metadata/object-internals.h"
#include "metadata/threads.h"
#include "metadata/sgen-gc.h"
#include "metadata/mono-gc.h"
#include "metadata/method-builder.h"
#include "metadata/profiler-private.h"
#include "utils/mono-mmap.h"

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LAST
};

#undef OPDEF

/*
 * ######################################################################
 * ########  Types and constants used by the GC.
 * ######################################################################
 */
#if SIZEOF_VOID_P == 4
typedef guint32 mword;
#else
typedef guint64 mword;
#endif

static int gc_initialized = 0;
static int gc_debug_level = 0;
static FILE* gc_debug_file;
/* If set, do a minor collection before every allocation */
static gboolean collect_before_allocs = FALSE;
/* If set, do a heap consistency check before each minor collection */
static gboolean consistency_check_at_minor_collection = FALSE;

void
mono_gc_flush_info (void)
{
	fflush (gc_debug_file);
}

#define MAX_DEBUG_LEVEL 9
#define DEBUG(level,a) do {if (G_UNLIKELY ((level) <= MAX_DEBUG_LEVEL && (level) <= gc_debug_level)) a;} while (0)

#define TV_DECLARE(name) struct timeval name
#define TV_GETTIME(tv) gettimeofday (&(tv), NULL)
#define TV_ELAPSED(start,end) (int)((((end).tv_sec - (start).tv_sec) * 1000000) + end.tv_usec - start.tv_usec)

#define GC_BITS_PER_WORD (sizeof (mword) * 8)

enum {
	MEMORY_ROLE_GEN0,
	MEMORY_ROLE_GEN1,
	MEMORY_ROLE_GEN2,
	MEMORY_ROLE_FIXED,
	MEMORY_ROLE_INTERNAL
};

/* each request from the OS ends up in a GCMemSection */
typedef struct _GCMemSection GCMemSection;
struct _GCMemSection {
	GCMemSection *next;
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
	int pin_queue_start;
	int pin_queue_end;
	unsigned short num_scan_start;
	unsigned char role;
};

/* large object space struct: 64+ KB */
/* we could make this limit much smaller to avoid memcpy copy
 * and potentially have more room in the GC descriptor: need to measure
 * This also means that such small OS objects will need to be
 * allocated in a different way (using pinned chunks).
 * We may want to put large but smaller than 64k objects in the fixed space
 * when we move the object from one generation to another (to limit the
 * pig in the snake effect).
 * Note: it may be worth to have an optimized copy function, since we can
 * assume that objects are aligned and have a multiple of 8 size.
 * FIXME: This structure needs to be a multiple of 8 bytes in size: this is not
 * true if MONO_ZERO_LEN_ARRAY is nonzero.
 */
typedef struct _LOSObject LOSObject;
struct _LOSObject {
	LOSObject *next;
	mword size; /* this is the object size */
	int dummy; /* to have a sizeof (LOSObject) a multiple of ALLOC_ALIGN  and data starting at same alignment */
	guint16 role;
	guint16 scanned;
	char data [MONO_ZERO_LEN_ARRAY];
};

/* Pinned objects are allocated in the LOS space if bigger than half a page
 * or from freelists otherwise. We assume that pinned objects are relatively few
 * and they have a slow dying speed (like interned strings, thread objects).
 * As such they will be collected only at major collections.
 * free lists are not global: when we need memory we allocate a PinnedChunk.
 * Each pinned chunk is made of several pages, the first of wich is used
 * internally for bookeeping (here think of a page as 4KB). The bookeeping
 * includes the freelists vectors and info about the object size of each page
 * in the pinned chunk. So, when needed, a free page is found in a pinned chunk,
 * a size is assigned to it, the page is divided in the proper chunks and each
 * chunk is added to the freelist. To not waste space, the remaining space in the
 * first page is used as objects of size 16 or 32 (need to measure which are more
 * common).
 * We use this same structure to allocate memory used internally by the GC, so
 * we never use malloc/free if we need to alloc during collection: the world is stopped
 * and malloc/free will deadlock.
 * When we want to iterate over pinned objects, we just scan a page at a time
 * linearly according to the size of objects in the page: the next pointer used to link
 * the items in the freelist uses the same word as the vtable. Since we keep freelists
 * for each pinned chunk, if the word points outside the pinned chunk it means
 * it is an object.
 * We could avoid this expensive scanning in creative ways. We could have a policy
 * of putting in the pinned space only objects we know about that have no struct fields
 * with references and we can easily use a even expensive write barrier for them,
 * since pointer writes on such objects should be rare.
 * The best compromise is to just alloc interned strings and System.MonoType in them.
 * It would be nice to allocate MonoThread in it, too: must check that we properly
 * use write barriers so we don't have to do any expensive scanning of the whole pinned
 * chunk list during minor collections. We can avoid it now because we alloc in it only
 * reference-free objects.
 */
#define PINNED_FIRST_SLOT_SIZE (sizeof (gpointer) * 4)
#define MAX_FREELIST_SIZE 2048
#define PINNED_PAGE_SIZE (4096)
#define PINNED_CHUNK_MIN_SIZE (4096*8)
typedef struct _PinnedChunk PinnedChunk;
struct _PinnedChunk {
	PinnedChunk *next;
	int num_pages;
	int *page_sizes; /* a 0 means the page is still unused */
	void **free_list;
	void *start_data;
	void *data [1]; /* page sizes and free lists are stored here */
};

/* The method used to clear the nursery */
/* Clearing at nursery collections is the safest, but has bad interactions with caches.
 * Clearing at TLAB creation is much faster, but more complex and it might expose hard
 * to find bugs.
 */
typedef enum {
	CLEAR_AT_GC,
	CLEAR_AT_TLAB_CREATION
} NurseryClearPolicy;

static NurseryClearPolicy nursery_clear_policy = CLEAR_AT_TLAB_CREATION;

/*
 * The young generation is divided into fragments. This is because
 * we can hand one fragments to a thread for lock-less fast alloc and
 * because the young generation ends up fragmented anyway by pinned objects.
 * Once a collection is done, a list of fragments is created. When doing
 * thread local alloc we use smallish nurseries so we allow new threads to
 * allocate memory from gen0 without triggering a collection. Threads that
 * are found to allocate lots of memory are given bigger fragments. This
 * should make the finalizer thread use little nursery memory after a while.
 * We should start assigning threads very small fragments: if there are many
 * threads the nursery will be full of reserved space that the threads may not
 * use at all, slowing down allocation speed.
 * Thread local allocation is done from areas of memory Hotspot calls Thread Local 
 * Allocation Buffers (TLABs).
 */
typedef struct _Fragment Fragment;

struct _Fragment {
	Fragment *next;
	char *fragment_start;
	char *fragment_limit; /* the current soft limit for allocation */
	char *fragment_end;
};

/* the runtime can register areas of memory as roots: we keep two lists of roots,
 * a pinned root set for conservatively scanned roots and a normal one for
 * precisely scanned roots (currently implemented as a single list).
 */
typedef struct _RootRecord RootRecord;
struct _RootRecord {
	RootRecord *next;
	char *start_root;
	char *end_root;
	mword root_desc;
};

/* for use with write barriers */
typedef struct _RememberedSet RememberedSet;
struct _RememberedSet {
	mword *store_next;
	mword *end_set;
	RememberedSet *next;
	mword data [MONO_ZERO_LEN_ARRAY];
};

/* we have 4 possible values in the low 2 bits */
enum {
	REMSET_LOCATION, /* just a pointer to the exact location */
	REMSET_RANGE,    /* range of pointer fields */
	REMSET_OBJECT,   /* mark all the object for scanning */
	REMSET_VTYPE,    /* a valuetype described by a gc descriptor */
	REMSET_TYPE_MASK = 0x3
};

static __thread RememberedSet *remembered_set MONO_TLS_FAST;
static pthread_key_t remembered_set_key;
static RememberedSet *global_remset;
static int store_to_global_remset = 0;

/* FIXME: later choose a size that takes into account the RememberedSet struct
 * and doesn't waste any alloc paddin space.
 */
#define DEFAULT_REMSET_SIZE 1024
static RememberedSet* alloc_remset (int size, gpointer id);

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
#define FORWARDED_BIT 1
#define PINNED_BIT 2
#define VTABLE_BITS_MASK 0x3

/* returns NULL if not forwarded, or the forwarded address */
#define object_is_forwarded(obj) (((mword*)(obj))[0] & FORWARDED_BIT? (void*)(((mword*)(obj))[1]): NULL)
/* set the forwarded address fw_addr for object obj */
#define forward_object(obj,fw_addr) do {	\
		((mword*)(obj))[0] |= FORWARDED_BIT;	\
		((mword*)(obj))[1] = (mword)(fw_addr);	\
	} while (0)

#define object_is_pinned(obj) (((mword*)(obj))[0] & PINNED_BIT)
#define pin_object(obj) do {	\
		((mword*)(obj))[0] |= PINNED_BIT;	\
	} while (0)
#define unpin_object(obj) do {	\
		((mword*)(obj))[0] &= ~PINNED_BIT;	\
	} while (0)


/*
 * Since we set bits in the vtable, use the macro to load it from the pointer to
 * an object that is potentially pinned.
 */
#define LOAD_VTABLE(addr) ((*(mword*)(addr)) & ~VTABLE_BITS_MASK)

static const char*
safe_name (void* obj)
{
	MonoVTable *vt = (MonoVTable*)LOAD_VTABLE (obj);
	return vt->klass->name;
}

static guint
safe_object_get_size (MonoObject* o)
{
	MonoClass *klass = ((MonoVTable*)LOAD_VTABLE (o))->klass;
	if (klass == mono_defaults.string_class) {
		return sizeof (MonoString) + 2 * mono_string_length ((MonoString*) o) + 2;
	} else if (klass->rank) {
		MonoArray *array = (MonoArray*)o;
		size_t size = sizeof (MonoArray) + mono_array_element_size (klass) * mono_array_length (array);
		if (array->bounds) {
			size += 3;
			size &= ~3;
			size += sizeof (MonoArrayBounds) * klass->rank;
		}
		return size;
	} else {
		/* from a created object: the class must be inited already */
		return klass->instance_size;
	}
}

static inline gboolean
is_maybe_half_constructed (MonoObject *o)
{
	MonoClass *klass;

	klass = ((MonoVTable*)LOAD_VTABLE (o))->klass;
	if ((klass == mono_defaults.string_class && mono_string_length ((MonoString*)o) == 0) ||
		(klass->rank && mono_array_length ((MonoArray*)o) == 0))
		return TRUE;
	else
		return FALSE;
}

/*
 * ######################################################################
 * ########  Global data.
 * ######################################################################
 */
static LOCK_DECLARE (gc_mutex);
static int gc_disabled = 0;
static int num_minor_gcs = 0;
static int num_major_gcs = 0;

/* good sizes are 512KB-1MB: larger ones increase a lot memzeroing time */
//#define DEFAULT_NURSERY_SIZE (1024*512*125+4096*118)
#define DEFAULT_NURSERY_SIZE (1024*512*2)
#define DEFAULT_MAX_SECTION (DEFAULT_NURSERY_SIZE * 16)
#define DEFAULT_LOS_COLLECTION_TARGET (DEFAULT_NURSERY_SIZE * 2)
/* to quickly find the head of an object pinned by a conservative address
 * we keep track of the objects allocated for each SCAN_START_SIZE memory
 * chunk in the nursery or other memory sections. Larger values have less
 * memory overhead and bigger runtime cost. 4-8 KB are reasonable values.
 */
#define SCAN_START_SIZE (4096*2)
/* the minimum size of a fragment that we consider useful for allocation */
#define FRAGMENT_MIN_SIZE (512)
/* This is a fixed value used for pinned chunks, not the system pagesize */
#define FREELIST_PAGESIZE 4096

static mword pagesize = 4096;
static mword nursery_size = DEFAULT_NURSERY_SIZE;
static mword next_section_size = DEFAULT_NURSERY_SIZE * 4;
static mword max_section_size = DEFAULT_MAX_SECTION;
static int section_size_used = 0;
static int degraded_mode = 0;

static LOSObject *los_object_list = NULL;
static mword los_memory_usage = 0;
static mword los_num_objects = 0;
static mword next_los_collection = 2*1024*1024; /* 2 MB, need to tune */
static mword total_alloc = 0;
/* use this to tune when to do a major/minor collection */
static mword memory_pressure = 0;

static GCMemSection *section_list = NULL;
static GCMemSection *nursery_section = NULL;
static mword lowest_heap_address = ~(mword)0;
static mword highest_heap_address = 0;

typedef struct _FinalizeEntry FinalizeEntry;
struct _FinalizeEntry {
	FinalizeEntry *next;
	void *object;
	void *data; /* can be a disappearing link or the data for the finalizer */
	/* Note we could use just one pointer if we don't support multiple callbacks
	 * for finalizers and per-finalizer data and if we store the obj pointers
	 * in the link like libgc does
	 */
};

/*
 * The finalizable hash has the object as the key, the 
 * disappearing_link hash, has the link address as key.
 */
static FinalizeEntry **finalizable_hash = NULL;
/* objects that are ready to be finalized */
static FinalizeEntry *fin_ready_list = NULL;
/* disappearing links use the same structure but a different list */
static FinalizeEntry **disappearing_link_hash = NULL;
static mword disappearing_link_hash_size = 0;
static mword finalizable_hash_size = 0;

static int num_registered_finalizers = 0;
static int num_ready_finalizers = 0;
static int num_disappearing_links = 0;
static int no_finalize = 0;

/* keep each size a multiple of ALLOC_ALIGN */
/* on 64 bit systems 8 is likely completely unused. */
static const int freelist_sizes [] = {
	8, 16, 24, 32, 40, 48, 64, 80,
	96, 128, 160, 192, 224, 256, 320, 384,
	448, 512, 584, 680, 816, 1024, 1360, 2048};
#define FREELIST_NUM_SLOTS (sizeof (freelist_sizes) / sizeof (freelist_sizes [0]))

static char* max_pinned_chunk_addr = NULL;
static char* min_pinned_chunk_addr = (char*)-1;
/* pinned_chunk_list is used for allocations of objects that are never moved */
static PinnedChunk *pinned_chunk_list = NULL;
/* internal_chunk_list is used for allocating structures needed by the GC */
static PinnedChunk *internal_chunk_list = NULL;

static gboolean
obj_is_from_pinned_alloc (char *p)
{
	PinnedChunk *chunk = pinned_chunk_list;
	for (; chunk; chunk = chunk->next) {
		if (p >= (char*)chunk->start_data && p < ((char*)chunk + chunk->num_pages * FREELIST_PAGESIZE))
			return TRUE;
	}
	return FALSE;
}

/* registered roots: the key to the hash is the root start address */
static RootRecord **roots_hash = NULL;
static int roots_hash_size = 0;
static mword roots_size = 0; /* amount of memory in the root set */
static int num_roots_entries = 0;

/* 
 * The current allocation cursors
 * We allocate objects in the nursery.
 * The nursery is the area between nursery_start and nursery_real_end.
 * Allocation is done from a Thread Local Allocation Buffer (TLAB). TLABs are allocated
 * from nursery fragments.
 * tlab_next is the pointer to the space inside the TLAB where the next object will 
 * be allocated.
 * tlab_temp_end is the pointer to the end of the temporary space reserved for
 * the allocation: it allows us to set the scan starts at reasonable intervals.
 * tlab_real_end points to the end of the TLAB.
 * nursery_frag_real_end points to the end of the currently used nursery fragment.
 * nursery_first_pinned_start points to the start of the first pinned object in the nursery
 * nursery_last_pinned_end points to the end of the last pinned object in the nursery
 * At the next allocation, the area of the nursery where objects can be present is
 * between MIN(nursery_first_pinned_start, first_fragment_start) and
 * MAX(nursery_last_pinned_end, nursery_frag_real_end)
 */
static char *nursery_start = NULL;

/*
 * FIXME: What is faster, a TLS variable pointing to a structure, or separate TLS 
 * variables for next+temp_end ?
 */
static __thread char *tlab_start;
static __thread char *tlab_next;
static __thread char *tlab_temp_end;
static __thread char *tlab_real_end;
/* Used by the managed allocator */
static __thread char **tlab_next_addr;
static char *nursery_next = NULL;
static char *nursery_frag_real_end = NULL;
static char *nursery_real_end = NULL;
static char *nursery_first_pinned_start = NULL;
static char *nursery_last_pinned_end = NULL;

/* The size of a TLAB */
/* The bigger the value, the less often we have to go to the slow path to allocate a new 
 * one, but the more space is wasted by threads not allocating much memory.
 * FIXME: Tune this.
 * FIXME: Make this self-tuning for each thread.
 */
static guint32 tlab_size = (1024 * 4);

/* fragments that are free and ready to be used for allocation */
static Fragment *nursery_fragments = NULL;
/* freeelist of fragment structures */
static Fragment *fragment_freelist = NULL;

/* 
 * used when moving the objects
 * When the nursery is collected, objects are copied to to_space.
 * The area between to_space and gray_objects is used as a stack
 * of objects that need their fields checked for more references
 * to be copied.
 * We should optimize somehow this mechanism to avoid rescanning
 * ptr-free objects. The order is also probably not optimal: need to
 * test cache misses and other graph traversal orders.
 */
static char *to_space = NULL;
static char *gray_objects = NULL;
static char *to_space_end = NULL;
static GCMemSection *to_space_section = NULL;

/* objects bigger then this go into the large object space */
#define MAX_SMALL_OBJ_SIZE 0xffff

/*
 * ######################################################################
 * ########  Macros and function declarations.
 * ######################################################################
 */

#define UPDATE_HEAP_BOUNDARIES(low,high) do {	\
		if ((mword)(low) < lowest_heap_address)	\
			lowest_heap_address = (mword)(low);	\
		if ((mword)(high) > highest_heap_address)	\
			highest_heap_address = (mword)(high);	\
	} while (0)

inline static void*
align_pointer (void *ptr)
{
	mword p = (mword)ptr;
	p += sizeof (gpointer) - 1;
	p &= ~ (sizeof (gpointer) - 1);
	return (void*)p;
}

/* forward declarations */
static void* get_internal_mem          (size_t size);
static void  free_internal_mem         (void *addr);
static void* get_os_memory             (size_t size, int activate);
static void  free_os_memory            (void *addr, size_t size);
static void  report_internal_mem_usage (void);

static int stop_world (void);
static int restart_world (void);
static void pin_thread_data (void *start_nursery, void *end_nursery);
static void scan_from_remsets (void *start_nursery, void *end_nursery);
static void find_pinning_ref_from_thread (char *obj, size_t size);
static void update_current_thread_stack (void *start);
static GCMemSection* alloc_section (size_t size);
static void finalize_in_range (char *start, char *end);
static void null_link_in_range (char *start, char *end);
static gboolean search_fragment_for_size (size_t size);
static void mark_pinned_from_addresses (PinnedChunk *chunk, void **start, void **end);
static void clear_remsets (void);
static void clear_tlabs (void);
static char *find_tlab_next_from_address (char *addr);
static void sweep_pinned_objects (void);
static void free_large_object (LOSObject *obj);
static void free_mem_section (GCMemSection *section);

void check_consistency (void);

/*
 * ######################################################################
 * ########  GC descriptors
 * ######################################################################
 * Used to quickly get the info the GC needs about an object: size and
 * where the references are held.
 */
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
	DESC_TYPE_RUN_LENGTH,   /* 16 bits aligned byte size | 1-3 (offset, numptr) bytes tuples */
	DESC_TYPE_SMALL_BITMAP, /* 16 bits aligned byte size | 16-48 bit bitmap */
	DESC_TYPE_STRING,       /* nothing */
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

#define OBJECT_HEADER_WORDS (sizeof(MonoObject)/sizeof(gpointer))
#define LOW_TYPE_BITS 3
#define SMALL_BITMAP_SHIFT 16
#define SMALL_BITMAP_SIZE (GC_BITS_PER_WORD - SMALL_BITMAP_SHIFT)
#define VECTOR_INFO_SHIFT 14
#define VECTOR_ELSIZE_SHIFT 3
#define LARGE_BITMAP_SIZE (GC_BITS_PER_WORD - LOW_TYPE_BITS)
#define MAX_SMALL_SIZE ((1 << SMALL_BITMAP_SHIFT) - 1)
#define SMALL_SIZE_MASK 0xfff8
#define MAX_ELEMENT_SIZE 0x3ff
#define ELEMENT_SIZE_MASK (0x3ff << LOW_TYPE_BITS)
#define VECTOR_SUBTYPE_PTRFREE (DESC_TYPE_V_PTRFREE << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_REFS    (DESC_TYPE_V_REFS << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_RUN_LEN (DESC_TYPE_V_RUN_LEN << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_BITMAP  (DESC_TYPE_V_BITMAP << VECTOR_INFO_SHIFT)

#define ALLOC_ALIGN 8


/* Root bitmap descriptors are simpler: the lower two bits describe the type
 * and we either have 30/62 bitmap bits or nibble-based run-length,
 * or a complex descriptor
 */
enum {
	ROOT_DESC_CONSERVATIVE, /* 0, so matches NULL value */
	ROOT_DESC_BITMAP,
	ROOT_DESC_RUN_LEN,
	ROOT_DESC_LARGE_BITMAP,
	ROOT_DESC_TYPE_MASK = 0x3,
	ROOT_DESC_TYPE_SHIFT = 2,
};

static gsize* complex_descriptors = NULL;
static int complex_descriptors_size = 0;
static int complex_descriptors_next = 0;

static int
alloc_complex_descriptor (gsize *bitmap, int numbits)
{
	int nwords = numbits/GC_BITS_PER_WORD + 2;
	int res;
	int i;

	LOCK_GC;
	res = complex_descriptors_next;
	/* linear search, so we don't have duplicates with domain load/unload
	 * this should not be performance critical or we'd have bigger issues
	 * (the number and size of complex descriptors should be small).
	 */
	for (i = 0; i < complex_descriptors_next; ) {
		if (complex_descriptors [i] == nwords) {
			int j, found = TRUE;
			for (j = 0; j < nwords - 1; ++j) {
				if (complex_descriptors [i + 1 + j] != bitmap [j]) {
					found = FALSE;
					break;
				}
			}
			if (found) {
				UNLOCK_GC;
				return i;
			}
		}
		i += complex_descriptors [i];
	}
	if (complex_descriptors_next + nwords > complex_descriptors_size) {
		int new_size = complex_descriptors_size * 2 + nwords;
		complex_descriptors = g_realloc (complex_descriptors, new_size * sizeof (gsize));
		complex_descriptors_size = new_size;
	}
	DEBUG (6, fprintf (gc_debug_file, "Complex descriptor %d, size: %d (total desc memory: %d)\n", res, nwords, complex_descriptors_size));
	complex_descriptors_next += nwords;
	complex_descriptors [res] = nwords;
	for (i = 0; i < nwords - 1; ++i) {
		complex_descriptors [res + 1 + i] = bitmap [i];
		DEBUG (6, fprintf (gc_debug_file, "\tvalue: %p\n", (void*)complex_descriptors [res + 1 + i]));
	}
	UNLOCK_GC;
	return res;
}

/*
 * Descriptor builders.
 */
void*
mono_gc_make_descr_for_string (gsize *bitmap, int numbits)
{
	return (void*) DESC_TYPE_STRING;
}

void*
mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	mword desc = 0;
	size_t stored_size = obj_size;
	stored_size += ALLOC_ALIGN - 1;
	stored_size &= ~(ALLOC_ALIGN - 1);
	for (i = 0; i < numbits; ++i) {
		if (bitmap [i / GC_BITS_PER_WORD] & (1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}
	if (stored_size <= MAX_SMALL_OBJ_SIZE) {
		/* check run-length encoding first: one byte offset, one byte number of pointers
		 * on 64 bit archs, we can have 3 runs, just one on 32.
		 * It may be better to use nibbles.
		 */
		if (first_set < 0) {
			desc = DESC_TYPE_RUN_LENGTH | stored_size;
			DEBUG (6, fprintf (gc_debug_file, "Ptrfree descriptor %p, size: %zd\n", (void*)desc, stored_size));
			return (void*) desc;
		} else if (first_set < 256 && num_set < 256 && (first_set + num_set == last_set + 1)) {
			desc = DESC_TYPE_RUN_LENGTH | stored_size | (first_set << 16) | (num_set << 24);
			DEBUG (6, fprintf (gc_debug_file, "Runlen descriptor %p, size: %zd, first set: %d, num set: %d\n", (void*)desc, stored_size, first_set, num_set));
			return (void*) desc;
		}
		/* we know the 2-word header is ptr-free */
		if (last_set < SMALL_BITMAP_SIZE + OBJECT_HEADER_WORDS) {
			desc = DESC_TYPE_SMALL_BITMAP | stored_size | ((*bitmap >> OBJECT_HEADER_WORDS) << SMALL_BITMAP_SHIFT);
			DEBUG (6, fprintf (gc_debug_file, "Smallbitmap descriptor %p, size: %zd, last set: %d\n", (void*)desc, stored_size, last_set));
			return (void*) desc;
		}
	}
	/* we know the 2-word header is ptr-free */
	if (last_set < LARGE_BITMAP_SIZE + OBJECT_HEADER_WORDS) {
		desc = DESC_TYPE_LARGE_BITMAP | ((*bitmap >> OBJECT_HEADER_WORDS) << LOW_TYPE_BITS);
		DEBUG (6, fprintf (gc_debug_file, "Largebitmap descriptor %p, size: %zd, last set: %d\n", (void*)desc, stored_size, last_set));
		return (void*) desc;
	}
	/* it's a complex object ... */
	desc = DESC_TYPE_COMPLEX | (alloc_complex_descriptor (bitmap, last_set + 1) << LOW_TYPE_BITS);
	return (void*) desc;
}

/* If the array holds references, numbits == 1 and the first bit is set in elem_bitmap */
void*
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	mword desc = vector? DESC_TYPE_VECTOR: DESC_TYPE_ARRAY;
	for (i = 0; i < numbits; ++i) {
		if (elem_bitmap [i / GC_BITS_PER_WORD] & (1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}
	if (elem_size <= MAX_ELEMENT_SIZE) {
		desc |= elem_size << VECTOR_ELSIZE_SHIFT;
		if (!num_set) {
			return (void*)(desc | VECTOR_SUBTYPE_PTRFREE);
		}
		/* Note: we also handle structs with just ref fields */
		if (num_set * sizeof (gpointer) == elem_size) {
			return (void*)(desc | VECTOR_SUBTYPE_REFS | ((gssize)(-1) << 16));
		}
		/* FIXME: try run-len first */
		/* Note: we can't skip the object header here, because it's not present */
		if (last_set <= SMALL_BITMAP_SIZE) {
			return (void*)(desc | VECTOR_SUBTYPE_BITMAP | (*elem_bitmap << 16));
		}
	}
	/* it's am array of complex structs ... */
	desc = DESC_TYPE_COMPLEX_ARR;
	desc |= alloc_complex_descriptor (elem_bitmap, last_set + 1) << LOW_TYPE_BITS;
	return (void*) desc;
}

/* helper macros to scan and traverse objects, macros because we resue them in many functions */
#define STRING_SIZE(size,str) do {	\
		(size) = sizeof (MonoString) + 2 * (mono_string_length ((MonoString*)(str)) + 1);	\
		(size) += (ALLOC_ALIGN - 1);	\
		(size) &= ~(ALLOC_ALIGN - 1);	\
	} while (0)

#define OBJ_RUN_LEN_SIZE(size,desc,obj) do { \
        (size) = (desc) & 0xfff8; \
    } while (0)

#define OBJ_BITMAP_SIZE(size,desc,obj) do { \
        (size) = (desc) & 0xfff8; \
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

#define OBJ_COMPLEX_FOREACH_PTR(vt,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize *bitmap_data = complex_descriptors + ((vt)->desc >> LOW_TYPE_BITS);	\
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
		gsize *mbitmap_data = complex_descriptors + ((vt)->desc >> LOW_TYPE_BITS);	\
		int mbwords = (*mbitmap_data++) - 1;	\
		int el_size = mono_array_element_size (((MonoObject*)(obj))->vtable->klass);	\
		char *e_start = (char*)(obj) +  G_STRUCT_OFFSET (MonoArray, vector);	\
		char *e_end = e_start + el_size * mono_array_length ((MonoArray*)(obj));	\
		if (0) {	\
			MonoObject *myobj = (MonoObject*)start;	\
			g_print ("found %d at %p (0x%zx): %s.%s\n", mbwords, (obj), (vt)->desc, myobj->vtable->klass->name_space, myobj->vtable->klass->name);	\
		}	\
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
				void **end_refs = (void**)((char*)p + el_size * mono_array_length ((MonoArray*)(obj)));	\
				/* Note: this code can handle also arrays of struct with only references in them */	\
				while (p < end_refs) {	\
					HANDLE_PTR (p, (obj));	\
					++p;	\
				}	\
			} else if (etype == DESC_TYPE_V_RUN_LEN << 14) {	\
				int offset = ((vt)->desc >> 16) & 0xff;	\
				int num_refs = ((vt)->desc >> 24) & 0xff;	\
				char *e_start = (char*)(obj) + G_STRUCT_OFFSET (MonoArray, vector);	\
				char *e_end = e_start + el_size * mono_array_length ((MonoArray*)(obj));	\
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
				char *e_end = e_start + el_size * mono_array_length ((MonoArray*)(obj));	\
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

static mword new_obj_references = 0;
static mword obj_references_checked = 0;

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr) && (char*)*(ptr) >= nursery_start && (char*)*(ptr) < nursery_next) {	\
			new_obj_references++;	\
			/*printf ("bogus ptr %p found at %p in object %p (%s.%s)\n", *(ptr), (ptr), o, o->vtable->klass->name_space, o->vtable->klass->name);*/	\
		} else {	\
			obj_references_checked++;	\
		}	\
	} while (0)

/*
 * ######################################################################
 * ########  Detecting and removing garbage.
 * ######################################################################
 * This section of code deals with detecting the objects no longer in use
 * and reclaiming the memory.
 */
static void __attribute__((noinline))
scan_area (char *start, char *end)
{
	GCVTable *vt;
	size_t skip_size;
	int type;
	int type_str = 0, type_rlen = 0, type_bitmap = 0, type_vector = 0, type_lbit = 0, type_complex = 0;
	mword desc;
	new_obj_references = 0;
	obj_references_checked = 0;
	while (start < end) {
		if (!*(void**)start) {
			start += sizeof (void*); /* should be ALLOC_ALIGN, really */
			continue;
		}
		vt = (GCVTable*)LOAD_VTABLE (start);
		DEBUG (8, fprintf (gc_debug_file, "Scanning object %p, vtable: %p (%s)\n", start, vt, vt->klass->name));
		if (0) {
			MonoObject *obj = (MonoObject*)start;
			g_print ("found at %p (0x%zx): %s.%s\n", start, vt->desc, obj->vtable->klass->name_space, obj->vtable->klass->name);
		}
		desc = vt->desc;
		type = desc & 0x7;
		if (type == DESC_TYPE_STRING) {
			STRING_SIZE (skip_size, start);
			start += skip_size;
			type_str++;
			continue;
		} else if (type == DESC_TYPE_RUN_LENGTH) {
			OBJ_RUN_LEN_SIZE (skip_size, desc, start);
			g_assert (skip_size);
			OBJ_RUN_LEN_FOREACH_PTR (desc,start);
			start += skip_size;
			type_rlen++;
			continue;
		} else if (type == DESC_TYPE_VECTOR) { // includes ARRAY, too
			skip_size = (vt->desc >> LOW_TYPE_BITS) & MAX_ELEMENT_SIZE;
			skip_size *= mono_array_length ((MonoArray*)start);
			skip_size += sizeof (MonoArray);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_VECTOR_FOREACH_PTR (vt, start);
			if (type == DESC_TYPE_ARRAY) {
				/* account for the bounds */
			}
			start += skip_size;
			type_vector++;
			continue;
		} else if (type == DESC_TYPE_SMALL_BITMAP) {
			OBJ_BITMAP_SIZE (skip_size, desc, start);
			g_assert (skip_size);
			OBJ_BITMAP_FOREACH_PTR (desc,start);
			start += skip_size;
			type_bitmap++;
			continue;
		} else if (type == DESC_TYPE_LARGE_BITMAP) {
			skip_size = safe_object_get_size ((MonoObject*)start);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_LARGE_BITMAP_FOREACH_PTR (vt,start);
			start += skip_size;
			type_lbit++;
			continue;
		} else if (type == DESC_TYPE_COMPLEX) {
			/* this is a complex object */
			skip_size = safe_object_get_size ((MonoObject*)start);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_COMPLEX_FOREACH_PTR (vt, start);
			start += skip_size;
			type_complex++;
			continue;
		} else if (type == DESC_TYPE_COMPLEX_ARR) {
			/* this is an array of complex structs */
			skip_size = mono_array_element_size (((MonoVTable*)vt)->klass);
			skip_size *= mono_array_length ((MonoArray*)start);
			skip_size += sizeof (MonoArray);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_COMPLEX_ARR_FOREACH_PTR (vt, start);
			if (type == DESC_TYPE_ARRAY) {
				/* account for the bounds */
			}
			start += skip_size;
			type_complex++;
			continue;
		} else {
			g_assert (0);
		}
	}
	/*printf ("references to new nursery %p-%p (size: %dk): %d, checked: %d\n", old_start, end, (end-old_start)/1024, new_obj_references, obj_references_checked);
	printf ("\tstrings: %d, runl: %d, vector: %d, bitmaps: %d, lbitmaps: %d, complex: %d\n",
		type_str, type_rlen, type_vector, type_bitmap, type_lbit, type_complex);*/
}

static void __attribute__((noinline))
scan_area_for_domain (MonoDomain *domain, char *start, char *end)
{
	GCVTable *vt;
	size_t skip_size;
	int type, remove;
	mword desc;

	while (start < end) {
		if (!*(void**)start) {
			start += sizeof (void*); /* should be ALLOC_ALIGN, really */
			continue;
		}
		vt = (GCVTable*)LOAD_VTABLE (start);
		/* handle threads someway (maybe insert the root domain vtable?) */
		if (mono_object_domain (start) == domain && vt->klass != mono_defaults.thread_class) {
			DEBUG (1, fprintf (gc_debug_file, "Need to cleanup object %p, (%s)\n", start, safe_name (start)));
			remove = 1;
		} else {
			remove = 0;
		}
		desc = vt->desc;
		type = desc & 0x7;
		if (type == DESC_TYPE_STRING) {
			STRING_SIZE (skip_size, start);
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else if (type == DESC_TYPE_RUN_LENGTH) {
			OBJ_RUN_LEN_SIZE (skip_size, desc, start);
			g_assert (skip_size);
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else if (type == DESC_TYPE_VECTOR) { // includes ARRAY, too
			skip_size = (vt->desc >> LOW_TYPE_BITS) & MAX_ELEMENT_SIZE;
			skip_size *= mono_array_length ((MonoArray*)start);
			skip_size += sizeof (MonoArray);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			if (type == DESC_TYPE_ARRAY) {
				/* account for the bounds */
			}
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else if (type == DESC_TYPE_SMALL_BITMAP) {
			OBJ_BITMAP_SIZE (skip_size, desc, start);
			g_assert (skip_size);
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else if (type == DESC_TYPE_LARGE_BITMAP) {
			skip_size = safe_object_get_size ((MonoObject*)start);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else if (type == DESC_TYPE_COMPLEX) {
			/* this is a complex object */
			skip_size = safe_object_get_size ((MonoObject*)start);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else if (type == DESC_TYPE_COMPLEX_ARR) {
			/* this is an array of complex structs */
			skip_size = mono_array_element_size (((MonoVTable*)vt)->klass);
			skip_size *= mono_array_length ((MonoArray*)start);
			skip_size += sizeof (MonoArray);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			if (type == DESC_TYPE_ARRAY) {
				/* account for the bounds */
			}
			if (remove) memset (start, 0, skip_size);
			start += skip_size;
			continue;
		} else {
			g_assert (0);
		}
	}
}

/*
 * When appdomains are unloaded we can easily remove objects that have finalizers,
 * but all the others could still be present in random places on the heap.
 * We need a sweep to get rid of them even though it's going to be costly
 * with big heaps.
 * The reason we need to remove them is because we access the vtable and class
 * structures to know the object size and the reference bitmap: once the domain is
 * unloaded the point to random memory.
 */
void
mono_gc_clear_domain (MonoDomain * domain)
{
	GCMemSection *section;
	LOCK_GC;
	for (section = section_list; section; section = section->next) {
		scan_area_for_domain (domain, section->data, section->end_data);
	}
	/* FIXME: handle big and fixed objects (we remove, don't clear in this case) */
	UNLOCK_GC;
}

static void
add_to_global_remset (gpointer ptr)
{
	RememberedSet *rs;
	DEBUG (8, fprintf (gc_debug_file, "Adding global remset for %p\n", ptr));
	if (global_remset->store_next < global_remset->end_set) {
		*(global_remset->store_next++) = (mword)ptr;
		return;
	}
	rs = alloc_remset (global_remset->end_set - global_remset->data, NULL);
	rs->next = global_remset;
	global_remset = rs;
	*(global_remset->store_next++) = (mword)ptr;
}

/*
 * This is how the copying happens from the nursery to the old generation.
 * We assume that at this time all the pinned objects have been identified and
 * marked as such.
 * We run scan_object() for each pinned object so that each referenced
 * objects if possible are copied. The new gray objects created can have
 * scan_object() run on them right away, too.
 * Then we run copy_object() for the precisely tracked roots. At this point
 * all the roots are either gray or black. We run scan_object() on the gray
 * objects until no more gray objects are created.
 * At the end of the process we walk again the pinned list and we unmark
 * the pinned flag. As we go we also create the list of free space for use
 * in the next allocation runs.
 *
 * We need to remember objects from the old generation that point to the new one
 * (or just addresses?).
 *
 * copy_object could be made into a macro once debugged (use inline for now).
 */

static char* __attribute__((noinline))
copy_object (char *obj, char *from_space_start, char *from_space_end)
{
	if (obj >= from_space_start && obj < from_space_end && (obj < to_space || obj >= to_space_end)) {
		MonoVTable *vt;
		char *forwarded;
		mword objsize;
		DEBUG (9, fprintf (gc_debug_file, "Precise copy of %p", obj));
		if ((forwarded = object_is_forwarded (obj))) {
			g_assert (((MonoVTable*)LOAD_VTABLE(obj))->gc_descr);
			DEBUG (9, fprintf (gc_debug_file, " (already forwarded to %p)\n", forwarded));
			return forwarded;
		}
		if (object_is_pinned (obj)) {
			g_assert (((MonoVTable*)LOAD_VTABLE(obj))->gc_descr);
			DEBUG (9, fprintf (gc_debug_file, " (pinned, no change)\n"));
			return obj;
		}
		objsize = safe_object_get_size ((MonoObject*)obj);
		objsize += ALLOC_ALIGN - 1;
		objsize &= ~(ALLOC_ALIGN - 1);
		DEBUG (9, fprintf (gc_debug_file, " (to %p, %s size: %zd)\n", gray_objects, ((MonoObject*)obj)->vtable->klass->name, objsize));
		/* FIXME: handle pinned allocs:
		 * Large objects are simple, at least until we always follow the rule:
		 * if objsize >= MAX_SMALL_OBJ_SIZE, pin the object and return it.
		 * At the end of major collections, we walk the los list and if
		 * the object is pinned, it is marked, otherwise it can be freed.
		 */
		if (objsize >= MAX_SMALL_OBJ_SIZE || (obj >= min_pinned_chunk_addr && obj < max_pinned_chunk_addr && obj_is_from_pinned_alloc (obj))) {
			DEBUG (9, fprintf (gc_debug_file, "Marked LOS/Pinned %p (%s), size: %zd\n", obj, safe_name (obj), objsize));
			pin_object (obj);
			return obj;
		}
		/* ok, the object is not pinned, we can move it */
		/* use a optimized memcpy here */
#if 0
		{
			int ecx;
			char* esi = obj;
			char* edi = gray_objects;
			__asm__ __volatile__(
				"rep; movsl"
				: "=&c" (ecx), "=&D" (edi), "=&S" (esi)
				: "0" (objsize/4), "1" (edi),"2" (esi)
				: "memory"
			);
		}
#else
		memcpy (gray_objects, obj, objsize);
#endif
		/* adjust array->bounds */
		vt = ((MonoObject*)obj)->vtable;
		g_assert (vt->gc_descr);
		if (vt->rank && ((MonoArray*)obj)->bounds) {
			MonoArray *array = (MonoArray*)gray_objects;
			array->bounds = (MonoArrayBounds*)((char*)gray_objects + ((char*)((MonoArray*)obj)->bounds - (char*)obj));
			DEBUG (9, fprintf (gc_debug_file, "Array instance %p: size: %zd, rank: %d, length: %d\n", array, objsize, vt->rank, mono_array_length (array)));
		}
		/* set the forwarding pointer */
		forward_object (obj, gray_objects);
		obj = gray_objects;
		to_space_section->scan_starts [((char*)obj - (char*)to_space_section->data)/SCAN_START_SIZE] = obj;
		gray_objects += objsize;
		DEBUG (8, g_assert (gray_objects <= to_space_end));
		return obj;
	}
	return obj;
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr)) {	\
			void *__old = *(ptr);	\
			*(ptr) = copy_object (*(ptr), from_start, from_end);	\
			DEBUG (9, if (__old != *(ptr)) fprintf (gc_debug_file, "Overwrote field at %p with %p (was: %p)\n", (ptr), *(ptr), __old));	\
			if (G_UNLIKELY (*(ptr) >= (void*)from_start && *(ptr) < (void*)from_end))	\
				add_to_global_remset ((ptr));	\
		}	\
	} while (0)

/*
 * Scan the object pointed to by @start for references to
 * other objects between @from_start and @from_end and copy
 * them to the gray_objects area.
 * Returns a pointer to the end of the object.
 */
static char*
scan_object (char *start, char* from_start, char* from_end)
{
	GCVTable *vt;
	size_t skip_size;
	mword desc;

	vt = (GCVTable*)LOAD_VTABLE (start);
	//type = vt->desc & 0x7;

	/* gcc should be smart enough to remove the bounds check, but it isn't:( */
	desc = vt->desc;
	switch (desc & 0x7) {
	//if (type == DESC_TYPE_STRING) {
	case DESC_TYPE_STRING:
		STRING_SIZE (skip_size, start);
		return start + skip_size;
	//} else if (type == DESC_TYPE_RUN_LENGTH) {
	case DESC_TYPE_RUN_LENGTH:
		OBJ_RUN_LEN_FOREACH_PTR (desc,start);
		OBJ_RUN_LEN_SIZE (skip_size, desc, start);
		g_assert (skip_size);
		return start + skip_size;
	//} else if (type == DESC_TYPE_VECTOR) { // includes ARRAY, too
	case DESC_TYPE_ARRAY:
	case DESC_TYPE_VECTOR:
		OBJ_VECTOR_FOREACH_PTR (vt, start);
		skip_size = safe_object_get_size ((MonoObject*)start);
#if 0
		skip_size = (vt->desc >> LOW_TYPE_BITS) & MAX_ELEMENT_SIZE;
		skip_size *= mono_array_length ((MonoArray*)start);
		skip_size += sizeof (MonoArray);
#endif
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
		return start + skip_size;
	//} else if (type == DESC_TYPE_SMALL_BITMAP) {
	case DESC_TYPE_SMALL_BITMAP:
		OBJ_BITMAP_FOREACH_PTR (desc,start);
		OBJ_BITMAP_SIZE (skip_size, desc, start);
		return start + skip_size;
	//} else if (type == DESC_TYPE_LARGE_BITMAP) {
	case DESC_TYPE_LARGE_BITMAP:
		OBJ_LARGE_BITMAP_FOREACH_PTR (vt,start);
		skip_size = safe_object_get_size ((MonoObject*)start);
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
		return start + skip_size;
	//} else if (type == DESC_TYPE_COMPLEX) {
	case DESC_TYPE_COMPLEX:
		OBJ_COMPLEX_FOREACH_PTR (vt, start);
		/* this is a complex object */
		skip_size = safe_object_get_size ((MonoObject*)start);
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
		return start + skip_size;
	//} else if (type == DESC_TYPE_COMPLEX_ARR) {
	case DESC_TYPE_COMPLEX_ARR:
		OBJ_COMPLEX_ARR_FOREACH_PTR (vt, start);
		/* this is an array of complex structs */
		skip_size = safe_object_get_size ((MonoObject*)start);
#if 0
		skip_size = mono_array_element_size (((MonoObject*)start)->vtable->klass);
		skip_size *= mono_array_length ((MonoArray*)start);
		skip_size += sizeof (MonoArray);
#endif
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
		return start + skip_size;
	}
	g_assert_not_reached ();
	return NULL;
}

/*
 * scan_vtype:
 *
 * Scan the valuetype pointed to by START, described by DESC for references to
 * other objects between @from_start and @from_end and copy them to the gray_objects area.
 * Returns a pointer to the end of the object.
 */
static char*
scan_vtype (char *start, mword desc, char* from_start, char* from_end)
{
	size_t skip_size;

	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

	switch (desc & 0x7) {
	case DESC_TYPE_RUN_LENGTH:
		OBJ_RUN_LEN_FOREACH_PTR (desc,start);
		OBJ_RUN_LEN_SIZE (skip_size, desc, start);
		g_assert (skip_size);
		return start + skip_size;
	case DESC_TYPE_SMALL_BITMAP:
		OBJ_BITMAP_FOREACH_PTR (desc,start);
		OBJ_BITMAP_SIZE (skip_size, desc, start);
		return start + skip_size;
	case DESC_TYPE_LARGE_BITMAP:
	case DESC_TYPE_COMPLEX:
		// FIXME:
		g_assert_not_reached ();
		break;
	default:
		// The other descriptors can't happen with vtypes
		g_assert_not_reached ();
		break;
	}
	return NULL;
}

/*
 * Addresses from start to end are already sorted. This function finds the object header
 * for each address and pins the object. The addresses must be inside the passed section.
 * Return the number of pinned objects.
 */
static int
pin_objects_from_addresses (GCMemSection *section, void **start, void **end, void *start_nursery, void *end_nursery)
{
	void *last = NULL;
	int count = 0;
	void *search_start;
	void *last_obj = NULL;
	size_t last_obj_size = 0;
	void *addr;
	int idx;
	void **definitely_pinned = start;
	while (start < end) {
		addr = *start;
		/* the range check should be reduntant */
		if (addr != last && addr >= start_nursery && addr < end_nursery) {
			DEBUG (5, fprintf (gc_debug_file, "Considering pinning addr %p\n", addr));
			/* multiple pointers to the same object */
			if (addr >= last_obj && (char*)addr < (char*)last_obj + last_obj_size) {
				start++;
				continue;
			}
			idx = ((char*)addr - (char*)section->data) / SCAN_START_SIZE;
			search_start = (void*)section->scan_starts [idx];
			if (!search_start || search_start > addr) {
				while (idx) {
					--idx;
					search_start = section->scan_starts [idx];
					if (search_start && search_start <= addr)
						break;
				}
				if (!search_start || search_start > addr)
					search_start = start_nursery;
			}
			if (search_start < last_obj)
				search_start = (char*)last_obj + last_obj_size;
			/* now addr should be in an object a short distance from search_start
			 * Note that search_start must point to zeroed mem or point to an object.
			 */
			do {
				if (!*(void**)search_start) {
					mword p = (mword)search_start;
					p += sizeof (gpointer);
					p += ALLOC_ALIGN - 1;
					p &= ~(ALLOC_ALIGN - 1);
					search_start = (void*)p;
					continue;
				}
				last_obj = search_start;
				last_obj_size = safe_object_get_size ((MonoObject*)search_start);
				last_obj_size += ALLOC_ALIGN - 1;
				last_obj_size &= ~(ALLOC_ALIGN - 1);
				DEBUG (8, fprintf (gc_debug_file, "Pinned try match %p (%s), size %zd\n", last_obj, safe_name (last_obj), last_obj_size));
				if (addr >= search_start && (char*)addr < (char*)last_obj + last_obj_size) {
					DEBUG (4, fprintf (gc_debug_file, "Pinned object %p, vtable %p (%s), count %d\n", search_start, *(void**)search_start, safe_name (search_start), count));
					pin_object (search_start);
					definitely_pinned [count] = search_start;
					count++;
					break;
				}
				/* skip to the next object */
				search_start = (void*)((char*)search_start + last_obj_size);
			} while (search_start <= addr);
			/* we either pinned the correct object or we ignored the addr because
			 * it points to unused zeroed memory.
			 */
			last = addr;
		}
		start++;
	}
	//printf ("effective pinned: %d (at the end: %d)\n", count, (char*)end_nursery - (char*)last);
	return count;
}

static void** pin_queue;
static int pin_queue_size = 0;
static int next_pin_slot = 0;

static int
new_gap (int gap)
{
	gap = (gap * 10) / 13;
	if (gap == 9 || gap == 10)
		return 11;
	if (gap < 1)
		return 1;
	return gap;
}

#if 0
static int
compare_addr (const void *a, const void *b)
{
	return *(const void **)a - *(const void **)b;
}
#endif

/* sort the addresses in array in increasing order */
static void
sort_addresses (void **array, int size)
{
	/*
	 * qsort is slower as predicted.
	 * qsort (array, size, sizeof (gpointer), compare_addr);
	 * return;
	 */
	int gap = size;
	int swapped, end;
	while (TRUE) {
		int i;
		gap = new_gap (gap);
		swapped = FALSE;
		end = size - gap;
		for (i = 0; i < end; i++) {
			int j = i + gap;
			if (array [i] > array [j]) {
				void* val = array [i];
				array [i] = array [j];
				array [j] = val;
				swapped = TRUE;
			}
		}
		if (gap == 1 && !swapped)
			break;
	}
}

static void
print_nursery_gaps (void* start_nursery, void *end_nursery)
{
	int i;
	gpointer first = start_nursery;
	gpointer next;
	for (i = 0; i < next_pin_slot; ++i) {
		next = pin_queue [i];
		fprintf (gc_debug_file, "Nursery range: %p-%p, size: %zd\n", first, next, (char*)next-(char*)first);
		first = next;
	}
	next = end_nursery;
	fprintf (gc_debug_file, "Nursery range: %p-%p, size: %zd\n", first, next, (char*)next-(char*)first);
}

/* reduce the info in the pin queue, removing duplicate pointers and sorting them */
static void
optimize_pin_queue (int start_slot)
{
	void **start, **cur, **end;
	/* sort and uniq pin_queue: we just sort and we let the rest discard multiple values */
	/* it may be better to keep ranges of pinned memory instead of individually pinning objects */
	DEBUG (5, fprintf (gc_debug_file, "Sorting pin queue, size: %d\n", next_pin_slot));
	if ((next_pin_slot - start_slot) > 1)
		sort_addresses (pin_queue + start_slot, next_pin_slot - start_slot);
	start = cur = pin_queue + start_slot;
	end = pin_queue + next_pin_slot;
	while (cur < end) {
		*start = *cur++;
		while (*start == *cur && cur < end)
			cur++;
		start++;
	};
	next_pin_slot = start - pin_queue;
	DEBUG (5, fprintf (gc_debug_file, "Pin queue reduced to size: %d\n", next_pin_slot));
	//DEBUG (6, print_nursery_gaps (start_nursery, end_nursery));
	
}

static void
realloc_pin_queue (void)
{
	int new_size = pin_queue_size? pin_queue_size + pin_queue_size/2: 1024;
	void **new_pin = get_internal_mem (sizeof (void*) * new_size);
	memcpy (new_pin, pin_queue, sizeof (void*) * next_pin_slot);
	free_internal_mem (pin_queue);
	pin_queue = new_pin;
	pin_queue_size = new_size;
	DEBUG (4, fprintf (gc_debug_file, "Reallocated pin queue to size: %d\n", new_size));
}

/* 
 * Scan the memory between start and end and queue values which could be pointers
 * to the area between start_nursery and end_nursery for later consideration.
 * Typically used for thread stacks.
 */
static void
conservatively_pin_objects_from (void **start, void **end, void *start_nursery, void *end_nursery)
{
	int count = 0;
	while (start < end) {
		if (*start >= start_nursery && *start < end_nursery) {
			/*
			 * *start can point to the middle of an object
			 * note: should we handle pointing at the end of an object?
			 * pinning in C# code disallows pointing at the end of an object
			 * but there is some small chance that an optimizing C compiler
			 * may keep the only reference to an object by pointing
			 * at the end of it. We ignore this small chance for now.
			 * Pointers to the end of an object are indistinguishable
			 * from pointers to the start of the next object in memory
			 * so if we allow that we'd need to pin two objects...
			 * We queue the pointer in an array, the
			 * array will then be sorted and uniqued. This way
			 * we can coalesce several pinning pointers and it should
			 * be faster since we'd do a memory scan with increasing
			 * addresses. Note: we can align the address to the allocation
			 * alignment, so the unique process is more effective.
			 */
			mword addr = (mword)*start;
			addr &= ~(ALLOC_ALIGN - 1);
			if (next_pin_slot >= pin_queue_size)
				realloc_pin_queue ();
			pin_queue [next_pin_slot++] = (void*)addr;
			DEBUG (6, if (count) fprintf (gc_debug_file, "Pinning address %p\n", (void*)addr));
			count++;
		}
		start++;
	}
	DEBUG (7, if (count) fprintf (gc_debug_file, "found %d potential pinned heap pointers\n", count));

#ifdef HAVE_VALGRIND_MEMCHECK_H
	/*
	 * The pinning addresses might come from undefined memory, this is normal. Since they
	 * are used in lots of functions, we make the memory defined here instead of having
	 * to add a supression for those functions.
	 */
	VALGRIND_MAKE_MEM_DEFINED (pin_queue, next_pin_slot * sizeof (pin_queue [0]));
#endif
}

/* 
 * If generation is 0, just mark objects in the nursery, the others we don't care,
 * since they are not going to move anyway.
 * There are different areas that are scanned for pinned pointers:
 * *) the thread stacks (when jit support is ready only the unmanaged frames)
 * *) the pinned handle table
 * *) the pinned roots
 *
 * Note: when we'll use a write barrier for old to new gen references, we need to
 * keep track of old gen objects that point to pinned new gen objects because in that
 * case the referenced object will be moved maybe at the next collection, but there
 * is no write in the old generation area where the pinned object is referenced
 * and we may not consider it as reachable.
 */
static void
mark_pinned_objects (int generation)
{
}

/*
 * Debugging function: find in the conservative roots where @obj is being pinned.
 */
static void
find_pinning_reference (char *obj, size_t size)
{
	RootRecord *root;
	int i;
	char *endobj = obj + size;
	for (i = 0; i < roots_hash_size; ++i) {
		for (root = roots_hash [i]; root; root = root->next) {
			/* if desc is non-null it has precise info */
			if (!root->root_desc) {
				char ** start = (char**)root->start_root;
				while (start < (char**)root->end_root) {
					if (*start >= obj && *start < endobj) {
						DEBUG (0, fprintf (gc_debug_file, "Object %p referenced in pinned roots %p-%p (at %p in record %p)\n", obj, root->start_root, root->end_root, start, root));
					}
					start++;
				}
			}
		}
	}
	find_pinning_ref_from_thread (obj, size);
}

/*
 * The first thing we do in a collection is to identify pinned objects.
 * This function considers all the areas of memory that need to be
 * conservatively scanned.
 */
static void
pin_from_roots (void *start_nursery, void *end_nursery)
{
	RootRecord *root;
	int i;
	DEBUG (3, fprintf (gc_debug_file, "Scanning pinned roots (%d bytes, %d entries)\n", (int)roots_size, num_roots_entries));
	/* objects pinned from the API are inside these roots */
	for (i = 0; i < roots_hash_size; ++i) {
		for (root = roots_hash [i]; root; root = root->next) {
			/* if desc is non-null it has precise info */
			if (root->root_desc)
				continue;
			DEBUG (6, fprintf (gc_debug_file, "Pinned roots %p-%p\n", root->start_root, root->end_root));
			conservatively_pin_objects_from ((void**)root->start_root, (void**)root->end_root, start_nursery, end_nursery);
		}
	}
	/* now deal with the thread stacks
	 * in the future we should be able to conservatively scan only:
	 * *) the cpu registers
	 * *) the unmanaged stack frames
	 * *) the _last_ managed stack frame
	 * *) pointers slots in managed frames
	 */
	pin_thread_data (start_nursery, end_nursery);
}

/*
 * The memory area from start_root to end_root contains pointers to objects.
 * Their position is precisely described by @desc (this means that the pointer
 * can be either NULL or the pointer to the start of an object).
 * This functions copies them to to_space updates them.
 */
static void
precisely_scan_objects_from (void** start_root, void** end_root, char* n_start, char *n_end, mword desc)
{
	switch (desc & ROOT_DESC_TYPE_MASK) {
	case ROOT_DESC_BITMAP:
		desc >>= ROOT_DESC_TYPE_SHIFT;
		while (desc) {
			if ((desc & 1) && *start_root) {
				*start_root = copy_object (*start_root, n_start, n_end);
				DEBUG (9, fprintf (gc_debug_file, "Overwrote root at %p with %p\n", start_root, *start_root));	\
			}
			desc >>= 1;
			start_root++;
		}
		return;
	case ROOT_DESC_RUN_LEN:
	case ROOT_DESC_LARGE_BITMAP:
		g_assert_not_reached ();
	}
}

static Fragment*
alloc_fragment (void)
{
	Fragment *frag = fragment_freelist;
	if (frag) {
		fragment_freelist = frag->next;
		frag->next = NULL;
		return frag;
	}
	frag = get_internal_mem (sizeof (Fragment));
	frag->next = NULL;
	return frag;
}

/*
 * Allocate and setup the data structures needed to be able to allocate objects
 * in the nursery. The nursery is stored in nursery_section.
 */
static void
alloc_nursery (void)
{
	GCMemSection *section;
	char *data;
	int scan_starts;
	Fragment *frag;

	if (nursery_section)
		return;
	DEBUG (2, fprintf (gc_debug_file, "Allocating nursery size: %zd\n", nursery_size));
	/* later we will alloc a larger area for the nursery but only activate
	 * what we need. The rest will be used as expansion if we have too many pinned
	 * objects in the existing nursery.
	 */
	/* FIXME: handle OOM */
	section = get_internal_mem (sizeof (GCMemSection));
	data = get_os_memory (nursery_size, TRUE);
	nursery_start = nursery_next = data;
	nursery_real_end = data + nursery_size;
	UPDATE_HEAP_BOUNDARIES (nursery_start, nursery_real_end);
	total_alloc += nursery_size;
	DEBUG (4, fprintf (gc_debug_file, "Expanding heap size: %zd, total: %zd\n", nursery_size, total_alloc));
	section->data = section->next_data = data;
	section->size = nursery_size;
	section->end_data = nursery_real_end;
	scan_starts = nursery_size / SCAN_START_SIZE;
	section->scan_starts = get_internal_mem (sizeof (char*) * scan_starts);
	section->num_scan_start = scan_starts;
	section->role = MEMORY_ROLE_GEN0;

	/* add to the section list */
	section->next = section_list;
	section_list = section;

	nursery_section = section;

	/* Setup the single first large fragment */
	frag = alloc_fragment ();
	frag->fragment_start = nursery_start;
	frag->fragment_limit = nursery_start;
	frag->fragment_end = nursery_real_end;
	nursery_frag_real_end = nursery_real_end;
	/* FIXME: frag here is lost */
}

/*
 * Update roots in the old generation. Since we currently don't have the
 * info from the write barriers, we just scan all the objects.
 */
static void
scan_old_generation (char *start, char* end)
{
	GCMemSection *section;
	FinalizeEntry *fin;
	LOSObject *big_object;
	char *p;
	
	for (section = section_list; section; section = section->next) {
		if (section == nursery_section)
			continue;
		DEBUG (2, fprintf (gc_debug_file, "Scan of old section: %p-%p, size: %d\n", section->data, section->next_data, (int)(section->next_data - section->data)));
		/* we have to deal with zeroed holes in old generation (truncated strings ...) */
		p = section->data;
		while (p < section->next_data) {
			if (!*(void**)p) {
				p += ALLOC_ALIGN;
				continue;
			}
			DEBUG (8, fprintf (gc_debug_file, "Precise old object scan of %p (%s)\n", p, safe_name (p)));
			p = scan_object (p, start, end);
		}
	}
	/* scan the old object space, too */
	for (big_object = los_object_list; big_object; big_object = big_object->next) {
		DEBUG (5, fprintf (gc_debug_file, "Scan of big object: %p (%s), size: %zd\n", big_object->data, safe_name (big_object->data), big_object->size));
		scan_object (big_object->data, start, end);
	}
	/* scan the list of objects ready for finalization */
	for (fin = fin_ready_list; fin; fin = fin->next) {
		DEBUG (5, fprintf (gc_debug_file, "Scan of fin ready object: %p (%s)\n", fin->object, safe_name (fin->object)));
		fin->object = copy_object (fin->object, start, end);
	}
}

static mword fragment_total = 0;
/*
 * We found a fragment of free memory in the nursery: memzero it and if
 * it is big enough, add it to the list of fragments that can be used for
 * allocation.
 */
static void
add_nursery_frag (size_t frag_size, char* frag_start, char* frag_end)
{
	Fragment *fragment;
	DEBUG (4, fprintf (gc_debug_file, "Found empty fragment: %p-%p, size: %zd\n", frag_start, frag_end, frag_size));
	/* memsetting just the first chunk start is bound to provide better cache locality */
	if (nursery_clear_policy == CLEAR_AT_GC)
		memset (frag_start, 0, frag_size);
	/* Not worth dealing with smaller fragments: need to tune */
	if (frag_size >= FRAGMENT_MIN_SIZE) {
		fragment = alloc_fragment ();
		fragment->fragment_start = frag_start;
		fragment->fragment_limit = frag_start;
		fragment->fragment_end = frag_end;
		fragment->next = nursery_fragments;
		nursery_fragments = fragment;
		fragment_total += frag_size;
	} else {
		/* Clear unused fragments, pinning depends on this */
		memset (frag_start, 0, frag_size);
	}
}

static int
scan_needed_big_objects (char *start_addr, char *end_addr)
{
	LOSObject *big_object;
	int count = 0;
	for (big_object = los_object_list; big_object; big_object = big_object->next) {
		if (!big_object->scanned && object_is_pinned (big_object->data)) {
			DEBUG (5, fprintf (gc_debug_file, "Scan of big object: %p (%s), size: %zd\n", big_object->data, safe_name (big_object->data), big_object->size));
			scan_object (big_object->data, start_addr, end_addr);
			big_object->scanned = TRUE;
			count++;
		}
	}
	return count;
}

static void
drain_gray_stack (char *start_addr, char *end_addr)
{
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	int fin_ready, bigo_scanned_num;
	char *gray_start;

	/*
	 * We copied all the reachable objects. Now it's the time to copy
	 * the objects that were not referenced by the roots, but by the copied objects.
	 * we built a stack of objects pointed to by gray_start: they are
	 * additional roots and we may add more items as we go.
	 * We loop until gray_start == gray_objects which means no more objects have
	 * been added. Note this is iterative: no recursion is involved.
	 * We need to walk the LO list as well in search of marked big objects
	 * (use a flag since this is needed only on major collections). We need to loop
	 * here as well, so keep a counter of marked LO (increasing it in copy_object).
	 */
	TV_GETTIME (btv);
	gray_start = to_space;
	DEBUG (6, fprintf (gc_debug_file, "Precise scan of gray area: %p-%p, size: %d\n", gray_start, gray_objects, (int)(gray_objects - gray_start)));
	while (gray_start < gray_objects) {
		DEBUG (9, fprintf (gc_debug_file, "Precise gray object scan %p (%s)\n", gray_start, safe_name (gray_start)));
		gray_start = scan_object (gray_start, start_addr, end_addr);
	}
	TV_GETTIME (atv);
	DEBUG (2, fprintf (gc_debug_file, "Gray stack scan: %d usecs\n", TV_ELAPSED (btv, atv)));
	//scan_old_generation (start_addr, end_addr);
	DEBUG (2, fprintf (gc_debug_file, "Old generation done\n"));
	/* walk the finalization queue and move also the objects that need to be
	 * finalized: use the finalized objects as new roots so the objects they depend
	 * on are also not reclaimed. As with the roots above, only objects in the nursery
	 * are marked/copied.
	 * We need a loop here, since objects ready for finalizers may reference other objects
	 * that are fin-ready. Speedup with a flag?
	 */
	do {
		fin_ready = num_ready_finalizers;
		finalize_in_range (start_addr, end_addr);
		bigo_scanned_num = scan_needed_big_objects (start_addr, end_addr);

		/* drain the new stack that might have been created */
		DEBUG (6, fprintf (gc_debug_file, "Precise scan of gray area post fin: %p-%p, size: %d\n", gray_start, gray_objects, (int)(gray_objects - gray_start)));
		while (gray_start < gray_objects) {
			DEBUG (9, fprintf (gc_debug_file, "Precise gray object scan %p (%s)\n", gray_start, safe_name (gray_start)));
			gray_start = scan_object (gray_start, start_addr, end_addr);
		}
	} while (fin_ready != num_ready_finalizers || bigo_scanned_num);

	DEBUG (2, fprintf (gc_debug_file, "Copied to old space: %d bytes\n", (int)(gray_objects - to_space)));
	to_space = gray_start;
	to_space_section->next_data = to_space;

	/*
	 * handle disappearing links
	 * Note we do this after checking the finalization queue because if an object
	 * survives (at least long enough to be finalized) we don't clear the link.
	 * This also deals with a possible issue with the monitor reclamation: with the Boehm
	 * GC a finalized object my lose the monitor because it is cleared before the finalizer is
	 * called.
	 */
	null_link_in_range (start_addr, end_addr);
	TV_GETTIME (btv);
	DEBUG (2, fprintf (gc_debug_file, "Finalize queue handling scan: %d usecs\n", TV_ELAPSED (atv, btv)));
}

static int last_num_pinned = 0;

static void
build_nursery_fragments (int start_pin, int end_pin)
{
	char *frag_start, *frag_end;
	size_t frag_size;
	int i;

	/* FIXME: handle non-NULL fragment_freelist */
	fragment_freelist = nursery_fragments;
	nursery_fragments = NULL;
	frag_start = nursery_start;
	fragment_total = 0;
	/* clear scan starts */
	memset (nursery_section->scan_starts, 0, nursery_section->num_scan_start * sizeof (gpointer));
	for (i = start_pin; i < end_pin; ++i) {
		frag_end = pin_queue [i];
		/* remove the pin bit from pinned objects */
		unpin_object (frag_end);
		nursery_section->scan_starts [((char*)frag_end - (char*)nursery_section->data)/SCAN_START_SIZE] = frag_end;
		frag_size = frag_end - frag_start;
		if (frag_size)
			add_nursery_frag (frag_size, frag_start, frag_end);
		frag_size = safe_object_get_size ((MonoObject*)pin_queue [i]);
		frag_size += ALLOC_ALIGN - 1;
		frag_size &= ~(ALLOC_ALIGN - 1);
		frag_start = (char*)pin_queue [i] + frag_size;
		/* 
		 * pin_queue [i] might point to a half-constructed string or vector whose
		 * length field is not set. In that case, frag_start points inside the 
		 * (zero initialized) object. Find the end of the object by scanning forward.
		 * 
		 */
		if (is_maybe_half_constructed (pin_queue [i])) {
			char *tlab_end;

			/* This is also hit for zero length arrays/strings */

			/* Find the end of the TLAB which contained this allocation */
			tlab_end = find_tlab_next_from_address (pin_queue [i]);

			if (tlab_end) {
				while ((frag_start < tlab_end) && *(mword*)frag_start == 0)
					frag_start += sizeof (mword);
			} else {
				/*
				 * FIXME: The object is either not allocated in a TLAB, or it isn't a
				 * half constructed object.
				 */
			}
		}
	}
	nursery_last_pinned_end = frag_start;
	frag_end = nursery_real_end;
	frag_size = frag_end - frag_start;
	if (frag_size)
		add_nursery_frag (frag_size, frag_start, frag_end);
	if (!nursery_fragments) {
		DEBUG (1, fprintf (gc_debug_file, "Nursery fully pinned (%d)\n", end_pin - start_pin));
		for (i = start_pin; i < end_pin; ++i) {
			DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", pin_queue [i], safe_name (pin_queue [i]), safe_object_get_size (pin_queue [i])));
		}
		degraded_mode = 1;
	}

	/* Clear TLABs for all threads */
	clear_tlabs ();
}

/* FIXME: later reduce code duplication here with the above
 * We don't keep track of section fragments for non-nursery sections yet, so
 * just memset to 0.
 */
static void
build_section_fragments (GCMemSection *section)
{
	int i;
	char *frag_start, *frag_end;
	size_t frag_size;

	/* clear scan starts */
	memset (section->scan_starts, 0, section->num_scan_start * sizeof (gpointer));
	frag_start = section->data;
	section->next_data = section->data;
	for (i = section->pin_queue_start; i < section->pin_queue_end; ++i) {
		frag_end = pin_queue [i];
		/* remove the pin bit from pinned objects */
		unpin_object (frag_end);
		if (frag_end >= section->data + section->size) {
			frag_end = section->data + section->size;
		} else {
			section->scan_starts [((char*)frag_end - (char*)section->data)/SCAN_START_SIZE] = frag_end;
		}
		frag_size = frag_end - frag_start;
		if (frag_size)
			memset (frag_start, 0, frag_size);
		frag_size = safe_object_get_size ((MonoObject*)pin_queue [i]);
		frag_size += ALLOC_ALIGN - 1;
		frag_size &= ~(ALLOC_ALIGN - 1);
		frag_start = (char*)pin_queue [i] + frag_size;
		section->next_data = MAX (section->next_data, frag_start);
	}
	frag_end = section->end_data;
	frag_size = frag_end - frag_start;
	if (frag_size)
		memset (frag_start, 0, frag_size);
}

static void
scan_from_registered_roots (char *addr_start, char *addr_end)
{
	int i;
	RootRecord *root;
	for (i = 0; i < roots_hash_size; ++i) {
		for (root = roots_hash [i]; root; root = root->next) {
			/* if desc is non-null it has precise info */
			if (!root->root_desc)
				continue;
			DEBUG (6, fprintf (gc_debug_file, "Precise root scan %p-%p (desc: %p)\n", root->start_root, root->end_root, (void*)root->root_desc));
			precisely_scan_objects_from ((void**)root->start_root, (void**)root->end_root, addr_start, addr_end, root->root_desc);
		}
	}
}

/*
 * Collect objects in the nursery.
 */
static void
collect_nursery (size_t requested_size)
{
	GCMemSection *section;
	size_t max_garbage_amount;
	int i;
	char *orig_nursery_next;
	Fragment *frag;
	TV_DECLARE (all_atv);
	TV_DECLARE (all_btv);
	TV_DECLARE (atv);
	TV_DECLARE (btv);

	degraded_mode = 0;
	orig_nursery_next = nursery_next;
	nursery_next = MAX (nursery_next, nursery_last_pinned_end);
	/* FIXME: optimize later to use the higher address where an object can be present */
	nursery_next = MAX (nursery_next, nursery_real_end);

	if (consistency_check_at_minor_collection)
		check_consistency ();

	DEBUG (1, fprintf (gc_debug_file, "Start nursery collection %d %p-%p, size: %d\n", num_minor_gcs, nursery_start, nursery_next, (int)(nursery_next - nursery_start)));
	max_garbage_amount = nursery_next - nursery_start;

	/* Clear all remaining nursery fragments, pinning depends on this */
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		g_assert (orig_nursery_next <= nursery_frag_real_end);
		memset (orig_nursery_next, 0, nursery_frag_real_end - orig_nursery_next);
		for (frag = nursery_fragments; frag; frag = frag->next) {
			memset (frag->fragment_start, 0, frag->fragment_end - frag->fragment_start);
		}
	}

	/* 
	 * not enough room in the old generation to store all the possible data from 
	 * the nursery in a single continuous space.
	 * We reset to_space if we allocated objects in degraded mode.
	 */
	if (to_space_section)
		to_space = gray_objects = to_space_section->next_data;
	if ((to_space_end - to_space) < max_garbage_amount) {
		section = alloc_section (nursery_section->size * 4);
		g_assert (nursery_section->size >= max_garbage_amount);
		to_space = gray_objects = section->next_data;
		to_space_end = section->end_data;
		to_space_section = section;
	}
	DEBUG (2, fprintf (gc_debug_file, "To space setup: %p-%p in section %p\n", to_space, to_space_end, to_space_section));
	nursery_section->next_data = nursery_next;

	num_minor_gcs++;
	mono_stats.minor_gc_count ++;
	/* world must be stopped already */
	TV_GETTIME (all_atv);
	TV_GETTIME (atv);
	/* pin from pinned handles */
	pin_from_roots (nursery_start, nursery_next);
	/* identify pinned objects */
	optimize_pin_queue (0);
	next_pin_slot = pin_objects_from_addresses (nursery_section, pin_queue, pin_queue + next_pin_slot, nursery_start, nursery_next);
	TV_GETTIME (btv);
	DEBUG (2, fprintf (gc_debug_file, "Finding pinned pointers: %d in %d usecs\n", next_pin_slot, TV_ELAPSED (atv, btv)));
	DEBUG (4, fprintf (gc_debug_file, "Start scan with %d pinned objects\n", next_pin_slot));

	/* 
	 * walk all the roots and copy the young objects to the old generation,
	 * starting from to_space
	 */

	scan_from_remsets (nursery_start, nursery_next);
	/* we don't have complete write barrier yet, so we scan all the old generation sections */
	TV_GETTIME (atv);
	DEBUG (2, fprintf (gc_debug_file, "Old generation scan: %d usecs\n", TV_ELAPSED (btv, atv)));
	/* FIXME: later scan also alloc_pinned objects */

	/* the pinned objects are roots */
	for (i = 0; i < next_pin_slot; ++i) {
		DEBUG (6, fprintf (gc_debug_file, "Precise object scan %d of pinned %p (%s)\n", i, pin_queue [i], safe_name (pin_queue [i])));
		scan_object (pin_queue [i], nursery_start, nursery_next);
	}
	/* registered roots, this includes static fields */
	scan_from_registered_roots (nursery_start, nursery_next);
	TV_GETTIME (btv);
	DEBUG (2, fprintf (gc_debug_file, "Root scan: %d usecs\n", TV_ELAPSED (atv, btv)));

	drain_gray_stack (nursery_start, nursery_next);

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	build_nursery_fragments (0, next_pin_slot);
	TV_GETTIME (atv);
	DEBUG (2, fprintf (gc_debug_file, "Fragment creation: %d usecs, %zd bytes available\n", TV_ELAPSED (btv, atv), fragment_total));

	TV_GETTIME (all_btv);
	mono_stats.minor_gc_time_usecs += TV_ELAPSED (all_atv, all_btv);

	/* prepare the pin queue for the next collection */
	last_num_pinned = next_pin_slot;
	next_pin_slot = 0;
	if (fin_ready_list) {
		DEBUG (4, fprintf (gc_debug_file, "Finalizer-thread wakeup: ready %d\n", num_ready_finalizers));
		mono_gc_finalize_notify ();
	}
}

static void
major_collection (void)
{
	GCMemSection *section, *prev_section;
	LOSObject *bigobj, *prevbo;
	int i;
	PinnedChunk *chunk;
	FinalizeEntry *fin;
	Fragment *frag;
	int count;
	TV_DECLARE (all_atv);
	TV_DECLARE (all_btv);
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	/* FIXME: only use these values for the precise scan
	 * note that to_space pointers should be excluded anyway...
	 */
	char *heap_start = NULL;
	char *heap_end = (char*)-1;
	size_t copy_space_required = 0;

	degraded_mode = 0;
	DEBUG (1, fprintf (gc_debug_file, "Start major collection %d\n", num_major_gcs));
	num_major_gcs++;
	mono_stats.major_gc_count ++;

	/* Clear all remaining nursery fragments, pinning depends on this */
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		g_assert (nursery_next <= nursery_frag_real_end);
		memset (nursery_next, 0, nursery_frag_real_end - nursery_next);
		for (frag = nursery_fragments; frag; frag = frag->next) {
			memset (frag->fragment_start, 0, frag->fragment_end - frag->fragment_start);
		}
	}

	/* 
	 * FIXME: implement Mark/Compact
	 * Until that is done, we can just apply mostly the same alg as for the nursery:
	 * this means we need a big section to potentially copy all the other sections, so
	 * it is not ideal specially with large heaps.
	 */
	if (g_getenv ("MONO_GC_NO_MAJOR")) {
		collect_nursery (0);
		return;
	}
	TV_GETTIME (all_atv);
	/* FIXME: make sure the nursery next_data ptr is updated */
	nursery_section->next_data = nursery_real_end;
	/* we should also coalesce scanning from sections close to each other
	 * and deal with pointers outside of the sections later.
	 */
	/* The remsets are not useful for a major collection */
	clear_remsets ();
	/* world must be stopped already */
	TV_GETTIME (atv);
	DEBUG (6, fprintf (gc_debug_file, "Pinning from sections\n"));
	for (section = section_list; section; section = section->next) {
		section->pin_queue_start = count = section->pin_queue_end = next_pin_slot;
		pin_from_roots (section->data, section->next_data);
		if (count != next_pin_slot) {
			int reduced_to;
			optimize_pin_queue (count);
			DEBUG (6, fprintf (gc_debug_file, "Found %d pinning addresses in section %p (%d-%d)\n", next_pin_slot - count, section, count, next_pin_slot));
			reduced_to = pin_objects_from_addresses (section, pin_queue + count, pin_queue + next_pin_slot, section->data, section->next_data);
			section->pin_queue_end = next_pin_slot = count + reduced_to;
		}
		copy_space_required += (char*)section->next_data - (char*)section->data;
	}
	/* identify possible pointers to the insize of large objects */
	DEBUG (6, fprintf (gc_debug_file, "Pinning from large objects\n"));
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next) {
		count = next_pin_slot;
		pin_from_roots (bigobj->data, (char*)bigobj->data + bigobj->size);
		/* FIXME: this is only valid until we don't optimize the pin queue midway */
		if (next_pin_slot != count) {
			next_pin_slot = count;
			pin_object (bigobj->data);
			DEBUG (6, fprintf (gc_debug_file, "Marked large object %p (%s) size: %zd from roots\n", bigobj->data, safe_name (bigobj->data), bigobj->size));
		}
	}
	/* look for pinned addresses for pinned-alloc objects */
	DEBUG (6, fprintf (gc_debug_file, "Pinning from pinned-alloc objects\n"));
	for (chunk = pinned_chunk_list; chunk; chunk = chunk->next) {
		count = next_pin_slot;
		pin_from_roots (chunk->start_data, (char*)chunk + chunk->num_pages * FREELIST_PAGESIZE);
		/* FIXME: this is only valid until we don't optimize the pin queue midway */
		if (next_pin_slot != count) {
			mark_pinned_from_addresses (chunk, pin_queue + count, pin_queue + next_pin_slot);
			next_pin_slot = count;
		}
	}

	TV_GETTIME (btv);
	DEBUG (2, fprintf (gc_debug_file, "Finding pinned pointers: %d in %d usecs\n", next_pin_slot, TV_ELAPSED (atv, btv)));
	DEBUG (4, fprintf (gc_debug_file, "Start scan with %d pinned objects\n", next_pin_slot));

	/* allocate the big to space */
	DEBUG (4, fprintf (gc_debug_file, "Allocate tospace for size: %zd\n", copy_space_required));
	section = alloc_section (copy_space_required);
	to_space = gray_objects = section->next_data;
	to_space_end = section->end_data;
	to_space_section = section;

	/* the old generation doesn't need to be scanned (no remembered sets or card
	 * table needed either): the only objects that must survive are those pinned and
	 * those referenced by the precise roots.
	 * mark any section without pinned objects, so we can free it since we will be able to
	 * move all the objects.
	 */
	/* the pinned objects are roots (big objects are included in this list, too) */
	for (i = 0; i < next_pin_slot; ++i) {
		DEBUG (6, fprintf (gc_debug_file, "Precise object scan %d of pinned %p (%s)\n", i, pin_queue [i], safe_name (pin_queue [i])));
		scan_object (pin_queue [i], heap_start, heap_end);
	}
	/* registered roots, this includes static fields */
	scan_from_registered_roots (heap_start, heap_end);

	/* scan the list of objects ready for finalization */
	for (fin = fin_ready_list; fin; fin = fin->next) {
		DEBUG (5, fprintf (gc_debug_file, "Scan of fin ready object: %p (%s)\n", fin->object, safe_name (fin->object)));
		fin->object = copy_object (fin->object, heap_start, heap_end);
	}
	TV_GETTIME (atv);
	DEBUG (2, fprintf (gc_debug_file, "Root scan: %d usecs\n", TV_ELAPSED (btv, atv)));

	/* we need to go over the big object list to see if any was marked and scan it
	 * And we need to make this in a loop, considering that objects referenced by finalizable
	 * objects could reference big objects (this happens in drain_gray_stack ())
	 */
	scan_needed_big_objects (heap_start, heap_end);
	/* all the objects in the heap */
	drain_gray_stack (heap_start, heap_end);

	/* sweep the big objects list */
	prevbo = NULL;
	for (bigobj = los_object_list; bigobj;) {
		if (object_is_pinned (bigobj->data)) {
			unpin_object (bigobj->data);
			bigobj->scanned = FALSE;
		} else {
			LOSObject *to_free;
			/* not referenced anywhere, so we can free it */
			if (prevbo)
				prevbo->next = bigobj->next;
			else
				los_object_list = bigobj->next;
			to_free = bigobj;
			bigobj = bigobj->next;
			free_large_object (to_free);
			continue;
		}
		prevbo = bigobj;
		bigobj = bigobj->next;
	}
	/* unpin objects from the pinned chunks and free the unmarked ones */
	sweep_pinned_objects ();

	/* free the unused sections */
	prev_section = NULL;
	for (section = section_list; section;) {
		/* to_space doesn't need handling here and the nursery is special */
		if (section == to_space_section || section == nursery_section) {
			prev_section = section;
			section = section->next;
			continue;
		}
		/* no pinning object, so the section is free */
		if (section->pin_queue_start == section->pin_queue_end) {
			GCMemSection *to_free;
			if (prev_section)
				prev_section->next = section->next;
			else
				section_list = section->next;
			to_free = section;
			section = section->next;
			free_mem_section (to_free);
			continue;
		} else {
			DEBUG (6, fprintf (gc_debug_file, "Section %p has still pinned objects (%d)\n", section, section->pin_queue_end - section->pin_queue_start));
			build_section_fragments (section);
		}
		prev_section = section;
		section = section->next;
	}

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	build_nursery_fragments (nursery_section->pin_queue_start, nursery_section->pin_queue_end);

	TV_GETTIME (all_btv);
	mono_stats.major_gc_time_usecs += TV_ELAPSED (all_atv, all_btv);
	/* prepare the pin queue for the next collection */
	next_pin_slot = 0;
	if (fin_ready_list) {
		DEBUG (4, fprintf (gc_debug_file, "Finalizer-thread wakeup: ready %d\n", num_ready_finalizers));
		mono_gc_finalize_notify ();
	}
}

/*
 * Allocate a new section of memory to be used as old generation.
 */
static GCMemSection*
alloc_section (size_t size)
{
	GCMemSection *section;
	char *data;
	int scan_starts;
	size_t new_size = next_section_size;

	if (size > next_section_size) {
		new_size = size;
		new_size += pagesize - 1;
		new_size &= ~(pagesize - 1);
	}
	section_size_used++;
	if (section_size_used > 3) {
		section_size_used = 0;
		next_section_size *= 2;
		if (next_section_size > max_section_size)
			next_section_size = max_section_size;
	}
	section = get_internal_mem (sizeof (GCMemSection));
	data = get_os_memory (new_size, TRUE);
	section->data = section->next_data = data;
	section->size = new_size;
	section->end_data = data + new_size;
	UPDATE_HEAP_BOUNDARIES (data, section->end_data);
	total_alloc += new_size;
	DEBUG (2, fprintf (gc_debug_file, "Expanding heap size: %zd, total: %zd\n", new_size, total_alloc));
	section->data = data;
	section->size = new_size;
	scan_starts = new_size / SCAN_START_SIZE;
	section->scan_starts = get_internal_mem (sizeof (char*) * scan_starts);
	section->num_scan_start = scan_starts;
	section->role = MEMORY_ROLE_GEN1;

	/* add to the section list */
	section->next = section_list;
	section_list = section;

	return section;
}

static void
free_mem_section (GCMemSection *section)
{
	char *data = section->data;
	size_t size = section->size;
	DEBUG (2, fprintf (gc_debug_file, "Freed section %p, size %zd\n", data, size));
	free_os_memory (data, size);
	free_internal_mem (section);
	total_alloc -= size;
}

/*
 * When deciding if it's better to collect or to expand, keep track
 * of how much garbage was reclaimed with the last collection: if it's too
 * little, expand.
 * This is called when we could not allocate a small object.
 */
static void __attribute__((noinline))
minor_collect_or_expand_inner (size_t size)
{
	int do_minor_collection = 1;

	if (!nursery_section) {
		alloc_nursery ();
		return;
	}
	if (do_minor_collection) {
		stop_world ();
		collect_nursery (size);
		DEBUG (2, fprintf (gc_debug_file, "Heap size: %zd, LOS size: %zd\n", total_alloc, los_memory_usage));
		restart_world ();
		/* this also sets the proper pointers for the next allocation */
		if (!search_fragment_for_size (size)) {
			int i;
			/* TypeBuilder and MonoMethod are killing mcs with fragmentation */
			DEBUG (1, fprintf (gc_debug_file, "nursery collection didn't find enough room for %zd alloc (%d pinned)\n", size, last_num_pinned));
			for (i = 0; i < last_num_pinned; ++i) {
				DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", pin_queue [i], safe_name (pin_queue [i]), safe_object_get_size (pin_queue [i])));
			}
			degraded_mode = 1;
			/* This is needed by collect_nursery () to calculate nursery_last_allocated */
			nursery_next = nursery_frag_real_end = NULL;
		}
	}
	//report_internal_mem_usage ();
}

/*
 * ######################################################################
 * ########  Memory allocation from the OS
 * ######################################################################
 * This section of code deals with getting memory from the OS and
 * allocating memory for GC-internal data structures.
 * Internal memory can be handled with a freelist for small objects.
 */

/*
 * Allocate a big chunk of memory from the OS (usually 64KB to several megabytes).
 * This must not require any lock.
 */
static void*
get_os_memory (size_t size, int activate)
{
	void *ptr;
	unsigned long prot_flags = activate? MONO_MMAP_READ|MONO_MMAP_WRITE: MONO_MMAP_NONE;

	prot_flags |= MONO_MMAP_PRIVATE | MONO_MMAP_ANON;
	size += pagesize - 1;
	size &= ~(pagesize - 1);
	ptr = mono_valloc (0, size, prot_flags);
	return ptr;
}

/*
 * Free the memory returned by get_os_memory (), returning it to the OS.
 */
static void
free_os_memory (void *addr, size_t size)
{
	munmap (addr, size);
}

/*
 * Debug reporting.
 */
static void
report_pinned_chunk (PinnedChunk *chunk, int seq) {
	void **p;
	int i, free_pages, num_free, free_mem;
	free_pages = 0;
	for (i = 0; i < chunk->num_pages; ++i) {
		if (!chunk->page_sizes [i])
			free_pages++;
	}
	printf ("Pinned chunk %d at %p, size: %d, pages: %d, free: %d\n", seq, chunk, chunk->num_pages * FREELIST_PAGESIZE, chunk->num_pages, free_pages);
	free_mem = FREELIST_PAGESIZE * free_pages;
	for (i = 0; i < FREELIST_NUM_SLOTS; ++i) {
		if (!chunk->free_list [i])
			continue;
		num_free = 0;
		p = chunk->free_list [i];
		while (p) {
			num_free++;
			p = *p;
		}
		printf ("\tfree list of size %d, %d items\n", freelist_sizes [i], num_free);
		free_mem += freelist_sizes [i] * num_free;
	}
	printf ("\tfree memory in chunk: %d\n", free_mem);
}

/*
 * Debug reporting.
 */
static void
report_internal_mem_usage (void) {
	PinnedChunk *chunk;
	int i;
	printf ("Internal memory usage:\n");
	i = 0;
	for (chunk = internal_chunk_list; chunk; chunk = chunk->next) {
		report_pinned_chunk (chunk, i++);
	}
	printf ("Pinned memory usage:\n");
	i = 0;
	for (chunk = pinned_chunk_list; chunk; chunk = chunk->next) {
		report_pinned_chunk (chunk, i++);
	}
}

/*
 * the array of pointers from @start to @end contains conservative
 * pointers to objects inside @chunk: mark each referenced object
 * with the PIN bit.
 */
static void
mark_pinned_from_addresses (PinnedChunk *chunk, void **start, void **end)
{
	for (; start < end; start++) {
		char *addr = *start;
		int offset = (char*)addr - (char*)chunk;
		int page = offset / FREELIST_PAGESIZE;
		int obj_offset = page == 0? offset - ((char*)chunk->start_data - (char*)chunk): offset % FREELIST_PAGESIZE;
		int slot_size = chunk->page_sizes [page];
		void **ptr;
		/* the page is not allocated */
		if (!slot_size)
			continue;
		/* would be faster if we restrict the sizes to power of two,
		 * but that's a waste of memory: need to measure. it could reduce
		 * fragmentation since there are less pages needed, if for example
		 * someone interns strings of each size we end up with one page per
		 * interned string (still this is just ~40 KB): with more fine-grained sizes
		 * this increases the number of used pages.
		 */
		if (page == 0) {
			obj_offset /= slot_size;
			obj_offset *= slot_size;
			addr = (char*)chunk->start_data + obj_offset;
		} else {
			obj_offset /= slot_size;
			obj_offset *= slot_size;
			addr = (char*)chunk + page * FREELIST_PAGESIZE + obj_offset;
		}
		ptr = (void**)addr;
		/* if the vtable is inside the chunk it's on the freelist, so skip */
		if (*ptr && (*ptr < (void*)chunk->start_data || *ptr > (void*)((char*)chunk + chunk->num_pages * FREELIST_PAGESIZE))) {
			pin_object (addr);
			DEBUG (6, fprintf (gc_debug_file, "Marked pinned object %p (%s) from roots\n", addr, safe_name (addr)));
		}
	}
}

static void
sweep_pinned_objects (void)
{
	PinnedChunk *chunk;
	int i, obj_size;
	char *p, *endp;
	void **ptr;
	void *end_chunk;
	for (chunk = pinned_chunk_list; chunk; chunk = chunk->next) {
		end_chunk = (char*)chunk + chunk->num_pages * FREELIST_PAGESIZE;
		DEBUG (6, fprintf (gc_debug_file, "Sweeping pinned chunk %p (ranhe: %p-%p)\n", chunk, chunk->start_data, end_chunk));
		for (i = 0; i < chunk->num_pages; ++i) {
			obj_size = chunk->page_sizes [i];
			if (!obj_size)
				continue;
			p = i? (char*)chunk + i * FREELIST_PAGESIZE: chunk->start_data;
			endp = i? p + FREELIST_PAGESIZE: (char*)chunk + FREELIST_PAGESIZE;
			DEBUG (6, fprintf (gc_debug_file, "Page %d (size: %d, range: %p-%p)\n", i, obj_size, p, endp));
			while (p + obj_size <= endp) {
				ptr = (void**)p;
				DEBUG (9, fprintf (gc_debug_file, "Considering %p (vtable: %p)\n", ptr, *ptr));
				/* if the first word (the vtable) is outside the chunk we have an object */
				if (*ptr && (*ptr < (void*)chunk || *ptr >= end_chunk)) {
					if (object_is_pinned (ptr)) {
						unpin_object (ptr);
						DEBUG (6, fprintf (gc_debug_file, "Unmarked pinned object %p (%s)\n", ptr, safe_name (ptr)));
					} else {
						/* FIXME: add to freelist */
						DEBUG (6, fprintf (gc_debug_file, "Going to free unmarked pinned object %p (%s)\n", ptr, safe_name (ptr)));
					}
				}
				p += obj_size;
			}
		}
	}
}

/*
 * Find the slot number in the freelist for memory chunks that
 * can contain @size objects.
 */
static int
slot_for_size (size_t size)
{
	int slot;
	/* do a binary search or lookup table later. */
	for (slot = 0; slot < FREELIST_NUM_SLOTS; ++slot) {
		if (freelist_sizes [slot] >= size)
			return slot;
	}
	g_assert_not_reached ();
	return -1;
}

/*
 * Build a free list for @size memory chunks from the memory area between
 * start_page and end_page.
 */
static void
build_freelist (PinnedChunk *chunk, int slot, int size, char *start_page, char *end_page)
{
	void **p, **end;
	int count = 0;
	/*g_print ("building freelist for slot %d, size %d in %p\n", slot, size, chunk);*/
	p = (void**)start_page;
	end = (void**)(end_page - size);
	g_assert (!chunk->free_list [slot]);
	chunk->free_list [slot] = p;
	while ((char*)p + size <= (char*)end) {
		count++;
		*p = (void*)((char*)p + size);
		p = *p;
	}
	*p = NULL;
	/*g_print ("%d items created, max: %d\n", count, (end_page - start_page) / size);*/
}

static PinnedChunk*
alloc_pinned_chunk (size_t size)
{
	PinnedChunk *chunk;
	int offset;

	size += pagesize; /* at least one page */
	size += pagesize - 1;
	size &= ~(pagesize - 1);
	if (size < PINNED_CHUNK_MIN_SIZE * 2)
		size = PINNED_CHUNK_MIN_SIZE * 2;
	chunk = get_os_memory (size, TRUE);
	UPDATE_HEAP_BOUNDARIES (chunk, ((char*)chunk + size));
	total_alloc += size;

	/* setup the bookeeping fields */
	chunk->num_pages = size / FREELIST_PAGESIZE;
	offset = G_STRUCT_OFFSET (PinnedChunk, data);
	chunk->page_sizes = (void*)((char*)chunk + offset);
	offset += sizeof (int) * chunk->num_pages;
	offset += ALLOC_ALIGN - 1;
	offset &= ~(ALLOC_ALIGN - 1);
	chunk->free_list = (void*)((char*)chunk + offset);
	offset += sizeof (void*) * FREELIST_NUM_SLOTS;
	offset += ALLOC_ALIGN - 1;
	offset &= ~(ALLOC_ALIGN - 1);
	chunk->start_data = (void*)((char*)chunk + offset);

	/* allocate the first page to the freelist */
	chunk->page_sizes [0] = PINNED_FIRST_SLOT_SIZE;
	build_freelist (chunk, slot_for_size (PINNED_FIRST_SLOT_SIZE), PINNED_FIRST_SLOT_SIZE, chunk->start_data, ((char*)chunk + FREELIST_PAGESIZE));
	DEBUG (4, fprintf (gc_debug_file, "Allocated pinned chunk %p, size: %zd\n", chunk, size));
	min_pinned_chunk_addr = MIN (min_pinned_chunk_addr, (char*)chunk->start_data);
	max_pinned_chunk_addr = MAX (max_pinned_chunk_addr, ((char*)chunk + size));
	return chunk;
}

/* assumes freelist for slot is empty, so try to alloc a new page */
static void*
get_chunk_freelist (PinnedChunk *chunk, int slot)
{
	int i;
	void **p;
	p = chunk->free_list [slot];
	if (p) {
		chunk->free_list [slot] = *p;
		return p;
	}
	for (i = 0; i < chunk->num_pages; ++i) {
		int size;
		if (chunk->page_sizes [i])
			continue;
		size = freelist_sizes [slot];
		chunk->page_sizes [i] = size;
		build_freelist (chunk, slot, size, (char*)chunk + FREELIST_PAGESIZE * i, (char*)chunk + FREELIST_PAGESIZE * (i + 1));
		break;
	}
	/* try again */
	p = chunk->free_list [slot];
	if (p) {
		chunk->free_list [slot] = *p;
		return p;
	}
	return NULL;
}

static void*
alloc_from_freelist (size_t size)
{
	int slot;
	void *res = NULL;
	PinnedChunk *pchunk;
	slot = slot_for_size (size);
	/*g_print ("using slot %d for size %d (slot size: %d)\n", slot, size, freelist_sizes [slot]);*/
	g_assert (size <= freelist_sizes [slot]);
	for (pchunk = pinned_chunk_list; pchunk; pchunk = pchunk->next) {
		void **p = pchunk->free_list [slot];
		if (p) {
			/*g_print ("found freelist for slot %d in chunk %p, returning %p, next %p\n", slot, pchunk, p, *p);*/
			pchunk->free_list [slot] = *p;
			return p;
		}
	}
	for (pchunk = pinned_chunk_list; pchunk; pchunk = pchunk->next) {
		res = get_chunk_freelist (pchunk, slot);
		if (res)
			return res;
	}
	pchunk = alloc_pinned_chunk (size);
	/* FIXME: handle OOM */
	pchunk->next = pinned_chunk_list;
	pinned_chunk_list = pchunk;
	res = get_chunk_freelist (pchunk, slot);
	return res;
}

/* used for the GC-internal data structures */
/* FIXME: add support for bigger sizes by allocating more than one page
 * in the chunk.
 */
static void*
get_internal_mem (size_t size)
{
	return calloc (1, size);
#if 0
	int slot;
	void *res = NULL;
	PinnedChunk *pchunk;
	slot = slot_for_size (size);
	g_assert (size <= freelist_sizes [slot]);
	for (pchunk = internal_chunk_list; pchunk; pchunk = pchunk->next) {
		void **p = pchunk->free_list [slot];
		if (p) {
			pchunk->free_list [slot] = *p;
			return p;
		}
	}
	for (pchunk = internal_chunk_list; pchunk; pchunk = pchunk->next) {
		res = get_chunk_freelist (pchunk, slot);
		if (res)
			return res;
	}
	pchunk = alloc_pinned_chunk (size);
	/* FIXME: handle OOM */
	pchunk->next = internal_chunk_list;
	internal_chunk_list = pchunk;
	res = get_chunk_freelist (pchunk, slot);
	return res;
#endif
}

static void
free_internal_mem (void *addr)
{
	free (addr);
#if 0
	PinnedChunk *pchunk;
	for (pchunk = internal_chunk_list; pchunk; pchunk = pchunk->next) {
		/*printf ("trying to free %p in %p (pages: %d)\n", addr, pchunk, pchunk->num_pages);*/
		if (addr >= (void*)pchunk && (char*)addr < (char*)pchunk + pchunk->num_pages * FREELIST_PAGESIZE) {
			int offset = (char*)addr - (char*)pchunk;
			int page = offset / FREELIST_PAGESIZE;
			int slot = slot_for_size (pchunk->page_sizes [page]);
			void **p = addr;
			*p = pchunk->free_list [slot];
			pchunk->free_list [slot] = p;
			return;
		}
	}
	printf ("free of %p failed\n", addr);
	g_assert_not_reached ();
#endif
}

/*
 * ######################################################################
 * ########  Object allocation
 * ######################################################################
 * This section of code deals with allocating memory for objects.
 * There are several ways:
 * *) allocate large objects
 * *) allocate normal objects
 * *) fast lock-free allocation
 * *) allocation of pinned objects
 */

static void
free_large_object (LOSObject *obj)
{
	size_t size = obj->size;
	DEBUG (4, fprintf (gc_debug_file, "Freed large object %p, size %zd\n", obj->data, obj->size));

	los_memory_usage -= size;
	size += sizeof (LOSObject);
	size += pagesize - 1;
	size &= ~(pagesize - 1);
	total_alloc -= size;
	los_num_objects--;
	free_os_memory (obj, size);
}

/*
 * Objects with size >= 64KB are allocated in the large object space.
 * They are currently kept track of with a linked list.
 * They don't move, so there is no need to pin them during collection
 * and we avoid the memcpy overhead.
 */
static void* __attribute__((noinline))
alloc_large_inner (MonoVTable *vtable, size_t size)
{
	LOSObject *obj;
	void **vtslot;
	size_t alloc_size;
	int just_did_major_gc = FALSE;

	if (los_memory_usage > next_los_collection) {
		DEBUG (4, fprintf (gc_debug_file, "Should trigger major collection: req size %zd (los already: %zu, limit: %zu)\n", size, los_memory_usage, next_los_collection));
		just_did_major_gc = TRUE;
		stop_world ();
		major_collection ();
		restart_world ();
		/* later increase based on a percent of the heap size */
		next_los_collection = los_memory_usage + 5*1024*1024;
	}
	alloc_size = size;
	alloc_size += sizeof (LOSObject);
	alloc_size += pagesize - 1;
	alloc_size &= ~(pagesize - 1);
	/* FIXME: handle OOM */
	obj = get_os_memory (alloc_size, TRUE);
	obj->size = size;
	vtslot = (void**)obj->data;
	*vtslot = vtable;
	total_alloc += alloc_size;
	UPDATE_HEAP_BOUNDARIES (obj->data, (char*)obj->data + size);
	obj->next = los_object_list;
	los_object_list = obj;
	los_memory_usage += size;
	los_num_objects++;
	DEBUG (4, fprintf (gc_debug_file, "Allocated large object %p, vtable: %p (%s), size: %zd\n", obj->data, vtable, vtable->klass->name, size));
	return obj->data;
}

/* check if we have a suitable fragment in nursery_fragments to be able to allocate
 * an object of size @size
 * Return FALSE if not found (which means we need a collection)
 */
static gboolean
search_fragment_for_size (size_t size)
{
	Fragment *frag, *prev;
	DEBUG (4, fprintf (gc_debug_file, "Searching nursery fragment %p, size: %zd\n", nursery_frag_real_end, size));

	if (nursery_frag_real_end > nursery_next && nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
		/* Clear the remaining space, pinning depends on this */
		memset (nursery_next, 0, nursery_frag_real_end - nursery_next);

	prev = NULL;
	for (frag = nursery_fragments; frag; frag = frag->next) {
		if (size <= (frag->fragment_end - frag->fragment_start)) {
			/* remove from the list */
			if (prev)
				prev->next = frag->next;
			else
				nursery_fragments = frag->next;
			nursery_next = frag->fragment_start;
			nursery_frag_real_end = frag->fragment_end;

			DEBUG (4, fprintf (gc_debug_file, "Using nursery fragment %p-%p, size: %zd (req: %zd)\n", nursery_next, nursery_frag_real_end, nursery_frag_real_end - nursery_next, size));
			frag->next = fragment_freelist;
			fragment_freelist = frag;
			return TRUE;
		}
		prev = frag;
	}
	return FALSE;
}

/*
 * size is already rounded up and we hold the GC lock.
 */
static void*
alloc_degraded (MonoVTable *vtable, size_t size)
{
	GCMemSection *section;
	void **p = NULL;
	for (section = section_list; section; section = section->next) {
		if (section != nursery_section && (section->end_data - section->next_data) >= size) {
			p = (void**)section->next_data;
			break;
		}
	}
	if (!p) {
		section = alloc_section (nursery_section->size * 4);
		/* FIXME: handle OOM */
		p = (void**)section->next_data;
	}
	section->next_data += size;
	degraded_mode += size;
	DEBUG (3, fprintf (gc_debug_file, "Allocated (degraded) object %p, vtable: %p (%s), size: %zd in section %p\n", p, vtable, vtable->klass->name, size, section));
	*p = vtable;
	return p;
}

/*
 * Provide a variant that takes just the vtable for small fixed-size objects.
 * The aligned size is already computed and stored in vt->gc_descr.
 * Note: every SCAN_START_SIZE or so we are given the chance to do some special
 * processing. We can keep track of where objects start, for example,
 * so when we scan the thread stacks for pinned objects, we can start
 * a search for the pinned object in SCAN_START_SIZE chunks.
 */
void*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	char *new_next;
	int dummy;
	gboolean res;
	size += ALLOC_ALIGN - 1;
	size &= ~(ALLOC_ALIGN - 1);

	g_assert (vtable->gc_descr);

	if (G_UNLIKELY (collect_before_allocs)) {
		int dummy;

		if (nursery_section) {
			LOCK_GC;

			update_current_thread_stack (&dummy);
			stop_world ();
			collect_nursery (0);
			restart_world ();
			if (!degraded_mode && !search_fragment_for_size (size)) {
				// FIXME:
				g_assert_not_reached ();
			}
			UNLOCK_GC;
		}
	}

	/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

	p = (void**)tlab_next;
	/* FIXME: handle overflow */
	new_next = (char*)p + size;
	tlab_next = new_next;

	if (G_LIKELY (new_next < tlab_temp_end)) {
		/* Fast path */

		/* 
		 * FIXME: We might need a memory barrier here so the change to tlab_next is 
		 * visible before the vtable store.
		 */

		DEBUG (6, fprintf (gc_debug_file, "Allocated object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
		*p = vtable;
		
		return p;
	}

	/* Slow path */

	/* there are two cases: the object is too big or we run out of space in the TLAB */
	/* we also reach here when the thread does its first allocation after a minor 
	 * collection, since the tlab_ variables are initialized to NULL.
	 * there can be another case (from ORP), if we cooperate with the runtime a bit:
	 * objects that need finalizers can have the high bit set in their size
	 * so the above check fails and we can readily add the object to the queue.
	 * This avoids taking again the GC lock when registering, but this is moot when
	 * doing thread-local allocation, so it may not be a good idea.
	 */
	LOCK_GC;
	if (size > MAX_SMALL_OBJ_SIZE) {
		/* get ready for possible collection */
		update_current_thread_stack (&dummy);
		tlab_next -= size;
		p = alloc_large_inner (vtable, size);
	} else {
		if (tlab_next >= tlab_real_end) {
			/* 
			 * Run out of space in the TLAB. When this happens, some amount of space
			 * remains in the TLAB, but not enough to satisfy the current allocation
			 * request. Currently, we retire the TLAB in all cases, later we could
			 * keep it if the remaining space is above a treshold, and satisfy the
			 * allocation directly from the nursery.
			 */
			tlab_next -= size;
			/* when running in degraded mode, we continue allocing that way
			 * for a while, to decrease the number of useless nursery collections.
			 */
			if (degraded_mode && degraded_mode < DEFAULT_NURSERY_SIZE) {
				p = alloc_degraded (vtable, size);
				UNLOCK_GC;
				return p;
			}

			if (size > tlab_size) {
				/* Allocate directly from the nursery */
				if (nursery_next + size >= nursery_frag_real_end) {
					if (!search_fragment_for_size (size)) {
						/* get ready for possible collection */
						update_current_thread_stack (&dummy);
						minor_collect_or_expand_inner (size);
						if (degraded_mode) {
							p = alloc_degraded (vtable, size);
							UNLOCK_GC;
							return p;
						}
					}
				}

				p = (void*)nursery_next;
				nursery_next += size;
				if (nursery_next > nursery_frag_real_end) {
					// no space left
					g_assert (0);
				}

				if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
					memset (p, 0, size);
			} else {
				DEBUG (3, fprintf (gc_debug_file, "Retire TLAB: %p-%p [%ld]\n", tlab_start, tlab_real_end, (long)(tlab_real_end - tlab_next - size)));

				if (nursery_next + tlab_size >= nursery_frag_real_end) {
					res = search_fragment_for_size (tlab_size);
					if (!res) {
						/* get ready for possible collection */
						update_current_thread_stack (&dummy);
						minor_collect_or_expand_inner (tlab_size);
						if (degraded_mode) {
							p = alloc_degraded (vtable, size);
							UNLOCK_GC;
							return p;
						}
					}
				}

				/* Allocate a new TLAB from the current nursery fragment */
				tlab_start = nursery_next;
				nursery_next += tlab_size;
				tlab_next = tlab_start;
				tlab_real_end = tlab_start + tlab_size;
				tlab_temp_end = tlab_start + MIN (SCAN_START_SIZE, tlab_size);

				if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
					memset (tlab_start, 0, tlab_size);

				/* Allocate from the TLAB */
				p = (void*)tlab_next;
				tlab_next += size;
				g_assert (tlab_next <= tlab_real_end);

				nursery_section->scan_starts [((char*)p - (char*)nursery_section->data)/SCAN_START_SIZE] = (char*)p;
			}
		} else {
			/* Reached tlab_temp_end */

			/* record the scan start so we can find pinned objects more easily */
			nursery_section->scan_starts [((char*)p - (char*)nursery_section->data)/SCAN_START_SIZE] = (char*)p;
			/* we just bump tlab_temp_end as well */
			tlab_temp_end = MIN (tlab_real_end, tlab_next + SCAN_START_SIZE);
			DEBUG (5, fprintf (gc_debug_file, "Expanding local alloc: %p-%p\n", tlab_next, tlab_temp_end));
		}
	}

	DEBUG (6, fprintf (gc_debug_file, "Allocated object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
	*p = vtable;

	UNLOCK_GC;

	return p;
}

/*
 * To be used for interned strings and possibly MonoThread, reflection handles.
 * We may want to explicitly free these objects.
 */
void*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	size += ALLOC_ALIGN - 1;
	size &= ~(ALLOC_ALIGN - 1);
	LOCK_GC;
	if (size > MAX_FREELIST_SIZE) {
		update_current_thread_stack (&p);
		/* large objects are always pinned anyway */
		p = alloc_large_inner (vtable, size);
	} else {
		p = alloc_from_freelist (size);
		memset (p, 0, size);
	}
	DEBUG (6, fprintf (gc_debug_file, "Allocated pinned object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
	*p = vtable;
	UNLOCK_GC;
	return p;
}

/*
 * ######################################################################
 * ########  Finalization support
 * ######################################################################
 */

/*
 * this is valid for the nursery: if the object has been forwarded it means it's
 * still refrenced from a root. If it is pinned it's still alive as well.
 * Return TRUE if @obj is ready to be finalized.
 */
#define object_is_fin_ready(obj) (!object_is_pinned (obj) && !object_is_forwarded (obj))

static void
finalize_in_range (char *start, char *end)
{
	FinalizeEntry *entry, *prev;
	int i;
	if (no_finalize)
		return;
	for (i = 0; i < finalizable_hash_size; ++i) {
		prev = NULL;
		for (entry = finalizable_hash [i]; entry;) {
			if ((char*)entry->object >= start && (char*)entry->object < end && ((char*)entry->object < to_space || (char*)entry->object >= to_space_end)) {
				if (object_is_fin_ready (entry->object)) {
					char *from;
					FinalizeEntry *next;
					/* remove and put in fin_ready_list */
					if (prev)
						prev->next = entry->next;
					else
						finalizable_hash [i] = entry->next;
					next = entry->next;
					num_ready_finalizers++;
					num_registered_finalizers--;
					entry->next = fin_ready_list;
					fin_ready_list = entry;
					/* Make it survive */
					from = entry->object;
					entry->object = copy_object (entry->object, start, end);
					DEBUG (5, fprintf (gc_debug_file, "Queueing object for finalization: %p (%s) (was at %p) (%d/%d)\n", entry->object, safe_name (entry->object), from, num_ready_finalizers, num_registered_finalizers));
					entry = next;
					continue;
				} else {
					/* update pointer */
					DEBUG (5, fprintf (gc_debug_file, "Updating object for finalization: %p (%s)\n", entry->object, safe_name (entry->object)));
					entry->object = copy_object (entry->object, start, end);
				}
			}
			prev = entry;
			entry = entry->next;
		}
	}
}

static void
null_link_in_range (char *start, char *end)
{
	FinalizeEntry *entry, *prev;
	int i;
	for (i = 0; i < disappearing_link_hash_size; ++i) {
		prev = NULL;
		for (entry = disappearing_link_hash [i]; entry;) {
			if ((char*)entry->object >= start && (char*)entry->object < end && ((char*)entry->object < to_space || (char*)entry->object >= to_space_end)) {
				if (object_is_fin_ready (entry->object)) {
					void **p = entry->data;
					FinalizeEntry *old;
					*p = NULL;
					/* remove from list */
					if (prev)
						prev->next = entry->next;
					else
						disappearing_link_hash [i] = entry->next;
					DEBUG (5, fprintf (gc_debug_file, "Dislink nullified at %p to GCed object %p\n", p, entry->object));
					old = entry->next;
					free_internal_mem (entry);
					entry = old;
					num_disappearing_links--;
					continue;
				} else {
					void **link;
					/* update pointer if it's moved
					 * FIXME: what if an object is moved earlier?
					 */
					entry->object = copy_object (entry->object, start, end);
					DEBUG (5, fprintf (gc_debug_file, "Updated dislink at %p to %p\n", entry->data, entry->object));
					link = entry->data;
					*link = entry->object;
				}
			}
			prev = entry;
			entry = entry->next;
		}
	}
}

/**
 * mono_gc_finalizers_for_domain:
 * @domain: the unloading appdomain
 * @out_array: output array
 * @out_size: size of output array
 *
 * Store inside @out_array up to @out_size objects that belong to the unloading
 * appdomain @domain. Returns the number of stored items. Can be called repeteadly
 * until it returns 0.
 * The items are removed from the finalizer data structure, so the caller is supposed
 * to finalize them.
 * @out_array should be on the stack to allow the GC to know the objects are still alive.
 */
int
mono_gc_finalizers_for_domain (MonoDomain *domain, MonoObject **out_array, int out_size)
{
	FinalizeEntry *entry, *prev;
	int i, count;
	if (no_finalize || !out_size || !out_array)
		return 0;
	count = 0;
	LOCK_GC;
	for (i = 0; i < finalizable_hash_size; ++i) {
		prev = NULL;
		for (entry = finalizable_hash [i]; entry;) {
			if (mono_object_domain (entry->object) == domain) {
				FinalizeEntry *next;
				/* remove and put in out_array */
				if (prev)
					prev->next = entry->next;
				else
					finalizable_hash [i] = entry->next;
				next = entry->next;
				num_registered_finalizers--;
				out_array [count ++] = entry->object;
				DEBUG (5, fprintf (gc_debug_file, "Collecting object for finalization: %p (%s) (%d/%d)\n", entry->object, safe_name (entry->object), num_ready_finalizers, num_registered_finalizers));
				entry = next;
				if (count == out_size) {
					UNLOCK_GC;
					return count;
				}
				continue;
			}
			prev = entry;
			entry = entry->next;
		}
	}
	UNLOCK_GC;
	return count;
}

static void
rehash_fin_table (void)
{
	int i;
	unsigned int hash;
	FinalizeEntry **new_hash;
	FinalizeEntry *entry, *next;
	int new_size = g_spaced_primes_closest (num_registered_finalizers);

	new_hash = get_internal_mem (new_size * sizeof (FinalizeEntry*));
	for (i = 0; i < finalizable_hash_size; ++i) {
		for (entry = finalizable_hash [i]; entry; entry = next) {
			hash = mono_object_hash (entry->object) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	free_internal_mem (finalizable_hash);
	finalizable_hash = new_hash;
	finalizable_hash_size = new_size;
}

void
mono_gc_register_for_finalization (MonoObject *obj, void *user_data)
{
	FinalizeEntry *entry, *prev;
	unsigned int hash;
	if (no_finalize)
		return;
	hash = mono_object_hash (obj);
	LOCK_GC;
	if (num_registered_finalizers >= finalizable_hash_size * 2)
		rehash_fin_table ();
	hash %= finalizable_hash_size;
	prev = NULL;
	for (entry = finalizable_hash [hash]; entry; entry = entry->next) {
		if (entry->object == obj) {
			if (user_data) {
				entry->data = user_data;
			} else {
				/* remove from the list */
				if (prev)
					prev->next = entry->next;
				else
					finalizable_hash [hash] = entry->next;
				num_registered_finalizers--;
				DEBUG (5, fprintf (gc_debug_file, "Removed finalizer %p for object: %p (%s) (%d)\n", entry, obj, obj->vtable->klass->name, num_registered_finalizers));
				free_internal_mem (entry);
			}
			UNLOCK_GC;
			return;
		}
		prev = entry;
	}
	if (!user_data) {
		/* request to deregister, but already out of the list */
		UNLOCK_GC;
		return;
	}
	entry = get_internal_mem (sizeof (FinalizeEntry));
	entry->object = obj;
	entry->data = user_data;
	entry->next = finalizable_hash [hash];
	finalizable_hash [hash] = entry;
	num_registered_finalizers++;
	DEBUG (5, fprintf (gc_debug_file, "Added finalizer %p for object: %p (%s) (%d)\n", entry, obj, obj->vtable->klass->name, num_registered_finalizers));
	UNLOCK_GC;
}

static void
rehash_dislink (void)
{
	int i;
	unsigned int hash;
	FinalizeEntry **new_hash;
	FinalizeEntry *entry, *next;
	int new_size = g_spaced_primes_closest (num_disappearing_links);

	new_hash = get_internal_mem (new_size * sizeof (FinalizeEntry*));
	for (i = 0; i < disappearing_link_hash_size; ++i) {
		for (entry = disappearing_link_hash [i]; entry; entry = next) {
			hash = mono_aligned_addr_hash (entry->data) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	free_internal_mem (disappearing_link_hash);
	disappearing_link_hash = new_hash;
	disappearing_link_hash_size = new_size;
}

static void
mono_gc_register_disappearing_link (MonoObject *obj, void *link)
{
	FinalizeEntry *entry, *prev;
	unsigned int hash;
	LOCK_GC;

	if (num_disappearing_links >= disappearing_link_hash_size * 2)
		rehash_dislink ();
	/* FIXME: add check that link is not in the heap */
	hash = mono_aligned_addr_hash (link) % disappearing_link_hash_size;
	entry = disappearing_link_hash [hash];
	prev = NULL;
	for (; entry; entry = entry->next) {
		/* link already added */
		if (link == entry->data) {
			/* NULL obj means remove */
			if (obj == NULL) {
				if (prev)
					prev->next = entry->next;
				else
					disappearing_link_hash [hash] = entry->next;
				num_disappearing_links--;
				DEBUG (5, fprintf (gc_debug_file, "Removed dislink %p (%d)\n", entry, num_disappearing_links));
				free_internal_mem (entry);
			} else {
				entry->object = obj; /* we allow the change of object */
			}
			UNLOCK_GC;
			return;
		}
		prev = entry;
	}
	entry = get_internal_mem (sizeof (FinalizeEntry));
	entry->object = obj;
	entry->data = link;
	entry->next = disappearing_link_hash [hash];
	disappearing_link_hash [hash] = entry;
	num_disappearing_links++;
	DEBUG (5, fprintf (gc_debug_file, "Added dislink %p for object: %p (%s) at %p\n", entry, obj, obj->vtable->klass->name, link));
	UNLOCK_GC;
}

int
mono_gc_invoke_finalizers (void)
{
	FinalizeEntry *entry;
	int count = 0;
	void *obj;
	/* FIXME: batch to reduce lock contention */
	while (fin_ready_list) {
		LOCK_GC;
		entry = fin_ready_list;
		if (entry) {
			fin_ready_list = entry->next;
			num_ready_finalizers--;
			obj = entry->object;
			DEBUG (7, fprintf (gc_debug_file, "Finalizing object %p (%s)\n", obj, safe_name (obj)));
		}
		UNLOCK_GC;
		if (entry) {
			void (*callback)(void *, void*) = entry->data;
			entry->next = NULL;
			obj = entry->object;
			count++;
			/* the object is on the stack so it is pinned */
			/*g_print ("Calling finalizer for object: %p (%s)\n", entry->object, safe_name (entry->object));*/
			callback (obj, NULL);
			free_internal_mem (entry);
		}
	}
	return count;
}

gboolean
mono_gc_pending_finalizers (void)
{
	return fin_ready_list != NULL;
}

/* Negative value to remove */
void
mono_gc_add_memory_pressure (gint64 value)
{
	/* FIXME: Use interlocked functions */
	LOCK_GC;
	memory_pressure += value;
	UNLOCK_GC;
}

/*
 * ######################################################################
 * ########  registered roots support
 * ######################################################################
 */

static void
rehash_roots (void)
{
	int i;
	unsigned int hash;
	RootRecord **new_hash;
	RootRecord *entry, *next;
	int new_size = g_spaced_primes_closest (num_roots_entries);

	new_hash = get_internal_mem (new_size * sizeof (RootRecord*));
	for (i = 0; i < roots_hash_size; ++i) {
		for (entry = roots_hash [i]; entry; entry = next) {
			hash = mono_aligned_addr_hash (entry->start_root) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	free_internal_mem (roots_hash);
	roots_hash = new_hash;
	roots_hash_size = new_size;
}

/*
 * We do not coalesce roots.
 */
int
mono_gc_register_root (char *start, size_t size, void *descr)
{
	RootRecord *new_root;
	unsigned int hash = mono_aligned_addr_hash (start);
	LOCK_GC;
	if (num_roots_entries >= roots_hash_size * 2)
		rehash_roots ();
	hash %= roots_hash_size;
	for (new_root = roots_hash [hash]; new_root; new_root = new_root->next) {
		/* we allow changing the size and the descriptor (for thread statics etc) */
		if (new_root->start_root == start) {
			size_t old_size = new_root->end_root - new_root->start_root;
			new_root->end_root = new_root->start_root + size;
			new_root->root_desc = (mword)descr;
			roots_size += size;
			roots_size -= old_size;
			UNLOCK_GC;
			return TRUE;
		}
	}
	new_root = get_internal_mem (sizeof (RootRecord));
	if (new_root) {
		new_root->start_root = start;
		new_root->end_root = new_root->start_root + size;
		new_root->root_desc = (mword)descr;
		roots_size += size;
		num_roots_entries++;
		new_root->next = roots_hash [hash];
		roots_hash [hash] = new_root;
		DEBUG (3, fprintf (gc_debug_file, "Added root %p for range: %p-%p, descr: %p  (%d/%d bytes)\n", new_root, new_root->start_root, new_root->end_root, descr, (int)size, (int)roots_size));
	} else {
		UNLOCK_GC;
		return FALSE;
	}
	UNLOCK_GC;
	return TRUE;
}

void
mono_gc_deregister_root (char* addr)
{
	RootRecord *tmp, *prev = NULL;
	unsigned int hash = mono_aligned_addr_hash (addr);
	LOCK_GC;
	hash %= roots_hash_size;
	tmp = roots_hash [hash];
	while (tmp) {
		if (tmp->start_root == (char*)addr) {
			if (prev)
				prev->next = tmp->next;
			else
				roots_hash [hash] = tmp->next;
			roots_size -= (tmp->end_root - tmp->start_root);
			num_roots_entries--;
			DEBUG (3, fprintf (gc_debug_file, "Removed root %p for range: %p-%p\n", tmp, tmp->start_root, tmp->end_root));
			free_internal_mem (tmp);
			break;
		}
		prev = tmp;
		tmp = tmp->next;
	}
	UNLOCK_GC;
}

/*
 * ######################################################################
 * ########  Thread handling (stop/start code)
 * ######################################################################
 */

/* eventually share with MonoThread? */
typedef struct _SgenThreadInfo SgenThreadInfo;

struct _SgenThreadInfo {
	SgenThreadInfo *next;
	ARCH_THREAD_TYPE id;
	unsigned int stop_count; /* to catch duplicate signals */
	int signal;
	int skip;
	void *stack_end;
	void *stack_start;
	char **tlab_next_addr;
	char **tlab_start_addr;
	char **tlab_temp_end_addr;
	char **tlab_real_end_addr;
	RememberedSet *remset;
};

/* FIXME: handle large/small config */
#define THREAD_HASH_SIZE 11
#define HASH_PTHREAD_T(id) (((unsigned int)(id) >> 4) * 2654435761u)

static SgenThreadInfo* thread_table [THREAD_HASH_SIZE];

#if USE_SIGNAL_BASED_START_STOP_WORLD

static sem_t suspend_ack_semaphore;
static unsigned int global_stop_count = 0;
static int suspend_signal_num = SIGPWR;
static int restart_signal_num = SIGXCPU;
static sigset_t suspend_signal_mask;
static mword cur_thread_regs [ARCH_NUM_REGS] = {0};

/* LOCKING: assumes the GC lock is held */
static SgenThreadInfo*
thread_info_lookup (ARCH_THREAD_TYPE id)
{
	unsigned int hash = HASH_PTHREAD_T (id) % THREAD_HASH_SIZE;
	SgenThreadInfo *info;

	info = thread_table [hash];
	while (info && !ARCH_THREAD_EQUALS (info->id, id)) {
		info = info->next;
	}
	return info;
}

static void
update_current_thread_stack (void *start)
{
	void *ptr = cur_thread_regs;
	SgenThreadInfo *info = thread_info_lookup (ARCH_GET_THREAD ());
	info->stack_start = align_pointer (&ptr);
	ARCH_STORE_REGS (ptr);
}

static const char*
signal_desc (int signum)
{
	if (signum == suspend_signal_num)
		return "suspend";
	if (signum == restart_signal_num)
		return "restart";
	return "unknown";
}

/* LOCKING: assumes the GC lock is held */
static int
thread_handshake (int signum)
{
	int count, i, result;
	SgenThreadInfo *info;
	pthread_t me = pthread_self ();

	count = 0;
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			DEBUG (4, fprintf (gc_debug_file, "considering thread %p for signal %d (%s)\n", info, signum, signal_desc (signum)));
			if (ARCH_THREAD_EQUALS (info->id, me)) {
				DEBUG (4, fprintf (gc_debug_file, "Skip (equal): %p, %p\n", (void*)me, (void*)info->id));
				continue;
			}
			/*if (signum == suspend_signal_num && info->stop_count == global_stop_count)
				continue;*/
			result = pthread_kill (info->id, signum);
			if (result == 0) {
				DEBUG (4, fprintf (gc_debug_file, "thread %p signal sent\n", info));
				count++;
			} else {
				DEBUG (4, fprintf (gc_debug_file, "thread %p signal failed: %d (%s)\n", (void*)info->id, result, strerror (result)));
				info->skip = 1;
			}
		}
	}

	for (i = 0; i < count; ++i) {
		while ((result = sem_wait (&suspend_ack_semaphore)) != 0) {
			if (errno != EINTR) {
				g_error ("sem_wait ()");
			}
		}
	}
	return count;
}

/* LOCKING: assumes the GC lock is held (by the stopping thread) */
static void
suspend_handler (int sig)
{
	SgenThreadInfo *info;
	pthread_t id;
	int stop_count;
	int old_errno = errno;

	id = pthread_self ();
	info = thread_info_lookup (id);
	stop_count = global_stop_count;
	/* duplicate signal */
	if (0 && info->stop_count == stop_count) {
		errno = old_errno;
		return;
	}
	/* update the remset info in the thread data structure */
	info->remset = remembered_set;
	/* 
	 * this includes the register values that the kernel put on the stack.
	 * Write arch-specific code to only push integer regs and a more accurate
	 * stack pointer.
	 */
	info->stack_start = align_pointer (&id);

	/* notify the waiting thread */
	sem_post (&suspend_ack_semaphore);
	info->stop_count = stop_count;

	/* wait until we receive the restart signal */
	do {
		info->signal = 0;
		sigsuspend (&suspend_signal_mask);
	} while (info->signal != restart_signal_num);

	/* notify the waiting thread */
	sem_post (&suspend_ack_semaphore);
	
	errno = old_errno;
}

static void
restart_handler (int sig)
{
	SgenThreadInfo *info;
	int old_errno = errno;

	info = thread_info_lookup (pthread_self ());
	info->signal = restart_signal_num;

	errno = old_errno;
}

static TV_DECLARE (stop_world_time);
static unsigned long max_pause_usec = 0;

/* LOCKING: assumes the GC lock is held */
static int
stop_world (void)
{
	int count;

	global_stop_count++;
	DEBUG (3, fprintf (gc_debug_file, "stopping world n %d from %p %p\n", global_stop_count, thread_info_lookup (ARCH_GET_THREAD ()), (gpointer)ARCH_GET_THREAD ()));
	TV_GETTIME (stop_world_time);
	count = thread_handshake (suspend_signal_num);
	DEBUG (3, fprintf (gc_debug_file, "world stopped %d thread(s)\n", count));
	return count;
}

/* LOCKING: assumes the GC lock is held */
static int
restart_world (void)
{
	int count;
	TV_DECLARE (end_sw);
	unsigned long usec;

	count = thread_handshake (restart_signal_num);
	TV_GETTIME (end_sw);
	usec = TV_ELAPSED (stop_world_time, end_sw);
	max_pause_usec = MAX (usec, max_pause_usec);
	DEBUG (2, fprintf (gc_debug_file, "restarted %d thread(s) (pause time: %d usec, max: %d)\n", count, (int)usec, (int)max_pause_usec));
	return count;
}

#endif /* USE_SIGNAL_BASED_START_STOP_WORLD */

/*
 * Identify objects pinned in a thread stack and its registers.
 */
static void
pin_thread_data (void *start_nursery, void *end_nursery)
{
	int i;
	SgenThreadInfo *info;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			if (info->skip) {
				DEBUG (2, fprintf (gc_debug_file, "Skipping dead thread %p, range: %p-%p, size: %zd\n", info, info->stack_start, info->stack_end, (char*)info->stack_end - (char*)info->stack_start));
				continue;
			}
			DEBUG (2, fprintf (gc_debug_file, "Scanning thread %p, range: %p-%p, size: %zd\n", info, info->stack_start, info->stack_end, (char*)info->stack_end - (char*)info->stack_start));
			conservatively_pin_objects_from (info->stack_start, info->stack_end, start_nursery, end_nursery);
		}
	}
	DEBUG (2, fprintf (gc_debug_file, "Scanning current thread registers\n"));
	conservatively_pin_objects_from ((void*)cur_thread_regs, (void*)(cur_thread_regs + ARCH_NUM_REGS), start_nursery, end_nursery);
}

static void
find_pinning_ref_from_thread (char *obj, size_t size)
{
	int i;
	SgenThreadInfo *info;
	char *endobj = obj + size;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			char **start = (char**)info->stack_start;
			if (info->skip)
				continue;
			while (start < (char**)info->stack_end) {
				if (*start >= obj && *start < endobj) {
					DEBUG (0, fprintf (gc_debug_file, "Object %p referenced in thread %p (id %p) at %p, stack: %p-%p\n", obj, info, (gpointer)info->id, start, info->stack_start, info->stack_end));
				}
				start++;
			}
		}
	}
	/* FIXME: check register */
}

/* return TRUE if ptr points inside the managed heap */
static gboolean
ptr_in_heap (void* ptr)
{
	mword p = (mword)ptr;
	if (p < lowest_heap_address || p >= highest_heap_address)
		return FALSE;
	/* FIXME: more checks */
	return TRUE;
}

static mword*
handle_remset (mword *p, void *start_nursery, void *end_nursery, gboolean global)
{
	void **ptr;
	mword count;
	mword desc;

	/* FIXME: exclude stack locations */
	switch ((*p) & REMSET_TYPE_MASK) {
	case REMSET_LOCATION:
		ptr = (void**)(*p);
		if (((void*)ptr < start_nursery || (void*)ptr >= end_nursery) && ptr_in_heap (ptr)) {
			*ptr = copy_object (*ptr, start_nursery, end_nursery);
			DEBUG (9, fprintf (gc_debug_file, "Overwrote remset at %p with %p\n", ptr, *ptr));
			if (!global && *ptr >= start_nursery && *ptr < end_nursery)
				add_to_global_remset (ptr);
		} else {
			DEBUG (9, fprintf (gc_debug_file, "Skipping remset at %p holding %p\n", ptr, *ptr));
		}
		return p + 1;
	case REMSET_RANGE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery) || !ptr_in_heap (ptr))
			return p + 2;
		count = p [1];
		while (count-- > 0) {
			*ptr = copy_object (*ptr, start_nursery, end_nursery);
			DEBUG (9, fprintf (gc_debug_file, "Overwrote remset at %p with %p (count: %d)\n", ptr, *ptr, (int)count));
			if (!global && *ptr >= start_nursery && *ptr < end_nursery)
				add_to_global_remset (ptr);
			++ptr;
		}
		return p + 2;
	case REMSET_OBJECT:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery) || !ptr_in_heap (ptr))
			return p + 1;
		scan_object (*ptr, start_nursery, end_nursery);
		return p + 1;
	case REMSET_VTYPE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery) || !ptr_in_heap (ptr))
			return p + 2;
		desc = p [1];
		scan_vtype ((char*)ptr, desc, start_nursery, end_nursery);
		return p + 2;
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

static void
scan_from_remsets (void *start_nursery, void *end_nursery)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset, *next;
	mword *p;

	/* the global one */
	for (remset = global_remset; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning global remset range: %p-%p, size: %zd\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;) {
			p = handle_remset (p, start_nursery, end_nursery, TRUE);
		}
	}
	/* the per-thread ones */
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			for (remset = info->remset; remset; remset = next) {
				DEBUG (4, fprintf (gc_debug_file, "Scanning remset for thread %p, range: %p-%p, size: %zd\n", info, remset->data, remset->store_next, remset->store_next - remset->data));
				for (p = remset->data; p < remset->store_next;) {
					p = handle_remset (p, start_nursery, end_nursery, FALSE);
				}
				remset->store_next = remset->data;
				next = remset->next;
				remset->next = NULL;
				if (remset != info->remset) {
					DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
					free_internal_mem (remset);
				}
			}
		}
	}
}

/*
 * Clear the info in the remembered sets: we're doing a major collection, so
 * the per-thread ones are not needed and the global ones will be reconstructed
 * during the copy.
 */
static void
clear_remsets (void)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset, *next;

	/* the global list */
	for (remset = global_remset; remset; remset = next) {
		remset->store_next = remset->data;
		next = remset->next;
		remset->next = NULL;
		if (remset != global_remset) {
			DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
			free_internal_mem (remset);
		}
	}
	/* the per-thread ones */
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			for (remset = info->remset; remset; remset = next) {
				remset->store_next = remset->data;
				next = remset->next;
				remset->next = NULL;
				if (remset != info->remset) {
					DEBUG (1, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
					free_internal_mem (remset);
				}
			}
		}
	}
}

/*
 * Clear the thread local TLAB variables for all threads.
 */
static void
clear_tlabs (void)
{
	SgenThreadInfo *info;
	int i;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			/* A new TLAB will be allocated when the thread does its first allocation */
			*info->tlab_start_addr = NULL;
			*info->tlab_next_addr = NULL;
			*info->tlab_temp_end_addr = NULL;
			*info->tlab_real_end_addr = NULL;
		}
	}
}

/*
 * Find the tlab_next value of the TLAB which contains ADDR.
 */
static char*
find_tlab_next_from_address (char *addr)
{
	SgenThreadInfo *info;
	int i;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			if (addr >= *info->tlab_start_addr && addr < *info->tlab_next_addr)
				return *info->tlab_next_addr;
		}
	}

	return NULL;
}

/* LOCKING: assumes the GC lock is held */
static SgenThreadInfo*
gc_register_current_thread (void *addr)
{
	int hash;
	SgenThreadInfo* info = malloc (sizeof (SgenThreadInfo));
	if (!info)
		return NULL;
	info->id = ARCH_GET_THREAD ();
	info->stop_count = -1;
	info->skip = 0;
	info->signal = 0;
	info->stack_start = NULL;
	info->tlab_start_addr = &tlab_start;
	info->tlab_next_addr = &tlab_next;
	info->tlab_temp_end_addr = &tlab_temp_end;
	info->tlab_real_end_addr = &tlab_real_end;

	tlab_next_addr = &tlab_next;

	/* try to get it with attributes first */
#if defined(HAVE_PTHREAD_GETATTR_NP) && defined(HAVE_PTHREAD_ATTR_GETSTACK)
	{
		size_t size;
		void *sstart;
		pthread_attr_t attr;
		pthread_getattr_np (pthread_self (), &attr);
		pthread_attr_getstack (&attr, &sstart, &size);
		info->stack_end = (char*)sstart + size;
		pthread_attr_destroy (&attr);
	}
#elif defined(HAVE_PTHREAD_GET_STACKSIZE_NP) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
		 info->stack_end = (char*)pthread_get_stackaddr_np (pthread_self ());
#else
	{
		/* FIXME: we assume the stack grows down */
		gsize stack_bottom = (gsize)addr;
		stack_bottom += 4095;
		stack_bottom &= ~4095;
		info->stack_end = (char*)stack_bottom;
	}
#endif

	/* hash into the table */
	hash = HASH_PTHREAD_T (info->id) % THREAD_HASH_SIZE;
	info->next = thread_table [hash];
	thread_table [hash] = info;

	remembered_set = info->remset = alloc_remset (DEFAULT_REMSET_SIZE, info);
	pthread_setspecific (remembered_set_key, remembered_set);
	DEBUG (3, fprintf (gc_debug_file, "registered thread %p (%p) (hash: %d)\n", info, (gpointer)info->id, hash));
	return info;
}

static void
unregister_current_thread (void)
{
	int hash;
	SgenThreadInfo *prev = NULL;
	SgenThreadInfo *p;
	RememberedSet *rset;
	ARCH_THREAD_TYPE id = ARCH_GET_THREAD ();

	hash = HASH_PTHREAD_T (id) % THREAD_HASH_SIZE;
	p = thread_table [hash];
	assert (p);
	DEBUG (3, fprintf (gc_debug_file, "unregister thread %p (%p)\n", p, (gpointer)p->id));
	while (!ARCH_THREAD_EQUALS (p->id, id)) {
		prev = p;
		p = p->next;
	}
	if (prev == NULL) {
		thread_table [hash] = p->next;
	} else {
		prev->next = p->next;
	}
	rset = p->remset;
	/* FIXME: transfer remsets if any */
	while (rset) {
		RememberedSet *next = rset->next;
		free_internal_mem (rset);
		rset = next;
	}
	free (p);
}

static void
unregister_thread (void *k)
{
	LOCK_GC;
	unregister_current_thread ();
	UNLOCK_GC;
}

gboolean
mono_gc_register_thread (void *baseptr)
{
	SgenThreadInfo *info;
	LOCK_GC;
	info = thread_info_lookup (ARCH_GET_THREAD ());
	if (info == NULL)
		info = gc_register_current_thread (baseptr);
	UNLOCK_GC;
	return info != NULL;
}

#if USE_PTHREAD_INTERCEPT

#undef pthread_create
#undef pthread_join
#undef pthread_detach

typedef struct {
	void *(*start_routine) (void *);
	void *arg;
	int flags;
	sem_t registered;
} SgenThreadStartInfo;

static void*
gc_start_thread (void *arg)
{
	SgenThreadStartInfo *start_info = arg;
	SgenThreadInfo* info;
	void *t_arg = start_info->arg;
	void *(*start_func) (void*) = start_info->start_routine;
	void *result;

	LOCK_GC;
	info = gc_register_current_thread (&result);
	UNLOCK_GC;
	sem_post (&(start_info->registered));
	result = start_func (t_arg);
	/*
	 * this is done by the pthread key dtor
	LOCK_GC;
	unregister_current_thread ();
	UNLOCK_GC;
	*/

	return result;
}

int
mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	SgenThreadStartInfo *start_info;
	int result;

	start_info = malloc (sizeof (SgenThreadStartInfo));
	if (!start_info)
		return ENOMEM;
	sem_init (&(start_info->registered), 0, 0);
	start_info->arg = arg;
	start_info->start_routine = start_routine;

	result = pthread_create (new_thread, attr, gc_start_thread, start_info);
	if (result == 0) {
		while (sem_wait (&(start_info->registered)) != 0) {
			/*if (EINTR != errno) ABORT("sem_wait failed"); */
		}
	}
	sem_destroy (&(start_info->registered));
	free (start_info);
	return result;
}

int
mono_gc_pthread_join (pthread_t thread, void **retval)
{
	return pthread_join (thread, retval);
}

int
mono_gc_pthread_detach (pthread_t thread)
{
	return pthread_detach (thread);
}

#endif /* USE_PTHREAD_INTERCEPT */

/*
 * ######################################################################
 * ########  Write barriers
 * ######################################################################
 */

static RememberedSet*
alloc_remset (int size, gpointer id) {
	RememberedSet* res = get_internal_mem (sizeof (RememberedSet) + (size * sizeof (gpointer)));
	res->store_next = res->data;
	res->end_set = res->data + size;
	res->next = NULL;
	DEBUG (4, fprintf (gc_debug_file, "Allocated remset size %d at %p for %p\n", size, res->data, id));
	return res;
}

/*
 * Note: the write barriers first do the needed GC work and then do the actual store:
 * this way the value is visible to the conservative GC scan after the write barrier
 * itself. If a GC interrupts the barrier in the middle, value will be kept alive by
 * the conservative scan, otherwise by the remembered set scan. FIXME: figure out what
 * happens when we need to record which pointers contain references to the new generation.
 * The write barrier will be executed, but the pointer is still not stored.
 */
void
mono_gc_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	RememberedSet *rs;
	if ((char*)field_ptr >= nursery_start && (char*)field_ptr < nursery_real_end) {
		*(void**)field_ptr = value;
		return;
	}
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p\n", field_ptr));
	rs = remembered_set;
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)field_ptr;
		*(void**)field_ptr = value;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = remembered_set;
	remembered_set = rs;
	thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
	*(rs->store_next++) = (mword)field_ptr;
	*(void**)field_ptr = value;
}

void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	RememberedSet *rs = remembered_set;
	if ((char*)slot_ptr >= nursery_start && (char*)slot_ptr < nursery_real_end) {
		*(void**)slot_ptr = value;
		return;
	}
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p\n", slot_ptr));
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)slot_ptr;
		*(void**)slot_ptr = value;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = remembered_set;
	remembered_set = rs;
	thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
	*(rs->store_next++) = (mword)slot_ptr;
	*(void**)slot_ptr = value;
}

void
mono_gc_wbarrier_arrayref_copy (MonoArray *arr, gpointer slot_ptr, int count)
{
	RememberedSet *rs = remembered_set;
	if ((char*)slot_ptr >= nursery_start && (char*)slot_ptr < nursery_real_end)
		return;
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p, %d\n", slot_ptr, count));
	if (rs->store_next + 1 < rs->end_set) {
		*(rs->store_next++) = (mword)slot_ptr | REMSET_RANGE;
		*(rs->store_next++) = count;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = remembered_set;
	remembered_set = rs;
	thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
	*(rs->store_next++) = (mword)slot_ptr | REMSET_RANGE;
	*(rs->store_next++) = count;
}

void
mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value)
{
	RememberedSet *rs = remembered_set;
	if ((char*)ptr >= nursery_start && (char*)ptr < nursery_real_end) {
		DEBUG (8, fprintf (gc_debug_file, "Skipping remset at %p\n", ptr));
		*(void**)ptr = value;
		return;
	}
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p\n", ptr));
	/* FIXME: ensure it is on the heap */
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)ptr;
		*(void**)ptr = value;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = remembered_set;
	remembered_set = rs;
	thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
	*(rs->store_next++) = (mword)ptr;
	*(void**)ptr = value;
}

void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	RememberedSet *rs = remembered_set;
	if ((char*)dest >= nursery_start && (char*)dest < nursery_real_end) {
		return;
	}
	DEBUG (1, fprintf (gc_debug_file, "Adding value remset at %p, count %d for class %s\n", dest, count, klass->name));

	if (rs->store_next + 1 < rs->end_set) {
		*(rs->store_next++) = (mword)dest | REMSET_VTYPE;
		*(rs->store_next++) = (mword)klass->gc_descr;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = remembered_set;
	remembered_set = rs;
	thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
	*(rs->store_next++) = (mword)dest | REMSET_VTYPE;
	*(rs->store_next++) = (mword)klass->gc_descr;
}

/**
 * mono_gc_wbarrier_object:
 *
 * Write barrier to call when obj is the result of a clone or copy of an object.
 */
void
mono_gc_wbarrier_object (MonoObject* obj)
{
	RememberedSet *rs = remembered_set;
	DEBUG (1, fprintf (gc_debug_file, "Adding object remset for %p\n", obj));
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = remembered_set;
	remembered_set = rs;
	thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
	*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
}

/*
 * ######################################################################
 * ########  Collector debugging
 * ######################################################################
 */

const char*descriptor_types [] = {
	"run_length",
	"small_bitmap",
	"string",
	"complex",
	"vector",
	"array",
	"large_bitmap",
	"complex_arr"
};

void
describe_ptr (char *ptr)
{
	GCMemSection *section;
	MonoVTable *vtable;
	mword desc;
	int type;

	if ((ptr >= nursery_start) && (ptr < nursery_real_end)) {
		printf ("Pointer inside nursery.\n");
	} else {
		for (section = section_list; section;) {
			if (ptr >= section->data && ptr < section->data + section->size)
				break;
			section = section->next;
		}

		if (section) {
			printf ("Pointer inside oldspace.\n");
		} else {
			printf ("Pointer unknown.\n");
			return;
		}
	}

	// FIXME: Handle pointers to the inside of objects
	vtable = (MonoVTable*)LOAD_VTABLE (ptr);

	printf ("VTable: %p\n", vtable);
	if (vtable == NULL) {
		printf ("VTable is invalid (empty).\n");
		return;
	}
	if (((char*)vtable >= nursery_start) && ((char*)vtable < nursery_real_end)) {
		printf ("VTable is invalid (points inside nursery).\n");
		return;
	}
	printf ("Class: %s\n", vtable->klass->name);

	desc = ((GCVTable*)vtable)->desc;
	printf ("Descriptor: %lx\n", desc);

	type = desc & 0x7;
	printf ("Descriptor type: %d (%s)\n", type, descriptor_types [type]);
}

static mword*
find_in_remset_loc (mword *p, char *addr, gboolean *found)
{
	void **ptr;
	mword count, desc;
	size_t skip_size;

	switch ((*p) & REMSET_TYPE_MASK) {
	case REMSET_LOCATION:
		if (*p == (mword)addr)
			*found = TRUE;
		return p + 1;
	case REMSET_RANGE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		count = p [1];
		if ((void**)addr >= ptr && (void**)addr < ptr + count)
			*found = TRUE;
		return p + 2;
	case REMSET_OBJECT:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		count = safe_object_get_size ((MonoObject*)ptr); 
		count += (ALLOC_ALIGN - 1);
		count &= (ALLOC_ALIGN - 1);
		count /= sizeof (mword);
		if ((void**)addr >= ptr && (void**)addr < ptr + count)
			*found = TRUE;
		return p + 1;
	case REMSET_VTYPE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		desc = p [1];

		switch (desc & 0x7) {
		case DESC_TYPE_RUN_LENGTH:
			OBJ_RUN_LEN_SIZE (skip_size, desc, ptr);
			/* The descriptor includes the size of MonoObject */
			skip_size -= sizeof (MonoObject);
			if ((void**)addr >= ptr && (void**)addr < ptr + (skip_size / sizeof (gpointer)))
				*found = TRUE;
			break;
		default:
			// FIXME:
			g_assert_not_reached ();
		}

		return p + 2;
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

/*
 * Return whenever ADDR occurs in the remembered sets
 */
static gboolean
find_in_remsets (char *addr)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset;
	mword *p;
	gboolean found = FALSE;

	/* the global one */
	for (remset = global_remset; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning global remset range: %p-%p, size: %zd\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;) {
			p = find_in_remset_loc (p, addr, &found);
			if (found)
				return TRUE;
		}
	}
	/* the per-thread ones */
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			for (remset = info->remset; remset; remset = remset->next) {
				DEBUG (4, fprintf (gc_debug_file, "Scanning remset for thread %p, range: %p-%p, size: %zd\n", info, remset->data, remset->store_next, remset->store_next - remset->data));
				for (p = remset->data; p < remset->store_next;) {
					p = find_in_remset_loc (p, addr, &found);
					if (found)
						return TRUE;
				}
			}
		}
	}

	return FALSE;
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr) && (char*)*(ptr) >= nursery_start && (char*)*(ptr) < nursery_next) {	\
            if (!find_in_remsets ((char*)(ptr))) { \
                fprintf (gc_debug_file, "Oldspace->newspace reference %p at offset %zd in object %p (%s.%s) not found in remsets.\n", *(ptr), (char*)(ptr) - (char*)(obj), (obj), ((MonoObject*)(obj))->vtable->klass->name_space, ((MonoObject*)(obj))->vtable->klass->name); \
                g_assert_not_reached (); \
            } \
        } \
	} while (0)

/*
 * Check that each object reference inside the area which points into the nursery
 * can be found in the remembered sets.
 */
static void __attribute__((noinline))
check_remsets_for_area (char *start, char *end)
{
	GCVTable *vt;
	size_t skip_size;
	int type;
	int type_str = 0, type_rlen = 0, type_bitmap = 0, type_vector = 0, type_lbit = 0, type_complex = 0;
	mword desc;
	new_obj_references = 0;
	obj_references_checked = 0;
	while (start < end) {
		if (!*(void**)start) {
			start += sizeof (void*); /* should be ALLOC_ALIGN, really */
			continue;
		}
		vt = (GCVTable*)LOAD_VTABLE (start);
		DEBUG (8, fprintf (gc_debug_file, "Scanning object %p, vtable: %p (%s)\n", start, vt, vt->klass->name));
		if (0) {
			MonoObject *obj = (MonoObject*)start;
			g_print ("found at %p (0x%lx): %s.%s\n", start, (long)vt->desc, obj->vtable->klass->name_space, obj->vtable->klass->name);
		}
		desc = vt->desc;
		type = desc & 0x7;
		if (type == DESC_TYPE_STRING) {
			STRING_SIZE (skip_size, start);
			start += skip_size;
			type_str++;
			continue;
		} else if (type == DESC_TYPE_RUN_LENGTH) {
			OBJ_RUN_LEN_SIZE (skip_size, desc, start);
			g_assert (skip_size);
			OBJ_RUN_LEN_FOREACH_PTR (desc,start);
			start += skip_size;
			type_rlen++;
			continue;
		} else if (type == DESC_TYPE_VECTOR) { // includes ARRAY, too
			skip_size = (vt->desc >> LOW_TYPE_BITS) & MAX_ELEMENT_SIZE;
			skip_size *= mono_array_length ((MonoArray*)start);
			skip_size += sizeof (MonoArray);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_VECTOR_FOREACH_PTR (vt, start);
			if (((MonoArray*)start)->bounds) {
				/* account for the bounds */
				skip_size += sizeof (MonoArrayBounds) * vt->klass->rank;
			}
			start += skip_size;
			type_vector++;
			continue;
		} else if (type == DESC_TYPE_SMALL_BITMAP) {
			OBJ_BITMAP_SIZE (skip_size, desc, start);
			g_assert (skip_size);
			OBJ_BITMAP_FOREACH_PTR (desc,start);
			start += skip_size;
			type_bitmap++;
			continue;
		} else if (type == DESC_TYPE_LARGE_BITMAP) {
			skip_size = safe_object_get_size ((MonoObject*)start);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_LARGE_BITMAP_FOREACH_PTR (vt,start);
			start += skip_size;
			type_lbit++;
			continue;
		} else if (type == DESC_TYPE_COMPLEX) {
			/* this is a complex object */
			skip_size = safe_object_get_size ((MonoObject*)start);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_COMPLEX_FOREACH_PTR (vt, start);
			start += skip_size;
			type_complex++;
			continue;
		} else if (type == DESC_TYPE_COMPLEX_ARR) {
			/* this is an array of complex structs */
			skip_size = mono_array_element_size (((MonoVTable*)vt)->klass);
			skip_size *= mono_array_length ((MonoArray*)start);
			skip_size += sizeof (MonoArray);
			skip_size += (ALLOC_ALIGN - 1);
			skip_size &= ~(ALLOC_ALIGN - 1);
			OBJ_COMPLEX_ARR_FOREACH_PTR (vt, start);
			if (((MonoArray*)start)->bounds) {
				/* account for the bounds */
				skip_size += sizeof (MonoArrayBounds) * vt->klass->rank;
			}
			start += skip_size;
			type_complex++;
			continue;
		} else {
			g_assert (0);
		}
	}
}

/*
 * Perform consistency check of the heap.
 *
 * Assumes the world is stopped.
 */
void
check_consistency (void)
{
	GCMemSection *section;

	// Need to add more checks
	// FIXME: Create a general heap enumeration function and use that

	DEBUG (1, fprintf (gc_debug_file, "Begin heap consistency check...\n"));

	// Check that oldspace->newspace pointers are registered with the collector
	for (section = section_list; section; section = section->next) {
		if (section->role == MEMORY_ROLE_GEN0)
			continue;
		DEBUG (2, fprintf (gc_debug_file, "Scan of old section: %p-%p, size: %d\n", section->data, section->next_data, (int)(section->next_data - section->data)));
		check_remsets_for_area (section->data, section->next_data);
	}

	DEBUG (1, fprintf (gc_debug_file, "Heap consistency check done.\n"));
}

/*
 * ######################################################################
 * ########  Other mono public interface functions.
 * ######################################################################
 */

void
mono_gc_collect (int generation)
{
	LOCK_GC;
	update_current_thread_stack (&generation);
	stop_world ();
	if (generation == 0) {
		collect_nursery (0);
	} else {
		major_collection ();
	}
	restart_world ();
	UNLOCK_GC;
}

int
mono_gc_max_generation (void)
{
	return 1;
}

int
mono_gc_collection_count (int generation)
{
	if (generation == 0)
		return num_minor_gcs;
	return num_major_gcs;
}

gint64
mono_gc_get_used_size (void)
{
	gint64 tot = 0;
	GCMemSection *section;
	LOCK_GC;
	tot = los_memory_usage;
	for (section = section_list; section; section = section->next) {
		/* this is approximate... */
		tot += section->next_data - section->data;
	}
	/* FIXME: account for pinned objects */
	UNLOCK_GC;
	return tot;
}

gint64
mono_gc_get_heap_size (void)
{
	return total_alloc;
}

void
mono_gc_disable (void)
{
	LOCK_GC;
	gc_disabled++;
	UNLOCK_GC;
}

void
mono_gc_enable (void)
{
	LOCK_GC;
	gc_disabled--;
	UNLOCK_GC;
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	return TRUE;
}

int
mono_gc_get_generation (MonoObject *obj)
{
	if ((char*)obj >= nursery_start && (char*)obj < nursery_real_end)
		return 0;
	return 1;
}

void
mono_gc_enable_events (void)
{
}

void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj)
{
	mono_gc_register_disappearing_link (obj, link_addr);
	*link_addr = obj;
}

void
mono_gc_weak_link_remove (void **link_addr)
{
	mono_gc_register_disappearing_link (NULL, link_addr);
	*link_addr = NULL;
}

MonoObject*
mono_gc_weak_link_get (void **link_addr)
{
	return *link_addr;
}

void*
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	if (numbits < ((sizeof (*bitmap) * 8) - ROOT_DESC_TYPE_SHIFT)) {
		mword desc = ROOT_DESC_BITMAP | (bitmap [0] << ROOT_DESC_TYPE_SHIFT);
		return (void*)desc;
	}
	/* conservative scanning */
	DEBUG (3, fprintf (gc_debug_file, "Conservative root descr for size: %d\n", numbits));
	return NULL;
}

void*
mono_gc_alloc_fixed (size_t size, void *descr)
{
	/* FIXME: do a single allocation */
	void *res = calloc (1, size);
	if (!res)
		return NULL;
	if (!mono_gc_register_root (res, size, descr)) {
		free (res);
		res = NULL;
	}
	return res;
}

void
mono_gc_free_fixed (void* addr)
{
	mono_gc_deregister_root (addr);
	free (addr);
}

gboolean
mono_gc_is_gc_thread (void)
{
	gboolean result;
	LOCK_GC;
        result = thread_info_lookup (ARCH_GET_THREAD ()) != NULL;
	UNLOCK_GC;
	return result;
}

void
mono_gc_base_init (void)
{
	char *env;
	char **opts, **ptr;
	struct sigaction sinfo;

	LOCK_INIT (gc_mutex);
	LOCK_GC;
	if (gc_initialized) {
		UNLOCK_GC;
		return;
	}
	pagesize = mono_pagesize ();
	gc_debug_file = stderr;
	if ((env = getenv ("MONO_GC_DEBUG"))) {
		opts = g_strsplit (env, ",", -1);
		for (ptr = opts; ptr && *ptr; ptr ++) {
			char *opt = *ptr;
			if (opt [0] >= '0' && opt [0] <= '9') {
				gc_debug_level = atoi (opt);
				opt++;
				if (opt [0] == ':')
					opt++;
				if (opt [0]) {
					char *rf = g_strdup_printf ("%s.%d", opt, getpid ());
					gc_debug_file = fopen (rf, "wb");
					if (!gc_debug_file)
						gc_debug_file = stderr;
					g_free (rf);
				}
			} else if (!strcmp (opt, "collect-before-allocs")) {
				collect_before_allocs = TRUE;
			} else if (!strcmp (opt, "check-at-minor-collections")) {
				consistency_check_at_minor_collection = TRUE;
			} else {
				fprintf (stderr, "Invalid format for the MONO_GC_DEBUG env variable: '%s'\n", env);
				fprintf (stderr, "The format is: MONO_GC_DEBUG=[l[:filename]|<option>]+ where l is a debug level 0-9.\n");
				fprintf (stderr, "Valid options are: collect-before-allocs, check-at-minor-collections.\n");
				exit (1);
			}
		}
		g_strfreev (opts);
	}

	sem_init (&suspend_ack_semaphore, 0, 0);

	sigfillset (&sinfo.sa_mask);
	sinfo.sa_flags = SA_RESTART | SA_SIGINFO;
	sinfo.sa_handler = suspend_handler;
	if (sigaction (suspend_signal_num, &sinfo, NULL) != 0) {
		g_error ("failed sigaction");
	}

	sinfo.sa_handler = restart_handler;
	if (sigaction (restart_signal_num, &sinfo, NULL) != 0) {
		g_error ("failed sigaction");
	}

	sigfillset (&suspend_signal_mask);
	sigdelset (&suspend_signal_mask, restart_signal_num);

	global_remset = alloc_remset (1024, NULL);
	global_remset->next = NULL;

	pthread_key_create (&remembered_set_key, unregister_thread);
	gc_initialized = TRUE;
	UNLOCK_GC;
	mono_gc_register_thread (&sinfo);
}

enum {
	ATYPE_NORMAL,
	ATYPE_NUM
};

/* FIXME: Do this in the JIT, where specialized allocation sequences can be created
 * for each class. This is currently not easy to do, as it is hard to generate basic 
 * blocks + branches, but it is easy with the linear IL codebase.
 */
static MonoMethod*
create_allocator (int atype)
{
	int tlab_next_addr_offset = -1;
	int tlab_temp_end_offset = -1;
	int p_var, size_var, tlab_next_addr_var, new_next_var;
	guint32 slowpath_branch;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	static gboolean registered = FALSE;

	MONO_THREAD_VAR_OFFSET (tlab_next_addr, tlab_next_addr_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	g_assert (tlab_next_addr_offset != -1);
	g_assert (tlab_temp_end_offset != -1);

	g_assert (atype == ATYPE_NORMAL);

	if (!registered) {
		mono_register_jit_icall (mono_gc_alloc_obj, "mono_gc_alloc_obj", mono_create_icall_signature ("object ptr int"), FALSE);
		registered = TRUE;
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	csig->ret = &mono_defaults.object_class->byval_arg;
	csig->params [0] = &mono_defaults.int_class->byval_arg;

	mb = mono_mb_new (mono_defaults.object_class, "Alloc", MONO_WRAPPER_ALLOC);
	size_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	/* size = vtable->klass->instance_size; */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoClass, instance_size));
	mono_mb_emit_byte (mb, CEE_ADD);
	/* FIXME: assert instance_size stays a 4 byte integer */
	mono_mb_emit_byte (mb, CEE_LDIND_U4);
	mono_mb_emit_stloc (mb, size_var);

	/* size += ALLOC_ALIGN - 1; */
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_icon (mb, ALLOC_ALIGN - 1);
	mono_mb_emit_byte (mb, CEE_ADD);
	/* size &= ~(ALLOC_ALIGN - 1); */
	mono_mb_emit_icon (mb, ~(ALLOC_ALIGN - 1));
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_stloc (mb, size_var);

	/*
	 * We need to modify tlab_next, but the JIT only supports reading, so we read
	 * another tls var holding its address instead.
	 */

	/* tlab_next_addr (local) = tlab_next_addr (TLS var) */
	tlab_next_addr_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_TLS);
	mono_mb_emit_i4 (mb, tlab_next_addr_offset);
	mono_mb_emit_stloc (mb, tlab_next_addr_var);

	/* p = (void**)tlab_next; */
	p_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, p_var);
	
	/* new_next = (char*)p + size; */
	new_next_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, new_next_var);

	/* tlab_next = new_next */
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_ldloc (mb, new_next_var);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/* if (G_LIKELY (new_next < tlab_temp_end)) */
	mono_mb_emit_ldloc (mb, new_next_var);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_TLS);
	mono_mb_emit_i4 (mb, tlab_temp_end_offset);
	slowpath_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLT_UN_S);

	/* Slowpath */

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	/* FIXME: mono_gc_alloc_obj takes a 'size_t' as an argument, not an int32 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_icall (mb, mono_gc_alloc_obj);
	mono_mb_emit_byte (mb, CEE_RET);

	/* Fastpath */
	mono_mb_patch_short_branch (mb, slowpath_branch);

	/* FIXME: Memory barrier */

	/* *p = vtable; */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);
	
	/* return p */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, csig, 8);
	mono_mb_free (mb);
	mono_method_get_header (res)->init_locals = FALSE;
	return res;
}

static MonoMethod* alloc_method_cache [ATYPE_NUM];

/*
 * Generate an allocator method implementing the fast path of mono_gc_alloc_obj ().
 * The signature of the called method is:
 * 	object allocate (MonoVTable *vtable)
 */
MonoMethod*
mono_gc_get_managed_allocator (MonoVTable *vtable, gboolean for_box)
{
	int tlab_next_offset = -1;
	int tlab_temp_end_offset = -1;
	MonoClass *klass = vtable->klass;
	MONO_THREAD_VAR_OFFSET (tlab_next, tlab_next_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	if (tlab_next_offset == -1 || tlab_temp_end_offset == -1)
		return NULL;
	if (klass->instance_size > tlab_size)
		return NULL;
	if (klass->has_finalize || klass->marshalbyref || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank)
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING)
		return NULL;
	if (collect_before_allocs)
		return NULL;

	return mono_gc_get_managed_allocator_by_type (0);
}

int
mono_gc_get_managed_allocator_type (MonoMethod *managed_alloc)
{
	return 0;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype)
{
	MonoMethod *res;

	mono_loader_lock ();
	res = alloc_method_cache [atype];
	if (!res)
		res = alloc_method_cache [atype] = create_allocator (atype);
	mono_loader_unlock ();
	return res;
}

#endif /* HAVE_SGEN_GC */

