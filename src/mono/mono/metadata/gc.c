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

#include <mono/metadata/gc-internal.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#define GC_I_HIDE_POINTERS
#include <mono/os/gc_wrapper.h>

#ifndef HIDE_POINTER
#define HIDE_POINTER(v)         (v)
#define REVEAL_POINTER(v)       (v)
#endif

static int finalize_slot = -1;

static void object_register_finalizer (MonoObject *obj, void (*callback)(void *, void*));

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

	/* make sure the finalizer is not called again if the object is resurrected */
	object_register_finalizer (obj, NULL);
	/* speedup later... and use a timeout */
	/*g_print ("Finalize run on %p %s.%s\n", o, mono_object_class (o)->name_space, mono_object_class (o)->name);*/
	mono_domain_set (mono_object_domain (o));

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
	GC_REGISTER_FINALIZER_NO_ORDER ((char*)obj - offset, callback, GUINT_TO_POINTER (offset), NULL, NULL);
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
	MONO_ARCH_SAVE_REGS;

#if HAVE_BOEHM_GC
	GC_gcollect ();
#endif
}

gint64
ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection)
{
	MONO_ARCH_SAVE_REGS;

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
	MONO_ARCH_SAVE_REGS;

	/*
	 * Does nothing.
	 */
}

void
ves_icall_System_GC_ReRegisterForFinalize (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	object_register_finalizer (obj, run_finalize);
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	object_register_finalizer (obj, NULL);
}

void
ves_icall_System_GC_WaitForPendingFinalizers (void)
{
	MONO_ARCH_SAVE_REGS;
}

static CRITICAL_SECTION handle_section;
static guint32 next_handle = 0;
static gpointer *gc_handles = NULL;
static guint8 *gc_handle_types = NULL;
static guint32 array_size = 0;

/*
 * The handle type is encoded in the lower two bits of the handle value:
 * 0 -> normal
 * 1 -> pinned
 * 2 -> weak
 */

typedef enum {
	HANDLE_WEAK,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED
} HandleType;

/*
 * FIXME: make thread safe and reuse the array entries.
 */
MonoObject *
ves_icall_System_GCHandle_GetTarget (guint32 handle)
{
	MonoObject *obj;
	gint32 type;

	MONO_ARCH_SAVE_REGS;

	if (gc_handles) {
		type = handle & 0x3;
		EnterCriticalSection (&handle_section);
		g_assert (type == gc_handle_types [handle >> 2]);
		obj = gc_handles [handle >> 2];
		LeaveCriticalSection (&handle_section);
		if (!obj)
			return NULL;

		if ((type == HANDLE_WEAK) || (type == HANDLE_WEAK_TRACK))
			return REVEAL_POINTER (obj);
		else
			return obj;
	}
	return NULL;
}

guint32
ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type)
{
	gpointer val = obj;
	guint32 h, idx;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection (&handle_section);
	idx = next_handle++;
	if (idx >= array_size) {
#if HAVE_BOEHM_GC
		gpointer *new_array;
		guint8 *new_type_array;
		if (!array_size)
			array_size = 16;
		new_array = GC_MALLOC (sizeof (gpointer) * (array_size * 2));
		new_type_array = GC_MALLOC (sizeof (guint8) * (array_size * 2));
		if (gc_handles) {
			int i;
			memcpy (new_array, gc_handles, sizeof (gpointer) * array_size);
			memcpy (new_type_array, gc_handle_types, sizeof (guint8) * array_size);
			/* need to re-register links for weak refs. test if GC_realloc needs the same */
			for (i = 0; i < array_size; ++i) {
#if 0 /* This breaks the threaded finalizer, by causing segfaults deep
       * inside libgc.  I assume it will also break without the
       * threaded finalizer, just that the stress test (bug 31333)
       * deadlocks too early without it.  Reverting to the previous
       * version here stops the segfault.
       */
				if ((gc_handle_types[i] == HANDLE_WEAK) || (gc_handle_types[i] == HANDLE_WEAK_TRACK)) { /* all and only disguised pointers have it set */
#else
				if (((gulong)new_array [i]) & 0x1) {
#endif
					if (gc_handles [i] != (gpointer)-1)
						GC_unregister_disappearing_link (&(gc_handles [i]));
					if (new_array [i] != (gpointer)-1)
						GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(new_array [i]), REVEAL_POINTER (new_array [i]));
				}
			}
		}
		array_size *= 2;
		gc_handles = new_array;
		gc_handle_types = new_type_array;
#else
		g_error ("No GCHandle support built-in");
#endif
	}

	/* resuse the type from the old target */
	if (type == -1)
		type =  handle & 0x3;
	h = (idx << 2) | type;
	switch (type) {
	case HANDLE_WEAK:
	case HANDLE_WEAK_TRACK:
		val = (gpointer)HIDE_POINTER (val);
		gc_handles [idx] = val;
		gc_handle_types [idx] = type;
#if HAVE_BOEHM_GC
		if (gc_handles [idx] != (gpointer)-1)
			GC_GENERAL_REGISTER_DISAPPEARING_LINK (&(gc_handles [idx]), obj);
#else
		g_error ("No weakref support");
#endif
		break;
	default:
		gc_handles [idx] = val;
		gc_handle_types [idx] = type;
		break;
	}
	LeaveCriticalSection (&handle_section);
	return h;
}

void
ves_icall_System_GCHandle_FreeHandle (guint32 handle)
{
	int idx = handle >> 2;
	int type = handle & 0x3;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection (&handle_section);

#ifdef HAVE_BOEHM_GC
	g_assert (type == gc_handle_types [idx]);
	if ((type == HANDLE_WEAK) || (type == HANDLE_WEAK_TRACK)) {
		if (gc_handles [idx] != (gpointer)-1)
			GC_unregister_disappearing_link (&(gc_handles [idx]));
	}
#else
	g_error ("No GCHandle support");
#endif

	gc_handles [idx] = (gpointer)-1;
	gc_handle_types [idx] = (guint8)-1;
	LeaveCriticalSection (&handle_section);
}

gpointer
ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle)
{
	MonoObject *obj;
	int type = handle & 0x3;

	MONO_ARCH_SAVE_REGS;

	if (gc_handles) {
		EnterCriticalSection (&handle_section);
		obj = gc_handles [handle >> 2];
		g_assert (gc_handle_types [handle >> 2] == type);
		LeaveCriticalSection (&handle_section);
		if ((type == HANDLE_WEAK) || (type == HANDLE_WEAK_TRACK)) {
			obj = REVEAL_POINTER (obj);
			if (obj == (MonoObject *) -1)
				return NULL;
		}
		return obj;
	}
	return NULL;
}

#if HAVE_BOEHM_GC

static HANDLE finalizer_event;
static volatile gboolean finished=FALSE;

static void finalize_notify (void)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": prodding finalizer");
#endif

	SetEvent (finalizer_event);
}

static guint32 finalizer_thread (gpointer unused)
{
	guint32 stack_start;
	
	mono_new_thread_init (NULL, &stack_start);
	
	while(!finished) {
		/* Wait to be notified that there's at least one
		 * finaliser to run
		 */
		WaitForSingleObject (finalizer_event, INFINITE);

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": invoking finalizers");
#endif

		GC_invoke_finalizers ();
	}
	
	return(0);
}

/* 
 * Enable or disable the separate finalizer thread.
 * It's currently disabled because it still requires some
 * work in the rest of the runtime.
 */
#undef ENABLE_FINALIZER_THREAD

void mono_gc_init (void)
{
	HANDLE gc_thread;

	InitializeCriticalSection (&handle_section);

#ifdef ENABLE_FINALIZER_THREAD

	if (getenv ("GC_DONT_GC"))
		return;
	
	finalizer_event = CreateEvent (NULL, FALSE, FALSE, NULL);
	if (finalizer_event == NULL) {
		g_assert_not_reached ();
	}

	GC_finalize_on_demand = 1;
	GC_finalizer_notifier = finalize_notify;
	
	/* Don't use mono_thread_create here, because we don't want
	 * the runtime to wait for this thread to exit when it's
	 * cleaning up.
	 */
	gc_thread = CreateThread (NULL, 0, finalizer_thread, NULL, 0, NULL);
	if (gc_thread == NULL) {
		g_assert_not_reached ();
	}
#endif
}

void mono_gc_cleanup (void)
{
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": cleaning up finalizer");
#endif

#ifdef ENABLE_FINALIZER_THREAD
	finished = TRUE;
	finalize_notify ();
#endif
}

#else

/* no Boehm GC support. */
void mono_gc_init (void)
{
	InitializeCriticalSection (&handle_section);
}

void mono_gc_cleanup (void)
{
}

#endif

