/*
 * gunicode-win32.c: Windows unicode support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#define CODESET 1
#include <windows.h>
#include "../utils/w32subset.h"

#if HAVE_API_SUPPORT_WIN32_GET_ACP || HAVE_API_SUPPORT_WIN32_GET_CP_INFO_EX
extern const char *eg_my_charset;
static gboolean is_utf8;

gboolean
g_get_charset (G_CONST_RETURN char **charset)
{
	if (eg_my_charset == NULL) {
		static char buf [14];
#if HAVE_API_SUPPORT_WIN32_GET_CP_INFO_EX
		CPINFOEXW cp_info;
		GetCPInfoExW (CP_ACP, 0, &cp_info);
		sprintf (buf, "CP%u", cp_info.CodePage);
#elif HAVE_API_SUPPORT_WIN32_GET_ACP
		sprintf (buf, "CP%u", GetACP ());
#endif
		eg_my_charset = buf;
		is_utf8 = FALSE;
	}
	
	if (charset != NULL)
		*charset = eg_my_charset;

	return is_utf8;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_ACP && !HAVE_EXTERN_DEFINED_WIN32_GET_CP_INFO_EX
gboolean
g_get_charset (G_CONST_RETURN char **charset)
{
	g_unsupported_api ("GetACP, GetCPInfoEx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#else
#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_mono_gunicode_win32_quiet_lnk4221(void) {}
#endif
#endif /* HAVE_API_SUPPORT_WIN32_GET_ACP || HAVE_API_SUPPORT_WIN32_GET_CP_INFO_EX */
