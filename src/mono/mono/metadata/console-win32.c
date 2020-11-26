/**
 * \file
 * ConsoleDriver internal calls for Win32
 *
 * Author:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright (C) 2005-2009 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <fcntl.h>
#include <errno.h>
#include <signal.h>
#include <sys/types.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#include <mono/metadata/appdomain.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/console-io.h>
#include <mono/metadata/exception.h>
#include <mono/utils/w32subset.h>
#include "icall-decl.h"

void
mono_console_init (void)
{
}

void
mono_console_handle_async_ops (void)
{
}

#if HAVE_API_SUPPORT_WIN32_CONSOLE
MonoBoolean
ves_icall_System_ConsoleDriver_Isatty (HANDLE handle, MonoError* error)
{
	DWORD mode;
	return GetConsoleMode (handle, &mode) != 0;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean want_echo, MonoError* error)
{
	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break, MonoError* error)
{
	return FALSE;
}

gint32
ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout, MonoError* error)
{
	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_TtySetup (MonoStringHandle keypad, MonoStringHandle teardown, MonoArrayHandleOut control_chars, int **size, MonoError* error)
{
	return FALSE;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_CONSOLE
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
#endif /* HAVE_API_SUPPORT_WIN32_CONSOLE */
