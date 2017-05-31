/**
 * \file
 * namespace for w32handles
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#ifndef HOST_WIN32

#include "w32handle-namespace.h"

#include "w32mutex.h"
#include "w32semaphore.h"
#include "w32event.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-coop-mutex.h"

static MonoCoopMutex lock;

void
mono_w32handle_namespace_init (void)
{
	mono_coop_mutex_init (&lock);
}

void
mono_w32handle_namespace_lock (void)
{
	mono_coop_mutex_lock (&lock);
}

void
mono_w32handle_namespace_unlock (void)
{
	mono_coop_mutex_unlock (&lock);
}

static gboolean
has_namespace (MonoW32HandleType type)
{
	switch (type) {
	case MONO_W32HANDLE_NAMEDMUTEX:
	case MONO_W32HANDLE_NAMEDSEM:
	case MONO_W32HANDLE_NAMEDEVENT:
		return TRUE;
	default:
		return FALSE;
	}
}

typedef struct {
	gpointer ret;
	MonoW32HandleType type;
	const gchar *name;
} NamespaceSearchHandleData;

static gboolean
mono_w32handle_namespace_search_handle_callback (gpointer handle, gpointer data, gpointer user_data)
{
	NamespaceSearchHandleData *search_data;
	MonoW32HandleType type;
	MonoW32HandleNamespace *sharedns;

	type = mono_w32handle_get_type (handle);
	if (!has_namespace (type))
		return FALSE;

	search_data = (NamespaceSearchHandleData*) user_data;

	switch (type) {
	case MONO_W32HANDLE_NAMEDMUTEX: sharedns = mono_w32mutex_get_namespace ((MonoW32HandleNamedMutex*) data); break;
	case MONO_W32HANDLE_NAMEDSEM:   sharedns = mono_w32semaphore_get_namespace ((MonoW32HandleNamedSemaphore*) data); break;
	case MONO_W32HANDLE_NAMEDEVENT: sharedns = mono_w32event_get_namespace ((MonoW32HandleNamedEvent*) data); break;
	default:
		g_assert_not_reached ();
	}

	if (strcmp (sharedns->name, search_data->name) == 0) {
		if (type != search_data->type) {
			/* Its the wrong type, so fail now */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: handle %p matches name but is wrong type: %s",
				__func__, handle, mono_w32handle_get_typename (type));
			search_data->ret = INVALID_HANDLE_VALUE;
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: handle %p matches name and type",
				__func__, handle);

			/* we do not want the handle to be destroyed before we return it  */
			mono_w32handle_ref (handle);

			search_data->ret = handle;
		}

		return TRUE;
	}

	return FALSE;
}

gpointer
mono_w32handle_namespace_search_handle (MonoW32HandleType type, const gchar *name)
{
	NamespaceSearchHandleData search_data;

	if (!has_namespace (type))
		g_error ("%s: type %s does not have a namespace", __func__, type);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Lookup for handle named [%s] type %s",
		__func__, name, mono_w32handle_get_typename (type));

	search_data.ret = NULL;
	search_data.type = type;
	search_data.name = name;
	mono_w32handle_foreach (mono_w32handle_namespace_search_handle_callback, &search_data);
	return search_data.ret;
}

#endif
