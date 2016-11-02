/*
 * icall-windows.c: Windows icall support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <winsock2.h>
#include <windows.h>
#include "mono/metadata/icall-windows-internals.h"

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#include <shlobj.h>
#endif

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
mono_icall_module_get_hinstance (MonoReflectionModule *module)
{
	if (module->image && module->image->is_module_handle)
		return module->image->raw_data;

	return (gpointer) (-1);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
MonoString *
mono_icall_get_machine_name (void)
{
	gunichar2 *buf;
	guint32 len;
	MonoString *result;

	len = MAX_COMPUTERNAME_LENGTH + 1;
	buf = g_new (gunichar2, len);

	result = NULL;
	if (GetComputerName (buf, (PDWORD) &len)) {
		MonoError error;
		result = mono_string_new_utf16_checked (mono_domain_get (), buf, len, &error);
		mono_error_set_pending_exception (&error);
	}

	g_free (buf);
	return result;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

int
mono_icall_get_platform (void)
{
	/* Win32NT */
	return 2;
}

MonoString *
mono_icall_get_new_line (void)
{
	return mono_string_new (mono_domain_get (), "\r\n");
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

MonoArray *
mono_icall_get_environment_variable_names (void)
{
	MonoError error;
	MonoArray *names;
	MonoDomain *domain;
	MonoString *str;
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

	domain = mono_domain_get ();
	names = mono_array_new_checked (domain, mono_defaults.string_class, n, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	if (env_strings) {
		n = 0;
		env_string = env_strings;
		while (*env_string != '\0') {
			/* weird case that MS seems to skip */
			if (*env_string != '=') {
				equal_str = wcschr(env_string, '=');
				g_assert(equal_str);
				str = mono_string_new_utf16_checked (domain, env_string, (gint32)(equal_str - env_string), &error);
				if (mono_error_set_pending_exception (&error))
					return NULL;

				mono_array_setref (names, n, str);
				n++;
			}
			while (*env_string != '\0')
				env_string++;
			env_string++;
		}

		FreeEnvironmentStrings (env_strings);
	}

	return names;
}

void
mono_icall_set_environment_variable (MonoString *name, MonoString *value)
{
	gunichar2 *utf16_name, *utf16_value;

	utf16_name = mono_string_to_utf16 (name);
	if ((value == NULL) || (mono_string_length (value) == 0) || (mono_string_chars (value)[0] == 0)) {
		SetEnvironmentVariable (utf16_name, NULL);
		g_free (utf16_name);
		return;
	}

	utf16_value = mono_string_to_utf16 (value);

	SetEnvironmentVariable (utf16_name, utf16_value);

	g_free (utf16_name);
	g_free (utf16_value);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
MonoString *
mono_icall_get_windows_folder_path (int folder)
{
	#ifndef CSIDL_FLAG_CREATE
		#define CSIDL_FLAG_CREATE	0x8000
	#endif

	WCHAR path [MAX_PATH];
	/* Create directory if no existing */
	if (SUCCEEDED (SHGetFolderPathW (NULL, folder | CSIDL_FLAG_CREATE, NULL, 0, path))) {
		int len = 0;
		while (path [len])
			++ len;
		MonoError error;
		MonoString *res = mono_string_new_utf16_checked (mono_domain_get (), path, len, &error);
		mono_error_set_pending_exception (&error);
		return res;
	}
	return mono_string_new (mono_domain_get (), "");
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
void
mono_icall_broadcast_setting_change (void)
{
	SendMessageTimeout (HWND_BROADCAST, WM_SETTINGCHANGE, (WPARAM)NULL, (LPARAM)L"Environment", SMTO_ABORTIFHUNG, 2000, 0);
}

gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds)
{
	return WaitForInputIdle (handle, milliseconds);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

void
mono_icall_write_windows_debug_string (MonoString *message)
{
	OutputDebugString (mono_string_chars (message));
}

#endif /* HOST_WIN32 */
