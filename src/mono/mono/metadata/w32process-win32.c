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

#include <mono/metadata/w32process.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-proclib.h>
#include <mono/metadata/w32handle.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/w32subset.h>

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

guint32
mono_w32process_get_pid (gpointer handle)
{
	return GetProcessId (handle);
}

#if HAVE_API_SUPPORT_WIN32_ENUM_WINDOWS
typedef struct {
	DWORD pid;
	HWND hwnd;
} EnumWindowsArgs;

static BOOL CALLBACK
mono_enum_windows_callback(HWND hwnd, LPARAM lparam)
{
	EnumWindowsArgs *args = (EnumWindowsArgs *)lparam;
	DWORD pid = 0;
	GetWindowThreadProcessId(hwnd, &pid);
	if (pid != args->pid || GetWindow(hwnd, GW_OWNER) != NULL || !IsWindowVisible(hwnd)) return TRUE;
	args->hwnd = hwnd;
	return FALSE;
}

HANDLE
ves_icall_System_Diagnostics_Process_MainWindowHandle_internal (guint32 pid, MonoError *error)
{
	EnumWindowsArgs args = {pid, NULL};
	EnumWindows(mono_enum_windows_callback, (LPARAM)&args);
	return args.hwnd;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_ENUM_WINDOWS
HANDLE
ves_icall_System_Diagnostics_Process_MainWindowHandle_internal (guint32 pid, MonoError *error)
{
	/*TODO: Implement for uwp*/
	return NULL;
}
#endif /* HAVE_API_SUPPORT_WIN32_ENUM_WINDOWS */

#if HAVE_API_SUPPORT_WIN32_ENUM_PROCESS_MODULES
gboolean
mono_w32process_try_get_modules (gpointer process, gpointer *modules, guint32 size, guint32 *needed)
{
	return EnumProcessModules (process, (HMODULE *)modules, size, (PDWORD)needed);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_ENUM_PROCESS_MODULES
gboolean
mono_w32process_try_get_modules (gpointer process, gpointer *modules, guint32 size, guint32 *needed)
{
	g_unsupported_api ("EnumProcessModules");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_GET_MODULE_BASE_NAME
gboolean
mono_w32process_module_get_name (gpointer process, gpointer module, gunichar2 **str, guint32 *len)
{
	return mono_get_module_basename (process, module, str, len);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_MODULE_BASE_NAME
gboolean
mono_w32process_module_get_name (gpointer process, gpointer module, gunichar2 **str, guint32 *len)
{
	g_unsupported_api ("GetModuleBaseName");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
 }
#endif /* HAVE_API_SUPPORT_WIN32_GET_MODULE_BASE_NAME */

#if HAVE_API_SUPPORT_WIN32_GET_MODULE_FILE_NAME_EX
gboolean
mono_w32process_module_get_filename (gpointer process, gpointer module, gunichar2 **str, guint32 *len)
{
	return mono_get_module_filename_ex (process, module, str, len);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_MODULE_FILE_NAME_EX
gboolean
mono_w32process_module_get_filename (gpointer process, gpointer module, gunichar2 **str, guint32 *len)
{
	g_unsupported_api ("GetModuleFileNameEx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
 }
#endif /* HAVE_API_SUPPORT_WIN32_GET_MODULE_FILE_NAME_EX */

#if HAVE_API_SUPPORT_WIN32_GET_MODULE_INFORMATION
gboolean
mono_w32process_module_get_information (gpointer process, gpointer module, gpointer modinfo, guint32 size)
{
	return GetModuleInformation (process, (HMODULE)module, (MODULEINFO *)modinfo, size);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_MODULE_INFORMATION
gboolean
mono_w32process_module_get_information (gpointer process, gpointer module, gpointer modinfo, guint32 size)
{
	g_unsupported_api ("GetModuleInformation");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_GET_MODULE_INFORMATION */

#if HAVE_API_SUPPORT_WIN32_GET_FILE_VERSION_INFO
gboolean
mono_w32process_get_fileversion_info (const gunichar2 *filename, gpointer *data)
{
	DWORD handle;

	g_assert (data);
	*data = NULL;

	DWORD datasize = GetFileVersionInfoSizeW (filename, &handle);
	if (datasize <= 0)
		return FALSE;

	*data = g_malloc0 (datasize);
	if (!GetFileVersionInfoW (filename, handle, datasize, *data)) {
		g_free (*data);
		return FALSE;
	}

	return TRUE;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_FILE_VERSION_INFO
gboolean
mono_w32process_get_fileversion_info (const gunichar2 *filename, gpointer *data)
{
	g_unsupported_api ("GetFileVersionInfo");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_GET_FILE_VERSION_INFO */

#if HAVE_API_SUPPORT_WIN32_VER_QUERY_VALUE
gboolean
mono_w32process_ver_query_value (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len)
{
	return VerQueryValueW (datablock, subblock, buffer, len);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_VER_QUERY_VALUE
gboolean
mono_w32process_ver_query_value (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len)
{
	g_unsupported_api ("VerQueryValue");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_VER_QUERY_VALUE */

#if HAVE_API_SUPPORT_WIN32_VER_LANGUAGE_NAME
guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	return VerLanguageNameW (lang, lang_out, lang_len);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_VER_LANGUAGE_NAME
guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	g_unsupported_api ("VerLanguageName");
	SetLastError (ERROR_NOT_SUPPORTED);
	return 0;
}
#endif /* HAVE_API_SUPPORT_WIN32_VER_LANGUAGE_NAME */

#if HAVE_API_SUPPORT_WIN32_OPEN_PROCESS
HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid, MonoError *error)
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
#elif !HAVE_EXTERN_DEFINED_WIN32_OPEN_PROCESS
HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid, MonoError *error)
{
	g_unsupported_api ("OpenProcess");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "OpenProcess");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}
#endif /* HAVE_API_SUPPORT_WIN32_OPEN_PROCESS */

#if HAVE_API_SUPPORT_WIN32_SHELL_EXECUTE_EX
#include <shellapi.h>
MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info, MonoError *error)
{
	MonoCreateProcessCoop coop;
	mono_createprocess_coop_init (&coop, proc_start_info, process_info);

	SHELLEXECUTEINFOW shellex = {0};
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
	ret = ShellExecuteExW (&shellex);
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
#elif !HAVE_EXTERN_DEFINED_WIN32_SHELL_EXECUTE_EX
MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info, MonoError *error)
{
	g_unsupported_api ("ShellExecuteEx");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "ShellExecuteEx");
	process_info->pid = (guint32)(-ERROR_NOT_SUPPORTED);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_SHELL_EXECUTE_EX */

#if HAVE_API_SUPPORT_WIN32_CREATE_PROCESS_WITH_LOGON || HAVE_API_SUPPORT_WIN32_CREATE_PROCESS
static gboolean
mono_process_create_process (MonoCreateProcessCoop *coop, MonoW32ProcessInfo *mono_process_info,
	MonoStringHandle cmd, guint32 creation_flags, gunichar2 *env_vars, gunichar2 *dir,
	HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, PROCESS_INFORMATION *process_info, MonoError *error)
{
	gboolean result = FALSE;
	MonoGCHandle cmd_gchandle = NULL;
	gunichar2 *cmd_chars = MONO_HANDLE_IS_NULL (cmd) ? NULL : mono_string_handle_pin_chars (cmd, &cmd_gchandle);

	STARTUPINFOW start_info={0};
#if HAVE_API_SUPPORT_WIN32_CONSOLE
	start_info.cb = sizeof(STARTUPINFOW);
	start_info.dwFlags = STARTF_USESTDHANDLES;
	start_info.hStdInput = stdin_handle;
	start_info.hStdOutput = stdout_handle;
	start_info.hStdError = stderr_handle;
#else
	start_info.dwFlags = 0;
	start_info.hStdInput = INVALID_HANDLE_VALUE;
	start_info.hStdOutput = INVALID_HANDLE_VALUE;
	start_info.hStdError = INVALID_HANDLE_VALUE;
#endif /* HAVE_API_SUPPORT_WIN32_CONSOLE */

	MONO_ENTER_GC_SAFE;
	if (coop->username) {
#if HAVE_API_SUPPORT_WIN32_CREATE_PROCESS_WITH_LOGON
		guint32 logon_flags = mono_process_info->load_user_profile ? LOGON_WITH_PROFILE : 0;

		result = CreateProcessWithLogonW (coop->username,
						  coop->domain,
						  mono_process_info->password,
						  logon_flags,
						  NULL,
						  cmd_chars,
						  creation_flags,
						  env_vars, dir, &start_info, process_info);
#else
		memset (process_info, 0, sizeof (PROCESS_INFORMATION));
		g_unsupported_api ("CreateProcessWithLogon");
		mono_error_set_not_supported (error, G_UNSUPPORTED_API, "CreateProcessWithLogon");
		SetLastError (ERROR_NOT_SUPPORTED);
#endif /* HAVE_API_SUPPORT_WIN32_CREATE_PROCESS_WITH_LOGON */
	} else {
#if HAVE_API_SUPPORT_WIN32_CREATE_PROCESS
		result = CreateProcessW (NULL,
					cmd_chars,
					NULL,
					NULL,
					TRUE,
					creation_flags,
					env_vars,
					dir,
					&start_info,
					process_info);
#else
		memset (process_info, 0, sizeof (PROCESS_INFORMATION));
		g_unsupported_api ("CreateProcess");
		mono_error_set_not_supported (error, G_UNSUPPORTED_API, "CreateProcess");
		SetLastError (ERROR_NOT_SUPPORTED);
#endif
	}
	MONO_EXIT_GC_SAFE;

	mono_gchandle_free_internal (cmd_gchandle);

	return result;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_CREATE_PROCESS_WITH_LOGON && !HAVE_EXTERN_DEFINED_WIN32_CREATE_PROCESS
static gboolean
mono_process_create_process (MonoCreateProcessCoop *coop, MonoW32ProcessInfo *mono_process_info,
	MonoStringHandle cmd, guint32 creation_flags, gunichar2 *env_vars, gunichar2 *dir,
	HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, PROCESS_INFORMATION *process_info, MonoError *error)
{
	memset (process_info, 0, sizeof (PROCESS_INFORMATION));
	g_unsupported_api ("CreateProcessWithLogon, CreateProcess");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "CreateProcessWithLogon, CreateProcess");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_CREATE_PROCESS_WITH_LOGON || HAVE_API_SUPPORT_WIN32_CREATE_PROCESS */

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
static gchar*
process_complete_path (const gunichar2 *appname)
{
	// FIXME This function should stick to gunichar2.

	char *utf8app;
	char *utf8appmemory = NULL;
	char *result;

	utf8appmemory = g_utf16_to_utf8 (appname, -1, NULL, NULL, NULL);
	utf8app = process_unquote_application_name (utf8appmemory);

	result = process_quote_path (utf8app);

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

	spath = process_complete_path (proc_start_info->filename);

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
	PROCESS_INFORMATION procinfo;
	gunichar2 *env_vars = NULL;
	MonoStringHandle cmd = NULL_HANDLE_STRING;
	guint32 creation_flags;

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
			MonoGCHandle gchandle = NULL;
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

	ret = mono_process_create_process (&coop, process_info, cmd, creation_flags, env_vars, dir, stdin_handle, stdout_handle, stderr_handle, &procinfo, error);

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

#if HAVE_API_SUPPORT_WIN32_ENUM_PROCESSES
MonoArrayHandle
ves_icall_System_Diagnostics_Process_GetProcesses_internal (MonoError *error)
{
	MonoArrayHandle procs = NULL_HANDLE_ARRAY;
	DWORD needed = 0;
	DWORD *pids = NULL;
	int count = 512;
	gboolean success;

	do {
		pids = g_new0 (DWORD, count);

		MONO_ENTER_GC_SAFE;
		success = EnumProcesses (pids, count * sizeof (DWORD), &needed);
		MONO_EXIT_GC_SAFE;

		if (!success)
			goto exit;
		if (needed < (count * sizeof (guint32)))
			break;
		g_free (pids);
		pids = NULL;
		count = (count * 3) / 2;
	} while (TRUE);

	count = needed / sizeof (guint32);
	procs = mono_array_new_handle (mono_domain_get (), mono_get_int32_class (), count, error);
	if (!is_ok (error)) {
		procs = NULL_HANDLE_ARRAY;
		goto exit;
	}

	MONO_ENTER_NO_SAFEPOINTS;

	memcpy (mono_array_addr_internal (MONO_HANDLE_RAW (procs), guint32, 0), pids, needed);

	MONO_EXIT_NO_SAFEPOINTS;

exit:
	g_free (pids);
	return procs;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_ENUM_PROCESSES
MonoArrayHandle
ves_icall_System_Diagnostics_Process_GetProcesses_internal (MonoError *error)
{
	g_unsupported_api ("EnumProcesses");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "EnumProcesses");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL_HANDLE_ARRAY;
}
#endif /* HAVE_API_SUPPORT_WIN32_ENUM_PROCESSES */

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
	return GetExitCodeProcess (handle, (PDWORD)exitcode);
}

#if HAVE_API_SUPPORT_WIN32_GET_WORKING_SET_SIZE
MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max)
{
	return GetProcessWorkingSetSize (handle, (PSIZE_T)min, (PSIZE_T)max);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_WORKING_SET_SIZE
MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max)
{
	ERROR_DECL (error);
	g_unsupported_api ("GetProcessWorkingSetSize");
	mono_error_set_not_supported(error, G_UNSUPPORTED_API, "GetProcessWorkingSetSize");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_GET_WORKING_SET_SIZE */

#if HAVE_API_SUPPORT_WIN32_SET_WORKING_SET_SIZE
MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max)
{
	return SetProcessWorkingSetSize (handle, min, max);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SET_WORKING_SET_SIZE
MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max)
{
	ERROR_DECL (error);
	g_unsupported_api ("SetProcessWorkingSetSize");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "SetProcessWorkingSetSize");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_SET_WORKING_SET_SIZE */

#if HAVE_API_SUPPORT_WIN32_GET_PRIORITY_CLASS
gint32
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle)
{
	return GetPriorityClass (handle);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_PRIORITY_CLASS
gint32
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle)
{
	ERROR_DECL (error);
	g_unsupported_api ("GetPriorityClass");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetPriorityClass");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_GET_PRIORITY_CLASS */

#if HAVE_API_SUPPORT_WIN32_SET_PRIORITY_CLASS
MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass)
{
	return SetPriorityClass (handle, (guint32) priorityClass);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SET_PRIORITY_CLASS
MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass)
{
	ERROR_DECL (error);
	g_unsupported_api ("SetPriorityClass");
	mono_error_set_not_supported(error, G_UNSUPPORTED_API, "SetPriorityClass");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_SET_PRIORITY_CLASS */

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
