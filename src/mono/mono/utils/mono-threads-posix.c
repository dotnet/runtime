/**
 * \file
 * Low-level threading, posix version
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

#if defined (HOST_FUCHSIA)
#include <zircon/syscalls.h>
#endif

#if defined (__HAIKU__)
#include <os/kernel/OS.h>
#endif

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/mono-threads-debug.h>
#include <mono/utils/mono-errno.h>

#include <errno.h>

#if defined(_POSIX_VERSION) && !defined (HOST_WASM)

#include <pthread.h>

#include <sys/mman.h>

#ifdef HAVE_SYS_RESOURCE_H
#include <sys/resource.h>
#endif

static pthread_mutex_t memory_barrier_process_wide_mutex = PTHREAD_MUTEX_INITIALIZER;
static void *memory_barrier_process_wide_helper_page;

gboolean
mono_thread_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data, gsize* const stack_size, MonoNativeThreadId *tid)
{
	pthread_attr_t attr;
	pthread_t thread;
	gint res;
	gsize set_stack_size;

	res = pthread_attr_init (&attr);
	if (res != 0)
		g_error ("%s: pthread_attr_init failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

	if (stack_size)
		set_stack_size = *stack_size;
	else
		set_stack_size = 0;

#ifdef HAVE_PTHREAD_ATTR_SETSTACKSIZE
	if (set_stack_size == 0) {
#if HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND)
			set_stack_size = 1 << 20;
		else
			set_stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#else
		set_stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#endif
	}

#ifdef PTHREAD_STACK_MIN
	if (set_stack_size < PTHREAD_STACK_MIN)
		set_stack_size = PTHREAD_STACK_MIN;
#endif

	res = pthread_attr_setstacksize (&attr, set_stack_size);
	if (res != 0)
		g_error ("%s: pthread_attr_setstacksize failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);
#endif /* HAVE_PTHREAD_ATTR_SETSTACKSIZE */

	/* Actually start the thread */
	res = mono_gc_pthread_create (&thread, &attr, (gpointer (*)(gpointer)) thread_fn, thread_data);
	if (res) {
		res = pthread_attr_destroy (&attr);
		if (res != 0)
			g_error ("%s: pthread_attr_destroy failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

		return FALSE;
	}

	if (tid)
		*tid = thread;

	if (stack_size) {
		res = pthread_attr_getstacksize (&attr, stack_size);
		if (res != 0)
			g_error ("%s: pthread_attr_getstacksize failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);
	}

	res = pthread_attr_destroy (&attr);
	if (res != 0)
		g_error ("%s: pthread_attr_destroy failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

	return TRUE;
}

void
mono_threads_platform_init (void)
{
}

gboolean
mono_threads_platform_in_critical_region (THREAD_INFO_TYPE *info)
{
	return FALSE;
}

gboolean
mono_threads_platform_yield (void)
{
	return sched_yield () == 0;
}

void
mono_threads_platform_exit (gsize exit_code)
{
	pthread_exit ((gpointer) exit_code);
}

gboolean
mono_thread_platform_external_eventloop_keepalive_check (void)
{
	/* vanilla POSIX thread creation doesn't support an external eventloop: when the thread main
	   function returns, the thread is done.
	*/
	return FALSE;
}

#if HOST_FUCHSIA
int
mono_thread_info_get_system_max_stack_size (void)
{
	/* For now, we do not enforce any limits */
	return INT_MAX;
}

#else
int
mono_thread_info_get_system_max_stack_size (void)
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
#endif

int
mono_threads_pthread_kill (MonoThreadInfo *info, int signum)
{
	THREADS_SUSPEND_DEBUG ("sending signal %d to %p[%p]\n", signum, info, mono_thread_info_get_tid (info));

	const int signal_queue_ovf_retry_count G_GNUC_UNUSED = 5;
	const gulong signal_queue_ovf_sleep_us G_GNUC_UNUSED = 10 * 1000; /* 10 milliseconds */
	int retry_count G_GNUC_UNUSED = 0;
	int result;

#if defined (__linux__)
redo:
#endif

#ifdef USE_TKILL_ON_ANDROID
	{
		int old_errno = errno;

		result = tkill (info->native_handle, signum);

		if (result < 0) {
			result = errno;
			mono_set_errno (old_errno);
		}
	}
#elif defined (HAVE_PTHREAD_KILL)
	result = pthread_kill (mono_thread_info_get_tid (info), signum);
#else
	result = -1;
	g_error ("pthread_kill () is not supported by this platform");
#endif

	/*
	 * ESRCH just means the thread is gone; this is usually not fatal.
	 *
	 * ENOTSUP can occur if we try to send signals (e.g. for sampling) to Grand
	 * Central Dispatch threads on Apple platforms. This is kinda bad, but
	 * since there's really nothing we can do about it, we just ignore it and
	 * move on.
	 *
	 * All other error codes are ill-documented and usually stem from various
	 * OS-specific idiosyncracies. We want to know about these, so fail loudly.
	 * One example is EAGAIN on Linux, which indicates a signal queue overflow.
	 */
	if (result &&
	    result != ESRCH
#if defined (__MACH__) && defined (ENOTSUP)
	    && result != ENOTSUP
#endif
#if defined (__linux__)
	    && !(result == EAGAIN && retry_count < signal_queue_ovf_retry_count)
#endif
	    )
		g_error ("%s: pthread_kill failed with error %d - potential kernel OOM or signal queue overflow", __func__, result);

#if defined (__linux__)
	if (result == EAGAIN && retry_count < signal_queue_ovf_retry_count) {
		/* HACK: if the signal queue overflows on linux, try again a couple of times.
		 * Tries to address https://github.com/dotnet/runtime/issues/32377
		 */
		g_warning ("%s: pthread_kill failed with error %d - potential kernel OOM or signal queue overflow, sleeping for %ld microseconds", __func__, result, signal_queue_ovf_sleep_us);
		g_usleep (signal_queue_ovf_sleep_us);
		++retry_count;
		goto redo;
	}
#endif

	return result;
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

size_t
mono_native_thread_get_name (MonoNativeThreadId tid, char *name_out, size_t max_len)
{
#ifdef HAVE_PTHREAD_GETNAME_NP
	int error = pthread_getname_np(tid, name_out, max_len);
	if (error != 0)
		return 0;
	return strlen(name_out);
#else
	return 0;
#endif
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

		strncpy (n, name, sizeof (n) - 1);
		n [sizeof (n) - 1] = '\0';
		pthread_setname_np (n);
	}
#elif defined (__HAIKU__)
	thread_id haiku_tid;
	haiku_tid = get_pthread_thread_id (tid);
	if (!name) {
		rename_thread (haiku_tid, "");
	} else {
		rename_thread (haiku_tid, name);
	}
#elif defined (__NetBSD__)
	if (!name) {
		pthread_setname_np (tid, "%s", (void*)"");
	} else {
		char n [PTHREAD_MAX_NAMELEN_NP];

		strncpy (n, name, sizeof (n) - 1);
		n [sizeof (n) - 1] = '\0';
		pthread_setname_np (tid, "%s", (void*)n);
	}
#elif defined (HAVE_PTHREAD_SETNAME_NP)
#if defined (__linux__)
	/* Ignore requests to set the main thread name because it causes the
	 * value returned by Process.ProcessName to change.
	 */
	MonoNativeThreadId main_thread_tid;
	if (mono_native_thread_id_main_thread_known (&main_thread_tid) &&
	    mono_native_thread_id_equals (tid, main_thread_tid))
		return;
#endif
	if (!name) {
		pthread_setname_np (tid, "");
	} else {
#if defined(__FreeBSD__)
		char n [20];
#else
		char n [16];
#endif

		strncpy (n, name, sizeof (n) - 1);
		n [sizeof (n) - 1] = '\0';
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
mono_memory_barrier_process_wide (void)
{
	int status;

	status = pthread_mutex_lock (&memory_barrier_process_wide_mutex);
	g_assert (status == 0);

	if (memory_barrier_process_wide_helper_page == NULL) {
		status = posix_memalign (&memory_barrier_process_wide_helper_page, mono_pagesize (), mono_pagesize ());
		g_assert (status == 0);
	}

	// Changing a helper memory page protection from read / write to no access
	// causes the OS to issue IPI to flush TLBs on all processors. This also
	// results in flushing the processor buffers.
	status = mono_mprotect (memory_barrier_process_wide_helper_page, mono_pagesize (), MONO_MMAP_READ | MONO_MMAP_WRITE);
	g_assert (status == 0);

	// Ensure that the page is dirty before we change the protection so that
	// we prevent the OS from skipping the global TLB flush.
	__sync_add_and_fetch ((size_t*)memory_barrier_process_wide_helper_page, 1);

	status = mono_mprotect (memory_barrier_process_wide_helper_page, mono_pagesize (), MONO_MMAP_NONE);
	g_assert (status == 0);

	status = pthread_mutex_unlock (&memory_barrier_process_wide_mutex);
	g_assert (status == 0);
}

#endif /* defined(_POSIX_VERSION) */

#if defined(USE_POSIX_BACKEND)

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	int sig = interrupt_kernel ? mono_threads_suspend_get_abort_signal () :  mono_threads_suspend_get_suspend_signal ();

	if (!mono_threads_pthread_kill (info, sig)) {
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	}
	if (!mono_threads_transition_abort_async_suspend (info)) {
		/* We raced with self suspend and lost so suspend can continue. */
		g_assert (mono_threads_is_hybrid_suspension_enabled ());
		info->suspend_can_continue = TRUE;
		THREADS_SUSPEND_DEBUG ("\tlost race with self suspend %p\n", mono_thread_info_get_tid (info));
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
	int sig = mono_threads_suspend_get_restart_signal ();

	if (!mono_threads_pthread_kill (info, sig)) {
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	}
	return FALSE;
}

void
mono_threads_suspend_abort_syscall (MonoThreadInfo *info)
{
	/* We signal a thread to break it from the current syscall.
	 * This signal should not be interpreted as a suspend request. */
	info->syscall_break_signal = TRUE;
	if (mono_threads_pthread_kill (info, mono_threads_suspend_get_abort_signal ()) == 0) {
		mono_threads_add_to_pending_operation_set (info);
	}
}

void
mono_threads_suspend_register (MonoThreadInfo *info)
{
#if defined (HOST_ANDROID)
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
}

#endif /* defined(USE_POSIX_BACKEND) */
