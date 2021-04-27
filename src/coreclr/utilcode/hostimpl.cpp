// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include "mscoree.h"
#include "clrinternal.h"
#include "clrhost.h"
#include "ex.h"

thread_local size_t t_ThreadType;

CRITSEC_COOKIE ClrCreateCriticalSection(CrstType crstType, CrstFlags flags)
{
    CRITICAL_SECTION *cs = (CRITICAL_SECTION*)malloc(sizeof(CRITICAL_SECTION));
    InitializeCriticalSection(cs);
    return (CRITSEC_COOKIE)cs;
}

void ClrDeleteCriticalSection(CRITSEC_COOKIE cookie)
{
    _ASSERTE(cookie);
    DeleteCriticalSection((CRITICAL_SECTION*)cookie);
    free(cookie);
}

void ClrEnterCriticalSection(CRITSEC_COOKIE cookie)
{
    _ASSERTE(cookie);
    EnterCriticalSection((CRITICAL_SECTION*)cookie);
}

void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie)
{
    _ASSERTE(cookie);
    LeaveCriticalSection((CRITICAL_SECTION*)cookie);
}

DWORD ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    return SleepEx(dwMilliseconds, bAlertable);
}

LPVOID ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect)
{
#ifdef FAILPOINTS_ENABLED
    if (RFS_HashStack ())
        return NULL;
#endif
    return VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}

BOOL ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
{
    return VirtualFree(lpAddress, dwSize, dwFreeType);
}

SIZE_T ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength)
{
    return VirtualQuery(lpAddress, lpBuffer, dwLength);
}

BOOL ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect)
{
    return VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}

//------------------------------------------------------------------------------
// Helper function to get an exception from outside the exception.  In
//  the CLR, it may be from the Thread object.  Non-CLR users have no thread object,
//  and it will do nothing.

void GetLastThrownObjectExceptionFromThread(Exception** ppException)
{
    *ppException = NULL;
}

#ifdef HOST_WINDOWS
void CreateCrashDumpIfEnabled(bool stackoverflow)
{
}
#endif
