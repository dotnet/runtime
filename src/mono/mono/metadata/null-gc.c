/*
 * null-gc.c: GC implementation using malloc: will leak everything, just for testing.
 *
 */

#include "config.h"
#include <glib.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internal.h>

#ifndef HAVE_BOEHM_GC

void
mono_gc_collect (int generation)
{
}

int
mono_gc_max_generation (void)
{
	return 0;
}

/* maybe track the size, not important, though */
gint64
mono_gc_get_used_size (void)
{
	return 1024*1024;
}

gint64
mono_gc_get_heap_size (void)
{
	return 2*1024*1024;
}

void
mono_gc_disable (void)
{
}

void
mono_gc_enable (void)
{
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	return TRUE;
}

void
mono_gc_enable_events (void)
{
}

#endif

