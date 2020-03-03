/**
 * \file
 * UWP w32file support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"
#include <windows.h>
#include "mono/metadata/w32file-win32-internals.h"
#include "mono/metadata/w32subset.h"
#include "icall-decl.h"

#if !HAVE_API_SUPPORT_WIN32_MOVE_FILE && HAVE_API_SUPPORT_WIN32_MOVE_FILE_EX
gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = MoveFileExW (path, dest, MOVEFILE_COPY_ALLOWED);
	if (result == FALSE) {
		*error=GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}
#endif

#if !HAVE_API_SUPPORT_WIN32_COPY_FILE && HAVE_API_SUPPORT_WIN32_COPY_FILE2
gboolean
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error)
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
#endif

#if !HAVE_API_SUPPORT_WIN32_GET_STD_HANDLE
HANDLE
mono_w32file_get_console_output (void)
{
	ERROR_DECL (error);

	g_unsupported_api ("GetStdHandle (STD_OUTPUT_HANDLE)");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetStdHandle (STD_OUTPUT_HANDLE)");
	mono_error_set_pending_exception (error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return INVALID_HANDLE_VALUE;
}

HANDLE
mono_w32file_get_console_input (void)
{
	ERROR_DECL (error);

	g_unsupported_api ("GetStdHandle (STD_INPUT_HANDLE)");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetStdHandle (STD_INPUT_HANDLE)");
	mono_error_set_pending_exception (error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return INVALID_HANDLE_VALUE;
}

HANDLE
mono_w32file_get_console_error (void)
{
	ERROR_DECL (error);

	g_unsupported_api ("GetStdHandle (STD_ERROR_HANDLE)");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetStdHandle (STD_ERROR_HANDLE)");
	mono_error_set_pending_exception (error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return INVALID_HANDLE_VALUE;
}
#endif // HAVE_API_SUPPORT_WIN32_GET_STD_HANDLE

MONO_EMPTY_SOURCE_FILE (file_io_windows_uwp);
