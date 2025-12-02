// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// WinWrap.cpp
//
//*****************************************************************************

#include "stdafx.h"                     // Precompiled header key.
#include "winwrap.h"                    // Header for macros and functions.
#include "utilcode.h"
#include "holder.h"

// The only purpose of this function is to make a local copy of lpCommandLine.
// Because windows implementation of CreateProcessW can actually change lpCommandLine,
// but we'd like to keep it const.
BOOL
WszCreateProcess(
    LPCWSTR lpApplicationName,
    LPCWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
    )
{
    BOOL fResult;
    DWORD err;
    {
        size_t commandLineLength = u16_strlen(lpCommandLine) + 1;
        NewArrayHolder<WCHAR> nonConstCommandLine(new (nothrow) WCHAR[commandLineLength]);
        if (nonConstCommandLine == NULL)
        {
            SetLastError(ERROR_OUTOFMEMORY);
            return 0;
        }

        memcpy(nonConstCommandLine, lpCommandLine, commandLineLength * sizeof(WCHAR));

        fResult = CreateProcessW(lpApplicationName,
                                   nonConstCommandLine,
                                   lpProcessAttributes,
                                   lpThreadAttributes,
                                   bInheritHandles,
                                   dwCreationFlags,
                                   lpEnvironment,
                                   (LPWSTR)lpCurrentDirectory,
                                   lpStartupInfo,
                                   lpProcessInformation);

        // At the end of the current scope, the last error code will be overwritten by the destructor of
        // NewArrayHolder. So we save the error code here, and restore it after the end of the current scope.
        err = GetLastError();
    }

    SetLastError(err);
    return fResult;
}

#ifndef HOST_UNIX


#include "psapi.h"
#include "winnls.h"

//********** Globals. *********************************************************
static volatile ULONG g_dwMaxDBCSCharByteSize = 0;

// Detect Unicode support of the operating system, and initialize globals
DWORD GetMaxDBCSCharByteSize()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    if (g_dwMaxDBCSCharByteSize == 0)
    {
        DWORD dwMaxDBCSCharByteSize;

        CPINFO cpInfo;
        if (GetCPInfo(CP_ACP, &cpInfo))
            dwMaxDBCSCharByteSize = cpInfo.MaxCharSize;
        else
            dwMaxDBCSCharByteSize = 2;

        g_dwMaxDBCSCharByteSize = dwMaxDBCSCharByteSize;
    }

    return g_dwMaxDBCSCharByteSize;
}

typedef HRESULT(WINAPI *pfnSetThreadDescription)(HANDLE hThread, PCWSTR lpThreadDescription);
extern pfnSetThreadDescription g_pfnSetThreadDescription;

// Dummy method if windows version does not support it
HRESULT SetThreadDescriptionDummy(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return NOERROR;
}

HRESULT WINAPI InitializeSetThreadDescription(HANDLE hThread, PCWSTR lpThreadDescription)
{
    HMODULE hKernel32 = WszLoadLibrary(W("kernel32.dll"));

    pfnSetThreadDescription pLocal = NULL;
    if (hKernel32 != NULL)
    {
        // store to thread local variable to prevent data race
        pLocal = (pfnSetThreadDescription)GetProcAddress(hKernel32, "SetThreadDescription");
    }

    if (pLocal == NULL) // method is only available with Windows 10 Creators Update or later
    {
        g_pfnSetThreadDescription = SetThreadDescriptionDummy;
    }
    else
    {
        g_pfnSetThreadDescription = pLocal;
    }

    return g_pfnSetThreadDescription(hThread, lpThreadDescription);
}

pfnSetThreadDescription g_pfnSetThreadDescription = &InitializeSetThreadDescription;

// Set unmanaged thread name which will show up in ETW and Debuggers which know how to read this data.
HRESULT SetThreadName(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return g_pfnSetThreadDescription(hThread, lpThreadDescription);
}

#else //!HOST_UNIX

HRESULT SetThreadName(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return SetThreadDescription(hThread, lpThreadDescription);
}

#endif //!HOST_UNIX
