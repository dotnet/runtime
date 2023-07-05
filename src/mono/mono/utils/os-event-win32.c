/**
 * \file
 * MonoOSEvent on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "os-event.h"

#include <windows.h>
#include <winbase.h>

#include "atomic.h"
#include "mono-os-wait.h"

void
mono_os_event_init (MonoOSEvent *event, gboolean initial)
{
	g_assert (event);

	event->handle = CreateEvent (NULL, TRUE, initial, NULL);
	if (G_UNLIKELY (!event->handle))
		g_error ("%s: CreateEvent failed with error %d", __func__, GetLastError ());
}

void
mono_os_event_destroy (MonoOSEvent *event)
{
	BOOL res;

	g_assert (event);
	g_assert (event->handle);

	res = CloseHandle (event->handle);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: CloseHandle failed with error %d", __func__, GetLastError ());
}

void
mono_os_event_set (MonoOSEvent *event)
{
	BOOL res;

	g_assert (event);
	g_assert (event->handle);

	res = SetEvent (event->handle);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: SetEvent failed with error %d", __func__, GetLastError ());
}

void
mono_os_event_reset (MonoOSEvent *event)
{
	BOOL res;

	g_assert (event);
	g_assert (event->handle);

	res = ResetEvent (event->handle);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: ResetEvent failed with error %d", __func__, GetLastError ());
}

MonoOSEventWaitRet
mono_os_event_wait_one (MonoOSEvent *event, guint32 timeout, gboolean alertable)
{
	DWORD res;

	g_assert (event);
	g_assert (event->handle);

	res = mono_win32_wait_for_single_object_ex (event->handle, timeout, alertable);
	if (res == WAIT_OBJECT_0)
		return MONO_OS_EVENT_WAIT_RET_SUCCESS_0;
	else if (res == WAIT_IO_COMPLETION)
		return MONO_OS_EVENT_WAIT_RET_ALERTED;
	else if (res == WAIT_TIMEOUT)
		return MONO_OS_EVENT_WAIT_RET_TIMEOUT;
	else if (res == WAIT_FAILED)
		g_error ("%s: mono_thread_win32_wait_one_handle failed with error %d", __func__, GetLastError ());
	else
		g_error ("%s: unknown res value %d", __func__, res);
}

MonoOSEventWaitRet
mono_os_event_wait_multiple (MonoOSEvent **events, gsize nevents, gboolean waitall, guint32 timeout, gboolean alertable)
{
	DWORD res;
	gpointer handles [MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS];

	g_assert (events);
	g_assert (nevents > 0);
	g_assert (nevents <= MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS);

	if (nevents == 1)
		return mono_os_event_wait_one (events [0], timeout, alertable);

	for (gsize i = 0; i < nevents; ++i) {
		g_assert (events [i]);
		g_assert (events [i]->handle);
		handles [i] = events [i]->handle;
	}

	res = mono_win32_wait_for_multiple_objects_ex ((DWORD)nevents, handles, waitall, timeout, alertable, NULL);
	if (res >= WAIT_OBJECT_0 && res < WAIT_OBJECT_0 + MONO_OS_EVENT_WAIT_MAXIMUM_OBJECTS)
		return (MonoOSEventWaitRet)(MONO_OS_EVENT_WAIT_RET_SUCCESS_0 + (res - WAIT_OBJECT_0));
	else if (res == WAIT_IO_COMPLETION)
		return MONO_OS_EVENT_WAIT_RET_ALERTED;
	else if (res == WAIT_TIMEOUT)
		return MONO_OS_EVENT_WAIT_RET_TIMEOUT;
	else if (res == WAIT_FAILED)
		g_error ("%s: mono_thread_win32_wait_multiple_handle failed with error %d", __func__, GetLastError ());
	else
		g_error ("%s: unknown res value %d", __func__, res);
}
