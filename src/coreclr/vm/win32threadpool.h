// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*++

Module Name:

    Win32ThreadPool.h

Abstract:

    This module is the header file for thread pools using Win32 APIs.

Revision History:


--*/

#ifndef _WIN32THREADPOOL_H
#define _WIN32THREADPOOL_H

#include "delegateinfo.h"
#include "util.hpp"
#include "nativeoverlapped.h"
#include "hillclimbing.h"

#define MAX_WAITHANDLES 64

#define MAX_CACHED_EVENTS 40        // upper limit on number of wait events cached

#define WAIT_REGISTERED     0x01
#define WAIT_ACTIVE         0x02
#define WAIT_DELETE         0x04

#define TIMER_REGISTERED    0x01
#define TIMER_ACTIVE        0x02
#define TIMER_DELETE        0x04

#define WAIT_SINGLE_EXECUTION      0x00000001
#define WAIT_FREE_CONTEXT          0x00000002
#define WAIT_INTERNAL_COMPLETION   0x00000004

#define QUEUE_ONLY                 0x00000000  // do not attempt to call on the thread
#define CALL_OR_QUEUE              0x00000001  // call on the same thread if not too busy, else queue

const int MaxLimitThreadsPerCPU=250;               //  upper limit on number of cp threads per CPU
const int MaxFreeCPThreadsPerCPU=2;                 //  upper limit on number of  free cp threads per CPU

const int CpuUtilizationHigh=95;                    // remove threads when above this
const int CpuUtilizationLow =80;                    // inject more threads if below this

#ifndef TARGET_UNIX
extern HANDLE (WINAPI *g_pufnCreateIoCompletionPort)(HANDLE FileHandle,
											  HANDLE ExistingCompletionPort,
											  ULONG_PTR CompletionKey,
											  DWORD NumberOfConcurrentThreads);

extern int (WINAPI *g_pufnNtQueryInformationThread) (HANDLE ThreadHandle,
											  THREADINFOCLASS ThreadInformationClass,
                                              PVOID ThreadInformation,
                                              ULONG ThreadInformationLength,
                                              PULONG ReturnLength);

extern int (WINAPI * g_pufnNtQuerySystemInformation) (SYSTEM_INFORMATION_CLASS SystemInformationClass,
                                                      PVOID SystemInformation,
                                                      ULONG SystemInformationLength,
                                                      PULONG ReturnLength OPTIONAL);
#endif // !TARGET_UNIX

#define FILETIME_TO_INT64(t) (*(__int64*)&(t))
#define MILLI_TO_100NANO(x)  ((x) * 10000)        // convert from milliseond to 100 nanosecond unit

/**
 * This type is supposed to be private to ThreadpoolMgr.
 * It's at global scope because Strike needs to be able to access its
 * definition.
 */
struct WorkRequest {
    WorkRequest*            next;
    LPTHREAD_START_ROUTINE  Function;
    PVOID                   Context;

};

typedef struct _IOCompletionContext
{
    DWORD ErrorCode;
    DWORD numBytesTransferred;
    LPOVERLAPPED lpOverlapped;
    size_t key;
} IOCompletionContext, *PIOCompletionContext;

typedef DPTR(WorkRequest) PTR_WorkRequest;
class ThreadpoolMgr
{
    friend class ClrDataAccess;
    friend struct DelegateInfo;
    friend class ThreadPoolNative;
    friend class TimerNative;
    friend class UnManagedPerAppDomainTPCount;
    friend class ManagedPerAppDomainTPCount;
    friend class PerAppDomainTPCountList;
    friend class HillClimbing;
    friend struct _DacGlobals;

public:
    struct ThreadCounter
    {
        static const int MaxPossibleCount = 0x7fff;

        // padding to ensure we get our own cache line
        BYTE padding1[MAX_CACHE_LINE_SIZE];

        union Counts
        {
            struct
            {
                //
                // Note: these are signed rather than unsigned to allow us to detect under/overflow.
                //
                int MaxWorking  : 16;  //Determined by HillClimbing; adjusted elsewhere for timeouts, etc.
                int NumActive  : 16;  //Active means working or waiting on WorkerSemaphore.  These are "warm/hot" threads.
                int NumWorking : 16;  //Trying to get work from various queues.  Not waiting on either semaphore.
                int NumRetired : 16;  //Not trying to get work; waiting on RetiredWorkerSemaphore.  These are "cold" threads.

                // Note: the only reason we need "retired" threads at all is that it allows some threads to eventually time out
                // even if other threads are getting work.  If we ever make WorkerSemaphore a true LIFO semaphore, we will no longer
                // need the concept of "retirement" - instead, the very "coldest" threads will naturally be the first to time out.
            };

            LONGLONG AsLongLong;

            bool operator==(Counts other) {LIMITED_METHOD_CONTRACT; return AsLongLong == other.AsLongLong;}
        } counts;

        // padding to ensure we get our own cache line
        BYTE padding2[MAX_CACHE_LINE_SIZE];

        Counts GetCleanCounts()
        {
            LIMITED_METHOD_CONTRACT;
#ifdef HOST_64BIT
            // VolatileLoad x64 bit read is atomic
            return DangerousGetDirtyCounts();
#else // !HOST_64BIT
            // VolatileLoad may result in torn read
            Counts result;
#ifndef DACCESS_COMPILE
            result.AsLongLong = FastInterlockCompareExchangeLong(&counts.AsLongLong, 0, 0);
            ValidateCounts(result);
#else
            result.AsLongLong = 0; //prevents prefast warning for DAC builds
#endif
            return result;
#endif // !HOST_64BIT
        }

        //
        // This does a non-atomic read of the counts.  The returned value is suitable only
        // for use inside of a read-compare-exchange loop, where the compare-exhcange must succeed
        // before any action is taken.  Use GetCleanWorkerCounts for other needs, but keep in mind
        // it's much slower.
        //
        Counts DangerousGetDirtyCounts()
        {
            LIMITED_METHOD_CONTRACT;
            Counts result;
#ifndef DACCESS_COMPILE
            result.AsLongLong = VolatileLoad(&counts.AsLongLong);
#else
            result.AsLongLong = 0; //prevents prefast warning for DAC builds
#endif
            return result;
        }


        Counts CompareExchangeCounts(Counts newCounts, Counts oldCounts)
        {
            LIMITED_METHOD_CONTRACT;
            Counts result;
#ifndef DACCESS_COMPILE
            result.AsLongLong = FastInterlockCompareExchangeLong(&counts.AsLongLong, newCounts.AsLongLong, oldCounts.AsLongLong);
            if (result == oldCounts)
            {
                // can only do validation on success; if we failed, it may have been due to a previous
                // dirty read, which may contain invalid values.
                ValidateCounts(result);
                ValidateCounts(newCounts);
            }
#else
            result.AsLongLong = 0; //prevents prefast warning for DAC builds
#endif
            return result;
        }

    private:
        static void ValidateCounts(Counts counts)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(counts.MaxWorking > 0);
            _ASSERTE(counts.NumActive >= 0);
            _ASSERTE(counts.NumWorking >= 0);
            _ASSERTE(counts.NumRetired >= 0);
            _ASSERTE(counts.NumWorking <= counts.NumActive);
        }
    };

public:

    static void ReportThreadStatus(bool isWorking);

    // enumeration of different kinds of memory blocks that are recycled
    enum MemType
    {
        MEMTYPE_AsyncCallback   = 0,
        MEMTYPE_DelegateInfo    = 1,
        MEMTYPE_WorkRequest     = 2,
        MEMTYPE_COUNT           = 3,
    };

    typedef struct {
        INT32 TimerId;
    } TimerInfoContext;

#ifndef DACCESS_COMPILE
    static void StaticInitialize()
    {
        WRAPPER_NO_CONTRACT;
        s_usePortableThreadPool = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_UsePortableThreadPool) != 0;
    }
#endif

    static bool UsePortableThreadPool()
    {
        LIMITED_METHOD_CONTRACT;
        return s_usePortableThreadPool;
    }

    static BOOL Initialize();

    static BOOL SetMaxThreadsHelper(DWORD MaxWorkerThreads,
                                        DWORD MaxIOCompletionThreads);

    static bool CanSetMinIOCompletionThreads(DWORD ioCompletionThreads);
    static bool CanSetMaxIOCompletionThreads(DWORD ioCompletionThreads);

    static BOOL SetMaxThreads(DWORD MaxWorkerThreads,
                              DWORD MaxIOCompletionThreads);

    static BOOL GetMaxThreads(DWORD* MaxWorkerThreads,
                              DWORD* MaxIOCompletionThreads);

    static BOOL SetMinThreads(DWORD MinWorkerThreads,
                              DWORD MinIOCompletionThreads);

    static BOOL GetMinThreads(DWORD* MinWorkerThreads,
                              DWORD* MinIOCompletionThreads);

    static BOOL GetAvailableThreads(DWORD* AvailableWorkerThreads,
                                 DWORD* AvailableIOCompletionThreads);

    static INT32 GetThreadCount();

    static BOOL QueueUserWorkItem(LPTHREAD_START_ROUTINE Function,
                                  PVOID Context,
                                  ULONG Flags,
                                  BOOL UnmanagedTPRequest=TRUE);

    static BOOL PostQueuedCompletionStatus(LPOVERLAPPED lpOverlapped,
                                  LPOVERLAPPED_COMPLETION_ROUTINE Function);

    inline static BOOL IsCompletionPortInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        return GlobalCompletionPort != NULL;
    }

    static BOOL RegisterWaitForSingleObject(PHANDLE phNewWaitObject,
                                            HANDLE hWaitObject,
                                            WAITORTIMERCALLBACK Callback,
                                            PVOID Context,
                                            ULONG timeout,
                                            DWORD dwFlag);

    static BOOL UnregisterWaitEx(HANDLE hWaitObject,HANDLE CompletionEvent);
    static void WaitHandleCleanup(HANDLE hWaitObject);

    static BOOL WINAPI BindIoCompletionCallback(HANDLE FileHandle,
                                            LPOVERLAPPED_COMPLETION_ROUTINE Function,
                                            ULONG Flags,
                                            DWORD& errorCode);

    static void WINAPI WaitIOCompletionCallback(DWORD dwErrorCode,
                                            DWORD numBytesTransferred,
                                            LPOVERLAPPED lpOverlapped);

#ifdef TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
    static void WINAPI ManagedWaitIOCompletionCallback(DWORD dwErrorCode,
                                                       DWORD dwNumberOfBytesTransfered,
                                                       LPOVERLAPPED lpOverlapped);
#endif


    static BOOL SetAppDomainRequestsActive(BOOL UnmanagedTP = FALSE);
    static void ClearAppDomainRequestsActive(BOOL UnmanagedTP = FALSE,  LONG index = -1);

    static inline void UpdateLastDequeueTime()
    {
        LIMITED_METHOD_CONTRACT;
        VolatileStore(&LastDequeueTime, (unsigned int)GetTickCount());
    }

    static BOOL CreateTimerQueueTimer(PHANDLE phNewTimer,
                                        WAITORTIMERCALLBACK Callback,
                                        PVOID Parameter,
                                        DWORD DueTime,
                                        DWORD Period,
                                        ULONG Flags);

    static BOOL ChangeTimerQueueTimer(HANDLE Timer,
                                      ULONG DueTime,
                                      ULONG Period);
    static BOOL DeleteTimerQueueTimer(HANDLE Timer,
                                      HANDLE CompletionEvent);

    static void RecycleMemory(LPVOID mem, enum MemType memType);

    static void FlushQueueOfTimerInfos();

    static BOOL HaveTimerInfosToFlush() { return TimerInfosToBeRecycled != NULL; }

#ifndef TARGET_UNIX
    static LPOVERLAPPED CompletionPortDispatchWorkWithinAppDomain(Thread* pThread, DWORD* pErrorCode, DWORD* pNumBytes, size_t* pKey);
    static void StoreOverlappedInfoInThread(Thread* pThread, DWORD dwErrorCode, DWORD dwNumBytes, size_t key, LPOVERLAPPED lpOverlapped);
#endif // !TARGET_UNIX

    // Enable filtering of correlation ETW events for cases handled at a higher abstraction level

#ifndef DACCESS_COMPILE
    static FORCEINLINE BOOL AreEtwQueueEventsSpeciallyHandled(LPTHREAD_START_ROUTINE Function)
    {
        // Timer events are handled at a higher abstraction level: in the managed Timer class
        return (Function == ThreadpoolMgr::AsyncTimerCallbackCompletion);
    }

    static FORCEINLINE BOOL AreEtwIOQueueEventsSpeciallyHandled(LPOVERLAPPED_COMPLETION_ROUTINE Function)
    {
        // We handle registered waits at a higher abstraction level
        return (Function == ThreadpoolMgr::WaitIOCompletionCallback
#ifdef TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
                || Function == ThreadpoolMgr::ManagedWaitIOCompletionCallback
#endif
            );
    }
#endif

private:

#ifndef DACCESS_COMPILE

    inline static void FreeWorkRequest(WorkRequest* workRequest)
    {
        RecycleMemory( workRequest, MEMTYPE_WorkRequest ); //delete workRequest;
    }

    inline static WorkRequest* MakeWorkRequest(LPTHREAD_START_ROUTINE  function, PVOID context)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;;

        WorkRequest* wr = (WorkRequest*) GetRecycledMemory(MEMTYPE_WorkRequest);
        _ASSERTE(wr);
		if (NULL == wr)
			return NULL;
        wr->Function = function;
        wr->Context = context;
        wr->next = NULL;
        return wr;
    }

#endif // #ifndef DACCESS_COMPILE

    typedef struct {
        DWORD           numBytes;
        ULONG_PTR      *key;
        LPOVERLAPPED    pOverlapped;
        DWORD           errorCode;
    } QueuedStatus;

    typedef DPTR(struct _LIST_ENTRY)                        PTR_LIST_ENTRY;
    typedef struct _LIST_ENTRY {
        struct _LIST_ENTRY *Flink;
        struct _LIST_ENTRY *Blink;
    } LIST_ENTRY, *PLIST_ENTRY;

    struct WaitInfo;

    typedef struct {
        HANDLE          threadHandle;
        DWORD           threadId;
        CLREvent        startEvent;
        LONG            NumWaitHandles;                 // number of wait objects registered to the thread <=64
        LONG            NumActiveWaits;                 // number of objects, thread is actually waiting on (this may be less than
                                                           // NumWaitHandles since the thread may not have activated some waits
        HANDLE          waitHandle[MAX_WAITHANDLES];    // array of wait handles (copied from waitInfo since
                                                           // we need them to be contiguous)
        LIST_ENTRY      waitPointer[MAX_WAITHANDLES];   // array of doubly linked list of corresponding waitinfo
    } ThreadCB;


    typedef struct {
        ULONG               startTime;          // time at which wait was started
                                                // endTime = startTime+timeout
        ULONG               remainingTime;      // endTime - currentTime
    } WaitTimerInfo;

    struct  WaitInfo {
        LIST_ENTRY          link;               // Win9x does not allow duplicate waithandles, so we need to
                                                // group all waits on a single waithandle using this linked list
        HANDLE              waitHandle;
        WAITORTIMERCALLBACK Callback;
        PVOID               Context;
        ULONG               timeout;
        WaitTimerInfo       timer;
        DWORD               flag;
        DWORD               state;
        ThreadCB*           threadCB;
        LONG                refCount;                // when this reaches 0, the waitInfo can be safely deleted
        CLREvent            PartialCompletionEvent;  // used to synchronize deactivation of a wait
        CLREvent            InternalCompletionEvent; // only one of InternalCompletion or ExternalCompletion is used
                                                     // but I cant make a union since CLREvent has a non-default constructor
        HANDLE              ExternalCompletionEvent; // they are signalled when all callbacks have completed (refCount=0)
        OBJECTHANDLE        ExternalEventSafeHandle;

    } ;

    // structure used to maintain global information about wait threads. Protected by WaitThreadsCriticalSection
    typedef struct WaitThreadTag {
        LIST_ENTRY      link;
        ThreadCB*       threadCB;
    } WaitThreadInfo;


    struct AsyncCallback{
        WaitInfo*   wait;
        BOOL        waitTimedOut;
    } ;

#ifndef DACCESS_COMPILE

    static VOID
    AcquireAsyncCallback(AsyncCallback *pAsyncCB)
    {
        LIMITED_METHOD_CONTRACT;
    }

    static VOID
    ReleaseAsyncCallback(AsyncCallback *pAsyncCB)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

            WaitInfo *waitInfo = pAsyncCB->wait;
            ThreadpoolMgr::RecycleMemory((LPVOID*)pAsyncCB, ThreadpoolMgr::MEMTYPE_AsyncCallback);

            // if this was a single execution, we now need to stop rooting registeredWaitHandle
            // in a GC handle. This will cause the finalizer to pick it up and call the cleanup
            // routine.
            if ( (waitInfo->flag & WAIT_SINGLE_EXECUTION)  && (waitInfo->flag & WAIT_FREE_CONTEXT))
            {

                DelegateInfo* pDelegate = (DelegateInfo*) waitInfo->Context;

                _ASSERTE(pDelegate->m_registeredWaitHandle);

                {
                    GCX_COOP();
                    StoreObjectInHandle(pDelegate->m_registeredWaitHandle, NULL);
                }
            }

            if (InterlockedDecrement(&waitInfo->refCount) == 0)
                ThreadpoolMgr::DeleteWait(waitInfo);

    }

    typedef Holder<AsyncCallback *, ThreadpoolMgr::AcquireAsyncCallback, ThreadpoolMgr::ReleaseAsyncCallback> AsyncCallbackHolder;
    inline static AsyncCallback* MakeAsyncCallback()
    {
        WRAPPER_NO_CONTRACT;
        return (AsyncCallback*) GetRecycledMemory(MEMTYPE_AsyncCallback);
    }

    static VOID ReleaseInfo(OBJECTHANDLE& hndSafeHandle,
        HANDLE hndNativeHandle)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_TRIGGERS;
        }
        CONTRACTL_END

// Use of EX_TRY, GCPROTECT etc in the same function is causing prefast to complain about local variables with
// same name masking each other (#246). The error could not be suppressed with "#pragma PREFAST_SUPPRESS"
#ifndef _PREFAST_

        if (hndSafeHandle != NULL)
        {

            SAFEHANDLEREF refSH = NULL;

            GCX_COOP();
            GCPROTECT_BEGIN(refSH);

            {
                EX_TRY
                {
                    // Read the GC handle
                    refSH = (SAFEHANDLEREF) ObjectToOBJECTREF(ObjectFromHandle(hndSafeHandle));

                    // Destroy the GC handle
                    DestroyHandle(hndSafeHandle);

                    if (refSH != NULL)
                    {
                        SafeHandleHolder h(&refSH);

                        HANDLE hEvent = refSH->GetHandle();
                        if (hEvent != INVALID_HANDLE_VALUE)
                        {
                            SetEvent(hEvent);
                        }
                    }
                }
                EX_CATCH
                {
                }
                EX_END_CATCH(SwallowAllExceptions);
            }

            GCPROTECT_END();

            hndSafeHandle = NULL;
        }
#endif
    }

#endif // #ifndef DACCESS_COMPILE

    typedef struct {
        LIST_ENTRY  link;
        HANDLE      Handle;
    } WaitEvent ;

    // Timer
    typedef struct {
        LIST_ENTRY  link;           // doubly linked list of timers
        ULONG FiringTime;           // TickCount of when to fire next
        WAITORTIMERCALLBACK Function;             // Function to call when timer fires
        PVOID Context;              // Context to pass to function when timer fires
        ULONG Period;
        DWORD flag;                 // How do we deal with the context
        DWORD state;
        LONG refCount;
        HANDLE ExternalCompletionEvent;     // only one of this is used, but cant do a union since CLREvent has a non-default constructor
        CLREvent InternalCompletionEvent;   // flags indicates which one is being used
        OBJECTHANDLE    ExternalEventSafeHandle;
    } TimerInfo;

    static VOID AcquireWaitInfo(WaitInfo *pInfo)
    {
    }
    static VOID ReleaseWaitInfo(WaitInfo *pInfo)
    {
        WRAPPER_NO_CONTRACT;
#ifndef DACCESS_COMPILE
        ReleaseInfo(pInfo->ExternalEventSafeHandle,
        pInfo->ExternalCompletionEvent);
#endif
    }
    static VOID AcquireTimerInfo(TimerInfo *pInfo)
    {
    }
    static VOID ReleaseTimerInfo(TimerInfo *pInfo)
    {
        WRAPPER_NO_CONTRACT;
#ifndef DACCESS_COMPILE
        ReleaseInfo(pInfo->ExternalEventSafeHandle,
        pInfo->ExternalCompletionEvent);
#endif
    }

    typedef Holder<WaitInfo *, ThreadpoolMgr::AcquireWaitInfo, ThreadpoolMgr::ReleaseWaitInfo> WaitInfoHolder;
    typedef Holder<TimerInfo *, ThreadpoolMgr::AcquireTimerInfo, ThreadpoolMgr::ReleaseTimerInfo> TimerInfoHolder;

    typedef struct {
        TimerInfo* Timer;           // timer to be updated
        ULONG DueTime ;             // new due time
        ULONG Period ;              // new period
    } TimerUpdateInfo;

    // Definitions and data structures to support recycling of high-frequency
    // memory blocks. We use a spin-lock to access the list

    class RecycledListInfo
    {
        static const unsigned int MaxCachedEntries = 40;

        struct Entry
	    {
	        Entry* next;
	    };

        Volatile<LONG> lock;   		// this is the spin lock
        DWORD         count;  		// count of number of elements in the list
        Entry*        root;   		// ptr to first element of recycled list
#ifndef HOST_64BIT
		DWORD         filler;       // Pad the structure to a multiple of the 16.
#endif

		//--//

public:
		RecycledListInfo()
		{
            LIMITED_METHOD_CONTRACT;

            lock  = 0;
            root  = NULL;
            count = 0;
		}

		FORCEINLINE bool CanInsert()
		{
            LIMITED_METHOD_CONTRACT;

			return count < MaxCachedEntries;
		}

	    FORCEINLINE LPVOID Remove()
	    {
	    	LIMITED_METHOD_CONTRACT;

			if(root == NULL) return NULL; // No need for acquiring the lock, there's nothing to remove.

	        AcquireLock();

	        Entry* ret = (Entry*)root;

	        if(ret)
	        {
	            root   = ret->next;
	            count -= 1;
	        }

	        ReleaseLock();

	        return ret;
	    }

	    FORCEINLINE void Insert( LPVOID mem )
	    {
	    	LIMITED_METHOD_CONTRACT;

		    AcquireLock();

	        Entry* entry = (Entry*)mem;

	        entry->next = root;

	        root   = entry;
	        count += 1;

	        ReleaseLock();
	    }

	private:
	    FORCEINLINE void AcquireLock()
	    {
	    	LIMITED_METHOD_CONTRACT;

	        unsigned int rounds = 0;

	        DWORD dwSwitchCount = 0;

	        while(lock != 0 || FastInterlockExchange( &lock, 1 ) != 0)
	        {
                YieldProcessorNormalized(); // indicate to the processor that we are spinning

	            rounds++;

	            if((rounds % 32) == 0)
	            {
	                __SwitchToThread( 0, ++dwSwitchCount );
	            }
	        }
	    }

	    FORCEINLINE void ReleaseLock()
	    {
	    	LIMITED_METHOD_CONTRACT;

	    	lock = 0;
	    }
	};

    //
    // It's critical that we ensure these pointers are allocated by the linker away from
    // variables that are modified a lot at runtime.
    //
    // The use of the CacheGuard is a temporary solution,
    // the thread pool has to be refactor away from static variable and
    // toward a single global structure, where we can control the locality of variables.
    //
    class RecycledListsWrapper
    {
        DWORD                        CacheGuardPre[MAX_CACHE_LINE_SIZE/sizeof(DWORD)];

        RecycledListInfo            (*pRecycledListPerProcessor)[MEMTYPE_COUNT];  // RecycledListInfo [numProc][MEMTYPE_COUNT]

        DWORD                        CacheGuardPost[MAX_CACHE_LINE_SIZE/sizeof(DWORD)];

    public:
        void Initialize( unsigned int numProcs );

        FORCEINLINE bool IsInitialized()
        {
            LIMITED_METHOD_CONTRACT;

            return pRecycledListPerProcessor != NULL;
        }

#ifndef DACCESS_COMPILE
    	FORCEINLINE RecycledListInfo& GetRecycleMemoryInfo( enum MemType memType )
        {
            LIMITED_METHOD_CONTRACT;

            DWORD processorNumber = 0;

#ifndef TARGET_UNIX
	        if (CPUGroupInfo::CanEnableGCCPUGroups() && CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
                processorNumber = CPUGroupInfo::CalculateCurrentProcessorNumber();
            else
                // Turns out GetCurrentProcessorNumber can return a value greater than the number of processors reported by
                // GetSystemInfo, if we're running in WOW64 on a machine with >32 processors.
        	    processorNumber = GetCurrentProcessorNumber()%NumberOfProcessors;
#else // !TARGET_UNIX
            if (PAL_HasGetCurrentProcessorNumber())
            {
                // On linux, GetCurrentProcessorNumber which uses sched_getcpu() can return a value greater than the number
                // of processors reported by sysconf(_SC_NPROCESSORS_ONLN) when using OpenVZ kernel.
                processorNumber = GetCurrentProcessorNumber()%NumberOfProcessors;
            }
#endif // !TARGET_UNIX
            return pRecycledListPerProcessor[processorNumber][memType];
    	}
#endif // DACCESS_COMPILE
    };

#define GATE_THREAD_STATUS_NOT_RUNNING         0 // There is no gate thread
#define GATE_THREAD_STATUS_REQUESTED           1 // There is a gate thread, and someone has asked it to stick around recently
#define GATE_THREAD_STATUS_WAITING_FOR_REQUEST 2 // There is a gate thread, but nobody has asked it to stay.  It may die soon

    // Private methods

    static Thread* CreateUnimpersonatedThread(LPTHREAD_START_ROUTINE lpStartAddress, LPVOID lpArgs, BOOL *pIsCLRThread);

    static BOOL CreateWorkerThread();

    static void EnqueueWorkRequest(WorkRequest* wr);

    static WorkRequest* DequeueWorkRequest();

    static void ExecuteWorkRequest(bool* foundWork, bool* wasNotRecalled);

#ifndef DACCESS_COMPILE

    inline static void AppendWorkRequest(WorkRequest* entry)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        _ASSERTE(!UsePortableThreadPool());

        if (WorkRequestTail)
        {
            _ASSERTE(WorkRequestHead != NULL);
            WorkRequestTail->next = entry;
        }
        else
        {
            _ASSERTE(WorkRequestHead == NULL);
            WorkRequestHead = entry;
        }

        WorkRequestTail = entry;
        _ASSERTE(WorkRequestTail->next == NULL);
    }

    inline static WorkRequest* RemoveWorkRequest()
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        _ASSERTE(!UsePortableThreadPool());

        WorkRequest* entry = NULL;
        if (WorkRequestHead)
        {
            entry = WorkRequestHead;
            WorkRequestHead = entry->next;
            if (WorkRequestHead == NULL)
                WorkRequestTail = NULL;
        }
        return entry;
    }

public:
    static void EnsureInitialized();
private:
    static void EnsureInitializedSlow();

public:
    static void InitPlatformVariables();

    inline static BOOL IsInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        return Initialization == -1;
    }

    static void MaybeAddWorkingWorker();

    static void NotifyWorkItemCompleted()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(!UsePortableThreadPool());

        Thread::IncrementWorkerThreadPoolCompletionCount(GetThreadNULLOk());
        UpdateLastDequeueTime();
    }

    static bool ShouldAdjustMaxWorkersActive()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(!UsePortableThreadPool());

        DWORD priorTime = PriorCompletedWorkRequestsTime;
        MemoryBarrier(); // read fresh value for NextCompletedWorkRequestsTime below
        DWORD requiredInterval = NextCompletedWorkRequestsTime - priorTime;
        DWORD elapsedInterval = GetTickCount() - priorTime;
        if (elapsedInterval >= requiredInterval)
        {
            ThreadCounter::Counts counts = WorkerCounter.GetCleanCounts();
            if (counts.NumActive <= counts.MaxWorking)
                return !IsHillClimbingDisabled;
        }

        return false;
    }

    static void AdjustMaxWorkersActive();
    static bool ShouldWorkerKeepRunning();

    static DWORD SafeWait(CLREvent * ev, DWORD sleepTime, BOOL alertable);

    static DWORD WINAPI WorkerThreadStart(LPVOID lpArgs);

    static BOOL AddWaitRequest(HANDLE waitHandle, WaitInfo* waitInfo);


    static ThreadCB* FindWaitThread();              // returns a wait thread that can accomodate another wait request

    static BOOL CreateWaitThread();

    static void WINAPI InsertNewWaitForSelf(WaitInfo* pArg);

    static int FindWaitIndex(const ThreadCB* threadCB, const HANDLE waitHandle);

    static DWORD MinimumRemainingWait(LIST_ENTRY* waitInfo, unsigned int numWaits);

    static void ProcessWaitCompletion( WaitInfo* waitInfo,
                                unsigned index,      // array index
                                BOOL waitTimedOut);

#ifdef TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
    static void ManagedWaitIOCompletionCallback_Worker(LPVOID state);
#endif

    static DWORD WINAPI WaitThreadStart(LPVOID lpArgs);

    static DWORD WINAPI AsyncCallbackCompletion(PVOID pArgs);

    static void QueueTimerInfoForRelease(TimerInfo *pTimerInfo);

    static void DeactivateWait(WaitInfo* waitInfo);
    static void DeactivateNthWait(WaitInfo* waitInfo, DWORD index);

    static void DeleteWait(WaitInfo* waitInfo);


    inline static void ShiftWaitArray( ThreadCB* threadCB,
                                       ULONG SrcIndex,
                                       ULONG DestIndex,
                                       ULONG count)
    {
        LIMITED_METHOD_CONTRACT;
        memmove(&threadCB->waitHandle[DestIndex],
               &threadCB->waitHandle[SrcIndex],
               count * sizeof(HANDLE));
        memmove(&threadCB->waitPointer[DestIndex],
               &threadCB->waitPointer[SrcIndex],
               count * sizeof(LIST_ENTRY));
    }

    static void WINAPI DeregisterWait(WaitInfo* pArgs);

#ifndef TARGET_UNIX
    // holds the aggregate of system cpu usage of all processors
    typedef struct _PROCESS_CPU_INFORMATION
    {
        LARGE_INTEGER idleTime;
        LARGE_INTEGER kernelTime;
        LARGE_INTEGER userTime;
        DWORD_PTR affinityMask;
        int  numberOfProcessors;
        SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION* usageBuffer;
        int  usageBufferSize;
    } PROCESS_CPU_INFORMATION;

    static int GetCPUBusyTime_NT(PROCESS_CPU_INFORMATION* pOldInfo);
    static BOOL CreateCompletionPortThread(LPVOID lpArgs);
    static DWORD WINAPI CompletionPortThreadStart(LPVOID lpArgs);
public:
    inline static bool HaveNativeWork()
    {
        LIMITED_METHOD_CONTRACT;
        return WorkRequestHead != NULL;
    }

    static void GrowCompletionPortThreadpoolIfNeeded();
    static BOOL ShouldGrowCompletionPortThreadpool(ThreadCounter::Counts counts);
#else
    static int GetCPUBusyTime_NT(PAL_IOCP_CPU_INFORMATION* pOldInfo);

#endif // !TARGET_UNIX

private:
    static BOOL IsIoPending();

    static BOOL CreateGateThread();
    static void EnsureGateThreadRunning();
    static bool NeedGateThreadForIOCompletions();
    static bool ShouldGateThreadKeepRunning();
    static DWORD WINAPI GateThreadStart(LPVOID lpArgs);
    static void PerformGateActivities(int cpuUtilization);
    static BOOL SufficientDelaySinceLastSample(unsigned int LastThreadCreationTime,
                                               unsigned NumThreads, // total number of threads of that type (worker or CP)
                                               double   throttleRate=0.0 // the delay is increased by this percentage for each extra thread
                                               );
    static BOOL SufficientDelaySinceLastDequeue();

    static LPVOID   GetRecycledMemory(enum MemType memType);

    static DWORD WINAPI TimerThreadStart(LPVOID args);
    static void TimerThreadFire(); // helper method used by TimerThreadStart
    static void WINAPI InsertNewTimer(TimerInfo* pArg);
    static DWORD FireTimers();
    static DWORD WINAPI AsyncTimerCallbackCompletion(PVOID pArgs);
    static void DeactivateTimer(TimerInfo* timerInfo);
    static DWORD WINAPI AsyncDeleteTimer(PVOID pArgs);
    static void DeleteTimer(TimerInfo* timerInfo);
    static void WINAPI UpdateTimer(TimerUpdateInfo* pArgs);

    static void WINAPI DeregisterTimer(TimerInfo* pArgs);

    inline static DWORD QueueDeregisterWait(HANDLE waitThread, WaitInfo* waitInfo)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        _ASSERTE(!UsePortableThreadPool());

        DWORD result = QueueUserAPC(reinterpret_cast<PAPCFUNC>(DeregisterWait), waitThread, reinterpret_cast<ULONG_PTR>(waitInfo));
        SetWaitThreadAPCPending();
        return result;
    }


    inline static void SetWaitThreadAPCPending() {IsApcPendingOnWaitThread = TRUE;}
    inline static void ResetWaitThreadAPCPending() {IsApcPendingOnWaitThread = FALSE;}
    inline static BOOL IsWaitThreadAPCPending()  {return IsApcPendingOnWaitThread;}

#endif // #ifndef DACCESS_COMPILE
    // Private variables

    static Volatile<LONG> Initialization;                         // indicator of whether the threadpool is initialized.

    static bool s_usePortableThreadPool;

    SVAL_DECL(LONG,MinLimitTotalWorkerThreads);         // same as MinLimitTotalCPThreads
    SVAL_DECL(LONG,MaxLimitTotalWorkerThreads);         // same as MaxLimitTotalCPThreads

    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static unsigned int LastDequeueTime;      // used to determine if work items are getting thread starved

    static HillClimbing HillClimbingInstance;

    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static LONG PriorCompletedWorkRequests;
    static DWORD PriorCompletedWorkRequestsTime;
    static DWORD NextCompletedWorkRequestsTime;

    static LARGE_INTEGER CurrentSampleStartTime;

    static unsigned int WorkerThreadSpinLimit;
    static bool IsHillClimbingDisabled;
    static int ThreadAdjustmentInterval;

    SPTR_DECL(WorkRequest,WorkRequestHead);             // Head of work request queue
    SPTR_DECL(WorkRequest,WorkRequestTail);             // Head of work request queue

    static unsigned int LastCPThreadCreation;		// last time a completion port thread was created
    static unsigned int NumberOfProcessors;             // = NumberOfWorkerThreads - no. of blocked threads

    static BOOL IsApcPendingOnWaitThread;               // Indicates if an APC is pending on the wait thread

    // This needs to be non-hosted, because worker threads can run prior to EE startup.
    static DangerousNonHostedSpinLock ThreadAdjustmentLock;

public:
    static CrstStatic WorkerCriticalSection;

private:
    static const DWORD WorkerTimeout = 20 * 1000;

    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) SVAL_DECL(ThreadCounter,WorkerCounter);

    //
    // WorkerSemaphore is an UnfairSemaphore because:
    // 1) Threads enter and exit this semaphore very frequently, and thus benefit greatly from the spinning done by UnfairSemaphore
    // 2) There is no functional reason why any particular thread should be preferred when waking workers.  This only impacts performance,
    //    and un-fairness helps performance in this case.
    //
    static CLRLifoSemaphore* WorkerSemaphore;

    //
    // RetiredWorkerSemaphore is a regular CLRSemaphore, not an UnfairSemaphore, because if a thread waits on this semaphore is it almost certainly
    // NOT going to be released soon, so the spinning done in UnfairSemaphore only burns valuable CPU time.  However, if UnfairSemaphore is ever
    // implemented in terms of a Win32 IO Completion Port, we should reconsider this.  The IOCP's LIFO unblocking behavior could help keep working set
    // down, by constantly re-using the same small set of retired workers rather than round-robining between all of them as CLRSemaphore will do.
    // If we go that route, we should add a "no-spin" option to UnfairSemaphore.Wait to avoid wasting CPU.
    //
    static CLRLifoSemaphore* RetiredWorkerSemaphore;

    static CLREvent * RetiredCPWakeupEvent;

    static CrstStatic WaitThreadsCriticalSection;
    static LIST_ENTRY WaitThreadsHead;                  // queue of wait threads, each thread can handle upto 64 waits

    static TimerInfo *TimerInfosToBeRecycled;           // list of delegate infos associated with deleted timers
    static CrstStatic TimerQueueCriticalSection;        // critical section to synchronize timer queue access
    SVAL_DECL(LIST_ENTRY,TimerQueue);                   // queue of timers
    static HANDLE TimerThread;                          // Currently we only have one timer thread
    static Thread*  pTimerThread;
    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static DWORD LastTickCount;      // the count just before timer thread goes to sleep

    static BOOL InitCompletionPortThreadpool;           // flag indicating whether completion port threadpool has been initialized
    static HANDLE GlobalCompletionPort;                 // used for binding io completions on file handles

public:
    SVAL_DECL(ThreadCounter,CPThreadCounter);

private:
    SVAL_DECL(LONG,MaxLimitTotalCPThreads);             // = MaxLimitCPThreadsPerCPU * number of CPUS
    SVAL_DECL(LONG,MinLimitTotalCPThreads);
    SVAL_DECL(LONG,MaxFreeCPThreads);                   // = MaxFreeCPThreadsPerCPU * Number of CPUS

    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static LONG GateThreadStatus;    // See GateThreadStatus enumeration

    SVAL_DECL(LONG,cpuUtilization);

    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static RecycledListsWrapper RecycledLists;
};




#endif // _WIN32THREADPOOL_H
