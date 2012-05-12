/*
 * sgen-os-mach.c: Simple generational GC.
 *
 * Author:
 *	Paolo Molaro (lupus@ximian.com)
 *	Mark Probst (mprobst@novell.com)
 * 	Geoff Norton (gnorton@novell.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC


#include <glib.h>
#include "metadata/sgen-gc.h"
#include "metadata/sgen-archdep.h"
#include "metadata/object-internals.h"
#include "metadata/gc-internal.h"

#if defined(__MACH__)
#include "utils/mach-support.h"
#endif

#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED
gboolean
sgen_resume_thread (SgenThreadInfo *info)
{
	return thread_resume (info->mach_port) == KERN_SUCCESS;
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

	ret = thread_suspend (info->mach_port);
	if (ret != KERN_SUCCESS)
		return FALSE;

	ret = mono_mach_arch_get_thread_state (info->mach_port, state, &num_state);
	if (ret != KERN_SUCCESS)
		return FALSE;

	mono_mach_arch_thread_state_to_mcontext (state, mctx);
	ctx.uc_mcontext = mctx;

	info->stopped_domain = mono_mach_arch_get_tls_value_from_thread (
		mono_thread_info_get_tid (info), mono_domain_get_tls_offset ());
	info->stopped_ip = (gpointer) mono_mach_arch_get_ip (state);
	stack_start = (char*) mono_mach_arch_get_sp (state) - REDZONE_SIZE;
	/* If stack_start is not within the limits, then don't set it in info and we will be restarted. */
	if (stack_start >= info->stack_start_limit && info->stack_start <= info->stack_end) {
		info->stack_start = stack_start;

#ifdef USE_MONO_CTX
		mono_sigctx_to_monoctx (&ctx, &info->ctx);
		info->monoctx = &info->ctx;
#else
		ARCH_COPY_SIGCTX_REGS (&info->regs, &ctx);
		info->stopped_regs = &info->regs;
#endif
	} else {
		g_assert (!info->stack_start);
	}

	/* Notify the JIT */
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, &ctx);

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
	SgenThreadInfo *info;

	int count = 0;

	FOREACH_THREAD_SAFE (info) {
		info->joined_stw = suspend;

		if (info == cur_thread || sgen_is_worker_thread (mono_thread_info_get_tid (info)))
			continue;
		if (info->gc_disabled)
			continue;

		if (suspend) {
			g_assert (!info->doing_handshake);
			info->doing_handshake = TRUE;

			if (!sgen_suspend_thread (info))
				continue;
		} else {
			g_assert (info->doing_handshake);
			info->doing_handshake = FALSE;

			ret = thread_resume (info->mach_port);
			if (ret != KERN_SUCCESS)
				continue;
		}
		count ++;
	} END_FOREACH_THREAD_SAFE
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
#endif
#endif
