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
#include <windows.h>
#include <psapi.h>
#include <gmodule-win32-internals.h>

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

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
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
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

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

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
const gchar *
g_module_error (void)
{
	gchar* ret = NULL;
	TCHAR* buf = NULL;
	DWORD code = GetLastError ();

	/* FIXME: buf must not be NULL! */
	FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER, NULL, 
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), buf, 0, NULL);

	ret = u16to8 (buf);
	LocalFree(buf);

	return ret;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

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

gchar *
g_module_build_path (const gchar *directory, const gchar *module_name)
{
	char *lib_prefix = "";
	
	if (module_name == NULL)
		return NULL;

	if (strncmp (module_name, "lib", 3) != 0)
		lib_prefix = LIBPREFIX;
	
	if (directory && *directory){ 
		
		return g_strdup_printf ("%s/%s%s" LIBSUFFIX, directory, lib_prefix, module_name);
	}
	return g_strdup_printf ("%s%s" LIBSUFFIX, lib_prefix, module_name); 
}
