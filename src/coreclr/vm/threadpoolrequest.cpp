// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//=========================================================================

//
// ThreadPoolRequest.cpp
//

//
//
//=========================================================================

#include "common.h"
#include "comdelegate.h"
#include "comthreadpool.h"
#include "threadpoolrequest.h"
#include "win32threadpool.h"
#include "class.h"
#include "object.h"
#include "field.h"
#include "excep.h"
#include "eeconfig.h"
#include "corhost.h"
#include "nativeoverlapped.h"
#include "appdomain.inl"

BYTE PerAppDomainTPCountList::s_padding[MAX_CACHE_LINE_SIZE - sizeof(LONG)];
// Make this point to unmanaged TP in case, no appdomains have initialized yet.
// Cacheline aligned, hot variable
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) LONG PerAppDomainTPCountList::s_ADHint = -1;

// Move out of from preceeding variables' cache line
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) UnManagedPerAppDomainTPCount PerAppDomainTPCountList::s_unmanagedTPCount;
//The list of all per-appdomain work-request counts.
ArrayListStatic PerAppDomainTPCountList::s_appDomainIndexList;

void PerAppDomainTPCountList::InitAppDomainIndexList()
{
    LIMITED_METHOD_CONTRACT;

    if (!ThreadpoolMgr::UsePortableThreadPool())
    {
        s_appDomainIndexList.Init();
    }
}


//---------------------------------------------------------------------------
//AddNewTPIndex adds and returns a per-appdomain TP entry whenever a new appdomain
//is created. Our list count should be equal to the max number of appdomains created
//in the system.
//
//Assumptions:
//This function needs to be called under the SystemDomain lock.
//The ArrayListStatic data dtructure allows traversing of the counts without a
//lock, but addition to the list requires synchronization.
//
TPIndex PerAppDomainTPCountList::AddNewTPIndex()
{
    STANDARD_VM_CONTRACT;

    if (ThreadpoolMgr::UsePortableThreadPool())
    {
        return TPIndex();
    }

    DWORD count = s_appDomainIndexList.GetCount();
    DWORD i = FindFirstFreeTpEntry();

    if (i == UNUSED_THREADPOOL_INDEX)
        i = count;

    TPIndex index(i+1);
    if(count > i)
    {

        IPerAppDomainTPCount * pAdCount = dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(i));
        pAdCount->SetTPIndex(index);
        return index;
    }

#ifdef _MSC_VER
    // Disable this warning - we intentionally want __declspec(align()) to insert trailing padding for us
#pragma warning(disable:4316)  // Object allocated on the heap may not be aligned for this type.
#endif
    ManagedPerAppDomainTPCount * pAdCount = new ManagedPerAppDomainTPCount(index);
#ifdef _MSC_VER
#pragma warning(default:4316)  // Object allocated on the heap may not be aligned for this type.
#endif
    pAdCount->ResetState();

    IfFailThrow(s_appDomainIndexList.Append(pAdCount));

    return index;
}

DWORD PerAppDomainTPCountList::FindFirstFreeTpEntry()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    DWORD DwnumADs = s_appDomainIndexList.GetCount();
    DWORD Dwi;
    IPerAppDomainTPCount * pAdCount;
    DWORD DwfreeIndex = UNUSED_THREADPOOL_INDEX;

    for (Dwi=0;Dwi < DwnumADs;Dwi++)
    {
        pAdCount = dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(Dwi));
        _ASSERTE(pAdCount);

        if(pAdCount->IsTPIndexUnused())
        {
            DwfreeIndex = Dwi;
            STRESS_LOG1(LF_THREADPOOL, LL_INFO1000, "FindFirstFreeTpEntry: reusing index %d\n", DwfreeIndex + 1);
            break;
        }
    }

    return DwfreeIndex;
}

//---------------------------------------------------------------------------
//ResetAppDomainIndex: Resets the  AppDomain ID  and the  per-appdomain
//                     thread pool counts
//
//Arguments:
//index - The index into the s_appDomainIndexList for the AppDomain we're
//        trying to clear (the AD being unloaded)
//
//Assumptions:
//This function needs to be called from the AD unload thread after all domain
//bound objects have been finalized when it's safe to recycle  the TPIndex.
//ClearAppDomainRequestsActive can be called from this function because no
// managed code is running (If managed code is running, this function needs
//to be called under a managed per-appdomain lock).
//
void PerAppDomainTPCountList::ResetAppDomainIndex(TPIndex index)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (ThreadpoolMgr::UsePortableThreadPool())
    {
        _ASSERTE(index.m_dwIndex == TPIndex().m_dwIndex);
        return;
    }

    IPerAppDomainTPCount * pAdCount = dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(index.m_dwIndex-1));
    _ASSERTE(pAdCount);

    STRESS_LOG2(LF_THREADPOOL, LL_INFO1000, "ResetAppDomainIndex: index %d pAdCount %p\n", index.m_dwIndex, pAdCount);

    pAdCount->ResetState();
    pAdCount->SetTPIndexUnused();
}

//---------------------------------------------------------------------------
//AreRequestsPendingInAnyAppDomains checks to see if there any requests pending
//in other appdomains. It also checks for pending unmanaged work requests.
//This function is called at end of thread quantum to see if the thread needs to
//transition into a different appdomain. This function may also be called by
//the scheduler to check for any unscheduled work.
//
bool PerAppDomainTPCountList::AreRequestsPendingInAnyAppDomains()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    DWORD DwnumADs = s_appDomainIndexList.GetCount();
    DWORD Dwi;
    IPerAppDomainTPCount * pAdCount;
    bool fRequestsPending = false;

    for (Dwi=0;Dwi < DwnumADs;Dwi++)
    {

        pAdCount = dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(Dwi));
        _ASSERTE(pAdCount);

        if(pAdCount->IsRequestPending())
        {
            fRequestsPending = true;
            break;
        }
    }

    if(s_unmanagedTPCount.IsRequestPending())
    {
        fRequestsPending = true;
    }

    return fRequestsPending;
}


//---------------------------------------------------------------------------
//GetAppDomainIndexForThreadpoolDispatch is essentailly the
//"AppDomain Scheduler". This function makes fairness/policy decisions as to
//which appdomain the thread needs to enter to. This function needs to guarantee
//that all appdomain work requests are processed fairly. At this time all
//appdomain requests and the unmanaged work requests are treated with the same
//priority.
//
//Return Value:
//The appdomain ID in which to dispatch the worker thread,nmanaged work items
//need to be processed.
//
LONG PerAppDomainTPCountList::GetAppDomainIndexForThreadpoolDispatch()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    LONG hint = s_ADHint;
    DWORD count = s_appDomainIndexList.GetCount();
    IPerAppDomainTPCount * pAdCount;
    DWORD Dwi;


    if (hint != -1)
    {
        pAdCount = dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(hint));
    }
    else
    {
        pAdCount = &s_unmanagedTPCount;
    }

    //temphint ensures that the check for appdomains proceeds in a pure round robin fashion.
    LONG temphint = hint;

    _ASSERTE( pAdCount);

    if (pAdCount->TakeActiveRequest())
        goto HintDone;

    //If there is no work in any appdomains, check the unmanaged queue,
    hint = -1;

    for (Dwi=0;Dwi<count;Dwi++)
    {
        if (temphint == -1)
        {
            temphint = 0;
        }

        pAdCount = dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(temphint));
        if (pAdCount->TakeActiveRequest())
        {
            hint = temphint;
            goto HintDone;
        }

        temphint++;

        _ASSERTE( temphint <= (LONG)count);

        if(temphint == (LONG)count)
        {
            temphint = 0;
        }
    }

    if (hint == -1 && !s_unmanagedTPCount.TakeActiveRequest())
    {
        //no work!
        return 0;
    }

HintDone:

    if((hint+1) < (LONG)count)
    {
         s_ADHint = hint+1;
    }
    else
    {
        s_ADHint = -1;
    }

    if (hint == -1)
    {
        return hint;
    }
    else
    {
        return (hint+1);
    }
}


void UnManagedPerAppDomainTPCount::SetAppDomainRequestsActive()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

#ifndef DACCESS_COMPILE
    LONG count = VolatileLoad(&m_outstandingThreadRequestCount);
    while (count < (LONG)ThreadpoolMgr::NumberOfProcessors)
    {
        LONG prevCount = FastInterlockCompareExchange(&m_outstandingThreadRequestCount, count+1, count);
        if (prevCount == count)
        {
            ThreadpoolMgr::MaybeAddWorkingWorker();
            ThreadpoolMgr::EnsureGateThreadRunning();
            break;
        }
        count = prevCount;
    }
#endif
}

bool FORCEINLINE UnManagedPerAppDomainTPCount::TakeActiveRequest()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    LONG count = VolatileLoad(&m_outstandingThreadRequestCount);

    while (count > 0)
    {
        LONG prevCount = FastInterlockCompareExchange(&m_outstandingThreadRequestCount, count-1, count);
        if (prevCount == count)
            return true;
        count = prevCount;
    }

    return false;
}


FORCEINLINE void ReleaseWorkRequest(WorkRequest *workRequest) { ThreadpoolMgr::RecycleMemory( workRequest, ThreadpoolMgr::MEMTYPE_WorkRequest ); }
typedef Wrapper< WorkRequest *, DoNothing<WorkRequest *>, ReleaseWorkRequest > WorkRequestHolder;

void UnManagedPerAppDomainTPCount::QueueUnmanagedWorkRequest(LPTHREAD_START_ROUTINE  function, PVOID context)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

#ifndef DACCESS_COMPILE
    WorkRequestHolder pWorkRequest;

    //Note, ideally we would want to use our own queues instead of those in
    //the thread pool class. However, the queus in thread pool class have
    //caching support, that shares memory with other commonly used structures
    //in the VM thread pool implementation. So, we decided to leverage those.

    pWorkRequest = ThreadpoolMgr::MakeWorkRequest(function, context);

    //MakeWorkRequest should throw if unable to allocate memory
    _ASSERTE(pWorkRequest != NULL);
    PREFIX_ASSUME(pWorkRequest != NULL);

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolEnqueue) &&
        !ThreadpoolMgr::AreEtwQueueEventsSpeciallyHandled(function))
        FireEtwThreadPoolEnqueue(pWorkRequest, GetClrInstanceId());

    m_lock.Init(LOCK_TYPE_DEFAULT);

    {
        SpinLock::Holder slh(&m_lock);

        ThreadpoolMgr::EnqueueWorkRequest(pWorkRequest);
        pWorkRequest.SuppressRelease();
        m_NumRequests++;
    }

    SetAppDomainRequestsActive();
#endif //DACCESS_COMPILE
}

PVOID UnManagedPerAppDomainTPCount::DeQueueUnManagedWorkRequest(bool* lastOne)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    *lastOne = true;

    WorkRequest * pWorkRequest = ThreadpoolMgr::DequeueWorkRequest();

    if (pWorkRequest)
    {
        m_NumRequests--;

        if(m_NumRequests > 0)
            *lastOne = false;
    }

    return (PVOID) pWorkRequest;
}

//---------------------------------------------------------------------------
//DispatchWorkItem manages dispatching of unmanaged work requests. It keeps
//processing unmanaged requests for the "Quanta". Essentially this function is
//a tight loop of dequeueing unmanaged work requests and dispatching them.
//
void UnManagedPerAppDomainTPCount::DispatchWorkItem(bool* foundWork, bool* wasNotRecalled)
{
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

#ifndef DACCESS_COMPILE
    *foundWork = false;
    *wasNotRecalled = true;

    bool enableWorkerTracking = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking) ? true : false;

    DWORD startTime;
    DWORD endTime;

    startTime = GetTickCount();

    //For all practical puposes, the unmanaged part of thread pool is treated
    //as a special appdomain for thread pool purposes. The same logic as the
    //one in managed code for dispatching thread pool requests is repeated here.
    //Namely we continue to process requests until eithere there are none, or
    //the "Quanta has expired". See threadpool.cs for the managed counterpart.

    WorkRequest * pWorkRequest=NULL;
    LPTHREAD_START_ROUTINE wrFunction;
    LPVOID wrContext;

    bool firstIteration = true;
    bool lastOne = false;

    while (*wasNotRecalled)
    {
        m_lock.Init(LOCK_TYPE_DEFAULT);
        {
            SpinLock::Holder slh(&m_lock);
            pWorkRequest = (WorkRequest*) DeQueueUnManagedWorkRequest(&lastOne);
        }

        if (NULL == pWorkRequest)
            break;

        if (firstIteration && !lastOne)
            SetAppDomainRequestsActive();

        firstIteration = false;
        *foundWork = true;

        if (GCHeapUtilities::IsGCInProgress(TRUE))
        {
            // GC is imminent, so wait until GC is complete before executing next request.
            // this reduces in-flight objects allocated right before GC, easing the GC's work
            GCHeapUtilities::WaitForGCCompletion(TRUE);
        }

        PREFIX_ASSUME(pWorkRequest != NULL);
        _ASSERTE(pWorkRequest);

        wrFunction = pWorkRequest->Function;
        wrContext  = pWorkRequest->Context;

        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolDequeue) &&
            !ThreadpoolMgr::AreEtwQueueEventsSpeciallyHandled(wrFunction))
            FireEtwThreadPoolDequeue(pWorkRequest, GetClrInstanceId());

        ThreadpoolMgr::FreeWorkRequest(pWorkRequest);

        if (enableWorkerTracking)
        {
            ThreadpoolMgr::ReportThreadStatus(true);
            (wrFunction) (wrContext);
            ThreadpoolMgr::ReportThreadStatus(false);
        }
        else
        {
            (wrFunction) (wrContext);
        }

        ThreadpoolMgr::NotifyWorkItemCompleted();
        if (ThreadpoolMgr::ShouldAdjustMaxWorkersActive())
        {
            DangerousNonHostedSpinLockTryHolder tal(&ThreadpoolMgr::ThreadAdjustmentLock);
            if (tal.Acquired())
            {
                ThreadpoolMgr::AdjustMaxWorkersActive();
            }
            else
            {
                // the lock is held by someone else, so they will take care of this for us.
            }
        }
        *wasNotRecalled = ThreadpoolMgr::ShouldWorkerKeepRunning();

        Thread *pThread = GetThreadNULLOk();
        if (pThread)
        {
            _ASSERTE(!pThread->IsAbortRequested());
            pThread->InternalReset();
        }

        endTime = GetTickCount();

        if ((endTime - startTime) >= TP_QUANTUM)
        {
           break;
        }
    }

    // if we're exiting for any reason other than the queue being empty, then we need to make sure another thread
    // will visit us later.
    if (NULL != pWorkRequest)
    {
        SetAppDomainRequestsActive();
    }

#endif //DACCESS_COMPILE
}


void ManagedPerAppDomainTPCount::SetAppDomainRequestsActive()
{
    //This function should either be called by managed code or during AD unload, but before
    //the TpIndex is set to unused.
    //
    // Note that there is a separate count in managed code that stays in sync with this one over time.
    // The manage count is incremented before this one, and this one is decremented before the managed
    // one.
    //

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());
    _ASSERTE(m_index.m_dwIndex != UNUSED_THREADPOOL_INDEX);

#ifndef DACCESS_COMPILE
        LONG count = VolatileLoad(&m_numRequestsPending);
        while (true)
        {
            LONG prev = FastInterlockCompareExchange(&m_numRequestsPending, count+1, count);
            if (prev == count)
            {
                ThreadpoolMgr::MaybeAddWorkingWorker();
                ThreadpoolMgr::EnsureGateThreadRunning();
                break;
            }
            count = prev;
        }
#endif
}

void ManagedPerAppDomainTPCount::ClearAppDomainRequestsActive()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    //This function should either be called by managed code or during AD unload, but before
    //the TpIndex is set to unused.

    _ASSERTE(m_index.m_dwIndex != UNUSED_THREADPOOL_INDEX);

    LONG count = VolatileLoad(&m_numRequestsPending);
    while (count > 0)
    {
        LONG prev = FastInterlockCompareExchange(&m_numRequestsPending, 0, count);
        if (prev == count)
            break;
        count = prev;
    }
}

bool ManagedPerAppDomainTPCount::TakeActiveRequest()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    LONG count = VolatileLoad(&m_numRequestsPending);
    while (count > 0)
    {
        LONG prev = FastInterlockCompareExchange(&m_numRequestsPending, count-1, count);
        if (prev == count)
            return true;
        count = prev;
    }
    return false;
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------
//DispatchWorkItem makes sure the right exception handling frames are setup,
//the thread is transitioned into the correct appdomain, and the right managed
//callback is called.
//
void ManagedPerAppDomainTPCount::DispatchWorkItem(bool* foundWork, bool* wasNotRecalled)
{
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    *foundWork = false;
    *wasNotRecalled = true;

    HRESULT hr;
    Thread * pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            return;
        }
    }

    {
        CONTRACTL
        {
            MODE_PREEMPTIVE;
            THROWS;
            GC_TRIGGERS;
        }
        CONTRACTL_END;

        GCX_COOP();

        //
        // NOTE: there is a potential race between the time we retrieve the app
        // domain pointer, and the time which this thread enters the domain.
        //
        // To solve the race, we rely on the fact that there is a thread sync (via
        // GC) between releasing an app domain's handle, and destroying the
        // app domain.  Thus it is important that we not go into preemptive gc mode
        // in that window.
        //

        {
            // This TPIndex may have been recycled since we chose it for workitem dispatch.
            // If so, the new AppDomain will necessarily have zero requests
            // pending (because the destruction of the previous AD that used this TPIndex
            // will have reset this object).  We don't want to call into such an AppDomain.
    // TODO: fix this another way!
    //        if (IsRequestPending())
            {
                ManagedThreadBase::ThreadPool(QueueUserWorkItemManagedCallback, wasNotRecalled);
            }

            if (pThread->IsAbortRequested())
            {
                // thread was aborted, and may not have had a chance to tell us it has work.
                ThreadpoolMgr::SetAppDomainRequestsActive();
                ThreadpoolMgr::QueueUserWorkItem(NULL,
                    NULL,
                    0,
                    FALSE);
            }
        }

        *foundWork = true;
    }
}

#endif // !DACCESS_COMPILE
