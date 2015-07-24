/*
 * mono-threads-posix.c: Low-level threading, posix version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include <config.h>

/* For pthread_main_np, pthread_get_stackaddr_np and pthread_get_stacksize_np */
#if defined (__MACH__)
#define _DARWIN_C_SOURCE 1
#endif

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-mmap.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/gc-internal.h>
#include <limits.h>

#include <errno.h>

#if defined(PLATFORM_ANDROID) && !defined(TARGET_ARM64) && !defined(TARGET_AMD64)
#define USE_TKILL_ON_ANDROID 1
#endif

#ifdef USE_TKILL_ON_ANDROID
extern int tkill (pid_t tid, int signal);
#endif

#if defined(_POSIX_VERSION) || defined(__native_client__)
#include <sys/resource.h>
#include <signal.h>

#if defined(__native_client__)
void nacl_shutdown_gc_thread(void);
#endif

typedef struct {
	void *(*start_routine)(void*);
	void *arg;
	int flags;
	MonoSemType registered;
	HANDLE handle;
} StartInfo;

static void*
inner_start_thread (void *arg)
{
	StartInfo *start_info = (StartInfo *) arg;
	void *t_arg = start_info->arg;
	int res;
	void *(*start_func)(void*) = start_info->start_routine;
	guint32 flags = start_info->flags;
	void *result;
	HANDLE handle;
	MonoThreadInfo *info;

	/* Register the thread with the io-layer */
	handle = wapi_create_thread_handle ();
	if (!handle) {
		res = MONO_SEM_POST (&(start_info->registered));
		g_assert (!res);
		return NULL;
	}
	start_info->handle = handle;

	info = mono_thread_info_attach (&result);
	MONO_PREPARE_BLOCKING

	info->runtime_thread = TRUE;
	info->handle = handle;

	if (flags & CREATE_SUSPENDED) {
		info->create_suspended = TRUE;
		MONO_SEM_INIT (&info->create_suspended_sem, 0);
	}

	/* start_info is not valid after this */
	res = MONO_SEM_POST (&(start_info->registered));
	g_assert (!res);
	start_info = NULL;

	if (flags & CREATE_SUSPENDED) {
		while (MONO_SEM_WAIT (&info->create_suspended_sem) != 0 &&
			   errno == EINTR);
		MONO_SEM_DESTROY (&info->create_suspended_sem);
	}

	MONO_FINISH_BLOCKING
	/* Run the actual main function of the thread */
	result = start_func (t_arg);

	mono_threads_core_exit (GPOINTER_TO_UINT (result));
	g_assert_not_reached ();
}

HANDLE
mono_threads_core_create_thread (LPTHREAD_START_ROUTINE start_routine, gpointer arg, guint32 stack_size, guint32 creation_flags, MonoNativeThreadId *out_tid)
{
	pthread_attr_t attr;
	int res;
	pthread_t thread;
	StartInfo start_info;

	res = pthread_attr_init (&attr);
	g_assert (!res);

	if (stack_size == 0) {
#if HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND)
			stack_size = 1 << 20;
		else
			stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#else
		stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#endif
	}

#ifdef PTHREAD_STACK_MIN
	if (stack_size < PTHREAD_STACK_MIN)
		stack_size = PTHREAD_STACK_MIN;
#endif

#ifdef HAVE_PTHREAD_ATTR_SETSTACKSIZE
	res = pthread_attr_setstacksize (&attr, stack_size);
	g_assert (!res);
#endif

	memset (&start_info, 0, sizeof (StartInfo));
	start_info.start_routine = (void *(*)(void *)) start_routine;
	start_info.arg = arg;
	start_info.flags = creation_flags;
	MONO_SEM_INIT (&(start_info.registered), 0);

	/* Actually start the thread */
	res = mono_gc_pthread_create (&thread, &attr, inner_start_thread, &start_info);
	if (res) {
		MONO_SEM_DESTROY (&(start_info.registered));
		return NULL;
	}

	/* Wait until the thread register itself in various places */
	while (MONO_SEM_WAIT (&(start_info.registered)) != 0) {
		/*if (EINTR != errno) ABORT("sem_wait failed"); */
	}
	MONO_SEM_DESTROY (&(start_info.registered));

	if (out_tid)
		*out_tid = thread;

	return start_info.handle;
}

/*
 * mono_threads_core_resume_created:
 *
 *   Resume a newly created thread created using CREATE_SUSPENDED.
 */
void
mono_threads_core_resume_created (MonoThreadInfo *info, MonoNativeThreadId tid)
{
	MONO_SEM_POST (&info->create_suspended_sem);
}

gboolean
mono_threads_core_yield (void)
{
	return sched_yield () == 0;
}

void
mono_threads_core_exit (int exit_code)
{
	MonoThreadInfo *current = mono_thread_info_current ();

#if defined(__native_client__)
	nacl_shutdown_gc_thread();
#endif

	wapi_thread_handle_set_exited (current->handle, exit_code);

	mono_thread_info_detach ();

	pthread_exit (NULL);
}

void
mono_threads_core_unregister (MonoThreadInfo *info)
{
	if (info->handle) {
		wapi_thread_handle_set_exited (info->handle, 0);
		info->handle = NULL;
	}
}

HANDLE
mono_threads_core_open_handle (void)
{
	MonoThreadInfo *info;

	info = mono_thread_info_current ();
	g_assert (info);

	if (!info->handle)
		info->handle = wapi_create_thread_handle ();
	else
		wapi_ref_thread_handle (info->handle);
	return info->handle;
}

int
mono_threads_get_max_stack_size (void)
{
	struct rlimit lim;

	/* If getrlimit fails, we don't enforce any limits. */
	if (getrlimit (RLIMIT_STACK, &lim))
		return INT_MAX;
	/* rlim_t is an unsigned long long on 64bits OSX but we want an int response. */
	if (lim.rlim_max > (rlim_t)INT_MAX)
		return INT_MAX;
	return (int)lim.rlim_max;
}

HANDLE
mono_threads_core_open_thread_handle (HANDLE handle, MonoNativeThreadId tid)
{
	wapi_ref_thread_handle (handle);

	return handle;
}

gpointer
mono_threads_core_prepare_interrupt (HANDLE thread_handle)
{
	return wapi_prepare_interrupt_thread (thread_handle);
}

void
mono_threads_core_finish_interrupt (gpointer wait_handle)
{
	wapi_finish_interrupt_thread (wait_handle);
}

void
mono_threads_core_self_interrupt (void)
{
	wapi_self_interrupt ();
}

void
mono_threads_core_clear_interruption (void)
{
	wapi_clear_interruption ();
}

int
mono_threads_pthread_kill (MonoThreadInfo *info, int signum)
{
	THREADS_SUSPEND_DEBUG ("sending signal %d to %p[%p]\n", signum, info, mono_thread_info_get_tid (info));
#ifdef USE_TKILL_ON_ANDROID
	int result, old_errno = errno;
	result = tkill (info->native_handle, signum);
	if (result < 0) {
		result = errno;
		errno = old_errno;
	}
	return result;
#elif defined(__native_client__)
	/* Workaround pthread_kill abort() in NaCl glibc. */
	return 0;
#elif !defined(HAVE_PTHREAD_KILL)
	g_error ("pthread_kill() is not supported by this platform");
#else
	return pthread_kill (mono_thread_info_get_tid (info), signum);
#endif
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
	return pthread_create (tid, NULL, (void *(*)(void *)) func, arg) == 0;
}

void
mono_threads_core_set_name (MonoNativeThreadId tid, const char *name)
{
#if defined (HAVE_PTHREAD_SETNAME_NP) && !defined (__MACH__)
	if (!name) {
		pthread_setname_np (tid, "");
	} else {
		char n [16];

		strncpy (n, name, 16);
		n [15] = '\0';
		pthread_setname_np (tid, n);
	}
#endif
}


#if defined (USE_POSIX_BACKEND) && !defined (USE_COOP_GC)

static int suspend_signal_num;
static int restart_signal_num;
static int abort_signal_num;
static sigset_t suspend_signal_mask;
static sigset_t suspend_ack_signal_mask;


#if defined(__APPLE__) || defined(__OpenBSD__) || defined(__FreeBSD__) || defined(__FreeBSD_kernel__)
#define DEFAULT_SUSPEND_SIGNAL SIGXFSZ
#else
#define DEFAULT_SUSPEND_SIGNAL SIGPWR
#endif
#define DEFAULT_RESTART_SIGNAL SIGXCPU

static int
mono_thread_search_alt_signal (int min_signal)
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

static int
mono_thread_get_alt_suspend_signal (void)
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
		suspend_signum = mono_thread_search_alt_signal (-1);
	return suspend_signum;
#endif /* SIGRTMIN */
}

static int
mono_thread_get_alt_resume_signal (void)
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
		resume_signum = mono_thread_search_alt_signal (mono_thread_get_alt_suspend_signal () + 1);
	return resume_signum;
#endif /* SIGRTMIN */
}


static int
mono_threads_get_abort_signal (void)
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
		abort_signum = mono_thread_search_alt_signal (mono_thread_get_alt_resume_signal () + 1);
	return abort_signum;
#endif /* SIGRTMIN */
}


#if !defined(__native_client__)
static void
restart_signal_handler (int _dummy, siginfo_t *_info, void *context)
{
	MonoThreadInfo *info;
	int old_errno = errno;

	info = mono_thread_info_current ();
	info->signal = restart_signal_num;
	errno = old_errno;
}

static void
suspend_signal_handler (int _dummy, siginfo_t *info, void *context)
{
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


	/*
	Block the restart signal.
	We need to block the restart signal while posting to the suspend_ack semaphore or we race to sigsuspend,
	which might miss the signal and get stuck.
	*/
	pthread_sigmask (SIG_BLOCK, &suspend_ack_signal_mask, NULL);

	/* We're done suspending */
	mono_threads_notify_initiator_of_suspend (current);

	/* This thread is doomed, all we can do is give up and let the suspender recover. */
	if (!ret) {
		THREADS_SUSPEND_DEBUG ("\tThread is dying, failed to capture state %p\n", current);
		mono_threads_transition_async_suspend_compensation (current);
		/* Unblock the restart signal. */
		pthread_sigmask (SIG_UNBLOCK, &suspend_ack_signal_mask, NULL);

		goto done;
	}

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
		current->async_target = current->user_data = NULL;
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
}

static void
abort_signal_handler (int _dummy, siginfo_t *info, void *context)
{
	suspend_signal_handler (_dummy, info, context);
}

#endif

static void
mono_posix_add_signal_handler (int signo, gpointer handler, int flags)
{
#if !defined(__native_client__)
	/*FIXME, move the code from mini to utils and do the right thing!*/
	struct sigaction sa;
	struct sigaction previous_sa;
	int ret;

	sa.sa_sigaction = handler;
	sigfillset (&sa.sa_mask);

	sa.sa_flags = SA_SIGINFO | flags;
	ret = sigaction (signo, &sa, &previous_sa);

	g_assert (ret != -1);
#endif
}

void
mono_threads_init_platform (void)
{
	sigset_t signal_set;

	abort_signal_num = mono_threads_get_abort_signal ();
	if (mono_thread_info_unified_management_enabled ()) {
		suspend_signal_num = DEFAULT_SUSPEND_SIGNAL;
		restart_signal_num = DEFAULT_RESTART_SIGNAL;
	} else {
		suspend_signal_num = mono_thread_get_alt_suspend_signal ();
		restart_signal_num = mono_thread_get_alt_resume_signal ();
	}

	sigfillset (&suspend_signal_mask);
	sigdelset (&suspend_signal_mask, restart_signal_num);

	sigemptyset (&suspend_ack_signal_mask);
	sigaddset (&suspend_ack_signal_mask, restart_signal_num);

	mono_posix_add_signal_handler (suspend_signal_num, suspend_signal_handler, SA_RESTART);
	mono_posix_add_signal_handler (restart_signal_num, restart_signal_handler, SA_RESTART);
	mono_posix_add_signal_handler (abort_signal_num, abort_signal_handler, 0);

	/* ensure all the new signals are unblocked */
	sigemptyset (&signal_set);
	sigaddset (&signal_set, suspend_signal_num);
	sigaddset (&signal_set, restart_signal_num);
	sigaddset (&signal_set, abort_signal_num);
	sigprocmask (SIG_UNBLOCK, &signal_set, NULL);
}

void
mono_threads_core_abort_syscall (MonoThreadInfo *info)
{
	/*
	We signal a thread to break it from the urrent syscall.
	This signal should not be interpreted as a suspend request.
	*/
	info->syscall_break_signal = TRUE;
	if (!mono_threads_pthread_kill (info, abort_signal_num))
		mono_threads_add_to_pending_operation_set (info);
}

gboolean
mono_threads_core_needs_abort_syscall (void)
{
	return TRUE;
}

gboolean
mono_threads_core_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	int sig = interrupt_kernel ? abort_signal_num :  suspend_signal_num;

	if (!mono_threads_pthread_kill (info, sig)) {
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	}
	return FALSE;
}

gboolean
mono_threads_core_check_suspend_result (MonoThreadInfo *info)
{
	return info->suspend_can_continue;
}

/*
This begins async resume. This function must do the following:

- Install an async target if one was requested.
- Notify the target to resume.
*/
gboolean
mono_threads_core_begin_async_resume (MonoThreadInfo *info)
{
	mono_threads_add_to_pending_operation_set (info);
	return mono_threads_pthread_kill (info, restart_signal_num) == 0;
}

void
mono_threads_platform_register (MonoThreadInfo *info)
{
#if defined (PLATFORM_ANDROID)
	info->native_handle = gettid ();
#endif
}

void
mono_threads_platform_free (MonoThreadInfo *info)
{
}

void
mono_threads_core_begin_global_suspend (void)
{
}

void
mono_threads_core_end_global_suspend (void)
{
}

#endif /*defined (USE_POSIX_BACKEND)*/

#endif
