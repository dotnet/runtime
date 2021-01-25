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
#include <mono/metadata/object-internals.h>
#include <mono/utils/w32subset.h>
#include "icall-decl.h"

void
mono_w32semaphore_init (void)
{
}

#if HAVE_API_SUPPORT_WIN32_CREATE_SEMAPHORE || HAVE_API_SUPPORT_WIN32_CREATE_SEMAPHORE_EX
gpointer
ves_icall_System_Threading_Semaphore_CreateSemaphore_icall (gint32 initialCount, gint32 maximumCount,
	const gunichar2 *name, gint32 name_length, gint32 *win32error)
{
	HANDLE sem;
	MONO_ENTER_GC_SAFE;
#if HAVE_API_SUPPORT_WIN32_CREATE_SEMAPHORE
	sem = CreateSemaphoreW (NULL, initialCount, maximumCount, name);
#elif HAVE_API_SUPPORT_WIN32_CREATE_SEMAPHORE_EX
	sem = CreateSemaphoreExW (NULL, initialCount, maximumCount, name, 0, SEMAPHORE_ALL_ACCESS);
#endif
	MONO_EXIT_GC_SAFE;
	*win32error = GetLastError ();
	return sem;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_CREATE_SEMAPHORE && !HAVE_EXTERN_DEFINED_WIN32_CREATE_SEMAPHORE_EX
gpointer
ves_icall_System_Threading_Semaphore_CreateSemaphore_icall (gint32 initialCount, gint32 maximumCount,
	const gunichar2 *name, gint32 name_length, gint32 *win32error)
{
	g_unsupported_api ("CreateSemaphore, CreateSemaphoreEx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}
#endif /* HAVE_API_SUPPORT_WIN32_CREATE_SEMAPHORE || HAVE_API_SUPPORT_WIN32_CREATE_SEMAPHORE_EX) */

MonoBoolean
ves_icall_System_Threading_Semaphore_ReleaseSemaphore_internal (gpointer handle, gint32 releaseCount, gint32 *prevcount)
{
	return ReleaseSemaphore (handle, releaseCount, (PLONG)prevcount);
}

gpointer
ves_icall_System_Threading_Semaphore_OpenSemaphore_icall (const gunichar2 *name, gint32 name_length,
	gint32 rights, gint32 *win32error)
{
	HANDLE sem;
	MONO_ENTER_GC_SAFE;
	sem = OpenSemaphoreW (rights, FALSE, name);
	MONO_EXIT_GC_SAFE;
	*win32error = GetLastError ();
	return sem;
}
