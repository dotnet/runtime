/*
 * sgen-os-posix.c: Simple generational GC.
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
#if !defined(__MACH__) && !MONO_MACH_ARCH_SUPPORTED && defined(HAVE_PTHREAD_KILL)

#include <errno.h>
#include <glib.h>
#include "metadata/sgen-gc.h"
#include "metadata/gc-internal.h"
#include "metadata/sgen-archdep.h"
#include "metadata/object-internals.h"

#if defined(__APPLE__) || defined(__OpenBSD__) || defined(__FreeBSD__)
const static int suspend_signal_num = SIGXFSZ;
#else
const static int suspend_signal_num = SIGPWR;
#endif
const static int restart_signal_num = SIGXCPU;

static MonoSemType suspend_ack_semaphore;
static MonoSemType *suspend_ack_semaphore_ptr;

static sigset_t suspend_signal_mask;
static sigset_t suspend_ack_signal_mask;

static void
suspend_thread (SgenThreadInfo *info, void *context)
{
	int stop_count;
#ifdef USE_MONO_CTX
	MonoContext monoctx;
#else
	gpointer regs [ARCH_NUM_REGS];
#endif
	gpointer stack_start;

	g_assert (info->doing_handshake);

	info->stopped_domain = mono_domain_get ();
	info->stopped_ip = context ? (gpointer) ARCH_SIGCTX_IP (context) : NULL;
	stop_count = sgen_global_stop_count;
	/* duplicate signal */
	if (0 && info->stop_count == stop_count)
		return;

	sgen_fill_thread_info_for_suspend (info);

	stack_start = context ? (char*) ARCH_SIGCTX_SP (context) - REDZONE_SIZE : NULL;
	/* If stack_start is not within the limits, then don't set it
	   in info and we will be restarted. */
	if (stack_start >= info->stack_start_limit && info->stack_start <= info->stack_end) {
		info->stack_start = stack_start;

#ifdef USE_MONO_CTX
		if (context) {
			mono_sigctx_to_monoctx (context, &monoctx);
			info->monoctx = &monoctx;
		} else {
			info->monoctx = NULL;
		}
#else
		if (context) {
			ARCH_COPY_SIGCTX_REGS (regs, context);
			info->stopped_regs = regs;
		} else {
			info->stopped_regs = NULL;
		}
#endif
	} else {
		g_assert (!info->stack_start);
	}

	/* Notify the JIT */
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, context);

	DEBUG (4, fprintf (gc_debug_file, "Posting suspend_ack_semaphore for suspend from %p %p\n", info, (gpointer)mono_native_thread_id_get ()));

	/*
	Block the restart signal. 
	We need to block the restart signal while posting to the suspend_ack semaphore or we race to sigsuspend,
	which might miss the signal and get stuck.
	*/
	pthread_sigmask (SIG_BLOCK, &suspend_ack_signal_mask, NULL);

	/* notify the waiting thread */
	MONO_SEM_POST (suspend_ack_semaphore_ptr);
	info->stop_count = stop_count;

	/* wait until we receive the restart signal */
	do {
		info->signal = 0;
		sigsuspend (&suspend_signal_mask);
	} while (info->signal != restart_signal_num && info->doing_handshake);

	/* Unblock the restart signal. */
	pthread_sigmask (SIG_UNBLOCK, &suspend_ack_signal_mask, NULL);

	DEBUG (4, fprintf (gc_debug_file, "Posting suspend_ack_semaphore for resume from %p %p\n", info, (gpointer)mono_native_thread_id_get ()));
	/* notify the waiting thread */
	MONO_SEM_POST (suspend_ack_semaphore_ptr);
}

/* LOCKING: assumes the GC lock is held (by the stopping thread) */
static void
suspend_handler (int sig, siginfo_t *siginfo, void *context)
{
	SgenThreadInfo *info;
	int old_errno = errno;

	info = mono_thread_info_current ();

	if (info) {
		suspend_thread (info, context);
	} else {
		/* This can happen while a thread is dying */
		//g_print ("no thread info in suspend\n");
	}

	errno = old_errno;
}

static void
restart_handler (int sig)
{
	SgenThreadInfo *info;
	int old_errno = errno;

	info = mono_thread_info_current ();
	/*
	If the thread info is null is means we're currently in the process of cleaning up,
	the pthread destructor has already kicked in and it has explicitly invoked the suspend handler.
	
	This means this thread has been suspended, TLS is dead, so the only option we have is to
	rely on pthread_self () and seatch over the thread list.
	*/
	if (!info)
		info = mono_thread_info_lookup (pthread_self ());

	/*
	 * If a thread is dying there might be no thread info.  In
	 * that case we rely on info->doing_handshake.
	 */
	if (info) {
		info->signal = restart_signal_num;
		DEBUG (4, fprintf (gc_debug_file, "Restart handler in %p %p\n", info, (gpointer)mono_native_thread_id_get ()));
	}
	errno = old_errno;
}

gboolean
sgen_resume_thread (SgenThreadInfo *info)
{
	return mono_threads_pthread_kill (info, restart_signal_num) == 0;
}

gboolean
sgen_suspend_thread (SgenThreadInfo *info)
{
	return mono_threads_pthread_kill (info, suspend_signal_num) == 0;
}

void
sgen_wait_for_suspend_ack (int count)
{
	int i, result;

	for (i = 0; i < count; ++i) {
		while ((result = MONO_SEM_WAIT (suspend_ack_semaphore_ptr)) != 0) {
			if (errno != EINTR) {
				g_error ("MONO_SEM_WAIT FAILED with %d errno %d (%s)", result, errno, strerror (errno));
			}
		}
	}
}

gboolean
sgen_park_current_thread_if_doing_handshake (SgenThreadInfo *p)
{
    if (!p->doing_handshake)
	    return FALSE;

    suspend_thread (p, NULL);
    return TRUE;
}

int
sgen_thread_handshake (BOOL suspend)
{
	int count, result;
	SgenThreadInfo *info;
	int signum = suspend ? suspend_signal_num : restart_signal_num;

	MonoNativeThreadId me = mono_native_thread_id_get ();

	count = 0;
	FOREACH_THREAD_SAFE (info) {
		info->joined_stw = suspend;
		if (mono_native_thread_id_equals (mono_thread_info_get_tid (info), me)) {
			continue;
		}
		if (info->gc_disabled)
			continue;
		/*if (signum == suspend_signal_num && info->stop_count == global_stop_count)
			continue;*/
		if (suspend) {
			g_assert (!info->doing_handshake);
			info->doing_handshake = TRUE;
		} else {
			g_assert (info->doing_handshake);
			info->doing_handshake = FALSE;
		}
		result = pthread_kill (mono_thread_info_get_tid (info), signum);
		if (result == 0) {
			count++;
		} else {
			info->skip = 1;
		}
	} END_FOREACH_THREAD_SAFE

	sgen_wait_for_suspend_ack (count);

	return count;
}

void
sgen_os_init (void)
{
	struct sigaction sinfo;

	suspend_ack_semaphore_ptr = &suspend_ack_semaphore;
	MONO_SEM_INIT (&suspend_ack_semaphore, 0);

	sigfillset (&sinfo.sa_mask);
	sinfo.sa_flags = SA_RESTART | SA_SIGINFO;
	sinfo.sa_sigaction = suspend_handler;
	if (sigaction (suspend_signal_num, &sinfo, NULL) != 0) {
		g_error ("failed sigaction");
	}

	sinfo.sa_handler = restart_handler;
	if (sigaction (restart_signal_num, &sinfo, NULL) != 0) {
		g_error ("failed sigaction");
	}

	sigfillset (&suspend_signal_mask);
	sigdelset (&suspend_signal_mask, restart_signal_num);

	sigemptyset (&suspend_ack_signal_mask);
	sigaddset (&suspend_ack_signal_mask, restart_signal_num);
	
}

int
mono_gc_get_suspend_signal (void)
{
	return suspend_signal_num;
}
#endif
#endif
