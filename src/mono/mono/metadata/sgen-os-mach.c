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
#include "metadata/gc-internal.h"
#include "metadata/sgen-gc.h"
#include "metadata/sgen-archdep.h"
#include "metadata/object-internals.h"

#if defined(__MACH__)
#include "utils/mach-support.h"
#endif

/* LOCKING: assumes the GC lock is held */
#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED
int
mono_sgen_thread_handshake (int signum)
{
	SgenThreadInfo *cur_thread = mono_sgen_thread_info_current ();
	mach_msg_type_number_t num_state;
	thread_state_t state;
	kern_return_t ret;
	ucontext_t ctx;
	mcontext_t mctx;

	SgenThreadInfo *info;
	gpointer stack_start;

	int count = 0;

	state = (thread_state_t) alloca (mono_mach_arch_get_thread_state_size ());
	mctx = (mcontext_t) alloca (mono_mach_arch_get_mcontext_size ());

	FOREACH_THREAD (info) {
		if (info == cur_thread || mono_sgen_is_worker_thread (info->id))
			continue;

		if (signum == suspend_signal_num) {
			ret = thread_suspend (info->mach_port);
			if (ret != KERN_SUCCESS)
				continue;

			ret = mono_mach_arch_get_thread_state (info->mach_port, state, &num_state);
			if (ret != KERN_SUCCESS)
				continue;

			mono_mach_arch_thread_state_to_mcontext (state, mctx);
			ctx.uc_mcontext = mctx;

			info->stopped_domain = mono_mach_arch_get_tls_value_from_thread ((pthread_t)info->id, mono_pthread_key_for_tls (mono_domain_get_tls_key ()));
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
		} else {
			ret = thread_resume (info->mach_port);
			if (ret != KERN_SUCCESS)
				continue;
		}
		count ++;
	} END_FOREACH_THREAD
	return count;
}
#endif
#endif
