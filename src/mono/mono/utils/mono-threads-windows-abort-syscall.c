/*
 * mono-threads-windows-abort-syscall.c: Low-level syscall aborting
 *
 * Author:
 *	Ludovic Henry (ludovic@xamarin.com)
 *
 * (C) 2015 Xamarin, Inc
 */

#include "config.h"
#include <glib.h>

#include <mono/utils/mono-threads.h>

#if defined(USE_WINDOWS_BACKEND)

#include <limits.h>

void
mono_threads_init_abort_syscall (void)
{
}

static void CALLBACK
abort_apc (ULONG_PTR param)
{
}

void
mono_threads_core_abort_syscall (MonoThreadInfo *info)
{
	DWORD id = mono_thread_info_get_tid (info);
	HANDLE handle;

	handle = OpenThread (THREAD_ALL_ACCESS, FALSE, id);
	g_assert (handle);

	QueueUserAPC ((PAPCFUNC)abort_apc, handle, (ULONG_PTR)NULL);

	CloseHandle (handle);
}

gboolean
mono_threads_core_needs_abort_syscall (void)
{
	return TRUE;
}

#endif
