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

#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/metadata/handle.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/w32handle.h"
#include "icall-decl.h"

#define MAX_PATH 260

typedef struct {
	gboolean manual;
	guint32 set_count;
} MonoW32HandleEvent;

static gpointer event_create (gboolean manual, gboolean initial);

static gint32 event_handle_signal (MonoW32Handle *handle_data)
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
	return MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
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

static const gchar* event_typename (void)
{
	return "Event";
}

static gsize event_typesize (void)
{
	return sizeof (MonoW32HandleEvent);
}

void
mono_w32event_init (void)
{
	static const MonoW32HandleOps event_ops = {
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

	mono_w32handle_register_ops (MONO_W32TYPE_EVENT,      &event_ops);

	mono_w32handle_register_capabilities (MONO_W32TYPE_EVENT,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
}

gpointer
mono_w32event_create (gboolean manual, gboolean initial)
{
	return event_create (manual, initial);

}

gboolean
mono_w32event_close (gpointer handle)
{
	return mono_w32handle_close (handle);
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

void
mono_w32event_set (gpointer handle)
{
	MonoW32Handle *handle_data;
	MonoW32HandleEvent *event_handle;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		g_warning ("%s: unkown handle %p", __func__, handle);
		return;
	}

	if (handle_data->type != MONO_W32TYPE_EVENT) {
		g_warning ("%s: unkown event handle %p", __func__, handle);
		mono_w32handle_unref (handle_data);
		return;
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
}
