/*
 * metadata/gc.c: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/gc.h>
#include <mono/metadata/threads.h>
#if HAVE_BOEHM_GC
#include <gc/gc.h>
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
	/* speedup later... */
	/* g_print ("Finalize run on %s\n", mono_object_class (o)->name); */
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
	object_register_finalizer (obj, run_finalize);
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

