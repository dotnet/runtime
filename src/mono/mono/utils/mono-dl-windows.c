/**
 * \file
 * Interface to the dynamic linker
 *
 * Author:
 *    Mono Team (http://www.mono-project.com)
 *
 * Copyright 2001-2004 Ximian, Inc.
 * Copyright 2004-2009 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#if defined(HOST_WIN32)

#include <mono/utils/mono-dl.h>
#include <mono/utils/mono-dl-windows-internals.h>
#include <mono/utils/mono-embed.h>
#include <mono/utils/mono-path.h>

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

#include <windows.h>
#include <psapi.h>

#include <mono/utils/w32subset.h>

const char*
mono_dl_get_so_prefix (void)
{
	return "";
}

const char**
mono_dl_get_so_suffixes (void)
{
	static const char *suffixes[] = {
		".dll",
		"",
	};
	return suffixes;
}

void*
mono_dl_open_file (const char *file, int flags)
{
	gpointer hModule = NULL;
	if (file) {
		gunichar2* file_utf16 = g_utf8_to_utf16 (file, strlen (file), NULL, NULL, NULL);

#if HAVE_API_SUPPORT_WIN32_SET_ERROR_MODE
		guint last_sem = SetErrorMode (SEM_FAILCRITICALERRORS);
#endif
		guint32 last_error = 0;

#if HAVE_API_SUPPORT_WIN32_LOAD_LIBRARY
		hModule = LoadLibraryExW (file_utf16, NULL, flags);
#elif HAVE_API_SUPPORT_WIN32_LOAD_PACKAGED_LIBRARY
		hModule = LoadPackagedLibrary (file_utf16, NULL);
#else
#error unknown Windows variant
#endif
		if (!hModule)
			last_error = GetLastError ();

#if HAVE_API_SUPPORT_WIN32_SET_ERROR_MODE
		SetErrorMode (last_sem);
#endif

		g_free (file_utf16);

		if (!hModule)
			SetLastError (last_error);
	} else {
#if HAVE_API_SUPPORT_WIN32_GET_MODULE_HANDLE
		hModule = GetModuleHandleW (NULL);
#else
#error unknown Windows variant
#endif
	}
	return hModule;
}

void
mono_dl_close_handle (MonoDl *module)
{
	if (!module->main_module)
		FreeLibrary ((HMODULE)module->handle);
}

#if HAVE_API_SUPPORT_WIN32_ENUM_PROCESS_MODULES
void*
mono_dl_lookup_symbol_in_process (const char *symbol_name)
{
	HMODULE *modules;
	DWORD buffer_size = sizeof (HMODULE) * 1024;
	DWORD needed, i;
	gpointer proc = NULL;

	/* get the symbol from the loaded DLLs */
	modules = (HMODULE *) g_malloc (buffer_size);
	if (modules == NULL)
		return NULL;

	if (!EnumProcessModules (GetCurrentProcess (), modules,
				 buffer_size, &needed)) {
		g_free (modules);
		return NULL;
	}

	/* check whether the supplied buffer was too small, realloc, retry */
	if (needed > buffer_size) {
		g_free (modules);

		buffer_size = needed;
		modules = (HMODULE *) g_malloc (buffer_size);

		if (modules == NULL)
			return NULL;

		if (!EnumProcessModules (GetCurrentProcess (), modules,
					 buffer_size, &needed)) {
			g_free (modules);
			return NULL;
		}
	}

	for (i = 0; i < needed / sizeof (HANDLE); i++) {
		proc = (gpointer)GetProcAddress (modules [i], symbol_name);
		if (proc != NULL) {
			g_free (modules);
			return proc;
		}
	}

	g_free (modules);
	return NULL;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_ENUM_PROCESS_MODULES
void*
mono_dl_lookup_symbol_in_process (const char *symbol_name)
{
	g_unsupported_api ("EnumProcessModules");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}
#endif /* HAVE_API_SUPPORT_WIN32_ENUM_PROCESS_MODULES */

void*
mono_dl_lookup_symbol (MonoDl *module, const char *symbol_name)
{
	gpointer proc = NULL;

	/* get the symbol directly from the specified module */
	if (!module->main_module)
		return (void*)GetProcAddress ((HMODULE)module->handle, symbol_name);

	/* get the symbol from the main module */
	proc = (gpointer)GetProcAddress ((HMODULE)module->handle, symbol_name);
	if (proc != NULL)
		return proc;

	/* get the symbol from the loaded DLLs */
	return mono_dl_lookup_symbol_in_process (symbol_name);
}

int
mono_dl_convert_flags (int mono_flags, int native_flags)
{
	// Mono flags are not applicable on Windows
	return native_flags;
}

#if HAVE_API_SUPPORT_WIN32_FORMAT_MESSAGE
char*
mono_dl_current_error_string (void)
{
	char* ret = NULL;
	DWORD code = GetLastError ();
#if HAVE_API_SUPPORT_WIN32_LOCAL_ALLOC_FREE
	PWSTR buf = NULL;
	if (FormatMessageW (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (PWSTR)&buf, 0, NULL)) {
		ret = u16to8 (buf);
		LocalFree (buf);
	}
#else
	WCHAR local_buf [1024];
	if (!FormatMessageW (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), local_buf, G_N_ELEMENTS (local_buf) - 1, NULL) )
		local_buf [0] = TEXT('\0');

	ret = u16to8 (local_buf)
#endif
	return ret;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_FORMAT_MESSAGE
char *
mono_dl_current_error_string (void)
{
	return g_strdup_printf ("GetLastError=%d. FormatMessage not supported.", GetLastError ());
}
#endif /* HAVE_API_SUPPORT_WIN32_FORMAT_MESSAGE */

int
mono_dl_get_executable_path (char *buf, int buflen)
{
	return -1; //TODO
}

const char*
mono_dl_get_system_dir (void)
{
	return NULL;
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_dl_windows);

#endif
