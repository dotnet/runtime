/**
 * \file
 * UWP console support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include "mono/metadata/console-win32-internals.h"
#include "icall-decl.h"

MonoBoolean
ves_icall_System_ConsoleDriver_Isatty (HANDLE handle, MonoError* error)
{
	g_unsupported_api ("Console");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "Console");

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean want_echo, MonoError* error)
{
	g_unsupported_api ("Console");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "Console");

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break, MonoError* error)
{
	g_unsupported_api ("Console");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "Console");

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gint32
ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout, MonoError* error)
{
	g_unsupported_api ("Console");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "Console");

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_TtySetup (MonoStringHandle keypad, MonoStringHandle teardown, MonoArrayHandleOut control_chars, int **size, MonoError* error)
{
	g_unsupported_api ("Console");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "Console");

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (console_win32_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
