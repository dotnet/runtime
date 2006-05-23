/*
 * null-gc.c: GC implementation using malloc: will leak everything, just for testing.
 *
 */

#include "config.h"
#include <glib.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internal.h>

#ifdef HAVE_NULL_GC

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
mono_gc_is_gc_thread (void)
{
	return TRUE;
}

gboolean
mono_gc_register_thread (void *baseptr)
{
	return TRUE;
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

void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj)
{
	*link_addr = obj;
}

void
mono_gc_weak_link_remove (void **link_addr)
{
	*link_addr = NULL;
}

MonoObject*
mono_gc_weak_link_get (void **link_addr)
{
	return *link_addr;
}

void*
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	return NULL;
}

void*
mono_gc_alloc_fixed (size_t size, void *descr)
{
	return g_malloc0 (size);
}

void
mono_gc_free_fixed (void* addr)
{
	g_free (addr);
}

void
mono_gc_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	*(void**)field_ptr = value;
}

void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	*(void**)slot_ptr = value;
}

void
mono_gc_wbarrier_arrayref_copy (MonoArray *arr, gpointer slot_ptr, int count)
{
	/* no need to do anything */
}

void
mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value)
{
	*(void**)ptr = value;
}

void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
}

#endif

