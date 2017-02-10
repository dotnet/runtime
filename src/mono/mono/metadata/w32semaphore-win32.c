/**
 * \file
 * Runtime support for managed Semaphore on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32semaphore.h"

#include <windows.h>
#include <winbase.h>

void
mono_w32semaphore_init (void)
{
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
gpointer
ves_icall_System_Threading_Semaphore_CreateSemaphore_internal (gint32 initialCount, gint32 maximumCount, MonoString *name, gint32 *error)
{ 
	HANDLE sem;

	sem = CreateSemaphore (NULL, initialCount, maximumCount, name ? mono_string_chars (name) : NULL);

	*error = GetLastError ();

	return sem;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_System_Threading_Semaphore_ReleaseSemaphore_internal (gpointer handle, gint32 releaseCount, gint32 *prevcount)
{ 
	return ReleaseSemaphore (handle, releaseCount, prevcount);
}

gpointer
ves_icall_System_Threading_Semaphore_OpenSemaphore_internal (MonoString *name, gint32 rights, gint32 *error)
{
	HANDLE sem;

	sem = OpenSemaphore (rights, FALSE, mono_string_chars (name));

	*error = GetLastError ();

	return sem;
}
