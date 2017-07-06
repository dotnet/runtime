/**
 * \file
 * System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright 2002 Ximian, Inc.
 * Copyright 2002-2006 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <glib.h>
#include <string.h>

#include <winsock2.h>
#include <windows.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/w32process.h>
#include <mono/metadata/w32process-win32-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-proclib.h>
/* FIXME: fix this code to not depend so much on the internals */
#include <mono/metadata/class-internals.h>
#include <mono/metadata/w32handle.h>
#include <mono/utils/w32api.h>

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#include <shellapi.h>
#endif

void
mono_w32process_init (void)
{
}

void
mono_w32process_cleanup (void)
{
}

void
mono_w32process_signal_finished (void)
{
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid)
{
	HANDLE handle;
	
	/* GetCurrentProcess returns a pseudo-handle, so use
	 * OpenProcess instead
	 */
	handle = OpenProcess (PROCESS_ALL_ACCESS, TRUE, pid);
	if (handle == NULL)
		/* FIXME: Throw an exception */
		return NULL;
	return handle;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfo *proc_start_info, MonoW32ProcessInfo *process_info)
{
	SHELLEXECUTEINFO shellex = {0};
	gboolean ret;

	shellex.cbSize = sizeof(SHELLEXECUTEINFO);
	shellex.fMask = (gulong)(SEE_MASK_FLAG_DDEWAIT | SEE_MASK_NOCLOSEPROCESS | SEE_MASK_UNICODE);
	shellex.nShow = (gulong)proc_start_info->window_style;
	shellex.nShow = (gulong)((shellex.nShow == 0) ? 1 : (shellex.nShow == 1 ? 0 : shellex.nShow));

	if (proc_start_info->filename != NULL) {
		shellex.lpFile = mono_string_chars (proc_start_info->filename);
	}

	if (proc_start_info->arguments != NULL) {
		shellex.lpParameters = mono_string_chars (proc_start_info->arguments);
	}

	if (proc_start_info->verb != NULL &&
	    mono_string_length (proc_start_info->verb) != 0) {
		shellex.lpVerb = mono_string_chars (proc_start_info->verb);
	}

	if (proc_start_info->working_directory != NULL &&
	    mono_string_length (proc_start_info->working_directory) != 0) {
		shellex.lpDirectory = mono_string_chars (proc_start_info->working_directory);
	}

	if (proc_start_info->error_dialog) {	
		shellex.hwnd = proc_start_info->error_dialog_parent_handle;
	} else {
		shellex.fMask = (gulong)(shellex.fMask | SEE_MASK_FLAG_NO_UI);
	}

	ret = ShellExecuteEx (&shellex);
	if (ret == FALSE) {
		process_info->pid = -GetLastError ();
	} else {
		process_info->process_handle = shellex.hProcess;
		process_info->thread_handle = NULL;
#if !defined(MONO_CROSS_COMPILE)
		process_info->pid = GetProcessId (shellex.hProcess);
#else
		process_info->pid = 0;
#endif
		process_info->tid = 0;
	}

	return ret;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline void
mono_process_init_startup_info (HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, STARTUPINFO *startinfo)
{
	startinfo->cb = sizeof(STARTUPINFO);
	startinfo->dwFlags = STARTF_USESTDHANDLES;
	startinfo->hStdInput = stdin_handle;
	startinfo->hStdOutput = stdout_handle;
	startinfo->hStdError = stderr_handle;
	return;
}

static gboolean
mono_process_create_process (MonoW32ProcessInfo *mono_process_info, MonoString *cmd, guint32 creation_flags,
	gunichar2 *env_vars, gunichar2 *dir, STARTUPINFO *start_info, PROCESS_INFORMATION *process_info)
{
	gboolean result = FALSE;

	if (mono_process_info->username) {
		guint32 logon_flags = mono_process_info->load_user_profile ? LOGON_WITH_PROFILE : 0;

		result = CreateProcessWithLogonW (mono_string_chars (mono_process_info->username),
						  mono_process_info->domain ? mono_string_chars (mono_process_info->domain) : NULL,
						  (const gunichar2 *)mono_process_info->password,
						  logon_flags,
						  NULL,
						  cmd ? mono_string_chars (cmd) : NULL,
						  creation_flags,
						  env_vars, dir, start_info, process_info);

	} else {

		result = CreateProcessW (NULL,
					cmd ? mono_string_chars (cmd): NULL,
					NULL,
					NULL,
					TRUE,
					creation_flags,
					env_vars,
					dir,
					start_info,
					process_info);

	}

	return result;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

static gchar*
process_unquote_application_name (gchar *appname)
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

static gchar*
process_quote_path (const gchar *path)
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

/* Only used when UseShellExecute is false */
static gboolean
process_complete_path (const gunichar2 *appname, gchar **completed)
{
	gchar *utf8app, *utf8appmemory;
	gchar *found;

	utf8appmemory = g_utf16_to_utf8 (appname, -1, NULL, NULL, NULL);
	utf8app = process_unquote_application_name (utf8appmemory);

	if (g_path_is_absolute (utf8app)) {
		*completed = process_quote_path (utf8app);
		g_free (utf8appmemory);
		return TRUE;
	}

	if (g_file_test (utf8app, G_FILE_TEST_IS_EXECUTABLE) && !g_file_test (utf8app, G_FILE_TEST_IS_DIR)) {
		*completed = process_quote_path (utf8app);
		g_free (utf8appmemory);
		return TRUE;
	}
	
	found = g_find_program_in_path (utf8app);
	if (found == NULL) {
		*completed = NULL;
		g_free (utf8appmemory);
		return FALSE;
	}

	*completed = process_quote_path (found);
	g_free (found);
	g_free (utf8appmemory);
	return TRUE;
}

static gboolean
process_get_shell_arguments (MonoW32ProcessStartInfo *proc_start_info, MonoString **cmd)
{
	gchar		*spath = NULL;
	gchar		*new_cmd, *cmd_utf8;
	MonoError	mono_error;

	*cmd = proc_start_info->arguments;

	if (process_complete_path (mono_string_chars (proc_start_info->filename), &spath)) {
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

MonoBoolean
ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoW32ProcessStartInfo *proc_start_info, HANDLE stdin_handle,
							     HANDLE stdout_handle, HANDLE stderr_handle, MonoW32ProcessInfo *process_info)
{
	gboolean ret;
	gunichar2 *dir;
	STARTUPINFO startinfo={0};
	PROCESS_INFORMATION procinfo;
	gunichar2 *env_vars = NULL;
	MonoString *cmd = NULL;
	guint32 creation_flags;

	mono_process_init_startup_info (stdin_handle, stdout_handle, stderr_handle, &startinfo);

	creation_flags = CREATE_UNICODE_ENVIRONMENT;
	if (proc_start_info->create_no_window)
		creation_flags |= CREATE_NO_WINDOW;
	
	if (process_get_shell_arguments (proc_start_info, &cmd) == FALSE) {
		process_info->pid = -ERROR_FILE_NOT_FOUND;
		return FALSE;
	}

	if (process_info->env_variables) {
		gint i, len;
		MonoString *var;
		gunichar2 *str, *ptr;

		len = 0;

		for (i = 0; i < mono_array_length (process_info->env_variables); i++) {
			var = mono_array_get (process_info->env_variables, MonoString*, i);

			len += mono_string_length (var) * sizeof (gunichar2);

			/* null-separated */
			len += sizeof (gunichar2);
		}
		/* null-terminated */
		len += sizeof (gunichar2);

		env_vars = ptr = g_new0 (gunichar2, len);

		for (i = 0; i < mono_array_length (process_info->env_variables); i++) {
			var = mono_array_get (process_info->env_variables, MonoString*, i);

			memcpy (ptr, mono_string_chars (var), mono_string_length (var) * sizeof (gunichar2));
			ptr += mono_string_length (var);
			ptr += 1; // Skip over the null-separator
		}
	}
	
	/* The default dir name is "".  Turn that into NULL to mean
	 * "current directory"
	 */
	if (proc_start_info->working_directory == NULL || mono_string_length (proc_start_info->working_directory) == 0)
		dir = NULL;
	else
		dir = mono_string_chars (proc_start_info->working_directory);

	ret = mono_process_create_process (process_info, cmd, creation_flags, env_vars, dir, &startinfo, &procinfo);

	g_free (env_vars);

	if (ret) {
		process_info->process_handle = procinfo.hProcess;
		/*process_info->thread_handle=procinfo.hThread;*/
		process_info->thread_handle = NULL;
		if (procinfo.hThread != NULL && procinfo.hThread != INVALID_HANDLE_VALUE)
			CloseHandle (procinfo.hThread);
		process_info->pid = procinfo.dwProcessId;
		process_info->tid = procinfo.dwThreadId;
	} else {
		process_info->pid = -GetLastError ();
	}
	
	return ret;
}

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

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_CloseProcess (gpointer handle)
{
	return CloseHandle (handle);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess (gpointer handle, gint32 exitcode)
{
	return TerminateProcess (handle, exitcode);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess (gpointer handle, gint32 *exitcode)
{
	return GetExitCodeProcess (handle, exitcode);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline MonoBoolean
mono_icall_get_process_working_set_size (gpointer handle, gsize *min, gsize *max)
{
	return GetProcessWorkingSetSize (handle, min, max);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max)
{
	return mono_icall_get_process_working_set_size (handle, min, max);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline MonoBoolean
mono_icall_set_process_working_set_size (gpointer handle, gsize min, gsize max)
{
	return SetProcessWorkingSetSize (handle, min, max);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max)
{
	return mono_icall_set_process_working_set_size (handle, min, max);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline gint32
mono_icall_get_priority_class (gpointer handle)
{
	return GetPriorityClass (handle);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

gint32
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle)
{
	return mono_icall_get_priority_class (handle);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline MonoBoolean
mono_icall_set_priority_class (gpointer handle, gint32 priorityClass)
{
	return SetPriorityClass (handle, (guint32) priorityClass);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass)
{
	return mono_icall_set_priority_class (handle, priorityClass);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes (gpointer handle, gint64 *creationtime, gint64 *exittime, gint64 *kerneltime, gint64 *usertime)
{
	return GetProcessTimes (handle, (LPFILETIME) creationtime, (LPFILETIME) exittime, (LPFILETIME) kerneltime, (LPFILETIME) usertime);
}

gpointer
ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess (void)
{
	return GetCurrentProcess ();
}
