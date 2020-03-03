/*
 * gmisc-win32-uwp.c: UWP misc support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include <assert.h>

gchar*
g_win32_getlocale(void)
{
	gunichar2 buf[19];
	gint ccBuf = GetLocaleInfoEx (LOCALE_NAME_USER_DEFAULT, LOCALE_SISO639LANGNAME, buf, 9);
	assert (ccBuf <= 9);
	if (ccBuf != 0) {
		buf[ccBuf - 1] = L'-';
		ccBuf = GetLocaleInfoEx (LOCALE_NAME_USER_DEFAULT, LOCALE_SISO3166CTRYNAME, buf + ccBuf, 9);
		assert (ccBuf <= 9);
	}

	// Check for GetLocaleInfoEx failure.
	if (ccBuf == 0)
		buf[0] = L'\0';

	return u16to8 (buf);
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_gmisc_win32_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
