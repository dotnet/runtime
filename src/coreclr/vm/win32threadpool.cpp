// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*++

Module Name:

    Win32ThreadPool.cpp

Abstract:

    This module implements Threadpool support using Win32 APIs


Revision History:
    December 1999 - Created

--*/

#include "common.h"
#include "log.h"
#include "threadpoolrequest.h"
#include "win32threadpool.h"
#include "delegateinfo.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "corhost.h"
#include "eventtrace.h"
#include "threads.h"
#include "appdomain.inl"
#include "nativeoverlapped.h"
#include "hillclimbing.h"
#include "configuration.h"


#ifndef TARGET_UNIX
#ifndef DACCESS_COMPILE

// APIs that must be accessed through dynamic linking.
typedef int (WINAPI *NtQueryInformationThreadProc) (
    HANDLE ThreadHandle,
    THREADINFOCLASS ThreadInformationClass,
    PVOID ThreadInformation,
    ULONG ThreadInformationLength,
    PULONG ReturnLength);
NtQueryInformationThreadProc g_pufnNtQueryInformationThread = NULL;

typedef int (WINAPI *NtQuerySystemInformationProc) (
    SYSTEM_INFORMATION_CLASS SystemInformationClass,
    PVOID SystemInformation,
    ULONG SystemInformationLength,
    PULONG ReturnLength OPTIONAL);
NtQuerySystemInformationProc g_pufnNtQuerySystemInformation = NULL;

typedef HANDLE (WINAPI * CreateWaitableTimerExProc) (
    LPSECURITY_ATTRIBUTES lpTimerAttributes,
    LPCTSTR lpTimerName,
    DWORD dwFlags,
    DWORD dwDesiredAccess);
CreateWaitableTimerExProc g_pufnCreateWaitableTimerEx = NULL;

typedef BOOL (WINAPI * SetWaitableTimerExProc) (
    HANDLE hTimer,
    const LARGE_INTEGER *lpDueTime,
    LONG lPeriod,
    PTIMERAPCROUTINE pfnCompletionRoutine,
    LPVOID lpArgToCompletionRoutine,
    void* WakeContext, //should be PREASON_CONTEXT, but it's not defined for us (and we don't use it)
    ULONG TolerableDelay);
SetWaitableTimerExProc g_pufnSetWaitableTimerEx = NULL;

#endif // !DACCESS_COMPILE
#endif // !TARGET_UNIX

BOOL ThreadpoolMgr::InitCompletionPortThreadpool = FALSE;
HANDLE ThreadpoolMgr::GlobalCompletionPort;                 // used for binding io completions on file handles

SVAL_IMPL(ThreadpoolMgr::ThreadCounter,ThreadpoolMgr,CPThreadCounter);

SVAL_IMPL_INIT(LONG,ThreadpoolMgr,MaxLimitTotalCPThreads,1000);   // = MaxLimitCPThreadsPerCPU * number of CPUS
SVAL_IMPL(LONG,ThreadpoolMgr,MinLimitTotalCPThreads);
SVAL_IMPL(LONG,ThreadpoolMgr,MaxFreeCPThreads);                   // = MaxFreeCPThreadsPerCPU * Number of CPUS

// Cacheline aligned, hot variable
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) SVAL_IMPL(ThreadpoolMgr::ThreadCounter, ThreadpoolMgr, WorkerCounter);

SVAL_IMPL(LONG,ThreadpoolMgr,MinLimitTotalWorkerThreads);          // = MaxLimitCPThreadsPerCPU * number of CPUS
SVAL_IMPL(LONG,ThreadpoolMgr,MaxLimitTotalWorkerThreads);        // = MaxLimitCPThreadsPerCPU * number of CPUS

SVAL_IMPL(LONG,ThreadpoolMgr,cpuUtilization);

HillClimbing ThreadpoolMgr::HillClimbingInstance;

// Cacheline aligned, 3 hot variables updated in a group
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) LONG ThreadpoolMgr::PriorCompletedWorkRequests = 0;
DWORD ThreadpoolMgr::PriorCompletedWorkRequestsTime;
DWORD ThreadpoolMgr::NextCompletedWorkRequestsTime;

LARGE_INTEGER ThreadpoolMgr::CurrentSampleStartTime;

unsigned int ThreadpoolMgr::WorkerThreadSpinLimit;
bool ThreadpoolMgr::IsHillClimbingDisabled;
int ThreadpoolMgr::ThreadAdjustmentInterval;

#define INVALID_HANDLE ((HANDLE) -1)
#define NEW_THREAD_THRESHOLD            7       // Number of requests outstanding before we start a new thread
#define CP_THREAD_PENDINGIO_WAIT 5000           // polling interval when thread is retired but has a pending io
#define GATE_THREAD_DELAY 500 /*milliseconds*/
#define GATE_THREAD_DELAY_TOLERANCE 50 /*milliseconds*/
#define DELAY_BETWEEN_SUSPENDS (5000 + GATE_THREAD_DELAY) // time to delay between suspensions

Volatile<LONG> ThreadpoolMgr::Initialization = 0;            // indicator of whether the threadpool is initialized.

bool ThreadpoolMgr::s_usePortableThreadPool = false;

// Cacheline aligned, hot variable
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) unsigned int ThreadpoolMgr::LastDequeueTime; // used to determine if work items are getting thread starved

SPTR_IMPL(WorkRequest,ThreadpoolMgr,WorkRequestHead);        // Head of work request queue
SPTR_IMPL(WorkRequest,ThreadpoolMgr,WorkRequestTail);        // Head of work request queue

SVAL_IMPL(ThreadpoolMgr::LIST_ENTRY,ThreadpoolMgr,TimerQueue);  // queue of timers

//unsigned int ThreadpoolMgr::LastCpuSamplingTime=0;      //  last time cpu utilization was sampled by gate thread
unsigned int ThreadpoolMgr::LastCPThreadCreation=0;     //  last time a completion port thread was created
unsigned int ThreadpoolMgr::NumberOfProcessors; // = NumberOfWorkerThreads - no. of blocked threads


CrstStatic ThreadpoolMgr::WorkerCriticalSection;
CLREvent * ThreadpoolMgr::RetiredCPWakeupEvent;       // wakeup event for completion port threads
CrstStatic ThreadpoolMgr::WaitThreadsCriticalSection;
ThreadpoolMgr::LIST_ENTRY ThreadpoolMgr::WaitThreadsHead;

CLRLifoSemaphore* ThreadpoolMgr::WorkerSemaphore;
CLRLifoSemaphore* ThreadpoolMgr::RetiredWorkerSemaphore;

CrstStatic ThreadpoolMgr::TimerQueueCriticalSection;
HANDLE ThreadpoolMgr::TimerThread=NULL;
Thread *ThreadpoolMgr::pTimerThread=NULL;

// Cacheline aligned, hot variable
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) DWORD ThreadpoolMgr::LastTickCount;

// Cacheline aligned, hot variable
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) LONG  ThreadpoolMgr::GateThreadStatus=GATE_THREAD_STATUS_NOT_RUNNING;

// Move out of from preceeding variables' cache line
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) ThreadpoolMgr::RecycledListsWrapper ThreadpoolMgr::RecycledLists;

ThreadpoolMgr::TimerInfo *ThreadpoolMgr::TimerInfosToBeRecycled = NULL;

BOOL ThreadpoolMgr::IsApcPendingOnWaitThread = FALSE;

#ifndef DACCESS_COMPILE

// Macros for inserting/deleting from doubly linked list

#define InitializeListHead(ListHead) (\
    (ListHead)->Flink = (ListHead)->Blink = (ListHead))

//
// these are named the same as slightly different macros in the NT headers
//
#undef RemoveHeadList
#undef RemoveEntryList
#undef InsertTailList
#undef InsertHeadList

#define RemoveHeadList(ListHead,FirstEntry) \
    {\
    FirstEntry = (LIST_ENTRY*) (ListHead)->Flink;\
    ((LIST_ENTRY*)FirstEntry->Flink)->Blink = (ListHead);\
    (ListHead)->Flink = FirstEntry->Flink;\
    }

#define RemoveEntryList(Entry) {\
    LIST_ENTRY* _EX_Entry;\
        _EX_Entry = (Entry);\
        ((LIST_ENTRY*) _EX_Entry->Blink)->Flink = _EX_Entry->Flink;\
        ((LIST_ENTRY*) _EX_Entry->Flink)->Blink = _EX_Entry->Blink;\
    }

#define InsertTailList(ListHead,Entry) \
    (Entry)->Flink = (ListHead);\
    (Entry)->Blink = (ListHead)->Blink;\
    ((LIST_ENTRY*)(ListHead)->Blink)->Flink = (Entry);\
    (ListHead)->Blink = (Entry);

#define InsertHeadList(ListHead,Entry) {\
    LIST_ENTRY* _EX_Flink;\
    LIST_ENTRY* _EX_ListHead;\
    _EX_ListHead = (LIST_ENTRY*)(ListHead);\
    _EX_Flink = (LIST_ENTRY*) _EX_ListHead->Flink;\
    (Entry)->Flink = _EX_Flink;\
    (Entry)->Blink = _EX_ListHead;\
    _EX_Flink->Blink = (Entry);\
    _EX_ListHead->Flink = (Entry);\
    }

#define IsListEmpty(ListHead) \
    ((ListHead)->Flink == (ListHead))

#define SetLastHRError(hr) \
    if (HRESULT_FACILITY(hr) == FACILITY_WIN32)\
        SetLastError(HRESULT_CODE(hr));\
    else \
        SetLastError(ERROR_INVALID_DATA);\

/************************************************************************/

void ThreadpoolMgr::RecycledListsWrapper::Initialize( unsigned int numProcs )
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pRecycledListPerProcessor = new RecycledListInfo[numProcs][MEMTYPE_COUNT];
}

//--//

void ThreadpoolMgr::EnsureInitialized()
{
    CONTRACTL
    {
        THROWS;         // EnsureInitializedSlow can throw
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (IsInitialized())
        return;

    EnsureInitializedSlow();
}

NOINLINE void ThreadpoolMgr::EnsureInitializedSlow()
{
    CONTRACTL
    {
        THROWS;         // Initialize can throw
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD dwSwitchCount = 0;

retry:
    if (InterlockedCompareExchange(&Initialization, 1, 0) == 0)
    {
        if (Initialize())
            Initialization = -1;
        else
        {
            Initialization = 0;
            COMPlusThrowOM();
        }
    }
    else // someone has already begun initializing.
    {
        // wait until it finishes
        while (Initialization != -1)
        {
            __SwitchToThread(0, ++dwSwitchCount);
            goto retry;
        }
    }
}

DWORD GetDefaultMaxLimitWorkerThreads(DWORD minLimit)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    //
    // We determine the max limit for worker threads as follows:
    //
    //  1) It must be at least MinLimitTotalWorkerThreads
    //  2) It must be no greater than (half the virtual address space)/(thread stack size)
    //  3) It must be <= MaxPossibleWorkerThreads
    //
    // TODO: what about CP threads?  Can they follow a similar plan?  How do we allocate
    // thread counts between the two kinds of threads?
    //
    SIZE_T stackReserveSize = 0;
    Thread::GetProcessDefaultStackSize(&stackReserveSize, NULL);

    ULONGLONG halfVirtualAddressSpace;

    MEMORYSTATUSEX memStats;
    memStats.dwLength = sizeof(memStats);
    if (GlobalMemoryStatusEx(&memStats))
    {
        halfVirtualAddressSpace = memStats.ullTotalVirtual / 2;
    }
    else
    {
        //assume the normal Win32 32-bit virtual address space
        halfVirtualAddressSpace = 0x000000007FFE0000ull / 2;
    }

    ULONGLONG limit = halfVirtualAddressSpace / stackReserveSize;
    limit = max(limit, (ULONGLONG)minLimit);
    limit = min(limit, (ULONGLONG)ThreadpoolMgr::ThreadCounter::MaxPossibleCount);

    _ASSERTE(FitsIn<DWORD>(limit));
    return (DWORD)limit;
}

DWORD GetForceMinWorkerThreadsValue()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    return Configuration::GetKnobDWORDValue(W("System.Threading.ThreadPool.MinThreads"), CLRConfig::INTERNAL_ThreadPool_ForceMinWorkerThreads);
}

DWORD GetForceMaxWorkerThreadsValue()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    return Configuration::GetKnobDWORDValue(W("System.Threading.ThreadPool.MaxThreads"), CLRConfig::INTERNAL_ThreadPool_ForceMaxWorkerThreads);
}

BOOL ThreadpoolMgr::Initialize()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    BOOL bRet = FALSE;
    BOOL bExceptionCaught = FALSE;

#ifndef TARGET_UNIX
    //ThreadPool_CPUGroup
    CPUGroupInfo::EnsureInitialized();
    if (CPUGroupInfo::CanEnableGCCPUGroups() && CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
        NumberOfProcessors = CPUGroupInfo::GetNumActiveProcessors();
    else
        NumberOfProcessors = GetCurrentProcessCpuCount();
#else // !TARGET_UNIX
    NumberOfProcessors = GetCurrentProcessCpuCount();
#endif // !TARGET_UNIX
    InitPlatformVariables();

    EX_TRY
    {
        if (!UsePortableThreadPool())
        {
            WorkerThreadSpinLimit = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit);
            IsHillClimbingDisabled = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_Disable) != 0;
            ThreadAdjustmentInterval = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_SampleIntervalLow);

            WaitThreadsCriticalSection.Init(CrstThreadpoolWaitThreads);
        }
        WorkerCriticalSection.Init(CrstThreadpoolWorker);
        TimerQueueCriticalSection.Init(CrstThreadpoolTimerQueue);

        if (!UsePortableThreadPool())
        {
            // initialize WaitThreadsHead
            InitializeListHead(&WaitThreadsHead);
        }

        // initialize TimerQueue
        InitializeListHead(&TimerQueue);

        RetiredCPWakeupEvent = new CLREvent();
        RetiredCPWakeupEvent->CreateAutoEvent(FALSE);
        _ASSERTE(RetiredCPWakeupEvent->IsValid());

        if (!UsePortableThreadPool())
        {
            WorkerSemaphore = new CLRLifoSemaphore();
            WorkerSemaphore->Create(0, ThreadCounter::MaxPossibleCount);

            RetiredWorkerSemaphore = new CLRLifoSemaphore();
            RetiredWorkerSemaphore->Create(0, ThreadCounter::MaxPossibleCount);
        }

#ifndef TARGET_UNIX
        //ThreadPool_CPUGroup
        if (CPUGroupInfo::CanEnableGCCPUGroups() && CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
            RecycledLists.Initialize( CPUGroupInfo::GetNumActiveProcessors() );
        else
            RecycledLists.Initialize( g_SystemInfo.dwNumberOfProcessors );
#else // !TARGET_UNIX
        RecycledLists.Initialize( PAL_GetTotalCpuCount() );
#endif // !TARGET_UNIX
    }
    EX_CATCH
    {
        if (RetiredCPWakeupEvent)
        {
            delete RetiredCPWakeupEvent;
            RetiredCPWakeupEvent = NULL;
        }

        // Note: It is fine to call Destroy on uninitialized critical sections
        if (!UsePortableThreadPool())
        {
            WaitThreadsCriticalSection.Destroy();
        }
        WorkerCriticalSection.Destroy();
        TimerQueueCriticalSection.Destroy();

        bExceptionCaught = TRUE;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (bExceptionCaught)
    {
        goto end;
    }

    if (!UsePortableThreadPool())
    {
        // initialize Worker thread settings
        DWORD forceMin;
        forceMin = GetForceMinWorkerThreadsValue();
        MinLimitTotalWorkerThreads = forceMin > 0 ? (LONG)forceMin : (LONG)NumberOfProcessors;

        DWORD forceMax;
        forceMax = GetForceMaxWorkerThreadsValue();
        MaxLimitTotalWorkerThreads = forceMax > 0 ? (LONG)forceMax : (LONG)GetDefaultMaxLimitWorkerThreads(MinLimitTotalWorkerThreads);

        ThreadCounter::Counts counts;
        counts.NumActive = 0;
        counts.NumWorking = 0;
        counts.NumRetired = 0;
        counts.MaxWorking = MinLimitTotalWorkerThreads;
        WorkerCounter.counts.AsLongLong = counts.AsLongLong;
    }

    // initialize CP thread settings
    MinLimitTotalCPThreads = NumberOfProcessors;

    // Use volatile store to guarantee make the value visible to the DAC (the store can be optimized out otherwise)
    VolatileStoreWithoutBarrier<LONG>(&MaxFreeCPThreads, NumberOfProcessors*MaxFreeCPThreadsPerCPU);

    ThreadCounter::Counts counts;
    counts.NumActive = 0;
    counts.NumWorking = 0;
    counts.NumRetired = 0;
    counts.MaxWorking = MinLimitTotalCPThreads;
    CPThreadCounter.counts.AsLongLong = counts.AsLongLong;

#ifndef TARGET_UNIX
    {
        GlobalCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE,
                                                      NULL,
                                                      0,        /*ignored for invalid handle value*/
                                                      NumberOfProcessors);
    }
#endif // !TARGET_UNIX

    if (!UsePortableThreadPool())
    {
        HillClimbingInstance.Initialize();
    }

    bRet = TRUE;
end:
    return bRet;
}

void ThreadpoolMgr::InitPlatformVariables()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    HINSTANCE  hNtDll;
    HINSTANCE  hCoreSynch = nullptr;
    {
        CONTRACT_VIOLATION(GCViolation|FaultViolation);
        hNtDll = CLRLoadLibrary(W("ntdll.dll"));
        _ASSERTE(hNtDll);
        if (!UsePortableThreadPool())
        {
#ifdef FEATURE_CORESYSTEM
            hCoreSynch = CLRLoadLibrary(W("api-ms-win-core-synch-l1-1-0.dll"));
#else
            hCoreSynch = CLRLoadLibrary(W("kernel32.dll"));
#endif
            _ASSERTE(hCoreSynch);
        }
    }

    // These APIs must be accessed via dynamic binding since they may be removed in future
    // OS versions.
    g_pufnNtQueryInformationThread = (NtQueryInformationThreadProc)GetProcAddress(hNtDll,"NtQueryInformationThread");
    g_pufnNtQuerySystemInformation = (NtQuerySystemInformationProc)GetProcAddress(hNtDll,"NtQuerySystemInformation");

    if (!UsePortableThreadPool())
    {
        // These APIs are only supported on newer Windows versions
        g_pufnCreateWaitableTimerEx = (CreateWaitableTimerExProc)GetProcAddress(hCoreSynch, "CreateWaitableTimerExW");
        g_pufnSetWaitableTimerEx = (SetWaitableTimerExProc)GetProcAddress(hCoreSynch, "SetWaitableTimerEx");
    }
#endif
}

bool ThreadpoolMgr::CanSetMinIOCompletionThreads(DWORD ioCompletionThreads)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(UsePortableThreadPool());

    EnsureInitialized();

    // The lock used by SetMinThreads() and SetMaxThreads() is not taken here, the caller is expected to synchronize between
    // them. The conditions here should be the same as in the corresponding Set function.
    return ioCompletionThreads <= (DWORD)MaxLimitTotalCPThreads;
}

bool ThreadpoolMgr::CanSetMaxIOCompletionThreads(DWORD ioCompletionThreads)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(UsePortableThreadPool());
    _ASSERTE(ioCompletionThreads != 0);

    EnsureInitialized();

    // The lock used by SetMinThreads() and SetMaxThreads() is not taken here, the caller is expected to synchronize between
    // them. The conditions here should be the same as in the corresponding Set function.
    return ioCompletionThreads >= (DWORD)MinLimitTotalCPThreads;
}

BOOL ThreadpoolMgr::SetMaxThreadsHelper(DWORD MaxWorkerThreads,
                                        DWORD MaxIOCompletionThreads)
{
    CONTRACTL
    {
        THROWS;     // Crst can throw and toggle GC mode
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    BOOL result = FALSE;

    // doesn't need to be WorkerCS, but using it to avoid race condition between setting min and max, and didn't want to create a new CS.
    CrstHolder csh(&WorkerCriticalSection);

    bool usePortableThreadPool = UsePortableThreadPool();
    if ((
            usePortableThreadPool ||
            (
                MaxWorkerThreads >= (DWORD)MinLimitTotalWorkerThreads &&
                MaxWorkerThreads != 0
            )
        ) &&
        MaxIOCompletionThreads >= (DWORD)MinLimitTotalCPThreads &&
        MaxIOCompletionThreads != 0)
    {
        if (!usePortableThreadPool && GetForceMaxWorkerThreadsValue() == 0)
        {
            MaxLimitTotalWorkerThreads = min(MaxWorkerThreads, (DWORD)ThreadCounter::MaxPossibleCount);

            ThreadCounter::Counts counts = WorkerCounter.GetCleanCounts();
            while (counts.MaxWorking > MaxLimitTotalWorkerThreads)
            {
                ThreadCounter::Counts newCounts = counts;
                newCounts.MaxWorking = MaxLimitTotalWorkerThreads;

                ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);
                if (oldCounts == counts)
                    counts = newCounts;
                else
                    counts = oldCounts;
            }
        }

        MaxLimitTotalCPThreads = min(MaxIOCompletionThreads, (DWORD)ThreadCounter::MaxPossibleCount);

        result = TRUE;
    }

    return result;
 }

/************************************************************************/
BOOL ThreadpoolMgr::SetMaxThreads(DWORD MaxWorkerThreads,
                                  DWORD MaxIOCompletionThreads)
{
    CONTRACTL
    {
        THROWS;     // SetMaxThreadsHelper can throw and toggle GC mode
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EnsureInitialized();

    return SetMaxThreadsHelper(MaxWorkerThreads, MaxIOCompletionThreads);
}

BOOL ThreadpoolMgr::GetMaxThreads(DWORD* MaxWorkerThreads,
                                  DWORD* MaxIOCompletionThreads)
{
    LIMITED_METHOD_CONTRACT;

    if (!MaxWorkerThreads || !MaxIOCompletionThreads)
    {
        SetLastHRError(ERROR_INVALID_DATA);
        return FALSE;
    }

    EnsureInitialized();

    *MaxWorkerThreads = UsePortableThreadPool() ? 1 : (DWORD)MaxLimitTotalWorkerThreads;
    *MaxIOCompletionThreads = MaxLimitTotalCPThreads;
    return TRUE;
}

BOOL ThreadpoolMgr::SetMinThreads(DWORD MinWorkerThreads,
                                  DWORD MinIOCompletionThreads)
{
    CONTRACTL
    {
        THROWS;     // Crst can throw and toggle GC mode
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EnsureInitialized();

    // doesn't need to be WorkerCS, but using it to avoid race condition between setting min and max, and didn't want to create a new CS.
    CrstHolder csh(&WorkerCriticalSection);

    BOOL init_result = FALSE;

    bool usePortableThreadPool = UsePortableThreadPool();
    if ((
            usePortableThreadPool ||
            (
                MinWorkerThreads >= 0 &&
                MinWorkerThreads <= (DWORD) MaxLimitTotalWorkerThreads
            )
        ) &&
        MinIOCompletionThreads >= 0 &&
        MinIOCompletionThreads <= (DWORD) MaxLimitTotalCPThreads)
    {
        if (!usePortableThreadPool && GetForceMinWorkerThreadsValue() == 0)
        {
            MinLimitTotalWorkerThreads = max(1, min(MinWorkerThreads, (DWORD)ThreadCounter::MaxPossibleCount));

            ThreadCounter::Counts counts = WorkerCounter.GetCleanCounts();
            while (counts.MaxWorking < MinLimitTotalWorkerThreads)
            {
                ThreadCounter::Counts newCounts = counts;
                newCounts.MaxWorking = MinLimitTotalWorkerThreads;

                ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);
                if (oldCounts == counts)
                {
                    counts = newCounts;

                    // if we increased the limit, and there are pending workitems, we need
                    // to dispatch a thread to process the work.
                    if (newCounts.MaxWorking > oldCounts.MaxWorking &&
                        PerAppDomainTPCountList::AreRequestsPendingInAnyAppDomains())
                    {
                        MaybeAddWorkingWorker();
                    }
                }
                else
                {
                    counts = oldCounts;
                }
            }
        }

        MinLimitTotalCPThreads = max(1, min(MinIOCompletionThreads, (DWORD)ThreadCounter::MaxPossibleCount));

        init_result = TRUE;
    }

    return init_result;
}

BOOL ThreadpoolMgr::GetMinThreads(DWORD* MinWorkerThreads,
                                  DWORD* MinIOCompletionThreads)
{
    LIMITED_METHOD_CONTRACT;

    if (!MinWorkerThreads || !MinIOCompletionThreads)
    {
        SetLastHRError(ERROR_INVALID_DATA);
        return FALSE;
    }

    EnsureInitialized();

    *MinWorkerThreads = UsePortableThreadPool() ? 1 : (DWORD)MinLimitTotalWorkerThreads;
    *MinIOCompletionThreads = MinLimitTotalCPThreads;
    return TRUE;
}

BOOL ThreadpoolMgr::GetAvailableThreads(DWORD* AvailableWorkerThreads,
                                        DWORD* AvailableIOCompletionThreads)
{
    LIMITED_METHOD_CONTRACT;

    if (!AvailableWorkerThreads || !AvailableIOCompletionThreads)
    {
        SetLastHRError(ERROR_INVALID_DATA);
        return FALSE;
    }

    EnsureInitialized();

    if (UsePortableThreadPool())
    {
        *AvailableWorkerThreads = 0;
    }
    else
    {
        ThreadCounter::Counts counts = WorkerCounter.GetCleanCounts();

        if (MaxLimitTotalWorkerThreads < counts.NumActive)
            *AvailableWorkerThreads = 0;
        else
            *AvailableWorkerThreads = MaxLimitTotalWorkerThreads - counts.NumWorking;
    }

    ThreadCounter::Counts counts = CPThreadCounter.GetCleanCounts();
    if (MaxLimitTotalCPThreads < counts.NumActive)
        *AvailableIOCompletionThreads = counts.NumActive - counts.NumWorking;
    else
        *AvailableIOCompletionThreads = MaxLimitTotalCPThreads - counts.NumWorking;
    return TRUE;
}

INT32 ThreadpoolMgr::GetThreadCount()
{
    WRAPPER_NO_CONTRACT;

    if (!IsInitialized())
    {
        return 0;
    }

    INT32 workerThreadCount = UsePortableThreadPool() ? 0 : WorkerCounter.DangerousGetDirtyCounts().NumActive;
    return workerThreadCount + CPThreadCounter.DangerousGetDirtyCounts().NumActive;
}

void QueueUserWorkItemHelp(LPTHREAD_START_ROUTINE Function, PVOID Context)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;
    /* Cannot use contract here because of SEH
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;*/

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    Function(Context);

    Thread *pThread = GetThread();
    if (pThread)
    {
        _ASSERTE(!pThread->IsAbortRequested());
        pThread->InternalReset();
    }
}

//
// WorkingThreadCounts tracks the number of worker threads currently doing user work, and the maximum number of such threads
// since the last time TakeMaxWorkingThreadCount was called.  This information is for diagnostic purposes only,
// and is tracked only if the CLR config value INTERNAL_ThreadPool_EnableWorkerTracking is non-zero (this feature is off
// by default).
//
union WorkingThreadCounts
{
    struct
    {
        int currentWorking : 16;
        int maxWorking : 16;
    };

    LONG asLong;
};

WorkingThreadCounts g_workingThreadCounts;

//
// If worker tracking is enabled (see above) then this is called immediately before and after a worker thread executes
// each work item.
//
void ThreadpoolMgr::ReportThreadStatus(bool isWorking)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(IsInitialized()); // can't be here without requesting a thread first
    _ASSERTE(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking));

    while (true)
    {
        WorkingThreadCounts currentCounts, newCounts;
        currentCounts.asLong = VolatileLoad(&g_workingThreadCounts.asLong);

        newCounts = currentCounts;

        if (isWorking)
            newCounts.currentWorking++;

        if (newCounts.currentWorking > newCounts.maxWorking)
            newCounts.maxWorking = newCounts.currentWorking;

        if (!isWorking)
            newCounts.currentWorking--;

        if (currentCounts.asLong == InterlockedCompareExchange(&g_workingThreadCounts.asLong, newCounts.asLong, currentCounts.asLong))
            break;
    }
}

//
// Returns the max working count since the previous call to TakeMaxWorkingThreadCount, and resets WorkingThreadCounts.maxWorking.
//
int TakeMaxWorkingThreadCount()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking));
    while (true)
    {
        WorkingThreadCounts currentCounts, newCounts;
        currentCounts.asLong = VolatileLoad(&g_workingThreadCounts.asLong);

        newCounts = currentCounts;
        newCounts.maxWorking = 0;

        if (currentCounts.asLong == InterlockedCompareExchange(&g_workingThreadCounts.asLong, newCounts.asLong, currentCounts.asLong))
        {
            // If we haven't updated the counts since the last call to TakeMaxWorkingThreadCount, then we never updated maxWorking.
            // In that case, the number of working threads for the whole period since the last TakeMaxWorkingThreadCount is the
            // current number of working threads.
            return currentCounts.maxWorking == 0 ? currentCounts.currentWorking : currentCounts.maxWorking;
        }
    }
}


/************************************************************************/

BOOL ThreadpoolMgr::QueueUserWorkItem(LPTHREAD_START_ROUTINE Function,
                                      PVOID Context,
                                      DWORD Flags,
                                      BOOL UnmanagedTPRequest)
{
    CONTRACTL
    {
        THROWS;     // EnsureInitialized, EnqueueWorkRequest can throw OOM
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE_ALL_BUILDS(__FILE__, !UsePortableThreadPool());

    EnsureInitialized();


    if (Flags == CALL_OR_QUEUE)
    {
        // we've been asked to call this directly if the thread pressure is not too high

        int MinimumAvailableCPThreads = (NumberOfProcessors < 3) ? 3 : NumberOfProcessors;

        ThreadCounter::Counts counts = CPThreadCounter.GetCleanCounts();
        if ((MaxLimitTotalCPThreads - counts.NumActive) >= MinimumAvailableCPThreads )
        {
            QueueUserWorkItemHelp(Function, Context);
            return TRUE;
        }

    }

    if (UnmanagedTPRequest)
    {
        UnManagedPerAppDomainTPCount* pADTPCount;
        pADTPCount = PerAppDomainTPCountList::GetUnmanagedTPCount();
        pADTPCount->QueueUnmanagedWorkRequest(Function, Context);
    }
    else
    {
        // caller has already registered its TPCount; this call is just to adjust the thread count
    }

    return TRUE;
}


bool ThreadpoolMgr::ShouldWorkerKeepRunning()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    //
    // Maybe this thread should retire now.  Let's see.
    //
    bool shouldThisThreadKeepRunning = true;

    // Dirty read is OK here; the worst that can happen is that we won't retire this time.  In the
    // case where we might retire, we have to succeed a CompareExchange, which will have the effect
    // of validating this read.
    ThreadCounter::Counts counts = WorkerCounter.DangerousGetDirtyCounts();
    while (true)
    {
        if (counts.NumActive <= counts.MaxWorking)
        {
            shouldThisThreadKeepRunning = true;
            break;
        }

        ThreadCounter::Counts newCounts = counts;
        newCounts.NumWorking--;
        newCounts.NumActive--;
        newCounts.NumRetired++;

        ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);

        if (oldCounts == counts)
        {
            shouldThisThreadKeepRunning = false;
            break;
        }

        counts = oldCounts;
    }

    return shouldThisThreadKeepRunning;
}

DangerousNonHostedSpinLock ThreadpoolMgr::ThreadAdjustmentLock;


//
// This method must only be called if ShouldAdjustMaxWorkersActive has returned true, *and*
// ThreadAdjustmentLock is held.
//
void ThreadpoolMgr::AdjustMaxWorkersActive()
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());
    _ASSERTE(ThreadAdjustmentLock.IsHeld());

    LARGE_INTEGER startTime = CurrentSampleStartTime;
    LARGE_INTEGER endTime;
    QueryPerformanceCounter(&endTime);

    static LARGE_INTEGER freq;
    if (freq.QuadPart == 0)
        QueryPerformanceFrequency(&freq);

    double elapsed = (double)(endTime.QuadPart - startTime.QuadPart) / freq.QuadPart;

    //
    // It's possible for the current sample to be reset while we're holding
    // ThreadAdjustmentLock.  This will result in a very short sample, possibly
    // with completely bogus counts.  We'll try to detect this by checking the sample
    // interval; if it's very short, then we try again later.
    //
    if (elapsed*1000.0 >= (ThreadAdjustmentInterval/2))
    {
        DWORD currentTicks = GetTickCount();
        LONG totalNumCompletions = (LONG)Thread::GetTotalWorkerThreadPoolCompletionCount();
        LONG numCompletions = totalNumCompletions - VolatileLoad(&PriorCompletedWorkRequests);
        ThreadCounter::Counts currentCounts = WorkerCounter.GetCleanCounts();

        int newMax = HillClimbingInstance.Update(
            currentCounts.MaxWorking,
            elapsed,
            numCompletions,
            &ThreadAdjustmentInterval);

        while (newMax != currentCounts.MaxWorking)
        {
            ThreadCounter::Counts newCounts = currentCounts;
            newCounts.MaxWorking = newMax;

            ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, currentCounts);
            if (oldCounts == currentCounts)
            {
                //
                // If we're increasing the max, inject a thread.  If that thread finds work, it will inject
                // another thread, etc., until nobody finds work or we reach the new maximum.
                //
                // If we're reducing the max, whichever threads notice this first will retire themselves.
                //
                if (newMax > oldCounts.MaxWorking)
                    MaybeAddWorkingWorker();

                break;
            }
            else
            {
                // we failed - maybe try again
                if (oldCounts.MaxWorking > currentCounts.MaxWorking &&
                    oldCounts.MaxWorking >= newMax)
                {
                    // someone (probably the gate thread) increased the thread count more than
                    // we are about to do.  Don't interfere.
                    break;
                }

                currentCounts = oldCounts;
            }
        }

        PriorCompletedWorkRequests = totalNumCompletions;
        NextCompletedWorkRequestsTime = currentTicks + ThreadAdjustmentInterval;
        MemoryBarrier(); // flush previous writes (especially NextCompletedWorkRequestsTime)
        PriorCompletedWorkRequestsTime = currentTicks;
        CurrentSampleStartTime = endTime;;
    }
}


void ThreadpoolMgr::MaybeAddWorkingWorker()
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    // counts volatile read paired with CompareExchangeCounts loop set
    ThreadCounter::Counts counts = WorkerCounter.DangerousGetDirtyCounts();
    ThreadCounter::Counts newCounts;
    while (true)
    {
        newCounts = counts;
        newCounts.NumWorking = max(counts.NumWorking, min(counts.NumWorking + 1, counts.MaxWorking));
        newCounts.NumActive = max(counts.NumActive, newCounts.NumWorking);
        newCounts.NumRetired = max(0, counts.NumRetired - (newCounts.NumActive - counts.NumActive));

        if (newCounts == counts)
            return;

        ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);

        if (oldCounts == counts)
            break;

        counts = oldCounts;
    }

    int toUnretire = counts.NumRetired - newCounts.NumRetired;
    int toCreate = (newCounts.NumActive - counts.NumActive) - toUnretire;
    int toRelease = (newCounts.NumWorking - counts.NumWorking) - (toUnretire + toCreate);

    _ASSERTE(toUnretire >= 0);
    _ASSERTE(toCreate >= 0);
    _ASSERTE(toRelease >= 0);
    _ASSERTE(toUnretire + toCreate + toRelease <= 1);

    if (toUnretire > 0)
    {
        RetiredWorkerSemaphore->Release(toUnretire);
    }

    if (toRelease > 0)
        WorkerSemaphore->Release(toRelease);

    while (toCreate > 0)
    {
        if (CreateWorkerThread())
        {
            toCreate--;
        }
        else
        {
            //
            // Uh-oh, we promised to create a new thread, but the creation failed.  We have to renege on our
            // promise.  This may possibly result in no work getting done for a while, but the gate thread will
            // eventually notice that no completions are happening and force the creation of a new thread.
            // Of course, there's no guarantee *that* will work - but hopefully enough time will have passed
            // to allow whoever's using all the memory right now to release some.
            //

            // counts volatile read paired with CompareExchangeCounts loop set
            counts = WorkerCounter.DangerousGetDirtyCounts();
            while (true)
            {
                //
                // If we said we would create a thread, we also said it would be working.  So we need to
                // decrement both NumWorking and NumActive by the number of threads we will no longer be creating.
                //
                newCounts = counts;
                newCounts.NumWorking -= toCreate;
                newCounts.NumActive -= toCreate;

                ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);

                if (oldCounts == counts)
                    break;

                counts = oldCounts;
            }

            toCreate = 0;
        }
    }
}

BOOL ThreadpoolMgr::PostQueuedCompletionStatus(LPOVERLAPPED lpOverlapped,
                                      LPOVERLAPPED_COMPLETION_ROUTINE Function)
{
    CONTRACTL
    {
        THROWS;     // EnsureInitialized can throw OOM
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    EnsureInitialized();

    _ASSERTE(GlobalCompletionPort != NULL);

    if (!InitCompletionPortThreadpool)
        InitCompletionPortThreadpool = TRUE;

    GrowCompletionPortThreadpoolIfNeeded();

    // In order to allow external ETW listeners to correlate activities that use our IO completion port
    // as a dispatch mechanism, we have to ensure the runtime's calls to ::PostQueuedCompletionStatus
    // and ::GetQueuedCompletionStatus are "annotated" with ETW events representing to operations
    // performed.
    // There are currently 2 codepaths that post to the GlobalCompletionPort:
    // 1. the managed API ThreadPool.UnsafeQueueNativeOverlapped(), calling CorPostQueuedCompletionStatus()
    //    which already fires the ETW event as needed
    // 2. the managed API ThreadPool.RegisterWaitForSingleObject which needs to fire the ETW event
    //    at the time the managed API is called (on the orignial user thread), and not when the ::PQCS
    //    is called (from the dedicated wait thread).
    // If additional codepaths appear they need to either fire the ETW event before calling this or ensure
    // we do not fire an unmatched "dequeue" event in ThreadpoolMgr::CompletionPortThreadStart
    // The current possible values for Function:
    //  - BindIoCompletionCallbackStub for ThreadPool.UnsafeQueueNativeOverlapped
    //  - WaitIOCompletionCallback for ThreadPool.RegisterWaitForSingleObject

    return ::PostQueuedCompletionStatus(GlobalCompletionPort,
                                        0,
                                        (ULONG_PTR) Function,
                                        lpOverlapped);
#else
    SetLastError(ERROR_CALL_NOT_IMPLEMENTED);
    return FALSE;
#endif // !TARGET_UNIX
}


void ThreadpoolMgr::WaitIOCompletionCallback(
    DWORD dwErrorCode,
    DWORD numBytesTransferred,
    LPOVERLAPPED lpOverlapped)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (dwErrorCode == ERROR_SUCCESS)
        DWORD ret = AsyncCallbackCompletion((PVOID)lpOverlapped);
}

#ifdef TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows

void WINAPI ThreadpoolMgr::ManagedWaitIOCompletionCallback(
    DWORD dwErrorCode,
    DWORD dwNumberOfBytesTransfered,
    LPOVERLAPPED lpOverlapped)
{
    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow();
        if (pThread == NULL)
        {
            return;
        }
    }

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (dwErrorCode != ERROR_SUCCESS)
    {
        return;
    }

    _ASSERTE(lpOverlapped != NULL);

    {
        GCX_COOP();
        ManagedThreadBase::ThreadPool(ManagedWaitIOCompletionCallback_Worker, lpOverlapped);
    }

    Thread::IncrementIOThreadPoolCompletionCount(pThread);
}

void ThreadpoolMgr::ManagedWaitIOCompletionCallback_Worker(LPVOID state)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(state != NULL);

    OBJECTHANDLE completeWaitWorkItemObjectHandle = (OBJECTHANDLE)state;
    OBJECTREF completeWaitWorkItemObject = ObjectFromHandle(completeWaitWorkItemObjectHandle);
    _ASSERTE(completeWaitWorkItemObject != NULL);

    GCPROTECT_BEGIN(completeWaitWorkItemObject);

    DestroyHandle(completeWaitWorkItemObjectHandle);
    completeWaitWorkItemObjectHandle = NULL;

    ARG_SLOT args[] = { ObjToArgSlot(completeWaitWorkItemObject) };
    MethodDescCallSite(METHOD__COMPLETE_WAIT_THREAD_POOL_WORK_ITEM__COMPLETE_WAIT, &completeWaitWorkItemObject).Call(args);

    GCPROTECT_END();
}

#endif // TARGET_WINDOWS

extern void WINAPI BindIoCompletionCallbackStub(DWORD ErrorCode,
                                            DWORD numBytesTransferred,
                                            LPOVERLAPPED lpOverlapped);


// This is either made by a worker thread or a CP thread
// indicated by threadTypeStatus
void ThreadpoolMgr::EnsureGateThreadRunning()
{
    LIMITED_METHOD_CONTRACT;

    if (UsePortableThreadPool())
    {
        GCX_COOP();
        MethodDescCallSite(METHOD__THREAD_POOL__ENSURE_GATE_THREAD_RUNNING).Call(NULL);
        return;
    }

    while (true)
    {
        switch (GateThreadStatus)
        {
        case GATE_THREAD_STATUS_REQUESTED:
            //
            // No action needed; the gate thread is running, and someone else has already registered a request
            // for it to stay.
            //
            return;

        case GATE_THREAD_STATUS_WAITING_FOR_REQUEST:
            //
            // Prevent the gate thread from exiting, if it hasn't already done so.  If it has, we'll create it on the next iteration of
            // this loop.
            //
            FastInterlockCompareExchange(&GateThreadStatus, GATE_THREAD_STATUS_REQUESTED, GATE_THREAD_STATUS_WAITING_FOR_REQUEST);
            break;

        case GATE_THREAD_STATUS_NOT_RUNNING:
            //
            // We need to create a new gate thread
            //
            if (FastInterlockCompareExchange(&GateThreadStatus, GATE_THREAD_STATUS_REQUESTED, GATE_THREAD_STATUS_NOT_RUNNING) == GATE_THREAD_STATUS_NOT_RUNNING)
            {
                if (!CreateGateThread())
                {
                    //
                    // If we failed to create the gate thread, someone else will need to try again later.
                    //
                    GateThreadStatus = GATE_THREAD_STATUS_NOT_RUNNING;
                }
                return;
            }
            break;

        default:
            _ASSERTE(!"Invalid value of ThreadpoolMgr::GateThreadStatus");
        }
    }
}

bool ThreadpoolMgr::NeedGateThreadForIOCompletions()
{
    LIMITED_METHOD_CONTRACT;

    if (!InitCompletionPortThreadpool)
    {
        return false;
    }

    ThreadCounter::Counts counts = CPThreadCounter.GetCleanCounts();
    return counts.NumActive <= counts.NumWorking;
}

bool ThreadpoolMgr::ShouldGateThreadKeepRunning()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());
    _ASSERTE(GateThreadStatus == GATE_THREAD_STATUS_WAITING_FOR_REQUEST ||
             GateThreadStatus == GATE_THREAD_STATUS_REQUESTED);

    //
    // Switch to WAITING_FOR_REQUEST, and see if we had a request since the last check.
    //
    LONG previousStatus = FastInterlockExchange(&GateThreadStatus, GATE_THREAD_STATUS_WAITING_FOR_REQUEST);

    if (previousStatus == GATE_THREAD_STATUS_WAITING_FOR_REQUEST)
    {
        //
        // No recent requests for the gate thread.  Check to see if we're still needed.
        //

        //
        // Are there any free threads in the I/O completion pool?  If there are, we don't need a gate thread.
        // This implies that whenever we decrement NumFreeCPThreads to 0, we need to call EnsureGateThreadRunning().
        //
        bool needGateThreadForCompletionPort = NeedGateThreadForIOCompletions();

        //
        // Are there any work requests in any worker queue?  If so, we need a gate thread.
        // This imples that whenever a work queue goes from empty to non-empty, we need to call EnsureGateThreadRunning().
        //
        bool needGateThreadForWorkerThreads = PerAppDomainTPCountList::AreRequestsPendingInAnyAppDomains();

        //
        // If worker tracking is enabled, we need to fire periodic ETW events with active worker counts.  This is
        // done by the gate thread.
        // We don't have to do anything special with EnsureGateThreadRunning() here, because this is only needed
        // once work has been added to the queue for the first time (which is covered above).
        //
        bool needGateThreadForWorkerTracking =
            0 != CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking);

        if (!(needGateThreadForCompletionPort ||
              needGateThreadForWorkerThreads ||
              needGateThreadForWorkerTracking))
        {
            //
            // It looks like we shouldn't be running.  But another thread may now tell us to run.  If so, they will set GateThreadStatus
            // back to GATE_THREAD_STATUS_REQUESTED.
            //
            previousStatus = FastInterlockCompareExchange(&GateThreadStatus, GATE_THREAD_STATUS_NOT_RUNNING, GATE_THREAD_STATUS_WAITING_FOR_REQUEST);
            if (previousStatus == GATE_THREAD_STATUS_WAITING_FOR_REQUEST)
                return false;
        }
    }


    _ASSERTE(GateThreadStatus == GATE_THREAD_STATUS_WAITING_FOR_REQUEST ||
             GateThreadStatus == GATE_THREAD_STATUS_REQUESTED);
    return true;
}



//************************************************************************
void ThreadpoolMgr::EnqueueWorkRequest(WorkRequest* workRequest)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    AppendWorkRequest(workRequest);
}

WorkRequest* ThreadpoolMgr::DequeueWorkRequest()
{
    WorkRequest* entry = NULL;
    CONTRACT(WorkRequest*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;

        POSTCONDITION(CheckPointer(entry, NULL_OK));
    } CONTRACT_END;

    _ASSERTE(!UsePortableThreadPool());

    entry = RemoveWorkRequest();

    RETURN entry;
}

void ThreadpoolMgr::ExecuteWorkRequest(bool* foundWork, bool* wasNotRecalled)
{
    CONTRACTL
    {
        THROWS;     // QueueUserWorkItem can throw
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    IPerAppDomainTPCount* pAdCount;

    LONG index = PerAppDomainTPCountList::GetAppDomainIndexForThreadpoolDispatch();

    if (index == 0)
    {
        *foundWork = false;
        *wasNotRecalled = true;
        return;
    }

    if (index == -1)
    {
        pAdCount = PerAppDomainTPCountList::GetUnmanagedTPCount();
    }
    else
    {

        pAdCount = PerAppDomainTPCountList::GetPerAppdomainCount(TPIndex((DWORD)index));
        _ASSERTE(pAdCount);
    }

    pAdCount->DispatchWorkItem(foundWork, wasNotRecalled);
}

//--------------------------------------------------------------------------
//This function informs the thread scheduler that the first requests has been
//queued on an appdomain, or it's the first unmanaged TP request.
//Arguments:
//         UnmanagedTP: Indicates that the request arises from the unmanaged
//part of Thread Pool.
//Assumptions:
//         This function must be called under a per-appdomain lock or the
//correct lock under unmanaged TP queue.
//
BOOL ThreadpoolMgr::SetAppDomainRequestsActive(BOOL UnmanagedTP)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    BOOL fShouldSignalEvent = FALSE;

    IPerAppDomainTPCount* pAdCount;

    if(UnmanagedTP)
    {
        pAdCount = PerAppDomainTPCountList::GetUnmanagedTPCount();
        _ASSERTE(pAdCount);
    }
    else
    {
        Thread* pCurThread = GetThread();
        _ASSERTE( pCurThread);

        AppDomain* pAppDomain = pCurThread->GetDomain();
        _ASSERTE(pAppDomain);

        TPIndex tpindex = pAppDomain->GetTPIndex();
        pAdCount = PerAppDomainTPCountList::GetPerAppdomainCount(tpindex);

        _ASSERTE(pAdCount);
    }

    pAdCount->SetAppDomainRequestsActive();

    return fShouldSignalEvent;
}

void ThreadpoolMgr::ClearAppDomainRequestsActive(BOOL UnmanagedTP, LONG id)
//--------------------------------------------------------------------------
//This function informs the thread scheduler that the kast request has been
//dequeued on an appdomain, or it's the last unmanaged TP request.
//Arguments:
//         UnmanagedTP: Indicates that the request arises from the unmanaged
//part of Thread Pool.
//         id: Indicates the id of the appdomain. The id is needed as this
//function can be called (indirectly) from the appdomain unload thread from
//unmanaged code to clear per-appdomain state during rude unload.
//Assumptions:
//         This function must be called under a per-appdomain lock or the
//correct lock under unmanaged TP queue.
//
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    IPerAppDomainTPCount* pAdCount;

    if(UnmanagedTP)
    {
        pAdCount = PerAppDomainTPCountList::GetUnmanagedTPCount();
        _ASSERTE(pAdCount);
    }
    else
    {
       Thread* pCurThread = GetThread();
       _ASSERTE( pCurThread);

       AppDomain* pAppDomain = pCurThread->GetDomain();
       _ASSERTE(pAppDomain);

       TPIndex tpindex = pAppDomain->GetTPIndex();

       pAdCount = PerAppDomainTPCountList::GetPerAppdomainCount(tpindex);

        _ASSERTE(pAdCount);
    }

    pAdCount->ClearAppDomainRequestsActive();
}


// Remove a block from the appropriate recycleList and return.
// If recycleList is empty, fall back to new.
LPVOID ThreadpoolMgr::GetRecycledMemory(enum MemType memType)
{
    LPVOID result = NULL;
    CONTRACT(LPVOID)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(result));
    } CONTRACT_END;

    if(RecycledLists.IsInitialized())
    {
        RecycledListInfo& list = RecycledLists.GetRecycleMemoryInfo( memType );

        result = list.Remove();
    }

    if(result == NULL)
    {
        switch (memType)
        {
            case MEMTYPE_DelegateInfo:
                result =  new DelegateInfo;
                break;
            case MEMTYPE_AsyncCallback:
                result =  new AsyncCallback;
                break;
            case MEMTYPE_WorkRequest:
                result =  new WorkRequest;
                break;
            default:
                _ASSERTE(!"Unknown Memtype");
                result = NULL;
                break;
        }
    }

    RETURN result;
}

// Insert freed block in recycle list. If list is full, return to system heap
void ThreadpoolMgr::RecycleMemory(LPVOID mem, enum MemType memType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(RecycledLists.IsInitialized())
    {
        RecycledListInfo& list = RecycledLists.GetRecycleMemoryInfo( memType );

        if(list.CanInsert())
        {
            list.Insert( mem );
            return;
        }
    }

    switch (memType)
    {
        case MEMTYPE_DelegateInfo:
            delete (DelegateInfo*) mem;
            break;
        case MEMTYPE_AsyncCallback:
            delete (AsyncCallback*) mem;
            break;
        case MEMTYPE_WorkRequest:
            delete (WorkRequest*) mem;
            break;
        default:
            _ASSERTE(!"Unknown Memtype");

    }
}

Thread* ThreadpoolMgr::CreateUnimpersonatedThread(LPTHREAD_START_ROUTINE lpStartAddress, LPVOID lpArgs, BOOL *pIsCLRThread)
{
    STATIC_CONTRACT_NOTHROW;
    if (GetThread()) { STATIC_CONTRACT_GC_TRIGGERS;} else {DISABLED(STATIC_CONTRACT_GC_NOTRIGGER);}
    STATIC_CONTRACT_MODE_ANY;
    /* cannot use contract because of SEH
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;*/

    Thread* pThread = NULL;

    if (g_fEEStarted) {
        *pIsCLRThread = TRUE;
    }
    else
        *pIsCLRThread = FALSE;
    if (*pIsCLRThread) {
        EX_TRY
        {
            pThread = SetupUnstartedThread();
        }
        EX_CATCH
        {
            pThread = NULL;
        }
        EX_END_CATCH(SwallowAllExceptions);
        if (pThread == NULL) {
            return NULL;
        }
    }
    DWORD threadId;
    BOOL bOK = FALSE;
    HANDLE threadHandle = NULL;

    if (*pIsCLRThread) {
        // CreateNewThread takes care of reverting any impersonation - so dont do anything here.
        bOK = pThread->CreateNewThread(0,               // default stack size
                                       lpStartAddress,
                                       lpArgs,           //arguments
                                       W(".NET ThreadPool Worker"));
    }
    else {
#ifndef TARGET_UNIX
        HandleHolder token;
        BOOL bReverted = FALSE;
        bOK = RevertIfImpersonated(&bReverted, &token);
        if (bOK != TRUE)
            return NULL;
#endif // !TARGET_UNIX
        threadHandle = CreateThread(NULL,               // security descriptor
                                    0,                  // default stack size
                                    lpStartAddress,
                                    lpArgs,
                                    CREATE_SUSPENDED,
                                    &threadId);

        SetThreadName(threadHandle, W(".NET ThreadPool Worker"));
#ifndef TARGET_UNIX
        UndoRevert(bReverted, token);
#endif // !TARGET_UNIX
    }

    if (*pIsCLRThread && !bOK)
    {
        pThread->DecExternalCount(FALSE);
        pThread = NULL;
    }

    if (*pIsCLRThread) {
        return pThread;
    }
    else
        return (Thread*)threadHandle;
}


BOOL ThreadpoolMgr::CreateWorkerThread()
{
    CONTRACTL
    {
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        NOTHROW;
        MODE_ANY;   // We may try to add a worker thread while queuing a work item thru an fcall
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    Thread *pThread;
    BOOL fIsCLRThread;
    if ((pThread = CreateUnimpersonatedThread(WorkerThreadStart, NULL, &fIsCLRThread)) != NULL)
    {
        if (fIsCLRThread) {
            pThread->ChooseThreadCPUGroupAffinity();
            pThread->StartThread();
        }
        else {
            DWORD status;
            status = ResumeThread((HANDLE)pThread);
            _ASSERTE(status != (DWORD) (-1));
            CloseHandle((HANDLE)pThread);          // we don't need this anymore
        }

        return TRUE;
    }

    return FALSE;
}


DWORD WINAPI ThreadpoolMgr::WorkerThreadStart(LPVOID lpArgs)
{
    ClrFlsSetThreadType (ThreadType_Threadpool_Worker);

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE_ALL_BUILDS(__FILE__, !UsePortableThreadPool());

    Thread *pThread = NULL;
    DWORD dwSwitchCount = 0;
    BOOL fThreadInit = FALSE;

    ThreadCounter::Counts counts, oldCounts, newCounts;
    bool foundWork = true, wasNotRecalled = true;

    counts = WorkerCounter.GetCleanCounts();
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolWorkerThreadStart))
        FireEtwThreadPoolWorkerThreadStart(counts.NumActive, counts.NumRetired, GetClrInstanceId());

#ifdef FEATURE_COMINTEROP
    BOOL fCoInited = FALSE;
    // Threadpool threads should be initialized as MTA. If we are unable to do so,
    // return failure.
    {
        fCoInited = SUCCEEDED(::CoInitializeEx(NULL, COINIT_MULTITHREADED));
        if (!fCoInited)
        {
            goto Exit;
        }
    }
#endif // FEATURE_COMINTEROP
Work:

    if (!fThreadInit) {
        if (g_fEEStarted) {
            pThread = SetupThreadNoThrow();
            if (pThread == NULL) {
                __SwitchToThread(0, ++dwSwitchCount);
                goto Work;
            }

            // converted to CLRThread and added to ThreadStore, pick an group affinity for this thread
            pThread->ChooseThreadCPUGroupAffinity();

            #ifdef FEATURE_COMINTEROP
            if (pThread->SetApartment(Thread::AS_InMTA) != Thread::AS_InMTA)
            {
                // counts volatile read paired with CompareExchangeCounts loop set
                counts = WorkerCounter.DangerousGetDirtyCounts();
                while (true)
                {
                    newCounts = counts;
                    newCounts.NumActive--;
                    newCounts.NumWorking--;
                    oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);
                    if (oldCounts == counts)
                        break;
                    counts = oldCounts;
                }
                goto Exit;
            }
            #endif // FEATURE_COMINTEROP

            pThread->SetBackground(TRUE);
            fThreadInit = TRUE;
        }
    }

    GCX_PREEMP_NO_DTOR();
    _ASSERTE(pThread == NULL || !pThread->PreemptiveGCDisabled());

    // make sure there's really work.  If not, go back to sleep

    // counts volatile read paired with CompareExchangeCounts loop set
    counts = WorkerCounter.DangerousGetDirtyCounts();
    while (true)
    {
        _ASSERTE(counts.NumActive > 0);
        _ASSERTE(counts.NumWorking > 0);

        newCounts = counts;

        bool retired;

        if (counts.NumActive > counts.MaxWorking)
        {
            newCounts.NumActive--;
            newCounts.NumRetired++;
            retired = true;
        }
        else
        {
            retired = false;

            if (foundWork)
                break;
        }

        newCounts.NumWorking--;

        oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);

        if (oldCounts == counts)
        {
            if (retired)
                goto Retire;
            else
                goto WaitForWork;
        }

        counts = oldCounts;
    }

    if (GCHeapUtilities::IsGCInProgress(TRUE))
    {
        // GC is imminent, so wait until GC is complete before executing next request.
        // this reduces in-flight objects allocated right before GC, easing the GC's work
        GCHeapUtilities::WaitForGCCompletion(TRUE);
    }

    {
        ThreadpoolMgr::UpdateLastDequeueTime();
        ThreadpoolMgr::ExecuteWorkRequest(&foundWork, &wasNotRecalled);
    }

    if (foundWork)
    {
        // Reset TLS etc. for next WorkRequest.
        if (pThread == NULL)
            pThread = GetThread();

        if (pThread)
        {
            _ASSERTE(!pThread->IsAbortRequested());
            pThread->InternalReset();
        }
    }

    if (wasNotRecalled)
        goto Work;

Retire:

    counts = WorkerCounter.GetCleanCounts();
    FireEtwThreadPoolWorkerThreadRetirementStart(counts.NumActive, counts.NumRetired, GetClrInstanceId());

    // It's possible that some work came in just before we decremented the active thread count, in which
    // case whoever queued that work may be expecting us to pick it up - so they would not have signalled
    // the worker semaphore.  If there are other threads waiting, they will never be woken up, because
    // whoever queued the work expects that it's already been picked up.  The solution is to signal the semaphore
    // if there's any work available.
    if (PerAppDomainTPCountList::AreRequestsPendingInAnyAppDomains())
        MaybeAddWorkingWorker();

    while (true)
    {
RetryRetire:
        if (RetiredWorkerSemaphore->Wait(WorkerTimeout))
        {
            foundWork = true;

            counts = WorkerCounter.GetCleanCounts();
            FireEtwThreadPoolWorkerThreadRetirementStop(counts.NumActive, counts.NumRetired, GetClrInstanceId());
            goto Work;
        }

        if (!IsIoPending())
        {
            //
            // We're going to exit.  There's a nasty race here.  We're about to decrement NumRetired,
            // since we're going to exit.  Once we've done that, nobody will expect this thread
            // to be waiting for RetiredWorkerSemaphore.  But between now and then, other threads still
            // think we're waiting on the semaphore, and they will happily do the following to try to
            // wake us up:
            //
            // 1) Decrement NumRetired
            // 2) Increment NumActive
            // 3) Increment NumWorking
            // 4) Signal RetiredWorkerSemaphore
            //
            // We will not receive that signal.  If we don't do something special here,
            // we will decrement NumRetired an extra time, and leave the world thinking there
            // are fewer retired threads, and more working threads than reality.
            //
            // What can we do about this?  First, we *need* to decrement NumRetired.  If someone did it before us,
            // it might go negative.  This is the easiest way to tell that we've encountered this race.  In that case,
            // we will simply not commit the decrement, swallow the signal that was sent, and proceed as if we
            // got WAIT_OBJECT_0 in the wait above.
            //
            // If we don't hit zero while decrementing NumRetired, we still may have encountered this race.  But
            // if we don't hit zero, then there's another retired thread that will pick up this signal.  So it's ok
            // to exit.
            //

            // counts volatile read paired with CompareExchangeCounts loop set
            counts = WorkerCounter.DangerousGetDirtyCounts();
            while (true)
            {
                if (counts.NumRetired == 0)
                    goto RetryRetire;

                newCounts = counts;
                newCounts.NumRetired--;

                oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);
                if (oldCounts == counts)
                {
                    counts = newCounts;
                    break;
                }
                counts = oldCounts;
            }

            FireEtwThreadPoolWorkerThreadRetirementStop(counts.NumActive, counts.NumRetired, GetClrInstanceId());
            goto Exit;
        }
    }

WaitForWork:

    // It's possible that we decided we had no work just before some work came in,
    // but reduced the worker count *after* the work came in.  In this case, we might
    // miss the notification of available work.  So we make a sweep through the ADs here,
    // and wake up a thread (maybe this one!) if there is work to do.
    if (PerAppDomainTPCountList::AreRequestsPendingInAnyAppDomains())
    {
        foundWork = true;
        MaybeAddWorkingWorker();
    }

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolWorkerThreadWait))
        FireEtwThreadPoolWorkerThreadWait(counts.NumActive, counts.NumRetired, GetClrInstanceId());

RetryWaitForWork:
    if (WorkerSemaphore->Wait(WorkerTimeout, WorkerThreadSpinLimit, NumberOfProcessors))
    {
        foundWork = true;
        goto Work;
    }

    if (!IsIoPending())
    {
        //
        // We timed out, and are about to exit.  This puts us in a very similar situation to the
        // retirement case above - someone may think we're still waiting, and go ahead and:
        //
        // 1) Increment NumWorking
        // 2) Signal WorkerSemaphore
        //
        // The solution is much like retirement; when we're decrementing NumActive, we need to make
        // sure it doesn't drop below NumWorking.  If it would, then we need to go back and wait
        // again.
        //

        DangerousNonHostedSpinLockHolder tal(&ThreadAdjustmentLock);

        // counts volatile read paired with CompareExchangeCounts loop set
        counts = WorkerCounter.DangerousGetDirtyCounts();
        while (true)
        {
            if (counts.NumActive == counts.NumWorking)
            {
                goto RetryWaitForWork;
            }

            newCounts = counts;
            newCounts.NumActive--;

            // if we timed out while active, then Hill Climbing needs to be told that we need fewer threads
            newCounts.MaxWorking = max(MinLimitTotalWorkerThreads, min(newCounts.NumActive, newCounts.MaxWorking));

            oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);

            if (oldCounts == counts)
            {
                HillClimbingInstance.ForceChange(newCounts.MaxWorking, ThreadTimedOut);
                goto Exit;
            }

            counts = oldCounts;
        }
    }
    else
    {
        goto RetryWaitForWork;
    }

Exit:

#ifdef FEATURE_COMINTEROP
    if (pThread) {
        pThread->SetApartment(Thread::AS_Unknown);
        pThread->CoUninitialize();
    }

    // Couninit the worker thread
    if (fCoInited)
    {
        CoUninitialize();
    }
#endif

    if (pThread) {
        pThread->ClearThreadCPUGroupAffinity();

        DestroyThread(pThread);
    }

    _ASSERTE(!IsIoPending());

    counts = WorkerCounter.GetCleanCounts();
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolWorkerThreadStop))
        FireEtwThreadPoolWorkerThreadStop(counts.NumActive, counts.NumRetired, GetClrInstanceId());

    return ERROR_SUCCESS;
}

// this should only be called by unmanaged thread (i.e. there should be no mgd
// caller on the stack) since we are swallowing terminal exceptions
DWORD ThreadpoolMgr::SafeWait(CLREvent * ev, DWORD sleepTime, BOOL alertable)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    /* cannot use contract because of SEH
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;*/

    DWORD status = WAIT_TIMEOUT;
    EX_TRY
    {
        status = ev->Wait(sleepTime,FALSE);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)
    return status;
}

/************************************************************************/

BOOL ThreadpoolMgr::RegisterWaitForSingleObject(PHANDLE phNewWaitObject,
                                                HANDLE hWaitObject,
                                                WAITORTIMERCALLBACK Callback,
                                                PVOID Context,
                                                ULONG timeout,
                                                DWORD dwFlag )
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    EnsureInitialized();

    ThreadCB* threadCB;
    {
        CrstHolder csh(&WaitThreadsCriticalSection);

        threadCB = FindWaitThread();
    }

    *phNewWaitObject = NULL;

    if (threadCB)
    {
        WaitInfo* waitInfo = new (nothrow) WaitInfo;

        if (waitInfo == NULL)
            return FALSE;

        waitInfo->waitHandle = hWaitObject;
        waitInfo->Callback = Callback;
        waitInfo->Context = Context;
        waitInfo->timeout = timeout;
        waitInfo->flag = dwFlag;
        waitInfo->threadCB = threadCB;
        waitInfo->state = 0;
        waitInfo->refCount = 1;     // safe to do this since no wait has yet been queued, so no other thread could be modifying this
        waitInfo->ExternalCompletionEvent = INVALID_HANDLE;
        waitInfo->ExternalEventSafeHandle = NULL;

        waitInfo->timer.startTime = GetTickCount();
        waitInfo->timer.remainingTime = timeout;

        *phNewWaitObject = waitInfo;

        // We fire the "enqueue" ETW event here, to "mark" the thread that had called the API, rather than the
        // thread that will PostQueuedCompletionStatus (the dedicated WaitThread).
        // This event correlates with ThreadPoolIODequeue in ThreadpoolMgr::AsyncCallbackCompletion
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolIOEnqueue))
            FireEtwThreadPoolIOEnqueue((LPOVERLAPPED)waitInfo, reinterpret_cast<void*>(Callback), (dwFlag & WAIT_SINGLE_EXECUTION) == 0, GetClrInstanceId());

        BOOL status = QueueUserAPC((PAPCFUNC)InsertNewWaitForSelf, threadCB->threadHandle, (size_t) waitInfo);

        if (status == FALSE)
        {
            *phNewWaitObject = NULL;
            delete waitInfo;
        }

        return status;
    }

    return FALSE;
}


// Returns a wait thread that can accomodate another wait request. The
// caller is responsible for synchronizing access to the WaitThreadsHead
ThreadpoolMgr::ThreadCB* ThreadpoolMgr::FindWaitThread()
{
    CONTRACTL
    {
        THROWS;     // CreateWaitThread can throw
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    do
    {
        for (LIST_ENTRY* Node = (LIST_ENTRY*) WaitThreadsHead.Flink ;
             Node != &WaitThreadsHead ;
             Node = (LIST_ENTRY*)Node->Flink)
        {
            _ASSERTE(offsetof(WaitThreadInfo,link) == 0);

            ThreadCB*  threadCB = ((WaitThreadInfo*) Node)->threadCB;

            if (threadCB->NumWaitHandles < MAX_WAITHANDLES)         // this test and following ...

            {
                InterlockedIncrement(&threadCB->NumWaitHandles);    // ... increment are protected by WaitThreadsCriticalSection.
                                                                    // but there might be a concurrent decrement in DeactivateWait
                                                                    // or InsertNewWaitForSelf, hence the interlock
                return threadCB;
            }
        }

        // if reached here, there are no wait threads available, so need to create a new one
        if (!CreateWaitThread())
            return NULL;


        // Now loop back
    } while (TRUE);

}

BOOL ThreadpoolMgr::CreateWaitThread()
{
    CONTRACTL
    {
        THROWS; // CLREvent::CreateAutoEvent can throw OOM
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());

    DWORD threadId;

    if (g_fEEShutDown & ShutDown_Finalize2){
        // The process is shutting down.  Shutdown thread has ThreadStore lock,
        // wait thread is blocked on the lock.
        return FALSE;
    }

    NewHolder<WaitThreadInfo> waitThreadInfo(new (nothrow) WaitThreadInfo);
    if (waitThreadInfo == NULL)
        return FALSE;

    NewHolder<ThreadCB> threadCB(new (nothrow) ThreadCB);

    if (threadCB == NULL)
    {
        return FALSE;
    }

    threadCB->startEvent.CreateAutoEvent(FALSE);
    HANDLE threadHandle = Thread::CreateUtilityThread(Thread::StackSize_Small, WaitThreadStart, (LPVOID)threadCB, W(".NET ThreadPool Wait"), CREATE_SUSPENDED, &threadId);

    if (threadHandle == NULL)
    {
        threadCB->startEvent.CloseEvent();
        return FALSE;
    }

    waitThreadInfo.SuppressRelease();
    threadCB.SuppressRelease();
    threadCB->threadHandle = threadHandle;
    threadCB->threadId = threadId;              // may be useful for debugging otherwise not used
    threadCB->NumWaitHandles = 0;
    threadCB->NumActiveWaits = 0;
    for (int i=0; i< MAX_WAITHANDLES; i++)
    {
        InitializeListHead(&(threadCB->waitPointer[i]));
    }

    waitThreadInfo->threadCB = threadCB;

    DWORD status = ResumeThread(threadHandle);

    {
        // We will QueueUserAPC on the newly created thread.
        // Let us wait until the thread starts running.
        GCX_PREEMP();
        DWORD timeout=500;
        while (TRUE) {
            if (g_fEEShutDown & ShutDown_Finalize2){
                // The process is shutting down.  Shutdown thread has ThreadStore lock,
                // wait thread is blocked on the lock.
                return FALSE;
            }
            DWORD wait_status = threadCB->startEvent.Wait(timeout, FALSE);
            if (wait_status == WAIT_OBJECT_0) {
                break;
            }
        }
    }
    threadCB->startEvent.CloseEvent();

    // check to see if setup succeeded
    if (threadCB->threadHandle == NULL)
        return FALSE;

    InsertHeadList(&WaitThreadsHead,&waitThreadInfo->link);

    _ASSERTE(status != (DWORD) (-1));

    return (status != (DWORD) (-1));

}

// Executed as an APC on a WaitThread. Add the wait specified in pArg to the list of objects it is waiting on
void ThreadpoolMgr::InsertNewWaitForSelf(WaitInfo* pArgs)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    WaitInfo* waitInfo = pArgs;

    // the following is safe since only this thread is allowed to change the state
    if (!(waitInfo->state & WAIT_DELETE))
    {
        waitInfo->state =  (WAIT_REGISTERED | WAIT_ACTIVE);
    }
    else
    {
        // some thread unregistered the wait
        DeleteWait(waitInfo);
        return;
    }


    ThreadCB* threadCB = waitInfo->threadCB;

    _ASSERTE(threadCB->NumActiveWaits <= threadCB->NumWaitHandles);

    int index = FindWaitIndex(threadCB, waitInfo->waitHandle);
    _ASSERTE(index >= 0 && index <= threadCB->NumActiveWaits);

    if (index == threadCB->NumActiveWaits)
    {
        threadCB->waitHandle[threadCB->NumActiveWaits] = waitInfo->waitHandle;
        threadCB->NumActiveWaits++;
    }
    else
    {
        // this is a duplicate waithandle, so the increment in FindWaitThread
        // wasn't strictly necessary.  This will avoid unnecessary thread creation.
        InterlockedDecrement(&threadCB->NumWaitHandles);
    }

    _ASSERTE(offsetof(WaitInfo, link) == 0);
    InsertTailList(&(threadCB->waitPointer[index]), (&waitInfo->link));

    return;
}

// returns the index of the entry that matches waitHandle or next free entry if not found
int ThreadpoolMgr::FindWaitIndex(const ThreadCB* threadCB, const HANDLE waitHandle)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    for (int i=0;i<threadCB->NumActiveWaits; i++)
        if (threadCB->waitHandle[i] == waitHandle)
            return i;

    // else not found
    return threadCB->NumActiveWaits;
}


// if no wraparound that the timer is expired if duetime is less than current time
// if wraparound occurred, then the timer expired if dueTime was greater than last time or dueTime is less equal to current time
#define TimeExpired(last,now,duetime) ((last) <= (now) ? \
                                       ((duetime) <= (now) && (duetime) >= (last)): \
                                       ((duetime) >= (last) || (duetime) <= (now)))

#define TimeInterval(end,start) ((end) > (start) ? ((end) - (start)) : ((0xffffffff - (start)) + (end) + 1))

// Returns the minimum of the remaining time to reach a timeout among all the waits
DWORD ThreadpoolMgr::MinimumRemainingWait(LIST_ENTRY* waitInfo, unsigned int numWaits)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    unsigned int min = (unsigned int) -1;
    DWORD currentTime = GetTickCount();

    for (unsigned i=0; i < numWaits ; i++)
    {
        WaitInfo* waitInfoPtr = (WaitInfo*) (waitInfo[i].Flink);
        PVOID waitInfoHead = &(waitInfo[i]);
        do
        {
            if (waitInfoPtr->timeout != INFINITE)
            {
                // compute remaining time
                DWORD elapsedTime = TimeInterval(currentTime,waitInfoPtr->timer.startTime );

                __int64 remainingTime = (__int64) (waitInfoPtr->timeout) - (__int64) elapsedTime;

                // update remaining time
                waitInfoPtr->timer.remainingTime =  remainingTime > 0 ? (int) remainingTime : 0;

                // ... and min
                if (waitInfoPtr->timer.remainingTime < min)
                    min = waitInfoPtr->timer.remainingTime;
            }

            waitInfoPtr = (WaitInfo*) (waitInfoPtr->link.Flink);

        } while ((PVOID) waitInfoPtr != waitInfoHead);

    }
    return min;
}

#ifdef _MSC_VER
#ifdef HOST_64BIT
#pragma warning (disable : 4716)
#else
#pragma warning (disable : 4715)
#endif
#endif
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22008) // "Prefast integer overflow check on (0 + lval) is bogus.  Tried local disable without luck, doing whole method."
#endif

DWORD WINAPI ThreadpoolMgr::WaitThreadStart(LPVOID lpArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ClrFlsSetThreadType (ThreadType_Wait);

    _ASSERTE_ALL_BUILDS(__FILE__, !UsePortableThreadPool());

    ThreadCB* threadCB = (ThreadCB*) lpArgs;
    Thread* pThread = SetupThreadNoThrow();

    if (pThread == NULL)
    {
        _ASSERTE(threadCB->threadHandle != NULL);
        threadCB->threadHandle = NULL;
    }

    threadCB->startEvent.Set();

    if (pThread == NULL)
    {
        return 0;
    }

    {
        // wait threads never die. (Why?)
        for (;;)
        {
            DWORD status;
            DWORD timeout = 0;

            if (threadCB->NumActiveWaits == 0)
            {

#undef SleepEx
                // <TODO>@TODO Consider doing a sleep for an idle period and terminating the thread if no activity</TODO>
        //We use SleepEx instead of CLRSLeepEx because CLRSleepEx calls into SQL(or other hosts) in hosted
        //scenarios. SQL does not deliver APC's, and the waithread wait insertion/deletion logic depends on
        //APC's being delivered.
                status = SleepEx(INFINITE,TRUE);
#define SleepEx(a,b) Dont_Use_SleepEx(a,b)

                _ASSERTE(status == WAIT_IO_COMPLETION);
            }
            else if (IsWaitThreadAPCPending())
            {
                //Do a sleep if an APC is pending, This was done to solve the corner case where the wait is signaled,
                //and APC to deregiter the wait never fires. That scenario leads to an infinite loop. This check would
                //allow the thread to enter alertable wait and thus cause the APC to fire.

                ResetWaitThreadAPCPending();

                //We use SleepEx instead of CLRSLeepEx because CLRSleepEx calls into SQL(or other hosts) in hosted
                //scenarios. SQL does not deliver APC's, and the waithread wait insertion/deletion logic depends on
                //APC's being delivered.

                #undef SleepEx
                status = SleepEx(0,TRUE);
                #define SleepEx(a,b) Dont_Use_SleepEx(a,b)

                continue;
            }
            else
            {
                // compute minimum timeout. this call also updates the remainingTime field for each wait
                timeout = MinimumRemainingWait(threadCB->waitPointer,threadCB->NumActiveWaits);

                status = WaitForMultipleObjectsEx(  threadCB->NumActiveWaits,
                                                    threadCB->waitHandle,
                                                    FALSE,                      // waitall
                                                    timeout,
                                                    TRUE  );                    // alertable

                _ASSERTE( (status == WAIT_TIMEOUT) ||
                          (status == WAIT_IO_COMPLETION) ||
                          //It could be that there are no waiters at this point,
                          //as the APC to deregister the wait may have run.
                          (status == WAIT_OBJECT_0) ||
                          (status >= WAIT_OBJECT_0 && status < (DWORD)(WAIT_OBJECT_0 + threadCB->NumActiveWaits))  ||
                          (status == WAIT_FAILED));

                //It could be that the last waiter also got deregistered.
                if (threadCB->NumActiveWaits == 0)
                {
                    continue;
                }
            }

            if (status == WAIT_IO_COMPLETION)
                continue;

            if (status == WAIT_TIMEOUT)
            {
                for (int i=0; i< threadCB->NumActiveWaits; i++)
                {
                    WaitInfo* waitInfo = (WaitInfo*) (threadCB->waitPointer[i]).Flink;
                    PVOID waitInfoHead = &(threadCB->waitPointer[i]);

                    do
                    {
                        _ASSERTE(waitInfo->timer.remainingTime >= timeout);

                        WaitInfo* wTemp = (WaitInfo*) waitInfo->link.Flink;

                        if (waitInfo->timer.remainingTime == timeout)
                        {
                            ProcessWaitCompletion(waitInfo,i,TRUE);
                        }

                        waitInfo = wTemp;

                    } while ((PVOID) waitInfo != waitInfoHead);
                }
            }
            else if (status >= WAIT_OBJECT_0 && status < (DWORD)(WAIT_OBJECT_0 + threadCB->NumActiveWaits))
            {
                unsigned index = status - WAIT_OBJECT_0;
                WaitInfo* waitInfo = (WaitInfo*) (threadCB->waitPointer[index]).Flink;
                PVOID waitInfoHead = &(threadCB->waitPointer[index]);
                BOOL isAutoReset;

                // Setting to unconditional TRUE is inefficient since we will re-enter the wait and release
                // the next waiter, but short of using undocumented NT apis is the only solution.
                // Querying the state with a WaitForSingleObject is not an option as it will reset an
                // auto reset event if it has been signalled since the previous wait.
                isAutoReset = TRUE;

                do
                {
                    WaitInfo* wTemp = (WaitInfo*) waitInfo->link.Flink;
                    ProcessWaitCompletion(waitInfo,index,FALSE);

                    waitInfo = wTemp;

                } while (((PVOID) waitInfo != waitInfoHead) && !isAutoReset);

                // If an app registers a recurring wait for an event that is always signalled (!),
                // then no apc's will be executed since the thread never enters the alertable state.
                // This can be fixed by doing the following:
                //     SleepEx(0,TRUE);
                // However, it causes an unnecessary context switch. It is not worth penalizing well
                // behaved apps to protect poorly written apps.


            }
            else
            {
                _ASSERTE(status == WAIT_FAILED);
                // wait failed: application error
                // find out which wait handle caused the wait to fail
                for (int i = 0; i < threadCB->NumActiveWaits; i++)
                {
                    DWORD subRet = WaitForSingleObject(threadCB->waitHandle[i], 0);

                    if (subRet != WAIT_FAILED)
                        continue;

                    // remove all waits associated with this wait handle

                    WaitInfo* waitInfo = (WaitInfo*) (threadCB->waitPointer[i]).Flink;
                    PVOID waitInfoHead = &(threadCB->waitPointer[i]);

                    do
                    {
                        WaitInfo* temp  = (WaitInfo*) waitInfo->link.Flink;

                        DeactivateNthWait(waitInfo,i);


                // Note, we cannot cleanup here since there is no way to suppress finalization
                // we will just leak, and rely on the finalizer to clean up the memory
                        //if (InterlockedDecrement(&waitInfo->refCount) == 0)
                        //    DeleteWait(waitInfo);


                        waitInfo = temp;

                    } while ((PVOID) waitInfo != waitInfoHead);

                    break;
                }
            }
        }
    }

    //This is unreachable...so no return required.
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#ifdef _MSC_VER
#ifdef HOST_64BIT
#pragma warning (default : 4716)
#else
#pragma warning (default : 4715)
#endif
#endif

void ThreadpoolMgr::ProcessWaitCompletion(WaitInfo* waitInfo,
                                          unsigned index,
                                          BOOL waitTimedOut
                                         )
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    /* cannot use contract because of SEH
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;*/

    AsyncCallback* asyncCallback = NULL;
    EX_TRY{
        if ( waitInfo->flag & WAIT_SINGLE_EXECUTION)
        {
            DeactivateNthWait (waitInfo,index) ;
        }
        else
        {   // reactivate wait by resetting timer
            waitInfo->timer.startTime = GetTickCount();
        }

        asyncCallback = MakeAsyncCallback();
        if (asyncCallback)
        {
            asyncCallback->wait = waitInfo;
            asyncCallback->waitTimedOut = waitTimedOut;

            InterlockedIncrement(&waitInfo->refCount);

#ifndef TARGET_UNIX
            if (FALSE == PostQueuedCompletionStatus((LPOVERLAPPED)asyncCallback, (LPOVERLAPPED_COMPLETION_ROUTINE)WaitIOCompletionCallback))
#else  // TARGET_UNIX
            if (FALSE == QueueUserWorkItem(AsyncCallbackCompletion, asyncCallback, QUEUE_ONLY))
#endif // !TARGET_UNIX
                ReleaseAsyncCallback(asyncCallback);
        }
    }
    EX_CATCH {
        if (asyncCallback)
            ReleaseAsyncCallback(asyncCallback);

        EX_RETHROW;
    }
    EX_END_CATCH(SwallowAllExceptions);
}


DWORD WINAPI ThreadpoolMgr::AsyncCallbackCompletion(PVOID pArgs)
{
    CONTRACTL
    {
        THROWS;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();

    if (pThread == NULL)
    {
        HRESULT hr = ERROR_SUCCESS;

        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow(&hr);

        if (pThread == NULL)
        {
            return hr;
        }
    }

    {
        AsyncCallback * asyncCallback = (AsyncCallback*) pArgs;

        WaitInfo * waitInfo = asyncCallback->wait;

        AsyncCallbackHolder asyncCBHolder;
        asyncCBHolder.Assign(asyncCallback);

        // We fire the "dequeue" ETW event here, before executing the user code, to enable correlation with
        // the ThreadPoolIOEnqueue fired in ThreadpoolMgr::RegisterWaitForSingleObject
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolIODequeue))
            FireEtwThreadPoolIODequeue(waitInfo, reinterpret_cast<void*>(waitInfo->Callback), GetClrInstanceId());

        // the user callback can throw, the host must be prepared to handle it.
        // SQL is ok, since they have a top-level SEH handler. However, there's
        // no easy way to verify it

        ((WAITORTIMERCALLBACKFUNC) waitInfo->Callback)
                                    ( waitInfo->Context, asyncCallback->waitTimedOut != FALSE);

#ifndef TARGET_UNIX
        Thread::IncrementIOThreadPoolCompletionCount(pThread);
#endif
    }

    return ERROR_SUCCESS;
}

void ThreadpoolMgr::DeactivateWait(WaitInfo* waitInfo)
{
    LIMITED_METHOD_CONTRACT;

    ThreadCB* threadCB = waitInfo->threadCB;
    DWORD endIndex = threadCB->NumActiveWaits-1;
    DWORD index;

    for (index = 0;  index <= endIndex; index++)
    {
        LIST_ENTRY* head = &(threadCB->waitPointer[index]);
        LIST_ENTRY* current = head;
        do {
            if (current->Flink == (PVOID) waitInfo)
                goto FOUND;

            current = (LIST_ENTRY*) current->Flink;

        } while (current != head);
    }

FOUND:
    _ASSERTE(index <= endIndex);

    DeactivateNthWait(waitInfo, index);
}


void ThreadpoolMgr::DeactivateNthWait(WaitInfo* waitInfo, DWORD index)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    ThreadCB* threadCB = waitInfo->threadCB;

    if (waitInfo->link.Flink != waitInfo->link.Blink)
    {
        RemoveEntryList(&(waitInfo->link));
    }
    else
    {

        ULONG EndIndex = threadCB->NumActiveWaits -1;

        // Move the remaining ActiveWaitArray left.

        ShiftWaitArray( threadCB, index+1, index,EndIndex - index ) ;

        // repair the blink and flink of the first and last elements in the list
        for (unsigned int i = 0; i< EndIndex-index; i++)
        {
            WaitInfo* firstWaitInfo = (WaitInfo*) threadCB->waitPointer[index+i].Flink;
            WaitInfo* lastWaitInfo = (WaitInfo*) threadCB->waitPointer[index+i].Blink;
            firstWaitInfo->link.Blink =  &(threadCB->waitPointer[index+i]);
            lastWaitInfo->link.Flink =  &(threadCB->waitPointer[index+i]);
        }
        // initialize the entry just freed
        InitializeListHead(&(threadCB->waitPointer[EndIndex]));

        threadCB->NumActiveWaits-- ;
        InterlockedDecrement(&threadCB->NumWaitHandles);
    }

    waitInfo->state &= ~WAIT_ACTIVE ;

}

void ThreadpoolMgr::DeleteWait(WaitInfo* waitInfo)
{
    CONTRACTL
    {
        if (waitInfo->ExternalEventSafeHandle != NULL) { THROWS;} else { NOTHROW; }
        MODE_ANY;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    if(waitInfo->Context && (waitInfo->flag & WAIT_FREE_CONTEXT)) {
        DelegateInfo* pDelegate = (DelegateInfo*) waitInfo->Context;

        // Since the delegate release destroys a handle, we need to be in
        // co-operative mode
        {
            GCX_COOP();
            pDelegate->Release();
        }

        RecycleMemory( pDelegate, MEMTYPE_DelegateInfo );
    }

    if (waitInfo->flag & WAIT_INTERNAL_COMPLETION)
    {
        waitInfo->InternalCompletionEvent.Set();
        return;  // waitInfo will be deleted by the thread that's waiting on this event
    }
    else if (waitInfo->ExternalCompletionEvent != INVALID_HANDLE)
    {
        SetEvent(waitInfo->ExternalCompletionEvent);
    }
    else if (waitInfo->ExternalEventSafeHandle != NULL)
    {
        // Release the safe handle and the GC handle holding it
        ReleaseWaitInfo(waitInfo);
    }

    delete waitInfo;


}



/************************************************************************/
BOOL ThreadpoolMgr::UnregisterWaitEx(HANDLE hWaitObject,HANDLE Event)
{
    CONTRACTL
    {
        THROWS; //NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());
    _ASSERTE(IsInitialized());              // cannot call unregister before first registering

    const BOOL Blocking = (Event == (HANDLE) -1);
    WaitInfo* waitInfo = (WaitInfo*) hWaitObject;

    if (!hWaitObject)
    {
        return FALSE;
    }

    // we do not allow callbacks to run in the wait thread, hence the assert
    _ASSERTE(GetCurrentThreadId() != waitInfo->threadCB->threadId);


    if (Blocking)
    {
        waitInfo->InternalCompletionEvent.CreateAutoEvent(FALSE);
        waitInfo->flag |= WAIT_INTERNAL_COMPLETION;

    }
    else
    {
        waitInfo->ExternalCompletionEvent = (Event ? Event : INVALID_HANDLE);
        _ASSERTE((waitInfo->flag & WAIT_INTERNAL_COMPLETION) == 0);
        // we still want to block until the wait has been deactivated
        waitInfo->PartialCompletionEvent.CreateAutoEvent(FALSE);
    }

    BOOL status = QueueDeregisterWait(waitInfo->threadCB->threadHandle, waitInfo);


    if (status == 0)
    {
        STRESS_LOG1(LF_THREADPOOL, LL_ERROR, "Queue APC failed in UnregisterWaitEx %x", status);

        if (Blocking)
            waitInfo->InternalCompletionEvent.CloseEvent();
        else
            waitInfo->PartialCompletionEvent.CloseEvent();
        return FALSE;
    }

    if (!Blocking)
    {
        waitInfo->PartialCompletionEvent.Wait(INFINITE,TRUE);
        waitInfo->PartialCompletionEvent.CloseEvent();
        // we cannot do DeleteWait in DeregisterWait, since the DeleteWait could happen before
        // we close the event. So, the code has been moved here.
        if (InterlockedDecrement(&waitInfo->refCount) == 0)
        {
            DeleteWait(waitInfo);
        }
    }

    else        // i.e. blocking
    {
        _ASSERTE(waitInfo->flag & WAIT_INTERNAL_COMPLETION);
        _ASSERTE(waitInfo->ExternalEventSafeHandle == NULL);

        waitInfo->InternalCompletionEvent.Wait(INFINITE,TRUE);
        waitInfo->InternalCompletionEvent.CloseEvent();
        delete waitInfo;  // if WAIT_INTERNAL_COMPLETION is not set, waitInfo will be deleted in DeleteWait
    }
    return TRUE;
}


void ThreadpoolMgr::DeregisterWait(WaitInfo* pArgs)
{
    WRAPPER_NO_CONTRACT;

    WaitInfo* waitInfo = pArgs;

    if ( ! (waitInfo->state & WAIT_REGISTERED) )
    {
        // set state to deleted, so that it does not get registered
        waitInfo->state |= WAIT_DELETE ;

        // since the wait has not even been registered, we dont need an interlock to decrease the RefCount
        waitInfo->refCount--;

        if (waitInfo->PartialCompletionEvent.IsValid())
        {
            waitInfo->PartialCompletionEvent.Set();
        }
        return;
    }

    if (waitInfo->state & WAIT_ACTIVE)
    {
        DeactivateWait(waitInfo);
    }

    if ( waitInfo->PartialCompletionEvent.IsValid())
    {
        waitInfo->PartialCompletionEvent.Set();
        return;     // we cannot delete the wait here since the PartialCompletionEvent
                    // may not have been closed yet. so, we return and rely on the waiter of PartialCompletionEvent
                    // to do the close
    }

    if (InterlockedDecrement(&waitInfo->refCount) == 0)
    {
        DeleteWait(waitInfo);
    }
    return;
}


/* This gets called in a finalizer thread ONLY IF an app does not deregister the
   the wait. Note that just because the registeredWaitHandle is collected by GC
   does not mean it is safe to delete the wait. The refcount tells us when it is
   safe.
*/
void ThreadpoolMgr::WaitHandleCleanup(HANDLE hWaitObject)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());
    _ASSERTE(IsInitialized()); // cannot call cleanup before first registering

    WaitInfo* waitInfo = (WaitInfo*) hWaitObject;
    _ASSERTE(waitInfo->refCount > 0);

    DWORD result = QueueDeregisterWait(waitInfo->threadCB->threadHandle, waitInfo);

    if (result == 0)
        STRESS_LOG1(LF_THREADPOOL, LL_ERROR, "Queue APC failed in WaitHandleCleanup %x", result);

}

BOOL ThreadpoolMgr::CreateGateThread()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    HANDLE threadHandle = Thread::CreateUtilityThread(Thread::StackSize_Small, GateThreadStart, NULL, W(".NET ThreadPool Gate"));

    if (threadHandle)
    {
        CloseHandle(threadHandle);  //we don't need this anymore
        return TRUE;
    }

    return FALSE;
}



/************************************************************************/

BOOL ThreadpoolMgr::BindIoCompletionCallback(HANDLE FileHandle,
                                            LPOVERLAPPED_COMPLETION_ROUTINE Function,
                                            ULONG Flags,
                                            DWORD& errCode)
{

    CONTRACTL
    {
        THROWS;     // EnsureInitialized can throw
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX

    errCode = S_OK;

    EnsureInitialized();


    _ASSERTE(GlobalCompletionPort != NULL);

    if (!InitCompletionPortThreadpool)
        InitCompletionPortThreadpool = TRUE;

    GrowCompletionPortThreadpoolIfNeeded();

    HANDLE h = CreateIoCompletionPort(FileHandle,
                                      GlobalCompletionPort,
                                      (ULONG_PTR) Function,
                                      NumberOfProcessors);
    if (h == NULL)
    {
        errCode = GetLastError();
        return FALSE;
    }

    _ASSERTE(h == GlobalCompletionPort);

    return TRUE;
#else // TARGET_UNIX
    SetLastError(ERROR_CALL_NOT_IMPLEMENTED);
    return FALSE;
#endif // !TARGET_UNIX
}

#ifndef TARGET_UNIX
BOOL ThreadpoolMgr::CreateCompletionPortThread(LPVOID lpArgs)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
    }
    CONTRACTL_END;

    Thread *pThread;
    BOOL fIsCLRThread;
    if ((pThread = CreateUnimpersonatedThread(CompletionPortThreadStart, lpArgs, &fIsCLRThread)) != NULL)
    {
        LastCPThreadCreation = GetTickCount();          // record this for use by logic to spawn additional threads

        if (fIsCLRThread) {
            pThread->ChooseThreadCPUGroupAffinity();
            pThread->StartThread();
        }
        else {
            DWORD status;
            status = ResumeThread((HANDLE)pThread);
            _ASSERTE(status != (DWORD) (-1));
            CloseHandle((HANDLE)pThread);          // we don't need this anymore
        }

        ThreadCounter::Counts counts = CPThreadCounter.GetCleanCounts();
        FireEtwIOThreadCreate_V1(counts.NumActive + counts.NumRetired, counts.NumRetired, GetClrInstanceId());

        return TRUE;
    }


    return FALSE;
}

DWORD WINAPI ThreadpoolMgr::CompletionPortThreadStart(LPVOID lpArgs)
{
    ClrFlsSetThreadType (ThreadType_Threadpool_IOCompletion);

    CONTRACTL
    {
        THROWS;
        if (GetThread()) { MODE_PREEMPTIVE;} else { DISABLED(MODE_ANY);}
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    DWORD numBytes=0;
    size_t key=0;

    LPOVERLAPPED pOverlapped = NULL;
    DWORD errorCode;
    PIOCompletionContext context;
    BOOL fIsCompletionContext;

    const DWORD CP_THREAD_WAIT = 15000; /* milliseconds */

    _ASSERTE(GlobalCompletionPort != NULL);

    BOOL fThreadInit = FALSE;
    Thread *pThread = NULL;

    DWORD cpThreadWait = 0;

    if (g_fEEStarted) {
        pThread = SetupThreadNoThrow();
        if (pThread == NULL) {
            return 0;
        }

        // converted to CLRThread and added to ThreadStore, pick an group affinity for this thread
        pThread->ChooseThreadCPUGroupAffinity();

        fThreadInit = TRUE;
    }

#ifdef FEATURE_COMINTEROP
    // Threadpool threads should be initialized as MTA. If we are unable to do so,
    // return failure.
    BOOL fCoInited = FALSE;
    {
        fCoInited = SUCCEEDED(::CoInitializeEx(NULL, COINIT_MULTITHREADED));
        if (!fCoInited)
        {
            goto Exit;
        }
    }

    if (pThread && pThread->SetApartment(Thread::AS_InMTA) != Thread::AS_InMTA)
    {
        // @todo: should we log the failure
        goto Exit;
    }
#endif // FEATURE_COMINTEROP

    ThreadCounter::Counts oldCounts;
    ThreadCounter::Counts newCounts;

    cpThreadWait = CP_THREAD_WAIT;
    for (;; )
    {
Top:
        if (!fThreadInit) {
            if (g_fEEStarted) {
                pThread = SetupThreadNoThrow();
                if (pThread == NULL) {
                    break;
                }

                // converted to CLRThread and added to ThreadStore, pick an group affinity for this thread
                pThread->ChooseThreadCPUGroupAffinity();

#ifdef FEATURE_COMINTEROP
                if (pThread->SetApartment(Thread::AS_InMTA) != Thread::AS_InMTA)
                {
                    // @todo: should we log the failure
                    goto Exit;
                }
#endif // FEATURE_COMINTEROP

                fThreadInit = TRUE;
            }
        }

        GCX_PREEMP_NO_DTOR();

        //
        // We're about to wait on the IOCP; mark ourselves as no longer "working."
        //
        while (true)
        {
            ThreadCounter::Counts oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
            ThreadCounter::Counts newCounts = oldCounts;
            newCounts.NumWorking--;

            //
            // If we've only got one thread left, it won't be allowed to exit, because we need to keep
            // one thread listening for completions.  So there's no point in having a timeout; it will
            // only use power unnecessarily.
            //
            cpThreadWait = (newCounts.NumActive == 1) ? INFINITE : CP_THREAD_WAIT;

            if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                break;
        }

        errorCode = S_OK;

        if (lpArgs == NULL)
        {
            CONTRACT_VIOLATION(ThrowsViolation);

            context = NULL;
            fIsCompletionContext = FALSE;

            if (pThread == NULL)
            {
                pThread = GetThread();
            }

            if (pThread)
            {

                context = (PIOCompletionContext) pThread->GetIOCompletionContext();

                if (context->lpOverlapped != NULL)
                {
                    errorCode = context->ErrorCode;
                    numBytes = context->numBytesTransferred;
                    pOverlapped = context->lpOverlapped;
                    key = context->key;

                    context->lpOverlapped = NULL;
                    fIsCompletionContext = TRUE;
                }
            }

            if((context == NULL) || (!fIsCompletionContext))
            {
                _ASSERTE (context == NULL || context->lpOverlapped == NULL);

                BOOL status = GetQueuedCompletionStatus(
                    GlobalCompletionPort,
                    &numBytes,
                    (PULONG_PTR)&key,
                    &pOverlapped,
                    cpThreadWait
                    );

                if (status == 0)
                    errorCode = GetLastError();
            }
        }
        else
        {
            QueuedStatus *CompletionStatus = (QueuedStatus*)lpArgs;
            numBytes = CompletionStatus->numBytes;
            key = (size_t)CompletionStatus->key;
            pOverlapped = CompletionStatus->pOverlapped;
            errorCode = CompletionStatus->errorCode;
            delete CompletionStatus;
            lpArgs = NULL;  // one-time deal for initial CP packet
        }

        // We fire IODequeue events whether the IO completion was retrieved in the above call to
        // GetQueuedCompletionStatus or during an earlier call (e.g. in GateThreadStart, and passed here in lpArgs,
        // or in CompletionPortDispatchWorkWithinAppDomain, and passed here through StoreOverlappedInfoInThread)

        // For the purposes of activity correlation we only fire ETW events here, if needed OR if not fired at a higher
        // abstraction level (e.g. ThreadpoolMgr::RegisterWaitForSingleObject)
        // Note: we still fire the event for managed async IO, despite the fact we don't have a paired IOEnqueue event
        // for this case. We do this to "mark" the end of the previous workitem. When we provide full support at the higher
        // abstraction level for managed IO we can remove the IODequeues fired here
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolIODequeue)
                && !AreEtwIOQueueEventsSpeciallyHandled((LPOVERLAPPED_COMPLETION_ROUTINE)key) && pOverlapped != NULL)
        {
            FireEtwThreadPoolIODequeue(pOverlapped, OverlappedDataObject::GetOverlappedForTracing(pOverlapped), GetClrInstanceId());
        }

        bool enterRetirement;

        while (true)
        {
            //
            // When we reach this point, this thread is "active" but not "working."  Depending on the result of the call to GetQueuedCompletionStatus,
            // and the state of the rest of the IOCP threads, we need to figure out whether to de-activate (exit) this thread, retire this thread,
            // or transition to "working."
            //

            // counts volatile read paired with CompareExchangeCounts loop set
            oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
            newCounts = oldCounts;
            enterRetirement = false;

            if (errorCode == WAIT_TIMEOUT)
            {
                //
                // We timed out, and are going to try to exit or retire.
                //
                newCounts.NumActive--;

                //
                // We need at least one free thread, or we have no way of knowing if completions are being queued.
                //
                if (newCounts.NumWorking == newCounts.NumActive)
                {
                    newCounts = oldCounts;
                    newCounts.NumWorking++; //not really working, but we'll decremented it at the top
                    if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                        goto Top;
                    else
                        continue;
                }

                //
                // We can't exit a thread that has pending I/O - we'll "retire" it instead.
                //
                if (IsIoPending())
                {
                    enterRetirement = true;
                    newCounts.NumRetired++;
                }
            }
            else
            {
                //
                // We have work to do
                //
                newCounts.NumWorking++;
            }

            if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                break;
        }

        if (errorCode == WAIT_TIMEOUT)
        {
            if (!enterRetirement)
            {
                goto Exit;
            }
            else
            {
                // now in "retired mode" waiting for pending io to complete
                FireEtwIOThreadRetire_V1(newCounts.NumActive + newCounts.NumRetired, newCounts.NumRetired, GetClrInstanceId());

                for (;;)
                {
                    DWORD status = SafeWait(RetiredCPWakeupEvent,CP_THREAD_PENDINGIO_WAIT,FALSE);
                    _ASSERTE(status == WAIT_TIMEOUT || status == WAIT_OBJECT_0);

                    if (status == WAIT_TIMEOUT)
                    {
                        if (IsIoPending())
                        {
                            continue;
                        }
                        else
                        {
                            // We can now exit; decrement the retired count.
                            while (true)
                            {
                                // counts volatile read paired with CompareExchangeCounts loop set
                                oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
                                newCounts = oldCounts;
                                newCounts.NumRetired--;
                                if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                                    break;
                            }
                            goto Exit;
                        }
                    }
                    else
                    {
                        // put back into rotation -- we need a thread
                        while (true)
                        {
                            // counts volatile read paired with CompareExchangeCounts loop set
                            oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
                            newCounts = oldCounts;
                            newCounts.NumRetired--;
                            newCounts.NumActive++;
                            newCounts.NumWorking++; //we're not really working, but we'll decrement this before waiting for work.
                            if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                                break;
                        }
                        FireEtwIOThreadUnretire_V1(newCounts.NumActive + newCounts.NumRetired, newCounts.NumRetired, GetClrInstanceId());
                        goto Top;
                    }
                }
            }
        }

        // we should not reach this point unless we have work to do
        _ASSERTE(errorCode != WAIT_TIMEOUT && !enterRetirement);

        // if we have no more free threads, start the gate thread
        if (newCounts.NumWorking >= newCounts.NumActive)
            EnsureGateThreadRunning();


        // We can not assert here.  If stdin/stdout/stderr of child process are redirected based on
        // async io, GetQueuedCompletionStatus returns when child process operates on its stdin/stdout/stderr.
        // Parent process does not issue any ReadFile/WriteFile, and hence pOverlapped is going to be NULL.
        //_ASSERTE(pOverlapped != NULL);

        if (pOverlapped != NULL)
        {
            _ASSERTE(key != 0);  // should be a valid function address

            if (key != 0)
            {
                if (GCHeapUtilities::IsGCInProgress(TRUE))
                {
                    //Indicate that this thread is free, and waiting on GC, not doing any user work.
                    //This helps in threads not getting injected when some threads have woken up from the
                    //GC event, and some have not.
                    while (true)
                    {
                        // counts volatile read paired with CompareExchangeCounts loop set
                        oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
                        newCounts = oldCounts;
                        newCounts.NumWorking--;
                        if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                            break;
                    }

                    // GC is imminent, so wait until GC is complete before executing next request.
                    // this reduces in-flight objects allocated right before GC, easing the GC's work
                    GCHeapUtilities::WaitForGCCompletion(TRUE);

                    while (true)
                    {
                        // counts volatile read paired with CompareExchangeCounts loop set
                        oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
                        newCounts = oldCounts;
                        newCounts.NumWorking++;
                        if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                            break;
                    }

                    if (newCounts.NumWorking >= newCounts.NumActive)
                        EnsureGateThreadRunning();
                }
                else
                {
                    GrowCompletionPortThreadpoolIfNeeded();
                }

                {
                    CONTRACT_VIOLATION(ThrowsViolation);

                    ((LPOVERLAPPED_COMPLETION_ROUTINE) key)(errorCode, numBytes, pOverlapped);
                }

                Thread::IncrementIOThreadPoolCompletionCount(pThread);

                if (pThread == NULL)
                {
                    pThread = GetThread();
                }

                if (pThread)
                {
                    _ASSERTE(!pThread->IsAbortRequested());
                    pThread->InternalReset();
                }
            }
            else
            {
                // Application bug - can't do much, just ignore it
            }

        }

    }   // for (;;)

Exit:

    oldCounts = CPThreadCounter.GetCleanCounts();

    // we should never destroy or retire all IOCP threads, because then we won't have any threads to notice incoming completions.
    _ASSERTE(oldCounts.NumActive > 0);

    FireEtwIOThreadTerminate_V1(oldCounts.NumActive + oldCounts.NumRetired, oldCounts.NumRetired, GetClrInstanceId());

#ifdef FEATURE_COMINTEROP
    if (pThread) {
        pThread->SetApartment(Thread::AS_Unknown);
        pThread->CoUninitialize();
    }
    // Couninit the worker thread
    if (fCoInited)
    {
        CoUninitialize();
    }
#endif

    if (pThread) {
        pThread->ClearThreadCPUGroupAffinity();

        DestroyThread(pThread);
    }

    return 0;
}

LPOVERLAPPED ThreadpoolMgr::CompletionPortDispatchWorkWithinAppDomain(
    Thread* pThread,
    DWORD* pErrorCode,
    DWORD* pNumBytes,
    size_t* pKey)
//
//This function is called just after dispatching the previous BindIO callback
//to Managed code. This is a perf optimization to do a quick call to
//GetQueuedCompletionStatus with a timeout of 0 ms. If there is work in the
//same appdomain, dispatch it back immediately. If not stick it in a well known
//place, and reenter the target domain. The timeout of zero is chosen so as to
//not delay appdomain unloads.
//
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    LPOVERLAPPED lpOverlapped=NULL;

    BOOL status=FALSE;
    OVERLAPPEDDATAREF overlapped=NULL;
    BOOL ManagedCallback=FALSE;

    *pErrorCode = S_OK;


    //Very Very Important!
    //Do not change the timeout for GetQueuedCompletionStatus to a non-zero value.
    //Selecting a non-zero value can cause the thread to block, and lead to expensive context switches.
    //In real life scenarios, we have noticed a packet to be not available immediately, but very shortly
    //(after few 100's of instructions), and falling back to the VM is good in that case as compared to
    //taking a context switch. Changing the timeout to non-zero can lead to perf degrades, that are very
    //hard to diagnose.

    status = ::GetQueuedCompletionStatus(
                 GlobalCompletionPort,
                 pNumBytes,
                 (PULONG_PTR)pKey,
                 &lpOverlapped,
                 0);

    DWORD lastError = GetLastError();

    if (status == 0)
    {
        if (lpOverlapped != NULL)
        {
            *pErrorCode = lastError;
        }
        else
        {
            return NULL;
        }
    }

    if (((LPOVERLAPPED_COMPLETION_ROUTINE) *pKey) != BindIoCompletionCallbackStub)
    {
        //_ASSERTE(FALSE);
    }
    else
    {
        ManagedCallback = TRUE;
        overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));
    }

    if (ManagedCallback)
    {
        _ASSERTE(*pKey != 0);  // should be a valid function address

        if (*pKey ==0)
        {
            //Application Bug.
            return NULL;
        }
    }
    else
    {
        //Just retruned back from managed code, a Thread structure should exist.
        _ASSERTE (pThread);

        //Oops, this is an overlapped fom a different appdomain. STick it in
        //the thread. We will process it later.

        StoreOverlappedInfoInThread(pThread, *pErrorCode, *pNumBytes, *pKey, lpOverlapped);

        lpOverlapped = NULL;
    }

#ifndef DACCESS_COMPILE
    return lpOverlapped;
#endif
}

void ThreadpoolMgr::StoreOverlappedInfoInThread(Thread* pThread, DWORD dwErrorCode, DWORD dwNumBytes, size_t key, LPOVERLAPPED lpOverlapped)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    _ASSERTE(pThread);

    PIOCompletionContext context;

    context = (PIOCompletionContext) pThread->GetIOCompletionContext();

    _ASSERTE(context);

    context->ErrorCode = dwErrorCode;
    context->numBytesTransferred = dwNumBytes;
    context->lpOverlapped = lpOverlapped;
    context->key = key;
}

BOOL ThreadpoolMgr::ShouldGrowCompletionPortThreadpool(ThreadCounter::Counts counts)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (counts.NumWorking >= counts.NumActive
        && (counts.NumActive == 0 ||  !GCHeapUtilities::IsGCInProgress(TRUE))
        )
    {
        // adjust limit if needed
        if (counts.NumRetired == 0)
        {
            if (counts.NumActive + counts.NumRetired < MaxLimitTotalCPThreads &&
                (counts.NumActive < MinLimitTotalCPThreads || cpuUtilization < CpuUtilizationLow))
            {
                // add one more check to make sure that we haven't fired off a new
                // thread since the last time time we checked the cpu utilization.
                // However, don't bother if we haven't reached the MinLimit (2*number of cpus)
                if ((counts.NumActive < MinLimitTotalCPThreads) ||
                    SufficientDelaySinceLastSample(LastCPThreadCreation,counts.NumActive))
                {
                    return TRUE;
                }
            }
        }

        if (counts.NumRetired > 0)
            return TRUE;
    }
    return FALSE;
}

void ThreadpoolMgr::GrowCompletionPortThreadpoolIfNeeded()
{
    CONTRACTL
    {
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    ThreadCounter::Counts oldCounts, newCounts;
    while (true)
    {
        oldCounts = CPThreadCounter.GetCleanCounts();
        newCounts = oldCounts;

        if(!ShouldGrowCompletionPortThreadpool(oldCounts))
        {
            break;
        }
        else
        {
            if (oldCounts.NumRetired > 0)
            {
                // wakeup retired thread instead
                RetiredCPWakeupEvent->Set();
                return;
            }
            else
            {
                // create a new thread.  New IOCP threads start as "active" and "working"
                newCounts.NumActive++;
                newCounts.NumWorking++;
                if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                {
                    if (!CreateCompletionPortThread(NULL))
                    {
                        // if thread creation failed, we have to adjust the counts back down.
                        while (true)
                        {
                            // counts volatile read paired with CompareExchangeCounts loop set
                            oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
                            newCounts = oldCounts;
                            newCounts.NumActive--;
                            newCounts.NumWorking--;
                            if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                                break;
                        }
                    }
                    return;
                }
            }
        }
    }
}
#endif // !TARGET_UNIX

// Returns true if there is pending io on the thread.
BOOL ThreadpoolMgr::IsIoPending()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    int Status;
    ULONG IsIoPending;

    if (g_pufnNtQueryInformationThread)
    {
        Status =(int) (*g_pufnNtQueryInformationThread)(GetCurrentThread(),
                                          ThreadIsIoPending,
                                          &IsIoPending,
                                          sizeof(IsIoPending),
                                          NULL);


        if ((Status < 0) || IsIoPending)
            return TRUE;
        else
            return FALSE;
    }
    return TRUE;
#else
    return FALSE;
#endif // !TARGET_UNIX
}

#ifndef TARGET_UNIX

#ifdef HOST_64BIT
#pragma warning (disable : 4716)
#else
#pragma warning (disable : 4715)
#endif

int ThreadpoolMgr::GetCPUBusyTime_NT(PROCESS_CPU_INFORMATION* pOldInfo)
{
    LIMITED_METHOD_CONTRACT;

    PROCESS_CPU_INFORMATION newUsage;
    newUsage.idleTime.QuadPart   = 0;
    newUsage.kernelTime.QuadPart = 0;
    newUsage.userTime.QuadPart   = 0;

    if (CPUGroupInfo::CanEnableGCCPUGroups() && CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
    {
#if !defined(FEATURE_REDHAWK) && !defined(TARGET_UNIX)
        FILETIME newIdleTime, newKernelTime, newUserTime;

        CPUGroupInfo::GetSystemTimes(&newIdleTime, &newKernelTime, &newUserTime);
        newUsage.idleTime.u.LowPart    = newIdleTime.dwLowDateTime;
        newUsage.idleTime.u.HighPart   = newIdleTime.dwHighDateTime;
        newUsage.kernelTime.u.LowPart  = newKernelTime.dwLowDateTime;
        newUsage.kernelTime.u.HighPart = newKernelTime.dwHighDateTime;
        newUsage.userTime.u.LowPart    = newUserTime.dwLowDateTime;
        newUsage.userTime.u.HighPart   = newUserTime.dwHighDateTime;
#endif
    }
    else
    {
        (*g_pufnNtQuerySystemInformation)(SystemProcessorPerformanceInformation,
                        pOldInfo->usageBuffer,
                        pOldInfo->usageBufferSize,
                        NULL);

        SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION* pInfoArray = pOldInfo->usageBuffer;
        DWORD_PTR pmask = pOldInfo->affinityMask;

        int proc_no = 0;
        while (pmask)
        {
            if (pmask & 1)
            {   //should be good: 1CPU 28823 years, 256CPUs 100+years
                newUsage.idleTime.QuadPart   += pInfoArray[proc_no].IdleTime.QuadPart;
                newUsage.kernelTime.QuadPart += pInfoArray[proc_no].KernelTime.QuadPart;
                newUsage.userTime.QuadPart   += pInfoArray[proc_no].UserTime.QuadPart;
            }

            pmask >>=1;
            proc_no++;
        }
    }

    __int64 cpuTotalTime, cpuBusyTime;

    cpuTotalTime  = (newUsage.userTime.QuadPart   - pOldInfo->userTime.QuadPart) +
                    (newUsage.kernelTime.QuadPart - pOldInfo->kernelTime.QuadPart);
    cpuBusyTime   = cpuTotalTime -
                    (newUsage.idleTime.QuadPart   - pOldInfo->idleTime.QuadPart);

    // Preserve reading
    pOldInfo->idleTime   = newUsage.idleTime;
    pOldInfo->kernelTime = newUsage.kernelTime;
    pOldInfo->userTime   = newUsage.userTime;

    __int64 reading = 0;

    if (cpuTotalTime > 0)
        reading = ((cpuBusyTime * 100) / cpuTotalTime);

    _ASSERTE(FitsIn<int>(reading));
    return (int)reading;
}

#else // !TARGET_UNIX

int ThreadpoolMgr::GetCPUBusyTime_NT(PAL_IOCP_CPU_INFORMATION* pOldInfo)
{
    return PAL_GetCPUBusyTime(pOldInfo);
}

#endif // !TARGET_UNIX

//
// A timer that ticks every GATE_THREAD_DELAY milliseconds.
// On platforms that support it, we use a coalescable waitable timer object.
// For other platforms, we use Sleep, via __SwitchToThread.
//
class GateThreadTimer
{
#ifndef TARGET_UNIX
    HANDLE m_hTimer;

public:
    GateThreadTimer()
        : m_hTimer(NULL)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        if (g_pufnCreateWaitableTimerEx && g_pufnSetWaitableTimerEx)
        {
            m_hTimer = g_pufnCreateWaitableTimerEx(NULL, NULL, 0, TIMER_ALL_ACCESS);
            if (m_hTimer)
            {
                //
                // Set the timer to fire GATE_THREAD_DELAY milliseconds from now, then every GATE_THREAD_DELAY milliseconds thereafter.
                // We also set the tolerance to GET_THREAD_DELAY_TOLERANCE, allowing the OS to coalesce this timer.
                //
                LARGE_INTEGER dueTime;
                dueTime.QuadPart = MILLI_TO_100NANO(-(LONGLONG)GATE_THREAD_DELAY); //negative value indicates relative time
                if (!g_pufnSetWaitableTimerEx(m_hTimer, &dueTime, GATE_THREAD_DELAY, NULL, NULL, NULL, GATE_THREAD_DELAY_TOLERANCE))
                {
                    CloseHandle(m_hTimer);
                    m_hTimer = NULL;
                }
            }
        }
    }

    ~GateThreadTimer()
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        if (m_hTimer)
        {
            CloseHandle(m_hTimer);
            m_hTimer = NULL;
        }
    }

#endif // !TARGET_UNIX

public:
    void Wait()
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

#ifndef TARGET_UNIX
        if (m_hTimer)
            WaitForSingleObject(m_hTimer, INFINITE);
        else
#endif // !TARGET_UNIX
            __SwitchToThread(GATE_THREAD_DELAY, CALLER_LIMITS_SPINNING);
    }
};

DWORD WINAPI ThreadpoolMgr::GateThreadStart(LPVOID lpArgs)
{
    ClrFlsSetThreadType (ThreadType_Gate);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(!UsePortableThreadPool());
    _ASSERTE(GateThreadStatus == GATE_THREAD_STATUS_REQUESTED);

    GateThreadTimer timer;

    // TODO: do we need to do this?
    timer.Wait(); // delay getting initial CPU reading

#ifndef TARGET_UNIX
    PROCESS_CPU_INFORMATION prevCPUInfo;

    if (!g_pufnNtQuerySystemInformation)
    {
        _ASSERT(!"NtQuerySystemInformation API not available!");
        return 0;
    }

#ifndef TARGET_UNIX
    //GateThread can start before EESetup, so ensure CPU group information is initialized;
    CPUGroupInfo::EnsureInitialized();
#endif // !TARGET_UNIX
    // initialize CPU usage information structure;
    prevCPUInfo.idleTime.QuadPart   = 0;
    prevCPUInfo.kernelTime.QuadPart = 0;
    prevCPUInfo.userTime.QuadPart   = 0;

    PREFIX_ASSUME(NumberOfProcessors < 65536);
    prevCPUInfo.numberOfProcessors = NumberOfProcessors;

    /* In following cases, affinity mask can be zero
     * 1. hosted, the hosted process already uses multiple cpu groups.
     *    thus, during CLR initialization, GetCurrentProcessCpuCount() returns 64, and GC threads
     *    are created to fill up the initial CPU group. ==> use g_SystemInfo.dwNumberOfProcessors
     * 2. GCCpuGroups=1, CLR creates GC threads for all processors in all CPU groups
     *    thus, the threadpool thread would use a whole CPU group (if Thread_UseAllCpuGroups is not set).
     *    ==> use g_SystemInfo.dwNumberOfProcessors.
     * 3. !defined(TARGET_UNIX) but defined(FEATURE_CORESYSTEM), GetCurrentProcessCpuCount()
     *    returns g_SystemInfo.dwNumberOfProcessors ==> use g_SystemInfo.dwNumberOfProcessors;
     * Other cases:
     * 1. Normal case: the mask is all or a subset of all processors in a CPU group;
     * 2. GCCpuGroups=1 && Thread_UseAllCpuGroups = 1, the mask is not used
     */
    prevCPUInfo.affinityMask = GetCurrentProcessCpuMask();
    if (prevCPUInfo.affinityMask == 0)
    {   // create a mask that has g_SystemInfo.dwNumberOfProcessors;
        DWORD_PTR mask = 0, maskpos = 1;
        for (unsigned int i=0; i < g_SystemInfo.dwNumberOfProcessors; i++)
        {
             mask |= maskpos;
             maskpos <<= 1;
        }
        prevCPUInfo.affinityMask = mask;
    }

    // in some cases GetCurrentProcessCpuCount() returns a number larger than
    // g_SystemInfo.dwNumberOfProcessor when there are CPU groups, use the larger
    // one to create buffer. This buffer must be cleared with 0's to get correct
    // CPU usage statistics
    int elementsNeeded = NumberOfProcessors > g_SystemInfo.dwNumberOfProcessors ?
                                                  NumberOfProcessors : g_SystemInfo.dwNumberOfProcessors;
    if (!ClrSafeInt<int>::multiply(elementsNeeded, sizeof(SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION),
                                                  prevCPUInfo.usageBufferSize))
        return 0;

    prevCPUInfo.usageBuffer = (SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION *)alloca(prevCPUInfo.usageBufferSize);
    if (prevCPUInfo.usageBuffer == NULL)
        return 0;

    memset((void *)prevCPUInfo.usageBuffer, 0, prevCPUInfo.usageBufferSize); //must clear it with 0s

    GetCPUBusyTime_NT(&prevCPUInfo);
#else // !TARGET_UNIX
    PAL_IOCP_CPU_INFORMATION prevCPUInfo;
    memset(&prevCPUInfo, 0, sizeof(prevCPUInfo));

    GetCPUBusyTime_NT(&prevCPUInfo);                  // ignore return value the first time
#endif // !TARGET_UNIX

    BOOL IgnoreNextSample = FALSE;

    do
    {
        timer.Wait();

        if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking))
            FireEtwThreadPoolWorkingThreadCount(TakeMaxWorkingThreadCount(), GetClrInstanceId());

#ifdef DEBUGGING_SUPPORTED
        // if we are stopped at a debug breakpoint, go back to sleep
        if (CORDebuggerAttached() && g_pDebugInterface->IsStopped())
            continue;
#endif // DEBUGGING_SUPPORTED

        if (!GCHeapUtilities::IsGCInProgress(FALSE) )
        {
            if (IgnoreNextSample)
            {
                IgnoreNextSample = FALSE;
                int cpuUtilizationTemp = GetCPUBusyTime_NT(&prevCPUInfo);            // updates prevCPUInfo as side effect
                // don't artificially drive down average if cpu is high
                if (cpuUtilizationTemp <= CpuUtilizationLow)
                    cpuUtilization = CpuUtilizationLow + 1;
                else
                    cpuUtilization = cpuUtilizationTemp;
            }
            else
            {
                cpuUtilization = GetCPUBusyTime_NT(&prevCPUInfo);            // updates prevCPUInfo as side effect
            }
        }
        else
        {
            int cpuUtilizationTemp = GetCPUBusyTime_NT(&prevCPUInfo);            // updates prevCPUInfo as side effect
            // don't artificially drive down average if cpu is high
            if (cpuUtilizationTemp <= CpuUtilizationLow)
                cpuUtilization = CpuUtilizationLow + 1;
            else
                cpuUtilization = cpuUtilizationTemp;
            IgnoreNextSample = TRUE;
        }

        PerformGateActivities(cpuUtilization);
    }
    while (ShouldGateThreadKeepRunning());

    return 0;
}

void ThreadpoolMgr::PerformGateActivities(int cpuUtilization)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ThreadpoolMgr::cpuUtilization = cpuUtilization;

#ifndef TARGET_UNIX
    // don't mess with CP thread pool settings if not initialized yet
    if (InitCompletionPortThreadpool)
    {
        ThreadCounter::Counts oldCounts, newCounts;
        oldCounts = CPThreadCounter.GetCleanCounts();

        if (oldCounts.NumActive == oldCounts.NumWorking &&
            oldCounts.NumRetired == 0 &&
            oldCounts.NumActive < MaxLimitTotalCPThreads &&
            !GCHeapUtilities::IsGCInProgress(TRUE))

        {
            BOOL status;
            DWORD numBytes;
            size_t key;
            LPOVERLAPPED pOverlapped;
            DWORD errorCode;

            errorCode = S_OK;

            status = GetQueuedCompletionStatus(
                        GlobalCompletionPort,
                        &numBytes,
                        (PULONG_PTR)&key,
                        &pOverlapped,
                        0 // immediate return
                        );

            if (status == 0)
            {
                errorCode = GetLastError();
            }

            if (errorCode != WAIT_TIMEOUT)
            {
                QueuedStatus *CompletionStatus = NULL;

                // loop, retrying until memory is allocated.  Under such conditions the gate
                // thread is not useful anyway, so I feel comfortable with this behavior
                do
                {
                    // make sure to free mem later in thread
                    CompletionStatus = new (nothrow) QueuedStatus;
                    if (CompletionStatus == NULL)
                    {
                        __SwitchToThread(GATE_THREAD_DELAY, CALLER_LIMITS_SPINNING);
                    }
                }
                while (CompletionStatus == NULL);

                CompletionStatus->numBytes = numBytes;
                CompletionStatus->key = (PULONG_PTR)key;
                CompletionStatus->pOverlapped = pOverlapped;
                CompletionStatus->errorCode = errorCode;

                // IOCP threads are created as "active" and "working"
                while (true)
                {
                    // counts volatile read paired with CompareExchangeCounts loop set
                    oldCounts = CPThreadCounter.DangerousGetDirtyCounts();
                    newCounts = oldCounts;
                    newCounts.NumActive++;
                    newCounts.NumWorking++;
                    if (oldCounts == CPThreadCounter.CompareExchangeCounts(newCounts, oldCounts))
                        break;
                }

                // loop, retrying until thread is created.
                while (!CreateCompletionPortThread((LPVOID)CompletionStatus))
                {
                    __SwitchToThread(GATE_THREAD_DELAY, CALLER_LIMITS_SPINNING);
                }
            }
        }
        else if (cpuUtilization < CpuUtilizationLow)
        {
            // this could be an indication that threads might be getting blocked or there is no work
            if (oldCounts.NumWorking == oldCounts.NumActive &&                // don't bump the limit if there are already free threads
                oldCounts.NumRetired > 0)
            {
                RetiredCPWakeupEvent->Set();
            }
        }
    }
#endif // !TARGET_UNIX

    if (!UsePortableThreadPool() &&
        0 == CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_DisableStarvationDetection))
    {
        if (PerAppDomainTPCountList::AreRequestsPendingInAnyAppDomains() && SufficientDelaySinceLastDequeue())
        {
            DangerousNonHostedSpinLockHolder tal(&ThreadAdjustmentLock);

            ThreadCounter::Counts counts = WorkerCounter.GetCleanCounts();
            while (counts.NumActive < MaxLimitTotalWorkerThreads && //don't add a thread if we're at the max
                    counts.NumActive >= counts.MaxWorking)            //don't add a thread if we're already in the process of adding threads
            {
                bool breakIntoDebugger = (0 != CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_DebugBreakOnWorkerStarvation));
                if (breakIntoDebugger)
                {
                    OutputDebugStringW(W("The CLR ThreadPool detected work queue starvation!"));
                    DebugBreak();
                }

                ThreadCounter::Counts newCounts = counts;
                newCounts.MaxWorking = newCounts.NumActive + 1;

                ThreadCounter::Counts oldCounts = WorkerCounter.CompareExchangeCounts(newCounts, counts);
                if (oldCounts == counts)
                {
                    HillClimbingInstance.ForceChange(newCounts.MaxWorking, Starvation);
                    MaybeAddWorkingWorker();
                    break;
                }
                else
                {
                    counts = oldCounts;
                }
            }
        }
    }
}

// called by logic to spawn a new completion port thread.
// return false if not enough time has elapsed since the last
// time we sampled the cpu utilization.
BOOL ThreadpoolMgr::SufficientDelaySinceLastSample(unsigned int LastThreadCreationTime,
                                                   unsigned NumThreads,   // total number of threads of that type (worker or CP)
                                                   double    throttleRate // the delay is increased by this percentage for each extra thread
                                                   )
{
    LIMITED_METHOD_CONTRACT;

    unsigned dwCurrentTickCount =  GetTickCount();

    unsigned delaySinceLastThreadCreation = dwCurrentTickCount - LastThreadCreationTime;

    unsigned minWaitBetweenThreadCreation =  GATE_THREAD_DELAY;

    if (throttleRate > 0.0)
    {
        _ASSERTE(throttleRate <= 1.0);

        unsigned adjustedThreadCount = NumThreads > NumberOfProcessors ? (NumThreads - NumberOfProcessors) : 0;

        minWaitBetweenThreadCreation = (unsigned) (GATE_THREAD_DELAY * pow((1.0 + throttleRate),(double)adjustedThreadCount));
    }
    // the amount of time to wait should grow up as the number of threads is increased

    return (delaySinceLastThreadCreation > minWaitBetweenThreadCreation);

}


// called by logic to spawn new worker threads, return true if it's been too long
// since the last dequeue operation - takes number of worker threads into account
// in deciding "too long"
BOOL ThreadpoolMgr::SufficientDelaySinceLastDequeue()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!UsePortableThreadPool());

    #define DEQUEUE_DELAY_THRESHOLD (GATE_THREAD_DELAY * 2)

    unsigned delay = GetTickCount() - VolatileLoad(&LastDequeueTime);
    unsigned tooLong;

    if(cpuUtilization < CpuUtilizationLow)
    {
        tooLong = GATE_THREAD_DELAY;
    }
    else
    {
        ThreadCounter::Counts counts = WorkerCounter.GetCleanCounts();
        unsigned numThreads = counts.MaxWorking;
        tooLong = numThreads * DEQUEUE_DELAY_THRESHOLD;
    }

    return (delay > tooLong);

}


#ifdef _MSC_VER
#ifdef HOST_64BIT
#pragma warning (default : 4716)
#else
#pragma warning (default : 4715)
#endif
#endif

/************************************************************************/

struct CreateTimerThreadParams {
    CLREvent    event;
    BOOL        setupSucceeded;
};

BOOL ThreadpoolMgr::CreateTimerQueueTimer(PHANDLE phNewTimer,
                                          WAITORTIMERCALLBACK Callback,
                                          PVOID Parameter,
                                          DWORD DueTime,
                                          DWORD Period,
                                          ULONG Flag)
{
    CONTRACTL
    {
        THROWS;     // EnsureInitialized, CreateAutoEvent can throw
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}  // There can be calls thru ICorThreadpool
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    EnsureInitialized();

    // For now we use just one timer thread. Consider using multiple timer threads if
    // number of timers in the queue exceeds a certain threshold. The logic and code
    // would be similar to the one for creating wait threads.
    if (NULL == TimerThread)
    {
        CrstHolder csh(&TimerQueueCriticalSection);

        // check again
        if (NULL == TimerThread)
        {
            CreateTimerThreadParams params;
            params.event.CreateAutoEvent(FALSE);

            params.setupSucceeded = FALSE;

            HANDLE TimerThreadHandle = Thread::CreateUtilityThread(Thread::StackSize_Small, TimerThreadStart, &params, W(".NET Timer"));

            if (TimerThreadHandle == NULL)
            {
                params.event.CloseEvent();
                ThrowOutOfMemory();
            }

            {
                GCX_PREEMP();
                for(;;)
                {
                    // if a host throws because it couldnt allocate another thread,
                    // just retry the wait.
                    if (SafeWait(&params.event,INFINITE, FALSE) != WAIT_TIMEOUT)
                        break;
                }
            }
            params.event.CloseEvent();

            if (!params.setupSucceeded)
            {
                CloseHandle(TimerThreadHandle);
                *phNewTimer = NULL;
                return FALSE;
            }

            TimerThread = TimerThreadHandle;
        }

    }


    NewHolder<TimerInfo> timerInfoHolder;
    TimerInfo * timerInfo = new (nothrow) TimerInfo;
    if (NULL == timerInfo)
        ThrowOutOfMemory();

    timerInfoHolder.Assign(timerInfo);

    timerInfo->FiringTime = DueTime;
    timerInfo->Function = Callback;
    timerInfo->Context = Parameter;
    timerInfo->Period = Period;
    timerInfo->state = 0;
    timerInfo->flag = Flag;
    timerInfo->ExternalCompletionEvent = INVALID_HANDLE;
    timerInfo->ExternalEventSafeHandle = NULL;

    *phNewTimer = (HANDLE)timerInfo;

    BOOL status = QueueUserAPC((PAPCFUNC)InsertNewTimer,TimerThread,(size_t)timerInfo);
    if (FALSE == status)
    {
        *phNewTimer = NULL;
        return FALSE;
    }

    timerInfoHolder.SuppressRelease();
    return TRUE;
}

#ifdef _MSC_VER
#ifdef HOST_64BIT
#pragma warning (disable : 4716)
#else
#pragma warning (disable : 4715)
#endif
#endif
DWORD WINAPI ThreadpoolMgr::TimerThreadStart(LPVOID p)
{
    ClrFlsSetThreadType (ThreadType_Timer);

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;        // due to SetApartment
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    /* cannot use contract because of SEH
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;*/

    CreateTimerThreadParams* params = (CreateTimerThreadParams*)p;

    Thread* pThread = SetupThreadNoThrow();

    params->setupSucceeded = (pThread == NULL) ? 0 : 1;
    params->event.Set();

    if (pThread == NULL)
        return 0;

    pTimerThread = pThread;
    // Timer threads never die

    LastTickCount = GetTickCount();

#ifdef FEATURE_COMINTEROP
    if (pThread->SetApartment(Thread::AS_InMTA) != Thread::AS_InMTA)
    {
        // @todo: should we log the failure
        return 0;
    }
#endif // FEATURE_COMINTEROP

    for (;;)
    {
         // moved to its own function since EX_TRY consumes stack
#ifdef _MSC_VER
#pragma inline_depth (0) // the function containing EX_TRY can't be inlined here
#endif
        TimerThreadFire();
#ifdef _MSC_VER
#pragma inline_depth (20)
#endif
    }

    // unreachable
}

void ThreadpoolMgr::TimerThreadFire()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    EX_TRY {
        DWORD timeout = FireTimers();

#undef SleepEx
        SleepEx(timeout, TRUE);
#define SleepEx(a,b) Dont_Use_SleepEx(a,b)

        // the thread could wake up either because an APC completed or the sleep timeout
        // in both case, we need to sweep the timer queue, firing timers, and readjusting
        // the next firing time

    }
    EX_CATCH {
        // Assert on debug builds since a dead timer thread is a fatal error
        _ASSERTE(FALSE);
        EX_RETHROW;
    }
    EX_END_CATCH(SwallowAllExceptions);
}

#ifdef _MSC_VER
#ifdef HOST_64BIT
#pragma warning (default : 4716)
#else
#pragma warning (default : 4715)
#endif
#endif

// Executed as an APC in timer thread
void ThreadpoolMgr::InsertNewTimer(TimerInfo* pArg)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pArg);
    TimerInfo * timerInfo = pArg;

    if (timerInfo->state & TIMER_DELETE)
    {   // timer was deleted before it could be registered
        DeleteTimer(timerInfo);
        return;
    }

    // set the firing time = current time + due time (note initially firing time = due time)
    DWORD currentTime = GetTickCount();
    if (timerInfo->FiringTime == (ULONG) -1)
    {
        timerInfo->state = TIMER_REGISTERED;
        timerInfo->refCount = 1;

    }
    else
    {
        timerInfo->FiringTime += currentTime;

        timerInfo->state = (TIMER_REGISTERED | TIMER_ACTIVE);
        timerInfo->refCount = 1;

        // insert the timer in the queue
        InsertTailList(&TimerQueue,(&timerInfo->link));
    }

    return;
}


// executed by the Timer thread
// sweeps through the list of timers, readjusting the firing times, queueing APCs for
// those that have expired, and returns the next firing time interval
DWORD ThreadpoolMgr::FireTimers()
{
    CONTRACTL
    {
        THROWS;     // QueueUserWorkItem can throw
        if (GetThread()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        if (GetThread()) { MODE_PREEMPTIVE;} else { DISABLED(MODE_ANY);}
    }
    CONTRACTL_END;

    DWORD currentTime = GetTickCount();
    DWORD nextFiringInterval = (DWORD) -1;
    TimerInfo* timerInfo = NULL;

    EX_TRY
    {
        for (LIST_ENTRY* node = (LIST_ENTRY*) TimerQueue.Flink;
             node != &TimerQueue;
            )
        {
            timerInfo = (TimerInfo*) node;
            node = (LIST_ENTRY*) node->Flink;

            if (TimeExpired(LastTickCount, currentTime, timerInfo->FiringTime))
            {
                if (timerInfo->Period == 0 || timerInfo->Period == (ULONG) -1)
                {
                    DeactivateTimer(timerInfo);
                }

                InterlockedIncrement(&timerInfo->refCount);

                if (UsePortableThreadPool())
                {
                    GCX_COOP();

                    ARG_SLOT args[] = { PtrToArgSlot(AsyncTimerCallbackCompletion), PtrToArgSlot(timerInfo) };
                    MethodDescCallSite(METHOD__THREAD_POOL__UNSAFE_QUEUE_UNMANAGED_WORK_ITEM).Call(args);
                }
                else
                {
                    QueueUserWorkItem(AsyncTimerCallbackCompletion,
                                      timerInfo,
                                      QUEUE_ONLY /* TimerInfo take care of deleting*/);
                }

                if (timerInfo->Period != 0 && timerInfo->Period != (ULONG)-1)
                {
                    ULONG nextFiringTime = timerInfo->FiringTime + timerInfo->Period;
                    DWORD firingInterval;
                    if (TimeExpired(timerInfo->FiringTime, currentTime, nextFiringTime))
                    {
                        // Enough time has elapsed to fire the timer yet again. The timer is not able to keep up with the short
                        // period, have it fire 1 ms from now to avoid spinning without a delay.
                        timerInfo->FiringTime = currentTime + 1;
                        firingInterval = 1;
                    }
                    else
                    {
                        timerInfo->FiringTime = nextFiringTime;
                        firingInterval = TimeInterval(nextFiringTime, currentTime);
                    }

                    if (firingInterval < nextFiringInterval)
                        nextFiringInterval = firingInterval;
                }
            }
            else
            {
                DWORD firingInterval = TimeInterval(timerInfo->FiringTime, currentTime);
                if (firingInterval < nextFiringInterval)
                    nextFiringInterval = firingInterval;
            }
        }
    }
    EX_CATCH
    {
        // If QueueUserWorkItem throws OOM, swallow the exception and retry on
        // the next call to FireTimers(), otherwise retrhow.
        Exception *ex = GET_EXCEPTION();
        // undo the call to DeactivateTimer()
        InterlockedDecrement(&timerInfo->refCount);
        timerInfo->state = timerInfo->state & TIMER_ACTIVE;
        InsertTailList(&TimerQueue, (&timerInfo->link));
        if (ex->GetHR() != E_OUTOFMEMORY)
        {
           EX_RETHROW;
        }
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    LastTickCount = currentTime;

    return nextFiringInterval;
}

DWORD WINAPI ThreadpoolMgr::AsyncTimerCallbackCompletion(PVOID pArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    Thread* pThread = GetThread();

    if (pThread == NULL)
    {
        HRESULT hr = ERROR_SUCCESS;

        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow(&hr);

        if (pThread == NULL)
        {
            return hr;
        }
    }

    {
        TimerInfo* timerInfo = (TimerInfo*) pArgs;
        ((WAITORTIMERCALLBACKFUNC) timerInfo->Function) (timerInfo->Context, TRUE) ;

        if (InterlockedDecrement(&timerInfo->refCount) == 0)
        {
            DeleteTimer(timerInfo);
        }
    }

    return ERROR_SUCCESS;
}


// removes the timer from the timer queue, thereby cancelling it
// there may still be pending callbacks that haven't completed
void ThreadpoolMgr::DeactivateTimer(TimerInfo* timerInfo)
{
    LIMITED_METHOD_CONTRACT;

    RemoveEntryList((LIST_ENTRY*) timerInfo);

    // This timer info could go into another linked list of timer infos
    // waiting to be released. Reinitialize the list pointers
    InitializeListHead(&timerInfo->link);
    timerInfo->state = timerInfo->state & ~TIMER_ACTIVE;
}

DWORD WINAPI ThreadpoolMgr::AsyncDeleteTimer(PVOID pArgs)
{
    CONTRACTL
    {
        THROWS;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    Thread * pThread = GetThread();

    if (pThread == NULL)
    {
        HRESULT hr = ERROR_SUCCESS;

        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow(&hr);

        if (pThread == NULL)
        {
            return hr;
        }
    }

    DeleteTimer((TimerInfo*) pArgs);

    return ERROR_SUCCESS;
}

void ThreadpoolMgr::DeleteTimer(TimerInfo* timerInfo)
{
    CONTRACTL
    {
        if (GetThread() == pTimerThread) { NOTHROW; } else { THROWS; }
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE((timerInfo->state & TIMER_ACTIVE) == 0);

    _ASSERTE(!(timerInfo->flag & WAIT_FREE_CONTEXT));

    if (timerInfo->flag & WAIT_INTERNAL_COMPLETION)
    {
        timerInfo->InternalCompletionEvent.Set();
        return; // the timerInfo will be deleted by the thread that's waiting on InternalCompletionEvent
    }

    // ExternalCompletionEvent comes from Host, ExternalEventSafeHandle from managed code.
    // They are mutually exclusive.
    _ASSERTE(!(timerInfo->ExternalCompletionEvent != INVALID_HANDLE &&
                        timerInfo->ExternalEventSafeHandle != NULL));

    if (timerInfo->ExternalCompletionEvent != INVALID_HANDLE)
    {
        SetEvent(timerInfo->ExternalCompletionEvent);
        timerInfo->ExternalCompletionEvent = INVALID_HANDLE;
    }

    // We cannot block the timer thread, so some cleanup is deferred to other threads.
    if (GetThread() == pTimerThread)
    {
        // Notify the ExternalEventSafeHandle with an user work item
        if (timerInfo->ExternalEventSafeHandle != NULL)
        {
            BOOL success = FALSE;
            EX_TRY
            {
                if (QueueUserWorkItem(AsyncDeleteTimer,
                          timerInfo,
                          QUEUE_ONLY) != FALSE)
                {
                    success = TRUE;
                }
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);

            // If unable to queue a user work item, fall back to queueing timer for release
            // which will happen *sometime* in the future.
            if (success == FALSE)
            {
                QueueTimerInfoForRelease(timerInfo);
            }

            return;
        }

        // Releasing GC handles can block. So we wont do this on the timer thread.
        // We'll put it in a list which will be processed by a worker thread
        if (timerInfo->Context != NULL)
        {
            QueueTimerInfoForRelease(timerInfo);
            return;
        }
    }

    // To get here we are either not the Timer thread or there is no blocking work to be done

    if (timerInfo->Context != NULL)
    {
        GCX_COOP();
        delete (ThreadpoolMgr::TimerInfoContext*)timerInfo->Context;
    }

    if (timerInfo->ExternalEventSafeHandle != NULL)
    {
        ReleaseTimerInfo(timerInfo);
    }

    delete timerInfo;

}

// We add TimerInfos from deleted timers into a linked list.
// A worker thread will later release the handles held by the TimerInfo
// and recycle them if possible.
void ThreadpoolMgr::QueueTimerInfoForRelease(TimerInfo *pTimerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // The synchronization in this method depends on the fact that
    //  - There is only one timer thread
    //  - The one and only timer thread is executing this method.
    //  - This function wont go into an alertable state. That could trigger another APC.
    // Else two threads can be queueing timerinfos and a race could
    // lead to leaked memory and handles
    _ASSERTE(GetThread());
    _ASSERTE(pTimerThread == GetThread());
    TimerInfo *pHead = NULL;

    // Make sure this timer info has been deactivated and removed from any other lists
    _ASSERTE((pTimerInfo->state & TIMER_ACTIVE) == 0);
    //_ASSERTE(pTimerInfo->link.Blink == &(pTimerInfo->link) &&
    //    pTimerInfo->link.Flink == &(pTimerInfo->link));
    // Make sure "link" is the first field in TimerInfo
    _ASSERTE(pTimerInfo == (PVOID)&pTimerInfo->link);

    // Grab any previously published list
    if ((pHead = InterlockedExchangeT(&TimerInfosToBeRecycled, NULL)) != NULL)
    {
        // If there already is a list, just append
        InsertTailList((LIST_ENTRY *)pHead, &pTimerInfo->link);
        pTimerInfo = pHead;
    }
    else
        // If this is the head, make its next and previous ptrs point to itself
        InitializeListHead((LIST_ENTRY*)&pTimerInfo->link);

    // Publish the list
    (void) InterlockedExchangeT(&TimerInfosToBeRecycled, pTimerInfo);

}

void ThreadpoolMgr::FlushQueueOfTimerInfos()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    TimerInfo *pHeadTimerInfo = NULL, *pCurrTimerInfo = NULL;
    LIST_ENTRY *pNextInfo = NULL;

    if ((pHeadTimerInfo = InterlockedExchangeT(&TimerInfosToBeRecycled, NULL)) == NULL)
        return;

    do
    {
        RemoveHeadList((LIST_ENTRY *)pHeadTimerInfo, pNextInfo);
        _ASSERTE(pNextInfo != NULL);

        pCurrTimerInfo = (TimerInfo *) pNextInfo;

        GCX_COOP();
        if (pCurrTimerInfo->Context != NULL)
        {
            delete (ThreadpoolMgr::TimerInfoContext*)pCurrTimerInfo->Context;
        }

        if (pCurrTimerInfo->ExternalEventSafeHandle != NULL)
        {
            ReleaseTimerInfo(pCurrTimerInfo);
        }

        delete pCurrTimerInfo;

    }
    while ((TimerInfo *)pNextInfo != pHeadTimerInfo);
}

/************************************************************************/
BOOL ThreadpoolMgr::ChangeTimerQueueTimer(
                                        HANDLE Timer,
                                        ULONG DueTime,
                                        ULONG Period)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    _ASSERTE(IsInitialized());
    _ASSERTE(Timer);                    // not possible to give invalid handle in managed code

    NewHolder<TimerUpdateInfo> updateInfoHolder;
    TimerUpdateInfo *updateInfo = new TimerUpdateInfo;
    updateInfoHolder.Assign(updateInfo);

    updateInfo->Timer = (TimerInfo*) Timer;
    updateInfo->DueTime = DueTime;
    updateInfo->Period = Period;

    BOOL status = QueueUserAPC((PAPCFUNC)UpdateTimer,
                               TimerThread,
                               (size_t) updateInfo);

    if (status)
        updateInfoHolder.SuppressRelease();

    return(status);
}

void ThreadpoolMgr::UpdateTimer(TimerUpdateInfo* pArgs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TimerUpdateInfo* updateInfo = (TimerUpdateInfo*) pArgs;
    TimerInfo* timerInfo = updateInfo->Timer;

    timerInfo->Period = updateInfo->Period;

    if (updateInfo->DueTime == (ULONG) -1)
    {
        if (timerInfo->state & TIMER_ACTIVE)
        {
            DeactivateTimer(timerInfo);
        }
        // else, noop (the timer was already inactive)
        _ASSERTE((timerInfo->state & TIMER_ACTIVE) == 0);

        delete updateInfo;
        return;
    }

    DWORD currentTime = GetTickCount();
    timerInfo->FiringTime = currentTime + updateInfo->DueTime;

    delete updateInfo;

    if (! (timerInfo->state & TIMER_ACTIVE))
    {
        // timer not active (probably a one shot timer that has expired), so activate it
        timerInfo->state |= TIMER_ACTIVE;
        _ASSERTE(timerInfo->refCount >= 1);
        // insert the timer in the queue
        InsertTailList(&TimerQueue,(&timerInfo->link));

    }

    return;
}

/************************************************************************/
BOOL ThreadpoolMgr::DeleteTimerQueueTimer(
                                        HANDLE Timer,
                                        HANDLE Event)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(IsInitialized());          // cannot call delete before creating timer
    _ASSERTE(Timer);                    // not possible to give invalid handle in managed code

    // make volatile to avoid compiler reordering check after async call.
    // otherwise, DeregisterTimer could delete timerInfo before the comparison.
    VolatilePtr<TimerInfo> timerInfo = (TimerInfo*) Timer;

    if (Event == (HANDLE) -1)
    {
        //CONTRACT_VIOLATION(ThrowsViolation);
        timerInfo->InternalCompletionEvent.CreateAutoEvent(FALSE);
        timerInfo->flag |= WAIT_INTERNAL_COMPLETION;
    }
    else if (Event)
    {
        timerInfo->ExternalCompletionEvent = Event;
    }
#ifdef _DEBUG
    else /* Event == NULL */
    {
        _ASSERTE(timerInfo->ExternalCompletionEvent == INVALID_HANDLE);
    }
#endif

    BOOL isBlocking = timerInfo->flag & WAIT_INTERNAL_COMPLETION;

    BOOL status = QueueUserAPC((PAPCFUNC)DeregisterTimer,
                               TimerThread,
                               (size_t)(TimerInfo*)timerInfo);

    if (FALSE == status)
    {
        if (isBlocking)
            timerInfo->InternalCompletionEvent.CloseEvent();
        return FALSE;
    }

    if (isBlocking)
    {
        _ASSERTE(timerInfo->ExternalEventSafeHandle == NULL);
        _ASSERTE(timerInfo->ExternalCompletionEvent == INVALID_HANDLE);
        _ASSERTE(GetThread() != pTimerThread);

        timerInfo->InternalCompletionEvent.Wait(INFINITE,TRUE /*alertable*/);
        timerInfo->InternalCompletionEvent.CloseEvent();
        // Release handles and delete TimerInfo
        _ASSERTE(timerInfo->refCount == 0);
        // if WAIT_INTERNAL_COMPLETION flag is not set, timerInfo will be deleted in DeleteTimer.
        timerInfo->flag &= ~WAIT_INTERNAL_COMPLETION;
        DeleteTimer(timerInfo);
    }
    return status;
}

void ThreadpoolMgr::DeregisterTimer(TimerInfo* pArgs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    TimerInfo* timerInfo = (TimerInfo*) pArgs;

    if (! (timerInfo->state & TIMER_REGISTERED) )
    {
        // set state to deleted, so that it does not get registered
        timerInfo->state |= TIMER_DELETE ;

        // since the timer has not even been registered, we dont need an interlock to decrease the RefCount
        timerInfo->refCount--;

        return;
    }

    if (timerInfo->state & TIMER_ACTIVE)
    {
        DeactivateTimer(timerInfo);
    }

    if (InterlockedDecrement(&timerInfo->refCount) == 0 )
    {
        DeleteTimer(timerInfo);
    }
    return;
}

#endif // !DACCESS_COMPILE
