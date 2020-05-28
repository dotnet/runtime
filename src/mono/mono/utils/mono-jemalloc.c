/**
 * \file
 *
 * Jemalloc registration code
 */

#include <glib.h>
#include <mono/utils/mono-jemalloc.h>

#ifdef MONO_JEMALLOC_ENABLED

void 
mono_init_jemalloc (void)
{
	GMemVTable g_mem_vtable = { MONO_JEMALLOC_MALLOC, MONO_JEMALLOC_REALLOC, MONO_JEMALLOC_FREE, MONO_JEMALLOC_CALLOC};
	g_mem_set_vtable (&g_mem_vtable);
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_jemalloc);

#endif
