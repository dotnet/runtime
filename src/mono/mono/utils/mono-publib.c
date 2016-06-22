#include "config.h"
#include <mono/utils/mono-publib.h>
#include <glib.h>

void
mono_free (void *ptr)
{
	g_free (ptr);
}

void
mono_set_allocator_vtable (MonoAllocatorVTable* vtable)
{
	GMemVTable g_mem_vtable = { vtable->malloc, vtable->realloc, vtable->free, vtable->calloc};
	g_mem_set_vtable (&g_mem_vtable);
}
