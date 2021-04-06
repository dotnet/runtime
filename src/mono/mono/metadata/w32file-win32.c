/**
 * \file
 * Windows File IO internal calls.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <winsock2.h>
#include <windows.h>
#include <mono/utils/w32subset.h>
#include "icall-decl.h"

void
mono_w32file_init (void)
{
}

gpointer
mono_w32file_create(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs)
{
	gpointer res;
	MONO_ENTER_GC_SAFE;
	res = CreateFileW (name, fileaccess, sharemode, NULL, createmode, attrs, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_close (gpointer handle)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = CloseHandle (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

static void
win32_io_interrupt_handler (gpointer ignored)
{
}

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error)
{
	gboolean res;
	MonoThreadInfo *info = mono_thread_info_current ();
	gboolean alerted = FALSE;

	if (info) {
		mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted);
		if (alerted) {
			SetLastError (ERROR_OPERATION_ABORTED);
			*win32error = ERROR_OPERATION_ABORTED;
			return FALSE;
		}
		mono_win32_enter_blocking_io_call (info, handle);
	}

	MONO_ENTER_GC_SAFE;
	if (info && mono_thread_info_is_interrupt_state (info)) {
		res = FALSE;
		SetLastError (ERROR_OPERATION_ABORTED);
	} else {
		res = WriteFile (handle, buffer, numbytes, (PDWORD)byteswritten, NULL);
	}
	if (!res)
		*win32error = GetLastError ();
	MONO_EXIT_GC_SAFE;

	if (info) {
		mono_win32_leave_blocking_io_call (info, handle);
		mono_thread_info_uninstall_interrupt (&alerted);
	}

	return res;
}
