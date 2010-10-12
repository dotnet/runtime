/*
 * console-null.c: Null driver, does nothing.
 *
 * Author:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright (C) 2005-2009 Novell, Inc. (http://www.novell.com)
 */

#include <mono/metadata/appdomain.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/gc-internal.h>

#include <mono/metadata/console-io.h>
#include <mono/metadata/exception.h>

void
mono_console_init (void)
{
}

void
mono_console_handle_async_ops (void)
{
}

MonoBoolean
ves_icall_System_ConsoleDriver_Isatty (HANDLE handle)
{
	MONO_ARCH_SAVE_REGS;

	return (GetFileType (handle) == FILE_TYPE_CHAR);
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean want_echo)
{
	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break)
{
	return FALSE;
}

gint32
ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout)
{
	return FALSE;
}

MonoBoolean
ves_icall_System_ConsoleDriver_TtySetup (MonoString *keypad, MonoString *teardown, MonoArray **control_chars, int **size)
{
	return FALSE;
}
