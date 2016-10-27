/*
 * process-windows.c: Windows process support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <winsock2.h>
#include <windows.h>
#include "mono/metadata/process-windows-internals.h"

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline gboolean
mono_process_win_enum_processes (DWORD *pids, DWORD count, DWORD *needed)
{
	return EnumProcesses (pids, count, needed);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoArray *
ves_icall_System_Diagnostics_Process_GetProcesses_internal (void)
{
	MonoError error;
	MonoArray *procs;
	gboolean ret;
	DWORD needed;
	int count;
	DWORD *pids;

	count = 512;
	do {
		pids = g_new0 (DWORD, count);
		ret = mono_process_win_enum_processes (pids, count * sizeof (guint32), &needed);
		if (ret == FALSE) {
			MonoException *exc;

			g_free (pids);
			pids = NULL;
			exc = mono_get_exception_not_supported ("This system does not support EnumProcesses");
			mono_set_pending_exception (exc);
			return NULL;
		}
		if (needed < (count * sizeof (guint32)))
			break;
		g_free (pids);
		pids = NULL;
		count = (count * 3) / 2;
	} while (TRUE);

	count = needed / sizeof (guint32);
	procs = mono_array_new_checked (mono_domain_get (), mono_get_int32_class (), count, &error);
	if (mono_error_set_pending_exception (&error)) {
		g_free (pids);
		return NULL;
	}

	memcpy (mono_array_addr (procs, guint32, 0), pids, needed);
	g_free (pids);
	pids = NULL;

	return procs;
}

gchar*
mono_process_quote_path (const gchar *path)
{
	gchar *res = g_shell_quote (path);
	gchar *q = res;
	while (*q) {
		if (*q == '\'')
			*q = '\"';
		q++;
	}
	return res;
}

gchar*
mono_process_unquote_application_name (gchar *appname)
{
	size_t len = strlen (appname);
	if (len) {
		if (appname[len-1] == '\"')
			appname[len-1] = '\0';
		if (appname[0] == '\"')
			appname++;
	}

	return appname;
}

gboolean
mono_process_get_shell_arguments (MonoProcessStartInfo *proc_start_info, gunichar2 **shell_path, MonoString **cmd)
{
	gchar		*spath = NULL;
	gchar		*new_cmd, *cmd_utf8;
	MonoError	mono_error;

	*shell_path = NULL;
	*cmd = proc_start_info->arguments;

	mono_process_complete_path (mono_string_chars (proc_start_info->filename), &spath);
	if (spath != NULL) {
		/* Seems like our CreateProcess does not work as the windows one.
		 * This hack is needed to deal with paths containing spaces */
		if (*cmd) {
			cmd_utf8 = mono_string_to_utf8_checked (*cmd, &mono_error);
			if (!mono_error_set_pending_exception (&mono_error)) {
				new_cmd = g_strdup_printf ("%s %s", spath, cmd_utf8);
				*cmd = mono_string_new_wrapper (new_cmd);
				g_free (cmd_utf8);
				g_free (new_cmd);
			} else {
				*cmd = NULL;
			}
		}
		else {
			*cmd = mono_string_new_wrapper (spath);
		}

		g_free (spath);
	}

	return (*cmd != NULL) ? TRUE : FALSE;
}
#endif /* HOST_WIN32 */
