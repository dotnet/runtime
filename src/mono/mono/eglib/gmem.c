/*
 * gmem.c: memory utility functions
 *
 * Author:
 * 	Gonzalo Paniagua Javier (gonzalo@novell.com)
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>
#include <eglib-remap.h> // Remove the cast macros and restore the rename macros.
#undef malloc
#undef realloc
#undef free
#undef calloc

#if defined (ENABLE_OVERRIDABLE_ALLOCATORS)

static GMemVTable sGMemVTable = { malloc, realloc, free, calloc };

void
g_mem_set_vtable (GMemVTable* vtable)
{
	sGMemVTable.calloc = vtable->calloc ? vtable->calloc : calloc;
	sGMemVTable.realloc = vtable->realloc ? vtable->realloc : realloc;
	sGMemVTable.malloc = vtable->malloc ? vtable->malloc : malloc;
	sGMemVTable.free = vtable->free ? vtable->free : free;
}

void
g_mem_get_vtable (GMemVTable* vtable)
{
	*vtable = sGMemVTable;
}

#define G_FREE_INTERNAL sGMemVTable.free
#define G_REALLOC_INTERNAL sGMemVTable.realloc
#define G_CALLOC_INTERNAL sGMemVTable.calloc
#define G_MALLOC_INTERNAL sGMemVTable.malloc
#else

void
g_mem_get_vtable (GMemVTable* vtable)
{
	memset (vtable, 0, sizeof (*vtable));
}

void
g_mem_set_vtable (GMemVTable* vtable)
{
}

#define G_FREE_INTERNAL free
#define G_REALLOC_INTERNAL realloc
#define G_CALLOC_INTERNAL calloc
#define G_MALLOC_INTERNAL malloc
#endif
void
g_free (void *ptr)
{
	if (ptr != NULL)
		G_FREE_INTERNAL (ptr);
}

gpointer
g_memdup (gconstpointer mem, guint byte_size)
{
	gpointer ptr;

	if (mem == NULL)
		return NULL;

	ptr = g_malloc (byte_size);
	if (ptr != NULL)
		memcpy (ptr, mem, byte_size);

	return ptr;
}

gpointer g_realloc (gpointer obj, gsize size)
{
	gpointer ptr;
	if (!size) {
		g_free (obj);
		return 0;
	}
	ptr = G_REALLOC_INTERNAL (obj, size);
	if (ptr)
		return ptr;
	g_error ("Could not allocate %i bytes", size);
}

gpointer 
g_malloc (gsize x) 
{ 
	gpointer ptr;
	if (!x)
		return 0;
	ptr = G_MALLOC_INTERNAL (x);
	if (ptr) 
		return ptr;
	g_error ("Could not allocate %i bytes", x);
}

gpointer g_calloc (gsize n, gsize x)
{
	gpointer ptr;
	if (!x || !n)
		return 0;
	ptr = G_CALLOC_INTERNAL (n, x);
	if (ptr)
		return ptr;
	g_error ("Could not allocate %i (%i * %i) bytes", x*n, n, x);
}
gpointer g_malloc0 (gsize x) 
{ 
	return g_calloc (1,x);
}

gpointer g_try_malloc (gsize x) 
{
	if (x)
		return G_MALLOC_INTERNAL (x);
	return 0;
}


gpointer g_try_realloc (gpointer obj, gsize size)
{ 
	if (!size) {
		G_FREE_INTERNAL (obj);
		return 0;
	} 
	return G_REALLOC_INTERNAL (obj, size);
}
