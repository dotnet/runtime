/*
 * threads.c:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/mutex-private.h>

#include <mono/utils/mono-threads.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-mutex.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-time.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#if 0
#define DEBUG(...) g_message(__VA_ARGS__)
#else
#define DEBUG(...)
#endif

#if 0
#define WAIT_DEBUG(code) do { code } while (0)
#else
#define WAIT_DEBUG(code) do { } while (0)
#endif

struct _WapiHandleOps _wapi_thread_ops = {
	NULL,				/* close */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	NULL,				/* special_wait */
	NULL				/* prewait */
};

static mono_once_t thread_ops_once = MONO_ONCE_INIT;

static void
thread_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_THREAD,
					    WAPI_HANDLE_CAP_WAIT);
}

void
_wapi_thread_cleanup (void)
{
}

static gpointer
get_current_thread_handle (void)
{
	MonoThreadInfo *info;

	info = mono_thread_info_current ();
	g_assert (info);
	g_assert (info->handle);
	return info->handle;
}

static WapiHandle_thread*
lookup_thread (HANDLE handle)
{
	WapiHandle_thread *thread;
	gboolean ok;

	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
							  (gpointer *)&thread);
	g_assert (ok);
	return thread;
}

static WapiHandle_thread*
get_current_thread (void)
{
	gpointer handle;

	handle = get_current_thread_handle ();
	return lookup_thread (handle);
}

void
wapi_thread_handle_set_exited (gpointer handle, guint32 exitstatus)
{
	WapiHandle_thread *thread_handle;
	int i, thr_ret;
	pid_t pid = _wapi_getpid ();
	pthread_t tid = pthread_self ();
	
	if (_wapi_handle_issignalled (handle) ||
	    _wapi_handle_type (handle) == WAPI_HANDLE_UNUSED) {
		/* We must have already deliberately finished with
		 * this thread, so don't do any more now
		 */
		return;
	}

	DEBUG ("%s: Thread %p terminating", __func__, handle);

	thread_handle = lookup_thread (handle);

	DEBUG ("%s: Thread %p abandoning held mutexes", __func__, handle);

	for (i = 0; i < thread_handle->owned_mutexes->len; i++) {
		gpointer mutex = g_ptr_array_index (thread_handle->owned_mutexes, i);

		_wapi_mutex_abandon (mutex, pid, tid);
		_wapi_thread_disown_mutex (mutex);
	}
	g_ptr_array_free (thread_handle->owned_mutexes, TRUE);
	
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	_wapi_handle_set_signal_state (handle, TRUE, TRUE);

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	DEBUG("%s: Recording thread handle %p id %ld status as %d",
		  __func__, handle, thread_handle->id, exitstatus);
	
	/* The thread is no longer active, so unref it */
	_wapi_handle_unref (handle);
}

/*
 * wapi_create_thread_handle:
 *
 *   Create a thread handle for the current thread.
 */
gpointer
wapi_create_thread_handle (void)
{
	WapiHandle_thread thread_handle = {0}, *thread;
	gpointer handle;

	mono_once (&thread_ops_once, thread_ops_init);

	thread_handle.owned_mutexes = g_ptr_array_new ();

	handle = _wapi_handle_new (WAPI_HANDLE_THREAD, &thread_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating thread handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		
		return NULL;
	}

	thread = lookup_thread (handle);

	thread->id = pthread_self ();

	/*
	 * Hold a reference while the thread is active, because we use
	 * the handle to store thread exit information
	 */
	_wapi_handle_ref (handle);

	DEBUG ("%s: started thread id %ld", __func__, thread->id);
	
	return handle;
}

void
wapi_ref_thread_handle (gpointer handle)
{
	_wapi_handle_ref (handle);
}

gpointer
wapi_get_current_thread_handle (void)
{
	return get_current_thread_handle ();
}

/**
 * GetCurrentThreadId:
 *
 * Looks up the thread ID of the current thread.  This ID can be
 * passed to OpenThread() to create a new handle on this thread.
 *
 * Return value: the thread ID.  NB this is defined as DWORD (ie 32
 * bit) in the MS API, but we need to cope with 64 bit IDs for s390x
 * and amd64.  This doesn't really break the API, it just embraces and
 * extends it on 64bit platforms :)
 */
gsize
GetCurrentThreadId (void)
{
	MonoNativeThreadId id;

	id = mono_native_thread_id_get ();
	return MONO_NATIVE_THREAD_ID_TO_UINT (id);
}

static mono_lazy_init_t sleepex_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
static mono_mutex_t sleepex_mutex;
static mono_cond_t sleepex_cond;

static void
sleepex_initialize (void)
{
	mono_mutex_init (&sleepex_mutex);
	mono_cond_init (&sleepex_cond, NULL);
}

static void
sleepex_interrupt (gpointer data)
{
	mono_mutex_lock (&sleepex_mutex);
	mono_cond_broadcast (&sleepex_cond);
	mono_mutex_unlock (&sleepex_mutex);
}

static inline guint32
sleepex_interruptable (guint32 ms)
{
	gboolean interrupted;
	guint32 start, now, end;

	g_assert (INFINITE == G_MAXUINT32);

	start = mono_msec_ticks ();

	if (start < G_MAXUINT32 - ms) {
		end = start + ms;
	} else {
		/* start + ms would overflow guint32 */
		end = G_MAXUINT32;
	}

	mono_lazy_initialize (&sleepex_init, sleepex_initialize);

	mono_mutex_lock (&sleepex_mutex);

	for (now = mono_msec_ticks (); ms == INFINITE || now - start < ms; now = mono_msec_ticks ()) {
		mono_thread_info_install_interrupt (sleepex_interrupt, NULL, &interrupted);
		if (interrupted) {
			mono_mutex_unlock (&sleepex_mutex);
			return WAIT_IO_COMPLETION;
		}

		if (ms < INFINITE)
			mono_cond_timedwait_ms (&sleepex_cond, &sleepex_mutex, end - now);
		else
			mono_cond_wait (&sleepex_cond, &sleepex_mutex);

		mono_thread_info_uninstall_interrupt (&interrupted);
		if (interrupted) {
			mono_mutex_unlock (&sleepex_mutex);
			return WAIT_IO_COMPLETION;
		}
	}

	mono_mutex_unlock (&sleepex_mutex);

	return 0;
}

/**
 * SleepEx:
 * @ms: The time in milliseconds to suspend for
 * @alertable: if TRUE, the wait can be interrupted by an APC call
 *
 * Suspends execution of the current thread for @ms milliseconds.  A
 * value of zero causes the thread to relinquish its time slice.  A
 * value of %INFINITE causes an infinite delay.
 */
guint32
SleepEx (guint32 ms, gboolean alertable)
{
	if (ms == 0) {
		MonoThreadInfo *info;

		mono_thread_info_yield ();

		info = mono_thread_info_current ();
		if (info && mono_thread_info_is_interrupt_state (info))
			return WAIT_IO_COMPLETION;

		return 0;
	}

	if (alertable)
		return sleepex_interruptable (ms);

	DEBUG("%s: Sleeping for %d ms", __func__, ms);

	if (ms == INFINITE) {
		do {
			sleep (G_MAXUINT32);
		} while (1);
	} else {
		int ret;
#if defined (__linux__) && !defined(PLATFORM_ANDROID)
		struct timespec start, target;

		/* Use clock_nanosleep () to prevent time drifting problems when nanosleep () is interrupted by signals */
		ret = clock_gettime (CLOCK_MONOTONIC, &start);
		g_assert (ret == 0);

		target = start;
		target.tv_sec += ms / 1000;
		target.tv_nsec += (ms % 1000) * 1000000;
		if (target.tv_nsec > 999999999) {
			target.tv_nsec -= 999999999;
			target.tv_sec ++;
		}

		do {
			ret = clock_nanosleep (CLOCK_MONOTONIC, TIMER_ABSTIME, &target, NULL);
		} while (ret != 0);
#else
		struct timespec req, rem;

		req.tv_sec = ms / 1000;
		req.tv_nsec = (ms % 1000) * 1000000;

		do {
			memset (&rem, 0, sizeof (rem));
			ret = nanosleep (&req, &rem);
		} while (ret != 0);
#endif /* __linux__ */
	}

	return 0;
}

void
Sleep(guint32 ms)
{
	SleepEx(ms, FALSE);
}

gboolean
_wapi_thread_cur_apc_pending (void)
{
	return mono_thread_info_is_interrupt_state (mono_thread_info_current ());
}

void
_wapi_thread_own_mutex (gpointer mutex)
{
	WapiHandle_thread *thread;
	
	thread = get_current_thread ();

	_wapi_handle_ref (mutex);
	
	g_ptr_array_add (thread->owned_mutexes, mutex);
}

void
_wapi_thread_disown_mutex (gpointer mutex)
{
	WapiHandle_thread *thread;

	thread = get_current_thread ();

	_wapi_handle_unref (mutex);
	
	g_ptr_array_remove (thread->owned_mutexes, mutex);
}

char*
wapi_current_thread_desc (void)
{
	WapiHandle_thread *thread;
	gpointer thread_handle;
	int i;
	GString* text;
	char *res;

	thread_handle = get_current_thread_handle ();
	thread = lookup_thread (thread_handle);

	text = g_string_new (0);
	g_string_append_printf (text, "thread handle %p state : ", thread_handle);

	mono_thread_info_describe_interrupt_token (mono_thread_info_current (), text);

	g_string_append_printf (text, " owns (");
	for (i = 0; i < thread->owned_mutexes->len; i++)
		g_string_append_printf (text, i > 0 ? ", %p" : "%p", g_ptr_array_index (thread->owned_mutexes, i));
	g_string_append_printf (text, ")");

	res = text->str;
	g_string_free (text, FALSE);
	return res;
}
