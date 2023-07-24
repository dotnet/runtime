/**
 * \file
 * SGen features specific to Mono.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"
#include "sgen/sgen-protocol.h"
#include "metadata/monitor.h"
#include "sgen/sgen-layout-stats.h"
#include "sgen/sgen-client.h"
#include "sgen/sgen-cardtable.h"
#include "sgen/sgen-pinning.h"
#include "sgen/sgen-workers.h"
#include "metadata/class-init.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/abi-details.h"
#include "metadata/class-abi-details.h"
#include <mono/metadata/mono-gc.h>
#include "metadata/runtime.h"
#include "metadata/sgen-bridge-internals.h"
#include "metadata/sgen-mono.h"
#include "metadata/sgen-mono-ilgen.h"
#include "metadata/gc-internals.h"
#include "metadata/handle.h"
#include "metadata/abi-details.h"
#include "utils/mono-memory-model.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-threads-coop.h"
#include "utils/mono-threads.h"
#include "metadata/w32handle.h"
#include "icall-signatures.h"
#include "mono/utils/mono-tls-inline.h"

#if _MSC_VER
#pragma warning(disable:4312) // FIXME pointer cast to different size
#endif

#ifdef HEAVY_STATISTICS
static guint64 stat_wbarrier_set_arrayref = 0;
static guint64 stat_wbarrier_value_copy = 0;
static guint64 stat_wbarrier_object_copy = 0;

static guint64 los_marked_cards;
static guint64 los_array_cards;
static guint64 los_array_remsets;
#endif

/* If set, mark stacks conservatively, even if precise marking is possible */
static gboolean conservative_stack_mark = FALSE;

/* Functions supplied by the runtime to be called by the GC */
static MonoGCCallbacks gc_callbacks;

/* Used for GetGCMemoryInfo */
SgenGCInfo sgen_gc_info;

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LAST
};

#undef OPDEF

/*
 * Write barriers
 */

static gboolean
ptr_on_stack (void *ptr)
{
	gpointer stack_start = &stack_start;
	SgenThreadInfo *info = mono_thread_info_current ();

	if (ptr >= stack_start && ptr < (gpointer)info->client_info.info.stack_end)
		return TRUE;
	return FALSE;
}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj) do {					\
		gpointer o = *(gpointer*)(ptr);				\
		if ((o)) {						\
			gpointer d = ((char*)dest) + ((char*)(ptr) - (char*)(obj)); \
			sgen_binary_protocol_wbarrier (d, o, (gpointer) SGEN_LOAD_VTABLE (o)); \
		}							\
	} while (0)

static void
scan_object_for_binary_protocol_copy_wbarrier (gpointer dest, char *start, mword desc)
{
#define SCAN_OBJECT_NOVTABLE
#include "sgen/sgen-scan-object.h"
}
#endif

void
mono_gc_wbarrier_value_copy_internal (gpointer dest, gconstpointer src, int count, MonoClass *klass)
{
	HEAVY_STAT (++stat_wbarrier_value_copy);
	g_assert (m_class_is_valuetype (klass));

	SGEN_LOG (8, "Adding value remset at %p, count %d, descr %p for class %s (%p)", dest, count, (gpointer)(uintptr_t)m_class_get_gc_descr (klass), m_class_get_name (klass), klass);

	if (sgen_ptr_in_nursery (dest) || ptr_on_stack (dest) || !sgen_gc_descr_has_references ((mword)m_class_get_gc_descr (klass))) {
		size_t element_size = mono_class_value_size (klass, NULL);
		size_t size = count * element_size;
		mono_gc_memmove_atomic (dest, src, size);
		return;
	}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	if (sgen_binary_protocol_is_heavy_enabled ()) {
		size_t element_size = mono_class_value_size (klass, NULL);
		int i;
		for (i = 0; i < count; ++i) {
			scan_object_for_binary_protocol_copy_wbarrier ((char*)dest + i * element_size,
					(char*)src + i * element_size - MONO_ABI_SIZEOF (MonoObject),
					(mword) m_class_get_gc_descr (klass));
		}
	}
#endif

	sgen_get_remset ()->wbarrier_value_copy (dest, src, count, mono_class_value_size (klass, NULL));
}

/**
 * mono_gc_wbarrier_object_copy_internal:
 *
 * Write barrier to call when \p obj is the result of a clone or copy of an object.
 */
void
mono_gc_wbarrier_object_copy_internal (MonoObject* obj, MonoObject *src)
{
	int size;

	HEAVY_STAT (++stat_wbarrier_object_copy);

	SGEN_ASSERT (6, !ptr_on_stack (obj), "Why is this called for a non-reference type?");
	if (sgen_ptr_in_nursery (obj) || !SGEN_OBJECT_HAS_REFERENCES (src)) {
		size = m_class_get_instance_size (mono_object_class (obj));
		mono_gc_memmove_aligned ((char*)obj + MONO_ABI_SIZEOF (MonoObject), (char*)src + MONO_ABI_SIZEOF (MonoObject),
				size - MONO_ABI_SIZEOF (MonoObject));
		return;
	}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	if (sgen_binary_protocol_is_heavy_enabled ())
		scan_object_for_binary_protocol_copy_wbarrier (obj, (char*)src, (mword) src->vtable->gc_descr);
#endif

	sgen_get_remset ()->wbarrier_object_copy (obj, src);
}

/**
 * mono_gc_wbarrier_set_arrayref_internal:
 */
void
mono_gc_wbarrier_set_arrayref_internal (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	HEAVY_STAT (++stat_wbarrier_set_arrayref);
	if (sgen_ptr_in_nursery (slot_ptr)) {
		*(void**)slot_ptr = value;
		return;
	}
	SGEN_LOG (8, "Adding remset at %p", slot_ptr);
	if (value)
		sgen_binary_protocol_wbarrier (slot_ptr, value, value->vtable);

	sgen_get_remset ()->wbarrier_set_field ((GCObject*)arr, slot_ptr, value);
}

/**
 * mono_gc_wbarrier_set_field_internal:
 */
void
mono_gc_wbarrier_set_field_internal (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	mono_gc_wbarrier_set_arrayref_internal ((MonoArray*)obj, field_ptr, value);
}

void
mono_gc_wbarrier_range_copy (gpointer _dest, gconstpointer _src, int size)
{
	sgen_wbarrier_range_copy (_dest, _src, size);
}

MonoRangeCopyFunction
mono_gc_get_range_copy_func (void)
{
	return sgen_get_remset ()->wbarrier_range_copy;
}

int
mono_gc_get_suspend_signal (void)
{
	return mono_threads_suspend_get_suspend_signal ();
}

int
mono_gc_get_restart_signal (void)
{
	return mono_threads_suspend_get_restart_signal ();
}

static MonoMethod *write_barrier_conc_method;
static MonoMethod *write_barrier_noconc_method;

gboolean
sgen_is_critical_method (MonoMethod *method)
{
	return sgen_is_managed_allocator (method);
}

gboolean
sgen_has_critical_method (void)
{
	return sgen_has_managed_allocator ();
}

gboolean
mono_gc_is_critical_method (MonoMethod *method)
{
#if defined(HOST_WASM) || defined(HOST_WASI)
	//methods can't be critical under wasm due to the single thread'ness of it
	return FALSE;
#else
	return sgen_is_critical_method (method);
#endif
}

static MonoSgenMonoCallbacks sgenmono_cb;
static gboolean cb_inited = FALSE;

void
mono_install_sgen_mono_callbacks (MonoSgenMonoCallbacks *cb)
{
	g_assert (!cb_inited);
	g_assert (cb->version == MONO_SGEN_MONO_CALLBACKS_VERSION);
	memcpy (&sgenmono_cb, cb, sizeof (MonoSgenMonoCallbacks));
	cb_inited = TRUE;
}

static MonoSgenMonoCallbacks *
get_sgen_mono_cb (void)
{
	if (G_UNLIKELY (!cb_inited)) {
		mono_sgen_mono_ilgen_init ();
	}
	return &sgenmono_cb;
}

MonoMethod*
mono_gc_get_specific_write_barrier (gboolean is_concurrent)
{
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	MonoMethod **write_barrier_method_addr;
	WrapperInfo *info;
	// FIXME: Maybe create a separate version for ctors (the branch would be
	// correctly predicted more times)
	if (is_concurrent)
		write_barrier_method_addr = &write_barrier_conc_method;
	else
		write_barrier_method_addr = &write_barrier_noconc_method;

	if (*write_barrier_method_addr)
		return *write_barrier_method_addr;

	/* Create the IL version of mono_gc_barrier_generic_store () */
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	sig->ret = mono_get_void_type ();
	sig->params [0] = mono_get_int_type ();

	if (is_concurrent)
		mb = mono_mb_new (mono_defaults.object_class, "wbarrier_conc", MONO_WRAPPER_WRITE_BARRIER);
	else
		mb = mono_mb_new (mono_defaults.object_class, "wbarrier_noconc", MONO_WRAPPER_WRITE_BARRIER);

	get_sgen_mono_cb ()->emit_nursery_check (mb, is_concurrent);

	res = mono_mb_create_method (mb, sig, 16);
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	mono_marshal_set_wrapper_info (res, info);
	mono_mb_free (mb);

	LOCK_GC;
	if (*write_barrier_method_addr) {
		/* Already created */
		mono_free_method (res);
	} else {
		/* double-checked locking */
		mono_memory_barrier ();
		*write_barrier_method_addr = res;
	}
	UNLOCK_GC;

	return *write_barrier_method_addr;
}

MonoMethod*
mono_gc_get_write_barrier (void)
{
	return mono_gc_get_specific_write_barrier (sgen_major_collector.is_concurrent);
}

/*
 * Dummy filler objects
 */

/* Vtable of the objects used to fill out nursery fragments before a collection */
static GCVTable array_fill_vtable;

static GCVTable
get_array_fill_vtable (void)
{
	if (!array_fill_vtable) {
		static char _vtable[sizeof(MonoVTable)+8];
		MonoVTable* vtable = (MonoVTable*) ALIGN_TO((mword)_vtable, 8);
		gsize bmap;

		MonoClass *klass = mono_class_create_array_fill_type ();
		MonoDomain *domain = mono_get_root_domain ();
		g_assert (domain);

		vtable->klass = klass;
		bmap = 0;
		vtable->gc_descr = mono_gc_make_descr_for_array (TRUE, &bmap, 0, 8);
		vtable->rank = 1;

		array_fill_vtable = vtable;
	}
	return array_fill_vtable;
}

gboolean
sgen_client_array_fill_range (char *start, size_t size)
{
	MonoArray *o;

	if (size < MONO_SIZEOF_MONO_ARRAY) {
		memset (start, 0, size);
		return FALSE;
	}

	o = (MonoArray*)start;
	o->obj.vtable = (MonoVTable*)get_array_fill_vtable ();
	/* Mark this as not a real object */
	o->obj.synchronisation = (MonoThreadsSync *)GINT_TO_POINTER (-1);
	o->bounds = NULL;
	/* We use array of int64 */
	g_assert ((size - MONO_SIZEOF_MONO_ARRAY) % 8 == 0);
	o->max_length = (mono_array_size_t)((size - MONO_SIZEOF_MONO_ARRAY) / 8);

	return TRUE;
}

void
sgen_client_zero_array_fill_header (void *p, size_t size)
{
	if (size >= MONO_SIZEOF_MONO_ARRAY) {
		memset (p, 0, MONO_SIZEOF_MONO_ARRAY);
	} else {
		static guint8 zeros [MONO_SIZEOF_MONO_ARRAY];

		SGEN_ASSERT (0, !memcmp (p, zeros, size), "TLAB segment must be zeroed out.");
	}
}

MonoVTable *
mono_gc_get_vtable (MonoObject *obj)
{
	// See sgen/sgen-tagged-pointer.h.
	return SGEN_LOAD_VTABLE (obj);
}

/*
 * Finalization
 */

static MonoGCFinalizerCallbacks fin_callbacks;

guint
mono_gc_get_vtable_bits (MonoClass *klass)
{
	guint res = 0;
	/* FIXME move this to the bridge code */
	if (sgen_need_bridge_processing ()) {
		switch (sgen_bridge_class_kind (klass)) {
		case GC_BRIDGE_TRANSPARENT_BRIDGE_CLASS:
		case GC_BRIDGE_OPAQUE_BRIDGE_CLASS:
			res = SGEN_GC_BIT_BRIDGE_OBJECT;
			break;
		case GC_BRIDGE_OPAQUE_CLASS:
			res = SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT;
			break;
		case GC_BRIDGE_TRANSPARENT_CLASS:
			break;
		}
	}
	if (fin_callbacks.is_class_finalization_aware) {
		if (fin_callbacks.is_class_finalization_aware (klass))
			res |= SGEN_GC_BIT_FINALIZER_AWARE;
	}

	if (m_class_get_image (klass) == mono_defaults.corlib &&
			strcmp (m_class_get_name_space (klass), "System") == 0 &&
			strncmp (m_class_get_name (klass), "WeakReference", 13) == 0)
		res |= SGEN_GC_BIT_WEAKREF;

	return res;
}

static gboolean
is_finalization_aware (MonoObject *obj)
{
	MonoVTable *vt = SGEN_LOAD_VTABLE (obj);
	return (vt->gc_bits & SGEN_GC_BIT_FINALIZER_AWARE) == SGEN_GC_BIT_FINALIZER_AWARE;
}

gboolean
sgen_client_object_finalize_eagerly (GCObject *obj)
{
	if (obj->vtable->gc_bits & SGEN_GC_BIT_WEAKREF) {
		MonoWeakReference *wr = (MonoWeakReference*)obj;
		MonoGCHandle gc_handle = (MonoGCHandle)(wr->taggedHandle & ~(gsize)1);
		mono_gchandle_free_internal (gc_handle);
		// keep the bit that indicates whether this reference was tracking resurrection, clear the rest.
		wr->taggedHandle &= (gsize)1;
		return TRUE;
	}

	return FALSE;
}

void
sgen_client_object_queued_for_finalization (GCObject *obj)
{
	if (fin_callbacks.object_queued_for_finalization && is_finalization_aware (obj))
		fin_callbacks.object_queued_for_finalization (obj);

#ifdef ENABLE_DTRACE
	if (G_UNLIKELY (MONO_GC_FINALIZE_ENQUEUE_ENABLED ())) {
		int gen = sgen_ptr_in_nursery (obj) ? GENERATION_NURSERY : GENERATION_OLD;
		GCVTable vt = SGEN_LOAD_VTABLE (obj);
		MONO_GC_FINALIZE_ENQUEUE ((mword)obj, sgen_safe_object_get_size (obj),
				sgen_client_vtable_get_namespace (vt), sgen_client_vtable_get_name (vt), gen,
				sgen_client_object_has_critical_finalizer (obj));
	}
#endif
}

void
mono_gc_register_finalizer_callbacks (MonoGCFinalizerCallbacks *callbacks)
{
	if (callbacks->version != MONO_GC_FINALIZER_EXTENSION_VERSION)
		g_error ("Invalid finalizer callback version. Expected %d but got %d\n", MONO_GC_FINALIZER_EXTENSION_VERSION, callbacks->version);

	fin_callbacks = *callbacks;
}

void
sgen_client_run_finalize (MonoObject *obj)
{
	mono_gc_run_finalize (obj, NULL);
}

/**
 * mono_gc_invoke_finalizers:
 */
int
mono_gc_invoke_finalizers (void)
{
	return sgen_gc_invoke_finalizers ();
}

/**
 * mono_gc_pending_finalizers:
 */
MonoBoolean
mono_gc_pending_finalizers (void)
{
	return !!sgen_have_pending_finalizers ();
}

void
sgen_client_finalize_notify (void)
{
	mono_gc_finalize_notify ();
}

void
mono_gc_register_for_finalization (MonoObject *obj, MonoFinalizationProc user_data)
{
	sgen_object_register_for_finalization (obj, user_data);
}

/**
 * mono_gc_finalizers_for_domain:
 * \param domain the unloading appdomain
 * \param out_array output array
 * \param out_size size of output array
 * Enqueue for finalization all objects that belong to the unloading appdomain \p domain.
 * \p suspend is used for early termination of the enqueuing process.
 */
void
mono_gc_finalize_domain (MonoDomain *domain)
{
	sgen_finalize_all ();
}

/*
 * Ephemerons
 */

typedef struct _EphemeronLinkNode EphemeronLinkNode;

struct _EphemeronLinkNode {
	EphemeronLinkNode *next;
	MonoArray *array;
};

typedef struct {
       GCObject *key;
       GCObject *value;
} Ephemeron;

static EphemeronLinkNode *ephemeron_list;

/* LOCKING: requires that the GC lock is held */
void
sgen_client_clear_unreachable_ephemerons (ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	SgenGrayQueue *queue = ctx.queue;
	EphemeronLinkNode *current = ephemeron_list, *prev = NULL;
	Ephemeron *cur, *array_end;
	GCObject *tombstone;

	while (current) {
		MonoArray *array = current->array;

		if (!sgen_is_object_alive_for_current_gen ((GCObject*)array)) {
			EphemeronLinkNode *tmp = current;

			SGEN_LOG (5, "Dead Ephemeron array at %p", array);

			if (prev)
				prev->next = current->next;
			else
				ephemeron_list = current->next;

			current = current->next;
			sgen_free_internal (tmp, INTERNAL_MEM_EPHEMERON_LINK);

			continue;
		}

		copy_func ((GCObject**)&array, queue);
		current->array = array;

		SGEN_LOG (5, "Clearing unreachable entries for ephemeron array at %p", array);

		cur = mono_array_addr_internal (array, Ephemeron, 0);
		array_end = cur + mono_array_length_internal (array);
		tombstone = SGEN_LOAD_VTABLE ((GCObject*)array)->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			GCObject *key = cur->key;

			if (!key || key == tombstone)
				continue;

			SGEN_LOG (5, "[%" G_GSIZE_FORMAT "d] key %p (%s) value %p (%s)", cur - mono_array_addr_internal (array, Ephemeron, 0),
				key, sgen_is_object_alive_for_current_gen (key) ? "reachable" : "unreachable",
				cur->value, cur->value && sgen_is_object_alive_for_current_gen (cur->value) ? "reachable" : "unreachable");

			if (!sgen_is_object_alive_for_current_gen (key)) {
				cur->key = tombstone;
				cur->value = NULL;
				continue;
			}
		}
		prev = current;
		current = current->next;
	}
}

/*
LOCKING: requires that the GC lock is held

Limitations: We scan all ephemerons on every collection since the current design doesn't allow for a simple nursery/mature split.
*/
gboolean
sgen_client_mark_ephemerons (ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	SgenGrayQueue *queue = ctx.queue;
	gboolean nothing_marked = TRUE;
	EphemeronLinkNode *current = ephemeron_list;
	Ephemeron *cur, *array_end;
	GCObject *tombstone;

	for (current = ephemeron_list; current; current = current->next) {
		MonoArray *array = current->array;
		SGEN_LOG (5, "Ephemeron array at %p", array);

		/*It has to be alive*/
		if (!sgen_is_object_alive_for_current_gen ((GCObject*)array)) {
			SGEN_LOG (5, "\tnot reachable");
			continue;
		}

		copy_func ((GCObject**)&array, queue);

		cur = mono_array_addr_internal (array, Ephemeron, 0);
		array_end = cur + mono_array_length_internal (array);
		tombstone = SGEN_LOAD_VTABLE ((GCObject*)array)->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			GCObject *key = cur->key;

			if (!key || key == tombstone)
				continue;

			SGEN_LOG (5, "[%" G_GSIZE_FORMAT "d] key %p (%s) value %p (%s)", cur - mono_array_addr_internal (array, Ephemeron, 0),
				key, sgen_is_object_alive_for_current_gen (key) ? "reachable" : "unreachable",
				cur->value, cur->value && sgen_is_object_alive_for_current_gen (cur->value) ? "reachable" : "unreachable");

			if (sgen_is_object_alive_for_current_gen (key)) {
				GCObject *value = cur->value;

				copy_func (&cur->key, queue);
				if (value) {
					if (!sgen_is_object_alive_for_current_gen (value)) {
						nothing_marked = FALSE;
						sgen_binary_protocol_ephemeron_ref (current, key, value);
					}
					copy_func (&cur->value, queue);
				}
			}
		}
	}

	SGEN_LOG (5, "Ephemeron run finished. Is it done %d", nothing_marked);
	return nothing_marked;
}

gboolean
mono_gc_ephemeron_array_add (MonoObject *obj)
{
	EphemeronLinkNode *node;

	LOCK_GC;

	node = (EphemeronLinkNode *)sgen_alloc_internal (INTERNAL_MEM_EPHEMERON_LINK);
	if (!node) {
		UNLOCK_GC;
		return FALSE;
	}
	node->array = (MonoArray*)obj;
	node->next = ephemeron_list;
	ephemeron_list = node;

	SGEN_LOG (5, "Registered ephemeron array %p", obj);

	UNLOCK_GC;
	return TRUE;
}

/*
 * Allocation
 */

MonoObject*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = sgen_alloc_obj (vtable, size);

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()) && obj)
		MONO_PROFILER_RAISE (gc_allocation, (obj));

	return obj;
}

MonoObject*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = sgen_alloc_obj_pinned (vtable, size);

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()) && obj)
		MONO_PROFILER_RAISE (gc_allocation, (obj));

	return obj;
}

MonoObject*
mono_gc_alloc_mature (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = sgen_alloc_obj_mature (vtable, size);

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()) && obj)
		MONO_PROFILER_RAISE (gc_allocation, (obj));

	return obj;
}

/**
 * mono_gc_alloc_fixed:
 */
MonoObject*
mono_gc_alloc_fixed (size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	/* FIXME: do a single allocation */
	void *res = g_calloc (1, size);
	if (!res)
		return NULL;
	if (!mono_gc_register_root ((char *)res, size, descr, source, key, msg)) {
		g_free (res);
		res = NULL;
	}
	return (MonoObject*)res;
}

MonoObject*
mono_gc_alloc_fixed_no_descriptor (size_t size, MonoGCRootSource source, void *key, const char *msg)
{
	return mono_gc_alloc_fixed (size, 0, source, key, msg);
}

/**
 * mono_gc_free_fixed:
 */
void
mono_gc_free_fixed (void* addr)
{
	mono_gc_deregister_root ((char *)addr);
	g_free (addr);
}

/*
 * Managed allocator
 */

static MonoMethod* alloc_method_cache [ATYPE_NUM];
static MonoMethod* slowpath_alloc_method_cache [ATYPE_NUM];
static MonoMethod* profiler_alloc_method_cache [ATYPE_NUM];
static gboolean use_managed_allocator = TRUE;
static gboolean debug_coop_no_stack_scan;

#ifdef MANAGED_ALLOCATION
/* FIXME: Do this in the JIT, where specialized allocation sequences can be created
 * for each class. This is currently not easy to do, as it is hard to generate basic
 * blocks + branches, but it is easy with the linear IL codebase.
 *
 * For this to work we'd need to solve the TLAB race, first.  Now we
 * require the allocator to be in a few known methods to make sure
 * that they are executed atomically via the restart mechanism.
 */
static MonoMethod*
create_allocator (int atype, ManagedAllocatorVariant variant)
{
	gboolean slowpath = variant == MANAGED_ALLOCATOR_SLOW_PATH;
	gboolean profiler = variant == MANAGED_ALLOCATOR_PROFILER;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	const char *name = NULL;
	WrapperInfo *info;
	int num_params, i;

	if (atype == ATYPE_SMALL) {
		name = slowpath ? "SlowAllocSmall" : (profiler ? "ProfilerAllocSmall" : "AllocSmall");
	} else if (atype == ATYPE_NORMAL) {
		name = slowpath ? "SlowAlloc" : (profiler ? "ProfilerAlloc" : "Alloc");
	} else if (atype == ATYPE_VECTOR) {
		name = slowpath ? "SlowAllocVector" : (profiler ? "ProfilerAllocVector" : "AllocVector");
	} else if (atype == ATYPE_STRING) {
		name = slowpath ? "SlowAllocString" : (profiler ? "ProfilerAllocString" : "AllocString");
	} else {
		g_assert_not_reached ();
	}

	if (atype == ATYPE_NORMAL)
		num_params = 1;
	else
		num_params = 2;

	MonoType *int_type = mono_get_int_type ();
	csig = mono_metadata_signature_alloc (mono_defaults.corlib, num_params);
	if (atype == ATYPE_STRING) {
		csig->ret = m_class_get_byval_arg (mono_defaults.string_class);
		csig->params [0] = int_type;
		csig->params [1] = mono_get_int32_type ();
	} else {
		csig->ret = mono_get_object_type ();
		for (i = 0; i < num_params; i++)
			csig->params [i] = int_type;
	}

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ALLOC);

	get_sgen_mono_cb ()->emit_managed_allocator (mb, slowpath, profiler, atype);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.alloc.gc_name = "sgen";
	info->d.alloc.alloc_type = atype;

	res = mono_mb_create (mb, csig, 8, info);
	mono_mb_free (mb);

	return res;
}
#endif

int
mono_gc_get_aligned_size_for_allocator (int size)
{
	return SGEN_ALIGN_UP (size);
}

/*
 * Generate an allocator method implementing the fast path of mono_gc_alloc_obj ().
 * The signature of the called method is:
 * 	object allocate (MonoVTable *vtable)
 */
MonoMethod*
mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box, gboolean known_instance_size)
{
#ifdef MANAGED_ALLOCATION
	ManagedAllocatorVariant variant = mono_profiler_allocations_enabled () ?
		MANAGED_ALLOCATOR_PROFILER : MANAGED_ALLOCATOR_REGULAR;

	if (sgen_collect_before_allocs)
		return NULL;
	if (GINT_TO_UINT32(m_class_get_instance_size (klass)) > sgen_tlab_size)
		return NULL;
	if (known_instance_size && ALIGN_TO (m_class_get_instance_size (klass), SGEN_ALLOC_ALIGN) >= SGEN_MAX_SMALL_OBJ_SIZE)
		return NULL;
	if (mono_class_has_finalizer (klass) || m_class_has_weak_fields (klass))
		return NULL;
	if (m_class_get_rank (klass))
		return NULL;
	if (m_class_get_byval_arg (klass)->type == MONO_TYPE_STRING)
		return mono_gc_get_managed_allocator_by_type (ATYPE_STRING, variant);
	/* Generic classes have dynamic field and can go above MAX_SMALL_OBJ_SIZE. */
	if (known_instance_size)
		return mono_gc_get_managed_allocator_by_type (ATYPE_SMALL, variant);
	else
		return mono_gc_get_managed_allocator_by_type (ATYPE_NORMAL, variant);
#else
	return NULL;
#endif
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoClass *klass)
{
#ifdef MANAGED_ALLOCATION
	if (m_class_get_rank (klass) != 1)
		return NULL;
	if (sgen_has_per_allocation_action)
		return NULL;
	g_assert (!mono_class_has_finalizer (klass));

	return mono_gc_get_managed_allocator_by_type (ATYPE_VECTOR, mono_profiler_allocations_enabled () ?
		MANAGED_ALLOCATOR_PROFILER : MANAGED_ALLOCATOR_REGULAR);
#else
	return NULL;
#endif
}

void
sgen_set_use_managed_allocator (gboolean flag)
{
	use_managed_allocator = flag;
}

void
sgen_disable_native_stack_scan (void)
{
	debug_coop_no_stack_scan = TRUE;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype, ManagedAllocatorVariant variant)
{
#ifdef MANAGED_ALLOCATION
	MonoMethod *res;
	MonoMethod **cache;

	if (variant != MANAGED_ALLOCATOR_SLOW_PATH && !use_managed_allocator)
		return NULL;

	switch (variant) {
	case MANAGED_ALLOCATOR_REGULAR: cache = alloc_method_cache; break;
	case MANAGED_ALLOCATOR_SLOW_PATH: cache = slowpath_alloc_method_cache; break;
	case MANAGED_ALLOCATOR_PROFILER: cache = profiler_alloc_method_cache; break;
	default: g_assert_not_reached (); break;
	}

	res = cache [atype];
	if (res)
		return res;

	res = create_allocator (atype, variant);
	LOCK_GC;
	if (cache [atype]) {
		mono_free_method (res);
		res = cache [atype];
	} else {
		mono_memory_barrier ();
		cache [atype] = res;
	}
	UNLOCK_GC;

	return res;
#else
	return NULL;
#endif
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return ATYPE_NUM;
}

gboolean
sgen_is_managed_allocator (MonoMethod *method)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (method == alloc_method_cache [i] || method == slowpath_alloc_method_cache [i] || method == profiler_alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

gboolean
sgen_has_managed_allocator (void)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (alloc_method_cache [i] || slowpath_alloc_method_cache [i] || profiler_alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

#define ARRAY_OBJ_INDEX(ptr,array,elem_size) (((char*)(ptr) - ((char*)(array) + G_STRUCT_OFFSET (MonoArray, vector))) / (elem_size))

gboolean
sgen_client_cardtable_scan_object (GCObject *obj, guint8 *cards, ScanCopyContext ctx)
{
	MonoVTable *vt = SGEN_LOAD_VTABLE (obj);
	MonoClass *klass = vt->klass;

	SGEN_ASSERT (0, SGEN_VTABLE_HAS_REFERENCES (vt), "Why would we ever call this on reference-free objects?");

	if (vt->rank) {
		MonoArray *arr = (MonoArray*)obj;
		guint8 *card_data, *card_base;
		guint8 *card_data_end;
		char *obj_start = (char *)sgen_card_table_align_pointer (obj);
		mword bounds_size;
		mword obj_size = sgen_mono_array_size (vt, arr, &bounds_size, sgen_vtable_get_descriptor (vt));
		/* We don't want to scan the bounds entries at the end of multidimensional arrays */
		char *obj_end = (char*)obj + obj_size - bounds_size;
		size_t card_count;
		size_t extra_idx = 0;

		mword desc = (mword)m_class_get_gc_descr (m_class_get_element_class (klass));
		int elem_size = mono_array_element_size (klass);

#ifdef SGEN_OBJECT_LAYOUT_STATISTICS
		if (m_class_is_valuetype (m_class_get_element_class (klass)))
			sgen_object_layout_scanned_vtype_array ();
		else
			sgen_object_layout_scanned_ref_array ();
#endif

		if (cards)
			card_data = cards;
		else
			card_data = sgen_card_table_get_card_scan_address ((mword)obj);

		card_base = card_data;
		card_count = sgen_card_table_number_of_cards_in_range ((mword)obj, obj_size);

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
LOOP_HEAD:
		card_data_end = card_base + card_count;

		/*
		 * Check for overflow and if so, scan only until the end of the shadow
		 * card table, leaving the rest for next iterations.
		 */
		if (!cards && card_data_end >= SGEN_SHADOW_CARDTABLE_END) {
			card_data_end = SGEN_SHADOW_CARDTABLE_END;
		}
		card_count -= (card_data_end - card_base);

#else
		card_data_end = card_data + card_count;
#endif

		card_data = sgen_find_next_card (card_data, card_data_end);
		for (; card_data < card_data_end; card_data = sgen_find_next_card (card_data + 1, card_data_end)) {
			size_t index;
			size_t idx = (card_data - card_base) + extra_idx;
			char *start = (char*)(obj_start + idx * CARD_SIZE_IN_BYTES);
			char *card_end = start + CARD_SIZE_IN_BYTES;
			char *first_elem, *elem;

			HEAVY_STAT (++los_marked_cards);

			if (!cards)
				sgen_card_table_prepare_card_for_scanning (card_data);

			card_end = MIN (card_end, obj_end);

			if (start <= (char*)arr->vector)
				index = 0;
			else
				index = ARRAY_OBJ_INDEX (start, obj, elem_size);

			elem = first_elem = (char*)mono_array_addr_with_size_fast ((MonoArray*)obj, elem_size, index);
			if (m_class_is_valuetype (m_class_get_element_class (klass))) {
				ScanVTypeFunc scan_vtype_func = ctx.ops->scan_vtype;

				for (; elem < card_end; elem += elem_size)
					scan_vtype_func (obj, elem, desc, ctx.queue BINARY_PROTOCOL_ARG (elem_size));
			} else {
				ScanPtrFieldFunc scan_ptr_field_func = ctx.ops->scan_ptr_field;

				HEAVY_STAT (++los_array_cards);
				for (; elem < card_end; elem += SIZEOF_VOID_P)
					scan_ptr_field_func (obj, (GCObject**)elem, ctx.queue);
			}

			sgen_binary_protocol_card_scan (first_elem, elem - first_elem);
		}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		if (card_count > 0) {
			SGEN_ASSERT (0, card_data == SGEN_SHADOW_CARDTABLE_END, "Why we didn't stop at shadow cardtable end ?");
			extra_idx += card_data - card_base;
			card_base = card_data = sgen_shadow_cardtable;
			goto LOOP_HEAD;
		}
#endif
		return TRUE;
	}

	return FALSE;
}

/*
 * Array and string allocation
 */

MonoArray*
mono_gc_alloc_pinned_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *arr;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;

	arr = (MonoArray*)sgen_alloc_obj_pinned (vtable, size);
	if (G_UNLIKELY (!arr))
		return NULL;

	arr->max_length = (mono_array_size_t)max_length;

	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&arr->obj));

	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size (vtable, (GCObject*)arr)), "Vector has incorrect size.");
	return arr;
}

MonoArray*
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *arr;
	TLAB_ACCESS_INIT;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;

#ifndef DISABLE_CRITICAL_REGION
	ENTER_CRITICAL_REGION;
	arr = (MonoArray*)sgen_try_alloc_obj_nolock (vtable, size);
	if (arr) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		arr->max_length = (mono_array_size_t)max_length;
		EXIT_CRITICAL_REGION;
		goto done;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	arr = (MonoArray*)sgen_alloc_obj_nolock (vtable, size);
	if (G_UNLIKELY (!arr)) {
		UNLOCK_GC;
		return NULL;
	}

	arr->max_length = (mono_array_size_t)max_length;

	UNLOCK_GC;

 done:
	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&arr->obj));

	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size (vtable, (GCObject*)arr)), "Vector has incorrect size.");
	return arr;
}

MonoArray*
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size)
{
	MonoArray *arr;
	MonoArrayBounds *bounds;
	TLAB_ACCESS_INIT;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;

#ifndef DISABLE_CRITICAL_REGION
	ENTER_CRITICAL_REGION;
	arr = (MonoArray*)sgen_try_alloc_obj_nolock (vtable, size);
	if (arr) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		arr->max_length = (mono_array_size_t)max_length;

		bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
		arr->bounds = bounds;
		EXIT_CRITICAL_REGION;
		goto done;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	arr = (MonoArray*)sgen_alloc_obj_nolock (vtable, size);
	if (G_UNLIKELY (!arr)) {
		UNLOCK_GC;
		return NULL;
	}

	arr->max_length = (mono_array_size_t)max_length;

	bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
	arr->bounds = bounds;

	UNLOCK_GC;

 done:
	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&arr->obj));

	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size (vtable, (GCObject*)arr)), "Array has incorrect size.");
	return arr;
}

MonoString*
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len)
{
	MonoString *str;
	TLAB_ACCESS_INIT;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;

#ifndef DISABLE_CRITICAL_REGION
	ENTER_CRITICAL_REGION;
	str = (MonoString*)sgen_try_alloc_obj_nolock (vtable, size);
	if (str) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		str->length = len;
		EXIT_CRITICAL_REGION;
		goto done;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	str = (MonoString*)sgen_alloc_obj_nolock (vtable, size);
	if (G_UNLIKELY (!str)) {
		UNLOCK_GC;
		return NULL;
	}

	str->length = len;

	UNLOCK_GC;

 done:
	if (G_UNLIKELY (mono_profiler_allocations_enabled ()))
		MONO_PROFILER_RAISE (gc_allocation, (&str->object));

	return str;
}

/*
 * Strings
 */

void
mono_gc_set_string_length (MonoString *str, gint32 new_length)
{
	mono_unichar2 *new_end = str->chars + new_length;

	/* zero the discarded string. This null-delimits the string and allows
	 * the space to be reclaimed by SGen. */

	if (sgen_nursery_canaries_enabled () && sgen_ptr_in_nursery (str)) {
		CHECK_CANARY_FOR_OBJECT ((GCObject*)str, TRUE);
		memset (new_end, 0, (str->length - new_length + 1) * sizeof (mono_unichar2) + CANARY_SIZE);
		memcpy (new_end + 1 , CANARY_STRING, CANARY_SIZE);
	} else {
		memset (new_end, 0, (str->length - new_length + 1) * sizeof (mono_unichar2));
	}

	str->length = new_length;
}

/*
 * Profiling
 */

#define GC_ROOT_NUM 32
#define SPECIAL_ADDRESS_FIN_QUEUE ((mono_byte*)1)
#define SPECIAL_ADDRESS_CRIT_FIN_QUEUE ((mono_byte*)2)
#define SPECIAL_ADDRESS_EPHEMERON ((mono_byte*)3)
#define SPECIAL_ADDRESS_TOGGLEREF ((mono_byte*)4)

typedef struct {
	int count;		/* must be the first field */
	void *addresses [GC_ROOT_NUM];
	void *objects [GC_ROOT_NUM];
} GCRootReport;

static void
notify_gc_roots (GCRootReport *report)
{
	if (!report->count)
		return;
	MONO_PROFILER_RAISE (gc_roots, (report->count, (const mono_byte *const *)report->addresses, (MonoObject *const *) report->objects));
	report->count = 0;
}

static void
report_gc_root (GCRootReport *report, void *address, void *object)
{
	if (report->count == GC_ROOT_NUM)
		notify_gc_roots (report);
	report->addresses [report->count] = address;
	report->objects [report->count] = object;
	report->count++;
}

static void
single_arg_report_root (MonoObject **obj, void *gc_data)
{
	GCRootReport *report = (GCRootReport*)gc_data;
	if (*obj)
		report_gc_root (report, obj, *obj);
}

static void
two_args_report_root (void *address, MonoObject *obj, void *gc_data)
{
	GCRootReport *report = (GCRootReport*)gc_data;
	if (obj)
		report_gc_root (report, address, obj);
}

static void
precisely_report_roots_from (GCRootReport *report, void** start_root, void** end_root, mword desc)
{
	switch (desc & ROOT_DESC_TYPE_MASK) {
	case ROOT_DESC_BITMAP:
		desc >>= ROOT_DESC_TYPE_SHIFT;
		while (desc) {
			if ((desc & 1) && *start_root)
				report_gc_root (report, start_root, *start_root);
			desc >>= 1;
			start_root++;
		}
		return;
	case ROOT_DESC_COMPLEX: {
		gsize *bitmap_data = (gsize *)sgen_get_complex_descriptor_bitmap (desc);
		gsize bwords = (*bitmap_data) - 1;
		void **start_run = start_root;
		bitmap_data++;
		while (bwords-- > 0) {
			gsize bmap = *bitmap_data++;
			void **objptr = start_run;
			while (bmap) {
				if ((bmap & 1) && *objptr)
					report_gc_root (report, objptr, *objptr);
				bmap >>= 1;
				++objptr;
			}
			start_run += GC_BITS_PER_WORD;
		}
		break;
	}
	case ROOT_DESC_VECTOR: {
		void **p;

		for (p = start_root; p < end_root; p++) {
			if (*p)
				report_gc_root (report, p, *p);
		}
		break;
	}
	case ROOT_DESC_USER: {
		MonoGCRootMarkFunc marker = (MonoGCRootMarkFunc)sgen_get_user_descriptor_func (desc);

		if ((void*)marker == (void*)sgen_mark_normal_gc_handles)
			sgen_gc_handles_report_roots (two_args_report_root, report);
		else
			marker ((MonoObject**)start_root, single_arg_report_root, report);
		break;
	}
	case ROOT_DESC_RUN_LEN:
		g_assert_not_reached ();
	default:
		g_assert_not_reached ();
	}
}

static void
report_pinning_roots (GCRootReport *report, void **start, void **end)
{
	while (start < end) {
		mword addr = (mword)*start;
		addr &= ~(SGEN_ALLOC_ALIGN - 1);
		if (addr)
			report_gc_root (report, start, (void*)addr);

		start++;
	}
}

static SgenPointerQueue pinned_objects = SGEN_POINTER_QUEUE_INIT (INTERNAL_MEM_MOVED_OBJECT);
static mword lower_bound, upper_bound;

static GCObject*
find_pinned_obj (char *addr)
{
	size_t idx = sgen_pointer_queue_search (&pinned_objects, addr);

	if (idx != pinned_objects.next_slot) {
		if (pinned_objects.data [idx] == addr)
			return (GCObject*)pinned_objects.data [idx];
		if (idx == 0)
			return NULL;
	}

	GCObject *obj = (GCObject*)pinned_objects.data [idx - 1];
	if (addr > (char*)obj && addr < ((char*)obj + sgen_safe_object_get_size (obj)))
		return obj;
	return NULL;
}


/*
 * We pass @root_report_address so register are properly accounted towards their thread
*/
static void
report_conservative_roots (GCRootReport *report, void *root_report_address, void **start, void **end)
{
	while (start < end) {
		mword addr = (mword)*start;
		addr &= ~(SGEN_ALLOC_ALIGN - 1);

		if (addr < lower_bound || addr > upper_bound) {
			++start;
			continue;
		}

		GCObject *obj = find_pinned_obj ((char*)addr);
		if (obj)
			report_gc_root (report, root_report_address, obj);
		start++;
	}
}

typedef struct {
	gboolean precise;
	GCRootReport *report;
	SgenThreadInfo *info;
} ReportHandleStackRoot;

static void
report_handle_stack_root (gpointer *ptr, gpointer user_data)
{
	ReportHandleStackRoot *ud = (ReportHandleStackRoot*)user_data;
	GCRootReport *report = ud->report;
	gpointer addr = ud->info->client_info.info.handle_stack;

	// Note: We know that *ptr != NULL.
	if (ud->precise)
		report_gc_root (report, addr, *ptr);
	else
		report_conservative_roots (report, addr, ptr, ptr + 1);
}

static void
report_handle_stack_roots (GCRootReport *report, SgenThreadInfo *info, gboolean precise)
{
	ReportHandleStackRoot ud;
	memset (&ud, 0, sizeof (ud));
	ud.precise = precise;
	ud.report = report;
	ud.info = info;

	mono_handle_stack_scan (info->client_info.info.handle_stack, report_handle_stack_root, &ud, ud.precise, FALSE);
}

static void*
get_aligned_stack_start (SgenThreadInfo *info)
{
	void* aligned_stack_start = (void*)(mword) ALIGN_TO ((mword)info->client_info.stack_start, SIZEOF_VOID_P);
#if _WIN32
// Due to the guard page mechanism providing gradual commit of Windows stacks,
// stack pages must be touched in order.
//
// This mechanism is only transparent (kernel handles page faults and user never sees them),
// for the thread touching its own stack. Not for cross-thread stack references as are being
// done here.
//
// Here is a small program that demonstrates the behavior:
//
// #include <windows.h>
// #include <stdio.h>
//
// #pragma optimize ("x", on)
//
// int volatile * volatile Event1;
// int volatile Event2;
// HANDLE ThreadHandle;
//
// DWORD __stdcall thread (void* x)
// {
// 	while (!Event1)
// 		_mm_pause ();
//
// 	__try {
// 		*Event1 = 0x123;
// 	} __except (GetExceptionCode () == STATUS_GUARD_PAGE_VIOLATION) {
// 		printf ("oops\n");
// 	}
// 	Event2 = 1;
// 	return 0;
// }
//
// int unlucky;
// int print = 1;
//
// __declspec (noinline)
// __declspec (safebuffers)
// void f (void)
// {
// 	int local [5];
//
// 	while (unlucky && ((size_t)_AddressOfReturnAddress () - 8) & 0xFFF)
// 		f ();
//
// 	unlucky = 0;
// 	Event1 = local;
//
// 	while (!Event2)
// 		_mm_pause ();
//
// 	if (print) {
// 		printf ("%X\n", local [0]);
// 		print = 0;
// 	}
//
// 	if (ThreadHandle) {
// 		WaitForSingleObject (ThreadHandle, INFINITE);
// 		ThreadHandle = NULL;
// 	}
// }
//
// int main (int argc, char** argv)
// {
// 	unlucky = argc > 1;
// 	ThreadHandle = CreateThread (0, 0, thread, 0, 0, 0);
// 	f ();
// }
//
// This would seem to be a problem otherwise, not just for garbage collectors.
//
// We therefore have a few choices:
//
// 1. Historical slow code: VirtualQuery and check for guard page. Slow.
//
// MEMORY_BASIC_INFORMATION mem_info;
// SIZE_T result = VirtualQuery (info->client_info.stack_start, &mem_info, sizeof(mem_info));
// g_assert (result != 0);
// if (mem_info.Protect & PAGE_GUARD) {
// 	aligned_stack_start = ((char*) mem_info.BaseAddress) + mem_info.RegionSize;
// }
//
// VirtualQuery not historically allowed in UWP, but it is now.
//
// 2. Touch page under __try / __except and handle STATUS_GUARD_PAGE_VIOLATION.
//    Good but compiler specific.
//
// __try {
// 	*(volatile char*)aligned_stack_start;
// } __except (GetExceptionCode () == STATUS_GUARD_PAGE_VIOLATION) {
// 	MEMORY_BASIC_INFORMATION mem_info;
// 	const SIZE_T result = VirtualQuery(aligned_stack_start, &mem_info, sizeof(mem_info));
// 	g_assert (result >= sizeof (mem_info));
// 	VirtualProtect (aligned_stack_start, 1, mem_info.Protect | PAGE_GUARD, &mem_info.Protect);
// }
//
// 3. Vectored exception handler. Not terrible. Not compiler specific.
//
// 4. Check against the high watermark in the TIB. That is done.
//  TIB is the public prefix TEB. It is Windows.h, ntddk.h, etc.
//
	aligned_stack_start = MAX (aligned_stack_start, info->client_info.info.windows_tib->StackLimit);
#endif
	return aligned_stack_start;
}

static void
report_stack_roots (void)
{
	GCRootReport report = {0};
	FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
		void *aligned_stack_start;

		if (info->client_info.skip) {
			continue;
		} else if (!mono_thread_info_is_live (info)) {
			continue;
		} else if (!info->client_info.stack_start) {
			continue;
		}

		g_assert (info->client_info.stack_start);
		g_assert (info->client_info.info.stack_end);

		aligned_stack_start = get_aligned_stack_start (info);
		g_assert (info->client_info.suspend_done);

		report_conservative_roots (&report, aligned_stack_start, (void **)aligned_stack_start, (void **)info->client_info.info.stack_end);
		report_conservative_roots (&report, aligned_stack_start, (void**)&info->client_info.ctx, (void**)(&info->client_info.ctx + 1));

		report_handle_stack_roots (&report, info, FALSE);
		report_handle_stack_roots (&report, info, TRUE);
	} FOREACH_THREAD_END

	notify_gc_roots (&report);
}

static void
report_pin_queue (void)
{
	lower_bound = SIZE_MAX;
	upper_bound = 0;

	//sort the addresses
	sgen_pointer_queue_sort_uniq (&pinned_objects);

	for (gsize i = 0; i < pinned_objects.next_slot; ++i) {
		GCObject *obj = (GCObject*)pinned_objects.data [i];
		ssize_t size = sgen_safe_object_get_size (obj);

		ssize_t addr = (ssize_t)obj;
		lower_bound = MIN (lower_bound, GSSIZE_TO_SIZE(addr));
		upper_bound = MAX (upper_bound, GSSIZE_TO_SIZE(addr + size));
	}

	report_stack_roots ();
	sgen_pointer_queue_clear (&pinned_objects);
}

static void
report_finalizer_roots_from_queue (SgenPointerQueue *queue, void* queue_address)
{
	GCRootReport report;
	size_t i;

	report.count = 0;
	for (i = 0; i < queue->next_slot; ++i) {
		void *obj = queue->data [i];
		if (!obj)
			continue;
		report_gc_root (&report, queue_address, obj);
	}
	notify_gc_roots (&report);
}

static void
report_registered_roots_by_type (int root_type)
{
	GCRootReport report = { 0 };
	void **start_root;
	RootRecord *root;
	report.count = 0;
	SGEN_HASH_TABLE_FOREACH (&sgen_roots_hash [root_type], void **, start_root, RootRecord *, root) {
		SGEN_LOG (6, "Profiler root scan %p-%p (desc: %p)", start_root, root->end_root, (void*)(intptr_t)root->root_desc);
		if (root_type == ROOT_TYPE_PINNED)
			report_pinning_roots (&report, start_root, (void**)root->end_root);
		else
			precisely_report_roots_from (&report, start_root, (void**)root->end_root, root->root_desc);
	} SGEN_HASH_TABLE_FOREACH_END;
	notify_gc_roots (&report);
}

static void
report_registered_roots (void)
{
	for (int i = 0; i < ROOT_TYPE_NUM; ++i)
		report_registered_roots_by_type (i);
}

static void
report_ephemeron_roots (void)
{
        EphemeronLinkNode *current = ephemeron_list;
        Ephemeron *cur, *array_end;
        GCObject *tombstone;
        GCRootReport report = { 0 };

        for (current = ephemeron_list; current; current = current->next) {
                MonoArray *array = current->array;

                if (!sgen_is_object_alive_for_current_gen ((GCObject*)array))
                        continue;

                cur = mono_array_addr_internal (array, Ephemeron, 0);
                array_end = cur + mono_array_length_internal (array);
                tombstone = SGEN_LOAD_VTABLE ((GCObject*)array)->domain->ephemeron_tombstone;

                for (; cur < array_end; ++cur) {
                        GCObject *key = cur->key;

                        if (!key || key == tombstone)
                                continue;

                        if (cur->value && sgen_is_object_alive_for_current_gen (key))
				report_gc_root (&report, SPECIAL_ADDRESS_EPHEMERON, cur->value);
		}
	}

	notify_gc_roots (&report);
}

static void
report_toggleref_root (MonoObject* obj, gpointer data)
{
	report_gc_root ((GCRootReport*)data, SPECIAL_ADDRESS_TOGGLEREF, obj);
}

static void
report_toggleref_roots (void)
{
	GCRootReport report = { 0 };
	sgen_foreach_toggleref_root (report_toggleref_root, &report);
	notify_gc_roots (&report);
}

static void
sgen_report_all_roots (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;

	report_registered_roots ();
	report_ephemeron_roots ();
	report_toggleref_roots ();
	report_pin_queue ();
	report_finalizer_roots_from_queue (fin_ready_queue, SPECIAL_ADDRESS_FIN_QUEUE);
	report_finalizer_roots_from_queue (critical_fin_queue, SPECIAL_ADDRESS_CRIT_FIN_QUEUE);
}

void
sgen_client_pinning_start (void)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;

	sgen_pointer_queue_clear (&pinned_objects);
}

void
sgen_client_pinning_end (void)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;
}

void
sgen_client_nursery_objects_pinned (void **definitely_pinned, int count)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;

	for (int i = 0; i < count; ++i)
		sgen_pointer_queue_add (&pinned_objects, definitely_pinned [i]);
}

void
sgen_client_pinned_los_object (GCObject *obj)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;

	sgen_pointer_queue_add (&pinned_objects, obj);
}

void
sgen_client_pinned_cemented_object (GCObject *obj)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;

	// TODO: How do we report this in a way that makes sense?
}

void
sgen_client_pinned_major_heap_object (GCObject *obj)
{
	if (!MONO_PROFILER_ENABLED (gc_roots))
		return;

	sgen_pointer_queue_add (&pinned_objects, obj);
}

void
sgen_client_collecting_minor_report_roots (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue)
{
	sgen_report_all_roots (fin_ready_queue, critical_fin_queue);
}

void
sgen_client_collecting_major_report_roots (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue)
{
	sgen_report_all_roots (fin_ready_queue, critical_fin_queue);
}

#define MOVED_OBJECTS_NUM 64
static void *moved_objects [MOVED_OBJECTS_NUM];
static int moved_objects_idx = 0;

static SgenPointerQueue moved_objects_queue = SGEN_POINTER_QUEUE_INIT (INTERNAL_MEM_MOVED_OBJECT);

void
mono_sgen_register_moved_object (void *obj, void *destination)
{
	/*
	 * This function can be called from SGen's worker threads. We want to try
	 * and avoid exposing those threads to the profiler API, so queue up move
	 * events and send them later when the main GC thread calls
	 * mono_sgen_gc_event_moves ().
	 *
	 * TODO: Once SGen has multiple worker threads, we need to switch to a
	 * lock-free data structure for the queue as multiple threads will be
	 * adding to it at the same time.
	 */
	if (sgen_workers_is_worker_thread (mono_native_thread_id_get ())) {
		sgen_pointer_queue_add (&moved_objects_queue, obj);
		sgen_pointer_queue_add (&moved_objects_queue, destination);
	} else {
		if (moved_objects_idx == MOVED_OBJECTS_NUM) {
			MONO_PROFILER_RAISE (gc_moves, ((MonoObject **) moved_objects, moved_objects_idx));
			moved_objects_idx = 0;
		}

		moved_objects [moved_objects_idx++] = obj;
		moved_objects [moved_objects_idx++] = destination;
	}
}

void
mono_sgen_gc_event_moves (void)
{
	while (!sgen_pointer_queue_is_empty (&moved_objects_queue)) {
		void *dst = sgen_pointer_queue_pop (&moved_objects_queue);
		void *src = sgen_pointer_queue_pop (&moved_objects_queue);

		mono_sgen_register_moved_object (src, dst);
	}

	if (moved_objects_idx) {
		MONO_PROFILER_RAISE (gc_moves, ((MonoObject **) moved_objects, moved_objects_idx));
		moved_objects_idx = 0;
	}
}

/*
 * Heap walking
 */

#define REFS_SIZE 128
typedef struct {
	void *data;
	MonoGCReferences callback;
	int flags;
	int count;
	int called;
	MonoObject *refs [REFS_SIZE];
	uintptr_t offsets [REFS_SIZE];
} HeapWalkInfo;

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr)) {	\
			if (hwi->count == REFS_SIZE) {	\
				hwi->callback ((MonoObject*)start, mono_object_class (start), hwi->called? 0: size, hwi->count, hwi->refs, hwi->offsets, hwi->data);	\
				hwi->count = 0;	\
				hwi->called = 1;	\
			}	\
			hwi->offsets [hwi->count] = (char*)(ptr)-(char*)start;	\
			hwi->refs [hwi->count++] = *(ptr);	\
		}	\
	} while (0)

static void
collect_references (HeapWalkInfo *hwi, GCObject *obj, size_t size)
{
	char *start = (char*)obj;
	mword desc = sgen_obj_get_descriptor (obj);

#include "sgen/sgen-scan-object.h"
}

static void
walk_references (GCObject *start, size_t size, void *data)
{
	HeapWalkInfo *hwi = (HeapWalkInfo *)data;
	hwi->called = 0;
	hwi->count = 0;
	collect_references (hwi, start, size);
	if (hwi->count || !hwi->called)
		hwi->callback (start, mono_object_class (start), hwi->called? 0: size, hwi->count, hwi->refs, hwi->offsets, hwi->data);
}

/**
 * mono_gc_walk_heap:
 * \param flags flags for future use
 * \param callback a function pointer called for each object in the heap
 * \param data a user data pointer that is passed to callback
 * This function can be used to iterate over all the live objects in the heap;
 * for each object, \p callback is invoked, providing info about the object's
 * location in memory, its class, its size and the objects it references.
 * For each referenced object its offset from the object address is
 * reported in the offsets array.
 * The object references may be buffered, so the callback may be invoked
 * multiple times for the same object: in all but the first call, the size
 * argument will be zero.
 * Note that this function can be only called in the \c MONO_GC_EVENT_PRE_START_WORLD
 * profiler event handler.
 * \returns a non-zero value if the GC doesn't support heap walking
 */
int
mono_gc_walk_heap (int flags, MonoGCReferences callback, void *data)
{
	HeapWalkInfo hwi;

	hwi.flags = flags;
	hwi.callback = callback;
	hwi.data = data;

	sgen_clear_nursery_fragments ();
	sgen_scan_area_with_callback (sgen_nursery_section->data, sgen_nursery_section->end_data, walk_references, &hwi, FALSE, TRUE);

	sgen_major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, walk_references, &hwi);
	sgen_los_iterate_objects (walk_references, &hwi);

	return 0;
}

/*
 * Threads
 */

void
mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks)
{
	gc_callbacks = *callbacks;
}

MonoGCCallbacks *
mono_gc_get_gc_callbacks (void)
{
	return &gc_callbacks;
}

gpointer
mono_gc_thread_attach (SgenThreadInfo *info)
{
	return sgen_thread_attach (info);
}

void
sgen_client_thread_attach (SgenThreadInfo* info)
{
	mono_tls_set_sgen_thread_info (info);

	info->client_info.skip = FALSE;

	info->client_info.stack_start = NULL;

	memset (&info->client_info.ctx, 0, sizeof (MonoContext));

	if (mono_gc_get_gc_callbacks ()->thread_attach_func)
		info->client_info.runtime_data = mono_gc_get_gc_callbacks ()->thread_attach_func ();

	sgen_binary_protocol_thread_register ((gpointer)(gsize) mono_thread_info_get_tid (info));

	SGEN_LOG (3, "registered thread %p (%p) stack end %p", info, (gpointer)(gsize) mono_thread_info_get_tid (info), info->client_info.info.stack_end);

	info->client_info.info.handle_stack = mono_handle_stack_alloc ();
}

void
mono_gc_thread_detach (SgenThreadInfo *info)
{
}

void
mono_gc_thread_detach_with_lock (SgenThreadInfo *info)
{
	sgen_thread_detach_with_lock (info);
}

void
sgen_client_thread_detach_with_lock (SgenThreadInfo *p)
{
	MonoNativeThreadId tid;

	mono_tls_set_sgen_thread_info (NULL);

	sgen_increment_bytes_allocated_detached (p->total_bytes_allocated + (p->tlab_next - p->tlab_start));

	tid = mono_thread_info_get_tid (p);

	mono_threads_add_joinable_runtime_thread (&p->client_info.info);

	if (mono_gc_get_gc_callbacks ()->thread_detach_func) {
		mono_gc_get_gc_callbacks ()->thread_detach_func (p->client_info.runtime_data);
		p->client_info.runtime_data = NULL;
	}

	sgen_binary_protocol_thread_unregister ((gpointer)(gsize) tid);
	SGEN_LOG (3, "unregister thread %p (%p)", p, (gpointer)(gsize) tid);

	HandleStack *handles = p->client_info.info.handle_stack;
	p->client_info.info.handle_stack = NULL;
	mono_handle_stack_free (handles);
}

void
mono_gc_skip_thread_changing (gboolean skip)
{
	/*
	 * SGen's STW will respect the thread info flags, but we do need to take
	 * the GC lock when changing them. If we don't do this, SGen might end up
	 * trying to resume a thread that wasn't suspended because it had
	 * MONO_THREAD_INFO_FLAGS_NO_GC set when STW began.
	 */
	LOCK_GC;

	if (skip) {
		/*
		 * If we skip scanning a thread with a non-empty handle stack, we may move an
		 * object but fail to update the reference in the handle.
		 */
		HandleStack *stack = mono_thread_info_current ()->client_info.info.handle_stack;
		g_assert (stack == NULL || mono_handle_stack_is_empty (stack));
	}
}

void
mono_gc_skip_thread_changed (gboolean skip)
{
	UNLOCK_GC;
}

gboolean
mono_gc_thread_in_critical_region (SgenThreadInfo *info)
{
	return info->client_info.in_critical_region;
}

/**
 * mono_gc_is_gc_thread:
 */
gboolean
mono_gc_is_gc_thread (void)
{
	gboolean result;
	LOCK_GC;
	result = mono_thread_info_current () != NULL;
	UNLOCK_GC;
	return result;
}

void
sgen_client_thread_register_worker (void)
{
	mono_thread_info_register_small_id ();
	mono_native_thread_set_name (mono_native_thread_id_get (), "SGen worker");
	mono_thread_set_name_windows (GetCurrentThread (), L"SGen worker");
}

/* Variables holding start/end nursery so it won't have to be passed at every call */
static void *scan_area_arg_start, *scan_area_arg_end;

void
mono_gc_conservatively_scan_area (void *start, void *end)
{
	sgen_conservatively_pin_objects_from ((void **)start, (void **)end, scan_area_arg_start, scan_area_arg_end, PIN_TYPE_STACK);
}

void*
mono_gc_scan_object (void *obj, void *gc_data)
{
	ScanCopyContext *ctx = (ScanCopyContext *)gc_data;
	ctx->ops->copy_or_mark_object ((GCObject**)&obj, ctx->queue);
	return obj;
}

typedef struct {
	void **start_nursery;
	void **end_nursery;
} PinHandleStackInteriorPtrData;

/* Called when we're scanning the handle stack imprecisely and we encounter a pointer into the
   middle of an object.
 */
static void
pin_handle_stack_interior_ptrs (void **ptr_slot, void *user_data)
{
	PinHandleStackInteriorPtrData *ud = (PinHandleStackInteriorPtrData *)user_data;
	sgen_conservatively_pin_objects_from (ptr_slot, ptr_slot+1, ud->start_nursery, ud->end_nursery, PIN_TYPE_STACK);
}

#if defined(HOST_WASM) || defined(HOST_WASI)
extern gboolean mono_wasm_enable_gc;
#endif

/*
 * Mark from thread stacks and registers.
 */
void
sgen_client_scan_thread_data (void *start_nursery, void *end_nursery, gboolean precise, ScanCopyContext ctx)
{
	scan_area_arg_start = start_nursery;
	scan_area_arg_end = end_nursery;
#if defined(HOST_WASM) || defined(HOST_WASI)
	//Under WASM we don't scan thread stacks and we can't trust the values we find there either.
	if (!mono_wasm_enable_gc)
		return;
#endif

	SGEN_TV_DECLARE (scan_thread_data_start);
	SGEN_TV_DECLARE (scan_thread_data_end);

	SGEN_TV_GETTIME (scan_thread_data_start);

	if (gc_callbacks.interp_mark_func)
		/* The interpreter code uses only compiler write barriers so have to synchronize with it */
		mono_memory_barrier_process_wide ();

	FOREACH_THREAD_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_GC) {
		int skip_reason = 0;
		void *aligned_stack_start;

		if (info->client_info.skip) {
			SGEN_LOG (3, "Skipping dead thread %p, range: %p-%p, size: %" G_GSIZE_FORMAT "d", info, info->client_info.stack_start, info->client_info.info.stack_end, (char*)info->client_info.info.stack_end - (char*)info->client_info.stack_start);
			skip_reason = 1;
		} else if (!mono_thread_info_is_live (info)) {
			SGEN_LOG (3, "Skipping non-running thread %p, range: %p-%p, size: %" G_GSIZE_FORMAT "d (state %x)", info, info->client_info.stack_start, info->client_info.info.stack_end, (char*)info->client_info.info.stack_end - (char*)info->client_info.stack_start, info->client_info.info.thread_state.raw);
			skip_reason = 3;
		} else if (!info->client_info.stack_start) {
			SGEN_LOG (3, "Skipping starting or detaching thread %p", info);
			skip_reason = 4;
		}

		sgen_binary_protocol_scan_stack ((gpointer)(gsize) mono_thread_info_get_tid (info), info->client_info.stack_start, info->client_info.info.stack_end, skip_reason);

		if (skip_reason) {
			if (precise) {
				/* If we skip a thread with a non-empty handle stack and then it
				 * resumes running we may potentially move an object but fail to
				 * update the reference in the handle.
				 */
				HandleStack *stack = info->client_info.info.handle_stack;
				g_assert (stack == NULL || mono_handle_stack_is_empty (stack));
			}
			continue;
		}

		g_assert (info->client_info.stack_start);
		g_assert (info->client_info.info.stack_end);

		aligned_stack_start = get_aligned_stack_start (info);
		g_assert (info->client_info.suspend_done);
		if (!debug_coop_no_stack_scan) {
			SGEN_LOG (3, "Scanning thread %p, range: %p-%p, size: %" G_GSIZE_FORMAT "d, pinned=%" G_GSIZE_FORMAT "d", info, info->client_info.stack_start, info->client_info.info.stack_end, (char*)info->client_info.info.stack_end - (char*)info->client_info.stack_start, sgen_get_pinned_count ());
			if (mono_gc_get_gc_callbacks ()->thread_mark_func && !conservative_stack_mark) {
				mono_gc_get_gc_callbacks ()->thread_mark_func (info->client_info.runtime_data, (guint8 *)aligned_stack_start, (guint8 *)info->client_info.info.stack_end, precise, &ctx);
			} else if (!precise) {
				if (!conservative_stack_mark) {
					fprintf (stderr, "Precise stack mark not supported - disabling.\n");
					conservative_stack_mark = TRUE;
				}
				//FIXME we should eventually use the new stack_mark from coop
				sgen_conservatively_pin_objects_from ((void **)aligned_stack_start, (void **)info->client_info.info.stack_end, start_nursery, end_nursery, PIN_TYPE_STACK);
			}

			if (!precise) {
				sgen_conservatively_pin_objects_from ((void**)&info->client_info.ctx, (void**)(&info->client_info.ctx + 1),
					start_nursery, end_nursery, PIN_TYPE_STACK);

				{
					// This is used on Coop GC for platforms where we cannot get the data for individual registers.
					// We force a spill of all registers into the stack and pass a chunk of data into sgen.
					//FIXME under coop, for now, what we need to ensure is that we scan any extra memory from info->client_info.info.stack_end to stack_mark
					MonoThreadUnwindState *state = &info->client_info.info.thread_saved_state [SELF_SUSPEND_STATE_INDEX];
					if (state && state->gc_stackdata) {
						sgen_conservatively_pin_objects_from ((void **)state->gc_stackdata, (void**)((char*)state->gc_stackdata + state->gc_stackdata_size),
							start_nursery, end_nursery, PIN_TYPE_STACK);
					}
				}
			}
		}
		if (gc_callbacks.interp_mark_func) {
			PinHandleStackInteriorPtrData ud;
			memset (&ud, 0, sizeof (ud));
			ud.start_nursery = (void**)start_nursery;
			ud.end_nursery = (void**)end_nursery;
			SGEN_LOG (3, "Scanning thread %p interp stack", info);
			gc_callbacks.interp_mark_func (&info->client_info.info, pin_handle_stack_interior_ptrs, &ud, precise);
		}
		if (info->client_info.info.handle_stack) {
			/*
			  Make two passes over the handle stack.  On the imprecise pass, pin all
			  objects where the handle points into the interior of the object. On the
			  precise pass, copy or mark all the objects that have handles to the
			  beginning of the object.
			*/
			if (precise)
				mono_handle_stack_scan (info->client_info.info.handle_stack, (GcScanFunc)ctx.ops->copy_or_mark_object, ctx.queue, precise, TRUE);
			else {
				PinHandleStackInteriorPtrData ud;
				memset (&ud, 0, sizeof (ud));
				ud.start_nursery = (void**)start_nursery;
				ud.end_nursery = (void**)end_nursery;
				mono_handle_stack_scan (info->client_info.info.handle_stack, pin_handle_stack_interior_ptrs, &ud, precise, FALSE);
			}
		}
	} FOREACH_THREAD_END

	SGEN_TV_GETTIME (scan_thread_data_end);
	SGEN_LOG (2, "Scanning thread data: %lld usecs", (long long)(SGEN_TV_ELAPSED (scan_thread_data_start, scan_thread_data_end) / 10));
}

/*
 * mono_gc_set_stack_end:
 *
 *   Set the end of the current threads stack to STACK_END. The stack space between
 * STACK_END and the real end of the threads stack will not be scanned during collections.
 */
void
mono_gc_set_stack_end (void *stack_end)
{
	SgenThreadInfo *info;

	LOCK_GC;
	info = mono_thread_info_current ();
	if (info) {
		SGEN_ASSERT (0, stack_end < info->client_info.info.stack_end, "Can only lower stack end");
		info->client_info.info.stack_end = stack_end;
	}
	UNLOCK_GC;
}

/*
 * Roots
 */

int
mono_gc_register_root (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	return sgen_register_root (start, size, descr, descr ? ROOT_TYPE_NORMAL : ROOT_TYPE_PINNED, source, key, msg);
}

int
mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg)
{
	return sgen_register_root (start, size, descr, ROOT_TYPE_WBARRIER, source, key, msg);
}

void
mono_gc_deregister_root (char* addr)
{
	sgen_deregister_root (addr);
}

/*
 * PThreads
 */

#ifndef HOST_WIN32
int
mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	int res;

	MONO_ENTER_GC_SAFE;
	mono_threads_join_lock ();
	res = pthread_create (new_thread, attr, start_routine, arg);
	mono_threads_join_unlock ();
	MONO_EXIT_GC_SAFE;

	return res;
}
#endif

/*
 * Miscellaneous
 */

static size_t last_heap_size = -1;
static size_t worker_heap_size;

void
sgen_client_total_allocated_heap_changed (size_t allocated_heap)
{
	mono_runtime_resource_check_limit (MONO_RESOURCE_GC_HEAP, allocated_heap);

	/*
	 * This function can be called from SGen's worker threads. We want to try
	 * and avoid exposing those threads to the profiler API, so save the heap
	 * size value and report it later when the main GC thread calls
	 * mono_sgen_gc_event_resize ().
	 */
	worker_heap_size = allocated_heap;
}

void
mono_sgen_gc_event_resize (void)
{
	if (worker_heap_size != last_heap_size) {
		last_heap_size = worker_heap_size;
		MONO_PROFILER_RAISE (gc_resize, (last_heap_size));
	}
}

gboolean
mono_gc_user_markers_supported (void)
{
	return TRUE;
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	return TRUE;
}

int
mono_gc_get_generation (MonoObject *obj)
{
	if (sgen_ptr_in_nursery (obj))
		return 0;
	return 1;
}

const char *
mono_gc_get_gc_name (void)
{
	return "sgen";
}

char*
mono_gc_get_description (void)
{
#ifdef HAVE_CONC_GC_AS_DEFAULT
	return g_strdup ("sgen (concurrent by default)");
#else
	return g_strdup ("sgen");
#endif
}

void
mono_gc_set_desktop_mode (void)
{
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

#ifdef HOST_WIN32
BOOL APIENTRY mono_gc_dllmain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	return TRUE;
}
#endif

int
mono_gc_max_generation (void)
{
	return 1;
}

gboolean
mono_gc_precise_stack_mark_enabled (void)
{
	return !conservative_stack_mark;
}

void
mono_gc_collect (int generation)
{
	MONO_ENTER_GC_UNSAFE;
	sgen_gc_collect (generation);
	MONO_EXIT_GC_UNSAFE;
}

int
mono_gc_collection_count (int generation)
{
	return sgen_gc_collection_count (generation);
}

int64_t
mono_gc_get_generation_size (int generation)
{
	if (generation == GENERATION_NURSERY)
		return (int64_t)sgen_gc_info.total_nursery_size_bytes;
	else if (generation == GENERATION_OLD)
		return (int64_t)sgen_gc_info.total_major_size_bytes;
	else if (generation == 3)
		return (int64_t)sgen_gc_info.total_los_size_bytes;
	else
		return 0;
}

int64_t
mono_gc_get_used_size (void)
{
	return (int64_t)sgen_gc_get_used_size ();
}

int64_t
mono_gc_get_heap_size (void)
{
	return (int64_t)sgen_gc_get_total_heap_allocation ();
}

void
mono_gc_get_gcmemoryinfo (
	gint64 *high_memory_load_threshold_bytes,
	gint64 *memory_load_bytes,
	gint64 *total_available_memory_bytes,
	gint64 *total_committed_bytes,
	gint64 *heap_size_bytes,
	gint64 *fragmented_bytes)
{
	*high_memory_load_threshold_bytes = sgen_gc_info.high_memory_load_threshold_bytes;
	*fragmented_bytes = sgen_gc_info.fragmented_bytes;

	*heap_size_bytes = sgen_gc_info.heap_size_bytes;

	*memory_load_bytes = sgen_gc_info.memory_load_bytes;
	*total_available_memory_bytes = sgen_gc_info.total_available_memory_bytes;
	*total_committed_bytes = sgen_gc_info.total_committed_bytes;
}

void mono_gc_get_gctimeinfo (
	guint64 *time_last_gc_100ns,
	guint64 *time_since_last_gc_100ns,
	guint64 *time_max_gc_100ns)
{
	sgen_gc_get_gctimeinfo (time_last_gc_100ns, time_since_last_gc_100ns, time_max_gc_100ns);
}

MonoGCDescriptor
mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker)
{
	return sgen_make_user_root_descriptor (marker);
}

MonoGCDescriptor
mono_gc_make_descr_for_string (gsize *bitmap, int numbits)
{
	return SGEN_DESC_STRING;
}

void
mono_gc_register_obj_with_weak_fields (void *obj)
{
	sgen_register_obj_with_weak_fields ((MonoObject*)obj);
}

void*
mono_gc_get_nursery (int *shift_bits, size_t *size)
{
	*size = sgen_nursery_size;
	*shift_bits = sgen_nursery_bits;
	return sgen_get_nursery_start ();
}

int
mono_gc_get_los_limit (void)
{
	return SGEN_MAX_SMALL_OBJ_SIZE;
}

guint64
mono_gc_get_allocated_bytes_for_current_thread (void)
{
	SgenThreadInfo* info;
	info = mono_thread_info_current ();

	/*There are some more allocated bytes in the current tlab that have not been recorded yet */
	return info->total_bytes_allocated + (ptrdiff_t)(info->tlab_next - info->tlab_start);
}

guint64
mono_gc_get_total_allocated_bytes (MonoBoolean precise)
{
	return sgen_get_total_allocated_bytes (precise);
}

gpointer
sgen_client_default_metadata (void)
{
	return mono_domain_get ();
}

gpointer
sgen_client_metadata_for_object (GCObject *obj)
{
	return mono_object_domain (obj);
}

/**
 * mono_gchandle_new_internal:
 * \param obj managed object to get a handle for
 * \param pinned whether the object should be pinned
 * This returns a handle that wraps the object, this is used to keep a
 * reference to a managed object from the unmanaged world and preventing the
 * object from being disposed.
 *
 * If \p pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object.
 *
 * \returns a handle that can be used to access the object from unmanaged code.
 */
MonoGCHandle
mono_gchandle_new_internal (MonoObject *obj, gboolean pinned)
{
	return MONO_GC_HANDLE_FROM_UINT (sgen_gchandle_new (obj, pinned));
}

/**
 * mono_gchandle_new_weakref_internal:
 * \param obj managed object to get a handle for
 * \param track_resurrection Determines how long to track the object, if this is set to TRUE, the object is tracked after finalization, if FALSE, the object is only tracked up until the point of finalization.
 *
 * This returns a weak handle that wraps the object, this is used to
 * keep a reference to a managed object from the unmanaged world.
 * Unlike the \c mono_gchandle_new_internal the object can be reclaimed by the
 * garbage collector.  In this case the value of the GCHandle will be
 * set to zero.
 *
 * If \p track_resurrection is TRUE the object will be tracked through
 * finalization and if the object is resurrected during the execution
 * of the finalizer, then the returned weakref will continue to hold
 * a reference to the object.   If \p track_resurrection is FALSE, then
 * the weak reference's target will become NULL as soon as the object
 * is passed on to the finalizer.
 *
 * \returns a handle that can be used to access the object from
 * unmanaged code.
 */
MonoGCHandle
mono_gchandle_new_weakref_internal (GCObject *obj, gboolean track_resurrection)
{
	return MONO_GC_HANDLE_FROM_UINT (sgen_gchandle_new_weakref (obj, track_resurrection));
}

/**
 * mono_gchandle_free_internal:
 * \param gchandle a GCHandle's handle.
 *
 * Frees the \p gchandle handle.  If there are no outstanding
 * references, the garbage collector can reclaim the memory of the
 * object wrapped.
 */
void
mono_gchandle_free_internal (MonoGCHandle gchandle)
{
	sgen_gchandle_free (MONO_GC_HANDLE_TO_UINT (gchandle));
}

/**
 * mono_gchandle_get_target_internal:
 * \param gchandle a GCHandle's handle.
 *
 * The handle was previously created by calling \c mono_gchandle_new_internal or
 * \c mono_gchandle_new_weakref.
 *
 * \returns a pointer to the \c MonoObject* represented by the handle or
 * NULL for a collected object if using a weakref handle.
 */
MonoObject*
mono_gchandle_get_target_internal (MonoGCHandle gchandle)
{
	return sgen_gchandle_get_target (MONO_GC_HANDLE_TO_UINT (gchandle));
}

void
mono_gchandle_set_target (MonoGCHandle gchandle, MonoObject *obj)
{
	sgen_gchandle_set_target (MONO_GC_HANDLE_TO_UINT (gchandle), obj);
}

void
sgen_client_gchandle_created (int handle_type, GCObject *obj, guint32 handle)
{

	MONO_PROFILER_RAISE (gc_handle_created, (handle, (MonoGCHandleType)handle_type, obj));
}

void
sgen_client_gchandle_destroyed (int handle_type, guint32 handle)
{

	MONO_PROFILER_RAISE (gc_handle_deleted, (handle, (MonoGCHandleType)handle_type));
}

void
sgen_client_ensure_weak_gchandles_accessible (void)
{
	/*
	 * During the second bridge processing step the world is
	 * running again.  That step processes all weak links once
	 * more to null those that refer to dead objects.  Before that
	 * is completed, those links must not be followed, so we
	 * conservatively wait for bridge processing when any weak
	 * link is dereferenced.
	 */
	/* FIXME: A GC can occur after this check fails, in which case we
	 * should wait for bridge processing but would fail to do so.
	 */
	if (G_UNLIKELY (mono_bridge_processing_in_progress))
		mono_gc_wait_for_bridge_processing_internal ();
}

void*
mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data)
{
	void *result;
	LOCK_INTERRUPTION;
	result = func (data);
	UNLOCK_INTERRUPTION;
	return result;
}

void
mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size)
{
	// FIXME:
}

guint8*
mono_gc_get_card_table (int *shift_bits, gpointer *mask)
{
	return sgen_get_card_table_configuration (shift_bits, mask);
}

guint8*
mono_gc_get_target_card_table (int *shift_bits, target_mgreg_t *mask)
{
	return sgen_get_target_card_table_configuration (shift_bits, mask);
}

gboolean
mono_gc_card_table_nursery_check (void)
{
	return !sgen_get_major_collector ()->is_concurrent;
}

void
mono_gc_add_memory_pressure (guint64 value)
{
	sgen_add_memory_pressure(value);
}

void
mono_gc_remove_memory_pressure (guint64 value)
{
	sgen_remove_memory_pressure(value);
}

/*
 * Logging
 */

void
sgen_client_degraded_allocation (void)
{
	//The WASM target always triggers degrated allocation before collecting. So no point in printing the warning as it will just confuse users
#ifndef HOST_WASM
	static gint32 last_major_gc_warned = -1;
	static gint32 num_degraded = 0;

	gint32 major_gc_count = mono_atomic_load_i32 (&mono_gc_stats.major_gc_count);
	if (mono_atomic_load_i32 (&last_major_gc_warned) < major_gc_count) {
		gint32 num = mono_atomic_inc_i32 (&num_degraded);
		if (num == 1 || num == 3)
			mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_GC, "Warning: Degraded allocation.  Consider increasing nursery-size if the warning persists.");
		else if (num == 10)
			mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_GC, "Warning: Repeated degraded allocation.  Consider increasing nursery-size.");

		mono_atomic_store_i32 (&last_major_gc_warned, major_gc_count);
	}
#endif
}

/*
 * Debugging
 */

const char*
sgen_client_description_for_internal_mem_type (int type)
{
	switch (type) {
	case INTERNAL_MEM_EPHEMERON_LINK: return "ephemeron-link";
	case INTERNAL_MEM_MOVED_OBJECT: return "moved-object";
	default:
		return NULL;
	}
}

gboolean
sgen_client_vtable_is_inited (MonoVTable *vt)
{
	return m_class_is_inited (vt->klass);
}

const char*
sgen_client_vtable_get_namespace (MonoVTable *vt)
{
	return m_class_get_name_space (vt->klass);
}

const char*
sgen_client_vtable_get_name (MonoVTable *vt)
{
	return m_class_get_name (vt->klass);
}

/*
 * Initialization
 */

void
sgen_client_init (void)
{
	mono_thread_callbacks_init ();
	mono_thread_info_init (sizeof (SgenThreadInfo));

	///* Keep this the default for now */
	/* Precise marking is broken on all supported targets. Disable until fixed. */
	conservative_stack_mark = TRUE;

	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_EPHEMERON_LINK, sizeof (EphemeronLinkNode));

	mono_sgen_init_stw ();

	mono_tls_init_gc_keys ();

	mono_thread_info_attach ();
}

void
mono_gc_init_icalls (void)
{
	mono_register_jit_icall (mono_gc_alloc_obj, mono_icall_sig_object_ptr_sizet, FALSE);
	mono_register_jit_icall (mono_gc_alloc_vector, mono_icall_sig_object_ptr_sizet_ptr, FALSE);
	mono_register_jit_icall (mono_gc_alloc_string, mono_icall_sig_object_ptr_sizet_int32, FALSE);
	mono_register_jit_icall (mono_profiler_raise_gc_allocation, mono_icall_sig_void_object, FALSE);
}

gboolean
sgen_client_handle_gc_param (const char *opt)
{
	if (g_str_has_prefix (opt, "stack-mark=")) {
		opt = strchr (opt, '=') + 1;
		if (!strcmp (opt, "precise")) {
			conservative_stack_mark = FALSE;
		} else if (!strcmp (opt, "conservative")) {
			conservative_stack_mark = TRUE;
		} else {
			sgen_env_var_error (MONO_GC_PARAMS_NAME, conservative_stack_mark ? "Using `conservative`." : "Using `precise`.",
					"Invalid value `%s` for `stack-mark` option, possible values are: `precise`, `conservative`.", opt);
		}
	} else if (g_str_has_prefix (opt, "bridge-implementation=")) {
		opt = strchr (opt, '=') + 1;
		sgen_set_bridge_implementation (opt);
	} else if (g_str_has_prefix (opt, "toggleref-test")) {
		/* FIXME: This should probably in MONO_GC_DEBUG */
		sgen_register_test_toggleref_callback ();
	} else if (!sgen_bridge_handle_gc_param (opt)) {
		return FALSE;
	}
	return TRUE;
}

void
sgen_client_print_gc_params_usage (void)
{
	fprintf (stderr, "  stack-mark=MARK-METHOD (where MARK-METHOD is 'precise' or 'conservative')\n");
}

gboolean
sgen_client_handle_gc_debug (const char *opt)
{
	if (!strcmp (opt, "do-not-finalize")) {
		mono_do_not_finalize = TRUE;
	} else if (g_str_has_prefix (opt, "do-not-finalize=")) {
		opt = strchr (opt, '=') + 1;
		mono_do_not_finalize = TRUE;
		mono_do_not_finalize_class_names = g_strsplit (opt, ",", 0);
	} else if (!strcmp (opt, "log-finalizers")) {
		mono_log_finalizers = TRUE;
	} else if (!strcmp (opt, "no-managed-allocator")) {
		sgen_set_use_managed_allocator (FALSE);
	} else if (!strcmp (opt, "managed-allocator")) {
		/*
		 * This option can be used to override the disabling of the managed allocator by
		 * the nursery canaries option. This can be used when knowing for sure that no
		 * aot code will be used by the application.
		 */
		sgen_set_use_managed_allocator (TRUE);
	} else if (!sgen_bridge_handle_gc_debug (opt)) {
		return FALSE;
	}
	return TRUE;
}

void
sgen_client_print_gc_debug_usage (void)
{
	fprintf (stderr, "  do-not-finalize\n");
	fprintf (stderr, "  log-finalizers\n");
	fprintf (stderr, "  no-managed-allocator\n");
	sgen_bridge_print_gc_debug_usage ();
}


gpointer
sgen_client_get_provenance (void)
{
#ifdef SGEN_OBJECT_PROVENANCE
	MonoGCCallbacks *cb = mono_gc_get_gc_callbacks ();
	gpointer (*get_provenance_func) (void);
	if (!cb)
		return NULL;
	get_provenance_func = cb->get_provenance_func;
	if (get_provenance_func)
		return get_provenance_func ();
	return NULL;
#else
	return NULL;
#endif
}

void
sgen_client_describe_invalid_pointer (GCObject *ptr)
{
	sgen_bridge_describe_pointer (ptr);
}

static gboolean gc_inited;

/**
 * mono_gc_base_init:
 */
void
mono_gc_base_init (void)
{
	if (gc_inited)
		return;

	mono_counters_init ();

#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

#ifdef HEAVY_STATISTICS
	mono_counters_register ("los marked cards", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &los_marked_cards);
	mono_counters_register ("los array cards scanned ", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &los_array_cards);
	mono_counters_register ("los array remsets", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &los_array_remsets);

	mono_counters_register ("WBarrier set arrayref", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_set_arrayref);
	mono_counters_register ("WBarrier value copy", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_value_copy);
	mono_counters_register ("WBarrier object copy", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_object_copy);
#endif

	sgen_gc_init ();

	gc_inited = TRUE;
}

gboolean
mono_gc_is_null (void)
{
	return FALSE;
}

gsize *
sgen_client_get_weak_bitmap (MonoVTable *vt, int *nbits)
{
	MonoClass *klass = vt->klass;

	return mono_class_get_weak_bitmap (klass, nbits);
}

void
sgen_client_binary_protocol_collection_begin (int minor_gc_count, int generation)
{
	static gboolean pseudo_roots_registered;

	MONO_GC_BEGIN (generation);

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_START, generation, generation == GENERATION_OLD && sgen_concurrent_collection_in_progress));

	if (!pseudo_roots_registered) {
		pseudo_roots_registered = TRUE;
		MONO_PROFILER_RAISE (gc_root_register, (SPECIAL_ADDRESS_FIN_QUEUE, 1, MONO_ROOT_SOURCE_FINALIZER_QUEUE, NULL, "Finalizer Queue"));
		MONO_PROFILER_RAISE (gc_root_register, (SPECIAL_ADDRESS_CRIT_FIN_QUEUE, 1, MONO_ROOT_SOURCE_FINALIZER_QUEUE, NULL, "Finalizer Queue (Critical)"));
		MONO_PROFILER_RAISE (gc_root_register, (SPECIAL_ADDRESS_EPHEMERON, 1, MONO_ROOT_SOURCE_EPHEMERON, NULL, "Ephemerons"));
		MONO_PROFILER_RAISE (gc_root_register, (SPECIAL_ADDRESS_TOGGLEREF, 1, MONO_ROOT_SOURCE_TOGGLEREF, NULL, "ToggleRefs"));
	}

}

void
sgen_client_binary_protocol_collection_end (int minor_gc_count, int generation, long long num_objects_scanned, long long num_unique_objects_scanned)
{
	MONO_GC_END (generation);

	MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_END, generation, generation == GENERATION_OLD && sgen_concurrent_collection_in_progress));
}

#ifdef HOST_WASM
void
sgen_client_schedule_background_job (void (*cb)(void))
{
	mono_main_thread_schedule_background_job (cb);
}

#endif

#endif
