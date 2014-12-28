/*
 * sgen-client-mono.h: Mono's client definitions for SGen.
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

#ifdef SGEN_DEFINE_OBJECT_VTABLE

typedef MonoObject GCObject;
typedef MonoVTable GCVTable;

/* FIXME: This should return a GCVTable* and be a function. */
#define SGEN_LOAD_VTABLE_UNCHECKED(obj)	((void*)(((GCObject*)(obj))->vtable))

static inline mword
sgen_vtable_get_descriptor (GCVTable *vtable)
{
	return (mword)vtable->gc_descr;
}

static mword /*__attribute__((noinline)) not sure if this hint is a good idea*/
sgen_client_slow_object_get_size (GCVTable *vtable, GCObject* o)
{
	MonoClass *klass = ((MonoVTable*)vtable)->klass;

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

/*
 * This function can be called on an object whose first word, the
 * vtable field, is not intact.  This is necessary for the parallel
 * collector.
 */
static MONO_NEVER_INLINE mword
sgen_client_par_object_get_size (GCVTable *vtable, GCObject* o)
{
	mword descr = sgen_vtable_get_descriptor (vtable);
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
			size += sizeof (MonoArrayBounds) * ((MonoVTable*)vtable)->klass->rank;
		}
		return size;
	}

	return sgen_client_slow_object_get_size (vtable, o);
}

#else

#include "metadata/profiler-private.h"
#include "utils/dtrace.h"

extern void mono_sgen_register_moved_object (void *obj, void *destination);
extern void mono_sgen_gc_event_moves (void);

extern void mono_sgen_init_stw (void);

enum {
	INTERNAL_MEM_EPHEMERON_LINK = INTERNAL_MEM_FIRST_CLIENT,
	INTERNAL_MEM_MAX
};

#define SGEN_CLIENT_OBJECT_HEADER_SIZE		(sizeof (GCObject))
#define SGEN_CLIENT_MINIMUM_OBJECT_SIZE		SGEN_CLIENT_OBJECT_HEADER_SIZE

static MONO_ALWAYS_INLINE size_t G_GNUC_UNUSED
sgen_client_array_element_size (GCVTable *gc_vtable)
{
	MonoVTable *vt = (MonoVTable*)gc_vtable;
	return mono_array_element_size (vt->klass);
}

static MONO_ALWAYS_INLINE G_GNUC_UNUSED char*
sgen_client_array_data_start (GCObject *obj)
{
	return (char*)(obj) +  G_STRUCT_OFFSET (MonoArray, vector);
}

static MONO_ALWAYS_INLINE size_t G_GNUC_UNUSED
sgen_client_array_length (GCObject *obj)
{
	return mono_array_length_fast ((MonoArray*)obj);
}

static MONO_ALWAYS_INLINE gboolean G_GNUC_UNUSED
sgen_client_object_is_array_fill (GCObject *o)
{
	return ((MonoObject*)o)->synchronisation == GINT_TO_POINTER (-1);
}

/* FIXME: Why do we even need this?  Can't we get it from the descriptor? */
static gboolean G_GNUC_UNUSED
sgen_client_vtable_has_references (GCVTable *vt)
{
	return ((MonoVTable*)vt)->klass->has_references;
}

static MONO_ALWAYS_INLINE void G_GNUC_UNUSED
sgen_client_pre_copy_checks (char *destination, GCVTable *gc_vtable, void *obj, mword objsize)
{
	MonoVTable *vt = (MonoVTable*)gc_vtable;
	SGEN_ASSERT (9, vt->klass->inited, "vtable %p for class %s:%s was not initialized", vt, vt->klass->name_space, vt->klass->name);
}

static MONO_ALWAYS_INLINE void G_GNUC_UNUSED
sgen_client_update_copied_object (char *destination, GCVTable *gc_vtable, void *obj, mword objsize)
{
	MonoVTable *vt = (MonoVTable*)gc_vtable;
	if (G_UNLIKELY (vt->rank && ((MonoArray*)obj)->bounds)) {
		MonoArray *array = (MonoArray*)destination;
		array->bounds = (MonoArrayBounds*)((char*)destination + ((char*)((MonoArray*)obj)->bounds - (char*)obj));
		SGEN_LOG (9, "Array instance %p: size: %lu, rank: %d, length: %lu", array, (unsigned long)objsize, vt->rank, (unsigned long)mono_array_length (array));
	}

	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		mono_sgen_register_moved_object (obj, destination);
}

#ifdef XDOMAIN_CHECKS_IN_WBARRIER
extern gboolean sgen_mono_xdomain_checks;

#define sgen_client_wbarrier_generic_nostore_check(ptr) do {		\
		/* FIXME: ptr_in_heap must be called with the GC lock held */ \
		if (sgen_mono_xdomain_checks && *(MonoObject**)ptr && ptr_in_heap (ptr)) { \
			char *start = find_object_for_ptr (ptr);	\
			MonoObject *value = *(MonoObject**)ptr;		\
			LOCK_GC;					\
			SGEN_ASSERT (0, start, "Write barrier outside an object?"); \
			if (start) {					\
				MonoObject *obj = (MonoObject*)start;	\
				if (obj->vtable->domain != value->vtable->domain) \
					SGEN_ASSERT (0, is_xdomain_ref_allowed (ptr, start, obj->vtable->domain), "Cross-domain ref not allowed"); \
			}						\
			UNLOCK_GC;					\
		}							\
	} while (0)
#else
#define sgen_client_wbarrier_generic_nostore_check(ptr)
#endif

static gboolean G_GNUC_UNUSED
sgen_client_object_has_critical_finalizer (GCObject *obj)
{
	MonoClass *class;

	if (!mono_defaults.critical_finalizer_object)
		return FALSE;

	class = ((MonoVTable*)SGEN_LOAD_VTABLE (obj))->klass;

	return mono_class_has_parent_fast (class, mono_defaults.critical_finalizer_object);
}

const char* sgen_client_vtable_get_namespace (GCVTable *vtable);
const char* sgen_client_vtable_get_name (GCVTable *vtable);

static void G_GNUC_UNUSED
sgen_client_binary_protocol_collection_requested (int generation, size_t requested_size, gboolean force)
{
	MONO_GC_REQUESTED (generation, requested_size, force);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_collection_begin (int minor_gc_count, int generation)
{
	MONO_GC_BEGIN (generation);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_collection_end (int minor_gc_count, int generation, long long num_objects_scanned, long long num_unique_objects_scanned)
{
	MONO_GC_END (generation);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_concurrent_start (void)
{
	MONO_GC_CONCURRENT_START_BEGIN (GENERATION_OLD);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_concurrent_update (void)
{
	MONO_GC_CONCURRENT_UPDATE_FINISH_BEGIN (GENERATION_OLD, sgen_get_major_collector ()->get_and_reset_num_major_objects_marked ());
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_concurrent_finish (void)
{
	MONO_GC_CONCURRENT_UPDATE_FINISH_BEGIN (GENERATION_OLD, sgen_get_major_collector ()->get_and_reset_num_major_objects_marked ());
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_sweep_begin (int generation, int full_sweep)
{
	MONO_GC_SWEEP_BEGIN (generation, full_sweep);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_sweep_end (int generation, int full_sweep)
{
	MONO_GC_SWEEP_END (generation, full_sweep);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_world_stopping (int generation, long long timestamp)
{
	MONO_GC_WORLD_STOP_BEGIN ();
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_world_stopped (int generation, long long timestamp, long long total_major_cards, long long marked_major_cards, long long total_los_cards, long long marked_los_cards)
{
	MONO_GC_WORLD_STOP_END ();
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_world_restarting (int generation, long long timestamp, long long total_major_cards, long long marked_major_cards, long long total_los_cards, long long marked_los_cards)
{
	MONO_GC_WORLD_RESTART_BEGIN (generation);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_world_restarted (int generation, long long timestamp)
{
	MONO_GC_WORLD_RESTART_END (generation);
}

static void
mono_binary_protocol_alloc_generic (gpointer obj, gpointer vtable, size_t size, gboolean pinned)
{
#ifdef ENABLE_DTRACE
	const char *namespace = sgen_client_vtable_get_namespace (vtable);
	const char *name = sgen_client_vtable_get_name (vtable);

	if (sgen_ptr_in_nursery (obj)) {
		if (G_UNLIKELY (MONO_GC_NURSERY_OBJ_ALLOC_ENABLED ()))
			MONO_GC_NURSERY_OBJ_ALLOC ((mword)obj, size, namespace, name);
	} else {
		if (size > SGEN_MAX_SMALL_OBJ_SIZE) {
			if (G_UNLIKELY (MONO_GC_MAJOR_OBJ_ALLOC_LARGE_ENABLED ()))
				MONO_GC_MAJOR_OBJ_ALLOC_LARGE ((mword)obj, size, namespace, name);
		} else if (pinned) {
			MONO_GC_MAJOR_OBJ_ALLOC_PINNED ((mword)obj, size, namespace, name);
		}
	}
#endif
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_alloc (gpointer obj, gpointer vtable, size_t size)
{
	mono_binary_protocol_alloc_generic (obj, vtable, size, FALSE);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_alloc_pinned (gpointer obj, gpointer vtable, size_t size)
{
	mono_binary_protocol_alloc_generic (obj, vtable, size, TRUE);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_alloc_degraded (gpointer obj, gpointer vtable, size_t size)
{
	MONO_GC_MAJOR_OBJ_ALLOC_DEGRADED ((mword)obj, size, sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_pin (gpointer obj, gpointer vtable, size_t size)
{
#ifdef ENABLE_DTRACE
	if (G_UNLIKELY (MONO_GC_OBJ_PINNED_ENABLED ())) {
		int gen = sgen_ptr_in_nursery (obj) ? GENERATION_NURSERY : GENERATION_OLD;
		MONO_GC_OBJ_PINNED ((mword)obj,
				sgen_safe_object_get_size (obj),
				sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable), gen);
	}
#endif
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_cement (gpointer ptr, gpointer vtable, size_t size)
{
#ifdef ENABLE_DTRACE
	if (G_UNLIKELY (MONO_GC_OBJ_CEMENTED_ENABLED())) {
		MONO_GC_OBJ_CEMENTED ((mword)ptr, sgen_safe_object_get_size ((GCObject*)ptr),
				sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
	}
#endif
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_copy (gpointer from, gpointer to, gpointer vtable, size_t size)
{
#ifdef ENABLE_DTRACE
	if (G_UNLIKELY (MONO_GC_OBJ_MOVED_ENABLED ())) {
		int dest_gen = sgen_ptr_in_nursery (to) ? GENERATION_NURSERY : GENERATION_OLD;
		int src_gen = sgen_ptr_in_nursery (from) ? GENERATION_NURSERY : GENERATION_OLD;
		MONO_GC_OBJ_MOVED ((mword)to, (mword)from, dest_gen, src_gen, size, sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
	}
#endif
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_global_remset (gpointer ptr, gpointer value, gpointer value_vtable)
{
#ifdef ENABLE_DTRACE
	if (G_UNLIKELY (MONO_GC_GLOBAL_REMSET_ADD_ENABLED ())) {
		MONO_GC_GLOBAL_REMSET_ADD ((mword)ptr, (mword)value, sgen_safe_object_get_size (value),
				sgen_client_vtable_get_namespace (value_vtable), sgen_client_vtable_get_name (value_vtable));
	}
#endif
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_dislink_update (gpointer link, gpointer obj, gboolean track, gboolean staged)
{
#ifdef ENABLE_DTRACE
	if (MONO_GC_WEAK_UPDATE_ENABLED ()) {
		GCVTable *vt = obj ? (GCVTable*)SGEN_LOAD_VTABLE (obj) : NULL;
		MONO_GC_WEAK_UPDATE ((mword)link,
				(mword)obj,
				obj ? (mword)sgen_safe_object_get_size (obj) : (mword)0,
				obj ? sgen_client_vtable_get_namespace (vt) : NULL,
				obj ? sgen_client_vtable_get_name (vt) : NULL,
				track ? 1 : 0);
	}
#endif
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_empty (gpointer start, size_t size)
{
	if (sgen_ptr_in_nursery (start))
		MONO_GC_NURSERY_SWEPT ((mword)start, size);
	else
		MONO_GC_MAJOR_SWEPT ((mword)start, size);
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_thread_suspend (gpointer thread, gpointer stopped_ip)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_thread_restart (gpointer thread)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_thread_register (gpointer thread)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_thread_unregister (gpointer thread)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_missing_remset (gpointer obj, gpointer obj_vtable, int offset, gpointer value, gpointer value_vtable, gboolean value_pinned)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_cement_reset (void)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_domain_unload_begin (gpointer domain)
{
}

static void G_GNUC_UNUSED
sgen_client_binary_protocol_domain_unload_end (gpointer domain)
{
}

#endif
