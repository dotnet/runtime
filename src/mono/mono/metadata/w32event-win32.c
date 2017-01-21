/*
 * w32event-win32.c: Runtime support for managed Event on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32event.h"

#include <windows.h>
#include <winbase.h>

void
mono_w32event_init (void)
{
}

gpointer
mono_w32event_create (gboolean manual, gboolean initial)
{
	return CreateEvent (NULL, manual, initial, NULL);
}

gboolean
mono_w32event_close (gpointer handle)
{
	return CloseHandle (handle);
}

void
mono_w32event_set (gpointer handle)
{
	SetEvent (handle);
}

void
mono_w32event_reset (gpointer handle)
{
	ResetEvent (handle);
}

gpointer
ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, MonoString *name, gint32 *error)
{
	gpointer event;

	event = CreateEvent (NULL, manual, initial, name ? mono_string_chars (name) : NULL);

	*error = GetLastError ();

	return event;
}

gboolean
ves_icall_System_Threading_Events_SetEvent_internal (gpointer handle)
{
	return SetEvent (handle);
}

gboolean
ves_icall_System_Threading_Events_ResetEvent_internal (gpointer handle)
{
	return ResetEvent (handle);
}

void
ves_icall_System_Threading_Events_CloseEvent_internal (gpointer handle)
{
	CloseHandle (handle);
}

gpointer
ves_icall_System_Threading_Events_OpenEvent_internal (MonoString *name, gint32 rights, gint32 *error)
{
	gpointer handle;

	*error = ERROR_SUCCESS;

	handle = OpenEvent (rights, FALSE, mono_string_chars (name));
	if (!handle)
		*error = GetLastError ();

	return handle;
}
