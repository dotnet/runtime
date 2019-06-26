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
#include "icall-decl.h"

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
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info, MonoError *error)
{
	MonoCreateProcessCoop coop;
	mono_createprocess_coop_init (&coop, proc_start_info, process_info);

	SHELLEXECUTEINFO shellex = {0};
	gboolean ret;

	shellex.cbSize = sizeof(SHELLEXECUTEINFO);
	shellex.fMask = (gulong)(SEE_MASK_NOASYNC | SEE_MASK_NOCLOSEPROCESS | SEE_MASK_UNICODE);
	shellex.nShow = (gulong)MONO_HANDLE_GETVAL (proc_start_info, window_style);
	shellex.nShow = (gulong)((shellex.nShow == 0) ? 1 : (shellex.nShow == 1 ? 0 : shellex.nShow));

	shellex.lpFile = coop.filename;
	shellex.lpParameters = coop.arguments;

	if (coop.length.verb)
		shellex.lpVerb = coop.verb;

	if (coop.length.working_directory)
		shellex.lpDirectory = coop.working_directory;

	if (MONO_HANDLE_GETVAL (proc_start_info, error_dialog))
		shellex.hwnd = (HWND)MONO_HANDLE_GETVAL (proc_start_info, error_dialog_parent_handle);
	else
		shellex.fMask = (gulong)(shellex.fMask | SEE_MASK_FLAG_NO_UI);

	MONO_ENTER_GC_SAFE;
	ret = ShellExecuteEx (&shellex);
	MONO_EXIT_GC_SAFE;
	
	if (ret == FALSE) {
		process_info->pid = -GetLastError ();
	} else {
		process_info->process_handle = shellex.hProcess;
#if !defined(MONO_CROSS_COMPILE)
		process_info->pid = GetProcessId (shellex.hProcess);
#else
		process_info->pid = 0;
#endif
	}

	mono_createprocess_coop_cleanup (&coop);

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
mono_process_create_process (MonoCreateProcessCoop *coop, MonoW32ProcessInfo *mono_process_info,
	MonoStringHandle cmd, guint32 creation_flags, gunichar2 *env_vars, gunichar2 *dir, STARTUPINFO *start_info,
	PROCESS_INFORMATION *process_info)
{
	gboolean result = FALSE;
	gchandle_t cmd_gchandle = 0;
	gunichar2 *cmd_chars = MONO_HANDLE_IS_NULL (cmd) ? NULL : mono_string_handle_pin_chars (cmd, &cmd_gchandle);

	MONO_ENTER_GC_SAFE;
	if (coop->username) {
		guint32 logon_flags = mono_process_info->load_user_profile ? LOGON_WITH_PROFILE : 0;

		result = CreateProcessWithLogonW (coop->username,
						  coop->domain,
						  mono_process_info->password,
						  logon_flags,
						  NULL,
						  cmd_chars,
						  creation_flags,
						  env_vars, dir, start_info, process_info);
	} else {
		result = CreateProcessW (NULL,
					cmd_chars,
					NULL,
					NULL,
					TRUE,
					creation_flags,
					env_vars,
					dir,
					start_info,
					process_info);
	}
	MONO_EXIT_GC_SAFE;

	mono_gchandle_free_internal (cmd_gchandle);

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
	// FIXME This function should stick to gunichar2.

	char *utf8app;
	char *utf8appmemory = NULL;
	char *found = NULL;
	gboolean result, file_test_result;

	utf8appmemory = g_utf16_to_utf8 (appname, -1, NULL, NULL, NULL);
	utf8app = process_unquote_application_name (utf8appmemory);

	if (g_path_is_absolute (utf8app)) {
		*completed = process_quote_path (utf8app);
		result = TRUE;
		goto exit;
	}

	MONO_ENTER_GC_SAFE;
	file_test_result = g_file_test (utf8app, G_FILE_TEST_IS_EXECUTABLE) && !g_file_test (utf8app, G_FILE_TEST_IS_DIR);
	MONO_EXIT_GC_SAFE;
	if (file_test_result) {
		*completed = process_quote_path (utf8app);
		result = TRUE;
		goto exit;
	}
	
	MONO_ENTER_GC_SAFE;
	found = g_find_program_in_path (utf8app);
	MONO_EXIT_GC_SAFE;
	if (found == NULL) {
		*completed = NULL;
		result = FALSE;
		goto exit;
	}

	*completed = process_quote_path (found);
	result = TRUE;
exit:
	g_free (found);
	g_free (utf8appmemory);
	return result;
}

static gboolean
process_get_shell_arguments (MonoCreateProcessCoop *proc_start_info, MonoStringHandle *cmd, MonoError *error)
{
	char *spath = NULL;
	char *new_cmd = NULL;
	char *cmd_utf8 = NULL;

	*cmd = proc_start_info->coophandle.arguments;

	// FIXME There are excess utf8 <=> gunichar2 conversions here.
	// We are either returning spath, or spath + " " + cmd.
	// Just use gunichar2. Maybe move logic to C#.

	if (process_complete_path (proc_start_info->filename, &spath)) {
		/* Seems like our CreateProcess does not work as the windows one.
		 * This hack is needed to deal with paths containing spaces */
		if (!MONO_HANDLE_IS_NULL (*cmd)) {
			cmd_utf8 = mono_string_handle_to_utf8 (*cmd, error);
			goto_if_nok (error, error);
			new_cmd = g_strdup_printf ("%s %s", spath, cmd_utf8);
			*cmd = mono_string_new_utf8_len (mono_domain_get (), new_cmd, strlen (new_cmd), error);
			goto_if_nok (error, error);
		}
		else {
			*cmd = mono_string_new_utf8_len (mono_domain_get (), spath, strlen (spath), error);
			goto_if_nok (error, error);
		}
	}

exit:
	g_free (spath);
	g_free (cmd_utf8);
	g_free (new_cmd);
	return !MONO_HANDLE_IS_NULL (*cmd);
error:
	*cmd = NULL_HANDLE_STRING;
	goto exit;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoW32ProcessStartInfoHandle proc_start_info,
	HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoW32ProcessInfo *process_info, MonoError *error)
{
	MonoCreateProcessCoop coop;
	mono_createprocess_coop_init (&coop, proc_start_info, process_info);

	gboolean ret;
	gunichar2 *dir = NULL;
	STARTUPINFO startinfo={0};
	PROCESS_INFORMATION procinfo;
	gunichar2 *env_vars = NULL;
	MonoStringHandle cmd = NULL_HANDLE_STRING;
	guint32 creation_flags;

	mono_process_init_startup_info (stdin_handle, stdout_handle, stderr_handle, &startinfo);

	creation_flags = CREATE_UNICODE_ENVIRONMENT;
	if (MONO_HANDLE_GETVAL (proc_start_info, create_no_window))
		creation_flags |= CREATE_NO_WINDOW;
	
	if (process_get_shell_arguments (&coop, &cmd, error) == FALSE) {
		// FIXME This should be passed back separately.
		process_info->pid = -ERROR_FILE_NOT_FOUND;
		ret = FALSE;
		goto exit;
	}

	if (process_info->env_variables) {
		MonoArrayHandle array = MONO_HANDLE_NEW (MonoArray, process_info->env_variables);
		MonoStringHandle var = MONO_HANDLE_NEW (MonoString, NULL);
		gsize const array_length = mono_array_handle_length (array);

		// nul-separated and nul-terminated
		gsize len = array_length + 1 + !array_length;

		for (gsize i = 0; i < array_length; i++) {
			MONO_HANDLE_ARRAY_GETREF (var, array, i);
			len += mono_string_handle_length (var);
		}

		gunichar2 *ptr = g_new0 (gunichar2, len);
		env_vars = ptr;

		for (gsize i = 0; i < array_length; i++) {
			MONO_HANDLE_ARRAY_GETREF (var, array, i);
			gchandle_t gchandle = 0;
			memcpy (ptr, mono_string_handle_pin_chars (var, &gchandle), mono_string_handle_length (var) * sizeof (gunichar2));
			mono_gchandle_free_internal (gchandle);
			ptr += mono_string_handle_length (var);
			ptr += 1; // Skip over the null-separator
		}
	}
	
	/* The default dir name is "".  Turn that into NULL to mean
	 * "current directory"
	 */
	if (coop.length.working_directory)
		dir = coop.working_directory;

	ret = mono_process_create_process (&coop, process_info, cmd, creation_flags, env_vars, dir, &startinfo, &procinfo);

	g_free (env_vars);

	if (ret) {
		process_info->process_handle = procinfo.hProcess;
		/*process_info->thread_handle=procinfo.hThread;*/
		if (procinfo.hThread != NULL && procinfo.hThread != INVALID_HANDLE_VALUE)
			CloseHandle (procinfo.hThread);
		process_info->pid = procinfo.dwProcessId;
	} else {
		// FIXME This should be passed back separately.
		process_info->pid = -GetLastError ();
	}

exit:
	mono_createprocess_coop_cleanup (&coop);
	return ret;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static gboolean
mono_process_win_enum_processes (DWORD *pids, DWORD count, DWORD *needed)
{
	gboolean success;
	MONO_ENTER_GC_SAFE;
	success = EnumProcesses (pids, count, needed);
	MONO_EXIT_GC_SAFE;
	return success;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoArray *
ves_icall_System_Diagnostics_Process_GetProcesses_internal (void)
{
	ERROR_DECL (error);
	MonoArray *procs = NULL;
	DWORD needed = 0;
	DWORD *pids = NULL;
	int count = 512;

	do {
		pids = g_new0 (DWORD, count);
		if (!mono_process_win_enum_processes (pids, count * sizeof (guint32), &needed)) {
			// FIXME GetLastError?
			mono_error_set_not_supported (error, "This system does not support EnumProcesses");
			mono_error_set_pending_exception (error);
			goto exit;
		}
		if (needed < (count * sizeof (guint32)))
			break;
		g_free (pids);
		pids = NULL;
		count = (count * 3) / 2;
	} while (TRUE);

	count = needed / sizeof (guint32);
	procs = mono_array_new_checked (mono_domain_get (), mono_get_int32_class (), count, error);
	if (mono_error_set_pending_exception (error)) {
		procs = NULL;
		goto exit;
	}

	memcpy (mono_array_addr_internal (procs, guint32, 0), pids, needed);
exit:
	g_free (pids);
	return procs;
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_CloseProcess (gpointer handle, MonoError *error)
{
	return CloseHandle (handle);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess (gpointer handle, gint32 exitcode, MonoError *error)
{
	return TerminateProcess (handle, exitcode);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess (gpointer handle, gint32 *exitcode, MonoError *error)
{
	return GetExitCodeProcess (handle, (PDWORD)exitcode);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline MonoBoolean
mono_icall_get_process_working_set_size (gpointer handle, gsize *min, gsize *max)
{
	return GetProcessWorkingSetSize (handle, (PSIZE_T)min, (PSIZE_T)max);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max, MonoError *error)
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
ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max, MonoError *error)
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
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle, MonoError *error)
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
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass, MonoError *error)
{
	return mono_icall_set_priority_class (handle, priorityClass);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes (gpointer handle, gint64 *creationtime, gint64 *exittime, gint64 *kerneltime, gint64 *usertime, MonoError *error)
{
	return GetProcessTimes (handle, (LPFILETIME) creationtime, (LPFILETIME) exittime, (LPFILETIME) kerneltime, (LPFILETIME) usertime);
}

gpointer
ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess (MonoError *error)
{
	return GetCurrentProcess ();
}
