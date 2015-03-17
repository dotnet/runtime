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
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/hazard-pointer.h>

void
mono_threads_init_platform (void)
{
	mono_threads_init_dead_letter ();
}

void
mono_threads_core_interrupt (MonoThreadInfo *info)
{
	thread_abort (info->native_handle);
}

void
mono_threads_core_abort_syscall (MonoThreadInfo *info)
{
	kern_return_t ret;

	ret = thread_suspend (info->native_handle);
	if (ret != KERN_SUCCESS)
		return;

	ret = thread_abort_safely (info->native_handle);

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

	g_assert (thread_resume (info->native_handle) == KERN_SUCCESS);
}

gboolean
mono_threads_core_needs_abort_syscall (void)
{
	return TRUE;
}

gboolean
mono_threads_core_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	kern_return_t ret;
	gboolean res;

	g_assert (info);

	ret = thread_suspend (info->native_handle);
	THREADS_SUSPEND_DEBUG ("SUSPEND %p -> %d\n", (void*)info->native_handle, ret);
	if (ret != KERN_SUCCESS)
		return FALSE;

	/* We're in the middle of a self-suspend, resume and register */
	if (!mono_threads_transition_finish_async_suspend (info)) {
		mono_threads_add_to_pending_operation_set (info);
		g_assert (thread_resume (info->native_handle) == KERN_SUCCESS);
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/1 %p -> %d\n", (void*)info->native_handle, 0);
		//XXX interrupt_kernel doesn't make sense in this case as the target is not in a syscall
		return TRUE;
	}
	res = mono_threads_get_runtime_callbacks ()->
		thread_state_init_from_handle (&info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX], info);
	THREADS_SUSPEND_DEBUG ("thread state %p -> %d\n", (void*)info->native_handle, res);
	if (res) {
		if (interrupt_kernel)
			thread_abort (info->native_handle);
	} else {
		mono_threads_transition_async_suspend_compensation (info);
		g_assert (thread_resume (info->native_handle) == KERN_SUCCESS);
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/2 %p -> %d\n", (void*)info->native_handle, 0);
	}
	return res;
}

gboolean
mono_threads_core_check_suspend_result (MonoThreadInfo *info)
{
	return TRUE;
}

gboolean
mono_threads_core_begin_async_resume (MonoThreadInfo *info)
{
	kern_return_t ret;

	if (info->async_target) {
		MonoContext tmp = info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX].ctx;
		mach_msg_type_number_t num_state;
		thread_state_t state;
		ucontext_t uctx;
		mcontext_t mctx;

		mono_threads_get_runtime_callbacks ()->setup_async_callback (&tmp, info->async_target, info->user_data);
		info->async_target = info->user_data = NULL;

		state = (thread_state_t) alloca (mono_mach_arch_get_thread_state_size ());
		mctx = (mcontext_t) alloca (mono_mach_arch_get_mcontext_size ());

		ret = mono_mach_arch_get_thread_state (info->native_handle, state, &num_state);
		if (ret != KERN_SUCCESS)
			return FALSE;

		mono_mach_arch_thread_state_to_mcontext (state, mctx);
#ifdef TARGET_ARM64
		g_assert_not_reached ();
#else
		uctx.uc_mcontext = mctx;
#endif
		mono_monoctx_to_sigctx (&tmp, &uctx);

		mono_mach_arch_mcontext_to_thread_state (mctx, state);

		ret = mono_mach_arch_set_thread_state (info->native_handle, state, num_state);
		if (ret != KERN_SUCCESS)
			return FALSE;
	}

	ret = thread_resume (info->native_handle);
	THREADS_SUSPEND_DEBUG ("RESUME %p -> %d\n", (void*)info->native_handle, ret);

	return ret == KERN_SUCCESS;
}

void
mono_threads_platform_register (MonoThreadInfo *info)
{
	info->native_handle = mach_thread_self ();
	mono_threads_install_dead_letter ();
}

void
mono_threads_platform_free (MonoThreadInfo *info)
{
	mach_port_deallocate (current_task (), info->native_handle);
}

MonoNativeThreadId
mono_native_thread_id_get (void)
{
	return pthread_self ();
}

gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return pthread_equal (id1, id2);
}

/*
 * mono_native_thread_create:
 *
 *   Low level thread creation function without any GC wrappers.
 */
gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg)
{
	return pthread_create (tid, NULL, func, arg) == 0;
}

void
mono_threads_core_set_name (MonoNativeThreadId tid, const char *name)
{
	/* pthread_setnmae_np() on Mac is not documented and doesn't receive thread id. */
}
#endif /* USE_MACH_BACKEND */

#ifdef __MACH__
void
mono_threads_core_get_stack_bounds (guint8 **staddr, size_t *stsize)
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
