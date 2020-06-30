// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "emit.h"
#include "corexcep.h"

#ifndef DLLEXPORT
#define DLLEXPORT
#endif // !DLLEXPORT

extern void jitShutdown(bool processIsTerminating);

HINSTANCE g_hInst = nullptr;

/*****************************************************************************/
extern "C" DLLEXPORT BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID pvReserved)
{
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        g_hInst = (HINSTANCE)hInstance;
        DisableThreadLibraryCalls((HINSTANCE)hInstance);
    }
    else if (dwReason == DLL_PROCESS_DETACH)
    {
        // From MSDN: If fdwReason is DLL_PROCESS_DETACH, lpvReserved is NULL if FreeLibrary has
        // been called or the DLL load failed and non-NULL if the process is terminating.
        bool processIsTerminating = (pvReserved != nullptr);
        jitShutdown(processIsTerminating);
    }

    return TRUE;
}

HINSTANCE GetModuleInst()
{
    return (g_hInst);
}
