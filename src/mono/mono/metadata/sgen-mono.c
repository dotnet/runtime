/*
 * sgen-mono.c: SGen features specific to Mono.
 *
 * Copyright (C) 2014 Xamarin Inc
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

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"
#include "sgen/sgen-protocol.h"
#include "metadata/monitor.h"
#include "sgen/sgen-layout-stats.h"
#include "sgen/sgen-client.h"
#include "sgen/sgen-cardtable.h"
#include "sgen/sgen-pinning.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/abi-details.h"
#include "metadata/mono-gc.h"
#include "metadata/runtime.h"
#include "metadata/sgen-bridge-internals.h"
#include "metadata/gc-internals.h"
#include "metadata/handle.h"
#include "utils/mono-memory-model.h"
#include "utils/mono-logger-internals.h"

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
/* If set, check that there are no references to the domain left at domain unload */
gboolean sgen_mono_xdomain_checks = FALSE;

/* Functions supplied by the runtime to be called by the GC */
static MonoGCCallbacks gc_callbacks;

#ifdef HAVE_KW_THREAD
__thread SgenThreadInfo *sgen_thread_info;
#else
MonoNativeTlsKey thread_info_key;
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

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

	if (ptr >= stack_start && ptr < (gpointer)info->client_info.stack_end)
		return TRUE;
	return FALSE;
}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj) do {					\
		gpointer o = *(gpointer*)(ptr);				\
		if ((o)) {						\
			gpointer d = ((char*)dest) + ((char*)(ptr) - (char*)(obj)); \
			binary_protocol_wbarrier (d, o, (gpointer) SGEN_LOAD_VTABLE (o)); \
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
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	HEAVY_STAT (++stat_wbarrier_value_copy);
	g_assert (klass->valuetype);

	SGEN_LOG (8, "Adding value remset at %p, count %d, descr %p for class %s (%p)", dest, count, (gpointer)klass->gc_descr, klass->name, klass);

	if (sgen_ptr_in_nursery (dest) || ptr_on_stack (dest) || !sgen_gc_descr_has_references ((mword)klass->gc_descr)) {
		size_t element_size = mono_class_value_size (klass, NULL);
		size_t size = count * element_size;
		mono_gc_memmove_atomic (dest, src, size);		
		return;
	}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	if (binary_protocol_is_heavy_enabled ()) {
		size_t element_size = mono_class_value_size (klass, NULL);
		int i;
		for (i = 0; i < count; ++i) {
			scan_object_for_binary_protocol_copy_wbarrier ((char*)dest + i * element_size,
					(char*)src + i * element_size - sizeof (MonoObject),
					(mword) klass->gc_descr);
		}
	}
#endif

	sgen_get_remset ()->wbarrier_value_copy (dest, src, count, mono_class_value_size (klass, NULL));
}

/**
 * mono_gc_wbarrier_object_copy:
 *
 * Write barrier to call when obj is the result of a clone or copy of an object.
 */
void
mono_gc_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	int size;

	HEAVY_STAT (++stat_wbarrier_object_copy);

	if (sgen_ptr_in_nursery (obj) || ptr_on_stack (obj) || !SGEN_OBJECT_HAS_REFERENCES (src)) {
		size = mono_object_class (obj)->instance_size;
		mono_gc_memmove_aligned ((char*)obj + sizeof (MonoObject), (char*)src + sizeof (MonoObject),
				size - sizeof (MonoObject));
		return;	
	}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	if (binary_protocol_is_heavy_enabled ())
		scan_object_for_binary_protocol_copy_wbarrier (obj, (char*)src, (mword) src->vtable->gc_descr);
#endif

	sgen_get_remset ()->wbarrier_object_copy (obj, src);
}

void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	HEAVY_STAT (++stat_wbarrier_set_arrayref);
	if (sgen_ptr_in_nursery (slot_ptr)) {
		*(void**)slot_ptr = value;
		return;
	}
	SGEN_LOG (8, "Adding remset at %p", slot_ptr);
	if (value)
		binary_protocol_wbarrier (slot_ptr, value, value->vtable);

	sgen_get_remset ()->wbarrier_set_field ((GCObject*)arr, slot_ptr, value);
}

void
mono_gc_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	mono_gc_wbarrier_set_arrayref ((MonoArray*)obj, field_ptr, value);
}

void
mono_gc_wbarrier_value_copy_bitmap (gpointer _dest, gpointer _src, int size, unsigned bitmap)
{
	sgen_wbarrier_value_copy_bitmap (_dest, _src, size, bitmap);
}

static MonoMethod *write_barrier_conc_method;
static MonoMethod *write_barrier_noconc_method;

gboolean
sgen_is_critical_method (MonoMethod *method)
{
	return (method == write_barrier_conc_method || method == write_barrier_noconc_method || sgen_is_managed_allocator (method));
}

gboolean
sgen_has_critical_method (void)
{
	return write_barrier_conc_method || write_barrier_noconc_method || sgen_has_managed_allocator ();
}

#ifndef DISABLE_JIT

static void
emit_nursery_check (MonoMethodBuilder *mb, int *nursery_check_return_labels, gboolean is_concurrent)
{
	int shifted_nursery_start = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	memset (nursery_check_return_labels, 0, sizeof (int) * 2);
	// if (ptr_in_nursery (ptr)) return;
	/*
	 * Masking out the bits might be faster, but we would have to use 64 bit
	 * immediates, which might be slower.
	 */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_START);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_BITS);
	mono_mb_emit_byte (mb, CEE_SHR_UN);
	mono_mb_emit_stloc (mb, shifted_nursery_start);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_BITS);
	mono_mb_emit_byte (mb, CEE_SHR_UN);
	mono_mb_emit_ldloc (mb, shifted_nursery_start);
	nursery_check_return_labels [0] = mono_mb_emit_branch (mb, CEE_BEQ);

	if (!is_concurrent) {
		// if (!ptr_in_nursery (*ptr)) return;
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_BITS);
		mono_mb_emit_byte (mb, CEE_SHR_UN);
		mono_mb_emit_ldloc (mb, shifted_nursery_start);
		nursery_check_return_labels [1] = mono_mb_emit_branch (mb, CEE_BNE_UN);
	}
}
#endif

MonoMethod*
mono_gc_get_specific_write_barrier (gboolean is_concurrent)
{
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	MonoMethod **write_barrier_method_addr;
	WrapperInfo *info;
#ifdef MANAGED_WBARRIER
	int i, nursery_check_labels [2];
#endif

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
	sig->ret = &mono_defaults.void_class->byval_arg;
	sig->params [0] = &mono_defaults.int_class->byval_arg;

	if (is_concurrent)
		mb = mono_mb_new (mono_defaults.object_class, "wbarrier_conc", MONO_WRAPPER_WRITE_BARRIER);
	else
		mb = mono_mb_new (mono_defaults.object_class, "wbarrier_noconc", MONO_WRAPPER_WRITE_BARRIER);

#ifndef DISABLE_JIT
#ifdef MANAGED_WBARRIER
	emit_nursery_check (mb, nursery_check_labels, is_concurrent);
	/*
	addr = sgen_cardtable + ((address >> CARD_BITS) & CARD_MASK)
	*addr = 1;

	sgen_cardtable:
		LDC_PTR sgen_cardtable

	address >> CARD_BITS
		LDARG_0
		LDC_I4 CARD_BITS
		SHR_UN
	if (SGEN_HAVE_OVERLAPPING_CARDS) {
		LDC_PTR card_table_mask
		AND
	}
	AND
	ldc_i4_1
	stind_i1
	*/
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_CARD_TABLE);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, CARD_BITS);
	mono_mb_emit_byte (mb, CEE_SHR_UN);
	mono_mb_emit_byte (mb, CEE_CONV_I);
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
#if SIZEOF_VOID_P == 8
	mono_mb_emit_icon8 (mb, CARD_MASK);
#else
	mono_mb_emit_icon (mb, CARD_MASK);
#endif
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_AND);
#endif
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_icon (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_I1);

	// return;
	for (i = 0; i < 2; ++i) {
		if (nursery_check_labels [i])
			mono_mb_patch_branch (mb, nursery_check_labels [i]);
	}
	mono_mb_emit_byte (mb, CEE_RET);
#else
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icall (mb, mono_gc_wbarrier_generic_nostore);
	mono_mb_emit_byte (mb, CEE_RET);
#endif
#endif
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
	return mono_gc_get_specific_write_barrier (major_collector.is_concurrent);
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
		static MonoClass klass;
		static char _vtable[sizeof(MonoVTable)+8];
		MonoVTable* vtable = (MonoVTable*) ALIGN_TO((mword)_vtable, 8);
		gsize bmap;

		MonoDomain *domain = mono_get_root_domain ();
		g_assert (domain);

		klass.element_class = mono_defaults.byte_class;
		klass.rank = 1;
		klass.instance_size = MONO_SIZEOF_MONO_ARRAY;
		klass.sizes.element_size = 1;
		klass.name = "array_filler_type";

		vtable->klass = &klass;
		bmap = 0;
		vtable->gc_descr = mono_gc_make_descr_for_array (TRUE, &bmap, 0, 1);
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
	o->max_length = (mono_array_size_t)(size - MONO_SIZEOF_MONO_ARRAY);

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
	return res;
}

static gboolean
is_finalization_aware (MonoObject *obj)
{
	MonoVTable *vt = SGEN_LOAD_VTABLE (obj);
	return (vt->gc_bits & SGEN_GC_BIT_FINALIZER_AWARE) == SGEN_GC_BIT_FINALIZER_AWARE;
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

int
mono_gc_invoke_finalizers (void)
{
	return sgen_gc_invoke_finalizers ();
}

gboolean
mono_gc_pending_finalizers (void)
{
	return sgen_have_pending_finalizers ();
}

void
sgen_client_finalize_notify (void)
{
	mono_gc_finalize_notify ();
}

void
mono_gc_register_for_finalization (MonoObject *obj, void *user_data)
{
	sgen_object_register_for_finalization (obj, user_data);
}

static gboolean
object_in_domain_predicate (MonoObject *obj, void *user_data)
{
	MonoDomain *domain = (MonoDomain *)user_data;
	if (mono_object_domain (obj) == domain) {
		SGEN_LOG (5, "Unregistering finalizer for object: %p (%s)", obj, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (obj)));
		return TRUE;
	}
	return FALSE;
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
	return sgen_gather_finalizers_if (object_in_domain_predicate, domain, out_array, out_size);
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
static void
null_ephemerons_for_domain (MonoDomain *domain)
{
	EphemeronLinkNode *current = ephemeron_list, *prev = NULL;

	while (current) {
		MonoObject *object = (MonoObject*)current->array;

		if (object)
			SGEN_ASSERT (0, object->vtable, "Can't have objects without vtables.");

		if (object && object->vtable->domain == domain) {
			EphemeronLinkNode *tmp = current;

			if (prev)
				prev->next = current->next;
			else
				ephemeron_list = current->next;

			current = current->next;
			sgen_free_internal (tmp, INTERNAL_MEM_EPHEMERON_LINK);
		} else {
			prev = current;
			current = current->next;
		}
	}
}

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

		cur = mono_array_addr (array, Ephemeron, 0);
		array_end = cur + mono_array_length_fast (array);
		tombstone = SGEN_LOAD_VTABLE ((GCObject*)array)->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			GCObject *key = cur->key;

			if (!key || key == tombstone)
				continue;

			SGEN_LOG (5, "[%zd] key %p (%s) value %p (%s)", cur - mono_array_addr (array, Ephemeron, 0),
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

		cur = mono_array_addr (array, Ephemeron, 0);
		array_end = cur + mono_array_length_fast (array);
		tombstone = SGEN_LOAD_VTABLE ((GCObject*)array)->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			GCObject *key = cur->key;

			if (!key || key == tombstone)
				continue;

			SGEN_LOG (5, "[%zd] key %p (%s) value %p (%s)", cur - mono_array_addr (array, Ephemeron, 0),
				key, sgen_is_object_alive_for_current_gen (key) ? "reachable" : "unreachable",
				cur->value, cur->value && sgen_is_object_alive_for_current_gen (cur->value) ? "reachable" : "unreachable");

			if (sgen_is_object_alive_for_current_gen (key)) {
				GCObject *value = cur->value;

				copy_func (&cur->key, queue);
				if (value) {
					if (!sgen_is_object_alive_for_current_gen (value))
						nothing_marked = FALSE;
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
 * Appdomain handling
 */

void
mono_gc_set_current_thread_appdomain (MonoDomain *domain)
{
	SgenThreadInfo *info = mono_thread_info_current ();

	/* Could be called from sgen_thread_unregister () with a NULL info */
	if (domain) {
		g_assert (info);
		info->client_info.stopped_domain = domain;
	}
}

static gboolean
need_remove_object_for_domain (GCObject *start, MonoDomain *domain)
{
	if (mono_object_domain (start) == domain) {
		SGEN_LOG (4, "Need to cleanup object %p", start);
		binary_protocol_cleanup (start, (gpointer)SGEN_LOAD_VTABLE (start), sgen_safe_object_get_size ((GCObject*)start));
		return TRUE;
	}
	return FALSE;
}

static void
process_object_for_domain_clearing (GCObject *start, MonoDomain *domain)
{
	MonoVTable *vt = SGEN_LOAD_VTABLE (start);
	if (vt->klass == mono_defaults.internal_thread_class)
		g_assert (mono_object_domain (start) == mono_get_root_domain ());
	/* The object could be a proxy for an object in the domain
	   we're deleting. */
#ifndef DISABLE_REMOTING
	if (mono_defaults.real_proxy_class->supertypes && mono_class_has_parent_fast (vt->klass, mono_defaults.real_proxy_class)) {
		MonoObject *server = ((MonoRealProxy*)start)->unwrapped_server;

		/* The server could already have been zeroed out, so
		   we need to check for that, too. */
		if (server && (!SGEN_LOAD_VTABLE (server) || mono_object_domain (server) == domain)) {
			SGEN_LOG (4, "Cleaning up remote pointer in %p to object %p", start, server);
			((MonoRealProxy*)start)->unwrapped_server = NULL;
		}
	}
#endif
}

static gboolean
clear_domain_process_object (GCObject *obj, MonoDomain *domain)
{
	gboolean remove;

	process_object_for_domain_clearing (obj, domain);
	remove = need_remove_object_for_domain (obj, domain);

	if (remove && obj->synchronisation) {
		guint32 dislink = mono_monitor_get_object_monitor_gchandle (obj);
		if (dislink)
			mono_gchandle_free (dislink);
	}

	return remove;
}

static void
clear_domain_process_minor_object_callback (GCObject *obj, size_t size, MonoDomain *domain)
{
	if (clear_domain_process_object (obj, domain)) {
		CANARIFY_SIZE (size);
		memset (obj, 0, size);
	}
}

static void
clear_domain_process_major_object_callback (GCObject *obj, size_t size, MonoDomain *domain)
{
	clear_domain_process_object (obj, domain);
}

static void
clear_domain_free_major_non_pinned_object_callback (GCObject *obj, size_t size, MonoDomain *domain)
{
	if (need_remove_object_for_domain (obj, domain))
		major_collector.free_non_pinned_object (obj, size);
}

static void
clear_domain_free_major_pinned_object_callback (GCObject *obj, size_t size, MonoDomain *domain)
{
	if (need_remove_object_for_domain (obj, domain))
		major_collector.free_pinned_object (obj, size);
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
	LOSObject *bigobj, *prev;
	int i;

	LOCK_GC;

	binary_protocol_domain_unload_begin (domain);

	sgen_stop_world (0);

	if (sgen_concurrent_collection_in_progress ())
		sgen_perform_collection (0, GENERATION_OLD, "clear domain", TRUE);
	SGEN_ASSERT (0, !sgen_concurrent_collection_in_progress (), "We just ordered a synchronous collection.  Why are we collecting concurrently?");

	major_collector.finish_sweeping ();

	sgen_process_fin_stage_entries ();

	sgen_clear_nursery_fragments ();

	if (sgen_mono_xdomain_checks && domain != mono_get_root_domain ()) {
		sgen_scan_for_registered_roots_in_domain (domain, ROOT_TYPE_NORMAL);
		sgen_scan_for_registered_roots_in_domain (domain, ROOT_TYPE_WBARRIER);
		sgen_check_for_xdomain_refs ();
	}

	/*Ephemerons and dislinks must be processed before LOS since they might end up pointing
	to memory returned to the OS.*/
	null_ephemerons_for_domain (domain);
	sgen_null_links_for_domain (domain);

	for (i = GENERATION_NURSERY; i < GENERATION_MAX; ++i)
		sgen_remove_finalizers_if (object_in_domain_predicate, domain, i);

	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)clear_domain_process_minor_object_callback, domain, FALSE, TRUE);

	/* We need two passes over major and large objects because
	   freeing such objects might give their memory back to the OS
	   (in the case of large objects) or obliterate its vtable
	   (pinned objects with major-copying or pinned and non-pinned
	   objects with major-mark&sweep), but we might need to
	   dereference a pointer from an object to another object if
	   the first object is a proxy. */
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, (IterateObjectCallbackFunc)clear_domain_process_major_object_callback, domain);
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		clear_domain_process_object ((GCObject*)bigobj->data, domain);

	prev = NULL;
	for (bigobj = los_object_list; bigobj;) {
		if (need_remove_object_for_domain ((GCObject*)bigobj->data, domain)) {
			LOSObject *to_free = bigobj;
			if (prev)
				prev->next = bigobj->next;
			else
				los_object_list = bigobj->next;
			bigobj = bigobj->next;
			SGEN_LOG (4, "Freeing large object %p", bigobj->data);
			sgen_los_free_object (to_free);
			continue;
		}
		prev = bigobj;
		bigobj = bigobj->next;
	}
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_NON_PINNED, (IterateObjectCallbackFunc)clear_domain_free_major_non_pinned_object_callback, domain);
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_PINNED, (IterateObjectCallbackFunc)clear_domain_free_major_pinned_object_callback, domain);

	if (domain == mono_get_root_domain ()) {
		sgen_pin_stats_print_class_stats ();
		sgen_object_layout_dump (stdout);
	}

	sgen_restart_world (0, NULL);

	binary_protocol_domain_unload_end (domain);
	binary_protocol_flush_buffers (FALSE);

	UNLOCK_GC;
}

/*
 * Allocation
 */

static gboolean alloc_events = FALSE;

void
mono_gc_enable_alloc_events (void)
{
	alloc_events = TRUE;
}

void*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = sgen_alloc_obj (vtable, size);

	if (G_UNLIKELY (alloc_events)) {
		if (obj)
			mono_profiler_allocation (obj);
	}

	return obj;
}

void*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = sgen_alloc_obj_pinned (vtable, size);

	if (G_UNLIKELY (alloc_events)) {
		if (obj)
			mono_profiler_allocation (obj);
	}

	return obj;
}

void*
mono_gc_alloc_mature (MonoVTable *vtable, size_t size)
{
	MonoObject *obj = sgen_alloc_obj_mature (vtable, size);

	if (G_UNLIKELY (alloc_events)) {
		if (obj)
			mono_profiler_allocation (obj);
	}

	return obj;
}

void*
mono_gc_alloc_fixed (size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg)
{
	/* FIXME: do a single allocation */
	void *res = calloc (1, size);
	if (!res)
		return NULL;
	if (!mono_gc_register_root ((char *)res, size, descr, source, msg)) {
		free (res);
		res = NULL;
	}
	return res;
}

void
mono_gc_free_fixed (void* addr)
{
	mono_gc_deregister_root ((char *)addr);
	free (addr);
}

/*
 * Managed allocator
 */

static MonoMethod* alloc_method_cache [ATYPE_NUM];
static MonoMethod* slowpath_alloc_method_cache [ATYPE_NUM];
static gboolean use_managed_allocator = TRUE;

#ifdef MANAGED_ALLOCATION

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

#if defined(TARGET_OSX) || defined(TARGET_WIN32) || defined(TARGET_ANDROID) || defined(TARGET_IOS)
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

/* FIXME: Do this in the JIT, where specialized allocation sequences can be created
 * for each class. This is currently not easy to do, as it is hard to generate basic 
 * blocks + branches, but it is easy with the linear IL codebase.
 *
 * For this to work we'd need to solve the TLAB race, first.  Now we
 * require the allocator to be in a few known methods to make sure
 * that they are executed atomically via the restart mechanism.
 */
static MonoMethod*
create_allocator (int atype, gboolean slowpath)
{
	int p_var, size_var;
	guint32 slowpath_branch, max_size_branch;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	static gboolean registered = FALSE;
	int tlab_next_addr_var, new_next_var;
	const char *name = NULL;
	WrapperInfo *info;
	int num_params, i;

	if (!registered) {
		mono_register_jit_icall (mono_gc_alloc_obj, "mono_gc_alloc_obj", mono_create_icall_signature ("object ptr int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_vector, "mono_gc_alloc_vector", mono_create_icall_signature ("object ptr int int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_string, "mono_gc_alloc_string", mono_create_icall_signature ("object ptr int int32"), FALSE);
		registered = TRUE;
	}

	if (atype == ATYPE_SMALL) {
		name = slowpath ? "SlowAllocSmall" : "AllocSmall";
	} else if (atype == ATYPE_NORMAL) {
		name = slowpath ? "SlowAlloc" : "Alloc";
	} else if (atype == ATYPE_VECTOR) {
		name = slowpath ? "SlowAllocVector" : "AllocVector";
	} else if (atype == ATYPE_STRING) {
		name = slowpath ? "SlowAllocString" : "AllocString";
	} else {
		g_assert_not_reached ();
	}

	if (atype == ATYPE_NORMAL)
		num_params = 1;
	else
		num_params = 2;

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, num_params);
	if (atype == ATYPE_STRING) {
		csig->ret = &mono_defaults.string_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int32_class->byval_arg;
	} else {
		csig->ret = &mono_defaults.object_class->byval_arg;
		for (i = 0; i < num_params; i++)
			csig->params [i] = &mono_defaults.int_class->byval_arg;
	}

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ALLOC);

#ifndef DISABLE_JIT
	if (slowpath) {
		switch (atype) {
		case ATYPE_NORMAL:
		case ATYPE_SMALL:
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_icall (mb, ves_icall_object_new_specific);
			break;
		case ATYPE_VECTOR:
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_icall (mb, ves_icall_array_new_specific);
			break;
		case ATYPE_STRING:
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_icall (mb, ves_icall_string_alloc);
			break;
		default:
			g_assert_not_reached ();
		}

		goto done;
	}

	size_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	if (atype == ATYPE_SMALL) {
		/* size_var = size_arg */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_NORMAL) {
		/* size = vtable->klass->instance_size; */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoClass, instance_size));
		mono_mb_emit_byte (mb, CEE_ADD);
		/* FIXME: assert instance_size stays a 4 byte integer */
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_VECTOR) {
		MonoExceptionClause *clause;
		int pos, pos_leave, pos_error;
		MonoClass *oom_exc_class;
		MonoMethod *ctor;

		/*
		 * n > MONO_ARRAY_MAX_INDEX => OutOfMemoryException
		 * n < 0                    => OverflowException
		 *
		 * We can do an unsigned comparison to catch both cases, then in the error
		 * case compare signed to distinguish between them.
		 */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, MONO_ARRAY_MAX_INDEX);
		mono_mb_emit_byte (mb, CEE_CONV_U);
		pos = mono_mb_emit_short_branch (mb, CEE_BLE_UN_S);

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, 0);
		pos_error = mono_mb_emit_short_branch (mb, CEE_BLT_S);
		mono_mb_emit_exception (mb, "OutOfMemoryException", NULL);
		mono_mb_patch_short_branch (mb, pos_error);
		mono_mb_emit_exception (mb, "OverflowException", NULL);

		mono_mb_patch_short_branch (mb, pos);

		clause = (MonoExceptionClause *)mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
		clause->try_offset = mono_mb_get_label (mb);

		/* vtable->klass->sizes.element_size */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoClass, sizes));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_byte (mb, CEE_CONV_I);

		/* * n */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_MUL_OVF_UN);
		/* + sizeof (MonoArray) */
		mono_mb_emit_icon (mb, MONO_SIZEOF_MONO_ARRAY);
		mono_mb_emit_byte (mb, CEE_ADD_OVF_UN);
		mono_mb_emit_stloc (mb, size_var);

		pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

		/* catch */
		clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
		clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
		clause->data.catch_class = mono_class_load_from_name (mono_defaults.corlib,
				"System", "OverflowException");
		clause->handler_offset = mono_mb_get_label (mb);

		oom_exc_class = mono_class_load_from_name (mono_defaults.corlib,
				"System", "OutOfMemoryException");
		ctor = mono_class_get_method_from_name (oom_exc_class, ".ctor", 0);
		g_assert (ctor);

		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
		mono_mb_emit_byte (mb, CEE_THROW);

		clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;
		mono_mb_set_clauses (mb, 1, clause);
		mono_mb_patch_branch (mb, pos_leave);
		/* end catch */
	} else if (atype == ATYPE_STRING) {
		int pos;

		/*
		 * a string allocator method takes the args: (vtable, len)
		 *
		 * bytes = offsetof (MonoString, chars) + ((len + 1) * 2)
		 *
		 * condition:
		 *
		 * bytes <= INT32_MAX - (SGEN_ALLOC_ALIGN - 1)
		 *
		 * therefore:
		 *
		 * offsetof (MonoString, chars) + ((len + 1) * 2) <= INT32_MAX - (SGEN_ALLOC_ALIGN - 1)
		 * len <= (INT32_MAX - (SGEN_ALLOC_ALIGN - 1) - offsetof (MonoString, chars)) / 2 - 1
		 */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, (INT32_MAX - (SGEN_ALLOC_ALIGN - 1) - MONO_STRUCT_OFFSET (MonoString, chars)) / 2 - 1);
		pos = mono_mb_emit_short_branch (mb, MONO_CEE_BLE_UN_S);

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);
		mono_mb_emit_exception (mb, "OutOfMemoryException", NULL);
		mono_mb_patch_short_branch (mb, pos);

		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_SHL);
		//WE manually fold the above + 2 here
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoString, chars) + 2);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, size_var);
	} else {
		g_assert_not_reached ();
	}

	if (atype != ATYPE_SMALL) {
		/* size += ALLOC_ALIGN - 1; */
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, SGEN_ALLOC_ALIGN - 1);
		mono_mb_emit_byte (mb, CEE_ADD);
		/* size &= ~(ALLOC_ALIGN - 1); */
		mono_mb_emit_icon (mb, ~(SGEN_ALLOC_ALIGN - 1));
		mono_mb_emit_byte (mb, CEE_AND);
		mono_mb_emit_stloc (mb, size_var);
	}

	/* if (size > MAX_SMALL_OBJ_SIZE) goto slowpath */
	if (atype != ATYPE_SMALL) {
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, SGEN_MAX_SMALL_OBJ_SIZE);
		max_size_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BGT_UN_S);
	}

	/*
	 * We need to modify tlab_next, but the JIT only supports reading, so we read
	 * another tls var holding its address instead.
	 */

	/* tlab_next_addr (local) = tlab_next_addr (TLS var) */
	tlab_next_addr_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	EMIT_TLS_ACCESS_NEXT_ADDR (mb);
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

	/* if (G_LIKELY (new_next < tlab_temp_end)) */
	mono_mb_emit_ldloc (mb, new_next_var);
	EMIT_TLS_ACCESS_TEMP_END (mb);
	slowpath_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLT_UN_S);

	/* Slowpath */
	if (atype != ATYPE_SMALL)
		mono_mb_patch_short_branch (mb, max_size_branch);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	/* FIXME: mono_gc_alloc_obj takes a 'size_t' as an argument, not an int32 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, size_var);
	if (atype == ATYPE_NORMAL || atype == ATYPE_SMALL) {
		mono_mb_emit_icall (mb, mono_gc_alloc_obj);
	} else if (atype == ATYPE_VECTOR) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_vector);
	} else if (atype == ATYPE_STRING) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_string);
	} else {
		g_assert_not_reached ();
	}
	mono_mb_emit_byte (mb, CEE_RET);

	/* Fastpath */
	mono_mb_patch_short_branch (mb, slowpath_branch);

	/* FIXME: Memory barrier */

	/* tlab_next = new_next */
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_ldloc (mb, new_next_var);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/*The tlab store must be visible before the the vtable store. This could be replaced with a DDS but doing it with IL would be tricky. */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_MEMORY_BARRIER);
	mono_mb_emit_i4 (mb, MONO_MEMORY_BARRIER_REL);

	/* *p = vtable; */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	if (atype == ATYPE_VECTOR) {
		/* arr->max_length = max_length; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, max_length));
		mono_mb_emit_ldarg (mb, 1);
#ifdef MONO_BIG_ARRAYS
		mono_mb_emit_byte (mb, CEE_STIND_I);
#else
		mono_mb_emit_byte (mb, CEE_STIND_I4);
#endif
	} else 	if (atype == ATYPE_STRING) {
		/* need to set length and clear the last char */
		/* s->length = len; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoString, length));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I4);
		/* s->chars [len] = 0; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, 2);
		mono_mb_emit_byte (mb, MONO_CEE_SUB);
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I2);
	}

	/*
	We must make sure both vtable and max_length are globaly visible before returning to managed land.
	*/
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_MEMORY_BARRIER);
	mono_mb_emit_i4 (mb, MONO_MEMORY_BARRIER_REL);

	/* return p */
	mono_mb_emit_ldloc (mb, p_var);

 done:
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.alloc.gc_name = "sgen";
	info->d.alloc.alloc_type = atype;

	res = mono_mb_create (mb, csig, 8, info);
	mono_mb_free (mb);
#ifndef DISABLE_JIT
	mono_method_get_header (res)->init_locals = FALSE;
#endif


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
	if (collect_before_allocs)
		return NULL;
	if (!mono_runtime_has_tls_get ())
		return NULL;
	if (klass->instance_size > tlab_size)
		return NULL;
	if (known_instance_size && ALIGN_TO (klass->instance_size, SGEN_ALLOC_ALIGN) >= SGEN_MAX_SMALL_OBJ_SIZE)
		return NULL;
	if (mono_class_has_finalizer (klass) || mono_class_is_marshalbyref (klass) || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank)
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING)
		return mono_gc_get_managed_allocator_by_type (ATYPE_STRING, FALSE);
	/* Generic classes have dynamic field and can go above MAX_SMALL_OBJ_SIZE. */
	if (known_instance_size)
		return mono_gc_get_managed_allocator_by_type (ATYPE_SMALL, FALSE);
	else
		return mono_gc_get_managed_allocator_by_type (ATYPE_NORMAL, FALSE);
#else
	return NULL;
#endif
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoClass *klass)
{
#ifdef MANAGED_ALLOCATION
	if (klass->rank != 1)
		return NULL;
	if (!mono_runtime_has_tls_get ())
		return NULL;
	if (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS)
		return NULL;
	if (has_per_allocation_action)
		return NULL;
	g_assert (!mono_class_has_finalizer (klass) && !mono_class_is_marshalbyref (klass));

	return mono_gc_get_managed_allocator_by_type (ATYPE_VECTOR, FALSE);
#else
	return NULL;
#endif
}

void
sgen_set_use_managed_allocator (gboolean flag)
{
	use_managed_allocator = flag;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype, gboolean slowpath)
{
#ifdef MANAGED_ALLOCATION
	MonoMethod *res;
	MonoMethod **cache = slowpath ? slowpath_alloc_method_cache : alloc_method_cache;

	if (!use_managed_allocator)
		return NULL;

	if (!mono_runtime_has_tls_get ())
		return NULL;

	res = cache [atype];
	if (res)
		return res;

	res = create_allocator (atype, slowpath);
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
		if (method == alloc_method_cache [i] || method == slowpath_alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

gboolean
sgen_has_managed_allocator (void)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (alloc_method_cache [i] || slowpath_alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

/*
 * Cardtable scanning
 */

#define MWORD_MASK (sizeof (mword) - 1)

static inline int
find_card_offset (mword card)
{
/*XXX Use assembly as this generates some pretty bad code */
#if defined(__i386__) && defined(__GNUC__)
	return  (__builtin_ffs (card) - 1) / 8;
#elif defined(__x86_64__) && defined(__GNUC__)
	return (__builtin_ffsll (card) - 1) / 8;
#elif defined(__s390x__)
	return (__builtin_ffsll (GUINT64_TO_LE(card)) - 1) / 8;
#else
	int i;
	guint8 *ptr = (guint8 *) &card;
	for (i = 0; i < sizeof (mword); ++i) {
		if (ptr[i])
			return i;
	}
	return 0;
#endif
}

static guint8*
find_next_card (guint8 *card_data, guint8 *end)
{
	mword *cards, *cards_end;
	mword card;

	while ((((mword)card_data) & MWORD_MASK) && card_data < end) {
		if (*card_data)
			return card_data;
		++card_data;
	}

	if (card_data == end)
		return end;

	cards = (mword*)card_data;
	cards_end = (mword*)((mword)end & ~MWORD_MASK);
	while (cards < cards_end) {
		card = *cards;
		if (card)
			return (guint8*)cards + find_card_offset (card);
		++cards;
	}

	card_data = (guint8*)cards_end;
	while (card_data < end) {
		if (*card_data)
			return card_data;
		++card_data;
	}

	return end;
}

#define ARRAY_OBJ_INDEX(ptr,array,elem_size) (((char*)(ptr) - ((char*)(array) + G_STRUCT_OFFSET (MonoArray, vector))) / (elem_size))

gboolean
sgen_client_cardtable_scan_object (GCObject *obj, mword block_obj_size, guint8 *cards, gboolean mod_union, ScanCopyContext ctx)
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

		mword desc = (mword)klass->element_class->gc_descr;
		int elem_size = mono_array_element_size (klass);

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		guint8 *overflow_scan_end = NULL;
#endif

#ifdef SGEN_OBJECT_LAYOUT_STATISTICS
		if (klass->element_class->valuetype)
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
		card_data_end = card_data + card_count;


#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		/*Check for overflow and if so, setup to scan in two steps*/
		if (!cards && card_data_end >= SGEN_SHADOW_CARDTABLE_END) {
			overflow_scan_end = sgen_shadow_cardtable + (card_data_end - SGEN_SHADOW_CARDTABLE_END);
			card_data_end = SGEN_SHADOW_CARDTABLE_END;
		}

LOOP_HEAD:
#endif

		card_data = find_next_card (card_data, card_data_end);
		for (; card_data < card_data_end; card_data = find_next_card (card_data + 1, card_data_end)) {
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
			if (klass->element_class->valuetype) {
				ScanVTypeFunc scan_vtype_func = ctx.ops->scan_vtype;

				for (; elem < card_end; elem += elem_size)
					scan_vtype_func (obj, elem, desc, ctx.queue BINARY_PROTOCOL_ARG (elem_size));
			} else {
				CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;

				HEAVY_STAT (++los_array_cards);
				for (; elem < card_end; elem += SIZEOF_VOID_P) {
					GCObject *new_;
					gpointer old = *(gpointer*)elem;
					if ((mod_union && old) || G_UNLIKELY (sgen_ptr_in_nursery (old))) {
						HEAVY_STAT (++los_array_remsets);
						copy_func ((GCObject**)elem, ctx.queue);
						new_ = *(GCObject **)elem;
						if (G_UNLIKELY (sgen_ptr_in_nursery (new_)))
							sgen_add_to_global_remset (elem, new_);
					}
				}
			}

			binary_protocol_card_scan (first_elem, elem - first_elem);
		}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		if (overflow_scan_end) {
			extra_idx = card_data - card_base;
			card_base = card_data = sgen_shadow_cardtable;
			card_data_end = overflow_scan_end;
			overflow_scan_end = NULL;
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

void*
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
	if (G_UNLIKELY (alloc_events))
		mono_profiler_allocation (&arr->obj);

	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size (vtable, (GCObject*)arr)), "Vector has incorrect size.");
	return arr;
}

void*
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
	if (G_UNLIKELY (alloc_events))
		mono_profiler_allocation (&arr->obj);

	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size (vtable, (GCObject*)arr)), "Array has incorrect size.");
	return arr;
}

void*
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
	if (G_UNLIKELY (alloc_events))
		mono_profiler_allocation (&str->object);

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

	if (nursery_canaries_enabled () && sgen_ptr_in_nursery (str)) {
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
typedef struct {
	int count;		/* must be the first field */
	void *objects [GC_ROOT_NUM];
	int root_types [GC_ROOT_NUM];
	uintptr_t extra_info [GC_ROOT_NUM];
} GCRootReport;

static void
notify_gc_roots (GCRootReport *report)
{
	if (!report->count)
		return;
	mono_profiler_gc_roots (report->count, report->objects, report->root_types, report->extra_info);
	report->count = 0;
}

static void
add_profile_gc_root (GCRootReport *report, void *object, int rtype, uintptr_t extra_info)
{
	if (report->count == GC_ROOT_NUM)
		notify_gc_roots (report);
	report->objects [report->count] = object;
	report->root_types [report->count] = rtype;
	report->extra_info [report->count++] = (uintptr_t)SGEN_LOAD_VTABLE (object)->klass;
}

void
sgen_client_nursery_objects_pinned (void **definitely_pinned, int count)
{
	if (mono_profiler_get_events () & MONO_PROFILE_GC_ROOTS) {
		GCRootReport report;
		int idx;
		report.count = 0;
		for (idx = 0; idx < count; ++idx)
			add_profile_gc_root (&report, definitely_pinned [idx], MONO_PROFILE_GC_ROOT_PINNING | MONO_PROFILE_GC_ROOT_MISC, 0);
		notify_gc_roots (&report);
	}
}

static void
report_finalizer_roots_from_queue (SgenPointerQueue *queue)
{
	GCRootReport report;
	size_t i;

	report.count = 0;
	for (i = 0; i < queue->next_slot; ++i) {
		void *obj = queue->data [i];
		if (!obj)
			continue;
		add_profile_gc_root (&report, obj, MONO_PROFILE_GC_ROOT_FINALIZER, 0);
	}
	notify_gc_roots (&report);
}

static void
report_finalizer_roots (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue)
{
	report_finalizer_roots_from_queue (fin_ready_queue);
	report_finalizer_roots_from_queue (critical_fin_queue);
}

static GCRootReport *root_report;

static void
single_arg_report_root (MonoObject **obj, void *gc_data)
{
	if (*obj)
		add_profile_gc_root (root_report, *obj, MONO_PROFILE_GC_ROOT_OTHER, 0);
}

static void
precisely_report_roots_from (GCRootReport *report, void** start_root, void** end_root, mword desc)
{
	switch (desc & ROOT_DESC_TYPE_MASK) {
	case ROOT_DESC_BITMAP:
		desc >>= ROOT_DESC_TYPE_SHIFT;
		while (desc) {
			if ((desc & 1) && *start_root) {
				add_profile_gc_root (report, *start_root, MONO_PROFILE_GC_ROOT_OTHER, 0);
			}
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
				if ((bmap & 1) && *objptr) {
					add_profile_gc_root (report, *objptr, MONO_PROFILE_GC_ROOT_OTHER, 0);
				}
				bmap >>= 1;
				++objptr;
			}
			start_run += GC_BITS_PER_WORD;
		}
		break;
	}
	case ROOT_DESC_USER: {
		MonoGCRootMarkFunc marker = (MonoGCRootMarkFunc)sgen_get_user_descriptor_func (desc);
		root_report = report;
		marker ((MonoObject**)start_root, single_arg_report_root, NULL);
		break;
	}
	case ROOT_DESC_RUN_LEN:
		g_assert_not_reached ();
	default:
		g_assert_not_reached ();
	}
}

static void
report_registered_roots_by_type (int root_type)
{
	GCRootReport report;
	void **start_root;
	RootRecord *root;
	report.count = 0;
	SGEN_HASH_TABLE_FOREACH (&roots_hash [root_type], void **, start_root, RootRecord *, root) {
		SGEN_LOG (6, "Precise root scan %p-%p (desc: %p)", start_root, root->end_root, (void*)root->root_desc);
		precisely_report_roots_from (&report, start_root, (void**)root->end_root, root->root_desc);
	} SGEN_HASH_TABLE_FOREACH_END;
	notify_gc_roots (&report);
}

static void
report_registered_roots (void)
{
	report_registered_roots_by_type (ROOT_TYPE_NORMAL);
	report_registered_roots_by_type (ROOT_TYPE_WBARRIER);
}

void
sgen_client_collecting_minor (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue)
{
	if (mono_profiler_get_events () & MONO_PROFILE_GC_ROOTS)
		report_registered_roots ();
	if (mono_profiler_get_events () & MONO_PROFILE_GC_ROOTS)
		report_finalizer_roots (fin_ready_queue, critical_fin_queue);
}

static GCRootReport major_root_report;
static gboolean profile_roots;

void
sgen_client_collecting_major_1 (void)
{
	profile_roots = mono_profiler_get_events () & MONO_PROFILE_GC_ROOTS;
	memset (&major_root_report, 0, sizeof (GCRootReport));
}

void
sgen_client_pinned_los_object (GCObject *obj)
{
	if (profile_roots)
		add_profile_gc_root (&major_root_report, (char*)obj, MONO_PROFILE_GC_ROOT_PINNING | MONO_PROFILE_GC_ROOT_MISC, 0);
}

void
sgen_client_collecting_major_2 (void)
{
	if (profile_roots)
		notify_gc_roots (&major_root_report);

	if (mono_profiler_get_events () & MONO_PROFILE_GC_ROOTS)
		report_registered_roots ();
}

void
sgen_client_collecting_major_3 (SgenPointerQueue *fin_ready_queue, SgenPointerQueue *critical_fin_queue)
{
	if (mono_profiler_get_events () & MONO_PROFILE_GC_ROOTS)
		report_finalizer_roots (fin_ready_queue, critical_fin_queue);
}

#define MOVED_OBJECTS_NUM 64
static void *moved_objects [MOVED_OBJECTS_NUM];
static int moved_objects_idx = 0;

void
mono_sgen_register_moved_object (void *obj, void *destination)
{
	g_assert (mono_profiler_events & MONO_PROFILE_GC_MOVES);

	if (moved_objects_idx == MOVED_OBJECTS_NUM) {
		mono_profiler_gc_moves (moved_objects, moved_objects_idx);
		moved_objects_idx = 0;
	}
	moved_objects [moved_objects_idx++] = obj;
	moved_objects [moved_objects_idx++] = destination;
}

void
mono_sgen_gc_event_moves (void)
{
	if (moved_objects_idx) {
		mono_profiler_gc_moves (moved_objects, moved_objects_idx);
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
 * @flags: flags for future use
 * @callback: a function pointer called for each object in the heap
 * @data: a user data pointer that is passed to callback
 *
 * This function can be used to iterate over all the live objects in the heap:
 * for each object, @callback is invoked, providing info about the object's
 * location in memory, its class, its size and the objects it references.
 * For each referenced object it's offset from the object address is
 * reported in the offsets array.
 * The object references may be buffered, so the callback may be invoked
 * multiple times for the same object: in all but the first call, the size
 * argument will be zero.
 * Note that this function can be only called in the #MONO_GC_EVENT_PRE_START_WORLD
 * profiler event handler.
 *
 * Returns: a non-zero value if the GC doesn't support heap walking
 */
int
mono_gc_walk_heap (int flags, MonoGCReferences callback, void *data)
{
	HeapWalkInfo hwi;

	hwi.flags = flags;
	hwi.callback = callback;
	hwi.data = data;

	sgen_clear_nursery_fragments ();
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data, walk_references, &hwi, FALSE, TRUE);

	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, walk_references, &hwi);
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
mono_gc_get_gc_callbacks ()
{
	return &gc_callbacks;
}

void
sgen_client_thread_register (SgenThreadInfo* info, void *stack_bottom_fallback)
{
	size_t stsize = 0;
	guint8 *staddr = NULL;

#ifndef HAVE_KW_THREAD
	g_assert (!mono_native_tls_get_value (thread_info_key));
	mono_native_tls_set_value (thread_info_key, info);
#else
	sgen_thread_info = info;
#endif

	info->client_info.skip = 0;
	info->client_info.stopped_ip = NULL;
	info->client_info.stopped_domain = NULL;

	info->client_info.stack_start = NULL;

#ifdef SGEN_POSIX_STW
	info->client_info.stop_count = -1;
	info->client_info.signal = 0;
#endif

	/* On win32, stack_start_limit should be 0, since the stack can grow dynamically */
	mono_thread_info_get_stack_bounds (&staddr, &stsize);
	if (staddr) {
#ifndef HOST_WIN32
		info->client_info.stack_start_limit = staddr;
#endif
		info->client_info.stack_end = staddr + stsize;
	} else {
		gsize stack_bottom = (gsize)stack_bottom_fallback;
		stack_bottom += 4095;
		stack_bottom &= ~4095;
		info->client_info.stack_end = (char*)stack_bottom;
	}

#ifdef USE_MONO_CTX
	memset (&info->client_info.ctx, 0, sizeof (MonoContext));
#else
	memset (&info->client_info.regs, 0, sizeof (info->client_info.regs));
#endif

	if (mono_gc_get_gc_callbacks ()->thread_attach_func)
		info->client_info.runtime_data = mono_gc_get_gc_callbacks ()->thread_attach_func ();

	binary_protocol_thread_register ((gpointer)mono_thread_info_get_tid (info));

	SGEN_LOG (3, "registered thread %p (%p) stack end %p", info, (gpointer)mono_thread_info_get_tid (info), info->client_info.stack_end);
}

void
sgen_client_thread_unregister (SgenThreadInfo *p)
{
	MonoNativeThreadId tid;

#ifndef HAVE_KW_THREAD
	mono_native_tls_set_value (thread_info_key, NULL);
#else
	sgen_thread_info = NULL;
#endif

	tid = mono_thread_info_get_tid (p);

	if (p->client_info.info.runtime_thread)
		mono_threads_add_joinable_thread ((gpointer)tid);

	if (mono_gc_get_gc_callbacks ()->thread_detach_func) {
		mono_gc_get_gc_callbacks ()->thread_detach_func (p->client_info.runtime_data);
		p->client_info.runtime_data = NULL;
	}

	binary_protocol_thread_unregister ((gpointer)tid);
	SGEN_LOG (3, "unregister thread %p (%p)", p, (gpointer)tid);
}

void
mono_gc_set_skip_thread (gboolean skip)
{
	SgenThreadInfo *info = mono_thread_info_current ();

	LOCK_GC;
	info->client_info.gc_disabled = skip;
	UNLOCK_GC;
}

static gboolean
is_critical_method (MonoMethod *method)
{
	return mono_runtime_is_critical_method (method) || sgen_is_critical_method (method);
}

static gboolean
thread_in_critical_region (SgenThreadInfo *info)
{
	return info->client_info.in_critical_region;
}

static void
sgen_thread_attach (SgenThreadInfo *info)
{
	mono_handle_arena_init ((MonoHandleArena**) &info->client_info.info.handle_arena);

	if (mono_gc_get_gc_callbacks ()->thread_attach_func && !info->client_info.runtime_data)
		info->client_info.runtime_data = mono_gc_get_gc_callbacks ()->thread_attach_func ();
}

static void
sgen_thread_detach (SgenThreadInfo *p)
{
	/* If a delegate is passed to native code and invoked on a thread we dont
	 * know about, the jit will register it with mono_jit_thread_attach, but
	 * we have no way of knowing when that thread goes away.  SGen has a TSD
	 * so we assume that if the domain is still registered, we can detach
	 * the thread
	 */
	if (mono_domain_get ())
		mono_thread_detach_internal (mono_thread_internal_current ());

	mono_handle_arena_cleanup ((MonoHandleArena**) &p->client_info.info.handle_arena);
}

gboolean
mono_gc_register_thread (void *baseptr)
{
	return mono_thread_info_attach (baseptr) != NULL;
}

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

/*
 * Mark from thread stacks and registers.
 */
void
sgen_client_scan_thread_data (void *start_nursery, void *end_nursery, gboolean precise, ScanCopyContext ctx)
{
	SgenThreadInfo *info;

	scan_area_arg_start = start_nursery;
	scan_area_arg_end = end_nursery;

	FOREACH_THREAD (info) {
		int skip_reason = 0;
		void *aligned_stack_start = (void*)(mword) ALIGN_TO ((mword)info->client_info.stack_start, SIZEOF_VOID_P);

		if (info->client_info.skip) {
			SGEN_LOG (3, "Skipping dead thread %p, range: %p-%p, size: %zd", info, info->client_info.stack_start, info->client_info.stack_end, (char*)info->client_info.stack_end - (char*)info->client_info.stack_start);
			skip_reason = 1;
		} else if (info->client_info.gc_disabled) {
			SGEN_LOG (3, "GC disabled for thread %p, range: %p-%p, size: %zd", info, info->client_info.stack_start, info->client_info.stack_end, (char*)info->client_info.stack_end - (char*)info->client_info.stack_start);
			skip_reason = 2;
		} else if (!mono_thread_info_is_live (info)) {
			SGEN_LOG (3, "Skipping non-running thread %p, range: %p-%p, size: %zd (state %x)", info, info->client_info.stack_start, info->client_info.stack_end, (char*)info->client_info.stack_end - (char*)info->client_info.stack_start, info->client_info.info.thread_state);
			skip_reason = 3;
		}

		binary_protocol_scan_stack ((gpointer)mono_thread_info_get_tid (info), info->client_info.stack_start, info->client_info.stack_end, skip_reason);

		if (skip_reason)
			continue;

		g_assert (info->client_info.suspend_done);
		SGEN_LOG (3, "Scanning thread %p, range: %p-%p, size: %zd, pinned=%zd", info, info->client_info.stack_start, info->client_info.stack_end, (char*)info->client_info.stack_end - (char*)info->client_info.stack_start, sgen_get_pinned_count ());
		if (mono_gc_get_gc_callbacks ()->thread_mark_func && !conservative_stack_mark) {
			mono_gc_get_gc_callbacks ()->thread_mark_func (info->client_info.runtime_data, (guint8 *)aligned_stack_start, (guint8 *)info->client_info.stack_end, precise, &ctx);
		} else if (!precise) {
			if (!conservative_stack_mark) {
				fprintf (stderr, "Precise stack mark not supported - disabling.\n");
				conservative_stack_mark = TRUE;
			}
			sgen_conservatively_pin_objects_from ((void **)aligned_stack_start, (void **)info->client_info.stack_end, start_nursery, end_nursery, PIN_TYPE_STACK);
		}

		if (!precise) {
#ifdef USE_MONO_CTX
			sgen_conservatively_pin_objects_from ((void**)&info->client_info.ctx, (void**)(&info->client_info.ctx + 1),
				start_nursery, end_nursery, PIN_TYPE_STACK);
#else
			sgen_conservatively_pin_objects_from ((void**)&info->client_info.regs, (void**)&info->client_info.regs + ARCH_NUM_REGS,
					start_nursery, end_nursery, PIN_TYPE_STACK);
#endif
			{
				// This is used on Coop GC for platforms where we cannot get the data for individual registers.
				// We force a spill of all registers into the stack and pass a chunk of data into sgen.
				MonoThreadUnwindState *state = &info->client_info.info.thread_saved_state [SELF_SUSPEND_STATE_INDEX];
				if (state && state->gc_stackdata) {
					sgen_conservatively_pin_objects_from ((void **)state->gc_stackdata, (void**)((char*)state->gc_stackdata + state->gc_stackdata_size),
						start_nursery, end_nursery, PIN_TYPE_STACK);
				}
			}
		}
	} END_FOREACH_THREAD
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
		SGEN_ASSERT (0, stack_end < info->client_info.stack_end, "Can only lower stack end");
		info->client_info.stack_end = stack_end;
	}
	UNLOCK_GC;
}

/*
 * Roots
 */

int
mono_gc_register_root (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg)
{
	return sgen_register_root (start, size, descr, descr ? ROOT_TYPE_NORMAL : ROOT_TYPE_PINNED, source, msg);
}

int
mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg)
{
	return sgen_register_root (start, size, descr, ROOT_TYPE_WBARRIER, source, msg);
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
	return pthread_create (new_thread, attr, start_routine, arg);
}
#endif

/*
 * Miscellaneous
 */

void
sgen_client_total_allocated_heap_changed (size_t allocated_heap)
{
	mono_runtime_resource_check_limit (MONO_RESOURCE_GC_HEAP, allocated_heap);
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

void
mono_gc_enable_events (void)
{
}

const char *
mono_gc_get_gc_name (void)
{
	return "sgen";
}

char*
mono_gc_get_description (void)
{
	return g_strdup ("sgen");
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
	sgen_gc_collect (generation);
}

int
mono_gc_collection_count (int generation)
{
	return sgen_gc_collection_count (generation);
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

void*
mono_gc_get_nursery (int *shift_bits, size_t *size)
{
	*size = sgen_nursery_size;
	*shift_bits = DEFAULT_NURSERY_BITS;
	return sgen_get_nursery_start ();
}

int
mono_gc_get_los_limit (void)
{
	return SGEN_MAX_SMALL_OBJ_SIZE;
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
 * mono_gchandle_is_in_domain:
 * @gchandle: a GCHandle's handle.
 * @domain: An application domain.
 *
 * Returns: TRUE if the object wrapped by the @gchandle belongs to the specific @domain.
 */
gboolean
mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain)
{
	MonoDomain *gchandle_domain = (MonoDomain *)sgen_gchandle_get_metadata (gchandle);
	return domain->domain_id == gchandle_domain->domain_id;
}

/**
 * mono_gchandle_free_domain:
 * @unloading: domain that is unloading
 *
 * Function used internally to cleanup any GC handle for objects belonging
 * to the specified domain during appdomain unload.
 */
void
mono_gchandle_free_domain (MonoDomain *unloading)
{
}

static gpointer
null_link_if_in_domain (gpointer hidden, GCHandleType handle_type, int max_generation, gpointer user)
{
	MonoDomain *unloading_domain = (MonoDomain *)user;
	MonoDomain *obj_domain;
	gboolean is_weak = MONO_GC_HANDLE_TYPE_IS_WEAK (handle_type);
	if (MONO_GC_HANDLE_IS_OBJECT_POINTER (hidden)) {
		MonoObject *obj = (MonoObject *)MONO_GC_REVEAL_POINTER (hidden, is_weak);
		obj_domain = mono_object_domain (obj);
	} else {
		obj_domain = (MonoDomain *)MONO_GC_REVEAL_POINTER (hidden, is_weak);
	}
	if (unloading_domain->domain_id == obj_domain->domain_id)
		return NULL;
	return hidden;
}

void
sgen_null_links_for_domain (MonoDomain *domain)
{
	guint type;
	for (type = HANDLE_TYPE_MIN; type < HANDLE_TYPE_MAX; ++type)
		sgen_gchandle_iterate ((GCHandleType)type, GENERATION_OLD, null_link_if_in_domain, domain);
}

void
mono_gchandle_set_target (guint32 gchandle, MonoObject *obj)
{
	sgen_gchandle_set_target (gchandle, obj);
}

void
sgen_client_gchandle_created (int handle_type, GCObject *obj, guint32 handle)
{
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_num_handles++;
#endif
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_CREATED, handle_type, handle, obj);
}

void
sgen_client_gchandle_destroyed (int handle_type, guint32 handle)
{
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_num_handles--;
#endif
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_DESTROYED, handle_type, handle, NULL);
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
	if (G_UNLIKELY (bridge_processing_in_progress))
		mono_gc_wait_for_bridge_processing ();
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

gboolean
mono_gc_card_table_nursery_check (void)
{
	return !sgen_get_major_collector ()->is_concurrent;
}

/* Negative value to remove */
void
mono_gc_add_memory_pressure (gint64 value)
{
	/* FIXME: Implement at some point? */
}

/*
 * Logging
 */

void
sgen_client_degraded_allocation (size_t size)
{
	static int last_major_gc_warned = -1;
	static int num_degraded = 0;

	if (last_major_gc_warned < (int)gc_stats.major_gc_count) {
		++num_degraded;
		if (num_degraded == 1 || num_degraded == 3)
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_GC, "Warning: Degraded allocation.  Consider increasing nursery-size if the warning persists.");
		else if (num_degraded == 10)
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_GC, "Warning: Repeated degraded allocation.  Consider increasing nursery-size.");
		last_major_gc_warned = gc_stats.major_gc_count;
	}
}

void
sgen_client_log_timing (GGTimingInfo *info, mword last_major_num_sections, mword last_los_memory_usage)
{
	SgenMajorCollector *major_collector = sgen_get_major_collector ();
	mword num_major_sections = major_collector->get_num_major_sections ();
	char full_timing_buff [1024];
	full_timing_buff [0] = '\0';

	if (!info->is_overflow)
	        sprintf (full_timing_buff, "total %.2fms, bridge %.2fms", info->stw_time / 10000.0f, (int)info->bridge_time / 10000.0f);
	if (info->generation == GENERATION_OLD)
	        mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_GC, "GC_MAJOR%s: (%s) pause %.2fms, %s major %dK/%dK los %dK/%dK",
	                info->is_overflow ? "_OVERFLOW" : "",
	                info->reason ? info->reason : "",
	                (int)info->total_time / 10000.0f,
	                full_timing_buff,
	                major_collector->section_size * num_major_sections / 1024,
	                major_collector->section_size * last_major_num_sections / 1024,
	                los_memory_usage / 1024,
	                last_los_memory_usage / 1024);
	else
	        mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_GC, "GC_MINOR%s: (%s) pause %.2fms, %s promoted %dK major %dK los %dK",
	        		info->is_overflow ? "_OVERFLOW" : "",
	                info->reason ? info->reason : "",
	                (int)info->total_time / 10000.0f,
	                full_timing_buff,
	                (num_major_sections - last_major_num_sections) * major_collector->section_size / 1024,
	                major_collector->section_size * num_major_sections / 1024,
	                los_memory_usage / 1024);
}

/*
 * Debugging
 */

const char*
sgen_client_description_for_internal_mem_type (int type)
{
	switch (type) {
	case INTERNAL_MEM_EPHEMERON_LINK: return "ephemeron-link";
	default:
		return NULL;
	}
}

void
sgen_client_pre_collection_checks (void)
{
	if (sgen_mono_xdomain_checks) {
		sgen_clear_nursery_fragments ();
		sgen_check_for_xdomain_refs ();
	}
}

gboolean
sgen_client_vtable_is_inited (MonoVTable *vt)
{
	return vt->klass->inited;
}

const char*
sgen_client_vtable_get_namespace (MonoVTable *vt)
{
	return vt->klass->name_space;
}

const char*
sgen_client_vtable_get_name (MonoVTable *vt)
{
	return vt->klass->name;
}

/*
 * Initialization
 */

void
sgen_client_init (void)
{
	int dummy;
	MonoThreadInfoCallbacks cb;

	cb.thread_register = sgen_thread_register;
	cb.thread_detach = sgen_thread_detach;
	cb.thread_unregister = sgen_thread_unregister;
	cb.thread_attach = sgen_thread_attach;
	cb.mono_method_is_critical = (gboolean (*)(void *))is_critical_method;
	cb.mono_thread_in_critical_region = thread_in_critical_region;

	mono_threads_init (&cb, sizeof (SgenThreadInfo));

	///* Keep this the default for now */
	/* Precise marking is broken on all supported targets. Disable until fixed. */
	conservative_stack_mark = TRUE;

	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_EPHEMERON_LINK, sizeof (EphemeronLinkNode));

	mono_sgen_init_stw ();

#ifndef HAVE_KW_THREAD
	mono_native_tls_alloc (&thread_info_key, NULL);
#if defined(TARGET_OSX) || defined(TARGET_WIN32) || defined(TARGET_ANDROID) || defined(TARGET_IOS)
	/* 
	 * CEE_MONO_TLS requires the tls offset, not the key, so the code below only works on darwin,
	 * where the two are the same.
	 */
	mono_tls_key_set_offset (TLS_KEY_SGEN_THREAD_INFO, thread_info_key);
#endif
#else
	{
		int tls_offset = -1;
		MONO_THREAD_VAR_OFFSET (sgen_thread_info, tls_offset);
		mono_tls_key_set_offset (TLS_KEY_SGEN_THREAD_INFO, tls_offset);
	}
#endif

	/*
	 * This needs to happen before any internal allocations because
	 * it inits the small id which is required for hazard pointer
	 * operations.
	 */
	sgen_os_init ();

	mono_gc_register_thread (&dummy);
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
	} else {
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
	if (!strcmp (opt, "xdomain-checks")) {
		sgen_mono_xdomain_checks = TRUE;
	} else if (!strcmp (opt, "do-not-finalize")) {
		mono_do_not_finalize = TRUE;
	} else if (g_str_has_prefix (opt, "do-not-finalize=")) {
		opt = strchr (opt, '=') + 1;
		mono_do_not_finalize = TRUE;
		mono_do_not_finalize_class_names = g_strsplit (opt, ",", 0);
	} else if (!strcmp (opt, "log-finalizers")) {
		log_finalizers = TRUE;
	} else if (!strcmp (opt, "no-managed-allocator")) {
		sgen_set_use_managed_allocator (FALSE);
	} else if (!sgen_bridge_handle_gc_debug (opt)) {
		return FALSE;
	}
	return TRUE;
}

void
sgen_client_print_gc_debug_usage (void)
{
	fprintf (stderr, "  xdomain-checks\n");
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

void
mono_gc_base_init (void)
{
	mono_counters_init ();

#ifdef HEAVY_STATISTICS
	mono_counters_register ("los marked cards", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &los_marked_cards);
	mono_counters_register ("los array cards scanned ", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &los_array_cards);
	mono_counters_register ("los array remsets", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &los_array_remsets);

	mono_counters_register ("WBarrier set arrayref", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_set_arrayref);
	mono_counters_register ("WBarrier value copy", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_value_copy);
	mono_counters_register ("WBarrier object copy", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_object_copy);
#endif

	sgen_gc_init ();

	if (nursery_canaries_enabled ())
		sgen_set_use_managed_allocator (FALSE);

#if defined(HAVE_KW_THREAD)
	/* This can happen with using libmonosgen.so */
	if (mono_tls_key_get_offset (TLS_KEY_SGEN_TLAB_NEXT_ADDR) == -1)
		sgen_set_use_managed_allocator (FALSE);
#endif
}

void
mono_gc_base_cleanup (void)
{
}

gboolean
mono_gc_is_null (void)
{
	return FALSE;
}

#endif
