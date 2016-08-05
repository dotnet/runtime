/*
 * mono-threads-posix-abort-syscall.c: Low-level syscall aborting
 *
 * Author:
 *	Ludovic Henry (ludovic@xamarin.com)
 *
 * (C) 2015 Xamarin, Inc
 */

#include "config.h"
#include <glib.h>

#if defined (__MACH__)
#define _DARWIN_C_SOURCE 1
#endif

#include "mono-threads.h"
#include "mono-threads-posix-signals.h"

#if defined(USE_POSIX_BACKEND)

void
mono_threads_abort_syscall_init (void)
{
	mono_threads_posix_init_signals (MONO_THREADS_POSIX_INIT_SIGNALS_ABORT);
}

void
mono_threads_suspend_abort_syscall (MonoThreadInfo *info)
{
	/* We signal a thread to break it from the current syscall.
	 * This signal should not be interpreted as a suspend request. */
	info->syscall_break_signal = TRUE;
	if (!mono_threads_pthread_kill (info, mono_threads_posix_get_abort_signal ()))
		mono_threads_add_to_pending_operation_set (info);
}

gboolean
mono_threads_suspend_needs_abort_syscall (void)
{
	return TRUE;
}

#endif
