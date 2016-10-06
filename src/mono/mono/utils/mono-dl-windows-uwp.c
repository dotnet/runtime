/*
 * mono-dl-windows-uwp.c: UWP dl support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <Windows.h>
#include "mono/utils/mono-dl-windows.h"

void*
mono_dl_lookup_symbol_in_process (const char *symbol_name)
{
	g_unsupported_api ("EnumProcessModules");
	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

char*
mono_dl_current_error_string (void)
{
	char *ret = NULL;
	TCHAR buf [1024];
	DWORD code = GetLastError ();

	if (!FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
		code, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), buf, G_N_ELEMENTS(buf) - 1, NULL))
		buf[0] = TEXT('\0');

	ret = u16to8 (buf);
	return ret;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_mono_dl_windows_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
