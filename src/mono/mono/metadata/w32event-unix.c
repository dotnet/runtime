/**
 * \file
 * Runtime support for managed Event on Unix
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32event.h"

#include "w32error.h"
#include "w32handle-namespace.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/metadata/handle.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/w32handle.h"

#define MAX_PATH 260

static gpointer
mono_w32event_create_full (MonoBoolean manual, MonoBoolean initial, const gchar *name, gint32 *err);

static gpointer
mono_w32event_open (const gchar *utf8_name, gint32 rights G_GNUC_UNUSED, gint32 *error);

typedef struct {
	gboolean manual;
	guint32 set_count;
} MonoW32HandleEvent;

struct MonoW32HandleNamedEvent {
	MonoW32HandleEvent e;
	MonoW32HandleNamespace sharedns;
};

static void event_handle_signal (MonoW32Handle *handle_data)
{
	MonoW32HandleEvent *event_handle;

	event_handle = (MonoW32HandleEvent*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: signalling %s handle %p",
		__func__, mono_w32handle_get_typename (handle_data->type), handle_data);

	if (!event_handle->manual) {
		event_handle->set_count = 1;
		mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);
	} else {
		mono_w32handle_set_signal_state (handle_data, TRUE, TRUE);
	}
}

static gboolean event_handle_own (MonoW32Handle *handle_data, gboolean *abandoned)
{
	MonoW32HandleEvent *event_handle;

	*abandoned = FALSE;

	event_handle = (MonoW32HandleEvent*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: owning %s handle %p",
		__func__, mono_w32handle_get_typename (handle_data->type), handle_data);

	if (!event_handle->manual) {
		g_assert (event_handle->set_count > 0);
		event_handle->set_count --;

		if (event_handle->set_count == 0)
			mono_w32handle_set_signal_state (handle_data, FALSE, FALSE);
	}

	return TRUE;
}

static void event_details (MonoW32Handle *handle_data)
{
	MonoW32HandleEvent *event = (MonoW32HandleEvent *)handle_data->specific;
	g_print ("manual: %s, set_count: %d",
		event->manual ? "TRUE" : "FALSE", event->set_count);
}

static void namedevent_details (MonoW32Handle *handle_data)
{
	MonoW32HandleNamedEvent *namedevent = (MonoW32HandleNamedEvent *)handle_data->specific;
	g_print ("manual: %s, set_count: %d, name: \"%s\"",
		namedevent->e.manual ? "TRUE" : "FALSE", namedevent->e.set_count, namedevent->sharedns.name);
}

static const gchar* event_typename (void)
{
	return "Event";
}

static gsize event_typesize (void)
{
	return sizeof (MonoW32HandleEvent);
}

static const gchar* namedevent_typename (void)
{
	return "N.Event";
}

static gsize namedevent_typesize (void)
{
	return sizeof (MonoW32HandleNamedEvent);
}

void
mono_w32event_init (void)
{
	static MonoW32HandleOps event_ops = {
		NULL,			/* close */
		event_handle_signal,		/* signal */
		event_handle_own,		/* own */
		NULL,			/* is_owned */
		NULL,			/* special_wait */
		NULL,			/* prewait */
		event_details,	/* details */
		event_typename, /* typename */
		event_typesize, /* typesize */
	};

	static MonoW32HandleOps namedevent_ops = {
		NULL,			/* close */
		event_handle_signal,	/* signal */
		event_handle_own,		/* own */
		NULL,			/* is_owned */
		NULL,			/* special_wait */
		NULL,			/* prewait */
		namedevent_details,	/* details */
		namedevent_typename, /* typename */
		namedevent_typesize, /* typesize */
	};

	mono_w32handle_register_ops (MONO_W32TYPE_EVENT,      &event_ops);
	mono_w32handle_register_ops (MONO_W32TYPE_NAMEDEVENT, &namedevent_ops);

	mono_w32handle_register_capabilities (MONO_W32TYPE_EVENT,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
	mono_w32handle_register_capabilities (MONO_W32TYPE_NAMEDEVENT,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
}

gpointer
mono_w32event_create (gboolean manual, gboolean initial)
{
	gpointer handle;
	gint32 error;

	handle = mono_w32event_create_full (manual, initial, NULL, &error);
	if (error != ERROR_SUCCESS)
		g_assert (!handle);

	return handle;
}

gboolean
mono_w32event_close (gpointer handle)
{
	return mono_w32handle_close (handle);
}

void
mono_w32event_set (gpointer handle)
{
	ves_icall_System_Threading_Events_SetEvent_internal (handle);
}

void
mono_w32event_reset (gpointer handle)
{
	ves_icall_System_Threading_Events_ResetEvent_internal (handle);
}

static gpointer event_handle_create (MonoW32HandleEvent *event_handle, MonoW32Type type, gboolean manual, gboolean initial)
{
	MonoW32Handle *handle_data;
	gpointer handle;

	event_handle->manual = manual;
	event_handle->set_count = (initial && !manual) ? 1 : 0;

	handle = mono_w32handle_new (type, event_handle);
	if (handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating %s handle",
			__func__, mono_w32handle_get_typename (type));
		mono_w32error_set_last (ERROR_GEN_FAILURE);
		return NULL;
	}

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data))
		g_error ("%s: unkown handle %p", __func__, handle);

	if (handle_data->type != type)
		g_error ("%s: unknown event handle %p", __func__, handle);

	mono_w32handle_lock (handle_data);

	if (initial)
		mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);

	mono_w32handle_unlock (handle_data);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: created %s handle %p",
		__func__, mono_w32handle_get_typename (type), handle);

	mono_w32handle_unref (handle_data);

	return handle;
}

static gpointer event_create (gboolean manual, gboolean initial)
{
	MonoW32HandleEvent event_handle;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: creating %s handle",
		__func__, mono_w32handle_get_typename (MONO_W32TYPE_EVENT));
	return event_handle_create (&event_handle, MONO_W32TYPE_EVENT, manual, initial);
}

static gpointer namedevent_create (gboolean manual, gboolean initial, const gchar *utf8_name G_GNUC_UNUSED)
{
	gpointer handle;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: creating %s handle",
		__func__, mono_w32handle_get_typename (MONO_W32TYPE_NAMEDEVENT));

	/* w32 seems to guarantee that opening named objects can't race each other */
	mono_w32handle_namespace_lock ();

	glong utf8_len = strlen (utf8_name);

	handle = mono_w32handle_namespace_search_handle (MONO_W32TYPE_NAMEDEVENT, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		handle = NULL;
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
	} else if (handle) {
		/* Not an error, but this is how the caller is informed that the event wasn't freshly created */
		mono_w32error_set_last (ERROR_ALREADY_EXISTS);

		/* mono_w32handle_namespace_search_handle already adds a ref to the handle */
	} else {
		/* A new named event */
		MonoW32HandleNamedEvent namedevent_handle;

		size_t len = utf8_len < MAX_PATH ? utf8_len : MAX_PATH;
		memcpy (&namedevent_handle.sharedns.name [0], utf8_name, len);
		namedevent_handle.sharedns.name [len] = '\0';

		handle = event_handle_create ((MonoW32HandleEvent*) &namedevent_handle, MONO_W32TYPE_NAMEDEVENT, manual, initial);
	}

	mono_w32handle_namespace_unlock ();

	return handle;
}

gpointer
mono_w32event_create_full (MonoBoolean manual, MonoBoolean initial, const gchar *name, gint32 *error)
{
	gpointer event;

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if an event
	 * was freshly created */
	mono_w32error_set_last (ERROR_SUCCESS);

	event = name ? namedevent_create (manual, initial, name) : event_create (manual, initial);

	*error = mono_w32error_get_last ();

	return event;
}

gpointer
ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, MonoStringHandle name, gint32 *err, MonoError *error)
{
	error_init (error);
	gchar *utf8_name = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, NULL);
	gpointer result = mono_w32event_create_full (manual, initial, utf8_name, err);
	g_free (utf8_name);
	return result;
}

gboolean
ves_icall_System_Threading_Events_SetEvent_internal (gpointer handle)
{
	MonoW32Handle *handle_data;
	MonoW32HandleEvent *event_handle;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		g_warning ("%s: unkown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_EVENT && handle_data->type != MONO_W32TYPE_NAMEDEVENT) {
		g_warning ("%s: unkown event handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	event_handle = (MonoW32HandleEvent*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: setting %s handle %p",
		__func__, mono_w32handle_get_typename (handle_data->type), handle);

	mono_w32handle_lock (handle_data);

	if (!event_handle->manual) {
		event_handle->set_count = 1;
		mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);
	} else {
		mono_w32handle_set_signal_state (handle_data, TRUE, TRUE);
	}

	mono_w32handle_unlock (handle_data);

	mono_w32handle_unref (handle_data);
	return TRUE;
}

gboolean
ves_icall_System_Threading_Events_ResetEvent_internal (gpointer handle)
{
	MonoW32Handle *handle_data;
	MonoW32HandleEvent *event_handle;

	mono_w32error_set_last (ERROR_SUCCESS);

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		g_warning ("%s: unkown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_EVENT && handle_data->type != MONO_W32TYPE_NAMEDEVENT) {
		g_warning ("%s: unkown event handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	event_handle = (MonoW32HandleEvent*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: resetting %s handle %p",
		__func__, mono_w32handle_get_typename (handle_data->type), handle);

	mono_w32handle_lock (handle_data);

	if (!mono_w32handle_issignalled (handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: no need to reset %s handle %p",
			__func__, mono_w32handle_get_typename (handle_data->type), handle);
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: obtained write lock on %s handle %p",
			__func__, mono_w32handle_get_typename (handle_data->type), handle);

		mono_w32handle_set_signal_state (handle_data, FALSE, FALSE);
	}

	event_handle->set_count = 0;

	mono_w32handle_unlock (handle_data);

	mono_w32handle_unref (handle_data);
	return TRUE;
}

void
ves_icall_System_Threading_Events_CloseEvent_internal (gpointer handle)
{
	mono_w32handle_close (handle);
}

gpointer
ves_icall_System_Threading_Events_OpenEvent_internal (MonoStringHandle name, gint32 rights G_GNUC_UNUSED, gint32 *err, MonoError *error)
{
	error_init (error);
	gchar *utf8_name = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, NULL);
	gpointer handle = mono_w32event_open (utf8_name, rights, err);
	g_free (utf8_name);
	return handle;
}

gpointer
mono_w32event_open (const gchar *utf8_name, gint32 rights G_GNUC_UNUSED, gint32 *error)
{
	gpointer handle;
	*error = ERROR_SUCCESS;

	/* w32 seems to guarantee that opening named objects can't race each other */
	mono_w32handle_namespace_lock ();

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: Opening named event [%s]", __func__, utf8_name);

	handle = mono_w32handle_namespace_search_handle (MONO_W32TYPE_NAMEDEVENT, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		*error = ERROR_INVALID_HANDLE;
		goto cleanup;
	} else if (!handle) {
		/* This name doesn't exist */
		*error = ERROR_FILE_NOT_FOUND;
		goto cleanup;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_EVENT, "%s: returning named event handle %p", __func__, handle);

cleanup:
	mono_w32handle_namespace_unlock ();

	return handle;
}

MonoW32HandleNamespace*
mono_w32event_get_namespace (MonoW32HandleNamedEvent *event)
{
	return &event->sharedns;
}
