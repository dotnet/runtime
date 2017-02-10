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

MonoBoolean
ves_icall_System_ConsoleDriver_Isatty (HANDLE handle)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("Console");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "Console");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean want_echo)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("Console");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "Console");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("Console");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "Console");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gint32
ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("Console");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "Console");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_TtySetup (MonoString *keypad, MonoString *teardown, MonoArray **control_chars, int **size)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("Console");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "Console");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (console_win32_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
