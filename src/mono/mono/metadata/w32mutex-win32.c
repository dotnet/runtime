/**
 * \file
 * Runtime support for managed Mutex on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32mutex.h"

#include <windows.h>
#include <winbase.h>
#include <mono/metadata/handle.h>
#include <mono/utils/mono-error-internals.h>


void
mono_w32mutex_init (void)
{
}

gpointer
ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned, MonoStringHandle name, MonoBoolean *created, MonoError *error)
{
	HANDLE mutex;

	error_init (error);

	*created = TRUE;

	if (MONO_HANDLE_IS_NULL (name)) {
		MONO_ENTER_GC_SAFE;
		mutex = CreateMutex (NULL, owned, NULL);
		MONO_EXIT_GC_SAFE;
	} else {
		uint32_t gchandle;
		gunichar2 *uniname = mono_string_handle_pin_chars (name, &gchandle);
		MONO_ENTER_GC_SAFE;
		mutex = CreateMutex (NULL, owned, uniname);

		if (GetLastError () == ERROR_ALREADY_EXISTS)
			*created = FALSE;
		MONO_EXIT_GC_SAFE;
		mono_gchandle_free (gchandle);
	}

	return mutex;
}

MonoBoolean
ves_icall_System_Threading_Mutex_ReleaseMutex_internal (gpointer handle)
{
	return ReleaseMutex (handle);
}

gpointer
ves_icall_System_Threading_Mutex_OpenMutex_internal (MonoString *name, gint32 rights, gint32 *error)
{
	HANDLE ret;

	*error = ERROR_SUCCESS;

	ret = OpenMutex (rights, FALSE, mono_string_chars (name));
	if (!ret)
		*error = GetLastError ();

	return ret;
}
