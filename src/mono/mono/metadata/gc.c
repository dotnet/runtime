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
	MonoObject *o = obj;

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
	/*
	 * Disabled: it seems to be called too early.
	 * g_print ("finalizer is run on %s\n", mono_object_class(o)->name);
	mono_runtime_invoke (o->vtable->klass->vtable [finalize_slot], obj, NULL);*/
}

void
mono_object_register_finalizer (MonoObject *obj)
{
#if HAVE_BOEHM_GC
	GC_register_finalizer (obj, run_finalize, NULL, NULL, NULL);
#endif
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
#if HAVE_BOEHM_GC
	GC_register_finalizer (obj, run_finalize, NULL, NULL, NULL);
#endif
}

void
ves_icall_System_GC_SuppressFinalize (MonoObject *obj)
{
#if HAVE_BOEHM_GC
	GC_register_finalizer (obj, NULL, NULL, NULL, NULL);
#endif
}

void
ves_icall_System_GC_WaitForPendingFinalizers (void)
{
}

