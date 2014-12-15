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

/* If set, check that there are no references to the domain left at domain unload */
gboolean sgen_mono_xdomain_checks = FALSE;

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

	if (sgen_ptr_in_nursery (dest) || ptr_on_stack (dest) || !SGEN_CLASS_HAS_REFERENCES (klass)) {
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

	sgen_get_remset ()->wbarrier_value_copy (dest, src, count, klass);
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

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

/* Vtable of the objects used to fill out nursery fragments before a collection */
static MonoVTable *array_fill_vtable;

MonoVTable*
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

		array_fill_vtable = vtable;
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
	o->obj.vtable = sgen_client_get_array_fill_vtable ();
	/* Mark this as not a real object */
	o->obj.synchronisation = GINT_TO_POINTER (-1);
	o->bounds = NULL;
	o->max_length = (mono_array_size_t)(size - sizeof (MonoArray));

	return TRUE;
}

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
sgen_client_object_queued_for_finalization (MonoObject *obj)
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

static gboolean
need_remove_object_for_domain (char *start, MonoDomain *domain)
{
	if (mono_object_domain (start) == domain) {
		SGEN_LOG (4, "Need to cleanup object %p", start);
		binary_protocol_cleanup (start, (gpointer)SGEN_LOAD_VTABLE (start), sgen_safe_object_get_size ((MonoObject*)start));
		return TRUE;
	}
	return FALSE;
}

static void
process_object_for_domain_clearing (char *start, MonoDomain *domain)
{
	GCVTable *vt = (GCVTable*)SGEN_LOAD_VTABLE (start);
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
