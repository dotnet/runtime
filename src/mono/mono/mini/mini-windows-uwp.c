/**
 * \file
 * UWP profiler stat support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>

void
mono_runtime_setup_stat_profiler (void)
{
	g_unsupported_api ("OpenThread, GetThreadContext");
	SetLastError (ERROR_NOT_SUPPORTED);
	return;
}

void
mono_runtime_shutdown_stat_profiler (void)
{
	g_unsupported_api ("OpenThread, GetThreadContext");
	SetLastError (ERROR_NOT_SUPPORTED);
	return;
}

static gboolean
mono_setup_thread_context(DWORD thread_id, MonoContext *mono_context)
{
	memset (mono_context, 0, sizeof (MonoContext));
	return FALSE;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (mini_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

