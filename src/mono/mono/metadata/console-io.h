/**
 * \file
 * Console IO internal calls
 *
 * Author:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_CONSOLEIO_H
#define _MONO_METADATA_CONSOLEIO_H

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/utils/mono-compiler.h>

G_BEGIN_DECLS

void mono_console_init (void);
void mono_console_handle_async_ops (void);
MonoBoolean ves_icall_System_ConsoleDriver_Isatty (gpointer handle);
gint32 ves_icall_System_ConsoleDriver_InternalKeyAvailable (gint32 timeout);
MonoBoolean ves_icall_System_ConsoleDriver_SetEcho (MonoBoolean echo);
MonoBoolean ves_icall_System_ConsoleDriver_SetBreak (MonoBoolean want_break);
MonoBoolean ves_icall_System_ConsoleDriver_TtySetup (MonoString *keypad, MonoString *teardown, MonoArray **control_characters, int **size);
void ves_icall_System_ConsoleDriver_Suspend (void);

G_END_DECLS

#endif /* _MONO_METADATA_CONSOLEIO_H */

