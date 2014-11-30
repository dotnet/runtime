/*
 * mono-threads-mach.c: Low-level threading, mach version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include "config.h"

#if defined(__MACH__)

/* For pthread_main_np, pthread_get_stackaddr_np and pthread_get_stacksize_np */
#define _DARWIN_C_SOURCE 1

#include <mono/utils/mach-support.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-mmap.h>

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
}

gboolean
mono_threads_core_needs_abort_syscall (void)
{
	return FALSE;
}

gboolean
mono_threads_core_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	kern_return_t ret;
	gboolean res;

	g_assert (info);

	ret = thread_suspend (info->native_handle);
	if (ret != KERN_SUCCESS)
		return FALSE;
	res = mono_threads_get_runtime_callbacks ()->
		thread_state_init_from_handle (&info->suspend_state, info);
	if (!res)
		thread_resume (info->native_handle);
	return res;
}

gboolean
mono_threads_core_resume (MonoThreadInfo *info)
{
	kern_return_t ret;

	if (info->async_target) {
		MonoContext tmp = info->suspend_state.ctx;
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
