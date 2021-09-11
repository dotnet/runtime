/**
 * \file
 * POSIX signal handling support for Mono.
 *
 * Authors:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2008 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 *
 * See LICENSE for licensing information.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <signal.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_SYS_SYSCALL_H
#include <sys/syscall.h>
#endif
#ifdef HAVE_SYS_PRCTL_H
#include <sys/prctl.h>
#endif
#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>
#endif
#include <errno.h>
#include <sched.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-errno.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/os-event.h>
#include <mono/utils/mono-time.h>
#include <mono/component/debugger-state-machine.h>
#include <mono/metadata/components.h>

#include "mini.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"
#include <mono/component/debugger-agent.h>
#include "mini-runtime.h"
#include "jit-icalls.h"

#ifdef HOST_DARWIN
#include <mach/mach.h>
#include <mach/mach_time.h>
#include <mach/clock.h>
#endif

#ifndef HOST_WIN32
#include <mono/utils/mono-threads-debug.h>
#endif

#include <fcntl.h>
#include <gmodule.h>
#if HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#include "mono/utils/mono-tls-inline.h"

#if defined(HOST_WATCHOS)

void
mono_runtime_setup_stat_profiler (void)
{
	printf("WARNING: mono_runtime_setup_stat_profiler() called!\n");
}

gboolean
MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal)
{
	return FALSE;
}

#ifndef HOST_DARWIN
void
mono_runtime_install_handlers (void)
{
}
#endif

void
mono_runtime_posix_install_handlers(void)
{
	/* we still need to ignore SIGPIPE */
	signal (SIGPIPE, SIG_IGN);
}

void
mono_runtime_shutdown_handlers (void)
{
}

#else

static GHashTable *mono_saved_signal_handlers = NULL;

static struct sigaction *
get_saved_signal_handler (int signo)
{
	if (mono_saved_signal_handlers) {
		/* The hash is only modified during startup, so no need for locking */
		struct sigaction *handler = (struct sigaction*)g_hash_table_lookup (mono_saved_signal_handlers, GINT_TO_POINTER (signo));
		return handler;
	}
	return NULL;
}


static void
remove_saved_signal_handler (int signo)
{
	if (mono_saved_signal_handlers) {
		/* The hash is only modified during startup, so no need for locking */
		struct sigaction *handler = (struct sigaction*)g_hash_table_lookup (mono_saved_signal_handlers, GINT_TO_POINTER (signo));
		if (handler)
			g_hash_table_remove (mono_saved_signal_handlers, GINT_TO_POINTER (signo));
	}
	return;
}



static void
save_old_signal_handler (int signo, struct sigaction *old_action)
{
	struct sigaction *handler_to_save = (struct sigaction *)g_malloc (sizeof (struct sigaction));

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG,
				"Saving old signal handler for signal %d.", signo);

	if (! (old_action->sa_flags & SA_SIGINFO)) {
		handler_to_save->sa_handler = old_action->sa_handler;
	} else {
#ifdef MONO_ARCH_USE_SIGACTION
		handler_to_save->sa_sigaction = old_action->sa_sigaction;
#endif /* MONO_ARCH_USE_SIGACTION */
	}
	handler_to_save->sa_mask = old_action->sa_mask;
	handler_to_save->sa_flags = old_action->sa_flags;
	
	if (!mono_saved_signal_handlers)
		mono_saved_signal_handlers = g_hash_table_new_full (NULL, NULL, NULL, g_free);
	g_hash_table_insert (mono_saved_signal_handlers, GINT_TO_POINTER (signo), handler_to_save);
}

/*
 * mono_chain_signal:
 *
 *   Call the original signal handler for the signal given by the arguments, which
 * should be the same as for a signal handler. Returns TRUE if the original handler
 * was called, false otherwise.
 */
gboolean
MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal)
{
	int signal = MONO_SIG_HANDLER_GET_SIGNO ();
	struct sigaction *saved_handler = (struct sigaction *)get_saved_signal_handler (signal);

	if (saved_handler && saved_handler->sa_handler) {
		if (!(saved_handler->sa_flags & SA_SIGINFO)) {
			saved_handler->sa_handler (signal);
		} else {
#ifdef MONO_ARCH_USE_SIGACTION
			saved_handler->sa_sigaction (MONO_SIG_HANDLER_PARAMS);
#endif /* MONO_ARCH_USE_SIGACTION */
		}
		return TRUE;
	}
	return FALSE;
}

MONO_SIG_HANDLER_FUNC (static, sigabrt_signal_handler)
{
	MonoJitInfo *ji = NULL;
	MonoContext mctx;
	MONO_SIG_HANDLER_INFO_TYPE *info = MONO_SIG_HANDLER_GET_INFO ();
	MONO_SIG_HANDLER_GET_CONTEXT;

	if (mono_thread_internal_current ())
		ji = mono_jit_info_table_find_internal (mono_arch_ip_from_context (ctx), TRUE, TRUE);
	if (!ji) {
		if (mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;
		mono_sigctx_to_monoctx (ctx, &mctx);
		mono_handle_native_crash (mono_get_signame (info->si_signo), &mctx, info);
		abort ();
	}
}

#if (defined (USE_POSIX_BACKEND) && defined (SIGRTMIN)) || defined (SIGPROF)
#define HAVE_PROFILER_SIGNAL
#endif

#ifdef HAVE_PROFILER_SIGNAL

static MonoNativeThreadId sampling_thread;

static gint32 profiler_signals_sent;
static gint32 profiler_signals_received;
static gint32 profiler_signals_accepted;
static gint32 profiler_interrupt_signals_received;

MONO_SIG_HANDLER_FUNC (static, profiler_signal_handler)
{
	int old_errno = errno;

	MONO_SIG_HANDLER_GET_CONTEXT;

	/* See the comment in mono_runtime_shutdown_stat_profiler (). */
	if (mono_native_thread_id_get () == sampling_thread) {
		mono_atomic_inc_i32 (&profiler_interrupt_signals_received);
		return;
	}

	mono_atomic_inc_i32 (&profiler_signals_received);

	// Did a non-attached or detaching thread get the signal?
	if (mono_thread_info_get_small_id () == -1 ||
	    !mono_domain_get () ||
	    !mono_tls_get_jit_tls ()) {
		mono_set_errno (old_errno);
		return;
	}

	// See the comment in sampling_thread_func ().
	mono_atomic_store_i32 (&mono_thread_info_current ()->profiler_signal_ack, 1);

	mono_atomic_inc_i32 (&profiler_signals_accepted);

	int hp_save_index = mono_hazard_pointer_save_for_signal_handler ();

	mono_thread_info_set_is_async_context (TRUE);

	MONO_PROFILER_RAISE (sample_hit, ((const mono_byte*)mono_arch_ip_from_context (ctx), ctx));

	mono_thread_info_set_is_async_context (FALSE);

	mono_hazard_pointer_restore_for_signal_handler (hp_save_index);

	mono_set_errno (old_errno);

	mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
}

#endif

MONO_SIG_HANDLER_FUNC (static, sigquit_signal_handler)
{
	mono_threads_request_thread_dump ();

	mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
}

MONO_SIG_HANDLER_FUNC (static, sigusr2_signal_handler)
{
	gboolean enabled = mono_trace_is_enabled ();

	mono_trace_enable (!enabled);

	mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
}

typedef void MONO_SIG_HANDLER_SIGNATURE ((*MonoSignalHandler));

static void
add_signal_handler (int signo, MonoSignalHandler handler, int flags)
{
	struct sigaction sa;
	struct sigaction previous_sa;

#ifdef MONO_ARCH_USE_SIGACTION
	sa.sa_sigaction = handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO | flags;
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

/*Apple likes to deliver SIGBUS for *0 */
#ifdef HOST_DARWIN
	if (signo == SIGSEGV || signo == SIGBUS) {
#else
	if (signo == SIGSEGV) {
#endif
		sa.sa_flags |= SA_ONSTACK;

		/* 
		 * libgc will crash when trying to do stack marking for threads which are on
		 * an altstack, so delay the suspend signal after the signal handler has
		 * executed.
		 */
		if (mono_gc_get_suspend_signal () != -1)
			sigaddset (&sa.sa_mask, mono_gc_get_suspend_signal ());
	}
#endif
	if (signo == SIGSEGV) {
		/* 
		 * Delay abort signals while handling SIGSEGVs since they could go unnoticed.
		 */
		sigset_t block_mask;
     
		sigemptyset (&block_mask);
	}
#else
	sa.sa_handler = (void (*)(int))handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = flags;
#endif
	g_assert (sigaction (signo, &sa, &previous_sa) != -1);

	/* if there was already a handler in place for this signal, store it */
	if (! (previous_sa.sa_flags & SA_SIGINFO) &&
			(SIG_DFL == previous_sa.sa_handler)) { 
		/* it there is no sa_sigaction function and the sa_handler is default, we can safely ignore this */
	} else {
		if (mono_do_signal_chaining)
			save_old_signal_handler (signo, &previous_sa);
	}
}

static void
remove_signal_handler (int signo)
{
	struct sigaction sa;
	struct sigaction *saved_action = get_saved_signal_handler (signo);

	if (!saved_action) {
		sa.sa_handler = SIG_DFL;
		sigemptyset (&sa.sa_mask);
		sa.sa_flags = 0;

		sigaction (signo, &sa, NULL);
	} else {
		g_assert (sigaction (signo, saved_action, NULL) != -1);
	}
	remove_saved_signal_handler(signo);
}

void
mono_runtime_posix_install_handlers (void)
{

	sigset_t signal_set;
	sigemptyset (&signal_set);
	mono_load_signames ();
	if (mini_debug_options.handle_sigint) {
		add_signal_handler (SIGINT, mono_sigint_signal_handler, SA_RESTART);
		sigaddset (&signal_set, SIGINT);
	}

	add_signal_handler (SIGFPE, mono_sigfpe_signal_handler, 0);
	sigaddset (&signal_set, SIGFPE);
	add_signal_handler (SIGQUIT, sigquit_signal_handler, SA_RESTART);
	sigaddset (&signal_set, SIGQUIT);
	add_signal_handler (SIGILL, mono_crashing_signal_handler, 0);
	sigaddset (&signal_set, SIGILL);
	add_signal_handler (SIGBUS, mono_sigsegv_signal_handler, 0);
	sigaddset (&signal_set, SIGBUS);
	if (mono_jit_trace_calls != NULL) {
		add_signal_handler (SIGUSR2, sigusr2_signal_handler, SA_RESTART);
		sigaddset (&signal_set, SIGUSR2);
	}
	add_signal_handler (SIGSYS, mono_crashing_signal_handler, 0);
	sigaddset (&signal_set, SIGSYS);

	/* it seems to have become a common bug for some programs that run as parents
	 * of many processes to block signal delivery for real time signals.
	 * We try to detect and work around their breakage here.
	 */
	if (mono_gc_get_suspend_signal () != -1)
		sigaddset (&signal_set, mono_gc_get_suspend_signal ());
	if (mono_gc_get_restart_signal () != -1)
		sigaddset (&signal_set, mono_gc_get_restart_signal ());
	sigaddset (&signal_set, SIGCHLD);

	signal (SIGPIPE, SIG_IGN);
	sigaddset (&signal_set, SIGPIPE);

	add_signal_handler (SIGABRT, sigabrt_signal_handler, 0);
	sigaddset (&signal_set, SIGABRT);

	/* catch SIGSEGV */
	add_signal_handler (SIGSEGV, mono_sigsegv_signal_handler, 0);
	sigaddset (&signal_set, SIGSEGV);

	sigprocmask (SIG_UNBLOCK, &signal_set, NULL);
}

#ifndef HOST_DARWIN
void
mono_runtime_install_handlers (void)
{
	mono_runtime_posix_install_handlers ();
}
#endif

#ifdef HAVE_PROFILER_SIGNAL

static volatile gint32 sampling_thread_running;

#ifdef HOST_DARWIN

static clock_serv_t sampling_clock;

static void
clock_init_for_profiler (MonoProfilerSampleMode mode)
{
	mono_clock_init (&sampling_clock);
}

static void
clock_sleep_ns_abs (guint64 ns_abs)
{
	kern_return_t ret;
	mach_timespec_t then, remain_unused;

	then.tv_sec = ns_abs / 1000000000;
	then.tv_nsec = ns_abs % 1000000000;

	do {
		ret = clock_sleep (sampling_clock, TIME_ABSOLUTE, then, &remain_unused);

		if (ret != KERN_SUCCESS && ret != KERN_ABORTED)
			g_error ("%s: clock_sleep () returned %d", __func__, ret);
	} while (ret == KERN_ABORTED && mono_atomic_load_i32 (&sampling_thread_running));
}

#else

static clockid_t sampling_clock;

static void
clock_init_for_profiler (MonoProfilerSampleMode mode)
{
	switch (mode) {
	case MONO_PROFILER_SAMPLE_MODE_PROCESS: {
	/*
	 * If we don't have clock_nanosleep (), measuring the process time
	 * makes very little sense as we can only use nanosleep () to sleep on
	 * real time.
	 */
#if defined(HAVE_CLOCK_NANOSLEEP) && !defined(__PASE__)
		struct timespec ts = { 0 };

		/*
		 * Some systems (e.g. Windows Subsystem for Linux) declare the
		 * CLOCK_PROCESS_CPUTIME_ID clock but don't actually support it. For
		 * those systems, we fall back to CLOCK_MONOTONIC if we get EINVAL.
		 */
		if (clock_nanosleep (CLOCK_PROCESS_CPUTIME_ID, TIMER_ABSTIME, &ts, NULL) != EINVAL) {
			sampling_clock = CLOCK_PROCESS_CPUTIME_ID;
			break;
		}
#endif

		// fallthrough
	}
	case MONO_PROFILER_SAMPLE_MODE_REAL: sampling_clock = CLOCK_MONOTONIC; break;
	default: g_assert_not_reached (); break;
	}
}

static void
clock_sleep_ns_abs (guint64 ns_abs)
{
#if defined(HAVE_CLOCK_NANOSLEEP) && !defined(__PASE__)
	int ret;
	struct timespec then;

	then.tv_sec = ns_abs / 1000000000;
	then.tv_nsec = ns_abs % 1000000000;

	do {
		ret = clock_nanosleep (sampling_clock, TIMER_ABSTIME, &then, NULL);

		if (ret != 0 && ret != EINTR)
			g_error ("%s: clock_nanosleep () returned %d", __func__, ret);
	} while (ret == EINTR && mono_atomic_load_i32 (&sampling_thread_running));
#else
	int ret;
	gint64 diff;
	struct timespec req;

	/*
	 * What follows is a crude attempt at emulating clock_nanosleep () on OSs
	 * which don't provide it (e.g. FreeBSD).
	 *
	 * The problem with nanosleep () is that if it is interrupted by a signal,
	 * time will drift as a result of having to restart the call after the
	 * signal handler has finished. For this reason, we avoid using the rem
	 * argument of nanosleep (). Instead, before every nanosleep () call, we
	 * check if enough time has passed to satisfy the sleep request. If yes, we
	 * simply return. If not, we calculate the difference and do another sleep.
	 *
	 * This should reduce the amount of drift that happens because we account
	 * for the time spent executing the signal handler, which nanosleep () is
	 * not guaranteed to do for the rem argument.
	 *
	 * The downside to this approach is that it is slightly expensive: We have
	 * to make an extra system call to retrieve the current time whenever we're
	 * going to restart a nanosleep () call. This is unlikely to be a problem
	 * in practice since the sampling thread won't be receiving many signals in
	 * the first place (it's a tools thread, so no STW), and because typical
	 * sleep periods for the thread are many orders of magnitude bigger than
	 * the time it takes to actually perform that system call (just a few
	 * nanoseconds).
	 */
	do {
		diff = (gint64) ns_abs - (gint64) mono_clock_get_time_ns (sampling_clock);

		if (diff <= 0)
			break;

		req.tv_sec = diff / 1000000000;
		req.tv_nsec = diff % 1000000000;

		if ((ret = nanosleep (&req, NULL)) == -1 && errno != EINTR)
			g_error ("%s: nanosleep () returned -1, errno = %d", __func__, errno);
	} while (ret == -1 && mono_atomic_load_i32 (&sampling_thread_running));
#endif
}

#endif

static int profiler_signal;
static volatile gint32 sampling_thread_exiting;
static MonoOSEvent sampling_thread_exited;

static gsize
sampling_thread_func (gpointer unused)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	thread->flags |= MONO_THREAD_FLAG_DONT_MANAGE;

	mono_thread_set_name_constant_ignore_error (thread, "Profiler Sampler", MonoSetThreadNameFlag_None);

	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE);

	int old_policy;
	struct sched_param old_sched;
	pthread_getschedparam (pthread_self (), &old_policy, &old_sched);

	/*
	 * Attempt to switch the thread to real time scheduling. This will not
	 * necessarily work on all OSs; for example, most Linux systems will give
	 * us EPERM here unless configured to allow this.
	 *
	 * TODO: This does not work on Mac (and maybe some other OSs). On Mac, we
	 * have to use the Mach thread policy routines to switch to real-time
	 * scheduling. This is quite tricky as we need to specify how often we'll
	 * be doing work (easy), the normal processing time needed (also easy),
	 * and the maximum amount of processing time needed (hard). This is
	 * further complicated by the fact that if we misbehave and take too long
	 * to do our work, the kernel may knock us back down to the normal thread
	 * scheduling policy without telling us.
	 */
	struct sched_param sched;
	memset (&sched, 0, sizeof (sched));
	sched.sched_priority = sched_get_priority_max (SCHED_FIFO);
	pthread_setschedparam (pthread_self (), SCHED_FIFO, &sched);

	MonoProfilerSampleMode mode;

init:
	mono_profiler_get_sample_mode (NULL, &mode, NULL);

	if (mode == MONO_PROFILER_SAMPLE_MODE_NONE) {
		mono_profiler_sampling_thread_wait ();

		if (!mono_atomic_load_i32 (&sampling_thread_running))
			goto done;

		goto init;
	}

	clock_init_for_profiler (mode);

	for (guint64 sleep = mono_clock_get_time_ns (sampling_clock); mono_atomic_load_i32 (&sampling_thread_running); clock_sleep_ns_abs (sleep)) {
		uint32_t freq;
		MonoProfilerSampleMode new_mode;

		mono_profiler_get_sample_mode (NULL, &new_mode, &freq);

		if (new_mode != mode) {
			mono_clock_cleanup (sampling_clock);
			goto init;
		}

		sleep += 1000000000 / freq;

		FOREACH_THREAD_SAFE_EXCLUDE (info, MONO_THREAD_INFO_FLAGS_NO_SAMPLE) {
			g_assert (mono_thread_info_get_tid (info) != sampling_thread);

			/*
			 * Require an ack for the last sampling signal sent to the thread
			 * so that we don't overflow the signal queue, leading to all sorts
			 * of problems (e.g. GC STW failing).
			 */
			if (profiler_signal != SIGPROF && !mono_atomic_cas_i32 (&info->profiler_signal_ack, 0, 1))
				continue;

			mono_threads_pthread_kill (info, profiler_signal);
			mono_atomic_inc_i32 (&profiler_signals_sent);
		} FOREACH_THREAD_SAFE_END
	}

	mono_clock_cleanup (sampling_clock);

done:
	mono_atomic_store_i32 (&sampling_thread_exiting, 1);

	pthread_setschedparam (pthread_self (), old_policy, &old_sched);

	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NONE);

	mono_os_event_set (&sampling_thread_exited);

	return 0;
}

void
mono_runtime_setup_stat_profiler (void)
{
	/*
	 * Use a real-time signal when possible. This gives us roughly a 99% signal
	 * delivery rate in all cases. On the other hand, using a regular signal
	 * tends to result in awful delivery rates when the application is heavily
	 * loaded.
	 *
	 * We avoid real-time signals on Android as they're super broken in certain
	 * API levels (too small sigset_t, nonsensical SIGRTMIN/SIGRTMAX values,
	 * etc).
	 *
	 * TODO: On Mac, we should explore using the Mach thread suspend/resume
	 * functions and doing the stack walk from the sampling thread. This would
	 * get us a 100% sampling rate. However, this may interfere with the GC's
	 * STW logic. Could perhaps be solved by taking the suspend lock.
	 */
#if defined (USE_POSIX_BACKEND) && defined (SIGRTMIN) && !defined (HOST_ANDROID)
	/* Just take the first real-time signal we can get. */
	profiler_signal = mono_threads_suspend_search_alternative_signal ();
#else
	profiler_signal = SIGPROF;
#endif

	add_signal_handler (profiler_signal, profiler_signal_handler, SA_RESTART);

	mono_counters_register ("Sampling signals sent", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &profiler_signals_sent);
	mono_counters_register ("Sampling signals received", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &profiler_signals_received);
	mono_counters_register ("Sampling signals accepted", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &profiler_signals_accepted);
	mono_counters_register ("Shutdown signals received", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &profiler_interrupt_signals_received);

	mono_os_event_init (&sampling_thread_exited, FALSE);

	mono_atomic_store_i32 (&sampling_thread_running, 1);

	ERROR_DECL (error);
	MonoInternalThread *thread = mono_thread_create_internal ((MonoThreadStart)sampling_thread_func, NULL, MONO_THREAD_CREATE_FLAGS_NONE, error);
	mono_error_assert_ok (error);

	sampling_thread = MONO_UINT_TO_NATIVE_THREAD_ID (thread->tid);
}

#else

void
mono_runtime_shutdown_stat_profiler (void)
{
}

void
mono_runtime_setup_stat_profiler (void)
{
}

#endif

#endif /* defined(HOST_WATCHOS) */

#ifndef MONO_CROSS_COMPILE
static void
dump_memory_around_ip (MonoContext *mctx)
{
	if (!mctx)
		return;

	g_async_safe_printf ("\n=================================================================\n");
	g_async_safe_printf ("\tBasic Fault Address Reporting\n");
	g_async_safe_printf ("=================================================================\n");

	gpointer native_ip = MONO_CONTEXT_GET_IP (mctx);
	if (native_ip) {
		native_ip = MINI_FTNPTR_TO_ADDR (native_ip);
		g_async_safe_printf ("Memory around native instruction pointer (%p):", native_ip);
		mono_dump_mem (((guint8 *) native_ip) - 0x10, 0x40);
	} else {
		g_async_safe_printf ("instruction pointer is NULL, skip dumping");
	}
}

static void
assert_printer_callback (void)
{
	mono_dump_native_crash_info ("SIGABRT", NULL, NULL);
}

#if !defined (HOST_WIN32)
/**
 * fork_crash_safe:
 *
 * Version of \c fork that is safe to call from an async context such as a
 * signal handler even if the process crashed inside libc.
 *
 * Returns 0 to the child process, >0 to the parent process or <0 on error.
 */
static pid_t
fork_crash_safe (void)
{
	pid_t pid;
	/*
	 * glibc fork acquires some locks, so if the crash happened inside malloc/free,
	 * it will deadlock. Call the syscall directly instead.
	 */
#if defined(HOST_ANDROID)
	/* SYS_fork is defined to be __NR_fork which is not defined in some ndk versions */
	g_assert_not_reached ();
#elif !defined(HOST_DARWIN) && defined(SYS_fork)
	pid = (pid_t) syscall (SYS_fork);
#elif HAVE_FORK
	pid = (pid_t) fork ();
#else
	g_assert_not_reached ();
#endif
	return pid;
}
#endif

static void
dump_native_stacktrace (const char *signal, MonoContext *mctx)
{
	mono_memory_barrier ();
	static gint32 middle_of_crash = 0x0;
	gint32 double_faulted = mono_atomic_cas_i32 ((gint32 *)&middle_of_crash, 0x1, 0x0);
	mono_memory_write_barrier ();

	if (!double_faulted) {
		g_assertion_disable_global (assert_printer_callback);
	} else {
		g_async_safe_printf ("\nAn error has occured in the native fault reporting. Some diagnostic information will be unavailable.\n");

	}

#ifdef HAVE_BACKTRACE_SYMBOLS

	void *array [256];
	int size = backtrace (array, 256);

	g_async_safe_printf ("\n=================================================================\n");
	g_async_safe_printf ("\tNative stacktrace:\n");
	g_async_safe_printf ("=================================================================\n");
	if (size == 0)
		g_async_safe_printf ("\t (No frames) \n\n");

	for (int i = 0; i < size; ++i) {
		gpointer ip = array [i];
		char sname [256], fname [256];
		gboolean success = g_module_address ((void*)ip, fname, 256, NULL, sname, 256, NULL);
		if (!success) {
			g_async_safe_printf ("\t%p - Unknown\n", ip);
		} else {
			g_async_safe_printf ("\t%p - %s : %s\n", ip, fname, sname);
		}
	}

#if !defined(HOST_WIN32) && defined(HAVE_SYS_SYSCALL_H) && (defined(SYS_fork) || HAVE_FORK)
	pid_t crashed_pid = getpid ();

	pid_t pid = crashed_pid; /* init to some >0 value */
	gboolean need_to_fork = !mini_debug_options.no_gdb_backtrace;

	if (need_to_fork)
		pid = fork_crash_safe ();

#if defined (HAVE_PRCTL) && defined(PR_SET_PTRACER)
	if (need_to_fork && pid > 0) {
		// Allow gdb to attach to the process even if ptrace_scope sysctl variable is set to
		// a value other than 0 (the most permissive ptrace scope). Most modern Linux
		// distributions set the scope to 1 which allows attaching only to direct children of
		// the current process
		prctl (PR_SET_PTRACER, pid, 0, 0, 0);
	}
#endif

	if (!mini_debug_options.no_gdb_backtrace && pid == 0) {
		dup2 (STDERR_FILENO, STDOUT_FILENO);

		g_async_safe_printf ("\n=================================================================\n");
		g_async_safe_printf("\tExternal Debugger Dump:\n");
		g_async_safe_printf ("=================================================================\n");
		mono_gdb_render_native_backtraces (crashed_pid);
		_exit (1);
	} else if (need_to_fork && pid > 0) {
		int status;
		waitpid (pid, &status, 0);
	} else {
		// If we can't fork, do as little as possible before exiting
	}

	if (double_faulted) {
		g_async_safe_printf("\nExiting early due to double fault.\n");
		_exit (-1);
	}

#endif
#else
#ifdef HOST_ANDROID
	/* set DUMPABLE for this process so debuggerd can attach with ptrace(2), see:
	* https://android.googlesource.com/platform/bionic/+/151da681000c07da3c24cd30a3279b1ca017f452/linker/debugger.cpp#206
	* this has changed on later versions of Android.  Also, we don't want to
	* set this on start-up as DUMPABLE has security implications. */
	prctl (PR_SET_DUMPABLE, 1);

	g_async_safe_printf("\nNo native Android stacktrace (see debuggerd output).\n");
#endif
#endif
}

void
mono_dump_native_crash_info (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info)
{
	dump_native_stacktrace (signal, mctx);
	dump_memory_around_ip (mctx);
}

void
mono_post_native_crash_handler (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info, gboolean crash_chaining)
{
	if (!crash_chaining) {
		/*Android abort is a fluke, it doesn't abort, it triggers another segv. */
#if defined (HOST_ANDROID)
		exit (-1);
#else
		abort ();
#endif
	}
}
#endif /* !MONO_CROSS_COMPILE */

static gchar *gdb_path;
static gchar *lldb_path;

void
mono_init_native_crash_info (void)
{
	gdb_path = g_find_program_in_path ("gdb");
	lldb_path = g_find_program_in_path ("lldb");
}

static gboolean
native_stack_with_gdb (pid_t crashed_pid, const char **argv, int commands, char* commands_filename)
{
	if (!gdb_path)
		return FALSE;

	argv [0] = gdb_path;
	argv [1] = "-batch";
	argv [2] = "-x";
	argv [3] = commands_filename;
	argv [4] = "-nx";

	g_async_safe_fprintf (commands, "attach %ld\n", (long) crashed_pid);
	g_async_safe_fprintf (commands, "info threads\n");
	g_async_safe_fprintf (commands, "thread apply all bt\n");
	if (mini_debug_options.verbose_gdb) {
		for (int i = 0; i < 32; ++i) {
			g_async_safe_fprintf (commands, "info registers\n");
			g_async_safe_fprintf (commands, "info frame\n");
			g_async_safe_fprintf (commands, "info locals\n");
			g_async_safe_fprintf (commands, "up\n");
		}
	}

	return TRUE;
}


static gboolean
native_stack_with_lldb (pid_t crashed_pid, const char **argv, int commands, char* commands_filename)
{
	if (!lldb_path)
		return FALSE;

	argv [0] = lldb_path;
	argv [1] = "--batch";
	argv [2] = "--source";
	argv [3] = commands_filename;
	argv [4] = "--no-lldbinit";

	g_async_safe_fprintf (commands, "process attach --pid %ld\n", (long) crashed_pid);
	g_async_safe_fprintf (commands, "thread list\n");
	g_async_safe_fprintf (commands, "thread backtrace all\n");
	if (mini_debug_options.verbose_gdb) {
		for (int i = 0; i < 32; ++i) {
			g_async_safe_fprintf (commands, "reg read\n");
			g_async_safe_fprintf (commands, "frame info\n");
			g_async_safe_fprintf (commands, "frame variable\n");
			g_async_safe_fprintf (commands, "up\n");
		}
	}
	g_async_safe_fprintf (commands, "detach\n");
	g_async_safe_fprintf (commands, "quit\n");

	return TRUE;
}

void
mono_gdb_render_native_backtraces (pid_t crashed_pid)
{
#ifdef HAVE_EXECV
	const char *argv [10];
	memset (argv, 0, sizeof (char*) * 10);

	char commands_filename [100]; 
	commands_filename [0] = '\0';
	g_snprintf (commands_filename, sizeof (commands_filename), "/tmp/mono-gdb-commands.%d", crashed_pid);

	// Create this file, overwriting if it already exists
	int commands_handle = g_open (commands_filename, O_TRUNC | O_WRONLY | O_CREAT, S_IWUSR | S_IRUSR | S_IRGRP | S_IROTH);
	if (commands_handle == -1) {
		g_async_safe_printf ("Could not make debugger temp file %s\n", commands_filename);
		return;
	}

#if defined(HOST_DARWIN)
	// lldb hangs on attaching on Catalina
	return;
	//if (native_stack_with_lldb (crashed_pid, argv, commands_handle, commands_filename))
	//	goto exec;
#endif

	if (native_stack_with_gdb (crashed_pid, argv, commands_handle, commands_filename))
		goto exec;

#if !defined(HOST_DARWIN)
	if (native_stack_with_lldb (crashed_pid, argv, commands_handle, commands_filename))
		goto exec;
#endif

	g_async_safe_printf ("mono_gdb_render_native_backtraces not supported on this platform, unable to find gdb or lldb\n");

	close (commands_handle);
	unlink (commands_filename);
	return;

exec:
	close (commands_handle);
	execv (argv [0], (char**)argv);

	_exit (-1);
#else
	g_async_safe_printf ("mono_gdb_render_native_backtraces not supported on this platform\n");
#endif // HAVE_EXECV
}

#if !defined (__MACH__)

gboolean
mono_thread_state_init_from_handle (MonoThreadUnwindState *tctx, MonoThreadInfo *info, void *sigctx)
{
	g_error ("Posix systems don't support mono_thread_state_init_from_handle");
	return FALSE;
}

#endif
