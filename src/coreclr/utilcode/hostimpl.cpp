// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include <minipal/mutex.h>
#include <minipal.h>

#include "mscoree.h"
#include "clrinternal.h"
#include "clrhost.h"
#include "ex.h"

thread_local size_t t_ThreadType;

CRITSEC_COOKIE ClrCreateCriticalSection(CrstType crstType, CrstFlags flags)
{
    minipal_mutex* mt = (minipal_mutex*)malloc(sizeof(minipal_mutex));
    minipal_mutex_init(mt);
    return (CRITSEC_COOKIE)mt;
}

void ClrDeleteCriticalSection(CRITSEC_COOKIE cookie)
{
    _ASSERTE(cookie);
    minipal_mutex_destroy((minipal_mutex*)cookie);
    free(cookie);
}

void ClrEnterCriticalSection(CRITSEC_COOKIE cookie)
{
    _ASSERTE(cookie);
    minipal_mutex_enter((minipal_mutex*)cookie);
}

void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie) noexcept
{
    _ASSERTE(cookie);
    minipal_mutex_leave((minipal_mutex*)cookie);
}

#ifdef HOST_UNIX
static VMToOSInterface::MemoryProtection ClrWin32ProtectToMemoryProtection(DWORD flProtect)
{
    switch (flProtect)
    {
    case PAGE_NOACCESS:
        return VMToOSInterface::MemoryProtection::NoAccess;
    case PAGE_READONLY:
        return VMToOSInterface::MemoryProtection::ReadOnly;
    case PAGE_READWRITE:
        return VMToOSInterface::MemoryProtection::ReadWrite;
    case PAGE_EXECUTE:
        return VMToOSInterface::MemoryProtection::Execute;
    case PAGE_EXECUTE_READ:
        return VMToOSInterface::MemoryProtection::ExecuteRead;
    case PAGE_EXECUTE_READWRITE:
        return VMToOSInterface::MemoryProtection::ExecuteReadWrite;
    default:
        _ASSERTE(!"Unsupported page protection requested through ClrVirtual* APIs");
        return VMToOSInterface::MemoryProtection::NoAccess;
    }
}
#endif // HOST_UNIX

LPVOID ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect)
{
#ifdef HOST_WINDOWS
    return VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
#else
    _ASSERTE((flAllocationType & ~(MEM_COMMIT | MEM_RESERVE)) == 0);

    VMToOSInterface::MemoryProtection protection = ClrWin32ProtectToMemoryProtection(flProtect);

    if ((flAllocationType & MEM_RESERVE) != 0)
    {
        return VMToOSInterface::AllocateMemory(lpAddress, dwSize, protection);
    }

    // MEM_COMMIT only, over a previously reserved range.
    return VMToOSInterface::CommitReservedMemory(lpAddress, dwSize, protection);
#endif
}

BOOL ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
{
#ifdef HOST_WINDOWS
    return VirtualFree(lpAddress, dwSize, dwFreeType);
#else
    if (dwFreeType == MEM_DECOMMIT)
    {
        return VMToOSInterface::DecommitMemory(lpAddress, dwSize) ? TRUE : FALSE;
    }

    _ASSERTE(dwFreeType == MEM_RELEASE);
    return VMToOSInterface::ReleaseMemoryReservation(lpAddress, dwSize) ? TRUE : FALSE;
#endif
}

SIZE_T ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength)
{
    // All remaining callers of ClrVirtualQuery are Windows-only code paths; the raw PAL
    // VirtualQuery API has been removed from non-Windows builds.
#ifdef HOST_WINDOWS
    return VirtualQuery(lpAddress, lpBuffer, dwLength);
#else
    _ASSERTE(!"ClrVirtualQuery is not supported on non-Windows platforms");
    return 0;
#endif
}

BOOL ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect)
{
#ifdef HOST_WINDOWS
    return VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
#else
    // The PAL's VirtualProtect has never offered a real old-protection query on Unix (it always
    // returned a dummy fixed value), so non-Windows callers of this non-EE wrapper cannot rely on
    // *lpflOldProtect to reflect the true previous protection either.
    if (lpflOldProtect != NULL)
    {
        *lpflOldProtect = PAGE_EXECUTE_READWRITE;
    }

    return VMToOSInterface::ProtectMemory(lpAddress, dwSize, ClrWin32ProtectToMemoryProtection(flNewProtect)) ? TRUE : FALSE;
#endif
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
