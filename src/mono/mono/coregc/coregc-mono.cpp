/**
 * \file
 * GC implementation using CoreCLR GC
 *
 * Copyright 2019 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */


#include "config.h"

#define THREAD_INFO_TYPE CoreGCThreadInfo
struct _CoreGCThreadInfo;

typedef struct _CoreGCThreadInfo CoreGCThreadInfo;

#include <glib.h>
#include <sys/sysctl.h>
#include "sgen/sgen-archdep.h"
#include "sgen/gc-internal-agnostic.h"
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/abi-details.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-forward.h>
#include <mono/metadata/null-gc-handles.h>

#include "coregc-mono.h"
#include "volatile.h"
#include "gceventstatus.h"
#include "coregc-mono-mtflags.h"

struct _CoreGCThreadInfo {
	MonoThreadInfo info;
	gc_alloc_context alloc_context;
	gboolean skip, suspend_done;

	void *stack_end;
	void *stack_start;
	void *stack_start_limit;

	MonoContext ctx;
};

static gboolean gc_inited = FALSE;

G_BEGIN_DECLS

HRESULT GC_Initialize(IGCToCLR* clrToGC, IGCHeap** gcHeap, IGCHandleManager** gcHandleManager, GcDacVars* gcDacVars);

static IGCHeap *pGCHeap;
static IGCHandleManager *pGCHandleManager;

static void coregc_roots_init (void);

MonoCoopMutex coregc_mutex;

void
coregc_lock (void)
{
	mono_coop_mutex_lock (&coregc_mutex);
}

void
coregc_unlock (void)
{
	mono_coop_mutex_unlock (&coregc_mutex);
}

static void
mono_init_coregc (void)
{
	//
	// Initialize GC heap
	//
	GcDacVars dacVars;
	if (GC_Initialize(nullptr, &pGCHeap, &pGCHandleManager, &dacVars) != S_OK)
		g_assert_not_reached ();

	if (FAILED(pGCHeap->Initialize()))
		g_assert_not_reached ();

	//
	// Initialize handle manager
	//
	if (!pGCHandleManager->Initialize())
		g_assert_not_reached ();
}

void
mono_gc_base_init (void)
{
	if (gc_inited)
		return;

	mono_counters_init ();

#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

	mono_thread_callbacks_init ();
	mono_thread_info_init (sizeof (THREAD_INFO_TYPE));

	mono_init_coregc ();

	mono_coop_mutex_init (&coregc_mutex);

	coregc_roots_init ();

	gc_inited = TRUE;
}

void
mono_gc_base_cleanup (void)
{
}

void
mono_gc_init_icalls (void)
{
}

void
mono_gc_collect (int generation)
{
	pGCHeap->GarbageCollect(generation, false, collection_blocking);
}

int
mono_gc_max_generation (void)
{
	return 0;
}

guint64
mono_gc_get_allocated_bytes_for_current_thread (void)
{
	return 0;
}

int
mono_gc_get_generation  (MonoObject *object)
{
	return 0;
}

int
mono_gc_collection_count (int generation)
{
	return 0;
}

void
mono_gc_add_memory_pressure (gint64 value)
{
}

/* maybe track the size, not important, though */
int64_t
mono_gc_get_used_size (void)
{
	g_assert_not_reached ();
}

int64_t
mono_gc_get_heap_size (void)
{
	g_assert_not_reached ();
}

gboolean
mono_gc_is_gc_thread (void)
{
	return TRUE;
}

int
mono_gc_walk_heap (int flags, MonoGCReferences callback, void *data)
{
	g_assert_not_reached ();
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	g_assert_not_reached ();
}

// Root registering
#if TARGET_SIZEOF_VOID_P == 4
typedef guint32 target_mword;
#else
typedef guint64 target_mword;
#endif

typedef target_mword RootDescriptor;

typedef struct _RootRecord RootRecord;
struct _RootRecord {
	char *end_root;
	RootDescriptor root_desc;
	int source;
	const char *msg;
};

enum {
	ROOT_TYPE_NORMAL = 0, /* "normal" roots */
	ROOT_TYPE_PINNED = 1, /* roots without a GC descriptor */
	ROOT_TYPE_WBARRIER = 2, /* roots with a write barrier */
	ROOT_TYPE_NUM
};

GHashTable* coregc_roots_hash [ROOT_TYPE_NUM];

static void
coregc_roots_init (void)
{
	int i;
	for (i = 0; i < ROOT_TYPE_NUM; i++) {
		coregc_roots_hash [i] = g_hash_table_new (NULL, NULL);
	}
}

int
coregc_register_root (char *start, size_t size, RootDescriptor descr, int root_type, MonoGCRootSource source, void *key, const char *msg)
{
	RootRecord *new_root;
	int i;

	coregc_lock ();
	for (i = 0; i < ROOT_TYPE_NUM; i++) {
		RootRecord *root = (RootRecord *)g_hash_table_lookup (coregc_roots_hash [i], start);
		/* we allow changing the size and the descriptor (for thread statics etc) */
		if (root) {
			root->end_root = start + size;
			root->root_desc = descr;
			coregc_unlock ();
			return TRUE;
		}
	}

	new_root = (RootRecord*) malloc (sizeof (RootRecord));
	new_root->end_root = start + size;
	new_root->root_desc = descr;
	new_root->source = source;
	new_root->msg = msg;

	g_hash_table_replace (coregc_roots_hash [root_type], start, new_root);

	coregc_unlock ();
	return TRUE;
}

void
coregc_deregister_root (char* addr)
{
	int root_type;

	coregc_lock ();
	for (root_type = 0; root_type < ROOT_TYPE_NUM; ++root_type) {
		// FIXME free root
		g_hash_table_remove (coregc_roots_hash [root_type], addr);
	}
	coregc_unlock ();
}

int
mono_gc_register_root (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	return coregc_register_root (start, size, (RootDescriptor)descr, descr ? ROOT_TYPE_NORMAL : ROOT_TYPE_PINNED, source, key, msg);
}

int
mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	// FIXME we should kill this api and have these allocated as managed objects that automatically use card tables
	return mono_gc_register_root (start, size, descr, source, key, msg);
}

void
mono_gc_deregister_root (char* addr)
{
	coregc_deregister_root (addr);
}


typedef union {
	mono_gc_descr struct_gc_descr;
	MonoGCDescriptor ptr_gc_descr;
} mono_gc_descr_union;


#define BITMAP_EL_SIZE (sizeof (gsize) * 8)

#define MIN_OBJECT_SIZE (4 * sizeof (void*))

MonoGCDescriptor
mono_gc_make_descr_for_object (gpointer klass, gsize *bitmap, int numbits, size_t obj_size, GPtrArray **gc_descr_full)
{
	MonoClass *casted_class = (MonoClass*) klass;
	gboolean has_ptrs = FALSE;
	// Include the Header expected by the GC
	obj_size += 8;
	mono_gc_descr_union gc_descr;
	gc_descr.struct_gc_descr.m_componentSize = 0; // not array or string
	gc_descr.struct_gc_descr.m_flags = 0;
	if (mono_class_has_finalizer(casted_class))
		gc_descr.struct_gc_descr.m_flags |= MTFlag_HasFinalizer;
	gc_descr.struct_gc_descr.m_baseSize = obj_size;
	if (gc_descr.struct_gc_descr.m_baseSize < MIN_OBJECT_SIZE)
		gc_descr.struct_gc_descr.m_baseSize = MIN_OBJECT_SIZE;


	printf("mono_gc_make_descr_for_object: gc_descr.struct_gc_descr.m_baseSize (dec): %d\n", gc_descr.struct_gc_descr.m_baseSize);



	GPtrArray *full = g_ptr_array_new ();

	size_t last_start_offset = -1;
	size_t serie_size = 0;
	// Space for series length

	g_ptr_array_add (full, NULL);
	for (int i = 0; i < numbits; i++) {
		int is_set = bitmap [i / BITMAP_EL_SIZE] & (((gsize)1) << (i % BITMAP_EL_SIZE));
		if (is_set) {
			has_ptrs = TRUE;
			if (last_start_offset == -1)
				last_start_offset = i * SIZEOF_VOID_P;
			serie_size += SIZEOF_VOID_P;
		} else if (last_start_offset != -1) {
			g_assert (serie_size > 0);
			g_ptr_array_add (full, (gpointer)last_start_offset);
			g_ptr_array_add (full, (gpointer)(serie_size - obj_size));
			serie_size = 0;
			last_start_offset = -1;
		}
	}
	if (last_start_offset != -1) {
		g_assert (serie_size > 0);
		g_ptr_array_add (full, (gpointer)last_start_offset);
		g_ptr_array_add (full, (gpointer)(serie_size - obj_size));
	}
	full->pdata [0] = (gpointer)((size_t)full->len / 2);

	// Reverse the array so it can be stored directly in the vtable
	for (int i = 0; i < full->len / 2; i++) {
		gpointer tmp = full->pdata [i];
		full->pdata [i] = full->pdata [full->len - 1 - i];
		full->pdata [full->len - 1 - i] = tmp;
	}

	*gc_descr_full = full;
	if (has_ptrs)
		gc_descr.struct_gc_descr.m_flags |= MTFlag_ContainsPointers;

	return gc_descr.ptr_gc_descr;
}

MonoGCDescriptor
mono_gc_make_descr_for_string (gsize *bitmap, int numbits, GPtrArray **gc_descr_full)
{
	mono_gc_descr_union gc_descr;
	gc_descr.struct_gc_descr.m_componentSize = 2;
	gc_descr.struct_gc_descr.m_flags = MTFlag_IsArray | MTFlag_IsString | MTFlag_HasComponentSize;
	gc_descr.struct_gc_descr.m_baseSize = MONO_SIZEOF_MONO_STRING + 8;



	return gc_descr.ptr_gc_descr;
}

MonoGCDescriptor
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size, GPtrArray **gc_descr_full)
{
	mono_gc_descr_union gc_descr;
	gc_descr.struct_gc_descr.m_componentSize = elem_size;
	gc_descr.struct_gc_descr.m_flags = MTFlag_IsArray | MTFlag_HasComponentSize;
	gc_descr.struct_gc_descr.m_baseSize = MONO_SIZEOF_MONO_ARRAY;

	if (numbits == 1 && elem_size == sizeof (gpointer) && *elem_bitmap == 1) {
		// Array of references. We don't handle array of value types yet.
		gc_descr.struct_gc_descr.m_flags |= MTFlag_ContainsPointers;
		GPtrArray *full = g_ptr_array_new ();
		// The series size will be added with the entire array size, resulting
		// in scanning everything
		g_ptr_array_add (full, (gpointer) (-1 * MONO_SIZEOF_MONO_ARRAY));
		// Add start offset
		g_ptr_array_add (full, (gpointer)MONO_SIZEOF_MONO_ARRAY);
		g_ptr_array_add (full, (gpointer)1);
		*gc_descr_full = full;
	}
	return gc_descr.ptr_gc_descr;
}

// Root descriptors

enum {
        ROOT_DESC_CONSERVATIVE, /* 0, so matches NULL value */
        ROOT_DESC_BITMAP,
        ROOT_DESC_RUN_LEN,
        ROOT_DESC_COMPLEX,
        ROOT_DESC_VECTOR,
        ROOT_DESC_USER,
        ROOT_DESC_TYPE_MASK = 0x7,
        ROOT_DESC_TYPE_SHIFT = 3,
};

typedef void (*UserMarkFunc)     (MonoObject **addr, void *gc_data);
typedef void (*UserRootMarkFunc) (void *addr, UserMarkFunc mark_func, void *gc_data);

#define GC_BITS_PER_WORD (sizeof (target_mword) * 8)
#define LOW_TYPE_BITS 3

#define MAX_USER_DESCRIPTORS 16

#define MAKE_ROOT_DESC(type,val) ((type) | ((val) << ROOT_DESC_TYPE_SHIFT))

static gsize* complex_descriptors = NULL;
static int complex_descriptors_size = 0;
static int complex_descriptors_next = 0;

static UserRootMarkFunc user_descriptors [MAX_USER_DESCRIPTORS];
static int user_descriptors_next = 0;
static RootDescriptor all_ref_root_descrs [32];

static int
alloc_complex_descriptor (gsize *bitmap, int numbits)
{
	int nwords, res, i;

	numbits = ALIGN_TO (numbits, GC_BITS_PER_WORD);
	nwords = numbits / GC_BITS_PER_WORD + 1;

	coregc_lock ();
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
				coregc_unlock ();
				return i;
			}
		}
		i += (int)complex_descriptors [i];
	}
	if (complex_descriptors_next + nwords > complex_descriptors_size) {
		int new_size = complex_descriptors_size * 2 + nwords;
		complex_descriptors = g_realloc (complex_descriptors, new_size * sizeof (gsize));
		complex_descriptors_size = new_size;
	}
	complex_descriptors_next += nwords;
	complex_descriptors [res] = nwords;
	for (i = 0; i < nwords - 1; ++i) {
		complex_descriptors [res + 1 + i] = bitmap [i];
	}
	coregc_unlock ();
	return res;
}

static void*
coreclr_get_complex_descriptor_bitmap (RootDescriptor desc)
{
        return complex_descriptors + (desc >> ROOT_DESC_TYPE_SHIFT);
}

static gsize*
coreclr_get_complex_descriptor (RootDescriptor desc)
{
        return complex_descriptors + (desc >> LOW_TYPE_BITS);
}

/**
 * mono_gc_make_descr_from_bitmap:
 */
MonoGCDescriptor
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	if (numbits == 0) {
		return (MonoGCDescriptor)MAKE_ROOT_DESC (ROOT_DESC_BITMAP, 0);
	} else if (numbits < ((sizeof (*bitmap) * 8) - ROOT_DESC_TYPE_SHIFT)) {
		return (MonoGCDescriptor)MAKE_ROOT_DESC (ROOT_DESC_BITMAP, bitmap [0]);
	} else {
		RootDescriptor complex = alloc_complex_descriptor (bitmap, numbits);
		return (MonoGCDescriptor)MAKE_ROOT_DESC (ROOT_DESC_COMPLEX, complex);
	}
}

MonoGCDescriptor
mono_gc_make_vector_descr (void)
{
	return (MonoGCDescriptor)MAKE_ROOT_DESC (ROOT_DESC_VECTOR, 0);
}

MonoGCDescriptor
mono_gc_make_root_descr_all_refs (int numbits)
{
	gsize *gc_bitmap;
	RootDescriptor descr;
	int num_bytes = numbits / 8;

	if (numbits < 32 && all_ref_root_descrs [numbits])
		return (MonoGCDescriptor)all_ref_root_descrs [numbits];

	gc_bitmap = (gsize *)g_malloc0 (ALIGN_TO (ALIGN_TO (numbits, 8) + 1, sizeof (gsize)));
	memset (gc_bitmap, 0xff, num_bytes);
	if (numbits < ((sizeof (*gc_bitmap) * 8) - ROOT_DESC_TYPE_SHIFT))
		gc_bitmap[0] = GUINT64_TO_LE(gc_bitmap[0]);
	else if (numbits && num_bytes % (sizeof (*gc_bitmap)))
		gc_bitmap[num_bytes / 8] = GUINT64_TO_LE(gc_bitmap [num_bytes / 8]);
	if (numbits % 8)
		gc_bitmap [numbits / 8] = (1 << (numbits % 8)) - 1;
	descr = (RootDescriptor)mono_gc_make_descr_from_bitmap (gc_bitmap, numbits);
	g_free (gc_bitmap);

	if (numbits < 32)
		all_ref_root_descrs [numbits] = descr;

	return (MonoGCDescriptor)descr;
}

static RootDescriptor
coreclr_make_user_root_descriptor (UserRootMarkFunc marker)
{
	RootDescriptor descr;

	g_assert (user_descriptors_next < MAX_USER_DESCRIPTORS);
	descr = MAKE_ROOT_DESC (ROOT_DESC_USER, (RootDescriptor)user_descriptors_next);
	user_descriptors [user_descriptors_next ++] = marker;

	return descr;
}

static UserRootMarkFunc
coreclr_get_user_descriptor_func (RootDescriptor desc)
{
	return user_descriptors [desc >> ROOT_DESC_TYPE_SHIFT];
}

MonoObject*
mono_gc_alloc_fixed (size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	void *res = g_calloc (1, size);
	if (!res)
		return NULL;
	if (!mono_gc_register_root ((char *)res, size, descr, source, key, msg)) {
		g_free (res);
		res = NULL;
	}

	printf("mono_gc_alloc_fixed: %p\n", res);
	return (MonoObject*)res;
}

MonoObject*
mono_gc_alloc_fixed_no_descriptor (size_t size, MonoGCRootSource source, void *key, const char *msg)
{
	return mono_gc_alloc_fixed (size, 0, source, key, msg);
}

void
mono_gc_free_fixed (void* addr)
{
	mono_gc_deregister_root ((char *)addr);
	g_free (addr);
}

MonoObject*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *o;
	uint32_t flags = 0;
	// Add header size that the runtime doesn't know about.
	size += sizeof (gpointer);
	if (size < MIN_OBJECT_SIZE)
		size = MIN_OBJECT_SIZE;
	if (mono_class_has_finalizer(vtable->klass))
		flags |= GC_ALLOC_FINALIZE;

	// It looks like the AllocLHeap to allocate directly in the LargeObject heap
	// is no longe present in CoreCLR. I'm not sure if we need it; Alloc
	// should decide which I think works just fine?
	// Ask Vlad why his change was explicitly allocated in large object space. 
	CoreGCThreadInfo *info = (CoreGCThreadInfo*) mono_thread_info_current ();
	o = (MonoObject*) pGCHeap->Alloc (&info->alloc_context, size, flags);



	o->vtable = vtable;

	// Deubgging
	MethodTable* mt = (MethodTable*)(o->vtable);
	printf("mono_gc_alloc_obj: %p o->vtable: %p, o->vtable->gc_descr: %llu, size: %ld, o->vtable->gc_descr.m_baseSize: %d\n", o, o->vtable, o->vtable->gc_descr, size, mt->GetBaseSize());
	
	return o;
}

MonoArray*
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *arr = (MonoArray*) mono_gc_alloc_obj (vtable, size);
	arr->max_length = max_length;
	return arr;
}

MonoArray*
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size)
{
	MonoArray *arr = (MonoArray*) mono_gc_alloc_obj (vtable, size);
	arr->max_length = max_length;
    arr->bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
	return arr;
}

MonoString*
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len)
{
	MonoString *str = (MonoString*) mono_gc_alloc_obj (vtable, size);
	str->length = len;
	return str;
}

MonoObject*
mono_gc_alloc_mature (MonoVTable *vtable, size_t size)
{
	return mono_gc_alloc_obj (vtable, size);
}

MonoObject*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *o = mono_gc_alloc_obj (vtable, size);
	pGCHandleManager->CreateGlobalHandleOfType ((Object*)o, HNDTYPE_PINNED);
	return o;
}

MonoArray*
mono_gc_alloc_pinned_vector (MonoVTable *vtable, size_t size, uintptr_t max_length){
	MonoArray *arr = (MonoArray*) mono_gc_alloc_obj (vtable, size);
	arr->max_length = max_length;
	pGCHandleManager->CreateGlobalHandleOfType ((Object*)arr, HNDTYPE_PINNED);
	return arr;
}


void mono_gc_get_gcmemoryinfo (gint64* fragmented_bytes,
						       gint64* heap_size_bytes,
						       gint64* high_memory_load_threshold_bytes,
						       gint64* memory_load_bytes,
 						       gint64* total_available_memory_bytes){									
}

static int
get_size_for_vtable (gpointer vtable, gpointer o)
{
	MonoClass *klass = ((MonoVTable*)vtable)->klass;

	// FIXME: use gc desc for fast path

	/*
	 * We depend on mono_string_length_fast and
	 * mono_array_length_internal not using the object's vtable.
	 */
	if (klass == mono_defaults.string_class) {
		return MONO_SIZEOF_MONO_STRING + 2 * mono_string_length_fast ((MonoString*) o) + 2;
	} else if (m_class_get_rank (klass)) {
		g_error ("niy");
		// return sgen_mono_array_size (vtable, (MonoArray*)o, NULL, 0);
	} else {
		/* from a created object: the class must be inited already */
		return m_class_get_instance_size (klass);
	}
}

#if TARGET_SIZEOF_VOID_P == 8
#define card_byte_shift     11
#else
#define card_byte_shift     10
#endif

#define card_byte(addr) (((size_t)(addr)) >> card_byte_shift)

extern "C" guint32* g_gc_card_table;
extern "C" guint8* g_gc_lowest_address;
extern "C" guint8* g_gc_highest_address;

static void
coregc_mark_card_table (gpointer addr)
{
    if (((guint8 *) addr < g_gc_lowest_address) || ((guint8 *) addr >= g_gc_highest_address))
        return;

    // volatile is used here to prevent fetch of g_card_table from being reordered
    // with g_lowest/highest_address check above. See comments in StompWriteBarrier
    guint8* cardByte = (guint8 *)*(volatile guint8 **)(&g_gc_card_table) + card_byte((guint8 *)addr);
	*cardByte = 0xff;
}

#ifndef MAX
#define MAX(a,b) (((a)>(b)) ? (a) : (b))
#endif

static int
number_of_cards (gpointer start, int size)
{
	gpointer end = (guint8 *) start + MAX (1, size) - 1;
	return ((intptr_t) end >> card_byte_shift) - ((intptr_t) start >> card_byte_shift) + 1;
}

void
mono_gc_wbarrier_set_field_internal (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	*(MonoObject**)field_ptr = value;
	coregc_mark_card_table (field_ptr);
}

void
mono_gc_wbarrier_set_arrayref_internal (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	*(MonoObject**)slot_ptr = value;
	coregc_mark_card_table (slot_ptr);
}

void
mono_gc_wbarrier_arrayref_copy_internal (gpointer dest_ptr, gconstpointer src_ptr, int count)
{
	gpointer *dest = (gpointer*)dest_ptr;
	gpointer *src = (gpointer*)src_ptr;
	gpointer *end = dest + count;
	for (; dest < end; ++src, ++dest) {
		*dest = *src;
		coregc_mark_card_table (dest);
	}
}

void
mono_gc_wbarrier_generic_store_internal (void volatile* ptr, MonoObject* value)
{
	*(MonoObject**)ptr = value;
	coregc_mark_card_table ((gpointer)ptr);
}

void
mono_gc_wbarrier_generic_store_atomic_internal (gpointer ptr, MonoObject *value)
{
	mono_atomic_store_ptr ((volatile gpointer *)ptr, value);
	coregc_mark_card_table (ptr);
}

void
mono_gc_wbarrier_generic_nostore_internal (gpointer ptr)
{
	coregc_mark_card_table (ptr);
}

void
mono_gc_wbarrier_value_copy_internal (gpointer dest, gconstpointer src, int count, MonoClass *klass)
{
	int size = count * mono_class_value_size (klass, NULL);
	mono_gc_memmove_atomic (dest, src, size);

	// FIXME use memset
	for (int i = 0; i < size; i++)
		coregc_mark_card_table ((char *) dest + i);
}

void
mono_gc_wbarrier_object_copy_internal (MonoObject* obj, MonoObject *src)
{
	size_t size = get_size_for_vtable (obj->vtable, obj);

	// TLAB_ACCESS_INIT;
	// ENTER_CRITICAL_REGION;

	mono_gc_memmove_aligned ((char *) obj + COREGC_CLIENT_OBJECT_HEADER_SIZE, (char *) src + COREGC_CLIENT_OBJECT_HEADER_SIZE, size - COREGC_CLIENT_OBJECT_HEADER_SIZE);

	// FIXME use memset
	for (int i = 0; i < size; i++)
		coregc_mark_card_table ((char *) obj + i);

	// EXIT_CRITICAL_REGION;
}

gboolean
mono_gc_is_critical_method (MonoMethod *method)
{
	g_assert_not_reached ();
}

gpointer
mono_gc_thread_attach (THREAD_INFO_TYPE * info)
{
	info->info.handle_stack = mono_handle_stack_alloc ();
	return info;
}

void
mono_gc_thread_detach_with_lock (THREAD_INFO_TYPE *p)
{
}

gboolean
mono_gc_thread_in_critical_region (THREAD_INFO_TYPE *info)
{
	return FALSE;
}

int
mono_gc_get_aligned_size_for_allocator (int size)
{
	g_assert_not_reached ();
}

MonoMethod*
mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box, gboolean known_instance_size)
{
	return NULL;
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoClass *klass)
{
	return NULL;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype, ManagedAllocatorVariant variant)
{
	return NULL;
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return 0;
}

const char *
mono_gc_get_gc_name (void)
{
	return "coregc";
}

void
mono_gc_clear_domain (MonoDomain *domain)
{
}

void
mono_gc_suspend_finalizers (void)
{
	g_assert_not_reached ();
}

int
mono_gc_get_suspend_signal (void)
{
	return -1;
}

int
mono_gc_get_restart_signal (void)
{
	return -1;
}

MonoMethod*
mono_gc_get_specific_write_barrier (gboolean is_concurrent)
{
	g_assert_not_reached ();
	return NULL;
}

MonoMethod*
mono_gc_get_write_barrier (void)
{
	return NULL;
}

void*
mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data)
{
	g_assert_not_reached ();
}

char*
mono_gc_get_description (void)
{
	return g_strdup ("coregc");
}

void
mono_gc_set_desktop_mode (void)
{
	g_assert_not_reached ();
}

gboolean
mono_gc_is_moving (void)
{
	return TRUE;
}

gboolean
mono_gc_is_disabled (void)
{
	return FALSE;
}

void
mono_gc_wbarrier_range_copy (gpointer _dest, gconstpointer _src, int size)
{
	g_assert_not_reached ();
}

MonoRangeCopyFunction
mono_gc_get_range_copy_func (void)
{
	return &mono_gc_wbarrier_range_copy;
}

guint8*
mono_gc_get_card_table (int *shift_bits, gpointer *card_mask)
{
	g_assert_not_reached ();
	return NULL;
}

guint8*
mono_gc_get_target_card_table (int *shift_bits, target_mgreg_t *card_mask)
{
	g_assert_not_reached ();
}

gboolean
mono_gc_card_table_nursery_check (void)
{
	g_assert_not_reached ();
	return TRUE;
}

void
mono_gc_register_obj_with_weak_fields (void *obj)
{
	g_assert_not_reached ();
}

void*
mono_gc_get_nursery (int *shift_bits, size_t *size)
{
	g_assert_not_reached ();
	return NULL;
}

gboolean
mono_gc_precise_stack_mark_enabled (void)
{
	g_assert_not_reached ();
	return FALSE;
}

FILE *
mono_gc_get_logfile (void)
{
	return NULL;
}

void
mono_gc_params_set (const char* options)
{
}

void
mono_gc_debug_set (const char* options)
{
}

void
mono_gc_conservatively_scan_area (void *start, void *end)
{
	g_assert_not_reached ();
}

void *
mono_gc_scan_object (void *obj, void *gc_data)
{
	g_assert_not_reached ();
	return NULL;
}

gsize*
mono_gc_get_bitmap_for_descr (MonoGCDescriptor descr, int *numbits)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks)
{
}

void
mono_gc_set_stack_end (void *stack_end)
{
	CoreGCThreadInfo *info;

	coregc_lock ();
	info = mono_thread_info_current ();
	if (info) {
		info->stack_end = stack_end;
	}
	coregc_unlock ();
}

int
mono_gc_get_los_limit (void)
{
	g_assert_not_reached ();
}

gboolean
mono_gc_user_markers_supported (void)
{
	return FALSE;
}

MonoGCDescriptor
mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker)
{
	g_assert_not_reached ();
	return MONO_GC_DESCRIPTOR_NULL;
}

#ifndef HOST_WIN32
int
mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	return pthread_create (new_thread, attr, start_routine, arg);
}
#endif

void
mono_gc_skip_thread_changing (gboolean skip)
{
	coregc_lock ();

        if (skip) {
                /*
                 * If we skip scanning a thread with a non-empty handle stack, we may move an
                 * object but fail to update the reference in the handle.
                 */
                HandleStack *stack = mono_thread_info_current ()->info.handle_stack;
                g_assert (stack == NULL || mono_handle_stack_is_empty (stack));
        }
}

void
mono_gc_skip_thread_changed (gboolean skip)
{
	coregc_unlock ();
}

#ifdef HOST_WIN32
BOOL APIENTRY mono_gc_dllmain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	return TRUE;
}
#endif

MonoVTable *
mono_gc_get_vtable (MonoObject *obj)
{
	g_assert_not_reached ();
	// No pointer tagging.
	return obj->vtable;
}

guint
mono_gc_get_vtable_bits (MonoClass *klass)
{
	// FIXME we could store the HasFinalizer here instead of the gc descr
	return 0;
}

void
mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size)
{
}

gboolean
mono_gc_is_null (void)
{
	return FALSE;
}

int
mono_gc_invoke_finalizers (void)
{
	return 0;
}

MonoBoolean
mono_gc_pending_finalizers (void)
{
	return FALSE;
}

gboolean
mono_gc_ephemeron_array_add (MonoObject *obj)
{
	return TRUE;
}

guint64 mono_gc_get_total_allocated_bytes (MonoBoolean precise)
{
	return 0;
}


MonoObject*
mono_gchandle_get_target_internal (MonoGCHandle gchandle)
{
	/* TODO(naricc): Does this cast still work? */
	return *((MonoObject**)gchandle);
}

gboolean
mono_gchandle_is_in_domain (MonoGCHandle gchandle, MonoDomain *domain)
{
	g_assert_not_reached ();
}

MonoGCHandle
mono_gchandle_new_internal (MonoObject *obj, gboolean pinned)
{
	if (pinned)
		return (MonoGCHandle)pGCHandleManager->CreateGlobalHandleOfType ((Object*)obj, HNDTYPE_PINNED);
	else
		return (MonoGCHandle)pGCHandleManager->CreateGlobalHandleOfType ((Object*)obj, HNDTYPE_STRONG);
}

MonoGCHandle
mono_gchandle_new_weakref_internal (MonoObject* obj, gboolean track_resurrection)
{
	if (track_resurrection)
		return pGCHandleManager->CreateGlobalHandleOfType ((Object*)obj, HNDTYPE_WEAK_LONG);
	else
		return pGCHandleManager->CreateGlobalHandleOfType ((Object*)obj, HNDTYPE_WEAK_SHORT);
}

void  
mono_gc_finalize_domain (MonoDomain *domain){
}

void  
mono_gc_register_for_finalization (MonoObject *obj, MonoFinalizationProc user_data){
}

void
mono_gc_thread_detach (THREAD_INFO_TYPE *info)
{
}

void
mono_gchandle_set_target (MonoGCHandle gchandle, MonoObject *obj)
{
	*((MonoObject**)gchandle) = obj;
}

void
mono_gchandle_free_internal (MonoGCHandle gchandle)
{
	if (gchandle != NULL)
		pGCHandleManager->DestroyHandleOfUnknownType ((OBJECTHANDLE)gchandle);
}

void
mono_gchandle_free_domain (MonoDomain *unloading)
{
	g_assert_not_reached ();
}

G_END_DECLS

// Interface which coregc library links against

#define THREADS_STW_DEBUG(...)

static gboolean
coreclr_is_thread_in_current_stw (THREAD_INFO_TYPE *info, int *reason)
{
	/*
	 * No need to check MONO_THREAD_INFO_FLAGS_NO_GC here as we rely on the
	 * FOREACH_THREAD_EXCLUDE macro to skip such threads for us.
	 */

	/*
	We have detected that this thread is failing/dying, ignore it.
	FIXME: can't we merge this with thread_is_dying?
	*/
	if (info->skip) {
		if (reason)
			*reason = 2;
		return FALSE;
	}

	/*
	Suspending the current thread will deadlock us, bad idea.
	*/
	if (info == mono_thread_info_current ()) {
		if (reason)
			*reason = 3;
		return FALSE;
	}

	/*
	We can't suspend the workers that will do all the heavy lifting.
	FIXME Use some state bit in SgenThreadInfo for this.
	*/
        #if 0
	if (sgen_thread_pool_is_thread_pool_thread (mono_thread_info_get_tid (info))) {
		if (reason)
			*reason = 4;
		return FALSE;
	}
        #endif

	/*
	The thread has signaled that it started to detach, ignore it.
	FIXME: can't we merge this with skip
	*/
	if (!mono_thread_info_is_live (info)) {
		if (reason)
			*reason = 5;
		return FALSE;
	}

	return TRUE;
}

static void
coregc_unified_suspend_stop_world (void)
{
	int sleep_duration = -1;

	// we can't lead STW if we promised not to safepoint.
	g_assert (!mono_thread_info_will_not_safepoint (mono_thread_info_current ()));

	mono_threads_begin_global_suspend ();
	THREADS_STW_DEBUG ("[GC-STW-BEGIN][%p] *** BEGIN SUSPEND *** \n", mono_thread_info_get_tid (mono_thread_info_current ()));

	for (MonoThreadSuspendPhase phase = MONO_THREAD_SUSPEND_PHASE_INITIAL; phase < MONO_THREAD_SUSPEND_PHASE_COUNT; phase++) {
		gboolean need_next_phase = FALSE;
		FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
			/* look at every thread in the first phase. */
			if (phase == MONO_THREAD_SUSPEND_PHASE_INITIAL) {
                                #if 1
				info->skip = FALSE;
				info->suspend_done = FALSE;
                                #endif
			} else {
				/* skip threads suspended by previous phase. */
				/* threads with info->skip set to TRUE will be skipped by coreclr_is_thread_in_current_stw. */
                                #if 1
				if (info->suspend_done)
					continue;
                                #endif
			}

			int reason;
			if (!coreclr_is_thread_in_current_stw(info, &reason)) {
				THREADS_STW_DEBUG ("[GC-STW-BEGIN-SUSPEND-%d] IGNORE thread %p skip %s reason %d\n", (int)phase, mono_thread_info_get_tid (info), info->skip ? "true" : "false", reason);
				continue;
			}

			switch (mono_thread_info_begin_suspend (info, phase)) {
			case MONO_THREAD_BEGIN_SUSPEND_SUSPENDED:
                                #if 1
				info->skip = FALSE;
                                #endif
				break;
			case MONO_THREAD_BEGIN_SUSPEND_SKIP:
                                #if 1
				info->skip = TRUE;
                                #endif
				break;
			case MONO_THREAD_BEGIN_SUSPEND_NEXT_PHASE:
				need_next_phase = TRUE;
				break;
			default:
				g_assert_not_reached ();
			}

			THREADS_STW_DEBUG ("[GC-STW-BEGIN-SUSPEND-%d] SUSPEND thread %p skip %s\n", (int)phase, mono_thread_info_get_tid (info), info->skip ? "true" : "false");
		} FOREACH_THREAD_END;

                #if 1
		mono_thread_info_current ()->suspend_done = TRUE;
                #endif
		mono_threads_wait_pending_operations ();

		if (!need_next_phase)
			break;
	}

	for (;;) {
		gint restart_counter = 0;

		FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
			gint suspend_count;

			int reason = 0;
			if (info->suspend_done || !coreclr_is_thread_in_current_stw (info, &reason)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE RESUME thread %p not been processed done %d current %d reason %d\n", mono_thread_info_get_tid (info), info->suspend_done, !coreclr_is_thread_in_current_stw (info, NULL), reason);
				continue;
			}

			/*
			All threads that reach here are pristine suspended. This means the following:

			- We haven't accepted the previous suspend as good.
			- We haven't gave up on it for this STW (it's either bad or asked not to)
			*/
			if (!mono_thread_info_in_critical_location (info)) {
				info->suspend_done = TRUE;

				THREADS_STW_DEBUG ("[GC-STW-RESTART] DONE thread %p deemed fully suspended\n", mono_thread_info_get_tid (info));
				continue;
			}

			suspend_count = mono_thread_info_suspend_count (info);
			if (!(suspend_count == 1))
				g_error ("[%p] suspend_count = %d, but should be 1", mono_thread_info_get_tid (info), suspend_count);

			info->skip = !mono_thread_info_begin_pulse_resume_and_request_suspension (info);
			if (!info->skip)
				restart_counter += 1;

			THREADS_STW_DEBUG ("[GC-STW-RESTART] RESTART thread %p skip %s\n", mono_thread_info_get_tid (info), info->skip ? "true" : "false");
		} FOREACH_THREAD_END

		mono_threads_wait_pending_operations ();

		if (restart_counter == 0)
			break;

		if (sleep_duration < 0) {
			mono_thread_info_yield ();
			sleep_duration = 0;
		} else {
			g_usleep (sleep_duration);
			sleep_duration += 10;
		}

		FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
			int reason = 0;
			if (info->suspend_done || !coreclr_is_thread_in_current_stw (info, &reason)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE SUSPEND thread %p not been processed done %d current %d reason %d\n", mono_thread_info_get_tid (info), info->suspend_done, !coreclr_is_thread_in_current_stw (info, NULL), reason);
				continue;
			}

			if (!mono_thread_info_is_running (info)) {
				THREADS_STW_DEBUG ("[GC-STW-RESTART] IGNORE SUSPEND thread %p not running\n", mono_thread_info_get_tid (info));
				continue;
			}

			switch (mono_thread_info_begin_suspend (info, MONO_THREAD_SUSPEND_PHASE_MOPUP)) {
			case MONO_THREAD_BEGIN_SUSPEND_SUSPENDED:
				info->skip = FALSE;
				break;
			case MONO_THREAD_BEGIN_SUSPEND_SKIP:
				info->skip = TRUE;
				break;
			case MONO_THREAD_BEGIN_SUSPEND_NEXT_PHASE:
				g_assert_not_reached ();
			default:
				g_assert_not_reached ();
			}

			THREADS_STW_DEBUG ("[GC-STW-RESTART] SUSPEND thread %p skip %s\n", mono_thread_info_get_tid (info), info->skip ? "true" : "false");
		} FOREACH_THREAD_END

		mono_threads_wait_pending_operations ();
	}

	FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
		gpointer stopped_ip;

		int reason = 0;
		if (!coreclr_is_thread_in_current_stw (info, &reason)) {
			g_assert (!info->suspend_done || info == mono_thread_info_current ());

			THREADS_STW_DEBUG ("[GC-STW-SUSPEND-END] thread %p is NOT suspended, reason %d\n", mono_thread_info_get_tid (info), reason);
			continue;
		}

		g_assert (info->suspend_done);

		info->ctx = mono_thread_info_get_suspend_state (info)->ctx;

		/* Once we remove the old suspend code, we should move sgen to directly access the state in MonoThread */
		info->stack_start = (gpointer) ((char*)MONO_CONTEXT_GET_SP (&info->ctx) - REDZONE_SIZE);

		if (info->stack_start < info->info.stack_start_limit
			 || info->stack_start >= info->info.stack_end) {
			/*
			 * Thread context is in unhandled state, most likely because it is
			 * dying. We don't scan it.
			 * FIXME We should probably rework and check the valid flag instead.
			 */
			info->stack_start = NULL;
		}

		stopped_ip = (gpointer) (MONO_CONTEXT_GET_IP (&info->ctx));

                #if 0
		sgen_binary_protocol_thread_suspend ((gpointer) mono_thread_info_get_tid (info), stopped_ip);
                #endif

		THREADS_STW_DEBUG ("[GC-STW-SUSPEND-END] thread %p is suspended, stopped_ip = %p, stack = %p -> %p\n",
			mono_thread_info_get_tid (info), stopped_ip, info->stack_start, info->stack_start ? info->info.stack_end : NULL);
	} FOREACH_THREAD_END
}

static void
coreclr_unified_suspend_restart_world (void)
{
	THREADS_STW_DEBUG ("[GC-STW-END] *** BEGIN RESUME ***\n");
	FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
		int reason = 0;
		if (coreclr_is_thread_in_current_stw (info, &reason)) {
			g_assert (mono_thread_info_begin_resume (info));
			THREADS_STW_DEBUG ("[GC-STW-RESUME-WORLD] RESUME thread %p\n", mono_thread_info_get_tid (info));

                        #if 0
			sgen_binary_protocol_thread_restart ((gpointer) mono_thread_info_get_tid (info));
                        #endif
		} else {
			THREADS_STW_DEBUG ("[GC-STW-RESUME-WORLD] IGNORE thread %p, reason %d\n", mono_thread_info_get_tid (info), reason);
		}
	} FOREACH_THREAD_END

	mono_threads_wait_pending_operations ();
	mono_threads_end_global_suspend ();
}

inline static void*
align_pointer (void *ptr)
{
	size_t p = (size_t)ptr;
	p += sizeof (gpointer) - 1;
	p &= ~ (sizeof (gpointer) - 1);
	return (void*)p;
}

static void
update_current_thread_stack (void)
{
	int stack_guard = 0;
	CoreGCThreadInfo *info = mono_thread_info_current ();

	info->stack_start = align_pointer (&stack_guard);
	g_assert (info->stack_start);
	g_assert (info->stack_start >= info->stack_start_limit && info->stack_start < info->stack_end);

#if !defined(MONO_CROSS_COMPILE) && MONO_ARCH_HAS_MONO_CONTEXT
	MONO_CONTEXT_GET_CURRENT (info->ctx);
#elif defined (HOST_WASM)
	//nothing
#else
	g_error ("Sgen STW requires a working mono-context");
#endif
}

void GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
	update_current_thread_stack ();
	pGCHeap->SetGCInProgress(true);
        coregc_unified_suspend_stop_world();
}

void GCToEEInterface::RestartEE(bool bFinishedGC)
{
        coreclr_unified_suspend_restart_world();
	pGCHeap->SetGCInProgress(false);
}

typedef struct {
	promote_func *fn;
	ScanContext* sc;
} coregc_root_scan_args;

void
coregc_scan_root (gpointer key, gpointer value, gpointer user_data)
{
	coregc_root_scan_args *args = (coregc_root_scan_args*) user_data;
	RootRecord *root = (RootRecord*) value;
	gpointer *start_root = (gpointer*)key;
	gpointer *end_root = (gpointer*)root->end_root;

	while (start_root < end_root) {
		// FIXME look at the root descriptor so we don't use conservative scan
		args->fn ((PTR_PTR_Object)start_root, args->sc, GC_CALL_PINNED | GC_CALL_INTERIOR);
		start_root++;
	}
}

void GCToEEInterface::GcScanRoots(promote_func* fn,  int condemned, int max_gen, ScanContext* sc)
{
	FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
		gpointer *start = (gpointer*)(size_t) ALIGN_TO ((size_t)info->stack_start, SIZEOF_VOID_P);
		gpointer *end = (gpointer*)info->stack_end;
		while (start < end) {
			fn ((PTR_PTR_Object)start, sc, GC_CALL_PINNED | GC_CALL_INTERIOR);
			start++;
		}
		start = (gpointer*)&info->ctx;
		end = (gpointer*)(&info->ctx + 1);
		while (start < end) {
			fn ((PTR_PTR_Object)start, sc, GC_CALL_PINNED | GC_CALL_INTERIOR);
			start++;
		}
	} FOREACH_THREAD_END

	coregc_root_scan_args args;
	args.fn = fn;
	args.sc = sc;

	for (int root_type = ROOT_TYPE_NORMAL; root_type <= ROOT_TYPE_PINNED; root_type++) {
		g_hash_table_foreach (coregc_roots_hash [root_type], coregc_scan_root, &args);
	}
}

void GCToEEInterface::GcStartWork(int condemned, int max_gen)
{
}

void GCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
}

void GCToEEInterface::GcBeforeBGCSweepWork()
{
}

void GCToEEInterface::GcDone(int condemned)
{
}

bool GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
	return false;
}

bool GCToEEInterface::IsPreemptiveGCDisabled()
{
	return true;
}

bool GCToEEInterface::EnablePreemptiveGC()
{
	return false;
}

void GCToEEInterface::DisablePreemptiveGC()
{
}

Thread* GCToEEInterface::GetThread()
{
	return NULL;
}

gc_alloc_context * GCToEEInterface::GetAllocContext()
{
	return NULL;
}

void GCToEEInterface::GcEnumAllocContexts (enum_alloc_context_func* fn, void* param)
{
	FOREACH_THREAD_ALL (info) {
		fn (&info->alloc_context, param);
	} FOREACH_THREAD_END
}

uint8_t* GCToEEInterface::GetLoaderAllocatorObjectForGC(Object* pObject)
{
	return NULL;
}

void GCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC /*scanProc*/, uintptr_t /*lp1*/, uintptr_t /*lp2*/)
{
}

void GCToEEInterface::SyncBlockCacheDemote(int /*max_gen*/)
{
}

void GCToEEInterface::SyncBlockCachePromotionsGranted(int /*max_gen*/)
{
}

void GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
}

void GCToEEInterface::DiagUpdateGenerationBounds()
{
}

void GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
}

void GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
}

void GCToEEInterface::DiagWalkSurvivors(void* gcContext, bool fCompacting)
{
}

void GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
}

void GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
}

void GCToEEInterface::EnableFinalization(bool foundFinalizers)
{
    // Signal to finalizer thread that there are objects to finalize
    // TODO: Implement for finalization
}

void GCToEEInterface::HandleFatalError(unsigned int exitCode)
{
	abort();
}

bool GCToEEInterface::EagerFinalized(Object* obj)
{
	return false;
}

void GCToEEInterface::FreeStringConfigValue(const char *value)
{

}

bool GCToEEInterface::IsGCThread()
{
	return false;
}

bool GCToEEInterface::WasCurrentThreadCreatedByGC()
{
	return false;
}

/* Vtable of the objects used to fill out nursery fragments before a collection */
static MonoVTable *array_fill_vtable;

MethodTable* GCToEEInterface::GetFreeObjectMethodTable()
{
	if (!array_fill_vtable) {
		static char _vtable[sizeof(MonoVTable)+8];
		MonoVTable* vtable = (MonoVTable*) ALIGN_TO((size_t)_vtable, 8);
		gsize bmap;
		GPtrArray *gc_descr_full = NULL;

		vtable->klass = mono_class_create_array_fill_type ();

		bmap = 0;
		mono_gc_descr_union desc;
		desc.ptr_gc_descr = mono_gc_make_descr_for_array (TRUE, &bmap, 0, 1, &gc_descr_full);
		vtable->gc_descr = desc.ptr_gc_descr;
		vtable->rank = 1;

		array_fill_vtable = vtable;
	}
	return (MethodTable*)array_fill_vtable;
}

bool GCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
	return false;
}

void GCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
{
}

void GCToEEInterface::WalkAsyncPinned(Object* object, void* context, void (*callback)(Object*, Object*, void*))
{
}

uint32_t GCToEEInterface::GetTotalNumSizedRefHandles()
{
	return -1;
}

void GCToEEInterface::UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLevel, int privateKeywords)
{
}

inline bool GCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
	return false;
}

void DiagWalkUOHSurvivors(void* gcContext, int gen)
{
}

bool GCToEEInterface::GetIntConfigValue(char const*, char const*, long long*)
{
	return false;
}

void GCToEEInterface::DiagWalkUOHSurvivors(void*, int) 
{
}

bool GCToEEInterface::GetStringConfigValue(char const*, char const*, char const**)
{
	return false;
}

bool GCToEEInterface::GetBooleanConfigValue(char const*, char const*, bool*)
{
	return false;
}

size_t GCToEEInterface::GetObjectSize(Object* obj) 
{
	printf("coregc-mono.cpp: GetObjectsize: obj: %p\n", obj);

	MethodTable* mT = obj->GetMethodTable();
	MonoVTable* mono_vtable = (MonoVTable*)mT;

    size_t obj_size = (mT->GetBaseSize() +
                    (mT->HasComponentSize() ?
                    ((size_t)((ArrayBase*)obj)->GetNumComponents() * mT->RawGetComponentSize()) : 0));


	size_t bounds_size = 0; 

	/* array_fill objects are arrays, but do not have bounds data. 
	   Is there a better way to check for them? */

	char* debug_class_name = "array_fill";
	if (mono_vtable != array_fill_vtable)
	{
		debug_class_name = mono_type_get_full_name (mono_vtable->klass);
		bounds_size = m_class_get_rank(mono_vtable->klass) * sizeof (MonoArrayBounds);

		// Why do I need this? where are these extra bytes coming from?
		if (m_class_get_rank(mono_vtable->klass) > 1 )
		{
			bounds_size += sizeof (mono_array_size_t);
		}
	}

	size_t total_size = obj_size + bounds_size;

	if (total_size > MIN_OBJECT_SIZE)
	{
		printf("coregc-mono.cpp: GetObjectSize: obj: %p::%s obj_size: %d bounds_size %d returning %d\n", obj, debug_class_name, obj_size, bounds_size, total_size);
		return total_size;
	}
	else
	{
		printf("gc.cpp: my_get_size: returning %d\n", obj, MIN_OBJECT_SIZE);
		return MIN_OBJECT_SIZE;
	}
}

Volatile<GCEventLevel> GCEventStatus::enabledLevels[2] = {GCEventLevel_None, GCEventLevel_None};
Volatile<GCEventKeyword> GCEventStatus::enabledKeywords[2] = {GCEventKeyword_None, GCEventKeyword_None};