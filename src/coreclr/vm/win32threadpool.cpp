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
bool ThreadpoolMgr::s_usePortableThreadPoolForIO = false;

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

    NumberOfProcessors = GetCurrentProcessCpuCount();
    InitPlatformVariables();

    EX_TRY
    {
        TimerQueueCriticalSection.Init(CrstThreadpoolTimerQueue);

        // initialize TimerQueue
        InitializeListHead(&TimerQueue);

#ifndef TARGET_UNIX
        //ThreadPool_CPUGroup
        if (CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
            RecycledLists.Initialize( CPUGroupInfo::GetNumActiveProcessors() );
        else
            RecycledLists.Initialize( g_SystemInfo.dwNumberOfProcessors );
#else // !TARGET_UNIX
        RecycledLists.Initialize( PAL_GetTotalCpuCount() );
#endif // !TARGET_UNIX
    }
    EX_CATCH
    {
        // Note: It is fine to call Destroy on uninitialized critical sections
        TimerQueueCriticalSection.Destroy();

        bExceptionCaught = TRUE;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (bExceptionCaught)
    {
        goto end;
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
    }

    // These APIs must be accessed via dynamic binding since they may be removed in future
    // OS versions.
    g_pufnNtQueryInformationThread = (NtQueryInformationThreadProc)GetProcAddress(hNtDll,"NtQueryInformationThread");
    g_pufnNtQuerySystemInformation = (NtQuerySystemInformationProc)GetProcAddress(hNtDll,"NtQuerySystemInformation");

#endif
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

DangerousNonHostedSpinLock ThreadpoolMgr::ThreadAdjustmentLock;

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

extern void WINAPI BindIoCompletionCallbackStub(DWORD ErrorCode,
                                            DWORD numBytesTransferred,
                                            LPOVERLAPPED lpOverlapped);


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
    if (GetThreadNULLOk()) { STATIC_CONTRACT_GC_TRIGGERS;} else {DISABLED(STATIC_CONTRACT_GC_NOTRIGGER);}
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


// if no wraparound that the timer is expired if duetime is less than current time
// if wraparound occurred, then the timer expired if dueTime was greater than last time or dueTime is less equal to current time
#define TimeExpired(last,now,duetime) ((last) <= (now) ? \
                                       ((duetime) <= (now) && (duetime) >= (last)): \
                                       ((duetime) >= (last) || (duetime) <= (now)))

#define TimeInterval(end,start) ((end) > (start) ? ((end) - (start)) : ((0xffffffff - (start)) + (end) + 1))

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

    Thread * pThread = GetThreadNULLOk();
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


void ThreadpoolMgr::DeleteWait(WaitInfo* waitInfo)
{
    CONTRACTL
    {
        if (waitInfo->ExternalEventSafeHandle != NULL) { THROWS;} else { NOTHROW; }
        MODE_ANY;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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


/************************************************************************/

#ifndef TARGET_UNIX

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

    if (CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
    {
#if !defined(FEATURE_NATIVEAOT) && !defined(TARGET_UNIX)
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
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}  // There can be calls thru ICorThreadpool
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
        ClrSleepEx(timeout, TRUE);

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
        if (GetThreadNULLOk()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        if (GetThreadNULLOk()) { MODE_PREEMPTIVE;} else { DISABLED(MODE_ANY);}
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

                GCX_COOP();

                ARG_SLOT args[] = { PtrToArgSlot(AsyncTimerCallbackCompletion), PtrToArgSlot(timerInfo) };
                MethodDescCallSite(METHOD__THREAD_POOL__UNSAFE_QUEUE_UNMANAGED_WORK_ITEM).Call(args);

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

    Thread* pThread = GetThreadNULLOk();
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

    Thread * pThread = GetThreadNULLOk();
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
        if (GetThreadNULLOk() == pTimerThread) { NOTHROW; } else { THROWS; }
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
    if (GetThreadNULLOk() == pTimerThread)
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
        _ASSERTE(GetThreadNULLOk() != pTimerThread);

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
