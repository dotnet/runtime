/**
 * \file
 * Windows icall support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <winsock2.h>
#include <windows.h>
#include <mono/metadata/icall-internals.h>
#include <mono/utils/w32subset.h>

void
mono_icall_make_platform_path (gchar *path)
{
	for (size_t i = strlen (path); i > 0; i--)
		if (path [i-1] == '\\')
			path [i-1] = '/';
}

const gchar *
mono_icall_get_file_path_prefix (const gchar *path)
{
	if (*path == '/' && *(path + 1) == '/') {
		return "file:";
	} else {
		return "file:///";
	}
}

gpointer
mono_icall_module_get_hinstance (MonoImage *image)
{
	if (image && m_image_is_module_handle (image))
		return image->raw_data;

	return (gpointer) (-1);
}

#if HAVE_API_SUPPORT_WIN32_GET_COMPUTER_NAME
MonoStringHandle
mono_icall_get_machine_name (MonoError *error)
{
	gunichar2 buf [MAX_COMPUTERNAME_LENGTH + 1];
	DWORD len = G_N_ELEMENTS (buf);

	if (GetComputerNameW (buf, &len))
		return mono_string_new_utf16_handle (buf, len, error);
	return MONO_HANDLE_NEW (MonoString, NULL);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_COMPUTER_NAME
MonoStringHandle
mono_icall_get_machine_name (MonoError *error)
{
	g_unsupported_api ("GetComputerName");
	return mono_string_new_handle ("mono", error);
}
#endif

int
mono_icall_get_platform (void)
{
	/* Win32NT */
	return 2;
}

MonoStringHandle
mono_icall_get_new_line (MonoError *error)
{
	return mono_string_new_handle ("\r\n", error);
}

MonoBoolean
mono_icall_is_64bit_os (void)
{
#if SIZEOF_VOID_P == 8
	return TRUE;
#else
	gboolean isWow64Process = FALSE;
	if (IsWow64Process (GetCurrentProcess (), &isWow64Process)) {
		return (MonoBoolean)isWow64Process;
	}
	return FALSE;
#endif
}

MonoArrayHandle
mono_icall_get_environment_variable_names (MonoError *error)
{
	MonoArrayHandle names;
	MonoStringHandle str;
	WCHAR* env_strings;
	WCHAR* env_string;
	WCHAR* equal_str;
	int n = 0;

	env_strings = GetEnvironmentStrings();

	if (env_strings) {
		env_string = env_strings;
		while (*env_string != '\0') {
		/* weird case that MS seems to skip */
			if (*env_string != '=')
				n++;
			while (*env_string != '\0')
				env_string++;
			env_string++;
		}
	}

	names = mono_array_new_handle (mono_defaults.string_class, n, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	if (env_strings) {
		n = 0;
		str = MONO_HANDLE_NEW (MonoString, NULL);
		env_string = env_strings;
		while (*env_string != '\0') {
			/* weird case that MS seems to skip */
			if (*env_string != '=') {
				equal_str = wcschr(env_string, '=');
				g_assert(equal_str);
				MonoString *s = mono_string_new_utf16_checked (env_string, (gint32)(equal_str - env_string), error);
				goto_if_nok (error, cleanup);
				MONO_HANDLE_ASSIGN_RAW (str, s);

				mono_array_handle_setref (names, n, str);
				n++;
			}
			while (*env_string != '\0')
				env_string++;
			env_string++;
		}

	}

cleanup:
	if (env_strings)
		FreeEnvironmentStrings (env_strings);
	if (!is_ok (error))
		return NULL_HANDLE_ARRAY;
	return names;
}

#if HAVE_API_SUPPORT_WIN32_SH_GET_FOLDER_PATH
#include <shlobj.h>
MonoStringHandle
mono_icall_get_windows_folder_path (int folder, MonoError *error)
{
	error_init (error);
	#ifndef CSIDL_FLAG_CREATE
		#define CSIDL_FLAG_CREATE	0x8000
	#endif

	WCHAR path [MAX_PATH];
	/* Create directory if no existing */
	if (SUCCEEDED (SHGetFolderPathW (NULL, folder | CSIDL_FLAG_CREATE, NULL, 0, path))) {
		int len = 0;
		while (path [len])
			++ len;
		return mono_string_new_utf16_handle (path, len, error);
	}
	return mono_string_new_handle ("", error);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SH_GET_FOLDER_PATH
MonoStringHandle
mono_icall_get_windows_folder_path (int folder, MonoError *error)
{
	error_init (error);
	g_unsupported_api ("SHGetFolderPath");
	return mono_string_new_handle ("", error);
}
#endif

#if HAVE_API_SUPPORT_WIN32_SEND_MESSAGE_TIMEOUT
ICALL_EXPORT void
ves_icall_System_Environment_BroadcastSettingChange (MonoError *error)
{
	SendMessageTimeoutW (HWND_BROADCAST, WM_SETTINGCHANGE, (WPARAM)NULL, (LPARAM)L"Environment", SMTO_ABORTIFHUNG, 2000, 0);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SEND_MESSAGE_TIMEOUT
ICALL_EXPORT void
ves_icall_System_Environment_BroadcastSettingChange (MonoError *error)
{
	g_unsupported_api ("SendMessageTimeout");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "SendMessageTimeout");
	SetLastError (ERROR_NOT_SUPPORTED);
}
#endif

#if HAVE_API_SUPPORT_WIN32_WAIT_FOR_INPUT_IDLE
gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds)
{
	return WaitForInputIdle (handle, milliseconds);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_WAIT_FOR_INPUT_IDLE
gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds)
{
	ERROR_DECL (error);
	g_unsupported_api ("WaitForInputIdle");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "WaitForInputIdle");
	mono_error_set_pending_exception (error);
	return WAIT_TIMEOUT;
}
#endif

void
mono_icall_write_windows_debug_string (const gunichar2 *message)
{
	OutputDebugString (message);
}

#endif /* HOST_WIN32 */
