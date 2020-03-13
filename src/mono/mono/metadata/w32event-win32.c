/**
 * \file
 * Runtime support for managed Event on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32event.h"

#include <windows.h>
#include <winbase.h>
#include <mono/metadata/handle.h>
#include <mono/utils/mono-error-internals.h>
#include "icall-decl.h"

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
ves_icall_System_Threading_Events_CreateEvent_icall (MonoBoolean manual, MonoBoolean initial,
	const gunichar2 *name, gint32 name_length, gint32 *win32error, MonoError *error)
{
	gpointer event;
	
	MONO_ENTER_GC_SAFE;
	event = CreateEventW (NULL, manual, initial, name);
	*win32error = GetLastError ();
	MONO_EXIT_GC_SAFE;

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
ves_icall_System_Threading_Events_OpenEvent_icall (const gunichar2 *name, gint32 name_length,
	gint32 rights, gint32 *win32error, MonoError *error)
{
	gpointer handle;

	*win32error = ERROR_SUCCESS;

	MONO_ENTER_GC_SAFE;
	handle = OpenEventW (rights, FALSE, name);
	if (!handle)
		*win32error = GetLastError ();
	MONO_EXIT_GC_SAFE;

	return handle;
}
