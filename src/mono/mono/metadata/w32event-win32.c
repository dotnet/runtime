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
ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, MonoStringHandle name, gint32 *err, MonoError *error)
{
	gpointer event;

	error_init (error);
	
	uint32_t gchandle = 0;
	gunichar2 *uniname = NULL;
	if (!MONO_HANDLE_IS_NULL (name))
		uniname = mono_string_handle_pin_chars (name, &gchandle);
	MONO_ENTER_GC_SAFE;
	event = CreateEvent (NULL, manual, initial, uniname);
	*err = GetLastError ();
	MONO_EXIT_GC_SAFE;
	if (gchandle)
		mono_gchandle_free (gchandle);

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
ves_icall_System_Threading_Events_OpenEvent_internal (MonoStringHandle name, gint32 rights, gint32 *err, MonoError *error)
{
	gpointer handle;

	error_init (error);

	*err = ERROR_SUCCESS;

	uint32_t gchandle = 0;
	gunichar2 *uniname = NULL;

	if (!MONO_HANDLE_IS_NULL (name))
		uniname = mono_string_handle_pin_chars (name, &gchandle);

	MONO_ENTER_GC_SAFE;
	handle = OpenEvent (rights, FALSE, uniname);
	if (!handle)
		*err = GetLastError ();
	MONO_EXIT_GC_SAFE;

	if (gchandle)
		mono_gchandle_free (gchandle);

	return handle;
}
