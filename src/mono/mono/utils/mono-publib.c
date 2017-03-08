/**
 * \file
 */

#include "config.h"
#include <mono/utils/mono-publib.h>
#include <glib.h>

void
mono_free (void *ptr)
{
	g_free (ptr);
}


/**
 * mono_set_allocator_vtable
 * Make the runtime use the functions in \p vtable for allocating memory.
 * The provided functions must have the same semantics of their libc's equivalents.
 * \returns TRUE if the vtable was installed. FALSE if the version is incompatible.
 */
mono_bool
mono_set_allocator_vtable (MonoAllocatorVTable* vtable)
{
	if (vtable->version != MONO_ALLOCATOR_VTABLE_VERSION)
		return FALSE;
	GMemVTable g_mem_vtable = { vtable->malloc, vtable->realloc, vtable->free, vtable->calloc};
	g_mem_set_vtable (&g_mem_vtable);
	return TRUE;
}
