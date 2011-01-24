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
	task_t task = current_task ();
	thread_port_t cur_thread = mach_thread_self ();
	thread_act_array_t thread_list;
	mach_msg_type_number_t num_threads;
	mach_msg_type_number_t num_state;
	thread_state_t state;
	kern_return_t ret;
	ucontext_t ctx;
	mcontext_t mctx;
	pthread_t exception_thread = mono_gc_get_mach_exception_thread ();

	SgenThreadInfo *info;
	gpointer regs [ARCH_NUM_REGS];
	gpointer stack_start;

	int count, i;

	mono_mach_get_threads (&thread_list, &num_threads);

	for (i = 0, count = 0; i < num_threads; i++) {
		thread_port_t t = thread_list [i];
		pthread_t pt = pthread_from_mach_thread_np (t);
		if (t != cur_thread && pt != exception_thread && !mono_sgen_is_worker_thread (pt)) {
			if (signum == suspend_signal_num) {
				ret = thread_suspend (t);
				if (ret != KERN_SUCCESS) {
					mach_port_deallocate (task, t);
					continue;
				}

				state = (thread_state_t) alloca (mono_mach_arch_get_thread_state_size ());
				ret = mono_mach_arch_get_thread_state (t, state, &num_state);
				if (ret != KERN_SUCCESS) {
					mach_port_deallocate (task, t);
					continue;
				}


				info = mono_sgen_thread_info_lookup (pt);

				/* Ensure that the runtime is aware of this thread */
				if (info != NULL) {
					mctx = (mcontext_t) alloca (mono_mach_arch_get_mcontext_size ());
					mono_mach_arch_thread_state_to_mcontext (state, mctx);
					ctx.uc_mcontext = mctx;

					info->stopped_domain = mono_mach_arch_get_tls_value_from_thread (t, mono_pthread_key_for_tls (mono_domain_get_tls_key ()));
					info->stopped_ip = (gpointer) mono_mach_arch_get_ip (state);
					stack_start = (char*) mono_mach_arch_get_sp (state) - REDZONE_SIZE;
					/* If stack_start is not within the limits, then don't set it in info and we will be restarted. */
					if (stack_start >= info->stack_start_limit && info->stack_start <= info->stack_end) {
						info->stack_start = stack_start;

						ARCH_COPY_SIGCTX_REGS (regs, &ctx);
						info->stopped_regs = regs;
					} else {
						g_assert (!info->stack_start);
					}

					/* Notify the JIT */
					if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
						mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, &ctx);
				}
			} else {
				ret = thread_resume (t);
				if (ret != KERN_SUCCESS) {
					mach_port_deallocate (task, t);
					continue;
				}
			}
			count ++;

			mach_port_deallocate (task, t);
		} else {
			mach_port_deallocate (task, t);
		}
	}

	mono_mach_free_threads (thread_list, num_threads);

	mach_port_deallocate (task, cur_thread);

	return count;
}
#endif
#endif
