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
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_SYS_SYSCALL_H
#include <sys/syscall.h>
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
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/attach.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-threads.h>

#include "mini.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"
#include "version.h"
#include "debugger-agent.h"
#include "mini-runtime.h"
#include "jit-icalls.h"

#ifdef HOST_DARWIN
#include <mach/mach.h>
#include <mach/mach_time.h>
#include <mach/clock.h>
#endif

#if defined(HOST_WATCHOS)

void
mono_runtime_setup_stat_profiler (void)
{
	printf("WARNING: mono_runtime_setup_stat_profiler() called!\n");
}


void
mono_runtime_shutdown_stat_profiler (void)
{
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

void
mono_runtime_cleanup_handlers (void)
{
}

#else

static GHashTable *mono_saved_signal_handlers = NULL;

static struct sigaction *
get_saved_signal_handler (int signo, gboolean remove)
{
	if (mono_saved_signal_handlers) {
		/* The hash is only modified during startup, so no need for locking */
		struct sigaction *handler = g_hash_table_lookup (mono_saved_signal_handlers, GINT_TO_POINTER (signo));
		if (remove && handler)
			g_hash_table_remove (mono_saved_signal_handlers, GINT_TO_POINTER (signo));
		return handler;
	}
	return NULL;
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

static void
free_saved_signal_handlers (void)
{
	if (mono_saved_signal_handlers) {
		g_hash_table_destroy (mono_saved_signal_handlers);
		mono_saved_signal_handlers = NULL;
	}
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
	struct sigaction *saved_handler = (struct sigaction *)get_saved_signal_handler (signal, FALSE);

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
	MONO_SIG_HANDLER_INFO_TYPE *info = MONO_SIG_HANDLER_GET_INFO ();
	MONO_SIG_HANDLER_GET_CONTEXT;

	if (mono_thread_internal_current ())
		ji = mono_jit_info_table_find_internal (mono_domain_get (), (char *)mono_arch_ip_from_context (ctx), TRUE, TRUE);
	if (!ji) {
        if (mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;
		mono_handle_native_crash ("SIGABRT", ctx, info);
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
		errno = old_errno;
		return;
	}

	// See the comment in sampling_thread_func ().
	mono_atomic_store_i32 (&mono_thread_info_current ()->profiler_signal_ack, 1);

	mono_atomic_inc_i32 (&profiler_signals_accepted);

	int hp_save_index = mono_hazard_pointer_save_for_signal_handler ();

	mono_thread_info_set_is_async_context (TRUE);

	MONO_PROFILER_RAISE (sample_hit, (mono_arch_ip_from_context (ctx), ctx));

	mono_thread_info_set_is_async_context (FALSE);

	mono_hazard_pointer_restore_for_signal_handler (hp_save_index);

	errno = old_errno;

	mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
}

#endif

MONO_SIG_HANDLER_FUNC (static, sigquit_signal_handler)
{
	gboolean res;

	/* We use this signal to start the attach agent too */
	res = mono_attach_start ();
	if (res)
		return;

	mono_threads_request_thread_dump ();

	mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
}

MONO_SIG_HANDLER_FUNC (static, sigusr2_signal_handler)
{
	gboolean enabled = mono_trace_is_enabled ();

	mono_trace_enable (!enabled);

	mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
}

static void
add_signal_handler (int signo, gpointer handler, int flags)
{
	struct sigaction sa;
	struct sigaction previous_sa;

#ifdef MONO_ARCH_USE_SIGACTION
	sa.sa_sigaction = (void (*)(int, siginfo_t *, void *))handler;
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
	sa.sa_handler = handler;
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
	struct sigaction *saved_action = get_saved_signal_handler (signo, TRUE);

	if (!saved_action) {
		sa.sa_handler = SIG_DFL;
		sigemptyset (&sa.sa_mask);
		sa.sa_flags = 0;

		sigaction (signo, &sa, NULL);
	} else {
		g_assert (sigaction (signo, saved_action, NULL) != -1);
	}
}

void
mono_runtime_posix_install_handlers (void)
{

	sigset_t signal_set;

	if (mini_get_debug_options ()->handle_sigint)
		add_signal_handler (SIGINT, mono_sigint_signal_handler, SA_RESTART);

	add_signal_handler (SIGFPE, mono_sigfpe_signal_handler, 0);
	add_signal_handler (SIGQUIT, sigquit_signal_handler, SA_RESTART);
	add_signal_handler (SIGILL, mono_sigill_signal_handler, 0);
	add_signal_handler (SIGBUS, mono_sigsegv_signal_handler, 0);
	if (mono_jit_trace_calls != NULL)
		add_signal_handler (SIGUSR2, sigusr2_signal_handler, SA_RESTART);

	/* it seems to have become a common bug for some programs that run as parents
	 * of many processes to block signal delivery for real time signals.
	 * We try to detect and work around their breakage here.
	 */
	sigemptyset (&signal_set);
	if (mono_gc_get_suspend_signal () != -1)
		sigaddset (&signal_set, mono_gc_get_suspend_signal ());
	if (mono_gc_get_restart_signal () != -1)
		sigaddset (&signal_set, mono_gc_get_restart_signal ());
	sigaddset (&signal_set, SIGCHLD);
	sigprocmask (SIG_UNBLOCK, &signal_set, NULL);

	signal (SIGPIPE, SIG_IGN);

	add_signal_handler (SIGABRT, sigabrt_signal_handler, 0);

	/* catch SIGSEGV */
	add_signal_handler (SIGSEGV, mono_sigsegv_signal_handler, 0);
}

#ifndef HOST_DARWIN
void
mono_runtime_install_handlers (void)
{
	mono_runtime_posix_install_handlers ();
}
#endif

void
mono_runtime_cleanup_handlers (void)
{
	if (mini_get_debug_options ()->handle_sigint)
		remove_signal_handler (SIGINT);

	remove_signal_handler (SIGFPE);
	remove_signal_handler (SIGQUIT);
	remove_signal_handler (SIGILL);
	remove_signal_handler (SIGBUS);
	if (mono_jit_trace_calls != NULL)
		remove_signal_handler (SIGUSR2);

	remove_signal_handler (SIGABRT);

	remove_signal_handler (SIGSEGV);

	free_saved_signal_handlers ();
}

#ifdef HAVE_PROFILER_SIGNAL

static volatile gint32 sampling_thread_running;

#ifdef HOST_DARWIN

static clock_serv_t sampling_clock_service;

static void
clock_init (MonoProfilerSampleMode mode)
{
	kern_return_t ret;

	do {
		ret = host_get_clock_service (mach_host_self (), SYSTEM_CLOCK, &sampling_clock_service);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		g_error ("%s: host_get_clock_service () returned %d", __func__, ret);
}

static void
clock_cleanup (void)
{
	kern_return_t ret;

	do {
		ret = mach_port_deallocate (mach_task_self (), sampling_clock_service);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		g_error ("%s: mach_port_deallocate () returned %d", __func__, ret);
}

static guint64
clock_get_time_ns (void)
{
	kern_return_t ret;
	mach_timespec_t mach_ts;

	do {
		ret = clock_get_time (sampling_clock_service, &mach_ts);
	} while (ret == KERN_ABORTED);

	if (ret != KERN_SUCCESS)
		g_error ("%s: clock_get_time () returned %d", __func__, ret);

	return ((guint64) mach_ts.tv_sec * 1000000000) + (guint64) mach_ts.tv_nsec;
}

static void
clock_sleep_ns_abs (guint64 ns_abs)
{
	kern_return_t ret;
	mach_timespec_t then, remain_unused;

	then.tv_sec = ns_abs / 1000000000;
	then.tv_nsec = ns_abs % 1000000000;

	do {
		ret = clock_sleep (sampling_clock_service, TIME_ABSOLUTE, then, &remain_unused);

		if (ret != KERN_SUCCESS && ret != KERN_ABORTED)
			g_error ("%s: clock_sleep () returned %d", __func__, ret);
	} while (ret == KERN_ABORTED && mono_atomic_load_i32 (&sampling_thread_running));
}

#else

clockid_t sampling_posix_clock;

static void
clock_init (MonoProfilerSampleMode mode)
{
	switch (mode) {
	case MONO_PROFILER_SAMPLE_MODE_PROCESS: {
	/*
	 * If we don't have clock_nanosleep (), measuring the process time
	 * makes very little sense as we can only use nanosleep () to sleep on
	 * real time.
	 */
#ifdef HAVE_CLOCK_NANOSLEEP
		struct timespec ts = { 0 };

		/*
		 * Some systems (e.g. Windows Subsystem for Linux) declare the
		 * CLOCK_PROCESS_CPUTIME_ID clock but don't actually support it. For
		 * those systems, we fall back to CLOCK_MONOTONIC if we get EINVAL.
		 */
		if (clock_nanosleep (CLOCK_PROCESS_CPUTIME_ID, TIMER_ABSTIME, &ts, NULL) != EINVAL) {
			sampling_posix_clock = CLOCK_PROCESS_CPUTIME_ID;
			break;
		}
#endif

		// fallthrough
	}
	case MONO_PROFILER_SAMPLE_MODE_REAL: sampling_posix_clock = CLOCK_MONOTONIC; break;
	default: g_assert_not_reached (); break;
	}
}

static void
clock_cleanup (void)
{
}

static guint64
clock_get_time_ns (void)
{
	struct timespec ts;

	if (clock_gettime (sampling_posix_clock, &ts) == -1)
		g_error ("%s: clock_gettime () returned -1, errno = %d", __func__, errno);

	return ((guint64) ts.tv_sec * 1000000000) + (guint64) ts.tv_nsec;
}

static void
clock_sleep_ns_abs (guint64 ns_abs)
{
#ifdef HAVE_CLOCK_NANOSLEEP
	int ret;
	struct timespec then;

	then.tv_sec = ns_abs / 1000000000;
	then.tv_nsec = ns_abs % 1000000000;

	do {
		ret = clock_nanosleep (sampling_posix_clock, TIMER_ABSTIME, &then, NULL);

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
		diff = (gint64) ns_abs - (gint64) clock_get_time_ns ();

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

static mono_native_thread_return_t
sampling_thread_func (void *data)
{
	mono_threads_attach_tools_thread ();
	mono_native_thread_set_name (mono_native_thread_id_get (), "Profiler sampler");

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
	struct sched_param sched = { .sched_priority = sched_get_priority_max (SCHED_FIFO) };
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

	clock_init (mode);

	for (guint64 sleep = clock_get_time_ns (); mono_atomic_load_i32 (&sampling_thread_running); clock_sleep_ns_abs (sleep)) {
		uint32_t freq;
		MonoProfilerSampleMode new_mode;

		mono_profiler_get_sample_mode (NULL, &new_mode, &freq);

		if (new_mode != mode) {
			clock_cleanup ();
			goto init;
		}

		sleep += 1000000000 / freq;

		FOREACH_THREAD_SAFE (info) {
			/* info should never be this thread as we're a tools thread. */
			g_assert (mono_thread_info_get_tid (info) != mono_native_thread_id_get ());

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

	clock_cleanup ();

done:
	mono_atomic_store_i32 (&sampling_thread_exiting, 1);

	pthread_setschedparam (pthread_self (), old_policy, &old_sched);

	mono_thread_info_detach ();

	return NULL;
}

void
mono_runtime_shutdown_stat_profiler (void)
{
	mono_atomic_store_i32 (&sampling_thread_running, 0);

	mono_profiler_sampling_thread_post ();

#ifndef HOST_DARWIN
	/*
	 * There is a slight problem when we're using CLOCK_PROCESS_CPUTIME_ID: If
	 * we're shutting down and there's largely no activity in the process other
	 * than waiting for the sampler thread to shut down, it can take upwards of
	 * 20 seconds (depending on a lot of factors) for us to shut down because
	 * the sleep progresses very slowly as a result of the low CPU activity.
	 *
	 * We fix this by repeatedly sending the profiler signal to the sampler
	 * thread in order to interrupt the sleep. clock_sleep_ns_abs () will check
	 * sampling_thread_running upon an interrupt and return immediately if it's
	 * zero. profiler_signal_handler () has a special case to ignore the signal
	 * for the sampler thread.
	 */
	MonoThreadInfo *info;

	// Did it shut down already?
	if ((info = mono_thread_info_lookup (sampling_thread))) {
		while (!mono_atomic_load_i32 (&sampling_thread_exiting)) {
			mono_threads_pthread_kill (info, profiler_signal);
			mono_thread_info_usleep (10 * 1000 /* 10ms */);
		}

		// Make sure info can be freed.
		mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);
	}
#endif

	mono_native_thread_join (sampling_thread);

	/*
	 * We can't safely remove the signal handler because we have no guarantee
	 * that all pending signals have been delivered at this point. This should
	 * not really be a problem anyway.
	 */
	//remove_signal_handler (profiler_signal);
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

	mono_atomic_store_i32 (&sampling_thread_running, 1);
	mono_native_thread_create (&sampling_thread, sampling_thread_func, NULL);
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

static gboolean
native_stack_with_gdb (pid_t crashed_pid, const char **argv, FILE *commands, char* commands_filename)
{
	gchar *gdb;

	gdb = g_find_program_in_path ("gdb");
	if (!gdb)
		return FALSE;

	argv [0] = gdb;
	argv [1] = "-batch";
	argv [2] = "-x";
	argv [3] = commands_filename;
	argv [4] = "-nx";

	fprintf (commands, "attach %ld\n", (long) crashed_pid);
	fprintf (commands, "info threads\n");
	fprintf (commands, "thread apply all bt\n");

	return TRUE;
}


static gboolean
native_stack_with_lldb (pid_t crashed_pid, const char **argv, FILE *commands, char* commands_filename)
{
	gchar *lldb;

	lldb = g_find_program_in_path ("lldb");
	if (!lldb)
		return FALSE;

	argv [0] = lldb;
	argv [1] = "--batch";
	argv [2] = "--source";
	argv [3] = commands_filename;
	argv [4] = "--no-lldbinit";

	fprintf (commands, "process attach --pid %ld\n", (long) crashed_pid);
	fprintf (commands, "thread list\n");
	fprintf (commands, "thread backtrace all\n");
	fprintf (commands, "detach\n");
	fprintf (commands, "quit\n");

	return TRUE;
}

void
mono_gdb_render_native_backtraces (pid_t crashed_pid)
{
#ifdef HAVE_EXECV
	const char *argv [10];
	FILE *commands;
	char commands_filename [] = "/tmp/mono-gdb-commands.XXXXXX";

	if (mkstemp (commands_filename) == -1)
		return;

	commands = fopen (commands_filename, "w");
	if (!commands) {
		unlink (commands_filename);
		return;
	}

	memset (argv, 0, sizeof (char*) * 10);

#if defined(HOST_DARWIN)
	if (native_stack_with_lldb (crashed_pid, argv, commands, commands_filename))
		goto exec;
#endif

	if (native_stack_with_gdb (crashed_pid, argv, commands, commands_filename))
		goto exec;

#if !defined(HOST_DARWIN)
	if (native_stack_with_lldb (crashed_pid, argv, commands, commands_filename))
		goto exec;
#endif

	fprintf (stderr, "mono_gdb_render_native_backtraces not supported on this platform, unable to find gdb or lldb\n");

	fclose (commands);
	unlink (commands_filename);
	return;

exec:
	fclose (commands);
	execv (argv [0], (char**)argv);

	_exit (-1);
#else
	fprintf (stderr, "mono_gdb_render_native_backtraces not supported on this platform\n");
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
