/*
 * metadata/gc.c: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/gc.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/tabledefs.h>
#define GC_I_HIDE_POINTERS
#include <mono/os/gc_wrapper.h>

#ifndef HIDE_POINTER
#define HIDE_POINTER(v)         (v)
#define REVEAL_POINTER(v)       (v)
#endif

static int finalize_slot = -1;

/* 
 * actually, we might want to queue the finalize requests in a separate thread,
 * but we need to be careful about the execution domain of the thread...
 */
static void
run_finalize (void *obj, void *data)
{
	MonoObject *exc = NULL;
	MonoObject *o;
	o = (MonoObject*)((char*)obj + GPOINTER_TO_UINT (data));

	if (finalize_slot < 0) {
		int i;
		for (i = 0; i < mono_defaults.object_class->vtable_size; ++i) {
			MonoMethod *cm = mono_defaults.object_class->vtable [i];
	       
			if (!strcmp (cm->name, "Finalize")) {
				finalize_slot = i;
				break;
			}
		}
	}
	/* speedup later... and use a timeout */
	/*g_print ("Finalize run on %p %s.%s\n", o, mono_object_class (o)->name_space, mono_object_class (o)->name);*/
	mono_runtime_invoke (o->vtable->klass->vtable [finalize_slot], o, NULL, &exc);

	if (exc) {
		/* fixme: do something useful */
	}
}

/*
 * Some of our objects may point to a different address than the address returned by GC_malloc()
 * (because of the GetHashCode hack), but we need to pass the real address to register_finalizer.
 * This also means that in the callback we need to adjust the pointer to get back the real
 * MonoObject*.
 * We also need to be consistent in the use of the GC_debug* variants of malloc and register_finalizer, 
 * since that, too, can cause the underlying pointer to be offset.
 */
static void
object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*))
{
#if HAVE_BOEHM_GC
	guint offset = 0;

	g_assert (GC_base (obj) == (char*)obj - offset);
	GC_register_finalizer ((char*)obj - offset, callback, GUINT_TO_POINTER (offset), NULL, NULL);
#endif
}

void
mono_object_register_finalizer (MonoObject *obj)
{
	/*g_print ("Registered finalizer on %p %s.%s\n", obj, mono_object_class (obj)->name_space, mono_object_class (obj)->name);*/
	object_register_finalizer (obj, run_finalize);
}

/* 
 * to speedup, at class init time, check if a class or struct
 * have fields that need to be finalized and set a flag.
 */
static void
finalize_fields (MonoClass *class, char *data, gboolean instance, GHashTable *todo) {
	int i;
	MonoClassField *field;
	MonoObject *obj;

	/*if (!instance)
		g_print ("Finalize statics on on %s\n", class->name);*/
	if (instance && class->valuetype)
		data -= sizeof (MonoObject);
	do {
		for (i = 0; i < class->field.count; ++i) {
			field = &class->fields [i];
			if (instance) {
				if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
					continue;
			} else {
				if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
					continue;
			}
			switch (field->type->type) {
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_CLASS:
				obj = *((MonoObject**)(data + field->offset));
				if (obj) {
					if (mono_object_class (obj)->has_finalize) {
						/* disable the registered finalizer */
						object_register_finalizer (obj, NULL);
						run_finalize (obj, NULL);
					} else {
						/* 
						 * if the type doesn't have a finalizer, we finalize 
						 * the fields ourselves just like we do for structs.
						 * Disabled for now: how do we handle loops?
						 */
						/*finalize_fields (mono_object_class (obj), obj, TRUE, todo);*/
					}
				}
				break;
			case MONO_TYPE_VALUETYPE: {
				MonoClass *fclass = mono_class_from_mono_type (field->type);
				if (fclass->enumtype)
					continue;
				/*finalize_fields (fclass, data + field->offset, TRUE, todo);*/
				break;
			}
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				/* FIXME: foreach item... */
				break;
			}
		}
		if (!instance)
			return;
		class = class->parent;
	} while (class);
}

static void
finalize_static_data (MonoClass *class, MonoVTable *vtable, GHashTable *todo) {

	if (class->enumtype || !vtable->data)
		return;
	finalize_fields (class, vtable->data, FALSE, todo);
}

void
mono_domain_finalize (MonoDomain *domain) {

	GHashTable *todo = g_hash_table_new (NULL, NULL);
#if HAVE_BOEHM_GC
	GC_gcollect ();
#endif
	mono_g_hash_table_foreach (domain->class_vtable_hash, (GHFunc)finalize_static_data, todo);
	/* FIXME: finalize objects in todo... */
	g_hash_table_destroy (todo);
}

void
ves_icall_System_GC_InternalCollect (int generation)
{
#if HAVE_BOEHM_GC
	GC_gcollect ();
#endif
}

gint64
ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection)
{
#if HAVE_BOEHM_GC
	if (forceCollection)
		GC_gcollect ();
	return GC_get_heap_size ();
#else
	return 0;
#endif
}

void
ves_icall_System_GC_KeepAlive (MonoObject *obj)
{
	/*
	 * Does nothing.
	 */
}

void
ves_icall_System_GC_ReRegisterForFinalize (MonoObject *obj)
{
	object_register_finalizer (obj, run_finalize);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
	object_register_finalizer (obj, NULL);
}

void
ves_icall_System_GC_WaitForPendingFinalizers (void)
{
}

/*static CRITICAL_SECTION handle_section;*/
static guint32 next_handle = 0;
static gpointer *gc_handles = NULL;
static guint32 array_size = 0;

/*
 * The handle type is encoded in the lower two bits of the handle value:
 * 0 -> normal
 * 1 -> pinned
 * 2 -> weak
 */

/*
 * FIXME: make thread safe and reuse the array entries.
 */
MonoObject *
ves_icall_System_GCHandle_GetTarget (guint32 handle)
{
	MonoObject *obj;

	if (gc_handles) {
		obj = gc_handles [handle >> 2];
		if (!obj)
			return NULL;
		if ((handle & 0x3) > 1)
			return REVEAL_POINTER (obj);
		return obj;
	}
	return NULL;
}

typedef enum {
	HANDLE_WEAK,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED
} HandleType;

guint32
ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type)
{
	gpointer val = obj;
	guint32 h, idx = next_handle++;

	if (idx >= array_size) {
#if HAVE_BOEHM_GC
		gpointer *new_array;
		if (!array_size)
			array_size = 16;
		new_array = GC_malloc (sizeof (gpointer) * (array_size * 2));
		if (gc_handles) {
			int i;
			memcpy (new_array, gc_handles, sizeof (gpointer) * array_size);
			/* need to re-register links for weak refs. test if GC_realloc needs the same */
			for (i = 0; i < array_size; ++i) {
				if (((gulong)new_array [i]) & 0x1) { /* all and only disguised pointers have it set */
					GC_unregister_disappearing_link (&(gc_handles [i]));
					if (new_array [i] != (gpointer)-1)
						GC_general_register_disappearing_link (&(new_array [i]), REVEAL_POINTER (new_array [i]));
				}
			}
		}
		array_size *= 2;
		gc_handles = new_array;
#else
		g_error ("No GCHandle support built-in");
#endif
	}
	h = idx << 2;

	/* resuse the type from the old target */
	if (type == -1)
		type =  handle & 0x3;
	switch (type) {
	case HANDLE_WEAK:
	case HANDLE_WEAK_TRACK:
		h |= 2;
		val = (gpointer)HIDE_POINTER (val);
		gc_handles [idx] = val;
#if HAVE_BOEHM_GC
		GC_general_register_disappearing_link (&(gc_handles [idx]), obj);
#else
		g_error ("No weakref support");
#endif
		break;
	default:
		h |= type;
		gc_handles [idx] = val;
		break;
	}
	return h;
}

void
ves_icall_System_GCHandle_FreeHandle (guint32 handle)
{
	int idx = handle >> 2;

#ifdef HAVE_BOHEM_GC
	if ((handle & 0x3) > 1)
		GC_unregister_disappearing_link (&(gc_handles [idx]));
#else
	g_error ("No GCHandle support");
#endif

	gc_handles [idx] = (gpointer)-1;
}

gpointer
ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle)
{
	MonoObject *obj;

	if (gc_handles) {
		obj = gc_handles [handle >> 2];
		if ((handle & 0x3) > 1) {
			obj = REVEAL_POINTER (obj);
			if (obj == (MonoObject *) -1)
				return NULL;
		}
		return obj;
	}
	return NULL;
}


