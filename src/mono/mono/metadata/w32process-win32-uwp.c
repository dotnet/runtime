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

// FIXME: In order to share function declarations at least, this
// file should be merged with its non-uwp counterpart, and fine-grained #if used there.

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)

#include <windows.h>
#include <mono/metadata/object-internals.h>
#include "mono/metadata/w32process.h"
#include "mono/metadata/w32process-internals.h"
#include "icall-decl.h"

MonoArrayHandle
ves_icall_System_Diagnostics_Process_GetProcesses_internal (MonoError *error)
{
	g_unsupported_api ("EnumProcesses");
	mono_error_set_not_supported (error, "This system does not support EnumProcesses");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL_HANDLE_ARRAY;
}

HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid, MonoError *error)
{
	g_unsupported_api ("OpenProcess");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "OpenProcess");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}

void
ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObjectHandle this_obj,
		const gunichar2 *filename, int filename_length, MonoError *error)
{
	g_unsupported_api ("GetFileVersionInfoSize, GetFileVersionInfo, VerQueryValue, VerLanguageName");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetFileVersionInfoSize, GetFileVersionInfo, VerQueryValue, VerLanguageName");
	SetLastError (ERROR_NOT_SUPPORTED);
}

MonoArrayHandle
ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObjectHandle this_obj, HANDLE process, MonoError *error)
{
	g_unsupported_api ("EnumProcessModules");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "EnumProcessModules");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL_HANDLE_ARRAY;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info, MonoError *error)
{
	g_unsupported_api ("ShellExecuteEx");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "ShellExecuteEx");
	process_info->pid = (guint32)(-ERROR_NOT_SUPPORTED);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}

// This is the only function in this file that does anything.
// Note that process is ignored and it just operates on the current process.
MonoStringHandle
ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process, MonoError *error)
{
	gunichar2 *name = NULL;
	guint32 len = 0;
	// FIXME give allocator to mono_get_module_file_name to avoid copies, here and many other
	if (!mono_get_module_file_name (NULL, &name, &len))
		return NULL_HANDLE_STRING;
	MonoStringHandle res = mono_string_new_utf16_handle (mono_domain_get (), name, len, error);
	g_free (name);
	return res;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoW32ProcessStartInfoHandle proc_start_info,
	HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoW32ProcessInfo *process_info, MonoError *error)
{
	// FIXME CreateProcess is supported for UWP. Use finer grained #if.

	const char *api_name = mono_process_info->username ? "CreateProcessWithLogonW" : "CreateProcess";
	memset (process_info, 0, sizeof (*process_info));
	g_unsupported_api (api_name);
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, api_name);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}

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

gint32
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle)
{
	// FIXME GetPriorityClass is supported for UWP. Use finer grained #if.

	ERROR_DECL (error);
	g_unsupported_api ("GetPriorityClass");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetPriorityClass");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass)
{
	// FIXME SetPriorityClass is supported for UWP. Use finer grained #if.

	ERROR_DECL (error);
	g_unsupported_api ("SetPriorityClass");
	mono_error_set_not_supported(error, G_UNSUPPORTED_API, "SetPriorityClass");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (process_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
