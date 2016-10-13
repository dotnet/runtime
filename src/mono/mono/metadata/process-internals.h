/*
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_PROCESS_INTERNALS_H__
#define __MONO_METADATA_PROCESS_INTERNALS_H__

#include <config.h>
#include <glib.h>

// On Windows platform implementation of bellow methods are hosted in separate source file
// process-windows.c or process-windows-*.c. On other platforms the implementation is still keept
// in process.c still declared as static and in some places even inlined.
#ifdef HOST_WIN32
gchar*
mono_process_quote_path (const gchar *path);

gchar*
mono_process_unquote_application_name (gchar *path);

gboolean
mono_process_get_shell_arguments (MonoProcessStartInfo *proc_start_info, gunichar2 **shell_path,
				  MonoString **cmd);
#endif  /* HOST_WIN32 */

// On platforms not using classic WIN API support the  implementation of bellow methods are hosted in separate source file
// process-windows-*.c. On platforms using classic WIN API the implementation is still keept in process.c and still declared
// static and in some places even inlined.
#if !G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
void
process_get_fileversion (MonoObject *filever, gunichar2 *filename, MonoError *error);

void
mono_process_init_startup_info (HANDLE stdin_handle, HANDLE stdout_handle,
				HANDLE stderr_handle,STARTUPINFO *startinfo);

gboolean
mono_process_create_process (MonoProcInfo *mono_process_info, gunichar2 *shell_path, MonoString *cmd,
			     guint32 creation_flags, gchar *env_vars, gunichar2 *dir, STARTUPINFO *start_info,
			     PROCESS_INFORMATION *process_info);
#endif  /* !G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

// Shared between all platforms and implemented in process.c.
gboolean
mono_process_complete_path (const gunichar2 *appname, gchar **completed);

#endif /* __MONO_METADATA_PROCESS_INTERNALS_H__ */
