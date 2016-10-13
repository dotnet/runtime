/*
 * coree-windows-uwp.c: UWP coree support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <Windows.h>
#include "mono/metadata/coree-internals.h"

BOOL STDMETHODCALLTYPE
_CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
	g_unsupported_api ("_CorDllMain");
	return FALSE;
}

__int32 STDMETHODCALLTYPE
_CorExeMain(void)
{
	g_unsupported_api ("_CorExeMain");
	ExitProcess (EXIT_FAILURE);
}

STDAPI
_CorValidateImage(PVOID *ImageBase, LPCWSTR FileName)
{
	g_unsupported_api ("_CorValidateImage");
	return E_UNEXPECTED;
}

HMODULE WINAPI
MonoLoadImage(LPCWSTR FileName)
{
	g_unsupported_api ("MonoLoadImage");
	return NULL;
}

void
mono_coree_set_act_ctx (const char *file_name)
{
	g_unsupported_api ("CreateActCtx, ActivateActCtx");
	SetLastError (ERROR_NOT_SUPPORTED);

	return;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_coree_windows_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
