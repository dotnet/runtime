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
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/mutex-private.h>

#include <mono/utils/mono-threads.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-once.h>

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
