/*
 * w32mutex-win32.c: Runtime support for managed Mutex on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32mutex.h"

#include <windows.h>
#include <winbase.h>

void
mono_w32mutex_init (void)
{
}

gpointer
ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned, MonoString *name, MonoBoolean *created)
{
	HANDLE mutex;

	*created = TRUE;

	if (!name) {
		mutex = CreateMutex (NULL, owned, NULL);
	} else {
		mutex = CreateMutex (NULL, owned, mono_string_chars (name));

		if (GetLastError () == ERROR_ALREADY_EXISTS)
			*created = FALSE;
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
