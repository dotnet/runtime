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

#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-dl-windows-internals.h"
#include "mono/utils/mono-embed.h"
#include "mono/utils/mono-path.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

#include <windows.h>
#include <psapi.h>

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

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
		guint last_sem = SetErrorMode (SEM_FAILCRITICALERRORS);
#endif
		guint32 last_error = 0;

		hModule = LoadLibrary (file_utf16);
		if (!hModule)
			last_error = GetLastError ();

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
		SetErrorMode (last_sem);
#endif

		g_free (file_utf16);

		if (!hModule)
			SetLastError (last_error);
	} else {
		hModule = GetModuleHandle (NULL);
	}
	return hModule;
}

void
mono_dl_close_handle (MonoDl *module)
{
	if (!module->main_module)
		FreeLibrary (module->handle);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
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
		proc = GetProcAddress (modules [i], symbol_name);
		if (proc != NULL) {
			g_free (modules);
			return proc;
		}
	}

	g_free (modules);
	return NULL;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

void*
mono_dl_lookup_symbol (MonoDl *module, const char *symbol_name)
{
	gpointer proc = NULL;

	/* get the symbol directly from the specified module */
	if (!module->main_module)
		return GetProcAddress (module->handle, symbol_name);

	/* get the symbol from the main module */
	proc = GetProcAddress (module->handle, symbol_name);
	if (proc != NULL)
		return proc;

	/* get the symbol from the loaded DLLs */
	return mono_dl_lookup_symbol_in_process (symbol_name);
}

int
mono_dl_convert_flags (int flags)
{
	return 0;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
char*
mono_dl_current_error_string (void)
{
	char* ret = NULL;
	wchar_t* buf = NULL;
	DWORD code = GetLastError ();

	if (FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&buf, 0, NULL))
	{
		ret = g_utf16_to_utf8 (buf, wcslen(buf), NULL, NULL, NULL);
		LocalFree (buf);
	} else {
		g_assert_not_reached ();
	}
	return ret;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

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
#endif
