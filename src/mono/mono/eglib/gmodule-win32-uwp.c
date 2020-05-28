/*
 * gmodule-win32-uwp.c: UWP gmodule support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include <gmodule-win32-internals.h>

gpointer
w32_find_symbol (const gchar *symbol_name)
{
	g_unsupported_api ("EnumProcessModules");
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}

const gchar *
g_module_error (void)
{
	gchar *ret = NULL;
	TCHAR buf [1024];
	DWORD code = GetLastError ();

	if (!FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), buf, G_N_ELEMENTS (buf) - 1, NULL) )
		buf[0] = TEXT('\0');

	ret = u16to8 (buf);
	return ret;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_gmodule_win32_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
