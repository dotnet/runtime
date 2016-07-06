/*
 * mono-threads-mach-abort-syscall.c: Low-level syscall aborting
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

#include <mono/utils/mono-threads.h>

#if defined(USE_MACH_BACKEND)

#if defined(HOST_WATCHOS) || defined(HOST_TVOS)

void
mono_threads_abort_syscall_init (void)
{
}

void
mono_threads_suspend_abort_syscall (MonoThreadInfo *info)
{

}

gboolean
mono_threads_suspend_needs_abort_syscall (void)
{
	return FALSE;
}

#else

void
mono_threads_abort_syscall_init (void)
{
}

void
mono_threads_suspend_abort_syscall (MonoThreadInfo *info)
{
	kern_return_t ret;

	do {
		ret = thread_suspend (info->native_handle);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		return;

	do {
		ret = thread_abort_safely (info->native_handle);
	} while (ret == KERN_ABORTED);

	/*
	 * We are doing thread_abort when thread_abort_safely returns KERN_SUCCESS because
	 * for some reason accept is not interrupted by thread_abort_safely.
	 * The risk of aborting non-atomic operations while calling thread_abort should not
	 * exist because by the time thread_abort_safely returns KERN_SUCCESS the target
	 * thread should have return from the kernel and should be waiting for thread_resume
	 * to resume the user code.
	 */
	if (ret == KERN_SUCCESS)
		ret = thread_abort (info->native_handle);

	do {
		ret = thread_resume (info->native_handle);
	} while (ret == KERN_ABORTED);

	g_assert (ret == KERN_SUCCESS);
}

gboolean
mono_threads_suspend_needs_abort_syscall (void)
{
	return TRUE;
}

#endif

#endif
