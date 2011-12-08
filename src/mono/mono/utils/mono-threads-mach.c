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

#include <mono/utils/mach-support.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/threads-types.h>

#include <pthread.h>
#include <errno.h>

void
mono_threads_init_platform (void)
{	
}

void
mono_threads_core_interrupt (MonoThreadInfo *info)
{
	thread_abort (info->native_handle);
}

gboolean
mono_threads_core_suspend (MonoThreadInfo *info)
{
	kern_return_t ret;
	g_assert (info);

	ret = thread_suspend (info->native_handle);
	if (ret != KERN_SUCCESS)
		return FALSE;
	return mono_threads_get_runtime_callbacks ()->
		thread_state_init_from_handle (&info->suspend_state, mono_thread_info_get_tid (info), info->native_handle);
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
		uctx.uc_mcontext = mctx;
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

#endif
