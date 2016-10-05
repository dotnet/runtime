/*
 * mini-windows-uwp.c: UWP profiler stat support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <Windows.h>

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

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_mini_windows_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

