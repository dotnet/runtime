// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MSCoree.cpp
//*****************************************************************************
#include "stdafx.h"                     // Standard header.

#include <utilcode.h>                   // Utility helpers.
#include <corpriv.h>

#if !defined(CORECLR_EMBEDDED)

BOOL STDMETHODCALLTYPE EEDllMain( // TRUE on success, FALSE on error.
                       HINSTANCE    hInst,                  // Instance handle of the loaded module.
                       DWORD        dwReason,               // Reason for loading.
                       LPVOID       lpReserved);            // Unused.

//*****************************************************************************
// Handle lifetime of loaded library.
//*****************************************************************************

#ifdef TARGET_WINDOWS
extern "C" BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved);
#endif // TARGET_WINDOWS

extern "C"
#ifdef TARGET_UNIX
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI DllMain(HANDLE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    STATIC_CONTRACT_NOTHROW;

    return EEDllMain((HINSTANCE)hInstance, dwReason, lpReserved);
}

#endif // !defined(CORECLR_EMBEDDED)

// ---------------------------------------------------------------------------
// %%Function: MetaDataGetDispenser
// This function gets the Dispenser interface given the CLSID and REFIID.
// Exported from coreclr and used by external profilers.
// ---------------------------------------------------------------------------
STDAPI DLLEXPORT MetaDataGetDispenser(  // Return HRESULT
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv)                   // Return interface pointer here.
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;

    if (rclsid != CLSID_CorMetaDataDispenser)
        return CLASS_E_CLASSNOTAVAILABLE;

    return CreateMetaDataDispenser(riid, ppv);
}
