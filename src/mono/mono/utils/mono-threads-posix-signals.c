/*
 * mono-threads-posix-signals.c: Shared facility for Posix signals support
 *
 * Author:
 *	Ludovic Henry (ludovic@gmail.com)
 *
 * (C) 2015 Xamarin, Inc
 */

#include <config.h>
#include <glib.h>

#include "mono-threads.h"

#if defined(USE_POSIX_BACKEND)

#include <errno.h>
#include <signal.h>

#include "mono-threads-posix-signals.h"

#if defined(__APPLE__) || defined(__OpenBSD__) || defined(__FreeBSD__) || defined(__FreeBSD_kernel__)
#define DEFAULT_SUSPEND_SIGNAL SIGXFSZ
#else
#define DEFAULT_SUSPEND_SIGNAL SIGPWR
#endif
#define DEFAULT_RESTART_SIGNAL SIGXCPU

static int suspend_signal_num;
static int restart_signal_num;
static int abort_signal_num;

static sigset_t suspend_signal_mask;
static sigset_t suspend_ack_signal_mask;

//Can't avoid the circular dep on this. Will be gone pretty soon
extern int mono_gc_get_suspend_signal (void);

static int
signal_search_alternative (int min_signal)
{
#if !defined (SIGRTMIN)
	g_error ("signal search only works with RTMIN");
#else
	int i;
	/* we try to avoid SIGRTMIN and any one that might have been set already, see bug #75387 */
	for (i = MAX (min_signal, SIGRTMIN) + 1; i < SIGRTMAX; ++i) {
		struct sigaction sinfo;
		sigaction (i, NULL, &sinfo);
		if (sinfo.sa_handler == SIG_DFL && (void*)sinfo.sa_sigaction == (void*)SIG_DFL) {
			return i;
		}
	}
	g_error ("Could not find an available signal");
#endif
}

static void
signal_add_handler (int signo, gpointer handler, int flags)
{
#if !defined(__native_client__)
	/*FIXME, move the code from mini to utils and do the right thing!*/
	struct sigaction sa;
	struct sigaction previous_sa;
	int ret;

	sa.sa_sigaction = (void (*)(int, siginfo_t *, void *))handler;
	sigfillset (&sa.sa_mask);

	sa.sa_flags = SA_SIGINFO | flags;
	ret = sigaction (signo, &sa, &previous_sa);

	g_assert (ret != -1);
#endif
}

static int
suspend_signal_get (void)
{
#if defined(PLATFORM_ANDROID)
	return SIGUNUSED;
#elif !defined (SIGRTMIN)
#ifdef SIGUSR1
	return SIGUSR1;
#else
	return -1;
#endif /* SIGUSR1 */
#else
	static int suspend_signum = -1;
	if (suspend_signum == -1)
		suspend_signum = signal_search_alternative (-1);
	return suspend_signum;
#endif /* SIGRTMIN */
}

static int
restart_signal_get (void)
{
#if defined(PLATFORM_ANDROID)
	return SIGTTOU;
#elif !defined (SIGRTMIN)
#ifdef SIGUSR2
	return SIGUSR2;
#else
	return -1;
#endif /* SIGUSR1 */
#else
	static int resume_signum = -1;
	if (resume_signum == -1)
		resume_signum = signal_search_alternative (suspend_signal_get () + 1);
	return resume_signum;
#endif /* SIGRTMIN */
}


static int
abort_signal_get (void)
{
#if defined(PLATFORM_ANDROID)
	return SIGTTIN;
#elif !defined (SIGRTMIN)
#ifdef SIGTTIN
	return SIGTTIN;
#else
	return -1;
#endif /* SIGRTMIN */
#else
	static int abort_signum = -1;
	if (abort_signum == -1)
		abort_signum = signal_search_alternative (restart_signal_get () + 1);
	return abort_signum;
#endif /* SIGRTMIN */
}

static void
restart_signal_handler (int _dummy, siginfo_t *_info, void *context)
{
#if defined(__native_client__)
	g_assert_not_reached ();
#else
	MonoThreadInfo *info;
	int old_errno = errno;

	info = mono_thread_info_current ();
	info->signal = restart_signal_num;
	errno = old_errno;
#endif
}

static void
suspend_signal_handler (int _dummy, siginfo_t *info, void *context)
{
#if defined(__native_client__)
	g_assert_not_reached ();
#else
	int old_errno = errno;
	int hp_save_index = mono_hazard_pointer_save_for_signal_handler ();


	MonoThreadInfo *current = mono_thread_info_current ();
	gboolean ret;

	THREADS_SUSPEND_DEBUG ("SIGNAL HANDLER FOR %p [%p]\n", current, (void*)current->native_handle);
	if (current->syscall_break_signal) {
		current->syscall_break_signal = FALSE;
		THREADS_SUSPEND_DEBUG ("\tsyscall break for %p\n", current);
		mono_threads_notify_initiator_of_abort (current);
		goto done;
	}

	/* Have we raced with self suspend? */
	if (!mono_threads_transition_finish_async_suspend (current)) {
		current->suspend_can_continue = TRUE;
		THREADS_SUSPEND_DEBUG ("\tlost race with self suspend %p\n", current);
		goto done;
	}

	ret = mono_threads_get_runtime_callbacks ()->thread_state_init_from_sigctx (&current->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX], context);

	/* thread_state_init_from_sigctx return FALSE if the current thread is detaching and suspend can't continue. */
	current->suspend_can_continue = ret;

	/* This thread is doomed, all we can do is give up and let the suspender recover. */
	if (!ret) {
		THREADS_SUSPEND_DEBUG ("\tThread is dying, failed to capture state %p\n", current);
		mono_threads_transition_async_suspend_compensation (current);

		/* We're done suspending */
		mono_threads_notify_initiator_of_suspend (current);

		goto done;
	}

	/*
	Block the restart signal.
	We need to block the restart signal while posting to the suspend_ack semaphore or we race to sigsuspend,
	which might miss the signal and get stuck.
	*/
	pthread_sigmask (SIG_BLOCK, &suspend_ack_signal_mask, NULL);

	/* We're done suspending */
	mono_threads_notify_initiator_of_suspend (current);

	do {
		current->signal = 0;
		sigsuspend (&suspend_signal_mask);
	} while (current->signal != restart_signal_num);

	/* Unblock the restart signal. */
	pthread_sigmask (SIG_UNBLOCK, &suspend_ack_signal_mask, NULL);

	if (current->async_target) {
#if MONO_ARCH_HAS_MONO_CONTEXT
		MonoContext tmp = current->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX].ctx;
		mono_threads_get_runtime_callbacks ()->setup_async_callback (&tmp, current->async_target, current->user_data);
		current->user_data = NULL;
		current->async_target = NULL;
		mono_monoctx_to_sigctx (&tmp, context);
#else
		g_error ("The new interruption machinery requires a working mono-context");
#endif
	}

	/* We're done resuming */
	mono_threads_notify_initiator_of_resume (current);

done:
	mono_hazard_pointer_restore_for_signal_handler (hp_save_index);
	errno = old_errno;
#endif
}

static void
abort_signal_handler (int _dummy, siginfo_t *info, void *context)
{
#if defined(__native_client__)
	g_assert_not_reached ();
#else
	suspend_signal_handler (_dummy, info, context);
#endif
}

void
mono_threads_posix_init_signals (MonoThreadPosixInitSignals signals)
{
	sigset_t signal_set;

	g_assert ((signals == MONO_THREADS_POSIX_INIT_SIGNALS_SUSPEND_RESTART) ^ (signals == MONO_THREADS_POSIX_INIT_SIGNALS_ABORT));

	sigemptyset (&signal_set);

	switch (signals) {
	case MONO_THREADS_POSIX_INIT_SIGNALS_SUSPEND_RESTART: {
		if (mono_thread_info_unified_management_enabled ()) {
			suspend_signal_num = DEFAULT_SUSPEND_SIGNAL;
			restart_signal_num = DEFAULT_RESTART_SIGNAL;
		} else {
			suspend_signal_num = suspend_signal_get ();
			restart_signal_num = restart_signal_get ();
		}

		sigfillset (&suspend_signal_mask);
		sigdelset (&suspend_signal_mask, restart_signal_num);
		if (!mono_thread_info_unified_management_enabled ())
			sigdelset (&suspend_signal_mask, mono_gc_get_suspend_signal ());

		sigemptyset (&suspend_ack_signal_mask);
		sigaddset (&suspend_ack_signal_mask, restart_signal_num);

		signal_add_handler (suspend_signal_num, suspend_signal_handler, SA_RESTART);
		signal_add_handler (restart_signal_num, restart_signal_handler, SA_RESTART);

		sigaddset (&signal_set, suspend_signal_num);
		sigaddset (&signal_set, restart_signal_num);

		break;
	}
	case MONO_THREADS_POSIX_INIT_SIGNALS_ABORT: {
		abort_signal_num = abort_signal_get ();

		signal_add_handler (abort_signal_num, abort_signal_handler, 0);

		sigaddset (&signal_set, abort_signal_num);

		break;
	}
	default: g_assert_not_reached ();
	}

	/* ensure all the new signals are unblocked */
	sigprocmask (SIG_UNBLOCK, &signal_set, NULL);
}

gint
mono_threads_posix_get_suspend_signal (void)
{
	return suspend_signal_num;
}

gint
mono_threads_posix_get_restart_signal (void)
{
	return restart_signal_num;
}

gint
mono_threads_posix_get_abort_signal (void)
{
	return abort_signal_num;
}

#endif /* defined(USE_POSIX_BACKEND) */
