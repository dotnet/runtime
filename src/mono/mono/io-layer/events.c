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
#include <mono/io-layer/event-private.h>
#include <mono/io-layer/io-trace.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/w32handle.h>

static void event_signal(gpointer handle);
static gboolean event_own (gpointer handle);
static void event_details (gpointer data);
static const gchar* event_typename (void);
static gsize event_typesize (void);

static void namedevent_signal (gpointer handle);
static gboolean namedevent_own (gpointer handle);
static void namedevent_details (gpointer data);
static const gchar* namedevent_typename (void);
static gsize namedevent_typesize (void);

static MonoW32HandleOps _wapi_event_ops = {
	NULL,			/* close */
	event_signal,		/* signal */
	event_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL,			/* prewait */
	event_details,	/* details */
	event_typename, /* typename */
	event_typesize, /* typesize */
};

static MonoW32HandleOps _wapi_namedevent_ops = {
	NULL,			/* close */
	namedevent_signal,	/* signal */
	namedevent_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL,			/* prewait */
	namedevent_details,	/* details */
	namedevent_typename, /* typename */
	namedevent_typesize, /* typesize */
};

void
_wapi_event_init (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_EVENT,      &_wapi_event_ops);
	mono_w32handle_register_ops (MONO_W32HANDLE_NAMEDEVENT, &_wapi_namedevent_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_EVENT,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
	mono_w32handle_register_capabilities (MONO_W32HANDLE_NAMEDEVENT,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
}

static const char* event_handle_type_to_string (MonoW32HandleType type)
{
	switch (type) {
	case MONO_W32HANDLE_EVENT: return "event";
	case MONO_W32HANDLE_NAMEDEVENT: return "named event";
	default:
		g_assert_not_reached ();
	}
}

static gboolean event_handle_own (gpointer handle, MonoW32HandleType type)
{
	struct _WapiHandle_event *event_handle;
	gboolean ok;

	ok = mono_w32handle_lookup (handle, type, (gpointer *)&event_handle);
	if (!ok) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, event_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: owning %s handle %p",
		__func__, event_handle_type_to_string (type), handle);

	if (!event_handle->manual) {
		g_assert (event_handle->set_count > 0);
		event_handle->set_count --;

		if (event_handle->set_count == 0)
			mono_w32handle_set_signal_state (handle, FALSE, FALSE);
	}

	return TRUE;
}

static void event_signal(gpointer handle)
{
	SetEvent(handle);
}

static gboolean event_own (gpointer handle)
{
	return event_handle_own (handle, MONO_W32HANDLE_EVENT);
}

static void namedevent_signal (gpointer handle)
{
	SetEvent (handle);
}

/* NB, always called with the shared handle lock held */
static gboolean namedevent_own (gpointer handle)
{
	return event_handle_own (handle, MONO_W32HANDLE_NAMEDEVENT);
}

static void event_details (gpointer data)
{
	struct _WapiHandle_event *event = (struct _WapiHandle_event *)data;
	g_print ("manual: %s, set_count: %d",
		event->manual ? "TRUE" : "FALSE", event->set_count);
}

static void namedevent_details (gpointer data)
{
	struct _WapiHandle_namedevent *namedevent = (struct _WapiHandle_namedevent *)data;
	g_print ("manual: %s, set_count: %d, name: \"%s\"",
		namedevent->e.manual ? "TRUE" : "FALSE", namedevent->e.set_count, namedevent->sharedns.name);
}

static const gchar* event_typename (void)
{
	return "Event";
}

static gsize event_typesize (void)
{
	return sizeof (struct _WapiHandle_event);
}

static const gchar* namedevent_typename (void)
{
	return "N.Event";
}

static gsize namedevent_typesize (void)
{
	return sizeof (struct _WapiHandle_namedevent);
}

static gpointer event_handle_create (struct _WapiHandle_event *event_handle, MonoW32HandleType type, gboolean manual, gboolean initial)
{
	gpointer handle;
	int thr_ret;

	event_handle->manual = manual;
	event_handle->set_count = (initial && !manual) ? 1 : 0;

	handle = mono_w32handle_new (type, event_handle);
	if (handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating %s handle",
			__func__, event_handle_type_to_string (type));
		SetLastError (ERROR_GEN_FAILURE);
		return NULL;
	}

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (initial)
		mono_w32handle_set_signal_state (handle, TRUE, FALSE);

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: created %s handle %p",
		__func__, event_handle_type_to_string (type), handle);

	return handle;
}

static gpointer event_create (gboolean manual, gboolean initial)
{
	struct _WapiHandle_event event_handle;
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle",
		__func__, event_handle_type_to_string (MONO_W32HANDLE_EVENT));
	return event_handle_create (&event_handle, MONO_W32HANDLE_EVENT, manual, initial);
}

static gpointer namedevent_create (gboolean manual, gboolean initial, const gunichar2 *name G_GNUC_UNUSED)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle",
		__func__, event_handle_type_to_string (MONO_W32HANDLE_NAMEDEVENT));

	/* w32 seems to guarantee that opening named objects can't race each other */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);

	handle = _wapi_search_handle_namespace (MONO_W32HANDLE_NAMEDEVENT, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		handle = NULL;
		SetLastError (ERROR_INVALID_HANDLE);
	} else if (handle) {
		/* Not an error, but this is how the caller is informed that the event wasn't freshly created */
		SetLastError (ERROR_ALREADY_EXISTS);

		/* this is used as creating a new handle */
		mono_w32handle_ref (handle);
	} else {
		/* A new named event */
		struct _WapiHandle_namedevent namedevent_handle;

		strncpy (&namedevent_handle.sharedns.name [0], utf8_name, MAX_PATH);
		namedevent_handle.sharedns.name [MAX_PATH] = '\0';

		handle = event_handle_create ((struct _WapiHandle_event*) &namedevent_handle, MONO_W32HANDLE_NAMEDEVENT, manual, initial);
	}

	g_free (utf8_name);

	thr_ret = _wapi_namespace_unlock (NULL);
	g_assert (thr_ret == 0);

	return handle;
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
	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if an event
	 * was freshly created
	 */
	SetLastError (ERROR_SUCCESS);

	return name ? namedevent_create (manual, initial, name) : event_create (manual, initial);
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
	MonoW32HandleType type;
	struct _WapiHandle_event *event_handle;
	int thr_ret;

	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_EVENT:
	case MONO_W32HANDLE_NAMEDEVENT:
		break;
	default:
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&event_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, event_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: pulsing %s handle %p",
		__func__, event_handle_type_to_string (type), handle);

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (!event_handle->manual) {
		event_handle->set_count = 1;
		mono_w32handle_set_signal_state (handle, TRUE, FALSE);
	} else {
		mono_w32handle_set_signal_state (handle, TRUE, TRUE);

		thr_ret = mono_w32handle_unlock_handle (handle);
		g_assert (thr_ret == 0);

		/* For a manual-reset event, we're about to try and get the handle
		 * lock again, so give other threads a chance */
		sched_yield ();

		/* Reset the handle signal state */

		/* I'm not sure whether or not we need a barrier here to make sure
		 * that all threads waiting on the event have proceeded. Currently
		 * we rely on broadcasting a condition. */

		thr_ret = mono_w32handle_lock_handle (handle);
		g_assert (thr_ret == 0);

		mono_w32handle_set_signal_state (handle, FALSE, FALSE);
	}

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	return TRUE;
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
	MonoW32HandleType type;
	struct _WapiHandle_event *event_handle;
	int thr_ret;

	SetLastError (ERROR_SUCCESS);
	
	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_EVENT:
	case MONO_W32HANDLE_NAMEDEVENT:
		break;
	default:
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&event_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, event_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: resetting %s handle %p",
		__func__, event_handle_type_to_string (type), handle);

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (!mono_w32handle_issignalled (handle)) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: no need to reset %s handle %p",
			__func__, event_handle_type_to_string (type), handle);
	} else {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: obtained write lock on %s handle %p",
			__func__, event_handle_type_to_string (type), handle);

		mono_w32handle_set_signal_state (handle, FALSE, FALSE);
	}

	event_handle->set_count = 0;

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	return TRUE;
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
	MonoW32HandleType type;
	struct _WapiHandle_event *event_handle;
	int thr_ret;
	
	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_EVENT:
	case MONO_W32HANDLE_NAMEDEVENT:
		break;
	default:
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&event_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, event_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: setting %s handle %p",
		__func__, event_handle_type_to_string (type), handle);

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (!event_handle->manual) {
		event_handle->set_count = 1;
		mono_w32handle_set_signal_state (handle, TRUE, FALSE);
	} else {
		mono_w32handle_set_signal_state (handle, TRUE, TRUE);
	}

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	return TRUE;
}

gpointer OpenEvent (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;

	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Opening named event [%s]", __func__, utf8_name);
	
	handle = _wapi_search_handle_namespace (MONO_W32HANDLE_NAMEDEVENT,
						utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (!handle) {
		/* This name doesn't exist */
		SetLastError (ERROR_FILE_NOT_FOUND);	/* yes, really */
		goto cleanup;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning named event handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);

	_wapi_namespace_unlock (NULL);
	
	return handle;

}
