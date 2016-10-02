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
#include <mono/utils/w32handle.h>

#include <errno.h>

#if defined(PLATFORM_ANDROID) && !defined(TARGET_ARM64) && !defined(TARGET_AMD64)
#define USE_TKILL_ON_ANDROID 1
#endif

#ifdef USE_TKILL_ON_ANDROID
extern int tkill (pid_t tid, int signal);
#endif

#if defined(_POSIX_VERSION) || defined(__native_client__)

#include <pthread.h>

#include <sys/resource.h>

#if defined(__native_client__)
void nacl_shutdown_gc_thread(void);
#endif

void
mono_threads_platform_register (MonoThreadInfo *info)
{
	gpointer thread_handle;

	thread_handle = mono_w32handle_new (MONO_W32HANDLE_THREAD, NULL);
	if (thread_handle == INVALID_HANDLE_VALUE)
		g_error ("%s: failed to create handle", __func__);

	g_assert (!info->handle);
	info->handle = thread_handle;
}

int
mono_threads_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data, gsize stack_size, MonoNativeThreadId *out_tid)
{
	pthread_attr_t attr;
	pthread_t thread;
	int policy;
	struct sched_param param;
	gint res;

	res = pthread_attr_init (&attr);
	g_assert (!res);

#ifdef HAVE_PTHREAD_ATTR_SETSTACKSIZE
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

	res = pthread_attr_setstacksize (&attr, stack_size);
	g_assert (!res);
#endif /* HAVE_PTHREAD_ATTR_SETSTACKSIZE */

	memset (&param, 0, sizeof (param));

	res = pthread_attr_getschedpolicy (&attr, &policy);
	if (res != 0)
		g_error ("%s: pthread_attr_getschedpolicy failed, error: \"%s\" (%d)", g_strerror (res), res);

#ifdef _POSIX_PRIORITY_SCHEDULING
	int max, min;

	/* Necessary to get valid priority range */

	min = sched_get_priority_min (policy);
	max = sched_get_priority_max (policy);

	if (max > 0 && min >= 0 && max > min)
		param.sched_priority = (max - min) / 2 + min;
	else
#endif
	{
		switch (policy) {
		case SCHED_FIFO:
		case SCHED_RR:
			param.sched_priority = 50;
			break;
#ifdef SCHED_BATCH
		case SCHED_BATCH:
#endif
		case SCHED_OTHER:
			param.sched_priority = 0;
			break;
		default:
			g_error ("%s: unknown policy %d", __func__, policy);
		}
	}

	res = pthread_attr_setschedparam (&attr, &param);
	if (res != 0)
		g_error ("%s: pthread_attr_setschedparam failed, error: \"%s\" (%d)", g_strerror (res), res);

	/* Actually start the thread */
	res = mono_gc_pthread_create (&thread, &attr, (gpointer (*)(gpointer)) thread_fn, thread_data);
	if (res)
		return -1;

	if (out_tid)
		*out_tid = thread;

	return 0;
}

gboolean
mono_threads_platform_yield (void)
{
	return sched_yield () == 0;
}

void
mono_threads_platform_exit (int exit_code)
{
#if defined(__native_client__)
	nacl_shutdown_gc_thread();
#endif

	mono_thread_info_detach ();

	pthread_exit (NULL);
}

void
mono_threads_platform_unregister (MonoThreadInfo *info)
{
	g_assert (info->handle);

	/* The thread is no longer active, so unref it */
	mono_w32handle_unref (info->handle);
	info->handle = NULL;
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

gpointer
mono_threads_platform_duplicate_handle (MonoThreadInfo *info)
{
	g_assert (info->handle);
	mono_w32handle_ref (info->handle);
	return info->handle;
}

HANDLE
mono_threads_platform_open_thread_handle (HANDLE handle, MonoNativeThreadId tid)
{
	mono_w32handle_ref (handle);

	return handle;
}

void
mono_threads_platform_close_thread_handle (HANDLE handle)
{
	mono_w32handle_unref (handle);
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
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name)
{
#ifdef __MACH__
	/*
	 * We can't set the thread name for other threads, but we can at least make
	 * it work for threads that try to change their own name.
	 */
	if (tid != mono_native_thread_id_get ())
		return;

	if (!name) {
		pthread_setname_np ("");
	} else {
		char n [63];

		strncpy (n, name, 63);
		n [62] = '\0';
		pthread_setname_np (n);
	}
#elif defined (__NetBSD__)
	if (!name) {
		pthread_setname_np (tid, "%s", (void*)"");
	} else {
		char n [PTHREAD_MAX_NAMELEN_NP];

		strncpy (n, name, PTHREAD_MAX_NAMELEN_NP);
		n [PTHREAD_MAX_NAMELEN_NP - 1] = '\0';
		pthread_setname_np (tid, "%s", (void*)n);
	}
#elif defined (HAVE_PTHREAD_SETNAME_NP)
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

gboolean
mono_native_thread_join (MonoNativeThreadId tid)
{
	void *res;

	return !pthread_join (tid, &res);
}

void
mono_threads_platform_set_exited (gpointer handle)
{
	int thr_ret;

	g_assert (handle);
	if (mono_w32handle_issignalled (handle))
		g_error ("%s: handle %p thread %p has already exited, it's handle is signalled", __func__, handle, mono_native_thread_id_get ());
	if (mono_w32handle_get_type (handle) == MONO_W32HANDLE_UNUSED)
		g_error ("%s: handle %p thread %p has already exited, it's handle type is 'unused'", __func__, handle, mono_native_thread_id_get ());

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	mono_w32handle_set_signal_state (handle, TRUE, TRUE);

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
}

static const gchar* thread_typename (void)
{
	return "Thread";
}

static gsize thread_typesize (void)
{
	return 0;
}

static MonoW32HandleOps thread_ops = {
	NULL,				/* close */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	NULL,				/* special_wait */
	NULL,				/* prewait */
	NULL,				/* details */
	thread_typename,	/* typename */
	thread_typesize,	/* typesize */
};

void
mono_threads_platform_init (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_THREAD, &thread_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_THREAD, MONO_W32HANDLE_CAP_WAIT);
}

#endif /* defined(_POSIX_VERSION) || defined(__native_client__) */

#if defined(USE_POSIX_BACKEND)

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	int sig = interrupt_kernel ? mono_threads_posix_get_abort_signal () :  mono_threads_posix_get_suspend_signal ();

	if (!mono_threads_pthread_kill (info, sig)) {
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	}
	return FALSE;
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	return info->suspend_can_continue;
}

/*
This begins async resume. This function must do the following:

- Install an async target if one was requested.
- Notify the target to resume.
*/
gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	mono_threads_add_to_pending_operation_set (info);
	return mono_threads_pthread_kill (info, mono_threads_posix_get_restart_signal ()) == 0;
}

void
mono_threads_suspend_register (MonoThreadInfo *info)
{
#if defined (PLATFORM_ANDROID)
	info->native_handle = gettid ();
#endif
}

void
mono_threads_suspend_free (MonoThreadInfo *info)
{
}

void
mono_threads_suspend_init (void)
{
	mono_threads_posix_init_signals (MONO_THREADS_POSIX_INIT_SIGNALS_SUSPEND_RESTART);
}

#endif /* defined(USE_POSIX_BACKEND) */
