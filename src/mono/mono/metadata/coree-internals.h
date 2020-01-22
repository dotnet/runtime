/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_COREE_INTERNALS_H__
#define __MONO_COREE_INTERNALS_H__

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include <windows.h>

BOOL STDMETHODCALLTYPE
_CorDllMain (HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved);

__int32 STDMETHODCALLTYPE
_CorExeMain (void);

void STDMETHODCALLTYPE
CorExitProcess (int exitCode);

STDAPI
_CorValidateImage (PVOID *ImageBase, LPCWSTR FileName);

STDAPI_(VOID)
_CorImageUnloading (PVOID ImageBase);

STDAPI
CorBindToRuntimeEx (LPCWSTR pwszVersion, LPCWSTR pwszBuildFlavor,
		    DWORD startupFlags, REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv);

STDAPI
CorBindToRuntime (LPCWSTR pwszVersion, LPCWSTR pwszBuildFlavor,
		  REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv);

HMODULE WINAPI
MonoLoadImage (LPCWSTR FileName);

void mono_coree_set_act_ctx (const char *file_name);
#endif /* HOST_WIN32 */

#endif /* __MONO_COREE_INTERNALS_H__ */

