//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//
// Implementation of the GC environment
//

#include "common.h"

#include "windows.h"

#include "gcenv.h"
#include "gc.h"

int32_t FastInterlockIncrement(int32_t volatile *lpAddend)
{
    return InterlockedIncrement((LONG *)lpAddend);
}

int32_t FastInterlockDecrement(int32_t volatile *lpAddend)
{
    return InterlockedDecrement((LONG *)lpAddend);
}

int32_t FastInterlockExchange(int32_t volatile *Target, int32_t Value)
{
    return InterlockedExchange((LONG *)Target, Value);
}

int32_t FastInterlockCompareExchange(int32_t volatile *Destination, int32_t Exchange, int32_t Comperand)
{
    return InterlockedCompareExchange((LONG *)Destination, Exchange, Comperand);
}

int32_t FastInterlockExchangeAdd(int32_t volatile *Addend, int32_t Value)
{
    return InterlockedExchangeAdd((LONG *)Addend, Value);
}

void * _FastInterlockExchangePointer(void * volatile *Target, void * Value)
{
    return InterlockedExchangePointer(Target, Value);
}

void * _FastInterlockCompareExchangePointer(void * volatile *Destination, void * Exchange, void * Comperand)
{
    return InterlockedCompareExchangePointer(Destination, Exchange, Comperand);
}

void FastInterlockOr(uint32_t volatile *p, uint32_t msk)
{
    InterlockedOr((LONG *)p, msk);
}

void FastInterlockAnd(uint32_t volatile *p, uint32_t msk)
{
    InterlockedAnd((LONG *)p, msk);
}


void UnsafeInitializeCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    InitializeCriticalSection(lpCriticalSection);
}

void UnsafeEEEnterCriticalSection(CRITICAL_SECTION *lpCriticalSection)
{
    EnterCriticalSection(lpCriticalSection);
}

void UnsafeEELeaveCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    LeaveCriticalSection(lpCriticalSection);
}

void UnsafeDeleteCriticalSection(CRITICAL_SECTION *lpCriticalSection)
{
    DeleteCriticalSection(lpCriticalSection);
}


void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pMSEX->dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = GlobalMemoryStatusEx(pMSEX);
    _ASSERTE (fRet);

    // If the machine has more RAM than virtual address limit, let us cap it.
    // Our GC can never use more than virtual address limit.
    if (pMSEX->ullAvailPhys > pMSEX->ullTotalVirtual)
    {
        pMSEX->ullAvailPhys = pMSEX->ullAvailVirtual;
    }
}

void CLREventStatic::CreateManualEvent(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, TRUE, bInitialState, NULL);
    m_fInitialized = true;
}

void CLREventStatic::CreateAutoEvent(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, FALSE, bInitialState, NULL);
    m_fInitialized = true;
}

void CLREventStatic::CreateOSManualEvent(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, TRUE, bInitialState, NULL);
    m_fInitialized = true;
}

void CLREventStatic::CreateOSAutoEvent(bool bInitialState)
{
    m_hEvent = CreateEventW(NULL, FALSE, bInitialState, NULL);
    m_fInitialized = true;
}

void CLREventStatic::CloseEvent()
{
    if (m_fInitialized && m_hEvent != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hEvent);
        m_hEvent = INVALID_HANDLE_VALUE;
    }
}

bool CLREventStatic::IsValid() const
{
    return m_fInitialized && m_hEvent != INVALID_HANDLE_VALUE;
}

bool CLREventStatic::Set()
{
    if (!m_fInitialized)
        return false;
    return !!SetEvent(m_hEvent);
}

bool CLREventStatic::Reset()
{
    if (!m_fInitialized)
        return false;
    return !!ResetEvent(m_hEvent);
}

uint32_t CLREventStatic::Wait(uint32_t dwMilliseconds, bool bAlertable)
{
    DWORD result = WAIT_FAILED;

    if (m_fInitialized)
    {
        bool        disablePreemptive = false;
        Thread *    pCurThread = GetThread();

        if (NULL != pCurThread)
        {
            if (pCurThread->PreemptiveGCDisabled())
            {
                pCurThread->EnablePreemptiveGC();
                disablePreemptive = true;
            }
        }

        result = WaitForSingleObjectEx(m_hEvent, dwMilliseconds, bAlertable);

        if (disablePreemptive)
        {
            pCurThread->DisablePreemptiveGC();
        }
    }

    return result;
}

bool __SwitchToThread(uint32_t dwSleepMSec, uint32_t dwSwitchCount)
{
    SwitchToThread();
    return true;
}

void * ClrVirtualAlloc(
    void * lpAddress,
    size_t dwSize,
    uint32_t flAllocationType,
    uint32_t flProtect)
{
    return VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}

void * ClrVirtualAllocAligned(
    void * lpAddress,
    size_t dwSize,
    uint32_t flAllocationType,
    uint32_t flProtect,
    size_t dwAlignment)
{
    return VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}

bool ClrVirtualFree(
    void * lpAddress,
    size_t dwSize,
    uint32_t dwFreeType)
{
    return !!VirtualFree(lpAddress, dwSize, dwFreeType);
}

bool
ClrVirtualProtect(
           void * lpAddress,
           size_t dwSize,
           uint32_t flNewProtect,
           uint32_t * lpflOldProtect)
{
    return !!VirtualProtect(lpAddress, dwSize, flNewProtect, (DWORD *)lpflOldProtect);
}

MethodTable * g_pFreeObjectMethodTable;

GCSystemInfo g_SystemInfo;

void InitializeSystemInfo()
{
    SYSTEM_INFO systemInfo;
    GetSystemInfo(&systemInfo);

    g_SystemInfo.dwNumberOfProcessors = systemInfo.dwNumberOfProcessors;
    g_SystemInfo.dwPageSize = systemInfo.dwPageSize;
    g_SystemInfo.dwAllocationGranularity = systemInfo.dwAllocationGranularity;
}

int32_t g_TrapReturningThreads;

bool g_fFinalizerRunOnShutDown;

void DestroyThread(Thread * pThread)
{
    // TODO: Implement
}

bool PalHasCapability(PalCapability capability)
{
    // TODO: Implement for background GC
    return false;
}

