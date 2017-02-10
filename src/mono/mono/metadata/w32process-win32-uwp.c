/**
 * \file
 * UWP process support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include <mono/metadata/object-internals.h>
#include "mono/metadata/w32process.h"
#include "mono/metadata/w32process-internals.h"
#include "mono/metadata/w32process-win32-internals.h"

gboolean
mono_process_win_enum_processes (DWORD *pids, DWORD count, DWORD *needed)
{
	g_unsupported_api ("EnumProcesses");
	*needed = 0;
	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("OpenProcess");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "OpenProcess");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

void
mono_w32process_get_fileversion (MonoObject *filever, gunichar2 *filename, MonoError *error)
{
	g_unsupported_api ("GetFileVersionInfoSize, GetFileVersionInfo, VerQueryValue, VerLanguageName");

	error_init (error);
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetFileVersionInfoSize, GetFileVersionInfo, VerQueryValue, VerLanguageName");

	SetLastError (ERROR_NOT_SUPPORTED);
}

MonoObject*
process_add_module (HANDLE process, HMODULE mod, gunichar2 *filename, gunichar2 *modulename, MonoClass *proc_class, MonoError *error)
{
	g_unsupported_api ("GetModuleInformation");

	error_init (error);
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetModuleInformation");

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

MonoArray *
ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this_obj, HANDLE process)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("EnumProcessModules, GetModuleBaseName, GetModuleFileNameEx");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "EnumProcessModules, GetModuleBaseName, GetModuleFileNameEx");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfo *proc_start_info, MonoW32ProcessInfo *process_info)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("ShellExecuteEx");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "ShellExecuteEx");
	mono_error_set_pending_exception (&mono_error);

	process_info->pid = (guint32)(-ERROR_NOT_SUPPORTED);
	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoString *
ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process)
{
	MonoError error;
	MonoString *string;
	gunichar2 name[MAX_PATH];
	guint32 len;

	len = GetModuleFileName (NULL, name, G_N_ELEMENTS (name));
	if (len == 0)
		return NULL;

	string = mono_string_new_utf16_checked (mono_domain_get (), name, len, &error);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);

	return string;
}

void
mono_process_init_startup_info (HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, STARTUPINFO *startinfo)
{
	startinfo->cb = sizeof(STARTUPINFO);
	startinfo->dwFlags = 0;
	startinfo->hStdInput = INVALID_HANDLE_VALUE;
	startinfo->hStdOutput = INVALID_HANDLE_VALUE;
	startinfo->hStdError = INVALID_HANDLE_VALUE;
	return;
}

gboolean
mono_process_create_process (MonoW32ProcessInfo *mono_process_info, MonoString *cmd, guint32 creation_flags,
	gunichar2 *env_vars, gunichar2 *dir, STARTUPINFO *start_info, PROCESS_INFORMATION *process_info)
{
	MonoError	mono_error;
	gchar		*api_name = "";

	if (mono_process_info->username) {
		api_name = "CreateProcessWithLogonW";
	} else {
		api_name = "CreateProcess";
	}

	memset (&process_info, 0, sizeof (PROCESS_INFORMATION));
	g_unsupported_api (api_name);

	error_init (&mono_error);
	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, api_name);
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
mono_icall_get_process_working_set_size (gpointer handle, gsize *min, gsize *max)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetProcessWorkingSetSize");

	mono_error_set_not_supported(&mono_error, G_UNSUPPORTED_API, "GetProcessWorkingSetSize");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
mono_icall_set_process_working_set_size (gpointer handle, gsize min, gsize max)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("SetProcessWorkingSetSize");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "SetProcessWorkingSetSize");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gint32
mono_icall_get_priority_class (gpointer handle)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetPriorityClass");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetPriorityClass");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

MonoBoolean
mono_icall_set_priority_class (gpointer handle, gint32 priorityClass)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("SetPriorityClass");

	mono_error_set_not_supported(&mono_error, G_UNSUPPORTED_API, "SetPriorityClass");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (process_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
