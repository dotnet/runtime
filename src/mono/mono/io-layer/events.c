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

#include <mono/io-layer/event-private.h>

#include <mono/utils/mono-mutex.h>
#if 0
#define DEBUG(...) g_message(__VA_ARGS__)
#else
#define DEBUG(...)
#endif

static void event_signal(gpointer handle);
static gboolean event_own (gpointer handle);

static void namedevent_signal (gpointer handle);
static gboolean namedevent_own (gpointer handle);

struct _WapiHandleOps _wapi_event_ops = {
	NULL,			/* close */
	event_signal,		/* signal */
	event_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

struct _WapiHandleOps _wapi_namedevent_ops = {
	NULL,			/* close */
	namedevent_signal,	/* signal */
	namedevent_own,		/* own */
	NULL,			/* is_owned */
};

static gboolean event_pulse (gpointer handle);
static gboolean event_reset (gpointer handle);
static gboolean event_set (gpointer handle);

static gboolean namedevent_pulse (gpointer handle);
static gboolean namedevent_reset (gpointer handle);
static gboolean namedevent_set (gpointer handle);

static struct
{
	gboolean (*pulse)(gpointer handle);
	gboolean (*reset)(gpointer handle);
	gboolean (*set)(gpointer handle);
} event_ops[WAPI_HANDLE_COUNT] = {
		{NULL},
		{NULL},
		{NULL},
		{NULL},
		{NULL},
		{NULL},
		{event_pulse, event_reset, event_set},
		{NULL},
		{NULL},
		{NULL},
		{NULL},
		{NULL},
		{NULL},
		{namedevent_pulse, namedevent_reset, namedevent_set},
};

void _wapi_event_details (gpointer handle_info)
{
	struct _WapiHandle_event *event = (struct _WapiHandle_event *)handle_info;
	
	g_print ("manual: %s", event->manual?"TRUE":"FALSE");
}

static mono_once_t event_ops_once=MONO_ONCE_INIT;

static void event_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_EVENT,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL);
	_wapi_handle_register_capabilities (WAPI_HANDLE_NAMEDEVENT,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL);
}

static void event_signal(gpointer handle)
{
	SetEvent(handle);
}

static gboolean event_own (gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				(gpointer *)&event_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up event handle %p", __func__,
			   handle);
		return (FALSE);
	}
	
	DEBUG("%s: owning event handle %p", __func__, handle);

	if(event_handle->manual==FALSE) {
		g_assert (event_handle->set_count > 0);
		
		if (--event_handle->set_count == 0) {
			_wapi_handle_set_signal_state (handle, FALSE, FALSE);
		}
	}

	return(TRUE);
}

static void namedevent_signal (gpointer handle)
{
	SetEvent (handle);
}

/* NB, always called with the shared handle lock held */
static gboolean namedevent_own (gpointer handle)
{
	struct _WapiHandle_namedevent *namedevent_handle;
	gboolean ok;
	
	DEBUG ("%s: owning named event handle %p", __func__, handle);

	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDEVENT,
				  (gpointer *)&namedevent_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named event handle %p",
			   __func__, handle);
		return(FALSE);
	}
	
	if (namedevent_handle->manual == FALSE) {
		g_assert (namedevent_handle->set_count > 0);
		
		if (--namedevent_handle->set_count == 0) {
			_wapi_shared_handle_set_signal_state (handle, FALSE);
		}
	}
	
	return (TRUE);
}
static gpointer event_create (WapiSecurityAttributes *security G_GNUC_UNUSED,
			      gboolean manual, gboolean initial)
{
	struct _WapiHandle_event event_handle = {0};
	gpointer handle;
	int thr_ret;
	
	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if an event
	 * was freshly created
	 */
	SetLastError (ERROR_SUCCESS);

	DEBUG ("%s: Creating unnamed event", __func__);
	
	event_handle.manual = manual;
	event_handle.set_count = 0;

	if (initial == TRUE) {
		if (manual == FALSE) {
			event_handle.set_count = 1;
		}
	}
	
	handle = _wapi_handle_new (WAPI_HANDLE_EVENT, &event_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating event handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		return(NULL);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if (initial == TRUE) {
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}
	
	DEBUG("%s: created new event handle %p", __func__, handle);

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	return(handle);
}

static gpointer namedevent_create (WapiSecurityAttributes *security G_GNUC_UNUSED,
				   gboolean manual, gboolean initial,
				   const gunichar2 *name G_GNUC_UNUSED)
{
	struct _WapiHandle_namedevent namedevent_handle = {{{0}}, 0};
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	guint32 namelen;
	gint32 offset;
	
	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if an event
	 * was freshly created
	 */
	SetLastError (ERROR_SUCCESS);
	
	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	DEBUG ("%s: Creating named event [%s]", __func__, utf8_name);
	
	offset = _wapi_search_handle_namespace (WAPI_HANDLE_NAMEDEVENT,
						utf8_name);
	if (offset == -1) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (offset != 0) {
		/* Not an error, but this is how the caller is
		 * informed that the event wasn't freshly created
		 */
		SetLastError (ERROR_ALREADY_EXISTS);
	}
	/* Fall through to create the event handle. */

	if (offset == 0) {
		/* A new named event, so create both the private and
		 * shared parts
		 */
	
		if (strlen (utf8_name) < MAX_PATH) {
			namelen = strlen (utf8_name);
		} else {
			namelen = MAX_PATH;
		}
	
		memcpy (&namedevent_handle.sharedns.name, utf8_name, namelen);

		namedevent_handle.manual = manual;
		namedevent_handle.set_count = 0;
		
		if (initial == TRUE) {
			if (manual == FALSE) {
				namedevent_handle.set_count = 1;
			}
		}
		
		handle = _wapi_handle_new (WAPI_HANDLE_NAMEDEVENT,
					   &namedevent_handle);
	} else {
		/* A new reference to an existing named event, so just
		 * create the private part
		 */
		handle = _wapi_handle_new_from_offset (WAPI_HANDLE_NAMEDEVENT,
						       offset, TRUE);
	}
	
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating event handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}
	ret = handle;

	if (offset == 0) {
		/* Set the initial state, as this is a completely new
		 * handle
		 */
		thr_ret = _wapi_handle_lock_shared_handles ();
		g_assert (thr_ret == 0);
	
		if (initial == TRUE) {
			_wapi_shared_handle_set_signal_state (handle, TRUE);
		}

		_wapi_handle_unlock_shared_handles ();
	}
	
	DEBUG ("%s: returning event handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);

	_wapi_namespace_unlock (NULL);
	
	return(ret);

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
gpointer CreateEvent(WapiSecurityAttributes *security G_GNUC_UNUSED,
		     gboolean manual, gboolean initial,
		     const gunichar2 *name G_GNUC_UNUSED)
{
	mono_once (&event_ops_once, event_ops_init);

	if (name == NULL) {
		return(event_create (security, manual, initial));
	} else {
		return(namedevent_create (security, manual, initial, name));
	}
}

static gboolean event_pulse (gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				  (gpointer *)&event_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up event handle %p", __func__,
			   handle);
		return(FALSE);
	}
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	DEBUG ("%s: Pulsing event handle %p", __func__, handle);

	if (event_handle->manual == TRUE) {
		_wapi_handle_set_signal_state (handle, TRUE, TRUE);
	} else {
		event_handle->set_count = 1;
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	if (event_handle->manual == TRUE) {
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
		DEBUG ("%s: Obtained write lock on event handle %p",
			   __func__, handle);

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

static gboolean namedevent_pulse (gpointer handle)
{
	struct _WapiHandle_namedevent *namedevent_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDEVENT,
				  (gpointer *)&namedevent_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named event handle %p",
			   __func__, handle);
		return(FALSE);
	}
	
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);

	DEBUG ("%s: Pulsing named event handle %p", __func__, handle);

	if (namedevent_handle->manual == TRUE) {
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	} else {
		namedevent_handle->set_count = 1;
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	}

	_wapi_handle_unlock_shared_handles ();
	
	if (namedevent_handle->manual == TRUE) {
		/* For a manual-reset event, we're about to try and
		 * get the handle lock again, so give other processes
		 * a chance
		 */
		_wapi_handle_spin (200);

		/* Reset the handle signal state */
		/* I'm not sure whether or not we need a barrier here
		 * to make sure that all threads waiting on the event
		 * have proceeded.  Currently we rely on waiting for
		 * twice the shared handle poll interval.
		 */
		DEBUG ("%s: Obtained write lock on event handle %p",
			   __func__, handle);

		thr_ret = _wapi_handle_lock_shared_handles ();
		g_assert (thr_ret == 0);
		
		_wapi_shared_handle_set_signal_state (handle, FALSE);

		_wapi_handle_unlock_shared_handles ();
	}

	return(TRUE);
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
	WapiHandleType type;
	
	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	type = _wapi_handle_type (handle);
	
	if (event_ops[type].pulse == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(event_ops[type].pulse (handle));
}

static gboolean event_reset (gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				  (gpointer *)&event_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up event handle %p",
			   __func__, handle);
		return(FALSE);
	}

	DEBUG ("%s: Resetting event handle %p", __func__, handle);

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if (_wapi_handle_issignalled (handle) == FALSE) {
		DEBUG ("%s: No need to reset event handle %p", __func__,
			   handle);
	} else {
		DEBUG ("%s: Obtained write lock on event handle %p",
			   __func__, handle);

		_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	}
	
	event_handle->set_count = 0;
	
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	return(TRUE);
}

static gboolean namedevent_reset (gpointer handle)
{
	struct _WapiHandle_namedevent *namedevent_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDEVENT,
				  (gpointer *)&namedevent_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named event handle %p",
			   __func__, handle);
		return(FALSE);
	}

	DEBUG ("%s: Resetting named event handle %p", __func__, handle);

	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	if (_wapi_handle_issignalled (handle) == FALSE) {
		DEBUG ("%s: No need to reset named event handle %p",
			   __func__, handle);
	} else {
		DEBUG ("%s: Obtained write lock on named event handle %p",
			   __func__, handle);

		_wapi_shared_handle_set_signal_state (handle, FALSE);
	}
	
	namedevent_handle->set_count = 0;
	
	_wapi_handle_unlock_shared_handles ();
	
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
	WapiHandleType type;
	
	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	type = _wapi_handle_type (handle);
	
	if (event_ops[type].reset == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(event_ops[type].reset (handle));
}

static gboolean event_set (gpointer handle)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_EVENT,
				  (gpointer *)&event_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up event handle %p", __func__,
			   handle);
		return(FALSE);
	}
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	DEBUG ("%s: Setting event handle %p", __func__, handle);

	if (event_handle->manual == TRUE) {
		_wapi_handle_set_signal_state (handle, TRUE, TRUE);
	} else {
		event_handle->set_count = 1;
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);

	return(TRUE);
}

static gboolean namedevent_set (gpointer handle)
{
	struct _WapiHandle_namedevent *namedevent_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDEVENT,
				  (gpointer *)&namedevent_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named event handle %p",
			   __func__, handle);
		return(FALSE);
	}
	
	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);

	DEBUG ("%s: Setting named event handle %p", __func__, handle);

	if (namedevent_handle->manual == TRUE) {
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	} else {
		namedevent_handle->set_count = 1;
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	}

	_wapi_handle_unlock_shared_handles ();

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
	WapiHandleType type;
	
	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	type = _wapi_handle_type (handle);
	
	if (event_ops[type].set == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(event_ops[type].set (handle));
}

gpointer OpenEvent (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	gint32 offset;
	
	mono_once (&event_ops_once, event_ops_init);

	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	DEBUG ("%s: Opening named event [%s]", __func__, utf8_name);
	
	offset = _wapi_search_handle_namespace (WAPI_HANDLE_NAMEDEVENT,
						utf8_name);
	if (offset == -1) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (offset == 0) {
		/* This name doesn't exist */
		SetLastError (ERROR_FILE_NOT_FOUND);	/* yes, really */
		goto cleanup;
	}

	/* A new reference to an existing named event, so just create
	 * the private part
	 */
	handle = _wapi_handle_new_from_offset (WAPI_HANDLE_NAMEDEVENT, offset,
					       TRUE);
	
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error opening named event handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}
	ret = handle;

	DEBUG ("%s: returning named event handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);

	_wapi_namespace_unlock (NULL);
	
	return(ret);

}
