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

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-posix-signals.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/metadata/gc-internals.h>

#include <errno.h>

#if defined(PLATFORM_ANDROID) && !defined(TARGET_ARM64) && !defined(TARGET_AMD64)
#define USE_TKILL_ON_ANDROID 1
#endif

#ifdef USE_TKILL_ON_ANDROID
extern int tkill (pid_t tid, int signal);
#endif

#if defined(_POSIX_VERSION) || defined(__native_client__)

#include <sys/resource.h>

#if defined(__native_client__)
void nacl_shutdown_gc_thread(void);
#endif

typedef struct {
	void *(*start_routine)(void*);
	void *arg;
	int flags;
	MonoCoopSem registered;
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
		res = mono_coop_sem_post (&(start_info->registered));
		g_assert (!res);
		return NULL;
	}
	start_info->handle = handle;

	info = mono_thread_info_attach (&result);

	info->runtime_thread = TRUE;
	info->handle = handle;

	if (flags & CREATE_SUSPENDED) {
		info->create_suspended = TRUE;
		mono_coop_sem_init (&info->create_suspended_sem, 0);
	}

	/* start_info is not valid after this */
	res = mono_coop_sem_post (&(start_info->registered));
	g_assert (!res);
	start_info = NULL;

	if (flags & CREATE_SUSPENDED) {
		res = mono_coop_sem_wait (&info->create_suspended_sem, MONO_SEM_FLAGS_NONE);
		g_assert (res != -1);

		mono_coop_sem_destroy (&info->create_suspended_sem);
	}

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
	mono_coop_sem_init (&(start_info.registered), 0);

	/* Actually start the thread */
	res = mono_gc_pthread_create (&thread, &attr, inner_start_thread, &start_info);
	if (res) {
		mono_coop_sem_destroy (&(start_info.registered));
		return NULL;
	}

	/* Wait until the thread register itself in various places */
	res = mono_coop_sem_wait (&start_info.registered, MONO_SEM_FLAGS_NONE);
	g_assert (res != -1);

	mono_coop_sem_destroy (&(start_info.registered));

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
	mono_coop_sem_post (&info->create_suspended_sem);
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

#endif /* defined(_POSIX_VERSION) || defined(__native_client__) */

#if defined(USE_POSIX_BACKEND)

gboolean
mono_threads_core_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	int sig = interrupt_kernel ? mono_threads_posix_get_abort_signal () :  mono_threads_posix_get_suspend_signal ();

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
	return mono_threads_pthread_kill (info, mono_threads_posix_get_restart_signal ()) == 0;
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
mono_threads_init_platform (void)
{
	mono_threads_posix_init_signals (MONO_THREADS_POSIX_INIT_SIGNALS_SUSPEND_RESTART);
}

#endif /* defined(USE_POSIX_BACKEND) */
