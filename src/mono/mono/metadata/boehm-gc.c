/*
 * boehm-gc.c: GC implementation using either the installed or included Boehm GC.
 *
 */

#include "config.h"
#include <mono/os/gc_wrapper.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/profiler-private.h>

#if HAVE_BOEHM_GC

void
mono_gc_collect (int generation)
{
	GC_gcollect ();
}

int
mono_gc_max_generation (void)
{
	return 0;
}

gint64
mono_gc_get_used_size (void)
{
	return GC_get_heap_size () - GC_get_free_bytes ();
}

gint64
mono_gc_get_heap_size (void)
{
	return GC_get_heap_size ();
}

void
mono_gc_disable (void)
{
#ifdef HAVE_GC_ENABLE
	GC_disable ();
#else
	g_assert_not_reached ();
#endif
}

void
mono_gc_enable (void)
{
#ifdef HAVE_GC_ENABLE
	GC_enable ();
#else
	g_assert_not_reached ();
#endif
}

extern int GC_is_marked (void *p);

gboolean
mono_object_is_alive (MonoObject* o)
{
#ifdef USE_INCLUDED_LIBGC
	return GC_is_marked (o);
#else
	return TRUE;
#endif
}

#ifdef USE_INCLUDED_LIBGC

static void
on_gc_notification (GCEventType event)
{
	mono_profiler_gc_event ((MonoGCEvent) event, 0);
}
 
static void
on_gc_heap_resize (size_t new_size)
{
	mono_profiler_gc_heap_resize (new_size);
}

void
mono_gc_enable_events (void)
{
	GC_notify_event = on_gc_notification;
	GC_on_heap_resize = on_gc_heap_resize;
}

#else

void
mono_gc_enable_events (void)
{
}

#endif

#endif /* no Boehm GC */

