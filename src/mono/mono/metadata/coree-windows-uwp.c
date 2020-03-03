/**
 * \file
 * UWP coree support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
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

MONO_EMPTY_SOURCE_FILE (coree_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
