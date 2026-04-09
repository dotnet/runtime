/**
 * \file
 * MonoOSSemaphore on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mono-os-semaphore.h"

MonoSemTimedwaitRet
mono_os_sem_timedwait (MonoSemType *sem, guint32 timeout_ms, MonoSemFlags flags)
{
	BOOL res;

retry:
	res = mono_win32_wait_for_single_object_ex (*sem, timeout_ms, flags & MONO_SEM_FLAGS_ALERTABLE);
	if (G_UNLIKELY (res != WAIT_OBJECT_0 && res != WAIT_IO_COMPLETION && res != WAIT_TIMEOUT))
		g_error ("%s: mono_win32_wait_for_single_object_ex failed with error %d", __func__, GetLastError ());

	if (res == WAIT_IO_COMPLETION && !(flags & MONO_SEM_FLAGS_ALERTABLE))
		goto retry;

	switch (res) {
	case WAIT_OBJECT_0:
		return MONO_SEM_TIMEDWAIT_RET_SUCCESS;
	case WAIT_IO_COMPLETION:
		return MONO_SEM_TIMEDWAIT_RET_ALERTED;
	case WAIT_TIMEOUT:
		return MONO_SEM_TIMEDWAIT_RET_TIMEDOUT;
	default:
		g_assert_not_reached ();
	}
}

