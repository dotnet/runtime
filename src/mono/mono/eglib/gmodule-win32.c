/*
 * gmodule.c: dl* functions, glib style
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *   Jonathan Chambers (joncham@gmail.com)
 *   Robert Jordan (robertj@gmx.net)
 *
 * (C) 2006 Novell, Inc.
 * (C) 2006 Jonathan Chambers
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include <config.h>
#include <glib.h>
#ifndef PSAPI_VERSION
#define PSAPI_VERSION 2 // Use the Windows 7 or newer version more directly.
#endif
#include <windows.h>
#include <psapi.h>
#include <gmodule.h>
#include "../utils/w32subset.h"

#define LIBSUFFIX ".dll"
#define LIBPREFIX ""

struct _GModule {
	HMODULE handle;
	int main_module;
};

GModule *
g_module_open (const gchar *file, GModuleFlags flags)
{
	GModule *module;
	module = g_malloc (sizeof (GModule));
	if (module == NULL)
		return NULL;

	if (file != NULL) {
		gunichar2 *file16;
		file16 = u8to16(file);
		module->main_module = FALSE;
		module->handle = LoadLibraryW (file16);
		g_free(file16);
		if (!module->handle) {
			g_free (module);
			return NULL;
		}

	} else {
		module->main_module = TRUE;
		module->handle = GetModuleHandle (NULL);
	}

	return module;
}

#if HAVE_API_SUPPORT_WIN32_ENUM_PROCESS_MODULES
gpointer
w32_find_symbol (const gchar *symbol_name)
{
	HMODULE *modules;
	DWORD buffer_size = sizeof (HMODULE) * 1024;
	DWORD needed, i;

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
		gpointer proc = (gpointer)(intptr_t)GetProcAddress (modules [i], symbol_name);
		if (proc != NULL) {
			g_free (modules);
			return proc;
		}
	}

	g_free (modules);
	return NULL;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_ENUM_PROCESS_MODULES
gpointer
w32_find_symbol (const gchar *symbol_name)
{
	g_unsupported_api ("EnumProcessModules");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}
#else
extern gpointer w32_find_symbol (const gchar *symbol_name);
#endif /* HAVE_API_SUPPORT_WIN32_ENUM_PROCESS_MODULES */

gboolean
g_module_symbol (GModule *module, const gchar *symbol_name, gpointer *symbol)
{
	if (module == NULL || symbol_name == NULL || symbol == NULL)
		return FALSE;

	if (module->main_module) {
		*symbol = (gpointer)(intptr_t)GetProcAddress (module->handle, symbol_name);
		if (*symbol != NULL)
			return TRUE;

		*symbol = w32_find_symbol (symbol_name);
		return *symbol != NULL;
	} else {
		*symbol = (gpointer)(intptr_t)GetProcAddress (module->handle, symbol_name);
		return *symbol != NULL;
	}
}

#if HAVE_API_SUPPORT_WIN32_GET_MODULE_HANDLE_EX
gboolean
g_module_address (void *addr, char *file_name, size_t file_name_len,
                  void **file_base, char *sym_name, size_t sym_name_len,
                  void **sym_addr)
{
	HMODULE module;
	/*
	 * We have to cast the address because usually this func works with strings,
	 * this being an exception.
	 */
	BOOL ret = GetModuleHandleExW (GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCWSTR)addr, &module);
	if (!ret)
		return FALSE;

	if (file_name != NULL && file_name_len >= 1) {
		/* sigh, non-const. AIX for POSIX is the same way. */
		WCHAR fname [MAX_PATH];
		DWORD bytes = GetModuleFileNameW (module, fname, G_N_ELEMENTS (fname));
		if (bytes) {
			/* Convert back to UTF-8 from wide for runtime */
			GFixedBufferCustomAllocatorData custom_alloc_data;
			custom_alloc_data.buffer = file_name;
			custom_alloc_data.buffer_size = file_name_len;
			custom_alloc_data.req_buffer_size = 0;
			if (!g_utf16_to_utf8_custom_alloc (fname, -1, NULL, NULL, g_fixed_buffer_custom_allocator, &custom_alloc_data, NULL))
				*file_name = '\0';
		} else {
			*file_name = '\0';
		}
	}
	/* XXX: implement the rest */
	if (file_base != NULL)
		*file_base = NULL;
	if (sym_name != NULL && sym_name_len >= 1)
		sym_name[0] = '\0';
	if (sym_addr != NULL)
		*sym_addr = NULL;

	/* -1 reference count to avoid leaks; Ex variant does +1 refcount */
	FreeLibrary (module);
	return TRUE;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_MODULE_HANDLE_EX
gboolean
g_module_address (void *addr, char *file_name, size_t file_name_len,
	void **file_base, char *sym_name, size_t sym_name_len,
	void **sym_addr)
{
	g_unsupported_api ("GetModuleHandleEx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_GET_MODULE_HANDLE_EX */

#if HAVE_API_SUPPORT_WIN32_FORMAT_MESSAGE
const gchar *
g_module_error (void)
{
	gchar* ret = NULL;
	DWORD code = GetLastError ();
#if HAVE_API_SUPPORT_WIN32_LOCAL_ALLOC_FREE
	PWSTR buf = NULL;
	if (FormatMessageW (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER, NULL, code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (PWSTR)&buf, 0, NULL)) {
		ret = u16to8 (buf);
		LocalFree (buf);
	}
#else
	WCHAR local_buf [1024];
	if (!FormatMessageW (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), local_buf, STRING_LENGTH (local_buf), NULL) )
		local_buf [0] = TEXT('\0');

	ret = u16to8 (local_buf);
#endif
	return ret;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_FORMAT_MESSAGE
const gchar *
g_module_error (void)
{
	return g_strdup_printf ("GetLastError=%d. FormatMessage not supported.", GetLastError ());
}
#endif /* HAVE_API_SUPPORT_WIN32_FORMAT_MESSAGE */

gboolean
g_module_close (GModule *module)
{
	HMODULE handle;
	int main_module;

	if (module == NULL || module->handle == NULL)
		return FALSE;

	handle = module->handle;
	main_module = module->main_module;
	module->handle = NULL;
	g_free (module);
	return (main_module ? 1 : (0 == FreeLibrary (handle)));
}
