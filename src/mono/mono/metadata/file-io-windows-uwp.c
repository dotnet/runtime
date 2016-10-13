/*
 * file-io-windows-uwp.c: UWP file-io support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <Windows.h>
#include "mono/metadata/file-io-windows-internals.h"

gboolean
mono_file_io_move_file (gunichar2 *path, gunichar2 *dest, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = MoveFileEx (path, dest, MOVEFILE_COPY_ALLOWED);
	if (result == FALSE) {
		*error=GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}

gboolean
mono_file_io_replace_file (gunichar2 *destinationFileName, gunichar2 *sourceFileName,
			   gunichar2 *destinationBackupFileName, guint32 flags, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = ReplaceFile (destinationFileName, sourceFileName, destinationBackupFileName, flags, NULL, NULL);
	if (result == FALSE) {
		*error=GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}

gboolean
mono_file_io_copy_file (gunichar2 *path, gunichar2 *dest, gboolean overwrite, gint32 *error)
{
	gboolean						result = FALSE;
	COPYFILE2_EXTENDED_PARAMETERS	copy_param = {0};

	copy_param.dwSize = sizeof (COPYFILE2_EXTENDED_PARAMETERS);
	copy_param.dwCopyFlags = (!overwrite) ? COPY_FILE_FAIL_IF_EXISTS : 0;

	MONO_ENTER_GC_SAFE;

	result = SUCCEEDED (CopyFile2 (path, dest, &copy_param));
	if (result == FALSE) {
		*error=GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}

gint64
mono_file_io_get_file_size (HANDLE handle, gint32 *error)
{
	LARGE_INTEGER length;

	MONO_ENTER_GC_SAFE;

	if (!GetFileSizeEx (handle, &length)) {
		*error=GetLastError ();
		length.QuadPart = INVALID_FILE_SIZE;
	}

	MONO_EXIT_GC_SAFE;
	return length.QuadPart;
}

gboolean
mono_file_io_lock_file (HANDLE handle, gint64 position, gint64 length, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = LockFile (handle, position & 0xFFFFFFFF, position >> 32,
			   length & 0xFFFFFFFF, length >> 32);

	if (result == FALSE) {
		*error = GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}

gboolean
mono_file_io_unlock_file (HANDLE handle, gint64 position, gint64 length, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = UnlockFile (handle, position & 0xFFFFFFFF, position >> 32,
			     length & 0xFFFFFFFF, length >> 32);

	if (result == FALSE) {
		*error = GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_file_io_windows_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
