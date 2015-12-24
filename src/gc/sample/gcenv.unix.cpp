//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//
// Implementation of the GC environment
//

#include "common.h"

#include "gcenv.h"
#include "gc.h"

#include <sys/mman.h>
#include <sys/time.h>

int32_t FastInterlockIncrement(int32_t volatile *lpAddend)
{
    return __sync_add_and_fetch(lpAddend, 1);
}

int32_t FastInterlockDecrement(int32_t volatile *lpAddend)
{
    return __sync_sub_and_fetch(lpAddend, 1);
}

int32_t FastInterlockExchange(int32_t volatile *Target, int32_t Value)
{
    return __sync_swap(Target, Value);
}

int32_t FastInterlockCompareExchange(int32_t volatile *Destination, int32_t Exchange, int32_t Comperand)
{
    return __sync_val_compare_and_swap(Destination, Comperand, Exchange);
}

int32_t FastInterlockExchangeAdd(int32_t volatile *Addend, int32_t Value)
{
    return __sync_fetch_and_add(Addend, Value);
}

void * _FastInterlockExchangePointer(void * volatile *Target, void * Value)
{
    return __sync_swap(Target, Value);
}

void * _FastInterlockCompareExchangePointer(void * volatile *Destination, void * Exchange, void * Comperand)
{
    return __sync_val_compare_and_swap(Destination, Comperand, Exchange);
}

void FastInterlockOr(uint32_t volatile *p, uint32_t msk)
{
    __sync_fetch_and_or(p, msk);
}

void FastInterlockAnd(uint32_t volatile *p, uint32_t msk)
{
    __sync_fetch_and_and(p, msk);
}


void UnsafeInitializeCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    pthread_mutex_init(&lpCriticalSection->mutex, NULL);
}

void UnsafeEEEnterCriticalSection(CRITICAL_SECTION *lpCriticalSection)
{
    pthread_mutex_lock(&lpCriticalSection->mutex);
}

void UnsafeEELeaveCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    pthread_mutex_unlock(&lpCriticalSection->mutex);
}

void UnsafeDeleteCriticalSection(CRITICAL_SECTION *lpCriticalSection)
{
    pthread_mutex_destroy(&lpCriticalSection->mutex);
}


void GetProcessMemoryLoad(GCMemoryStatus* pGCMemStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pGCMemStatus->dwMemoryLoad = 0;
    pGCMemStatus->ullTotalPageFile = 0;
    pGCMemStatus->ullAvailPageFile = 0;

    // There is no API to get the total virtual address space size on 
    // Unix, so we use a constant value representing 128TB, which is 
    // the approximate size of total user virtual address space on
    // the currently supported Unix systems.
    static const uint64_t _128TB = (1ull << 47);
    pGCMemStatus->ullTotalVirtual = _128TB;
    pGCMemStatus->ullAvailVirtual = _128TB;

    // TODO: Implement
    pGCMemStatus->ullTotalPhys = _128TB;
    pGCMemStatus->ullAvailPhys = _128TB;

    // If the machine has more RAM than virtual address limit, let us cap it.
    // Our GC can never use more than virtual address limit.
    if (pGCMemStatus->ullAvailPhys > pGCMemStatus->ullTotalVirtual)
    {
        pGCMemStatus->ullAvailPhys = pGCMemStatus->ullAvailVirtual;
    }
}

#if 0
void CLREventStatic::CreateManualEvent(bool bInitialState)
{
    // TODO: Implement
    m_fInitialized = true;
}

void CLREventStatic::CreateAutoEvent(bool bInitialState)
{
    // TODO: Implement
    m_fInitialized = true;
}

void CLREventStatic::CreateOSManualEvent(bool bInitialState)
{
    CreateManualEvent(bInitialState);
}

void CLREventStatic::CreateOSAutoEvent (bool bInitialState)
{
    CreateAutoEvent(bInitialState);
}

void CLREventStatic::CloseEvent()
{
    if (m_fInitialized)
    {
        // TODO: Implement
        m_fInitialized = false;
    }
}

bool CLREventStatic::IsValid() const
{
    return m_fInitialized; 
}

bool CLREventStatic::Set()
{
    if (!m_fInitialized)
        return false;
    // TODO: Implement
    return true; 
}

bool CLREventStatic::Reset()
{
    if (!m_fInitialized)
        return false;
    // TODO: Implement
    return true;
}

uint32_t CLREventStatic::Wait(uint32_t dwMilliseconds, bool bAlertable)
{
    DWORD result = WAIT_FAILED;

    if (m_fInitialized)
    {
        bool        disablePreemptive = false;
        Thread *    pCurThread  = GetThread();

        if (NULL != pCurThread)
        {
            if (pCurThread->PreemptiveGCDisabled())
            {
                pCurThread->EnablePreemptiveGC();
                disablePreemptive = true;
            }
        }

        // TODO: Implement
        result = WAIT_OBJECT_0;

        if (disablePreemptive)
        {
            pCurThread->DisablePreemptiveGC();
        }
    }

    return result;
}
#endif // 0

void DestroyThread(Thread * pThread)
{
    // TODO: implement
}

bool __SwitchToThread(uint32_t dwSleepMSec, uint32_t dwSwitchCount)
{
    return sched_yield() == 0;
}

void * ClrVirtualAlloc(
    void * lpAddress,
    size_t dwSize,
    uint32_t flAllocationType,
    uint32_t flProtect)
{
    return ClrVirtualAllocAligned(lpAddress, dwSize, flAllocationType, flProtect, OS_PAGE_SIZE);
}

static int W32toUnixAccessControl(uint32_t flProtect)
{
    int prot = 0;

    switch (flProtect & 0xff)
    {
    case PAGE_NOACCESS:
        prot = PROT_NONE;
        break;
    case PAGE_READWRITE:
        prot = PROT_READ | PROT_WRITE;
        break;
    default:
        _ASSERTE(false);
        break;
    }
    return prot;
}

void * ClrVirtualAllocAligned(
    void * lpAddress,
    size_t dwSize,
    uint32_t flAllocationType,
    uint32_t flProtect,
    size_t dwAlignment)
{
    if ((flAllocationType & ~(MEM_RESERVE | MEM_COMMIT)) != 0)
    {
        // TODO: Implement
        return NULL;
    }

    _ASSERTE(((size_t)lpAddress & (OS_PAGE_SIZE - 1)) == 0);

    // Align size to whole pages
    dwSize = (dwSize + (OS_PAGE_SIZE - 1)) & ~(OS_PAGE_SIZE - 1);

    if (flAllocationType & MEM_RESERVE)
    {
        size_t alignedSize = dwSize;

        if (dwAlignment > OS_PAGE_SIZE)
            alignedSize += (dwAlignment - OS_PAGE_SIZE);

        void * pRetVal = mmap(lpAddress, alignedSize, W32toUnixAccessControl(flProtect),
            MAP_ANON | MAP_PRIVATE, -1, 0);
         
        if (dwAlignment > OS_PAGE_SIZE && pRetVal != NULL)
        {
            void * pAlignedRetVal = (void *)(((size_t)pRetVal + (dwAlignment - 1)) & ~(dwAlignment - 1));

            size_t startPadding = (size_t)pAlignedRetVal - (size_t)pRetVal;
            if (startPadding != 0)
            {
                int ret = munmap(pRetVal, startPadding);
                _ASSERTE(ret == 0);
            }

            size_t endPadding = alignedSize - (startPadding + dwSize);
            if (endPadding != 0)
            {
                int ret = munmap((void *)((size_t)pAlignedRetVal + dwSize), endPadding);
                _ASSERTE(ret == 0);
            }

            pRetVal = pAlignedRetVal;
        }

        return pRetVal;
    }

    if (flAllocationType & MEM_COMMIT)
    {
        int ret = mprotect(lpAddress, dwSize, W32toUnixAccessControl(flProtect));
        return (ret == 0) ? lpAddress : NULL;
    }

    return NULL;
}

bool ClrVirtualFree(
    void * lpAddress,
    size_t dwSize,
    uint32_t dwFreeType)
{
    // TODO: Implement
    return false;
}

bool
ClrVirtualProtect(
           void * lpAddress,
           size_t dwSize,
           uint32_t flNewProtect,
           uint32_t * lpflOldProtect)
{
    // TODO: Implement, not currently used
    return false;
}

MethodTable * g_pFreeObjectMethodTable;

GCSystemInfo g_SystemInfo;

void InitializeSystemInfo()
{
    // TODO: Implement
    g_SystemInfo.dwNumberOfProcessors = 4;

    g_SystemInfo.dwPageSize = OS_PAGE_SIZE;
    g_SystemInfo.dwAllocationGranularity = OS_PAGE_SIZE;
}

int32_t g_TrapReturningThreads;

bool g_fFinalizerRunOnShutDown;

#if 0
#ifdef _MSC_VER
__declspec(thread)
#else
__thread
#endif
Thread * pCurrentThread;

Thread * GetThread()
{
    return pCurrentThread;
}

Thread * g_pThreadList = NULL;

Thread * ThreadStore::GetThreadList(Thread * pThread)
{
    if (pThread == NULL)
        return g_pThreadList;

    return pThread->m_pNext;
}

void ThreadStore::AttachCurrentThread(bool fAcquireThreadStoreLock)
{
    // TODO: Locks

    Thread * pThread = new Thread();
    pThread->GetAllocContext()->init();
    pCurrentThread = pThread;

    pThread->m_pNext = g_pThreadList;
    g_pThreadList = pThread;
}
#endif // 0
void DestroyThread(Thread * pThread)
{
    // TODO: Implement
}

#if 0 
void GCToEEInterface::SuspendEE(GCToEEInterface::SUSPEND_REASON reason)
{
    GCHeap::GetGCHeap()->SetGCInProgress(TRUE);

    // TODO: Implement
}

void GCToEEInterface::RestartEE(bool bFinishedGC)
{
    // TODO: Implement

    GCHeap::GetGCHeap()->SetGCInProgress(FALSE);
}

void GCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    // TODO: Implement - Scan stack roots
}

void GCToEEInterface::GcStartWork(int condemned, int max_gen)
{
}

void GCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
}

void GCToEEInterface::GcBeforeBGCSweepWork()
{
}

void GCToEEInterface::GcDone(int condemned)
{
}

void FinalizerThread::EnableFinalization()
{
    // Signal to finalizer thread that there are objects to finalize
    // TODO: Implement for finalization
}

bool PalStartBackgroundGCThread(BackgroundCallback callback, void* pCallbackContext)
{
    // TODO: Implement for background GC
    return false;
}

bool IsGCSpecialThread()
{
    // TODO: Implement for background GC
    return false;
}

#endif // 0

bool PalHasCapability(PalCapability capability)
{
    // TODO: Implement for background GC
    return false;
}

WINBASEAPI
UINT
WINAPI
GetWriteWatch(
    DWORD dwFlags,
    PVOID lpBaseAddress,
    SIZE_T dwRegionSize,
    PVOID *lpAddresses,
    uintptr_t * lpdwCount,
    uint32_t * lpdwGranularity
    )
{
    // TODO: Implement for background GC
    *lpAddresses = NULL;
    *lpdwCount = 0;
    // Until it is implemented, return non-zero value as an indicator of failure
    return 1;
}

WINBASEAPI
UINT
WINAPI
ResetWriteWatch(
    LPVOID lpBaseAddress,
    SIZE_T dwRegionSize
    )
{
    // TODO: Implement for background GC
    // Until it is implemented, return non-zero value as an indicator of failure
    return 1;
}

WINBASEAPI
BOOL
WINAPI
VirtualUnlock(
    LPVOID lpAddress,
    SIZE_T dwSize
    )
{
    // TODO: Implement
    return false;
}


const int tccSecondsToMillieSeconds = 1000;
const int tccSecondsToMicroSeconds = 1000000;
const int tccMillieSecondsToMicroSeconds = 1000;       // 10^3

WINBASEAPI
DWORD
WINAPI
GetTickCount()
{
    // TODO: More efficient, platform-specific implementation
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == -1)
    {
        _ASSERTE(!"gettimeofday() failed");
        return 0;
    }
    return (tv.tv_sec * tccSecondsToMillieSeconds) + (tv.tv_usec / tccMillieSecondsToMicroSeconds);
}

WINBASEAPI
BOOL
WINAPI
QueryPerformanceCounter(LARGE_INTEGER *lpPerformanceCount)
{
    // TODO: More efficient, platform-specific implementation
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == -1)
    {
        _ASSERTE(!"gettimeofday() failed");
        return FALSE;
    }
    lpPerformanceCount->QuadPart =
        (int64_t) tv.tv_sec * (int64_t) tccSecondsToMicroSeconds + (int64_t) tv.tv_usec;
    return TRUE;
}

WINBASEAPI
BOOL
WINAPI
QueryPerformanceFrequency(LARGE_INTEGER *lpFrequency)
{
    lpFrequency->QuadPart = (int64_t) tccSecondsToMicroSeconds;
    return TRUE;
}

WINBASEAPI
DWORD
WINAPI
GetCurrentThreadId(
    VOID)
{
    // TODO: Implement
    return 1;
}

WINBASEAPI
VOID
WINAPI
YieldProcessor()
{
    // TODO: Implement
}

WINBASEAPI
void
WINAPI
DebugBreak()
{
    // TODO: Implement
}

WINBASEAPI
VOID
WINAPI
MemoryBarrier()
{
    // TODO: Implement
}

// File I/O - Used for tracking only

WINBASEAPI
DWORD
WINAPI
SetFilePointer(
    HANDLE hFile,
    int32_t lDistanceToMove,
    int32_t * lpDistanceToMoveHigh,
    DWORD dwMoveMethod)
{
    // TODO: Reimplement callers using CRT
    return 0;
}

WINBASEAPI
BOOL
WINAPI
FlushFileBuffers(
    HANDLE hFile)
{
    // TODO: Reimplement callers using CRT
    return FALSE;
}

WINBASEAPI
BOOL
WINAPI
WriteFile(
    HANDLE hFile,
    LPCVOID lpBuffer,
    DWORD nNumberOfBytesToWrite,
    DWORD * lpNumberOfBytesWritten,
    PVOID lpOverlapped)
{
    // TODO: Reimplement callers using CRT
    return FALSE;
}

WINBASEAPI
BOOL
WINAPI
CloseHandle(
    HANDLE hObject)
{
    // TODO: Reimplement callers using CRT
    return FALSE;
}

WINBASEAPI
DWORD
WINAPI
GetLastError()
{
    return 1;
}
