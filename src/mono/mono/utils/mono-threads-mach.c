/*
 * mono-threads-mach.c: Low-level threading, mach version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include "config.h"

/* For pthread_main_np, pthread_get_stackaddr_np and pthread_get_stacksize_np */
#if defined (__MACH__)
#define _DARWIN_C_SOURCE 1
#endif

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-mmap.h>

#if defined (USE_MACH_BACKEND)

#include <mono/utils/mach-support.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/hazard-pointer.h>

void
mono_threads_suspend_init (void)
{
	mono_threads_init_dead_letter ();
}

#if defined(HOST_WATCHOS) || defined(HOST_TVOS)

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	g_assert_not_reached ();
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	g_assert_not_reached ();
}

gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	g_assert_not_reached ();
}

#else /* defined(HOST_WATCHOS) || defined(HOST_TVOS) */

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	kern_return_t ret;

	g_assert (info);


	do {
		ret = thread_suspend (info->native_handle);
	} while (ret == KERN_ABORTED);

	THREADS_SUSPEND_DEBUG ("SUSPEND %p -> %d\n", (gpointer)(gsize)info->native_handle, ret);
	if (ret != KERN_SUCCESS)
		return FALSE;

	/* We're in the middle of a self-suspend, resume and register */
	if (!mono_threads_transition_finish_async_suspend (info)) {
		mono_threads_add_to_pending_operation_set (info);
		do {
			ret = thread_resume (info->native_handle);
		} while (ret == KERN_ABORTED);
		g_assert (ret == KERN_SUCCESS);
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/1 %p -> %d\n", (gpointer)(gsize)info->native_handle, 0);
		//XXX interrupt_kernel doesn't make sense in this case as the target is not in a syscall
		return TRUE;
	}
	info->suspend_can_continue = mono_threads_get_runtime_callbacks ()->
		thread_state_init_from_handle (&info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX], info);
	THREADS_SUSPEND_DEBUG ("thread state %p -> %d\n", (gpointer)(gsize)info->native_handle, res);
	if (info->suspend_can_continue) {
		if (interrupt_kernel)
			thread_abort (info->native_handle);
	} else {
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/2 %p -> %d\n", (gpointer)(gsize)info->native_handle, 0);
	}
	return info->suspend_can_continue;
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	return info->suspend_can_continue;
}

gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	kern_return_t ret;

	if (info->async_target) {
		MonoContext tmp = info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX].ctx;
		mach_msg_type_number_t num_state;
		thread_state_t state;
		ucontext_t uctx;
		mcontext_t mctx;

		mono_threads_get_runtime_callbacks ()->setup_async_callback (&tmp, info->async_target, info->user_data);
		info->user_data = NULL;
		info->async_target = (void (*)(void *)) info->user_data;

		state = (thread_state_t) alloca (mono_mach_arch_get_thread_state_size ());
		mctx = (mcontext_t) alloca (mono_mach_arch_get_mcontext_size ());

		do {
			ret = mono_mach_arch_get_thread_state (info->native_handle, state, &num_state);
		} while (ret == KERN_ABORTED);

		if (ret != KERN_SUCCESS)
			return FALSE;

		mono_mach_arch_thread_state_to_mcontext (state, mctx);
		uctx.uc_mcontext = mctx;
		mono_monoctx_to_sigctx (&tmp, &uctx);

		mono_mach_arch_mcontext_to_thread_state (mctx, state);

		do {
			ret = mono_mach_arch_set_thread_state (info->native_handle, state, num_state);
		} while (ret == KERN_ABORTED);

		if (ret != KERN_SUCCESS)
			return FALSE;
	}

	do {
		ret = thread_resume (info->native_handle);
	} while (ret == KERN_ABORTED);
	THREADS_SUSPEND_DEBUG ("RESUME %p -> %d\n", (gpointer)(gsize)info->native_handle, ret);

	return ret == KERN_SUCCESS;
}

#endif /* defined(HOST_WATCHOS) || defined(HOST_TVOS) */

void
mono_threads_suspend_register (MonoThreadInfo *info)
{
	char thread_name [64];

	info->native_handle = mach_thread_self ();

	snprintf (thread_name, sizeof (thread_name), "tid_%x", (int) info->native_handle);
	pthread_setname_np (thread_name);

	mono_threads_install_dead_letter ();
}

void
mono_threads_suspend_free (MonoThreadInfo *info)
{
	mach_port_deallocate (current_task (), info->native_handle);
}

#endif /* USE_MACH_BACKEND */

#ifdef __MACH__
void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	*staddr = (guint8*)pthread_get_stackaddr_np (pthread_self());
	*stsize = pthread_get_stacksize_np (pthread_self());

#ifdef TARGET_OSX
	/*
	 * Mavericks reports stack sizes as 512kb:
	 * http://permalink.gmane.org/gmane.comp.java.openjdk.hotspot.devel/11590
	 * https://bugs.openjdk.java.net/browse/JDK-8020753
	 */
	if (pthread_main_np () && *stsize == 512 * 1024)
		*stsize = 2048 * mono_pagesize ();
#endif

	/* staddr points to the start of the stack, not the end */
	*staddr -= *stsize;
}

#endif
