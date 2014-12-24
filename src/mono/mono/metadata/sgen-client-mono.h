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

enum {
	INTERNAL_MEM_EPHEMERON_LINK = INTERNAL_MEM_FIRST_CLIENT,
	INTERNAL_MEM_MAX
};

typedef MonoObject GCObject;
typedef MonoVTable GCVTable;

/* FIXME: This should return a GCVTable* and be a function. */
#define SGEN_LOAD_VTABLE_UNCHECKED(obj)	((void*)(((GCObject*)(obj))->vtable))

static inline mword
sgen_vtable_get_descriptor (GCVTable *vtable)
{
	return (mword)vtable->gc_descr;
}

#define SGEN_CLIENT_OBJECT_HEADER_SIZE		(sizeof (GCObject))
#define SGEN_CLIENT_MINIMUM_OBJECT_SIZE		SGEN_CLIENT_OBJECT_HEADER_SIZE

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

/* FIXME: Why do we even need this?  Can't we get it from the descriptor? */
static gboolean G_GNUC_UNUSED
sgen_client_vtable_has_references (GCVTable *vt)
{
	return ((MonoVTable*)vt)->klass->has_references;
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
