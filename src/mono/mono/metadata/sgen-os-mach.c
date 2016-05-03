/*
 * sgen-os-mach.c: Mach-OS support.
 *
 * Author:
 *	Paolo Molaro (lupus@ximian.com)
 *	Mark Probst (mprobst@novell.com)
 * 	Geoff Norton (gnorton@novell.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC


#include <glib.h>
#include "sgen/sgen-gc.h"
#include "sgen/sgen-archdep.h"
#include "sgen/sgen-protocol.h"
#include "sgen/sgen-thread-pool.h"
#include "metadata/object-internals.h"
#include "metadata/gc-internals.h"

#if defined(__MACH__)
#include "utils/mach-support.h"
#endif

#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED

#if !defined(USE_COOP_GC)
gboolean
sgen_resume_thread (SgenThreadInfo *info)
{
	kern_return_t ret;
	do {
		ret = thread_resume (info->client_info.info.native_handle);
	} while (ret == KERN_ABORTED);
	return ret == KERN_SUCCESS;
}

gboolean
sgen_suspend_thread (SgenThreadInfo *info)
{
	mach_msg_type_number_t num_state;
	thread_state_t state;
	kern_return_t ret;
	ucontext_t ctx;
	mcontext_t mctx;

	gpointer stack_start;

	state = (thread_state_t) alloca (mono_mach_arch_get_thread_state_size ());
	mctx = (mcontext_t) alloca (mono_mach_arch_get_mcontext_size ());

	do {
		ret = thread_suspend (info->client_info.info.native_handle);
	} while (ret == KERN_ABORTED);
	if (ret != KERN_SUCCESS)
		return FALSE;

	do {
		ret = mono_mach_arch_get_thread_state (info->client_info.info.native_handle, state, &num_state);
	} while (ret == KERN_ABORTED);
	if (ret != KERN_SUCCESS)
		return FALSE;

	mono_mach_arch_thread_state_to_mcontext (state, mctx);
	ctx.uc_mcontext = mctx;

	info->client_info.stopped_domain = mono_thread_info_tls_get (info, TLS_KEY_DOMAIN);
	info->client_info.stopped_ip = (gpointer) mono_mach_arch_get_ip (state);
	info->client_info.stack_start = NULL;
	stack_start = (char*) mono_mach_arch_get_sp (state) - REDZONE_SIZE;
	/* If stack_start is not within the limits, then don't set it in info and we will be restarted. */
	if (stack_start >= info->client_info.stack_start_limit && stack_start <= info->client_info.stack_end) {
		info->client_info.stack_start = stack_start;

		mono_sigctx_to_monoctx (&ctx, &info->client_info.ctx);
	} else {
		g_assert (!info->client_info.stack_start);
	}

	/* Notify the JIT */
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->client_info.runtime_data, &ctx, NULL);

	SGEN_LOG (2, "thread %p stopped at %p stack_start=%p", (void*)(gsize)info->client_info.info.native_handle, info->client_info.stopped_ip, info->client_info.stack_start);
	binary_protocol_thread_suspend ((gpointer)mono_thread_info_get_tid (info), info->client_info.stopped_ip);

	return TRUE;
}

void
sgen_wait_for_suspend_ack (int count)
{
    /* mach thread_resume is synchronous so we dont need to wait for them */
}

/* LOCKING: assumes the GC lock is held */
int
sgen_thread_handshake (BOOL suspend)
{
	SgenThreadInfo *cur_thread = mono_thread_info_current ();
	kern_return_t ret;

	int count = 0;

	cur_thread->client_info.suspend_done = TRUE;
	FOREACH_THREAD (info) {
		if (info == cur_thread || sgen_thread_pool_is_thread_pool_thread (mono_thread_info_get_tid (info)))
			continue;

		info->client_info.suspend_done = FALSE;
		if (info->client_info.gc_disabled)
			continue;

		if (suspend) {
			if (!sgen_suspend_thread (info))
				continue;
		} else {
			do {
				ret = thread_resume (info->client_info.info.native_handle);
			} while (ret == KERN_ABORTED);
			if (ret != KERN_SUCCESS)
				continue;
		}
		count ++;
	} FOREACH_THREAD_END
	return count;
}

void
sgen_os_init (void)
{
}

int
mono_gc_get_suspend_signal (void)
{
	return -1;
}

int
mono_gc_get_restart_signal (void)
{
	return -1;
}
#endif
#endif
#endif
