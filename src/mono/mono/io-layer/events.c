/*
 * events.c:  Event handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <string.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>

#include <mono/io-layer/mono-mutex.h>

#include <mono/io-layer/event-private.h>

#undef DEBUG

static void event_close_shared (gpointer handle);
static void event_signal(gpointer handle);
static void event_own (gpointer handle);

struct _WapiHandleOps _wapi_event_ops = {
	event_close_shared,	/* close_shared */
	NULL,			/* close_private */
	event_signal,		/* signal */
	event_own,		/* own */
	NULL,			/* is_owned */
};

static mono_once_t event_ops_once=MONO_ONCE_INIT;

static void event_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_EVENT,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL);
}

static void event_close_shared(gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up event handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing event handle %p", handle);
#endif
}

static void event_signal(gpointer handle)
{
	ResetEvent(handle);
}

static void event_own (gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up event handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": owning event handle %p", handle);
#endif

	if(event_handle->manual==FALSE) {
		g_assert (event_handle->set_count > 0);
		
		if (--event_handle->set_count == 0) {
			_wapi_handle_set_signal_state (handle, FALSE, FALSE);
		}
	}
}

/**
 * CreateEvent:
 * @security: Ignored for now.
 * @manual: Specifies whether the new event handle has manual or auto
 * reset behaviour.
 * @initial: Specifies whether the new event handle is initially
 * signalled or not.
 * @name:Pointer to a string specifying the name of this name, or
 * %NULL.  Currently ignored.
 *
 * Creates a new event handle.
 *
 * An event handle is signalled with SetEvent().  If the new handle is
 * a manual reset event handle, it remains signalled until it is reset
 * with ResetEvent().  An auto reset event remains signalled until a
 * single thread has waited for it, at which time the event handle is
 * automatically reset to unsignalled.
 *
 * Return value: A new handle, or %NULL on error.
 */
gpointer CreateEvent(WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean manual,
		     gboolean initial, const gunichar2 *name G_GNUC_UNUSED)
{
	struct _WapiHandle_event *event_handle;
	gpointer handle;
	gboolean ok;
	gpointer ret = NULL;
	int thr_ret;
	
	mono_once (&event_ops_once, event_ops_init);

	handle=_wapi_handle_new (WAPI_HANDLE_EVENT);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating event handle");
		return(NULL);
	}
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up event handle %p", handle);
		goto cleanup;
	}
	ret = handle;
	
	event_handle->manual=manual;
	event_handle->set_count = 0;

	if(initial==TRUE) {
		if (manual == FALSE) {
			event_handle->set_count = 1;
		}
		
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": created new event handle %p",
		  handle);
#endif

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);

	return(ret);
}

/**
 * PulseEvent:
 * @handle: The event handle.
 *
 * Sets the event handle @handle to the signalled state, and then
 * resets it to unsignalled after informing any waiting threads.
 *
 * If @handle is a manual reset event, all waiting threads that can be
 * released immediately are released.  @handle is then reset.  If
 * @handle is an auto reset event, one waiting thread is released even
 * if multiple threads are waiting.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (Currently only
 * ever returns %TRUE).
 */
gboolean PulseEvent(gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	int thr_ret;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up event handle %p", handle);
		return(FALSE);
	}
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Pulsing event handle %p", handle);
#endif

	if(event_handle->manual==TRUE) {
		_wapi_handle_set_signal_state (handle, TRUE, TRUE);
	} else {
		event_handle->set_count++;
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	if(event_handle->manual==TRUE) {
		/* For a manual-reset event, we're about to try and
		 * get the handle lock again, so give other threads a
		 * chance
		 */
		sched_yield ();

		/* Reset the handle signal state */
		/* I'm not sure whether or not we need a barrier here
		 * to make sure that all threads waiting on the event
		 * have proceeded.  Currently we rely on broadcasting
		 * a condition.
		 */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Obtained write lock on event handle %p", handle);
#endif

		pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle, handle);
		thr_ret = _wapi_handle_lock_handle (handle);
		g_assert (thr_ret == 0);
		
		_wapi_handle_set_signal_state (handle, FALSE, FALSE);

		thr_ret = _wapi_handle_unlock_handle (handle);
		g_assert (thr_ret == 0);
		pthread_cleanup_pop (0);
	}

	return(TRUE);
}

/**
 * ResetEvent:
 * @handle: The event handle.
 *
 * Resets the event handle @handle to the unsignalled state.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (Currently only
 * ever returns %TRUE).
 */
gboolean ResetEvent(gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	int thr_ret;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up event handle %p", handle);
		return(FALSE);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Resetting event handle %p",
		  handle);
#endif

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if(_wapi_handle_issignalled (handle)==FALSE) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": No need to reset event handle %p", handle);
#endif
	} else {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Obtained write lock on event handle %p", handle);
#endif

		_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	}
	
	event_handle->set_count = 0;
	
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	return(TRUE);
}

/**
 * SetEvent:
 * @handle: The event handle
 *
 * Sets the event handle @handle to the signalled state.
 *
 * If @handle is a manual reset event, it remains signalled until it
 * is reset with ResetEvent().  An auto reset event remains signalled
 * until a single thread has waited for it, at which time @handle is
 * automatically reset to unsignalled.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (Currently only
 * ever returns %TRUE).
 */
gboolean SetEvent(gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	int thr_ret;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up event handle %p", handle);
		return(FALSE);
	}
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Setting event handle %p", handle);
#endif

	if(event_handle->manual==TRUE) {
		_wapi_handle_set_signal_state (handle, TRUE, TRUE);
	} else {
		event_handle->set_count++;
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);

	return(TRUE);
}

