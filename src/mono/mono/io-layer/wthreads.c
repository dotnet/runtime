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
	struct timespec req;
	int ms_quot, ms_rem;
	int ret;
	gpointer current_thread = NULL;
#if defined (__linux__) && !defined(PLATFORM_ANDROID)
	struct timespec start, target;
#else
	struct timespec rem;
#endif
	
	DEBUG("%s: Sleeping for %d ms", __func__, ms);

	if (alertable) {
		current_thread = get_current_thread_handle ();
		
		if (_wapi_thread_apc_pending (current_thread))
			return WAIT_IO_COMPLETION;
	}
	
	if(ms==0) {
		sched_yield();
		return 0;
	}
	
	/* FIXME: check for INFINITE and sleep forever */
	ms_quot = ms / 1000;
	ms_rem = ms % 1000;
	
	req.tv_sec=ms_quot;
	req.tv_nsec=ms_rem*1000000;

#if defined (__linux__) && !defined(PLATFORM_ANDROID)
	/* Use clock_nanosleep () to prevent time drifting problems when nanosleep () is interrupted by signals */
	ret = clock_gettime (CLOCK_MONOTONIC, &start);
	g_assert (ret == 0);
	target = start;
	target.tv_sec += ms_quot;
	target.tv_nsec += ms_rem * 1000000;
	if (target.tv_nsec > 999999999) {
		target.tv_nsec -= 999999999;
		target.tv_sec ++;
	}

	while (TRUE) {
		ret = clock_nanosleep (CLOCK_MONOTONIC, TIMER_ABSTIME, &target, NULL);

		if (alertable && _wapi_thread_apc_pending (current_thread))
			return WAIT_IO_COMPLETION;

		if (ret == 0)
			break;
	}

#else

again:
	memset (&rem, 0, sizeof (rem));
	ret=nanosleep(&req, &rem);

	if (alertable && _wapi_thread_apc_pending (current_thread))
		return WAIT_IO_COMPLETION;
	
	if(ret==-1) {
		/* Sleep interrupted with rem time remaining */
#ifdef DEBUG_ENABLED
		guint32 rems=rem.tv_sec*1000 + rem.tv_nsec/1000000;
		
		g_message("%s: Still got %d ms to go", __func__, rems);
#endif
		req=rem;
		goto again;
	}

#endif /* __linux__ */

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
	return _wapi_thread_apc_pending (get_current_thread_handle ());
}

gboolean
_wapi_thread_apc_pending (gpointer handle)
{
	WapiHandle_thread *thread;

	thread = lookup_thread (handle);
	
	return thread->wait_handle == INTERRUPTION_REQUESTED_HANDLE;
}

/*
 * wapi_interrupt_thread:
 *
 * The state of the thread handle HANDLE is set to 'interrupted' which means that
 * if the thread calls one of the WaitFor functions, the function will return with 
 * WAIT_IO_COMPLETION instead of waiting. Also, if the thread was waiting when
 * this function was called, the wait will be broken.
 * It is possible that the wait functions return WAIT_IO_COMPLETION, but the
 * target thread didn't receive the interrupt signal yet, in this case it should
 * call the wait function again. This essentially means that the target thread will
 * busy wait until it is ready to process the interruption.
 */
void
wapi_interrupt_thread (gpointer thread_handle)
{
	gpointer wait_handle;

	wait_handle = wapi_prepare_interrupt_thread (thread_handle);
	wapi_finish_interrupt_thread (wait_handle);
}

gpointer
wapi_prepare_interrupt_thread (gpointer thread_handle)
{
	WapiHandle_thread *thread;
	gpointer prev_handle, wait_handle;

	thread = lookup_thread (thread_handle);

	while (TRUE) {
		wait_handle = thread->wait_handle;

		/* 
		 * Atomically obtain the handle the thread is waiting on, and
		 * change it to a flag value.
		 */
		prev_handle = InterlockedCompareExchangePointer (&thread->wait_handle,
														 INTERRUPTION_REQUESTED_HANDLE, wait_handle);
		if (prev_handle == INTERRUPTION_REQUESTED_HANDLE)
			/* Already interrupted */
			return 0;
		if (prev_handle == wait_handle)
			break;

		/* Try again */
	}

	WAIT_DEBUG (printf ("%p: state -> INTERRUPTED.\n", thread->id););

	return wait_handle;
}

void
wapi_finish_interrupt_thread (gpointer wait_handle)
{
	pthread_cond_t *cond;
	mono_mutex_t *mutex;
	guint32 idx;

	if (!wait_handle)
		/* Not waiting */
		return;

	/* If we reach here, then wait_handle is set to the flag value, 
	 * which means that the target thread is either
	 * - before the first CAS in timedwait, which means it won't enter the
	 * wait.
	 * - it is after the first CAS, so it is already waiting, or it will 
	 * enter the wait, and it will be interrupted by the broadcast.
	 */
	idx = GPOINTER_TO_UINT(wait_handle);
	cond = &_WAPI_PRIVATE_HANDLES(idx).signal_cond;
	mutex = &_WAPI_PRIVATE_HANDLES(idx).signal_mutex;

	mono_mutex_lock (mutex);
	mono_cond_broadcast (cond);
	mono_mutex_unlock (mutex);

	/* ref added by set_wait_handle */
	_wapi_handle_unref (wait_handle);
}

/*
 * wapi_self_interrupt:
 *
 *   This is not part of the WIN32 API.
 * Set the 'interrupted' state of the calling thread if it's NULL.
 */
void
wapi_self_interrupt (void)
{
	gpointer wait_handle;

	wait_handle = wapi_prepare_interrupt_thread (get_current_thread_handle ());
	if (wait_handle)
		/* ref added by set_wait_handle */
		_wapi_handle_unref (wait_handle);
}

/*
 * wapi_clear_interruption:
 *
 *   This is not part of the WIN32 API. 
 * Clear the 'interrupted' state of the calling thread.
 * This function is signal safe
 */
void
wapi_clear_interruption (void)
{
	WapiHandle_thread *thread;
	gpointer prev_handle;

	thread = get_current_thread ();

	prev_handle = InterlockedCompareExchangePointer (&thread->wait_handle,
													 NULL, INTERRUPTION_REQUESTED_HANDLE);
	if (prev_handle == INTERRUPTION_REQUESTED_HANDLE)
		WAIT_DEBUG (printf ("%p: state -> NORMAL.\n", GetCurrentThreadId ()););
}

/**
 * wapi_thread_set_wait_handle:
 *
 *   Set the wait handle for the current thread to HANDLE. Return TRUE on success, FALSE
 * if the thread is in interrupted state, and cannot start waiting.
 */
gboolean
wapi_thread_set_wait_handle (gpointer handle)
{
	WapiHandle_thread *thread;
	gpointer prev_handle;

	thread = get_current_thread ();

	prev_handle = InterlockedCompareExchangePointer (&thread->wait_handle,
													 handle, NULL);
	if (prev_handle == NULL) {
		/* thread->wait_handle acts as an additional reference to the handle */
		_wapi_handle_ref (handle);

		WAIT_DEBUG (printf ("%p: state -> WAITING.\n", GetCurrentThreadId ()););
	} else {
		g_assert (prev_handle == INTERRUPTION_REQUESTED_HANDLE);
		WAIT_DEBUG (printf ("%p: unable to set state to WAITING.\n", GetCurrentThreadId ()););
	}

	return prev_handle == NULL;
}

/**
 * wapi_thread_clear_wait_handle:
 *
 *   Clear the wait handle of the current thread.
 */
void
wapi_thread_clear_wait_handle (gpointer handle)
{
	WapiHandle_thread *thread;
	gpointer prev_handle;

	thread = get_current_thread ();

	prev_handle = InterlockedCompareExchangePointer (&thread->wait_handle,
													 NULL, handle);
	if (prev_handle == handle) {
		_wapi_handle_unref (handle);
		WAIT_DEBUG (printf ("%p: state -> NORMAL.\n", GetCurrentThreadId ()););
	} else {
		/*It can be NULL if it was asynchronously cleared*/
		g_assert (prev_handle == INTERRUPTION_REQUESTED_HANDLE || prev_handle == NULL);
		WAIT_DEBUG (printf ("%p: finished waiting.\n", GetCurrentThreadId ()););
	}
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
	gpointer handle;
	GString* text;
	char *res;

	thread_handle = get_current_thread_handle ();
	thread = lookup_thread (thread_handle);

	handle = thread->wait_handle;
	text = g_string_new (0);
	g_string_append_printf (text, "thread handle %p state : ", thread_handle);

	if (!handle)
		g_string_append_printf (text, "not waiting");
	else if (handle == INTERRUPTION_REQUESTED_HANDLE)
		g_string_append_printf (text, "interrupted state");
	else
		g_string_append_printf (text, "waiting on %p : %s ", handle, _wapi_handle_typename[_wapi_handle_type (handle)]);
	g_string_append_printf (text, " owns (");
	for (i = 0; i < thread->owned_mutexes->len; i++) {
		gpointer mutex = g_ptr_array_index (thread->owned_mutexes, i);
		if (i > 0)
			g_string_append_printf (text, ", %p", mutex);
		else
			g_string_append_printf (text, "%p", mutex);
	}
	g_string_append_printf (text, ")");

	res = text->str;
	g_string_free (text, FALSE);
	return res;
}
