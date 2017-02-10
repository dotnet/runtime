/**
 * \file
 * UWP marshal support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include "mono/metadata/marshal-windows-internals.h"

void *
mono_marshal_alloc_hglobal (size_t size)
{
	return HeapAlloc (GetProcessHeap (), 0, size);
}

gpointer
mono_marshal_realloc_hglobal (gpointer ptr, size_t size)
{
	return HeapReAlloc (GetProcessHeap (), 0, ptr, size);
}

void
mono_marshal_free_hglobal (gpointer ptr)
{
	HeapFree (GetProcessHeap (), 0, ptr);
	return;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (marshal_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
