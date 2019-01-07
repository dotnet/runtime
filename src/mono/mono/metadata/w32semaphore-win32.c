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
#include "object-internals.h"
#include "icall-decl.h"

void
mono_w32semaphore_init (void)
{
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)
gpointer
ves_icall_System_Threading_Semaphore_CreateSemaphore_icall (gint32 initialCount, gint32 maximumCount,
	const gunichar2 *name, gint32 name_length, gint32 *win32error, MonoError *error)
{ 
	HANDLE sem = CreateSemaphoreW (NULL, initialCount, maximumCount, name);
	*win32error = GetLastError ();
	return sem;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_System_Threading_Semaphore_ReleaseSemaphore_internal (gpointer handle, gint32 releaseCount, gint32 *prevcount, MonoError *error)
{
	return ReleaseSemaphore (handle, releaseCount, (PLONG)prevcount);
}

gpointer
ves_icall_System_Threading_Semaphore_OpenSemaphore_icall (const gunichar2 *name, gint32 name_length,
	gint32 rights, gint32 *win32error, MonoError *error)
{
	HANDLE sem = OpenSemaphoreW (rights, FALSE, name);
	*win32error = GetLastError ();
	return sem;
}
