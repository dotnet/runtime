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

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "metadata/monitor.h"
#include "metadata/sgen-layout-stats.h"
#include "metadata/sgen-client.h"
#include "metadata/sgen-cardtable.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/abi-details.h"
#include "metadata/profiler-private.h"
#include "metadata/mono-gc.h"
#include "utils/mono-memory-model.h"

/* If set, check that there are no references to the domain left at domain unload */
gboolean sgen_mono_xdomain_checks = FALSE;

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

/*
 * Write barriers
 */

static gboolean
ptr_on_stack (void *ptr)
{
	gpointer stack_start = &stack_start;
	SgenThreadInfo *info = mono_thread_info_current ();

	if (ptr >= stack_start && ptr < (gpointer)info->stack_end)
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
#include "sgen-scan-object.h"
}
#endif

void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	HEAVY_STAT (++stat_wbarrier_value_copy);
	g_assert (klass->valuetype);

	SGEN_LOG (8, "Adding value remset at %p, count %d, descr %p for class %s (%p)", dest, count, klass->gc_descr, klass->name, klass);

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

	if (sgen_ptr_in_nursery (obj) || ptr_on_stack (obj)) {
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

/*
 * Dummy filler objects
 */

/* Vtable of the objects used to fill out nursery fragments before a collection */
static GCVTable *array_fill_vtable;

GCVTable*
sgen_client_get_array_fill_vtable (void)
{
	if (!array_fill_vtable) {
		static MonoClass klass;
		static char _vtable[sizeof(MonoVTable)+8];
		MonoVTable* vtable = (MonoVTable*) ALIGN_TO(_vtable, 8);
		gsize bmap;

		MonoDomain *domain = mono_get_root_domain ();
		g_assert (domain);

		klass.element_class = mono_defaults.byte_class;
		klass.rank = 1;
		klass.instance_size = sizeof (MonoArray);
		klass.sizes.element_size = 1;
		klass.name = "array_filler_type";

		vtable->klass = &klass;
		bmap = 0;
		vtable->gc_descr = mono_gc_make_descr_for_array (TRUE, &bmap, 0, 1);
		vtable->rank = 1;

		array_fill_vtable = (GCVTable*)vtable;
	}
	return array_fill_vtable;
}

gboolean
sgen_client_array_fill_range (char *start, size_t size)
{
	MonoArray *o;

	if (size < sizeof (MonoArray)) {
		memset (start, 0, size);
		return FALSE;
	}

	o = (MonoArray*)start;
	o->obj.vtable = (MonoVTable*)sgen_client_get_array_fill_vtable ();
	/* Mark this as not a real object */
	o->obj.synchronisation = GINT_TO_POINTER (-1);
	o->bounds = NULL;
	o->max_length = (mono_array_size_t)(size - sizeof (MonoArray));

	return TRUE;
}

void
sgen_client_zero_array_fill_header (void *p, size_t size)
{
	if (size >= sizeof (MonoArray)) {
		memset (p, 0, sizeof (MonoArray));
	} else {
		static guint8 zeros [sizeof (MonoArray)];

		SGEN_ASSERT (0, !memcmp (p, zeros, size), "TLAB segment must be zeroed out.");
	}
}

/*
 * Finalization
 */

static MonoGCFinalizerCallbacks fin_callbacks;

guint
mono_gc_get_vtable_bits (MonoClass *class)
{
	guint res = 0;
	/* FIXME move this to the bridge code */
	if (sgen_need_bridge_processing ()) {
		switch (sgen_bridge_class_kind (class)) {
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
		if (fin_callbacks.is_class_finalization_aware (class))
			res |= SGEN_GC_BIT_FINALIZER_AWARE;
	}
	return res;
}

static gboolean
is_finalization_aware (MonoObject *obj)
{
	MonoVTable *vt = ((MonoVTable*)SGEN_LOAD_VTABLE (obj));
	return (vt->gc_bits & SGEN_GC_BIT_FINALIZER_AWARE) == SGEN_GC_BIT_FINALIZER_AWARE;
}

void
sgen_client_object_queued_for_finalization (GCObject *obj)
{
	if (fin_callbacks.object_queued_for_finalization && is_finalization_aware (obj))
		fin_callbacks.object_queued_for_finalization (obj);
}

void
mono_gc_register_finalizer_callbacks (MonoGCFinalizerCallbacks *callbacks)
{
	if (callbacks->version != MONO_GC_FINALIZER_EXTENSION_VERSION)
		g_error ("Invalid finalizer callback version. Expected %d but got %d\n", MONO_GC_FINALIZER_EXTENSION_VERSION, callbacks->version);

	fin_callbacks = *callbacks;
}

/*
 * Ephemerons
 */

typedef struct _EphemeronLinkNode EphemeronLinkNode;

struct _EphemeronLinkNode {
	EphemeronLinkNode *next;
	char *array;
};

typedef struct {
       void *key;
       void *value;
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
	MonoArray *array;
	Ephemeron *cur, *array_end;
	char *tombstone;

	while (current) {
		char *object = current->array;

		if (!sgen_is_object_alive_for_current_gen (object)) {
			EphemeronLinkNode *tmp = current;

			SGEN_LOG (5, "Dead Ephemeron array at %p", object);

			if (prev)
				prev->next = current->next;
			else
				ephemeron_list = current->next;

			current = current->next;
			sgen_free_internal (tmp, INTERNAL_MEM_EPHEMERON_LINK);

			continue;
		}

		copy_func ((void**)&object, queue);
		current->array = object;

		SGEN_LOG (5, "Clearing unreachable entries for ephemeron array at %p", object);

		array = (MonoArray*)object;
		cur = mono_array_addr (array, Ephemeron, 0);
		array_end = cur + mono_array_length_fast (array);
		tombstone = (char*)((MonoVTable*)SGEN_LOAD_VTABLE (object))->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			char *key = (char*)cur->key;

			if (!key || key == tombstone)
				continue;

			SGEN_LOG (5, "[%td] key %p (%s) value %p (%s)", cur - mono_array_addr (array, Ephemeron, 0),
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
	MonoArray *array;
	Ephemeron *cur, *array_end;
	char *tombstone;

	for (current = ephemeron_list; current; current = current->next) {
		char *object = current->array;
		SGEN_LOG (5, "Ephemeron array at %p", object);

		/*It has to be alive*/
		if (!sgen_is_object_alive_for_current_gen (object)) {
			SGEN_LOG (5, "\tnot reachable");
			continue;
		}

		copy_func ((void**)&object, queue);

		array = (MonoArray*)object;
		cur = mono_array_addr (array, Ephemeron, 0);
		array_end = cur + mono_array_length_fast (array);
		tombstone = (char*)((MonoVTable*)SGEN_LOAD_VTABLE (object))->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			char *key = cur->key;

			if (!key || key == tombstone)
				continue;

			SGEN_LOG (5, "[%td] key %p (%s) value %p (%s)", cur - mono_array_addr (array, Ephemeron, 0),
				key, sgen_is_object_alive_for_current_gen (key) ? "reachable" : "unreachable",
				cur->value, cur->value && sgen_is_object_alive_for_current_gen (cur->value) ? "reachable" : "unreachable");

			if (sgen_is_object_alive_for_current_gen (key)) {
				char *value = cur->value;

				copy_func ((void**)&cur->key, queue);
				if (value) {
					if (!sgen_is_object_alive_for_current_gen (value))
						nothing_marked = FALSE;
					copy_func ((void**)&cur->value, queue);
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

	node = sgen_alloc_internal (INTERNAL_MEM_EPHEMERON_LINK);
	if (!node) {
		UNLOCK_GC;
		return FALSE;
	}
	node->array = (char*)obj;
	node->next = ephemeron_list;
	ephemeron_list = node;

	SGEN_LOG (5, "Registered ephemeron array %p", obj);

	UNLOCK_GC;
	return TRUE;
}

/*
 * Appdomain handling
 */

static gboolean
need_remove_object_for_domain (char *start, MonoDomain *domain)
{
	if (mono_object_domain (start) == domain) {
		SGEN_LOG (4, "Need to cleanup object %p", start);
		binary_protocol_cleanup (start, (gpointer)SGEN_LOAD_VTABLE (start), sgen_safe_object_get_size ((GCObject*)start));
		return TRUE;
	}
	return FALSE;
}

static void
process_object_for_domain_clearing (char *start, MonoDomain *domain)
{
	MonoVTable *vt = (MonoVTable*)SGEN_LOAD_VTABLE (start);
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
clear_domain_process_object (char *obj, MonoDomain *domain)
{
	gboolean remove;

	process_object_for_domain_clearing (obj, domain);
	remove = need_remove_object_for_domain (obj, domain);

	if (remove && ((MonoObject*)obj)->synchronisation) {
		void **dislink = mono_monitor_get_object_monitor_weak_link ((MonoObject*)obj);
		if (dislink)
			sgen_register_disappearing_link (NULL, dislink, FALSE, TRUE);
	}

	return remove;
}

static void
clear_domain_process_minor_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	if (clear_domain_process_object (obj, domain)) {
		CANARIFY_SIZE (size);
		memset (obj, 0, size);
	}
}

static void
clear_domain_process_major_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	clear_domain_process_object (obj, domain);
}

static void
clear_domain_free_major_non_pinned_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	if (need_remove_object_for_domain (obj, domain))
		major_collector.free_non_pinned_object (obj, size);
}

static void
clear_domain_free_major_pinned_object_callback (char *obj, size_t size, MonoDomain *domain)
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
	sgen_process_dislink_stage_entries ();

	sgen_clear_nursery_fragments ();

	if (sgen_mono_xdomain_checks && domain != mono_get_root_domain ()) {
		sgen_scan_for_registered_roots_in_domain (domain, ROOT_TYPE_NORMAL);
		sgen_scan_for_registered_roots_in_domain (domain, ROOT_TYPE_WBARRIER);
		sgen_check_for_xdomain_refs ();
	}

	/*Ephemerons and dislinks must be processed before LOS since they might end up pointing
	to memory returned to the OS.*/
	null_ephemerons_for_domain (domain);

	for (i = GENERATION_NURSERY; i < GENERATION_MAX; ++i)
		sgen_null_links_for_domain (domain, i);

	for (i = GENERATION_NURSERY; i < GENERATION_MAX; ++i)
		sgen_remove_finalizers_for_domain (domain, i);

	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)clear_domain_process_minor_object_callback, domain, FALSE);

	/* We need two passes over major and large objects because
	   freeing such objects might give their memory back to the OS
	   (in the case of large objects) or obliterate its vtable
	   (pinned objects with major-copying or pinned and non-pinned
	   objects with major-mark&sweep), but we might need to
	   dereference a pointer from an object to another object if
	   the first object is a proxy. */
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, (IterateObjectCallbackFunc)clear_domain_process_major_object_callback, domain);
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		clear_domain_process_object (bigobj->data, domain);

	prev = NULL;
	for (bigobj = los_object_list; bigobj;) {
		if (need_remove_object_for_domain (bigobj->data, domain)) {
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
 * Managed allocator
 */

static MonoMethod* alloc_method_cache [ATYPE_NUM];
static gboolean use_managed_allocator = TRUE;

#ifdef MANAGED_ALLOCATION
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LAST
};

#undef OPDEF

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

/* FIXME: Do this in the JIT, where specialized allocation sequences can be created
 * for each class. This is currently not easy to do, as it is hard to generate basic 
 * blocks + branches, but it is easy with the linear IL codebase.
 *
 * For this to work we'd need to solve the TLAB race, first.  Now we
 * require the allocator to be in a few known methods to make sure
 * that they are executed atomically via the restart mechanism.
 */
static MonoMethod*
create_allocator (int atype)
{
	int p_var, size_var;
	guint32 slowpath_branch, max_size_branch;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	static gboolean registered = FALSE;
	int tlab_next_addr_var, new_next_var;
	int num_params, i;
	const char *name = NULL;
	AllocatorWrapperInfo *info;

	if (!registered) {
		mono_register_jit_icall (mono_gc_alloc_obj, "mono_gc_alloc_obj", mono_create_icall_signature ("object ptr int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_vector, "mono_gc_alloc_vector", mono_create_icall_signature ("object ptr int int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_string, "mono_gc_alloc_string", mono_create_icall_signature ("object ptr int int32"), FALSE);
		registered = TRUE;
	}

	if (atype == ATYPE_SMALL) {
		num_params = 2;
		name = "AllocSmall";
	} else if (atype == ATYPE_NORMAL) {
		num_params = 1;
		name = "Alloc";
	} else if (atype == ATYPE_VECTOR) {
		num_params = 2;
		name = "AllocVector";
	} else if (atype == ATYPE_STRING) {
		num_params = 2;
		name = "AllocString";
	} else {
		g_assert_not_reached ();
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, num_params);
	if (atype == ATYPE_STRING) {
		csig->ret = &mono_defaults.string_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int32_class->byval_arg;
	} else {
		csig->ret = &mono_defaults.object_class->byval_arg;
		for (i = 0; i < num_params; ++i)
			csig->params [i] = &mono_defaults.int_class->byval_arg;
	}

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ALLOC);

#ifndef DISABLE_JIT
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

		clause = mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
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
		mono_mb_emit_icon (mb, sizeof (MonoArray));
		mono_mb_emit_byte (mb, CEE_ADD_OVF_UN);
		mono_mb_emit_stloc (mb, size_var);

		pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

		/* catch */
		clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
		clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
		clause->data.catch_class = mono_class_from_name (mono_defaults.corlib,
				"System", "OverflowException");
		g_assert (clause->data.catch_class);
		clause->handler_offset = mono_mb_get_label (mb);

		oom_exc_class = mono_class_from_name (mono_defaults.corlib,
				"System", "OutOfMemoryException");
		g_assert (oom_exc_class);
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
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	res = mono_mb_create_method (mb, csig, 8);
	mono_mb_free (mb);
	mono_method_get_header (res)->init_locals = FALSE;

	info = mono_image_alloc0 (mono_defaults.corlib, sizeof (AllocatorWrapperInfo));
	info->gc_name = "sgen";
	info->alloc_type = atype;
	mono_marshal_set_wrapper_info (res, info);

	return res;
}
#endif

int
mono_gc_get_aligned_size_for_allocator (int size)
{
	int aligned_size = size;
	aligned_size += SGEN_ALLOC_ALIGN - 1;
	aligned_size &= ~(SGEN_ALLOC_ALIGN - 1);
	return aligned_size;
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
	if (klass->has_finalize || mono_class_is_marshalbyref (klass) || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank)
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING)
		return mono_gc_get_managed_allocator_by_type (ATYPE_STRING);
	/* Generic classes have dynamic field and can go above MAX_SMALL_OBJ_SIZE. */
	if (known_instance_size)
		return mono_gc_get_managed_allocator_by_type (ATYPE_SMALL);
	else
		return mono_gc_get_managed_allocator_by_type (ATYPE_NORMAL);
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

	return mono_gc_get_managed_allocator_by_type (ATYPE_VECTOR);
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
mono_gc_get_managed_allocator_by_type (int atype)
{
#ifdef MANAGED_ALLOCATION
	MonoMethod *res;

	if (!use_managed_allocator)
		return NULL;

	if (!mono_runtime_has_tls_get ())
		return NULL;

	res = alloc_method_cache [atype];
	if (res)
		return res;

	res = create_allocator (atype);
	LOCK_GC;
	if (alloc_method_cache [atype]) {
		mono_free_method (res);
		res = alloc_method_cache [atype];
	} else {
		mono_memory_barrier ();
		alloc_method_cache [atype] = res;
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
		if (method == alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

gboolean
sgen_has_managed_allocator (void)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (alloc_method_cache [i])
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
sgen_client_cardtable_scan_object (char *obj, mword block_obj_size, guint8 *cards, gboolean mod_union, ScanCopyContext ctx)
{
	MonoVTable *vt = (MonoVTable*)SGEN_LOAD_VTABLE (obj);
	MonoClass *klass = vt->klass;

	SGEN_ASSERT (0, SGEN_VTABLE_HAS_REFERENCES ((GCVTable*)vt), "Why would we ever call this on reference-free objects?");

	if (vt->rank) {
		guint8 *card_data, *card_base;
		guint8 *card_data_end;
		char *obj_start = sgen_card_table_align_pointer (obj);
		mword obj_size = sgen_client_par_object_get_size (vt, (GCObject*)obj);
		char *obj_end = obj + obj_size;
		size_t card_count;
		size_t extra_idx = 0;

		MonoArray *arr = (MonoArray*)obj;
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
					gpointer new, old = *(gpointer*)elem;
					if ((mod_union && old) || G_UNLIKELY (sgen_ptr_in_nursery (old))) {
						HEAVY_STAT (++los_array_remsets);
						copy_func ((void**)elem, ctx.queue);
						new = *(gpointer*)elem;
						if (G_UNLIKELY (sgen_ptr_in_nursery (new)))
							sgen_add_to_global_remset (elem, new);
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
	arr = sgen_try_alloc_obj_nolock ((GCVTable*)vtable, size);
	if (arr) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		arr->max_length = (mono_array_size_t)max_length;
		EXIT_CRITICAL_REGION;
		goto done;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	arr = sgen_alloc_obj_nolock ((GCVTable*)vtable, size);
	if (G_UNLIKELY (!arr)) {
		UNLOCK_GC;
		return mono_gc_out_of_memory (size);
	}

	arr->max_length = (mono_array_size_t)max_length;

	UNLOCK_GC;

 done:
	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size ((GCVTable*)vtable, (GCObject*)arr)), "Vector has incorrect size.");
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
	arr = sgen_try_alloc_obj_nolock ((GCVTable*)vtable, size);
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

	arr = sgen_alloc_obj_nolock ((GCVTable*)vtable, size);
	if (G_UNLIKELY (!arr)) {
		UNLOCK_GC;
		return mono_gc_out_of_memory (size);
	}

	arr->max_length = (mono_array_size_t)max_length;

	bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
	arr->bounds = bounds;

	UNLOCK_GC;

 done:
	SGEN_ASSERT (6, SGEN_ALIGN_UP (size) == SGEN_ALIGN_UP (sgen_client_par_object_get_size ((GCVTable*)vtable, (GCObject*)arr)), "Array has incorrect size.");
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
	str = sgen_try_alloc_obj_nolock ((GCVTable*)vtable, size);
	if (str) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		str->length = len;
		EXIT_CRITICAL_REGION;
		return str;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	str = sgen_alloc_obj_nolock ((GCVTable*)vtable, size);
	if (G_UNLIKELY (!str)) {
		UNLOCK_GC;
		return mono_gc_out_of_memory (size);
	}

	str->length = len;

	UNLOCK_GC;

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
		CHECK_CANARY_FOR_OBJECT (str);
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
	report->extra_info [report->count++] = (uintptr_t)((MonoVTable*)SGEN_LOAD_VTABLE (object))->klass;
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
single_arg_report_root (void **obj, void *gc_data)
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
		gsize *bitmap_data = sgen_get_complex_descriptor_bitmap (desc);
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
		MonoGCRootMarkFunc marker = sgen_get_user_descriptor_func (desc);
		root_report = report;
		marker (start_root, single_arg_report_root, NULL);
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
	SGEN_HASH_TABLE_FOREACH (&roots_hash [root_type], start_root, root) {
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
sgen_client_pinned_los_object (char *obj)
{
	if (profile_roots)
		add_profile_gc_root (&major_root_report, obj, MONO_PROFILE_GC_ROOT_PINNING | MONO_PROFILE_GC_ROOT_MISC, 0);
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
collect_references (HeapWalkInfo *hwi, char *start, size_t size)
{
	mword desc = sgen_obj_get_descriptor (start);

#include "sgen-scan-object.h"
}

static void
walk_references (char *start, size_t size, void *data)
{
	HeapWalkInfo *hwi = data;
	hwi->called = 0;
	hwi->count = 0;
	collect_references (hwi, start, size);
	if (hwi->count || !hwi->called)
		hwi->callback ((MonoObject*)start, mono_object_class (start), hwi->called? 0: size, hwi->count, hwi->refs, hwi->offsets, hwi->data);
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
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data, walk_references, &hwi, FALSE);

	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, walk_references, &hwi);
	sgen_los_iterate_objects (walk_references, &hwi);

	return 0;
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

const char*
sgen_client_object_safe_name (GCObject *obj)
{
	MonoVTable *vt = (MonoVTable*)SGEN_LOAD_VTABLE (obj);
	return vt->klass->name;
}

const char*
sgen_client_vtable_get_namespace (GCVTable *gc_vtable)
{
	MonoVTable *vt = (MonoVTable*)gc_vtable;
	return vt->klass->name_space;
}

const char*
sgen_client_vtable_get_name (GCVTable *gc_vtable)
{
	MonoVTable *vt = (MonoVTable*)gc_vtable;
	return vt->klass->name;
}

/*
 * Initialization
 */

void
sgen_client_init (void)
{
	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_EPHEMERON_LINK, sizeof (EphemeronLinkNode));
}

gboolean
sgen_client_handle_gc_debug (const char *opt)
{
	if (!strcmp (opt, "xdomain-checks")) {
		sgen_mono_xdomain_checks = TRUE;
	} else {
		return FALSE;
	}
	return TRUE;
}

void
sgen_client_print_gc_debug_usage (void)
{
	fprintf (stderr, "  xdomain-checks\n");
}

#endif
